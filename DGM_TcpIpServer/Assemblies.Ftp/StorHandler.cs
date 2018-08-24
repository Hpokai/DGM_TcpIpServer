using System;

namespace Assemblies.Ftp
{
    public class StorHandler
    {
        public delegate void StorEventHandler(int nId, string sStorPath);
		static public event StorEventHandler StorPath;

        protected StorHandler()
		{
		}

        public static void SendStorPath(int nId, string sStorPath)
		{
            if (StorPath != null)
			{
                StorPath(nId, sStorPath);
			}
		}
    }
}
