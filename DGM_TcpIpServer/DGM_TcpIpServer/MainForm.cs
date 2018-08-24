using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Security;
using System.Security.Permissions;
using FluentFTP;
using System.Net;

namespace DGM_TcpIpServer
{
    public partial class MainForm : Form
    {
        enum TOOLS_DIFF_COLUMN_HEADER { No_00, CurT_00, No_10, CurT_10, No_20, CurT_20,};

        private System.Collections.Hashtable map_IDtoUser = null;
        private ExtractFiles extract_files = null;
        private ToolsDifference tools_difference = null;
        private Form_ToolsDiffIpSettings form_ToolsDiffIpSettings = null;

        public MainForm()
        {
            InitializeComponent();
            ShareMemorys.initial();

            Assemblies.Ftp.FtpServerMessageHandler.Message += new Assemblies.Ftp.FtpServerMessageHandler.MessageEventHandler(this.MessageHandler_Message);
            this.MessageHandler_Message(000, "Copyright of JGP Automation Mechanics!");
            this.extract_files = new ExtractFiles();
            this.map_IDtoUser = new System.Collections.Hashtable();
            Assemblies.Ftp.StorHandler.StorPath += new Assemblies.Ftp.StorHandler.StorEventHandler(this.StorPathHandler_StorPath);

            System.Threading.Thread.Sleep(100);
            ShareMemorys.TcpIpServer = new Assemblies.Ftp.FtpServer(new Assemblies.Ftp.FileSystem.StandardFileSystemClassFactory());
            ShareMemorys.TcpIpServer.ConnectionClosed += new Assemblies.Ftp.FtpServer.ConnectionHandler(this.TcpIpServer_ConnectionClosed);
            ShareMemorys.TcpIpServer.NewConnection += new Assemblies.Ftp.FtpServer.ConnectionHandler(TcpIpServer_NewConnection);

            // for CNC Tools Diff
            this.form_ToolsDiffIpSettings = new Form_ToolsDiffIpSettings();
            this.form_ToolsDiffIpSettings.IpUpdated += new Form_ToolsDiffIpSettings.UpdateHandler(form_ToolsDiffIpSettings_IpUpdated);
            this.form_ToolsDiffIpSettings.CallIpUpdated("Update");
            this.InitialDataGridView();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // set file permission for all local files.
            FileIOPermission f = new FileIOPermission(PermissionState.None);
            f.AllLocalFiles = FileIOPermissionAccess.Read;
            try
            {
                f.Demand();
            }
            catch (SecurityException s)
            {
                Console.WriteLine(s.Message);
            }
            ////////////////////////////////////////////////

            this.users_listView.FullRowSelect = true;
            this.loadUserData();
            this.LoadConfiguration();

            this.isLoadingConfiguration = true;
            this.connectPort_textBox.Text = ShareMemorys.ConfigurationItemsData.ConnectPort.ToString();
            this.maxConnections_textBox.Text = ShareMemorys.ConfigurationItemsData.MaxConnections.ToString();
            this.connectionTimeout_textBox.Text = ShareMemorys.ConfigurationItemsData.ConnectionTimeout.ToString();
            this.autoActiveAtStartup_checkBox.Checked = ShareMemorys.ConfigurationItemsData.AutoActiveAtStartup;
            this.isLoadingConfiguration = false;

            if (true == this.autoActiveAtStartup_checkBox.Checked) { this.startServer_button.PerformClick(); }

            this.sendMode_comboBox.SelectedIndex = 0;
        }

        private void StorPathHandler_StorPath(int nId, string sStorPath)
        {
            string[] str = sStorPath.Split(' ');
            if ("USER" == str[0])
            {
                this.map_IDtoUser.Add(nId, str[1]);
            }
            else if (("STOR" == str[0]) && (str[1].Contains("__dgm__")))
            {
                string user = this.map_IDtoUser[nId].ToString(),
                       path = Assemblies.Ftp.UserData.Get().GetUserStartingDirectory(this.map_IDtoUser[nId].ToString()),
                       file = str[1].Replace("/", "\\");
                Console.WriteLine("{0}: {1} ({2})", nId, sStorPath, user);

                if (!this.extract_files.HasUser(user))
                {
                    this.extract_files.AddPath(user, path, file);
                }
                else
                {
                    this.extract_files.ChangePath(user, path, file);
                    Console.WriteLine("has user");
                }
            }
        }

