using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ViscTronics.Zeitlib;

namespace ViscTronics.ZeitgeberGUI
{
    public partial class DebugForm : Form
    {
        public CalendarForm CalendarForm = new CalendarForm();

        public Zeitgeber zeitgeber = new Zeitgeber();
        public bool isConnected = false;

        public DebugForm()
        {
            InitializeComponent();
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            //timerUpdate_Tick(null, null);
            //CalendarForm.Show();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void timerUpdate_Tick(object sender, EventArgs e)
        {
            // Automatic connection/disconnection
            if (zeitgeber.isConnected)
            {
                try
                {
                    zeitgeber.Ping();
                    UpdateDiagnostics();
                }
                catch (Exception ex)
                {
                    zeitgeber.isConnected = false;
                    lblConnected.Text = "Not Connected";
                    listViewInfo.Enabled = false;
                    groupBox1.Enabled = false;
                }
            }
            else
            {
                try
                {
                    zeitgeber.Connect();
                    
                    zeitgeber.Ping();

                    OnDeviceConnect();

                    lblConnected.Text = "Connected";
                    listViewInfo.Enabled = true;
                    groupBox1.Enabled = true;
                }
                catch (Exception ex)
                {
                    zeitgeber.isConnected = false;
                    // Couldn't connect
                }

                
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            zeitgeber.SetDateTime(DateTime.Now);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            zeitgeber.Reset();
        }

        /// <summary>
        /// Called when a device connects
        /// </summary>
        private void OnDeviceConnect()
        {
            txtConsole.Clear();
        }

        /// <summary>
        /// Called by the update timer when the device is connected
        /// </summary>
        private void UpdateDiagnostics()
        {
            listViewInfo.BeginUpdate();
            try
            {

                var cpuInfo = zeitgeber.GetCpuInfo();
                var cpuGroup = listViewInfo.Groups["listViewGroupCPU"];
                cpuGroup.Items[0].SubItems[1].Text = cpuInfo.systick.ToString();

                var batteryInfo = zeitgeber.GetBatteryInfo();
                var batteryGroup = listViewInfo.Groups["listViewGroupBattery"];
                batteryGroup.Items[0].SubItems[1].Text = batteryInfo.Level.ToString() + "%";
                batteryGroup.Items[1].SubItems[1].Text = batteryInfo.Voltage.ToString() + "mV";
                batteryGroup.Items[2].SubItems[1].Text = ((Zeitlib.PowerStatus)batteryInfo.PowerStatus).ToString();

                DateTime? dt = zeitgeber.GetDateTime();
                if (dt.HasValue)
                {
                    var rtcGroup = listViewInfo.Groups["listViewGroupRTC"];
                    rtcGroup.Items[0].SubItems[1].Text = dt.Value.ToLongTimeString();
                    rtcGroup.Items[1].SubItems[1].Text = dt.Value.ToShortDateString();
                }

                string msg;
                do
                {
                    msg = zeitgeber.GetNextDebugMessage();
                    if (msg != null)
                    {
                        msg = msg.Replace("\n", Environment.NewLine);
                        txtConsole.AppendText(msg);
                    }
                } while (msg != null);

            }
            finally
            {
                listViewInfo.EndUpdate();
            }

           // itm.SetValue("Test");
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            
        }

        /// <summary>
        /// Capture a screenshot
        /// </summary>
        private async void btnScreenshot_Click(object sender, EventArgs e)
        {
            btnScreenshot.Enabled = false;
            timerUpdate.Enabled = false;
            imgDisplay.Image = await Task.Run(() => zeitgeber.CaptureScreenImage());
            btnScreenshot.Enabled = true;
            timerUpdate.Enabled = true;
        }

        /// <summary>
        /// Save the captured screenshot to a file
        /// </summary>
        private void btnSaveScreenshot_Click(object sender, EventArgs e)
        {
            if (saveImageDialog.ShowDialog(this) == DialogResult.OK)
            {
                imgDisplay.Image.Save(saveImageDialog.FileName);
            }
        }
    }
}
