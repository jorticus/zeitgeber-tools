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
    public partial class MainForm : Form
    {
        public Zeitgeber zeitgeber = new Zeitgeber();
        public bool isConnected = false;

        public MainForm()
        {
            InitializeComponent();
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            //timerUpdate_Tick(null, null);
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
                catch (Exception)
                {
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
                    UpdateDiagnostics();

                    lblConnected.Text = "Connected";
                    listViewInfo.Enabled = true;
                    groupBox1.Enabled = true;
                }
                catch (Exception)
                {
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

        private void UpdateDiagnostics()
        {
            listViewInfo.BeginUpdate();

            var cpuInfo = zeitgeber.GetCpuInfo();
            var cpuGroup = listViewInfo.Groups["listViewGroupCPU"];
            cpuGroup.Items[0].SubItems[1].Text = cpuInfo.systick.ToString();

            var batteryInfo = zeitgeber.GetBatteryInfo();
            var batteryGroup = listViewInfo.Groups["listViewGroupBattery"];
            batteryGroup.Items[0].SubItems[1].Text = batteryInfo.Level.ToString() + "%";
            batteryGroup.Items[1].SubItems[1].Text = batteryInfo.Voltage.ToString() + "mV";
            batteryGroup.Items[2].SubItems[1].Text = ((Zeitlib.PowerStatus)batteryInfo.PowerStatus).ToString();

            var dt = zeitgeber.GetDateTime();
            var rtcGroup = listViewInfo.Groups["listViewGroupRTC"];
            rtcGroup.Items[0].SubItems[1].Text = dt.ToLongTimeString();
            rtcGroup.Items[1].SubItems[1].Text = dt.ToShortDateString();

            listViewInfo.EndUpdate();

           // itm.SetValue("Test");
        }
    }
}
