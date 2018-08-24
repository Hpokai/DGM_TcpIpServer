using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using FluentFTP;

namespace DGM_TcpIpServer
{
    class ToolsDifference
    {
        public enum PROCESS { ALL, ONE, NONE };
        public enum TRANSPORT { REQUIRE, WAIT_DATA, WAIT_SEND_DATA, SEND_DATA, UPDATED, NONE };

        #region Member Variables
        private Excel.Application excel_app = null;
        private Excel.Workbook excel_workbook = null;
        private string FileFullName = string.Empty;
        private List<string> process_list = null;
        private System.Threading.Thread getToolsDataThread = null;
        private System.Threading.Thread getExcelDataThread = null;
        private bool isRunning = false;
        private bool isNewToolListGot = false,
                     isCurrentToolListGot = false;
        public PROCESS process_state = PROCESS.NONE;
        public TRANSPORT transport_state = TRANSPORT.NONE;
        public List<int> current_tool_list = null,
                         same_tool_list = null,
                         other_tool_list = null;
        public int spindle_tool = 0;
        public string selected_process = string.Empty;
        public string remote_ip = string.Empty;
        private FtpClient client = null;
        #endregion

        #region Events

        public delegate void UpdateHandler(string msg);
        public event UpdateHandler DataUpdated;
        public event UpdateHandler LogMessage;

        #endregion

        #region Construction
        public ToolsDifference()
        {
            this.process_list = new List<string>();
            this.current_tool_list = new List<int>();
            this.same_tool_list = new List<int>();
            this.other_tool_list = new List<int>();

            this.InitailExcel();

            this.isRunning = true;

            this.getToolsDataThread = new System.Threading.Thread(new System.Threading.ThreadStart(this.ToolsDataThreadRun));
            this.getToolsDataThread.Start();

            this.getExcelDataThread = new System.Threading.Thread(new System.Threading.ThreadStart(this.ExcelDataThreadRun));
            this.getExcelDataThread.Start();

            this.client = new FtpClient();
            this.client.Encoding = Encoding.UTF8;
        }
        ~ToolsDifference()
        {
            this.Close();
        }
        #endregion

        #region Methods
        public void SendLogMessage(string msg)
        {
            if (LogMessage != null)
            {
                LogMessage(msg);
            }
        }
        private void InitailExcel()
        {
            //檢查PC有無Excel在執行
            bool flag = false;
            foreach (var item in Process.GetProcesses())
            {
                if (item.ProcessName == "EXCEL")
                {
                    flag = true;
                    break;
                }
            }

            if (!flag)
            {
                this.excel_app = new Excel.Application();
            }
            else
            {
                object obj = Marshal.GetActiveObject("Excel.Application");//引用已在執行的Excel
                this.excel_app = obj as Excel.Application;
            }

            this.excel_app.Visible = false;//設false效能會比較好
        }

