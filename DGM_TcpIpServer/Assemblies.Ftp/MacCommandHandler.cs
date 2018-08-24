using System;
using System.Threading.Tasks;

namespace Assemblies.Ftp.FtpCommands
{
    class MacCommandHandler : FtpCommandHandler
    {
        public MacCommandHandler(FtpConnectionObject connectionObject)
            : base("LMAC", connectionObject)
        {

        }

        protected override Task<string> OnProcess(string sMessage)
        {
            if (ConnectionObject.CheckMac(sMessage))
            {
                return GetMessage(220, "MAC ok, TCP/IP server ready");
            }
            else
            {
                return GetMessage(534, "Special Code incorrect, Started to disconnect!");
            }
        }
    }
}