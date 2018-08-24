using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;

namespace Assemblies.Ftp
{
    [Serializable]
    class ConnectDataItem
    {
        private string m_sPassword = "";
        private string m_sStartingDirectory = "C:\\";

        public ConnectDataItem() { }

        public bool AllowDownload { get; set; }
        public bool AllowUpload { get; set; }
        public bool AllowDelete { get; set; }
        public bool AllowCreateFolder { get; set; }

        public string MacAddr { get; set; }
    }

	/// <summary>
	/// Contains the socket read functionality. Works on its own thread since all socket operation is blocking.
	/// </summary>
	class FtpSocketHandler
	{
		#region Member Variables

		private System.Net.Sockets.TcpClient m_theSocket = null;
		private System.Threading.Thread m_theThread = null;
		private int m_nId = 0;
		private const int m_nBufferSize = 65536;
		private FtpConnectionObject m_theCommands = null;
		private FileSystem.IFileSystemClassFactory m_fileSystemClassFactory = null;
        
		#endregion

		#region Events
		
		public delegate void CloseHandler(FtpSocketHandler handler);
		public event CloseHandler Closed;

		#endregion

		#region Construction
		
		public FtpSocketHandler(FileSystem.IFileSystemClassFactory fileSystemClassFactory, int nId)
		{
			m_nId = nId;
			m_fileSystemClassFactory = fileSystemClassFactory;
		}

		#endregion

		#region Methods

		public void Start(System.Net.Sockets.TcpClient socket)
		{
            m_theSocket = socket;
            //Console.WriteLine(IPAddress.Parse(((IPEndPoint)socket.Client.RemoteEndPoint).Address.ToString()));

			m_theCommands = new Assemblies.Ftp.FtpConnectionObject(m_fileSystemClassFactory, m_nId, socket);
			m_theThread = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
			m_theThread.Start();
		}

		public void Stop()
		{
			Assemblies.General.SocketHelpers.Close(m_theSocket);
			m_theThread.Join();
		}

		private void ThreadRun()
		{
			Byte [] abData = new Byte[m_nBufferSize];
            //List<string> mylist = new List<string>();
			try
			{
				int nReceived = m_theSocket.GetStream().Read(abData, 0, m_nBufferSize);

				while (nReceived > 0)
                {
                    m_theCommands.Process(abData).Wait();
                    //mylist.Add(System.Text.Encoding.UTF8.GetString(abData));

                    Array.Clear(abData, 0, abData.Length - 1);
					nReceived = m_theSocket.GetStream().Read(abData, 0, m_nBufferSize);
				}
			}
			catch (System.Net.Sockets.SocketException)
			{
                Console.WriteLine("SocketException");
			}
			catch (System.IO.IOException)
            {
                Console.WriteLine("IOException");
			}
            catch (InvalidOperationException)
            {
                Console.WriteLine("InvalidOperationException");
            }

			FtpServerMessageHandler.SendMessage(m_nId, "Connection Closed");

			if (Closed != null)
			{
				Closed(this);
			}

			m_theSocket.Close();
            LoginData.Get().RemoveUser(m_nId);
		}

		#endregion

		#region Properties
	
		public int Id
		{
			get
			{
				return m_nId;
			}
		}

		#endregion
	}
}
