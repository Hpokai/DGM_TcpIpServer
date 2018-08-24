using System;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace Assemblies.Ftp
{
    [Serializable]
    class LoginDataItem
    {
        public LoginDataItem()
        {
            this.nID = 0;
            this.IpAddr = string.Empty;
            this.MacAddr = string.Empty;
            this.LoginTime = string.Empty;
            this.isLoginSuccess = false;
            this.isOnList = false;
        }

        public int nID { set; get; }
        public string IpAddr { set; get; }
        public string MacAddr { set; get; }
        public string LoginTime { set; get; }
        public bool isLoginSuccess { set; get; }
        public bool isOnList { set; get; }

    }
    public class LoginData
    {
        #region Member Variables

        private System.Collections.Hashtable m_mapLoginData = null;

        static private LoginData m_theObject = null;

        #endregion

        #region Construction

        protected LoginData()
        {
            this.m_mapLoginData = new System.Collections.Hashtable();
        }

        #endregion

        #region Properties

        static public LoginData Get()
        {
            if (m_theObject == null)
            {
                m_theObject = new LoginData();
            }

            return m_theObject;
        }

        public string[] Users
        {
            get
            {
                System.Collections.ICollection collectionUsers = this.m_mapLoginData.Keys;
                string[] asUsers = new string[collectionUsers.Count];

                int nIndex = 0;

                foreach (string sUser in collectionUsers)
                {
                    asUsers[nIndex] = sUser;
                    nIndex++;
                }

                return asUsers;
            }
        }

        public int LoginCount
        {
            get
            {
                return this.m_mapLoginData.Count;
            }
        }

        #endregion

        #region Methods

        private LoginDataItem GetLoginItem(string sUser)
        {
            return this.m_mapLoginData[sUser] as LoginDataItem;
        }

        public void AddUser(string sUser)
        {
            this.m_mapLoginData.Add(sUser, new LoginDataItem());
        }
        public void RemoveUser(string sUser)
        {
            this.m_mapLoginData.Remove(sUser);
        }
        public void RemoveUser(int nId)
        {
            foreach (var user in Users)
            {
                if (this.GetLoginItem(user).nID == nId)
                {
                    this.m_mapLoginData.Remove(user);
                    break;
                }
            }
        }

        public int GetLoginNID(string sUser)
        {
            LoginDataItem item = GetLoginItem(sUser);

            if (item != null)
            {
                return item.nID;
            }
            else
            {
                return 0;
            }
        }
        public void SetLoginNID(string sUser, int nId)
        {
            LoginDataItem item = GetLoginItem(sUser);

            if (item != null)
            {
                item.nID = nId;
            }
        }

        public string GetLoginIpAddr(string sUser)
        {
            LoginDataItem item = GetLoginItem(sUser);

            if (item != null)
            {
                return item.IpAddr;
            }
            else
            {
                return "";
            }
        }
        public void SetLoginIpAddr(string sUser, string IpAddr)
        {
            LoginDataItem item = GetLoginItem(sUser);

            if (item != null)
            {
                item.IpAddr = IpAddr;
            }
        }

        public string GetLoginTime(string sUser)
        {
            LoginDataItem item = GetLoginItem(sUser);

            if (item != null)
            {
                return item.LoginTime;
            }
            else
            {
                return "";
            }
        }
        public void SetLoginTime(string sUser, string LoginTime)
        {
            LoginDataItem item = GetLoginItem(sUser);

            if (item != null)
            {
                item.LoginTime = LoginTime;
            }
        }

        public string GetLoginMAC(string sUser)
        {
            LoginDataItem item = GetLoginItem(sUser);

            if (item != null)
            {
                return item.MacAddr;
            }
            else
            {
                return string.Empty;
            }
        }
        public void SetLoginMAC(string sUser, string MacAddr)
        {
            LoginDataItem item = GetLoginItem(sUser);

            if (item != null)
            {
                item.MacAddr = MacAddr;
            }
        }

        public bool GetLoginSuccess(string sUser)
        {
            LoginDataItem item = GetLoginItem(sUser);

            if (item != null)
            {
                return item.isLoginSuccess;
            }
            else
            {
                return false;
            }
        }
        public void SetLoginSuccess(string sUser, bool isLoginSuccess)
        {
            LoginDataItem item = GetLoginItem(sUser);

            if (item != null)
            {
                item.isLoginSuccess = isLoginSuccess;
            }
        }

        public bool GetOnList(string sUser)
        {
            LoginDataItem item = GetLoginItem(sUser);

            if (item != null)
            {
                return item.isOnList;
            }
            else
            {
                return false;
            }
        }
        public void SetOnList(string sUser, bool isOnList)
        {
            LoginDataItem item = GetLoginItem(sUser);

            if (item != null)
            {
                item.isLoginSuccess = isOnList;
            }
        }

        public bool HasUser(string sUser)
        {
            LoginDataItem item = GetLoginItem(sUser);
            return item != null;
        }

        #endregion
    }

	/// <summary>
	/// Processes incoming messages and passes the data on to the relevant handler class.
	/// </summary>
	public class FtpConnectionObject : FtpConnectionData
	{
		#region Member Variables

		private System.Collections.Hashtable m_theCommandHashTable = null;
		private FileSystem.IFileSystemClassFactory m_fileSystemClassFactory = null;
		
		#endregion

		#region Construction

		public FtpConnectionObject(FileSystem.IFileSystemClassFactory fileSystemClassFactory, int nId, System.Net.Sockets.TcpClient socket)
			: base(nId, socket)
		{
			m_theCommandHashTable = new System.Collections.Hashtable();
			m_fileSystemClassFactory = fileSystemClassFactory;
			
			LoadCommands();
		}

		#endregion

		#region Methods

		public bool Login(string sPassword)
		{
			FileSystem.IFileSystem fileSystem = m_fileSystemClassFactory.Create(this.User, sPassword);

			if (fileSystem == null)
			{
				return false;
			}
			SetFileSystemObject(fileSystem);
            LoginData.Get().SetLoginNID(this.User, this.Id);
            FtpServerMessageHandler.SendMessage(this.Id, this.GetIpAddress());
			return true;
		}

        public string GetIpAddress()
        { 
            return IPAddress.Parse(((IPEndPoint)Socket.Client.RemoteEndPoint).Address.ToString()).ToString();
        }

        public bool CheckMac(string sMac)
        {
            if (UserData.Get().HasUser(this.User) && UserData.Get().GetModuleMAC(this.User) == sMac)
            {
                // FtpServerMessageHandler.SendMessage(Id, this.GetIpAddress());
                LoginData.Get().SetLoginIpAddr(this.User, this.GetIpAddress());
                LoginData.Get().SetLoginMAC(this.User, sMac);
                LoginData.Get().SetLoginSuccess(this.User, true);
                return true;
            }
            else
            {
                LoginData.Get().RemoveUser(this.User);
                return false;
            }
        }

		private void LoadCommands()
		{
			AddCommand(new FtpCommands.UserCommandHandler(this));
			AddCommand(new FtpCommands.PasswordCommandHandler(this));
			AddCommand(new FtpCommands.QuitCommandHandler(this));
			AddCommand(new FtpCommands.CwdCommandHandler(this));
			AddCommand(new FtpCommands.PortCommandHandler(this));
			AddCommand(new FtpCommands.PasvCommandHandler(this));
			AddCommand(new FtpCommands.ListCommandHandler(this));
			AddCommand(new FtpCommands.NlstCommandHandler(this));
			AddCommand(new FtpCommands.PwdCommandHandler(this));
			AddCommand(new FtpCommands.XPwdCommandHandler(this));
			AddCommand(new FtpCommands.TypeCommandHandler(this));
			AddCommand(new FtpCommands.RetrCommandHandler(this));
			AddCommand(new FtpCommands.NoopCommandHandler(this));
			AddCommand(new FtpCommands.SizeCommandHandler(this));
			AddCommand(new FtpCommands.DeleCommandHandler(this));
			AddCommand(new FtpCommands.AlloCommandHandler(this));
			AddCommand(new FtpCommands.StoreCommandHandler(this));
			AddCommand(new FtpCommands.MakeDirectoryCommandHandler(this));
			AddCommand(new FtpCommands.RemoveDirectoryCommandHandler(this));
			AddCommand(new FtpCommands.AppendCommandHandler(this));
			AddCommand(new FtpCommands.RenameStartCommandHandler(this));
			AddCommand(new FtpCommands.RenameCompleteCommandHandler(this));
			AddCommand(new FtpCommands.XMkdCommandHandler(this));
            AddCommand(new FtpCommands.XRmdCommandHandler(this));
            AddCommand(new FtpCommands.MacCommandHandler(this));
		}

		private void AddCommand(FtpCommands.FtpCommandHandler handler)
		{
			m_theCommandHashTable.Add(handler.Command, handler);
		}

		public async Task Process(Byte [] abData)
		{
            string sMessage = System.Text.Encoding.UTF8.GetString(abData);
			sMessage = sMessage.Substring(0, sMessage.IndexOf('\r'));

            //Console.WriteLine(sMessage);
			FtpServerMessageHandler.SendMessage(Id, sMessage);
            StorHandler.SendStorPath(Id, sMessage);

			string sCommand;
			string sValue;

			int nSpaceIndex = sMessage.IndexOf(' ');

			if (nSpaceIndex < 0)
			{
				sCommand = sMessage.ToUpper();
				sValue = "";
			}
			else
			{
				sCommand = sMessage.Substring(0, nSpaceIndex).ToUpper();
				sValue = sMessage.Substring(sCommand.Length + 1);
			}

			FtpCommands.FtpCommandHandler handler = m_theCommandHashTable[sCommand] as FtpCommands.FtpCommandHandler;

			if (handler == null)
			{
				FtpServerMessageHandler.SendMessage(Id, string.Format("\"{0}\" : Unknown command", sCommand));
				await Assemblies.General.SocketHelpers.Send(Socket, "550 Unknown command\r\n");
			}
			else
			{
				await handler.Process(sValue);
			}
		}

		#endregion
	}
}
