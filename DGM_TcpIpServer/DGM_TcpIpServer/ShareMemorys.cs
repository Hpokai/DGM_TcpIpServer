using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assemblies.Ftp;
using Assemblies.General;

namespace DGM_TcpIpServer
{
    class ShareMemorys
    {
        static public Assemblies.Ftp.FtpServer TcpIpServer { set; get; }
        static public ConfigurationItems ConfigurationItemsData;

        static public bool isPowerOff { set; get; }
        static public bool isServerOn { set; get; }


        
        static public void initial()
        {
            TcpIpServer = null;
            ConfigurationItemsData = new ConfigurationItems();

            isPowerOff = false;
            isServerOn = false;

        }
    }

    [Serializable]
    class ConfigurationItems
    {
        public ConfigurationItems() 
        {
            ConnectPort = 21;
            MaxConnections = 0;
            ConnectionTimeout = 0;
            AutoActiveAtStartup = false;
        }

        public int ConnectPort { get; set; }
        public int MaxConnections { get; set; }
        public int ConnectionTimeout { get; set; }

        public bool AutoActiveAtStartup { get; set; }
    }

}
