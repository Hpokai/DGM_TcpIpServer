using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zip;

namespace DGM_TcpIpServer
{
    public class ExtractFiles
    {
        #region Member Variables
        private System.Collections.Hashtable extract_hashtable = null;
		#endregion

        #region Construction
        public ExtractFiles()
        {
            this.extract_hashtable = new System.Collections.Hashtable();
            Console.WriteLine("Construction");
        }
        ~ExtractFiles() { }
        #endregion

        #region Methods
        public void AddPath(string user, string path, string file)
        {
            Console.WriteLine("Add");
            this.extract_hashtable.Add(user, new DoExtract(path,file));
        }
        public void RemovePath(string user)
        {
            Console.WriteLine("Remove");
            if (this.extract_hashtable.ContainsKey(user))
            {
                DoExtract item = this.extract_hashtable[user] as DoExtract;
                item.Close();

                System.Threading.Thread.Sleep(2000);
                this.extract_hashtable.Remove(user);
            }
        }
        public bool HasUser(string user)
        {
            return this.extract_hashtable.ContainsKey(user);
        }
        public void ChangePath(string user, string path, string file)
        {
            Console.WriteLine("Change");
            if (this.extract_hashtable.ContainsKey(user))
            {
                DoExtract item = this.extract_hashtable[user] as DoExtract;
                item.full_path = path;
                item.file_name = file;
            }
        }
        #endregion

    }

    class DoExtract
    {
        #region Member Variables
        private System.Threading.Thread theThread = null;
        private bool isRunning;
        public string full_path = string.Empty,
                      file_name = string.Empty;
        public bool isClosing;
        private System.Collections.Hashtable month_hashtable = null, day_hashtable = null, hour_hashtable = null, num_hashtable = null;
        #endregion

        #region Construction
        public DoExtract(string path, string file)
        {
            Console.WriteLine("DoExtract");
            this.Encryption();
            this.isClosing = false;
            this.full_path = path;
            this.file_name = file;
            this.StartExtract();
        }

        ~DoExtract() { }
        #endregion

        #region Methods
        private void Encryption()
        {
            this.month_hashtable = new System.Collections.Hashtable();
            this.month_hashtable.Add("Line", "01");
            this.month_hashtable.Add("Make", "02");
            this.month_hashtable.Add("Nike", "03");
            this.month_hashtable.Add("Over", "04");
            this.month_hashtable.Add("Pink", "05");
            this.month_hashtable.Add("Quit", "06");
            this.month_hashtable.Add("Rest", "07");
            this.month_hashtable.Add("Stop", "08");
            this.month_hashtable.Add("Type", "09");
            this.month_hashtable.Add("Upon", "10");
            this.month_hashtable.Add("Vice", "11");
            this.month_hashtable.Add("Wind", "12");

            this.day_hashtable = new System.Collections.Hashtable();
            this.day_hashtable.Add("eli", "0");
            this.day_hashtable.Add("fox", "1");
            this.day_hashtable.Add("gay", "2");
            this.day_hashtable.Add("hot", "3");
            this.day_hashtable.Add("ice", "4");
            this.day_hashtable.Add("jim", "5");
            this.day_hashtable.Add("ken", "6");
            this.day_hashtable.Add("leo", "7");
            this.day_hashtable.Add("max", "8");
            this.day_hashtable.Add("nat", "9");

            this.hour_hashtable = new System.Collections.Hashtable();
            this.hour_hashtable.Add("annie", "00");
            this.hour_hashtable.Add("brave", "01");
            this.hour_hashtable.Add("coach", "02");
            this.hour_hashtable.Add("dance", "03");
            this.hour_hashtable.Add("eight", "04");
            this.hour_hashtable.Add("funny", "05");
            this.hour_hashtable.Add("ghost", "06");
            this.hour_hashtable.Add("hello", "07");
            this.hour_hashtable.Add("issue", "08");
            this.hour_hashtable.Add("jaunt", "09");
            this.hour_hashtable.Add("korea", "10");
            this.hour_hashtable.Add("leave", "11");
            this.hour_hashtable.Add("might", "12");
            this.hour_hashtable.Add("nurse", "13");
            this.hour_hashtable.Add("often", "14");
            this.hour_hashtable.Add("place", "15");
            this.hour_hashtable.Add("quite", "16");
            this.hour_hashtable.Add("right", "17");
            this.hour_hashtable.Add("super", "18");
            this.hour_hashtable.Add("teeth", "19");
            this.hour_hashtable.Add("until", "20");
            this.hour_hashtable.Add("value", "21");
            this.hour_hashtable.Add("which", "22");
            this.hour_hashtable.Add("xerox", "23");

            this.num_hashtable = new System.Collections.Hashtable();
            this.num_hashtable.Add("48", "0");
            this.num_hashtable.Add("49", "1");
            this.num_hashtable.Add("50", "2");
            this.num_hashtable.Add("51", "3");
            this.num_hashtable.Add("52", "4");
            this.num_hashtable.Add("53", "5");
            this.num_hashtable.Add("54", "6");
            this.num_hashtable.Add("55", "7");
            this.num_hashtable.Add("56", "8");
            this.num_hashtable.Add("57", "9");
        }

