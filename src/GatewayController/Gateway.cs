using Common.Loggers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GatewayController
{
    public class Gateway
    {
        [DllImport("Kernel32.Dll", EntryPoint = "Wow64EnableWow64FsRedirection")]
        private static extern bool EnableWow64FSRedirection(bool enable);

        private ILogger _____________________________________________________________________________Logger;
        private readonly string IBControllerDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "IBController");
        private string StartBatchPath => Path.Combine(IBControllerDirectory, "IBControllerGatewayStart.bat");
        private string StopBatchPath => Path.Combine(IBControllerDirectory, "IBControllerStop.bat");
        private string IniFile => Path.Combine(IBControllerDirectory, "IBController.ini");
        private string LogDirectory => Path.Combine(IBControllerDirectory, "Logs");




        public Gateway() : this(new SilentLogger())
        {

        }
        public Gateway(ILogger logger)
        {
            _____________________________________________________________________________Logger = logger;
        }



        public void Start(string version, string username, string password, bool liveAccount)
        {
            _____________________________________________________________________________Logger.Info($"Request for starting gateway with version {version} for {username} and live account was set to {liveAccount}");

            // Prevent multiple instances
            if (IsGatewayRunning())
            {
                _____________________________________________________________________________Logger.Info("Gateway is already running. So we'll kill it first");
                Stop();
            }

            // Modify batch file with given information
            _____________________________________________________________________________Logger.Info($"Let's modify some variables in {StartBatchPath}");
            ChangeBatchVariables(StartBatchPath, new NameValueCollection()
            {
                { "TWS_MAJOR_VRSN", version },
                { "IBC_INI", IniFile },
                { "IBC_PATH", IBControllerDirectory },
                { "LOG_PATH", LogDirectory },
                { "TWSUSERID", username },
                { "TWSPASSWORD", password },
                { "TRADING_MODE", liveAccount ? "live" : "paper" }
            });
            _____________________________________________________________________________Logger.Info("Variables were successfully changed");

            // Start IB
            _____________________________________________________________________________Logger.Info($"Starting gateway with by batch file {StartBatchPath}");
            RunBatch(StartBatchPath, "/INLINE");
            _____________________________________________________________________________Logger.Info("Batch file was started");

            // End timeout
            Thread.Sleep(60 * 1000);

            // Check if it running
            if (!IsGatewayRunning())
                throw new GatewayException("Something weird has happened. Even though we started IB Gateway, we were unable to find its process");
        }
        public void Stop()
        {
            _____________________________________________________________________________Logger.Info($"Request for stopping IB Gateway was received");

            // Nothing is needed to be ended
            if (!IsGatewayRunning())
            {
                _____________________________________________________________________________Logger.Info("IB Gateway doesn't run, so no action is needed to be taken");
                return;
            }

            // Try to end it gracefully
            _____________________________________________________________________________Logger.Info($"Let's run stopping batch file: {StopBatchPath}");
            RunBatch(StopBatchPath);
            Thread.Sleep(5000);
            _____________________________________________________________________________Logger.Info("Stopping batch file was run");

            // Kill all telnets we find - this can be remains from previous step
            _____________________________________________________________________________Logger.Info("Killing all telnet processes...");
            foreach (Process telnet in FindTelnetProcesses())
                telnet.Kill();
            Thread.Sleep(1000);
            _____________________________________________________________________________Logger.Info("All telnet processes should be killed");

            // Didn't work? Kill
            if (IsGatewayRunning())
            {
                _____________________________________________________________________________Logger.Info("Obviously stopping IB Gateway gracefully didn't work, so we will find its process and kill it");
                FindGatewayProcess()?.Kill();
                Thread.Sleep(5000);
                _____________________________________________________________________________Logger.Info("We may have killed IB Gateway process");
            }

            // Gateway shouldn't be running
            if (IsGatewayRunning())
                throw new GatewayException("Even though we tried to stop IB Gatway, we are still able to find its process");
        }




        private void RunBatch(string path)
        {
            RunBatch(path, "");
        }
        private void RunBatch(string path, string arguments)
        {
            // We need to disable redirection otherwise some things like telnet doesn't work
            EnableWow64FSRedirection(false);

            // Set up process
            ProcessStartInfo processInfo = new ProcessStartInfo(@"cmd.exe", $"/c {Path.GetFileName(path)} {arguments}")
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(path)
            };

            // Start the process
            Process.Start(processInfo);

            // Enable redirection again
            EnableWow64FSRedirection(true);
        }
        private bool IsGatewayRunning()
        {
            return FindGatewayProcess() != null;
        }
        private Process FindGatewayProcess()
        {
            // It may be run by Java
            Process gatewayProcess = Process.GetProcessesByName("java").SingleOrDefault(x => !String.IsNullOrEmpty(x.MainWindowTitle) && (x.MainWindowTitle.ToUpperInvariant().Contains("IB GATEWAY") || x.MainWindowTitle.ToUpperInvariant().Contains("IBGATEWAY")));

            // Or it runs by Java, but main window title is different
            if (gatewayProcess == null)
                using (ManagementObjectCollection results = new ManagementObjectSearcher("SELECT * FROM Win32_Process WHERE Name = 'java.exe'").Get())
                using (ManagementObject managementObject = results.Cast<ManagementObject>().SingleOrDefault(x => x.Properties["CommandLine"].Value is string str && str != null && str.Contains("ibgateway")))
                    if (managementObject != null)
                        gatewayProcess = Process.GetProcessById((int)(uint)managementObject["ProcessId"]);

            // Or it may be run directly
            if (gatewayProcess == null)
                gatewayProcess = Process.GetProcessesByName("ibgateway").SingleOrDefault();

            _____________________________________________________________________________Logger.Info(gatewayProcess == null ? "We didn't find gateway process" : $"We found a gateway process called '{gatewayProcess.ProcessName}' with window title '{gatewayProcess.MainWindowTitle}' which has started at {gatewayProcess.StartTime.ToUniversalTime().ToString(CultureInfo.InvariantCulture)}");
            return gatewayProcess;
        }
        private Process[] FindTelnetProcesses()
        {
            return Process.GetProcessesByName("telnet");
        }
        private void ChangeBatchVariables(string batchFilePath, NameValueCollection variables)
        {
            // Load file
            List<string> content = File.ReadAllLines(batchFilePath).ToList();

            // Modify variables
            foreach (string key in variables.AllKeys)
            {
                // Find line where this variable is set
                int line = content.FindIndex(x => x.StartsWith($"set {key}=", StringComparison.OrdinalIgnoreCase));

                // Replace variable value
                content[line] = content[line].Substring(0, content[line].IndexOf('=') + 1) + variables[key];
            }

            // Write the file back
            File.WriteAllLines(batchFilePath, content);
        }
    }
}
