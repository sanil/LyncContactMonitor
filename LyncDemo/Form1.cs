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
        string TableName = "LyncUsers";
        int offset = 50;
        string FileName = Path.Combine(Application.StartupPath,"Lync.xml");
        string LogFile = Path.Combine(Application.StartupPath, "LogFile.csv");
        List<Contact> contacts;
        LyncClient lyncClient;

        public Form1()
        {
            InitializeComponent();
            if (lyncClient == null)
            {
                lyncClient = LyncClient.GetClient();
                if (lyncClient != null)
                {
                    toolStripStatusLabel.Text = "State: " + lyncClient.State.ToString();
                }
                lyncClient.StateChanged += new EventHandler<ClientStateChangedEventArgs>(lyncClient_StateChanged);
            }
            contacts = new List<Contact>();
            InitDataSet();
        }

        void lyncClient_StateChanged(object sender, ClientStateChangedEventArgs e)
        {
            if (e.NewState != ClientState.SignedIn)
            {
                toolStripStatusLabel.Text = "State: " + e.NewState.ToString();
                System.Threading.Thread.Sleep(10000);
            }
        }

        public void InitDataSet()
        {
            if (File.Exists(FileName))
            {
                Lync.ReadXml(FileName);
                foreach (System.Data.DataRow row in Lync.Tables[TableName].Rows)
                {
                    AddContact((string)row.ItemArray[4]);
                }
            }
            timer1.Start();
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
                    Contact contact = lyncClient.ContactManager.GetContactByUri("sip:" + UserName);
                    contacts.Add(contact);
                    contact.ContactInformationChanged += new System.EventHandler<ContactInformationChangedEventArgs>(contact_ContactInformationChanged);
                    Microsoft.Lync.Model.ContactAvailability availability = (Microsoft.Lync.Model.ContactAvailability)contact.GetContactInformation(ContactInformationType.Availability);
                    string Name = (string)contact.GetContactInformation(ContactInformationType.DisplayName);
                    Lync.Tables[TableName].Rows.Add(new string[] { Name, availability.ToString(), "0", "0", UserName });
                }
                catch
                { }
                RefreshStatus();
            }
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
                        if ((string)dataGridView.Rows[rowIndex].Cells["UserName"].Value == Name)
                        {
                            dataGridView.Rows[rowIndex].Cells["Status"].Value = availability.ToString();
                            dataGridView.Rows[rowIndex].Cells["StatusImage"].Value = GetImage(availability.ToString());
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
                    //Color rowColor = GetColor(availability.ToString());

                    DataGridViewCellCollection cells = dataGridView.Rows[rowIndex].Cells;
                    cells["StatusImage"].Value = GetImage(availability.ToString());
                }
            }
            catch { } //Bad Programming
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
                        if ((string)dataGridView.Rows[rowIndex].Cells["UserName"].Value == Name)
                        {
                            string StateTime = dataGridView.Rows[rowIndex].Cells["StateTime"].Value.ToString();
                            string now = DateTime.Now.ToString("hh:mm:ss tt dd-MMM");
                            string OldState = dataGridView.Rows[rowIndex].Cells["Status"].Value.ToString();
                            string NewState = availability.ToString();

                            dataGridView.Rows[rowIndex].Cells["Status"].Value = availability.ToString();
                            dataGridView.Rows[rowIndex].Cells["ChangeTime"].Value = now;
                            dataGridView.Rows[rowIndex].Cells["StateTime"].Value = "0";
                            dataGridView.Rows[rowIndex].Cells["StatusImage"].Value = GetImage(availability.ToString());
                            LogChange(contact,e,StateTime,now, OldState, NewState);
                            break;
                        }
                    }
                }
            }
            catch (NotSignedInException ex)
            {
                if (lyncClient.State != ClientState.SignedIn)
                {
                    toolStripStatusLabel.Text = "Waiting for Lync to sign in... " + ex.Message;
                    System.Threading.Thread.Sleep(10000);
                }
            }
        }

        private void LogChange(Contact contact, ContactInformationChangedEventArgs e, string StateTime, string TimeOfEvent, string oldState, string newState)
        {
            string Log = contact.GetContactInformation(ContactInformationType.PrimaryEmailAddress) + "," + TimeOfEvent  + ","  + StateTime +","+ oldState + "," + newState+"\r\n";
            File.AppendAllText(LogFile, Log);
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            if (textBox1.Text.Length == 0)
            {
                errorProvider1.SetError(textBox1, "Text box must not be empty");
                toolStripStatusLabel.Text = "Text box must not be empty";
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
            Lync.WriteXml(FileName,System.Data.XmlWriteMode.WriteSchema);
        }

        private void dataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView.Columns[e.ColumnIndex].Name != "StatusImage")
            {
                if (e.RowIndex >= 0)
                {
                    DataGridViewCellCollection cells = dataGridView.Rows[e.RowIndex].Cells;
                    string availability;
                    try
                    {
                        availability = (string)cells["Status"].Value;
                    }
                    catch
                    {
                        availability = "None";
                    }
                    //foreach (DataGridViewCell cell in cells)
                    //{
                    //    cell.Style.BackColor = cellColor;
                    //}
                    dataGridView.Rows[e.RowIndex].Cells["StatusImage"].Value = GetImage(availability.ToString());
                }
            }
        }

        public Image GetImage(string availability)
        {
            Image img = (Image)new Bitmap(32, 32);
            Color col = GetColor(availability);
            using (Graphics g = Graphics.FromImage(img))
            {
                System.Drawing.Drawing2D.LinearGradientBrush LGB = new System.Drawing.Drawing2D.LinearGradientBrush(new RectangleF(0.0f, 8.0f, 16.0f, 32.0f),Color.White, col, System.Drawing.Drawing2D.LinearGradientMode.ForwardDiagonal);
                Brush c = (Brush)LGB;
                using (Brush b = new SolidBrush(GetColor(availability)))
                {
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.GammaCorrected;
                    g.DrawEllipse(new Pen(col, 0.1f), new Rectangle(8, 8, 16, 16));
                    g.FillEllipse(c, new Rectangle(8,8, 16, 16));
                }
                LGB.Dispose();
            }
            return img;
        }

        public Color GetColor(string availability)
        {
            if (availability == "Away") // todo : convert this into switch statements
            {
                return Color.Gold;
            }
            if (availability == "DoNotDisturb")
            {
                return Color.Red;
            }
            if (availability == "Busy")
            {
                return Color.Tomato;
            }
            if (availability == "FreeIdle")
            {
                return Color.GreenYellow;
            }
            if (availability == "Free")
            {
                return Color.LimeGreen;
            }
            if (availability == "Offline")
            {
                return Color.Silver;
            }
            return Color.White;
        }

        private void dataGridView_RowValidated(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView.Columns[e.ColumnIndex].Name != "StatusImage")
            {
                DataGridViewCellCollection cells = dataGridView.Rows[e.RowIndex].Cells;
                dataGridView.Rows[e.RowIndex].Cells["StatusImage"].Value = GetImage((string)cells["Status"].Value.ToString());
            }
        }

        private void textBox1_TextChanged(object sender, System.EventArgs e)
        {
            errorProvider1.Clear();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            RefreshStatus();
            for (int rowIndex = 0; rowIndex < dataGridView.Rows.Count; rowIndex++)
            {
                int time;
                try
                {
                    time = int.Parse((string)dataGridView.Rows[rowIndex].Cells["StateTime"].Value);
                }
                catch
                {
                    time = 0;
                }
                time++;
                dataGridView.Rows[rowIndex].Cells["StateTime"].Value = time.ToString();
            }
        }

        private void ReloadStatus_Click(object sender, EventArgs e)
        {
            RefreshStatus();
        }

        private void dataGridView_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            RefreshStatus();
        }

    }
}