        private void MessageHandler_Message(int nId, string sMessage)
        {
            if (!ShareMemorys.isPowerOff)
            {
                var writeMessageAction = new Action(
                    () =>
                    {
                        this.logs_listBox.BeginUpdate();

                        int nItem = this.logs_listBox.Items.Add(string.Format("({0}) <{1}> {2}", nId, System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), sMessage));

                        if (this.logs_listBox.Items.Count > 5000)
                        {
                            this.logs_listBox.Items.RemoveAt(0);
                        }

                        if (this.logs_listBox.SelectedIndex < 0)
                        {
                            this.logs_listBox.TopIndex = nItem;
                        }
                        else if (this.logs_listBox.SelectedIndex == nItem - 1)
                        {
                            this.logs_listBox.SelectedIndex = nItem;
                        }

                        this.logs_listBox.EndUpdate();
                    });

                if (this.logs_listBox.InvokeRequired)
                    this.logs_listBox.Invoke(writeMessageAction);
                else
                    writeMessageAction();
            }
        }

        private void TcpIpServer_ConnectionClosed(int nId)
        {
            //this.MessageHandler_Message(nId, "Connection Closed");
            if (this.map_IDtoUser.ContainsKey(nId))
            {
                this.extract_files.RemovePath(this.map_IDtoUser[nId].ToString());
                this.map_IDtoUser.Remove(nId);
            }
        }

        private void TcpIpServer_NewConnection(int nId)
        {
            //this.MessageHandler_Message(nId, "New Connection");
            //string[] asRow = new string[4] { nId.ToString(),  };
            //this.users_listView.Items.Add(new System.Windows.Forms.ListViewItem(asRow));
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.tools_difference.Close();
            ShareMemorys.isPowerOff = true;
            if (ShareMemorys.isServerOn == true)
            {
                ShareMemorys.TcpIpServer.Stop();
            }
        }

        private void startServer_button_Click(object sender, EventArgs e)
        {
            if (ShareMemorys.isServerOn == false)
            {
                ShareMemorys.TcpIpServer.Start(int.Parse(this.connectPort_textBox.Text));
                ShareMemorys.isServerOn = true;
            }
        }

        private void stopServer_button_Click(object sender, EventArgs e)
        {
            int count = ShareMemorys.TcpIpServer.ConnectionCount();
            if (0 == count)
            {
                if (ShareMemorys.isServerOn == true)
                {
                    ShareMemorys.TcpIpServer.Stop();
                    ShareMemorys.isServerOn = false;
                }
            }
            else
            {
                MessageBox.Show(string.Format("Can't Stop Server, still have {0} client.", count), "Warning", MessageBoxButtons.OK);
            }
        }

        private void clearText_button_Click(object sender, EventArgs e)
        {
            this.logs_listBox.Items.Clear();
        }


        #region User Form
        ///////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// tab2: user form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetUsersDataToModule(string mac)
        {
            Assemblies.Ftp.UserData.Get().SetUserPassword(this.userName_textBox.Text, this.password_textBox.Text);
            Assemblies.Ftp.UserData.Get().SetUserStartingDirectory(this.userName_textBox.Text, this.folderPath_textBox.Text);
            Assemblies.Ftp.UserData.Get().SetAllowDownload(this.userName_textBox.Text, this.allowDownload_checkBox.Checked);
            Assemblies.Ftp.UserData.Get().SetAllowUpload(this.userName_textBox.Text, this.allowUpload_checkBox.Checked);
            Assemblies.Ftp.UserData.Get().SetAllowDelete(this.userName_textBox.Text, this.allowDelete_checkBox.Checked);
            Assemblies.Ftp.UserData.Get().SetAllowCreateFolder(this.userName_textBox.Text, this.allowCreateFolder_checkBox.Checked);
            Assemblies.Ftp.UserData.Get().SetModuleMAC(this.userName_textBox.Text, mac);
        }

