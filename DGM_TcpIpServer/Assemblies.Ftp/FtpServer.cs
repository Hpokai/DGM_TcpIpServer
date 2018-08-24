using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace Assemblies.Ftp
{
	/// <summary>
	/// Listens for incoming connections and accepts them.
	/// Incomming socket connections are then passed to the socket handling class (FtpSocketHandler).
	/// </summary>
	public class FtpServer
	{
		#region Member Variables

		private System.Net.Sockets.TcpListener m_socketListen = null;
		private System.Threading.Thread m_theThread = null;
		private int m_nId = 0;
        private List<FtpSocketHandler> m_apConnections = null;
		private int m_nPort = 0;
		private FileSystem.IFileSystemClassFactory m_fileSystemClassFactory = null;

		#endregion

		#region Events

		public delegate void ConnectionHandler(int nId);
		public event ConnectionHandler ConnectionClosed;
		public event ConnectionHandler NewConnection;

		#endregion

		#region Construction
		
		public FtpServer(FileSystem.IFileSystemClassFactory fileSystemClassFactory)
		{
			m_apConnections = new List<FtpSocketHandler>();
			m_fileSystemClassFactory = fileSystemClassFactory;
		}

		~FtpServer()
		{
			if (m_socketListen != null)
			{
				m_socketListen.Stop();
			}
		}

		#endregion

		#region Methods

		public void Start()
		{
			Start(21);
		}

		public void Start(int nPort)
		{
			m_nPort = nPort;
			m_theThread = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
			m_theThread.Start();
		}

		public void Stop()
		{
            var connections = new List<FtpSocketHandler>(m_apConnections);
            foreach (var handler in connections)
            {
                handler.Stop();
            }
			m_socketListen.Stop();
            m_theThread.Join();

            FtpServerMessageHandler.SendMessage(0, "TCP/IP Server Stoped");
		}

        public int ConnectionCount()
        {
            var connections = new List<FtpSocketHandler>(m_apConnections);
            return connections.Count;
        }
		
		private void ThreadRun()
		{
			m_socketListen = Assemblies.General.SocketHelpers.CreateTcpListener(m_nPort);

			if (m_socketListen != null)
			{			
				m_socketListen.Start();

				FtpServerMessageHandler.SendMessage(0, "TCP/IP Server Started");

				bool fContinue = true;

				while (fContinue)
				{
					System.Net.Sockets.TcpClient socket = null;

					try
					{
						socket = m_socketListen.AcceptTcpClient();
					}
					catch (System.Net.Sockets.SocketException)
					{
						fContinue = false;
					}
					finally
					{
						if (socket == null)
						{
							fContinue = false;
						}
						else
						{
							socket.NoDelay = false;

							m_nId++;

							FtpServerMessageHandler.SendMessage(m_nId, "New Connection");

							SendAcceptMessage(socket).Wait();
							InitialiseSocketHandler(socket);
						}
                    }
                }
			}
			else
			{
				FtpServerMessageHandler.SendMessage(0, "Error in starting TCP/IP server");
			}
		}

		private async Task SendAcceptMessage(System.Net.Sockets.TcpClient socket)
		{
			await Assemblies.General.SocketHelpers.Send(socket, System.Text.Encoding.ASCII.GetBytes("220 TCP/IP Server Ready\r\n"));
		}

		private void InitialiseSocketHandler(System.Net.Sockets.TcpClient socket)
		{
			FtpSocketHandler handler = new FtpSocketHandler(m_fileSystemClassFactory, m_nId);
			handler.Start(socket);

			m_apConnections.Add(handler);

			handler.Closed += new Assemblies.Ftp.FtpSocketHandler.CloseHandler(handler_Closed);

			if (NewConnection != null)
			{
				NewConnection(m_nId);
			}
		}

		#endregion

		#region Event Handlers

		private void handler_Closed(FtpSocketHandler handler)
		{
			m_apConnections.Remove(handler);

			if (ConnectionClosed != null)
			{
				ConnectionClosed(handler.Id);
			}
		}

		#endregion
	}
}
