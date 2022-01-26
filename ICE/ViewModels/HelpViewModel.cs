using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.Research.VisionTools.Toolkit;


namespace Microsoft.Research.ICE.ViewModels
{
    public sealed class HelpViewModel : Notifier
    {
        public string Version { get; private set; }

        public string BuildDate { get; private set; }

        public HelpViewModel()
        {
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            try
            {
                DateTime dateTime = RetrieveLinkerTimestamp(entryAssembly.Location);
                BuildDate = string.Format(CultureInfo.InvariantCulture, "built {0:yyyy-MM-dd HH:mm:ss}", new object[1] { dateTime });
            }
            catch 
            {
                
                BuildDate = "unknown build date";
            }
            Version = string.Format(CultureInfo.InvariantCulture, "Version {0} ({1} bit), {2}", new object[3]
            {
            entryAssembly.GetName().Version,
            8 * IntPtr.Size,
            BuildDate
            });
        }

        private static DateTime RetrieveLinkerTimestamp(string filePath)
        {
            byte[] array = new byte[2048];
            Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            stream.Read(array, 0, 2048);
            stream.Close();
            int num = BitConverter.ToInt32(array, 60);
            int num2 = BitConverter.ToInt32(array, num + 8);
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(num2);
        }
    }
}