        private void addUser_button_Click(object sender, EventArgs e)
        {
            // check UI data
            if (this.userName_textBox.Text.Length == 0)
            {
                MessageBox.Show("User name was blank", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (this.password_textBox.Text.Length == 0)
            {
                MessageBox.Show("Password was blank", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (this.folderPath_textBox.Text.Length == 0)
            {
                MessageBox.Show("Folder path was blank", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if ((this.allowDownload_checkBox.Checked || this.allowUpload_checkBox.Checked ||
                 this.allowDelete_checkBox.Checked || this.allowCreateFolder_checkBox.Checked) == false)
            {
                MessageBox.Show("No Permissions selected", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if ((this.mac0_textBox.Text.Length == 0) || (this.mac1_textBox.Text.Length == 0) || (this.mac2_textBox.Text.Length == 0) ||
                (this.mac3_textBox.Text.Length == 0) || (this.mac4_textBox.Text.Length == 0) || (this.mac5_textBox.Text.Length == 0))
            {
                MessageBox.Show("MAC Address has a blank", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            // already exist?
            if (Assemblies.Ftp.UserData.Get().HasUser(this.userName_textBox.Text))
            {
                MessageBox.Show("User already exists", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            // add a new user
            Assemblies.Ftp.UserData.Get().AddUser(this.userName_textBox.Text);
            string mac = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", this.mac0_textBox.Text, this.mac1_textBox.Text, this.mac2_textBox.Text,
                                                                  this.mac3_textBox.Text, this.mac4_textBox.Text, this.mac5_textBox.Text);
            this.SetUsersDataToModule(mac);

            string[] asRow = new string[8] { this.userName_textBox.Text, "*****", this.allowDownload_checkBox.Checked.ToString(),
                                             this.allowUpload_checkBox.Checked.ToString(), this.allowDelete_checkBox.Checked.ToString(), 
                                             this.allowCreateFolder_checkBox.Checked.ToString(), this.folderPath_textBox.Text, mac };
            this.users_listView.Items.Add(new System.Windows.Forms.ListViewItem(asRow));

            this.saveUserData();
        }

        private void setData_button_Click(object sender, EventArgs e)
        {
            // check UI data
            if (this.userName_textBox.Text.Length == 0)
            {
                MessageBox.Show("User name was blank", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (this.password_textBox.Text.Length == 0)
            {
                MessageBox.Show("Password was blank", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (this.folderPath_textBox.Text.Length == 0)
            {
                MessageBox.Show("Folder path was blank", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if ((this.allowDownload_checkBox.Checked || this.allowUpload_checkBox.Checked ||
                 this.allowDelete_checkBox.Checked || this.allowCreateFolder_checkBox.Checked) == false)
            {
                MessageBox.Show("No Permissions selected", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if ((this.mac0_textBox.Text.Length == 0) || (this.mac1_textBox.Text.Length == 0) || (this.mac2_textBox.Text.Length == 0) ||
                (this.mac3_textBox.Text.Length == 0) || (this.mac4_textBox.Text.Length == 0) || (this.mac5_textBox.Text.Length == 0))
            {
                MessageBox.Show("MAC Address has a blank", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            string mac = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", this.mac0_textBox.Text, this.mac1_textBox.Text, this.mac2_textBox.Text,
                                                                  this.mac3_textBox.Text, this.mac4_textBox.Text, this.mac5_textBox.Text);

            // already exist?
            if (Assemblies.Ftp.UserData.Get().HasUser(this.userName_textBox.Text))
            {
                // set a exist user
                this.SetUsersDataToModule(mac);

                foreach (int index in this.users_listView.SelectedIndices)
                {
                    this.users_listView.Items[index].SubItems[1].Text = "*****";
                    this.users_listView.Items[index].SubItems[2].Text = this.allowDownload_checkBox.Checked.ToString();
                    this.users_listView.Items[index].SubItems[3].Text = this.allowUpload_checkBox.Checked.ToString();
                    this.users_listView.Items[index].SubItems[4].Text = this.allowDelete_checkBox.Checked.ToString();
                    this.users_listView.Items[index].SubItems[5].Text = this.allowCreateFolder_checkBox.Checked.ToString();
                    this.users_listView.Items[index].SubItems[6].Text = this.folderPath_textBox.Text;
                    this.users_listView.Items[index].SubItems[7].Text = mac;
                }
            }
            else
            {
                // add a new user
                Assemblies.Ftp.UserData.Get().AddUser(this.userName_textBox.Text);
                this.SetUsersDataToModule(mac);
                
                string[] asRow = new string[8] { this.userName_textBox.Text, "*****", this.allowDownload_checkBox.Checked.ToString(),
                                             this.allowUpload_checkBox.Checked.ToString(), this.allowDelete_checkBox.Checked.ToString(), 
                                             this.allowCreateFolder_checkBox.Checked.ToString(), this.folderPath_textBox.Text, mac };
                this.users_listView.Items.Add(new System.Windows.Forms.ListViewItem(asRow));
            }
            this.saveUserData();
        }

        private void deleteUser_button_Click(object sender, EventArgs e)
        {

            if (this.users_listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a user in the list", "Warning");
            }
            else
            {
                if (DialogResult.OK == MessageBox.Show("Delete User: " + this.userName_textBox.Text, "Warning", MessageBoxButtons.OKCancel))
                {
                    foreach (int index in this.users_listView.SelectedIndices)
                    {
                        Assemblies.Ftp.UserData.Get().RemoveUser(this.users_listView.Items[index].SubItems[0].Text);
                        this.users_listView.Items[index].Remove();
                    }
                    this.saveUserData();
                }
            }
        }

        private void clearData_button_Click(object sender, EventArgs e)
        {
            this.userName_textBox.Text = this.password_textBox.Text = this.folderPath_textBox.Text = string.Empty;
            // this.allowDownload_checkBox.Checked = this.allowUpload_checkBox.Checked = this.allowDelete_checkBox.Checked = this.allowCreateFolder_checkBox.Checked = false;
            this.mac0_textBox.Text = this.mac1_textBox.Text = this.mac2_textBox.Text = this.mac3_textBox.Text = this.mac4_textBox.Text = this.mac5_textBox.Text = "00";
            
            this.users_listView.SelectedIndices.Clear();
        }

        private void openFolderDialog_button_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (DialogResult.OK == fbd.ShowDialog()) { this.folderPath_textBox.Text = fbd.SelectedPath; }
        }

        private void users_listView_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (int item in ((ListView)sender).SelectedIndices)
            {
                Console.WriteLine(item.ToString());
                this.userName_textBox.Text = ((ListView)sender).Items[item].SubItems[0].Text;
                this.password_textBox.Text = Assemblies.Ftp.UserData.Get().GetUserPassword(((ListView)sender).Items[item].SubItems[0].Text);
                this.allowDownload_checkBox.Checked = Convert.ToBoolean(((ListView)sender).Items[item].SubItems[2].Text);
                this.allowUpload_checkBox.Checked = Convert.ToBoolean(((ListView)sender).Items[item].SubItems[3].Text);
                this.allowDelete_checkBox.Checked = Convert.ToBoolean(((ListView)sender).Items[item].SubItems[4].Text);
                this.allowCreateFolder_checkBox.Checked = Convert.ToBoolean(((ListView)sender).Items[item].SubItems[5].Text);
                this.folderPath_textBox.Text = ((ListView)sender).Items[item].SubItems[6].Text;

                string[] macs = ((ListView)sender).Items[item].SubItems[7].Text.Split(':');
                this.mac0_textBox.Text = macs[0];
                this.mac1_textBox.Text = macs[1];
                this.mac2_textBox.Text = macs[2];
                this.mac3_textBox.Text = macs[3];
                this.mac4_textBox.Text = macs[4];
                this.mac5_textBox.Text = macs[5];
            }
        }

        private void saveUserData()
        {
            Assemblies.Ftp.UserData.Get().Save();
        }

        private void loadUserData()
        {
            Assemblies.Ftp.UserData.Get().Load();

            string user_name = string.Empty;
            for (int i = 0; i < Assemblies.Ftp.UserData.Get().UserCount; i++)
            {
                user_name = Assemblies.Ftp.UserData.Get().Users[i];
                string[] asRow = new string[] { user_name, 
                                                "*****",
                                                Assemblies.Ftp.UserData.Get().GetAllowDownload(user_name).ToString(),
                                                Assemblies.Ftp.UserData.Get().GetAllowUpload(user_name).ToString(),
                                                Assemblies.Ftp.UserData.Get().GetAllowDelete(user_name).ToString(),
                                                Assemblies.Ftp.UserData.Get().GetAllowCreateFolder(user_name).ToString(),
                                                Assemblies.Ftp.UserData.Get().GetUserStartingDirectory(user_name),
                                                Assemblies.Ftp.UserData.Get().GetModuleMAC(user_name)
                                              };
                this.users_listView.Items.Add(new ListViewItem(asRow));
            }
        }
        
        
        private void users_listView_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            e.Cancel = true;
            e.NewWidth = ((ListView)sender).Columns[e.ColumnIndex].Width;
        }

        private void autoActiveAtStartup_checkBox_CheckedChanged(object sender, EventArgs e)
        {
            ShareMemorys.ConfigurationItemsData.AutoActiveAtStartup = ((CheckBox)sender).Checked;
            this.SaveConfiguration();
        }

        private string GetDefaultConfigurationPath()
        {
            return Path.Combine(System.Windows.Forms.Application.StartupPath, "Configuration.dat");
        }

        private bool SaveConfiguration() { return SaveConfiguration(GetDefaultConfigurationPath()); }
        private bool SaveConfiguration(string sFileName)
        {
            try
            {
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                System.IO.FileStream fileStream = new System.IO.FileStream(sFileName, System.IO.FileMode.Create);
                formatter.Serialize(fileStream, ShareMemorys.ConfigurationItemsData);
                fileStream.Close();
            }
            catch (System.IO.IOException e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            return true;
        }

        private bool LoadConfiguration() { return LoadConfiguration(GetDefaultConfigurationPath()); }
        public bool LoadConfiguration(string sFileName)
        {
            if (!System.IO.File.Exists(sFileName))
            {
                return true;
            }

            try
            {
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                System.IO.FileStream fileStream = new System.IO.FileStream(sFileName, System.IO.FileMode.Open);
                ShareMemorys.ConfigurationItemsData = formatter.Deserialize(fileStream) as ConfigurationItems;
                fileStream.Close();
            }
            catch (System.IO.IOException)
            {
                return false;
            }

            return true;
        }

        private bool isLoadingConfiguration;
        private void configuration_textBox_TextChanged(object sender, EventArgs e)
        {
            if (false == isLoadingConfiguration)
            {
                ShareMemorys.ConfigurationItemsData.ConnectPort = int.Parse(this.connectPort_textBox.Text);
                ShareMemorys.ConfigurationItemsData.MaxConnections = int.Parse(this.maxConnections_textBox.Text);
                ShareMemorys.ConfigurationItemsData.ConnectionTimeout = int.Parse(this.connectionTimeout_textBox.Text);

                this.SaveConfiguration();
            }
        }



        #endregion
        
        #region Online Users Form
        ///////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// tab2: Online Users form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private int old_login_count = -1, new_login_count = 0;
        private void updateOnlineUser_Timer_Tick(object sender, EventArgs e)
        {
            this.new_login_count = Assemblies.Ftp.LoginData.Get().LoginCount;
            if (new_login_count > old_login_count)
            {
                foreach (var user in Assemblies.Ftp.LoginData.Get().Users)
                {
                    if (false == Assemblies.Ftp.LoginData.Get().GetOnList(user))
                    {
                        // add a new user
                        string[] asRow = new string[6] {"",
                                                        Assemblies.Ftp.LoginData.Get().GetLoginNID(user).ToString(),
                                                        user,
                                                        Assemblies.Ftp.LoginData.Get().GetLoginIpAddr(user).ToString(),
                                                        Assemblies.Ftp.LoginData.Get().GetLoginMAC(user).ToString(),
                                                        Assemblies.Ftp.LoginData.Get().GetLoginTime(user).ToString()};
                        this.onlineUsers_listView.Items.Add(new System.Windows.Forms.ListViewItem(asRow));
                        Assemblies.Ftp.LoginData.Get().SetOnList(user, true);
                    }
                }
                this.old_login_count = this.new_login_count;
            }
            else if (new_login_count < old_login_count)
            {
                foreach (ListViewItem item in this.onlineUsers_listView.Items)
                {
                    bool isUserExist = false;
                    foreach (var user in Assemblies.Ftp.LoginData.Get().Users)
                    {
                        if (user == item.SubItems["User Name"].ToString())
                        {
                            isUserExist = true;
                        }
                    }
                    if (false == isUserExist)
                    {
                        item.Remove();
                    }
                }

                this.old_login_count = this.new_login_count;
            }
        }

        private void onlineUsers_listView_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            if (e.ColumnIndex == 0)
            {
                e.DrawBackground();
                bool value = false;
                try
                {
                    value = Convert.ToBoolean(e.Header.Tag);
                }
                catch (Exception)
                {
                }
                CheckBoxRenderer.DrawCheckBox(e.Graphics, new Point(e.Bounds.Left + 4, e.Bounds.Top + 4),
                    value ? System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal :
                    System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal);
            }
            else
            {
                e.DrawDefault = true;
            }
        }

        private void onlineUsers_listView_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void onlineUsers_listView_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void onlineUsers_listView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == 0)
            {
                bool value = false;
                try
                {
                    value = Convert.ToBoolean(this.onlineUsers_listView.Columns[e.Column].Tag);
                }
                catch (Exception)
                {
                }
                this.onlineUsers_listView.Columns[e.Column].Tag = !value;
                foreach (ListViewItem item in this.onlineUsers_listView.Items)
                    item.Checked = !value;

                this.onlineUsers_listView.Invalidate();
            }
        }

        private string safe_file_name = string.Empty;
        private void openFile_button_Click(object sender, EventArgs e)
        {
            this.localFile_textBox.Text = string.Empty;

            OpenFileDialog file_dialog = new OpenFileDialog();
            file_dialog.RestoreDirectory = false;

            if (file_dialog.ShowDialog() == DialogResult.OK)
            {
                this.safe_file_name = file_dialog.SafeFileName;
                this.localFile_textBox.Text = file_dialog.FileName;
            }
        }

        private void send_button_Click(object sender, EventArgs e)
        {
            if ("Update" == this.sendMode_comboBox.SelectedItem.ToString())
            {
                string[] file_name = this.localFile_textBox.Text.Split('\\');
                if ("__dgm_update__.zip" != file_name[file_name.Count() - 1])
                {
                    MessageBox.Show("Incorrect file!!", "Warning", MessageBoxButtons.OK);
                    return;
                }
            }

            ListView.CheckedListViewItemCollection checkedItems = this.onlineUsers_listView.CheckedItems;

            foreach (ListViewItem item in checkedItems)
            {
                // MessageBox.Show(item.SubItems[2].Text);

                // create an FTP client
                FtpClient client = new FtpClient();
                client.Host = item.SubItems[3].Text;

                // if you don't specify login credentials, we use the "anonymous" user account
                client.Credentials = new NetworkCredential("pi", "raspberry");

                // begin connecting to the server
                client.Connect();

                if (true == client.IsConnected)
                {
                    // Change Encoding
                    client.Encoding = Encoding.UTF8;

                    // Upload selected file
                    if ("Update" == this.sendMode_comboBox.SelectedItem.ToString())
                    {
                        client.UploadFile(this.localFile_textBox.Text, string.Format(@"/home/pi/Public/Program/Update/{0}", this.safe_file_name));
                    }
                    else
                    {
                        client.UploadFile(this.localFile_textBox.Text, string.Format(@"/home/pi/{0}", this.safe_file_name));
                    }

                    // disconnect! good bye!
                    client.Disconnect();
                }
            }

        }



        #endregion

        #region CNC Diff Form
        ///////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// tab2: user form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// 

        private void InitialDataGridView()
        {
            this.toolsDiff_dataGridView.Rows.Add(9);
            for (int i = 0; i < 10; i++)
			{
                this.toolsDiff_dataGridView[(int)TOOLS_DIFF_COLUMN_HEADER.No_00, i].Value = (i + 1).ToString().PadLeft(2, '0');
                this.toolsDiff_dataGridView[(int)TOOLS_DIFF_COLUMN_HEADER.No_10, i].Value = (i + 11).ToString().PadLeft(2, '0');
                this.toolsDiff_dataGridView[(int)TOOLS_DIFF_COLUMN_HEADER.No_20, i].Value = (i + 21).ToString().PadLeft(2, '0');
			}

            this.tools_difference = new ToolsDifference();
            this.tools_difference.LogMessage += new ToolsDifference.UpdateHandler(tools_difference_LogMessage);
            this.tools_difference.SendLogMessage("Copyright of JGP Automation Mechanics!");
            this.tools_difference.DataUpdated += new ToolsDifference.UpdateHandler(tools_difference_DataUpdated);
        }

        private void tools_difference_DataUpdated(string msg)
        {
            List<int> current_list = this.tools_difference.current_tool_list,
                      same_list = this.tools_difference.same_tool_list,
                      other_list = this.tools_difference.other_tool_list;

            for (int i = 0; i < 10; i++)
            {
                this.toolsDiff_dataGridView[(int)TOOLS_DIFF_COLUMN_HEADER.CurT_00, i].Style.BackColor = Color.FromArgb(255, 192, 255);
                this.toolsDiff_dataGridView[(int)TOOLS_DIFF_COLUMN_HEADER.CurT_10, i].Style.BackColor = Color.FromArgb(255, 192, 255);
                this.toolsDiff_dataGridView[(int)TOOLS_DIFF_COLUMN_HEADER.CurT_20, i].Style.BackColor = Color.FromArgb(255, 192, 255);

                this.toolsDiff_dataGridView[(int)TOOLS_DIFF_COLUMN_HEADER.CurT_00, i].Value = current_list[i + 0].ToString();
                this.toolsDiff_dataGridView[(int)TOOLS_DIFF_COLUMN_HEADER.CurT_10, i].Value = current_list[i + 10].ToString();
                this.toolsDiff_dataGridView[(int)TOOLS_DIFF_COLUMN_HEADER.CurT_20, i].Value = current_list[i + 20].ToString();

                if (0 != same_list[i + 0])
                    this.toolsDiff_dataGridView[(int)TOOLS_DIFF_COLUMN_HEADER.CurT_00, i].Style.BackColor = Color.Yellow;
                if (0 != same_list[i + 10])
                    this.toolsDiff_dataGridView[(int)TOOLS_DIFF_COLUMN_HEADER.CurT_10, i].Style.BackColor = Color.Yellow;
                if (0 != same_list[i + 20])
                    this.toolsDiff_dataGridView[(int)TOOLS_DIFF_COLUMN_HEADER.CurT_20, i].Style.BackColor = Color.Yellow;
            }

            this.spindleNo_textBox.InvokeIfRequired(() =>
            {
                this.spindleNo_textBox.Text = this.tools_difference.spindle_tool.ToString();
            });

            this.toolsNeedChanged_textBox.InvokeIfRequired(() =>
            {
                this.toolsNeedChanged_textBox.Clear();
                string tools = string.Empty;
                foreach (int item in other_list)
                {
                    tools = tools + item.ToString() + ", ";
                }
                this.toolsNeedChanged_textBox.AppendText(tools);
            });

            int tool_zero_count = current_list.Count(x => x == 0);
            this.toolZeroCount_textBox.InvokeIfRequired(() =>
            {
                this.toolZeroCount_textBox.Text = tool_zero_count.ToString();
            });

            this.toolZeroCount_textBox.InvokeIfRequired(() =>
            {
                if (0 < (other_list.Count - tool_zero_count))
                {
                    this.toolRemoveCount_textBox.Text = (other_list.Count - tool_zero_count).ToString();
                }
                else
                {
                    this.toolRemoveCount_textBox.Text = "0";
                }
            });

            //this.uploadNewTools_button.InvokeIfRequired(() =>
            //{
            //    this.uploadNewTools_button.Enabled = true;
            //});
            //this.tools_difference.SendLogMessage("Data is ready, Please Upload it!");
            this.process_comboBox.SelectedIndexChanged += new System.EventHandler(this.process_comboBox_SelectedIndexChanged);
        }

        private void tools_difference_LogMessage(string msg)
        {
            var writeMessageAction = new Action(
                    () =>
                    {
                        this.toolsDiffLogs_listBox.BeginUpdate();

                        int nItem = this.toolsDiffLogs_listBox.Items.Add(string.Format("<{0}>", System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")));
                        nItem = this.toolsDiffLogs_listBox.Items.Add(string.Format(" -- {0}", msg));

                        if (this.toolsDiffLogs_listBox.Items.Count > 5000)
                        {
                            this.toolsDiffLogs_listBox.Items.RemoveAt(0);
                        }

                        if (this.toolsDiffLogs_listBox.SelectedIndex < 0)
                        {
                            this.toolsDiffLogs_listBox.TopIndex = nItem;
                        }
                        else if (this.toolsDiffLogs_listBox.SelectedIndex == nItem - 1)
                        {
                            this.toolsDiffLogs_listBox.SelectedIndex = nItem;
                        }

                        this.toolsDiffLogs_listBox.EndUpdate();
                    });

            if (this.toolsDiffLogs_listBox.InvokeRequired)
                this.toolsDiffLogs_listBox.Invoke(writeMessageAction);
            else
                writeMessageAction();
        }

        private void targetDir_button_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (DialogResult.OK == fbd.ShowDialog()) { this.targetDir_textBox.Text = fbd.SelectedPath; }
        }                                                                         

        private void compare_button_Click(object sender, EventArgs e)
        {
            if ("No IP Data!" != this.remoteIpAddr_label.Text)
            {
                if (ToolsDifference.TRANSPORT.NONE == this.tools_difference.transport_state)
                {
                    this.process_comboBox.SelectedIndexChanged -= new System.EventHandler(this.process_comboBox_SelectedIndexChanged);

                    this.uploadNewTools_button.Enabled = false;

                    List<string> process_list = this.tools_difference.GetWorkOrderProcess(this.targetDir_textBox.Text, this.workOrder_textBox.Text);
                    this.process_comboBox.Items.Add("All");
                    this.process_comboBox.Items.AddRange(process_list.ToArray());
                    this.process_comboBox.SelectedIndex = 0;

                    this.tools_difference.remote_ip = this.remoteIpAddr_label.Text;
                    //this.tools_difference.remote_ip = string.Format("{0}.{1}.{2}.{3}", this.toolIP_0_textBox.Text, this.toolIP_1_textBox.Text, this.toolIP_2_textBox.Text, this.toolIP_3_textBox.Text);
                    this.tools_difference.transport_state = ToolsDifference.TRANSPORT.REQUIRE;
                    this.tools_difference.process_state = ToolsDifference.PROCESS.ALL;
                }
            }
        }

        private void process_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.uploadNewTools_button.Enabled = false;
            for (int i = 0; i < 10; i++)
            {
                this.toolsDiff_dataGridView[(int)TOOLS_DIFF_COLUMN_HEADER.CurT_00, i].Value = string.Empty;
                this.toolsDiff_dataGridView[(int)TOOLS_DIFF_COLUMN_HEADER.CurT_10, i].Value = string.Empty;
                this.toolsDiff_dataGridView[(int)TOOLS_DIFF_COLUMN_HEADER.CurT_20, i].Value = string.Empty;
            }

            if (0 == ((ComboBox)sender).SelectedIndex)
            {
                this.tools_difference.transport_state = ToolsDifference.TRANSPORT.REQUIRE;
                this.tools_difference.process_state = ToolsDifference.PROCESS.ALL;
            }
            else
            {
                this.tools_difference.transport_state = ToolsDifference.TRANSPORT.REQUIRE;
                this.tools_difference.selected_process = ((ComboBox)sender).SelectedItem.ToString();
                this.tools_difference.process_state = ToolsDifference.PROCESS.ONE;
            }
        }
        
        private void uploadNewTools_button_Click(object sender, EventArgs e)
        {
            if (ToolsDifference.TRANSPORT.WAIT_SEND_DATA == this.tools_difference.transport_state)
            {
                this.tools_difference.transport_state = ToolsDifference.TRANSPORT.SEND_DATA;
            }
        }

        private void form_ToolsDiffIpSettings_IpUpdated(string msg)
        {
            if ("Update" == msg)
            {
                this.remoteIpName_comboBox.Items.Clear();

                foreach (var item in this.form_ToolsDiffIpSettings.ip_name_list)
                {
                    this.remoteIpName_comboBox.Items.Add(item);
                }
                this.remoteIpAddr_label.Text = this.form_ToolsDiffIpSettings.GetIpAddress(this.remoteIpName_comboBox.Text);
            }
        }

        private void ipSettings_button_Click(object sender, EventArgs e)
        {
            this.form_ToolsDiffIpSettings.ShowDialog();
        }

        private void remoteIpName_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.remoteIpAddr_label.Text = this.form_ToolsDiffIpSettings.GetIpAddress(this.remoteIpName_comboBox.Text);
        }

        #endregion

    }

    //擴充方法
    public static class Extension
    {
        //非同步委派更新UI
        public static void InvokeIfRequired(
            this Control control, MethodInvoker action)
        {
            if (control.InvokeRequired)//在非當前執行緒內 使用委派
            {
                control.Invoke(action);
            }
            else
            {
                action();
            }
        }
    }
}
