using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Management;
using System;
using System.Globalization;
using System.Windows;
using System.Reflection;
namespace Microsoft.Research.ICE.Helpers
{
    public static class SystemProperties
    {
        // Methods
        static SystemProperties()
        {
            int dpi = 0x60;
            try
            {
                dpi = (int)typeof(SystemParameters).GetProperty("Dpi", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null);
            }
            catch
            {
            }
            object[] args = new object[] { GetPixelDimension(SystemParameters.PrimaryScreenWidth, dpi), GetPixelDimension(SystemParameters.PrimaryScreenHeight, dpi) };
            ScreenResolution = string.Format(CultureInfo.InvariantCulture, "{0}x{1}", args);
            Dictionary<string, string> dictionary = new Dictionary<string, string> {
            {
                "os",
                Environment.OSVersion.VersionString
            }
        };
            object[] objArray2 = new object[] { GetPixelDimension(SystemParameters.VirtualScreenWidth, dpi), GetPixelDimension(SystemParameters.VirtualScreenHeight, dpi) };
            dictionary.Add("desktop size", string.Format(CultureInfo.InvariantCulture, "{0}x{1}", objArray2));
            Properties = dictionary;
            Dictionary<string, double> dictionary2 = new Dictionary<string, double> {
            {
                "bitness",
                (double) (8 * IntPtr.Size)
            },
            {
                "dpi",
                (double) dpi
            }
        };
            Metrics = dictionary2;
            NativeMethods.MEMORYSTATUSEX lpBuffer = new NativeMethods.MEMORYSTATUSEX();
            if (NativeMethods.GlobalMemoryStatusEx(lpBuffer))
            {
                PhysicalMemory = (int)(lpBuffer.ullTotalPhys / ((ulong)0x10_0000L));
                Metrics.Add("memory", (double)PhysicalMemory);
            }
            QueryCpuProperties();
            QueryGpuProperties();
        }

        private static void AddMetric(string prefix, ManagementObject managementObject, string wmiPropertyName, string friendlyName, double denominator = 1.0)
        {
            try
            {
                PropertyData data = managementObject.Properties[wmiPropertyName];
                if ((data != null) && (data.Value != null))
                {
                    double num = Convert.ToDouble(data.Value) / denominator;
                    Metrics.Add(prefix + friendlyName, num);
                }
            }
            catch
            {
            }
        }

        private static void AddProperty(string prefix, ManagementObject managementObject, string wmiPropertyName, string friendlyName)
        {
            try
            {
                PropertyData data = managementObject.Properties[wmiPropertyName];
                if ((data != null) && (data.Value != null))
                {
                    Properties.Add(prefix + friendlyName, data.Value.ToString());
                }
            }
            catch
            {
            }
        }

        private static int GetPixelDimension(double dimension, int dpi) =>
            (int)Math.Round((double)((dimension * dpi) / 0x60));

        private static void QueryCpuProperties()
        {
            try
            {
                int num = 0;
                foreach (ManagementObject cpuData in new ManagementObjectSearcher("SELECT * FROM Win32_Processor").Get())
                {
                    num++;
                    object[] args = new object[] { num };
                    string prefix = string.Format(CultureInfo.InvariantCulture, "cpu{0} ", args);
                    AddProperty(prefix, cpuData, "Name", "name");
                    AddProperty(prefix, cpuData, "Manufacturer", "manufacturer");
                    AddMetric(prefix, cpuData, "NumberOfCores", "cores", 1.0);
                    AddMetric(prefix, cpuData, "NumberOfLogicalProcessors", "logical processors", 1.0);
                    AddMetric(prefix, cpuData, "L2CacheSize", "l2 cache", 1.0);
                    AddMetric(prefix, cpuData, "L3CacheSize", "l3 cache", 1.0);
                }
            }
            catch
            {
            }
        }

        private static void QueryGpuProperties()
        {
            try
            {
                int num = 0;
                foreach (ManagementObject gpuData in new ManagementObjectSearcher("SELECT * FROM Win32_VideoController").Get())
                {
                    PropertyData data = gpuData.Properties["CurrentBitsPerPixel"];
                    if ((data != null) && (data.Value != null))
                    {
                        object[] args = new object[] { num + 1 };
                        string prefix = string.Format(CultureInfo.InvariantCulture, "gpu{0} ", args);
                        AddProperty(prefix, gpuData, "Name", "name");
                        AddProperty(prefix, gpuData, "AdapterCompatibility", "manufacturer");
                        AddProperty(prefix, gpuData, "InstalledDisplayDrivers", "drivers");
                        AddProperty(prefix, gpuData, "DriverDate", "driver date");
                        AddProperty(prefix, gpuData, "DriverVersion", "driver version");
                        AddMetric(prefix, gpuData, "CurrentBitsPerPixel", "bits per pixel", 1.0);
                        AddMetric(prefix, gpuData, "AdapterRAM", "memory", 0x10_0000);
                    }
                }
            }
            catch
            {
            }
        }

        // Properties
        public static string ScreenResolution { get; private set; }

        public static int PhysicalMemory { get; private set; }

        public static IDictionary<string, string> Properties { get; private set; }

        public static IDictionary<string, double> Metrics { get; private set; }

        // Nested Types
        private sealed class NativeMethods
        {
            // Methods
            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

            // Nested Types
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            public class MEMORYSTATUSEX
            {
                public uint dwLength = ((uint)Marshal.SizeOf(typeof(SystemProperties.NativeMethods.MEMORYSTATUSEX)));
                public uint dwMemoryLoad;
                public ulong ullTotalPhys;
                public ulong ullAvailPhys;
                public ulong ullTotalPageFile;
                public ulong ullAvailPageFile;
                public ulong ullTotalVirtual;
                public ulong ullAvailVirtual;
                public ulong ullAvailExtendedVirtual;
            }
        }
    }


}