        private void ToolsDataThreadRun()
        {
            while (true == isRunning)
            {
                switch (this.transport_state)
                {
                    case TRANSPORT.REQUIRE:
                        if (true == this.client.IsConnected) this.client.Disconnect();
                        this.client.Host = this.remote_ip;
                        this.client.Credentials = new System.Net.NetworkCredential("pi", "raspberry");
                        this.client.Connect();

                        if (true == this.client.IsConnected)
                        {
                            this.client.UploadFile(System.Text.Encoding.Default.GetBytes(string.Empty), @"/home/pi/.wine/drive_c/Program Files/DGM/TypeB/Data/RequireToolTable.txt");

                            this.SendLogMessage("Require cmd is sent!");
                            this.transport_state = TRANSPORT.WAIT_DATA;
                        }
                        break;
                    case TRANSPORT.WAIT_DATA:
                        if (true == this.client.FileExists(@"/home/pi/.wine/drive_c/Program Files/DGM/TypeB/Data/CurrentToolTable_ok.txt"))
                        {
                            this.client.DownloadFile(@".\CurrentToolTable.txt", @"/home/pi/.wine/drive_c/Program Files/DGM/TypeB/Data/CurrentToolTable.txt");
                            
                            this.client.DeleteFile(@"/home/pi/.wine/drive_c/Program Files/DGM/TypeB/Data/CurrentToolTable.txt");
                            this.client.DeleteFile(@"/home/pi/.wine/drive_c/Program Files/DGM/TypeB/Data/CurrentToolTable_ok.txt");

                            this.SendLogMessage("File is downloaded!");
                            this.isCurrentToolListGot = true;
                            this.CompareTools();
                            this.transport_state = TRANSPORT.SEND_DATA;
                        }
                        break;
                    case TRANSPORT.WAIT_SEND_DATA:
                        break;
                    case TRANSPORT.SEND_DATA:
                        this.PackageNewToolsData();
                        this.SendLogMessage("Package New Tools Data!");

                        this.client.UploadFile(@".\HighlightTable.txt", @"/home/pi/.wine/drive_c/Program Files/DGM/TypeB/Data/HighlightTable.txt");
                        this.client.UploadFile(@".\NewToolTable.txt", @"/home/pi/.wine/drive_c/Program Files/DGM/TypeB/Data/NewToolTable.txt");
                        this.client.UploadFile(System.Text.Encoding.Default.GetBytes(string.Empty), @"/home/pi/.wine/drive_c/Program Files/DGM/TypeB/Data/NewToolTable_ok.txt");
                        this.SendLogMessage("New Tools Data Uploaded !");

                        this.transport_state = TRANSPORT.UPDATED;
                        break;
                    case TRANSPORT.UPDATED:
                        //if (true == this.client.FileExists(@"/home/pi/.wine/drive_c/Program Files/DGM/TypeB/Data/UpdateDone.txt"))
                        //{
                        //    this.SendLogMessage("All UpDate is Done!");
                        //    this.SendLogMessage("You could start next stage now!");

                        //    this.transport_state = TRANSPORT.NONE;
                        //}
                        if (null != this.excel_workbook) 
                            this.excel_workbook.Close(false);
                        this.transport_state = TRANSPORT.NONE;
                        break;
                    default:
                        break;
                }


                System.Threading.Thread.Sleep(200);
            }

            if ((true == this.client.IsConnected) && (null != this.client))
                this.client.Disconnect();
        }
        private void ExcelDataThreadRun()
        {
            while (true == isRunning)
            {
                switch (this.process_state)
                {
                    case PROCESS.ALL:
                        this.same_tool_list = this.ListAllProcessNewTools();
                        this.isNewToolListGot = true;
                        this.process_state = PROCESS.NONE;
                        this.SendLogMessage("Acquire all process data!");
                        break;
                    case PROCESS.ONE:
                        this.same_tool_list = this.ListOneProcessNewTools(this.selected_process);
                        this.isNewToolListGot = true;
                        this.process_state = PROCESS.NONE;
                        this.SendLogMessage(string.Format("Acquire {0} process data!", this.selected_process));
                        break;
                    default:
                        break;
                }
                System.Threading.Thread.Sleep(500);
            }
        }

        private void PackageNewToolsData()
        {
            using (StreamWriter sw = new StreamWriter(@".\NewToolTable.txt"))
            {
                sw.WriteLine(this.other_tool_list.Count());
                foreach (var item in this.other_tool_list)
                {
                    sw.WriteLine(item);
                }
            }
            using (StreamWriter sw = new StreamWriter(@".\HighlightTable.txt"))
            {
                sw.WriteLine(this.same_tool_list.Count());
                foreach (var item in this.same_tool_list)
                {
                    sw.WriteLine(item);
                }
            }
        }

