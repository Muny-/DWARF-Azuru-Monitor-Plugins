using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DWARF;

namespace RAM_Usage
{
    [Export(typeof(DWARF.IPlugin))]
    [ExportMetadata("Identifier", "RAM_Usage")]
    public class RAM_Usage : DWARF.IPlugin
    {
        Timer MonitorTimer;
        public List<ApplicationForm> monitoringForms = new List<ApplicationForm>();

        public object GetData(List<string> args, ApplicationForm form)
        {
            if (MonitorTimer == null)
            {
                MonitorTimer = new Timer();
                MonitorTimer.Interval = 1000;
                MonitorTimer.Tick += MonitorTimer_Tick;
                MonitorTimer.Start();
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
                    if (MonitorTimer.Interval < 100)
                        MonitorTimer.Interval = 100;
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
            Int64 total = PerformanceInfo.GetTotalMemoryInMiB();
            Int64 used = total - PerformanceInfo.GetPhysicalAvailableMemoryInMiB();

            string js = "RAM_UsageHelper.onmemoryusageupdate(" + total.ToString() + ", " + used.ToString() + ");";

            foreach (ApplicationForm form in monitoringForms)
            {
                form.JS(js);
            }
        }
    }

    public static class PerformanceInfo
    {
        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetPerformanceInfo([Out] out PerformanceInformation PerformanceInformation, [In] int Size);

        [StructLayout(LayoutKind.Sequential)]
        public struct PerformanceInformation
        {
            public int Size;
            public IntPtr CommitTotal;
            public IntPtr CommitLimit;
            public IntPtr CommitPeak;
            public IntPtr PhysicalTotal;
            public IntPtr PhysicalAvailable;
            public IntPtr SystemCache;
            public IntPtr KernelTotal;
            public IntPtr KernelPaged;
            public IntPtr KernelNonPaged;
            public IntPtr PageSize;
            public int HandlesCount;
            public int ProcessCount;
            public int ThreadCount;
        }

        public static Int64 GetPhysicalAvailableMemoryInMiB()
        {
            PerformanceInformation pi = new PerformanceInformation();
            if (GetPerformanceInfo(out pi, Marshal.SizeOf(pi)))
            {
                return Convert.ToInt64((pi.PhysicalAvailable.ToInt64() * pi.PageSize.ToInt64() / 1048576));
            }
            else
            {
                return -1;
            }

        }

        public static Int64 GetTotalMemoryInMiB()
        {
            PerformanceInformation pi = new PerformanceInformation();
            if (GetPerformanceInfo(out pi, Marshal.SizeOf(pi)))
            {
                return Convert.ToInt64((pi.PhysicalTotal.ToInt64() * pi.PageSize.ToInt64() / 1048576));
            }
            else
            {
                return -1;
            }

        }
    }
}
