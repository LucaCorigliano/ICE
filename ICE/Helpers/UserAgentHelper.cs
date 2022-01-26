using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.Win32;

namespace Microsoft.Research.ICE.Helpers
{
    internal static class UserAgentHelper
    {
        private static string userAgent;

        public static string UserAgent => userAgent ?? (userAgent = GetUserAgent());

        private static string GetUserAgent()
        {
            StringBuilder stringBuilder = new StringBuilder("ImageCompositeEditor/");
            stringBuilder.Append(Assembly.GetEntryAssembly().GetName().Version.ToString());
            stringBuilder.Append(" (");
            stringBuilder.Append(8 * IntPtr.Size);
            stringBuilder.Append("-bit; ");
            stringBuilder.Append(Environment.OSVersion.VersionString);
            try
            {
                RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\NET Framework Setup\\NDP");
                string[] subKeyNames = registryKey.GetSubKeyNames();
                foreach (string name in subKeyNames)
                {
                    RegistryKey registryKey2 = registryKey.OpenSubKey(name);
                    string text = registryKey2.GetValue(null) as string;
                    if (text != "deprecated" && !AppendFrameworkVersion(stringBuilder, registryKey2, null) && !AppendFrameworkVersion(stringBuilder, registryKey2, "Full"))
                    {
                        AppendFrameworkVersion(stringBuilder, registryKey2, "Client");
                    }
                }
            }
            catch 
            {
                
            }
            stringBuilder.Append("; ");
            stringBuilder.Append(CultureInfo.CurrentUICulture.Name);
            stringBuilder.Append("/");
            stringBuilder.Append(CultureInfo.CurrentCulture.Name);
            stringBuilder.Append(")");
            return stringBuilder.ToString();
        }

        private static bool AppendFrameworkVersion(StringBuilder builder, RegistryKey frameworkKey, string profileName)
        {
            if (profileName != null)
            {
                frameworkKey = frameworkKey.OpenSubKey(profileName);
            }
            if (frameworkKey != null && frameworkKey.GetValue("Version") is string value)
            {
                builder.Append("; .NET CLR ");
                builder.Append(value);
                if (profileName != null)
                {
                    builder.Append(' ');
                    builder.Append(profileName);
                }
                return true;
            }
            return false;
        }
    }
}