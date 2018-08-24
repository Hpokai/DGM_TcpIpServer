using System;
using System.Threading.Tasks;

namespace Assemblies.Ftp.FtpCommands
{
	class UserCommandHandler : FtpCommandHandler
	{
		public UserCommandHandler(FtpConnectionObject connectionObject)
			: base("USER", connectionObject)
		{
			
		}

		protected override Task<string> OnProcess(string sMessage)
		{
			ConnectionObject.User = sMessage;
            LoginData.Get().AddUser(ConnectionObject.User);
            LoginData.Get().SetLoginTime(ConnectionObject.User, System.DateTime.Now.ToString());

			return GetMessage(331, string.Format("User {0} logged in, needs password", sMessage));
		}
	}
}