        private void CompareTools()
        {
            if ((true == this.isNewToolListGot) && (true == this.isCurrentToolListGot))
            {
                this.isNewToolListGot = false;
                this.isCurrentToolListGot = false;
                this.SendLogMessage("Start to compare current tools & new tools!");

                int row_count;
                this.current_tool_list = this.LisCurrentTools(out row_count);
                int[] arr = new int[row_count];
                Array.Clear(arr, 0, arr.Length);

                List<int> temp_found_list = new List<int>(arr);
                List<int> temp_not_found_list = new List<int>();
                foreach (var item in this.same_tool_list)
                {
                    int index = this.current_tool_list.IndexOf(item);
                    if (-1 == index)
                    {
                        temp_not_found_list.Add(item);
                    }
                    else
                    {
                        temp_found_list[index] = item;
                    }
                }
                this.same_tool_list = temp_found_list;
                this.other_tool_list = temp_not_found_list;

                if (true == this.other_tool_list.Contains(this.spindle_tool))
                {
                    this.other_tool_list.Remove(this.spindle_tool);
                    this.same_tool_list.Add(this.spindle_tool);
                }
                else
                {
                    this.same_tool_list.Add(0);
                }

                if (DataUpdated != null)
                {
                    DataUpdated("Compared!");
                }
                this.SendLogMessage("Compare current tools & new tools Finish!");
            }
        }

        private List<int> LisCurrentTools(out int row_count)
        {
            List<int> tool_list = new List<int>();
            row_count = 0;
            using (System.IO.StreamReader file = new System.IO.StreamReader(@".\CurrentToolTable.txt"))
            {
                row_count = Convert.ToInt16(file.ReadLine());
                for (int i = 0; i < row_count; i++)
                {   
                    tool_list.Add(Convert.ToInt16(file.ReadLine()));
                }
                this.spindle_tool = Convert.ToInt16(file.ReadLine());
            }

            return tool_list;
        }

        public void Close()
        {
            this.isRunning = false;
        }

        public List<string> GetWorkOrderProcess(string target_dir, string work_order_num)
        {
            this.SendLogMessage("Ready to require Tools Data!");
            this.process_list.Clear(); 

            DirectoryInfo di = new DirectoryInfo(target_dir);
            foreach (FileInfo fileName in di.GetFiles(work_order_num + "*.*", SearchOption.AllDirectories))
            {
                this.FileFullName = fileName.FullName;
            }

            this.excel_workbook = excel_app.Workbooks.Open(this.FileFullName, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);//開啟舊檔案

            foreach (Excel.Worksheet item in this.excel_workbook.Worksheets)
            {
                if (true == item.Name.Contains("G"))
                {
                    this.process_list.Add(item.Name);
                }
            }

            return process_list;
        }
        private List<int> ListOneProcessNewTools(string sheet_name)
        {
            List<int> one_list = new List<int>();

            Excel.Worksheet sheet = this.excel_workbook.Sheets[sheet_name];
            Excel.Range range = null, range_t = null;
            int end_row = 100;

            range = sheet.get_Range("A:A");
            foreach (Excel.Range item in range)
            {
                if ("工件夾持" == item.Cells.Text)
                {
                    end_row = item.Row;
                    break;
                }
            }

            if ("?" != sheet.get_Range("B" + (end_row + 1).ToString()).Cells.Text)
            {
                range = sheet.get_Range("D7", "D" + (end_row - 1).ToString());
                range_t = sheet.get_Range("B1", "B" + (end_row - 1).ToString());

                foreach (Excel.Range item in range)
                {
                    if ((null == item.Cells.Value) || ("0" == Convert.ToString(item.Cells.Value))) break;
                    else
                    {
                        if (false == one_list.Contains(Convert.ToInt16(range_t.Cells[item.Row].Value2)))
                        {
                            one_list.Add(Convert.ToInt16(range_t.Cells[item.Row].Value2));
                        }
                    }
                }
            }

            return one_list;
        }
        private List<int> ListAllProcessNewTools()
        {
            List<int> all_list = new List<int>();

            foreach (Excel.Worksheet item in this.excel_workbook.Worksheets)
            {
                if (true == item.Name.Contains("G"))
                {
                    List<int> temp_one = this.ListOneProcessNewTools(item.Name);

                    foreach (int tool in temp_one)
                    {
                        if (false == all_list.Contains(tool))
                        {
                            all_list.Add(tool);
                        }
                    }
                }
            }

            // this.excel_workbook.Close();

            return all_list;
        }


        #endregion
    }
}
