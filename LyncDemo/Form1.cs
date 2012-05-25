using System.Drawing;
using System.IO;
using System;
using System.Windows.Forms;
using Microsoft.Lync.Model;
using System.Collections.Generic;

namespace LyncDemo
{
    public partial class Form1 : Form
    {
        public System.Data.DataSet DS;
        
        string TableName = "LyncUsers", DataSetName= "Lync";
        int offset = 50;
        string FileName = Path.Combine(Application.StartupPath,"Lync.xml");
        List<Contact> contacts;
        LyncClient lyncClient;

        public Form1()
        {
            InitializeComponent();
            if (lyncClient == null)
            {
                lyncClient = LyncClient.GetClient();
                lyncClient.StateChanged += new EventHandler<ClientStateChangedEventArgs>(lyncClient_StateChanged);
            }
            contacts = new List<Contact>();
            InitDataSet();
        }

        void lyncClient_StateChanged(object sender, ClientStateChangedEventArgs e)
        {
            if (e.NewState != ClientState.SignedIn)
            {
                MessageBox.Show("Lync state changed: " + e.NewState.ToString() + "\r\n Waiting for signing in again");
                System.Threading.Thread.Sleep(10000);
            }
        }

        public void InitDataSet()
        {
            DS = new System.Data.DataSet(DataSetName);
            if (File.Exists(FileName))
            {
                DS.ReadXml(FileName);
                foreach (System.Data.DataRow row in DS.Tables[TableName].Rows)
                {
                    AddContact((string)row.ItemArray[4]);
                }
            }
            else
            {                
                DS.Tables.Add(TableName);
                DS.Tables[TableName].Columns.Add("User", typeof(string));
                DS.Tables[TableName].Columns.Add("Status", typeof(string));
                DS.Tables[TableName].Columns.Add("Change Time", typeof(string));
                DS.Tables[TableName].Columns.Add("State Time", typeof(string));
                DS.Tables[TableName].Columns.Add("Email", typeof(string));
                DS.Tables[TableName].PrimaryKey = new System.Data.DataColumn[] { DS.Tables[TableName].Columns[DS.Tables[TableName].Columns.IndexOf("User")] };
            }
            dataGridView.DataSource = DS;
            dataGridView.DataMember = TableName;

            timer1.Start();
            RefreshStatus();
        }
        public void AddContact(string UserName)
        {
            if (lyncClient == null)
            {
                lyncClient = LyncClient.GetClient();
            }
            if (lyncClient.State == ClientState.SignedIn)
            {
                try
                {
                    Contact contact = lyncClient.ContactManager.GetContactByUri("sip:" + UserName);
                    contacts.Add(contact);
                    contact.ContactInformationChanged += new System.EventHandler<ContactInformationChangedEventArgs>(contact_ContactInformationChanged);
                    Microsoft.Lync.Model.ContactAvailability availability = (Microsoft.Lync.Model.ContactAvailability)contact.GetContactInformation(ContactInformationType.Availability);

                    string Name = (string)contact.GetContactInformation(ContactInformationType.DisplayName);
                    for (int rowIndex = 0; rowIndex < dataGridView.Rows.Count; rowIndex++)
                    {
                        if ((string)dataGridView.Rows[rowIndex].Cells["User"].Value == Name)
                        {
                            dataGridView.Rows[rowIndex].Cells["Status"].Value = availability.ToString();
                            break;
                        }
                    }
                }
                catch { }
            }
        }

        public void RefreshStatus()
        {
            try
            {
                for (int rowIndex = 0; rowIndex < dataGridView.Rows.Count; rowIndex++)
                {
                    string email = (string)dataGridView.Rows[rowIndex].Cells["Email"].Value;
                    Contact contact = lyncClient.ContactManager.GetContactByUri("sip:" + email);
                    Microsoft.Lync.Model.ContactAvailability availability = (Microsoft.Lync.Model.ContactAvailability)contact.GetContactInformation(ContactInformationType.Availability);
                    dataGridView.Rows[rowIndex].Cells["Status"].Value = availability.ToString();
                    Color rowColor = GetColor(availability.ToString());

                    DataGridViewCellCollection cells = dataGridView.Rows[rowIndex].Cells;
                    foreach (DataGridViewCell cell in cells)
                    {
                        cell.Style.BackColor = rowColor;
                    }
                }
            }
            catch { } //Bad Programming
        }