        private void StartExtract()
        {
            this.theThread = new System.Threading.Thread(new System.Threading.ThreadStart(this.ThreadRun));
            this.theThread.Start();
        }

        private string GetPassword(string file_name)
        {
            string pw_info = file_name.Substring(file_name.IndexOf("__dgm__.") + 8);

            string[] sub = new string[9];
            sub[0] = pw_info.Substring(0, 1);
            sub[1] = pw_info.Substring(1, 2);
            sub[2] = pw_info.Substring(12, 4);
            sub[3] = pw_info.Substring(5, 1);
            sub[4] = pw_info.Substring(9, 3);
            sub[5] = pw_info.Substring(16);
            sub[6] = pw_info.Substring(3, 2);
            sub[7] = pw_info.Substring(6, 2);
            sub[8] = pw_info.Substring(8, 1);

            string[] pw = new string[9];
            pw[0] = sub[0];
            pw[1] = this.num_hashtable[sub[1]].ToString();
            pw[2] = this.month_hashtable[sub[2]].ToString();
            pw[3] = sub[3];
            pw[4] = this.day_hashtable[sub[4]].ToString();
            pw[5] = this.hour_hashtable[sub[5]].ToString();
            pw[6] = sub[6];
            pw[7] = this.num_hashtable[sub[7]].ToString();
            pw[8] = sub[8];

            return (pw[0] + pw[1] + pw[2] + pw[3] + pw[4] + pw[5] + pw[6] + pw[7] + pw[8]);
        }

        private void ThreadRun()
        {
            Console.WriteLine("{0}: ThreadRun.", System.Threading.Thread.CurrentThread.ManagedThreadId);
            string ExistingZipFile = string.Empty;
            string TargetDirectory = string.Empty;

            this.isRunning = true;
            while (true == isRunning)
            {
                ExistingZipFile = this.full_path + this.file_name;
                 TargetDirectory = this.full_path;
                if (System.IO.File.Exists(ExistingZipFile))
                {
                    Console.WriteLine("{0}: Exists", System.Threading.Thread.CurrentThread.ManagedThreadId);
                    try
                    {
                        try
                        {
                            using (ZipFile zip = ZipFile.Read(ExistingZipFile))
                            {
                                //zip.Password = "1234";
                                zip.Password = this.GetPassword(ExistingZipFile);
                                zip.ExtractAll(TargetDirectory, ExtractExistingFileAction.OverwriteSilently);
                            }
                        }
                        catch (Ionic.Zip.ZipException e)
                        {
                            Console.WriteLine(e.Message);
                        }

                        try
                        {
                            System.IO.File.Delete(ExistingZipFile);
                        }
                        catch (System.IO.IOException e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                    catch (System.IO.IOException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                System.Threading.Thread.Sleep(1000);
            }

            this.isClosing = true;
            Console.WriteLine("{0}: isClosing", System.Threading.Thread.CurrentThread.ManagedThreadId);
        }

        public void Close()
        {
            Console.WriteLine("{0}: Close", System.Threading.Thread.CurrentThread.ManagedThreadId);
            this.isRunning = false;
        }
        #endregion
    }

}
