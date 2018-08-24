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
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;

namespace DGM_TcpIpServer
{
    public partial class Form_ToolsDiffIpSettings : Form
    {
        #region Member Variables
        private Hashtable mapIpToData = null;
        public List<string> ip_name_list = null;
        #endregion

        #region Events
        public delegate void UpdateHandler(string msg);
        public event UpdateHandler IpUpdated;
        #endregion

        public Form_ToolsDiffIpSettings()
        {
            InitializeComponent();
            this.mapIpToData = new Hashtable();

            this.LoadData();
            this.ip_name_list = new List<string>();
            this.update_ui();
        }

        public void CallIpUpdated(string msg)
        {
            if (IpUpdated != null)
            {
                IpUpdated(msg);
            }
        }

        private void update_ui()
        {
            this.ip_name_list.Clear();
            foreach (string item in this.mapIpToData.Keys) this.ip_name_list.Add(item);
            this.ip_name_list.Sort();
            this.ipName_comboBox.Items.Clear();
            foreach (var item in this.ip_name_list)
            {
                this.ipName_comboBox.Items.Add(item);
            }
            this.CallIpUpdated("Update");
        }

        private void add_button_Click(object sender, EventArgs e)
        {
            string sUser = this.ipName_comboBox.Text;
            if (false == this.HasUser(sUser))
            {
                this.AddUser(sUser);

                IpDataItem item = GetUserItem(sUser);
                if (item != null)
                {
                    item.ip_name = sUser;
                    item.ip_addr = string.Format("{0}.{1}.{2}.{3}", this.ip_0_textBox.Text, this.ip_1_textBox.Text, this.ip_2_textBox.Text, this.ip_3_textBox.Text);

                    this.update_ui();
                    this.SaveData();
                }
            }
        }

        private void remove_button_Click(object sender, EventArgs e)
        {
            string sUser = this.ipName_comboBox.Text;
            if (true == this.HasUser(sUser))
            {
                this.RemoveUser(sUser);

                this.update_ui();
                this.SaveData();
            }
        }

        private void Form_ToolsDiffIpSettings_Load(object sender, EventArgs e)
        {
            if (0 < this.ipName_comboBox.Items.Count)
            {
                this.ipName_comboBox.SelectedIndex = 0;
            }
        }

        private void ipName_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string sUser = this.ipName_comboBox.Text;
            IpDataItem item = GetUserItem(sUser);
            if (item != null)
            {
                string[] IPs = item.ip_addr.Split('.');
                this.ip_0_textBox.Text = IPs[0];
                this.ip_1_textBox.Text = IPs[1];
                this.ip_2_textBox.Text = IPs[2];
                this.ip_3_textBox.Text = IPs[3];
            }
        }

        #region Methods
        private IpDataItem GetUserItem(string sUser)
        {
            return mapIpToData[sUser] as IpDataItem;
        }
        public void AddUser(string sUser)
        {
            mapIpToData.Add(sUser, new IpDataItem());
        }
        public void RemoveUser(string sUser)
        {
            mapIpToData.Remove(sUser);
        }
        public bool HasUser(string sUser)
        {
            IpDataItem item = GetUserItem(sUser);
            return item != null;
        }
        public string GetIpAddress(string sUser)
        {
            IpDataItem item = GetUserItem(sUser);

            if (item != null)
            {
                return item.ip_addr;
            }
            else
            {
                return "No IP Data!";
            }
        }

        public bool SaveData(string sFileName)
        {
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream fileStream = new FileStream(sFileName, System.IO.FileMode.Create);
                formatter.Serialize(fileStream, this.mapIpToData);
                fileStream.Close();
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            return true;
        }
        public bool LoadData(string sFileName)
        {
            if (!File.Exists(sFileName)) return true;

            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream fileStream = new FileStream(sFileName, FileMode.Open);
                this.mapIpToData = formatter.Deserialize(fileStream) as Hashtable;
                fileStream.Close();
            }
            catch (IOException)
            {
                return false;
            }

            return true;
        }
        private string GetDefaultPath()
        {
            return Path.Combine(Application.StartupPath, "ToolIp.dat");
        }
        public bool SaveData()
        {
            return SaveData(GetDefaultPath());
        }
        public bool LoadData()
        {
            return LoadData(GetDefaultPath());
        }
        #endregion


    }

    [Serializable]
    class IpDataItem
    {
        public string ip_name = string.Empty;
        public string ip_addr = string.Empty;

        public IpDataItem() { }
    }
}