        public void GetInfo(string UserName)
        {
            if (lyncClient == null)
            {
                lyncClient = LyncClient.GetClient();
            }
            if (lyncClient.State == ClientState.SignedIn)
            {
                try
                {
                    //Contact contact = lyncClient.ContactManager.GetContactByUri("sip:sourav.sengupta@accenture.com");
                    Contact contact = lyncClient.ContactManager.GetContactByUri("sip:" + UserName);
                    contacts.Add(contact);
                    contact.ContactInformationChanged += new System.EventHandler<ContactInformationChangedEventArgs>(contact_ContactInformationChanged);
                    Microsoft.Lync.Model.ContactAvailability availability = (Microsoft.Lync.Model.ContactAvailability)contact.GetContactInformation(ContactInformationType.Availability);
                    string Name = (string)contact.GetContactInformation(ContactInformationType.DisplayName);
                    System.Data.DataRow row = DS.Tables[TableName].Rows.Add(new string[] { Name, availability.ToString(), "0", "0", UserName });
                }
                catch
                { }
            }
        }

        void contact_ContactInformationChanged(object sender, ContactInformationChangedEventArgs e)
        {
            try
            {
                int i = e.ChangedContactInformation.IndexOf(ContactInformationType.Availability);
                if (i >= 0)
                {
                    Contact contact = (Contact)sender;
                    Microsoft.Lync.Model.ContactAvailability availability = (Microsoft.Lync.Model.ContactAvailability)contact.GetContactInformation(ContactInformationType.Availability);
                    string Name = (string)contact.GetContactInformation(ContactInformationType.DisplayName);
                    for (int rowIndex = 0; rowIndex < dataGridView.Rows.Count; rowIndex++)
                    {
                        if ((string)dataGridView.Rows[rowIndex].Cells["User"].Value == Name)
                        {
                            dataGridView.Rows[rowIndex].Cells["Status"].Value = availability.ToString();
                            dataGridView.Rows[rowIndex].Cells["Change Time"].Value = DateTime.Now.ToString("hh:mm:ss tt dd-MMM");
                            dataGridView.Rows[rowIndex].Cells["State Time"].Value = "0";
                            break;
                        }
                    }
                }
            }
            catch (NotSignedInException ex)
            {
                if (lyncClient.State != ClientState.SignedIn)
                {
                    MessageBox.Show("Waiting for Lync to sign in\r\n"+ex.Message);
                    System.Threading.Thread.Sleep(10000);
                }
            }
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            if (textBox1.Text.Length == 0)
            {
                errorProvider1.SetError(textBox1, "Text box must not be empty");
                errorProvider1.SetIconAlignment(textBox1, ErrorIconAlignment.MiddleLeft);
                errorProvider1.SetIconPadding(textBox1, -20);
                return;
            }
            else
            {
                errorProvider1.Clear();
            }
            GetInfo(textBox1.Text);
            textBox1.Clear();
        }

        private void Form1_Resize(object sender, System.EventArgs e)
        {
            dataGridView.Height = this.ClientRectangle.Height - offset;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DS.WriteXml(FileName,System.Data.XmlWriteMode.WriteSchema);
        }

        private void dataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            DataGridViewCellCollection cells = dataGridView.Rows[e.RowIndex].Cells;
            Color cellColor;
            try
            {
                cellColor = GetColor((string)cells["Status"].Value);
            }
            catch
            {
                cellColor = Color.White;
            }
            foreach (DataGridViewCell cell in cells)
            {
                cell.Style.BackColor = cellColor;
            }
        }

        public Color GetColor(string availability)
        {
            if (availability == "Away")
            {
                return Color.Yellow;
            }
            if (availability == "DoNotDisturb")
            {
                return Color.Red;
            }
            if (availability == "Busy")
            {
                return Color.Orange;
            }
            if (availability == "FreeIdle")
            {
                return Color.GreenYellow;
            }
            if (availability == "Free")
            {
                return Color.Green;
            }
            if (availability == "Offline")
            {
                return Color.LightGray;
            }
            return Color.White;
        }

        private void dataGridView_RowValidated(object sender, DataGridViewCellEventArgs e)
        {
            DataGridViewCellCollection cells = dataGridView.Rows[e.RowIndex].Cells;
            Color cellColor = GetColor((string)cells["Status"].Value);
            foreach (DataGridViewCell cell in cells)
            {
                cell.Style.BackColor = cellColor;
            }
        }

        private void textBox1_TextChanged(object sender, System.EventArgs e)
        {
            errorProvider1.Clear();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            for (int rowIndex = 0; rowIndex < dataGridView.Rows.Count; rowIndex++)
            {
                int time;
                try
                {
                    time = int.Parse((string)dataGridView.Rows[rowIndex].Cells["State Time"].Value);
                }
                catch
                {
                    time = 0;
                }
                time++;
                dataGridView.Rows[rowIndex].Cells["State Time"].Value = time.ToString();
            }
        }

        private void ReloadStatus_Click(object sender, EventArgs e)
        {
            RefreshStatus();
        }
    }
}
