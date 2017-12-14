using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DWARF;
using System.Diagnostics;

namespace CPU_Utilization
{

    [Export(typeof(DWARF.IPlugin))]
    [ExportMetadata("Identifier", "CPU_Utilization")]
    public class CPUUtilization : DWARF.IPlugin
    {
        public List<ApplicationForm> monitoringForms = new List<ApplicationForm>();
        Timer MonitorTimer;
        PerformanceCounter[] performanceCounters;

        public object GetData(List<string> args, ApplicationForm form)
        {
            if (MonitorTimer == null)
            {
                MonitorTimer = new Timer();
                MonitorTimer.Interval = 1000;
                MonitorTimer.Tick += MonitorTimer_Tick;
                MonitorTimer.Start();
            }

            if (performanceCounters == null)
            {
                int coreCount = 0;
                foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
                {
                    coreCount += int.Parse(item["NumberOfCores"].ToString());
                }

                performanceCounters = new PerformanceCounter[coreCount+1];

                for (int i = 0; i < coreCount; i++)
                {
                    performanceCounters[i] = new PerformanceCounter("Processor", "% Processor Time", i.ToString());
                }

                performanceCounters[coreCount] = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            }

            if (args[0] == "Instantiate")
            {
                if (!monitoringForms.Contains(form))
                {
                    monitoringForms.Add(form);
                }
            }
            else if (args[0] == "SetMonitorInterval")
            {
                try
                {
                    MonitorTimer.Interval = Convert.ToInt32(args[1]);
                    if (MonitorTimer.Interval < 1000)
                        MonitorTimer.Interval = 1000;
                }
                catch
                {

                }
            }
            return "";
        }

        public void OnEnabled()
        {

        }

        public void OnDisabled()
        {

        }

        void MonitorTimer_Tick(object sender, EventArgs e)
        {
            string js2send = "";

            string coresjson = "{";

            for (int i = 0; i < performanceCounters.Length; i++)
            {
                if (performanceCounters[i].InstanceName == "_Total")
                    js2send = "CPU_UtilizationHelper.ontotalutilizationupdate(" + performanceCounters[i].NextValue() + ");";
                else
                    coresjson += performanceCounters[i].InstanceName + ": " + performanceCounters[i].NextValue() + ", ";
            }

            coresjson = coresjson.Remove(coresjson.Length - 2, 2);
            coresjson += "}";
            js2send += " CPU_UtilizationHelper.oncoreutilizationupdate(" + coresjson + ");";

            foreach (ApplicationForm form in monitoringForms)
            {
                form.JS(js2send);
            }
        }
    }
}
