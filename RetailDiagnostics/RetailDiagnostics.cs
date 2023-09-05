using System;
using System.Windows.Forms;

namespace RetailDiagnostics {
    public partial class RetailDiagnostics : Form {
        public RetailDiagnostics() {
            InitializeComponent();            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "Complete")
            {
                Hide();
            }

            var rand = new Random();
            Program.runStats();
            button1.Enabled = false;
            button1.Text = "Running...";

            progressBar1.Value = progressBar1.Minimum;
            for (var i = progressBar1.Minimum; i < progressBar1.Maximum; i++)
            {
                if (progressBar1.Value > 0 && progressBar1.Value < 15)
                {
                    label1.Text = "Calibrating...";
                }
                if (progressBar1.Value >= 15 && progressBar1.Value < 35)
                {
                    label1.Text = "OS logs...";
                }
                if (progressBar1.Value >= 35 && progressBar1.Value < 60)
                {
                    label1.Text = "Application logs...";
                }
                if (progressBar1.Value >= 60 && progressBar1.Value < 75)
                {
                    label1.Text = "Network utilization...";
                }
                if (progressBar1.Value >= 75 && progressBar1.Value < 95)
                {
                    label1.Text = "Unknown events...";
                }
                if (progressBar1.Value >= 95)
                {
                    label1.Text = "Reticulating splines...";
                }
                System.Threading.Thread.Sleep(rand.Next(50, 250));
                progressBar1.Increment(1);
                Application.DoEvents();
            }

            label1.Text = "Ready for Upload.";
            button1.Text = "Upload";
            button1.Enabled = true;

            progressBar1.Value = progressBar1.Minimum;
            button1.Text = "Uploading";
            button1.Enabled = false;
            label1.Text = "Sending to server...";
            for (var p = progressBar1.Minimum; p < progressBar1.Maximum; p++)
            {
                System.Threading.Thread.Sleep(rand.Next(10, 50));
                progressBar1.Increment(1);
                Application.DoEvents();
            }
            button1.Text = "Complete";
            button1.Enabled = true;
        }
    }
}
