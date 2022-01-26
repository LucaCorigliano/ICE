using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Research.VisionTools.Toolkit;


namespace Microsoft.Research.ICE.ViewModels
{
    public sealed class CacheLocationViewModel : Notifier
    {
        private sealed class NativeMethods
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);
        }

        private bool isEditable;

        private int number;

        private bool hasInternalError;

        private bool hasExternalError;

        private string directoryPath;

        private string diskSpaceMessage;

        public bool IsEditable
        {
            get
            {
                return isEditable;
            }
            set
            {
                if (SetProperty(ref isEditable, value, "IsEditable"))
                {
                    NotifyPropertyChanged("InUseMessage");
                }
            }
        }

        public string InUseMessage
        {
            get
            {
                if (!IsEditable)
                {
                    return "[in use]";
                }
                return null;
            }
        }

        public int Number
        {
            get
            {
                return number;
            }
            private set
            {
                SetProperty(ref number, value, "Number");
            }
        }

        public bool HasInternalError
        {
            get
            {
                return hasInternalError;
            }
            private set
            {
                if (SetProperty(ref hasInternalError, value, "HasInternalError"))
                {
                    NotifyPropertyChanged("HasError");
                }
            }
        }

        public bool HasExternalError
        {
            get
            {
                return hasExternalError;
            }
            set
            {
                if (SetProperty(ref hasExternalError, value, "HasExternalError"))
                {
                    NotifyPropertyChanged("HasError");
                    NotifyPropertyChanged("DiskSpaceMessage");
                }
            }
        }

        public bool HasError
        {
            get
            {
                if (!HasInternalError)
                {
                    return HasExternalError;
                }
                return true;
            }
        }

        public string DirectoryPath
        {
            get
            {
                return directoryPath;
            }
            set
            {
                if (SetProperty(ref directoryPath, value, "DirectoryPath"))
                {
                    UpdateStatus();
                }
            }
        }

        public string RootDrive => Path.GetPathRoot(ExpandedPath).TrimEnd('\\');

        public string ExpandedPath => Path.GetFullPath(Environment.ExpandEnvironmentVariables(DirectoryPath));

        public string DiskSpaceMessage
        {
            get
            {
                if (!HasExternalError)
                {
                    return diskSpaceMessage;
                }
                return $"{RootDrive} is already used above.";
            }
            private set
            {
                SetProperty(ref diskSpaceMessage, value, "DiskSpaceMessage");
            }
        }

        public CacheLocationViewModel(string directory)
        {
            DirectoryPath = directory;
            IsEditable = true;
        }

        public void UpdateNumber(int number, bool hasExternalError)
        {
            Number = number;
            HasExternalError = hasExternalError;
        }

        private void UpdateStatus()
        {
            string text = null;
            bool flag = true;
            string expandedPath = ExpandedPath;
            if (!Directory.Exists(expandedPath))
            {
                try
                {
                    Directory.CreateDirectory(expandedPath);
                }
                catch
                {
                    
                    text = "Could not create directory.";
                }
            }
            if (text == null)
            {
                try
                {
                    string path = Path.Combine(expandedPath, Path.GetRandomFileName());
                    File.WriteAllText(path, string.Empty);
                    File.Delete(path);
                }
                catch
                {
                    
                    text = "Could not write files in directory.";
                }
            }
            if (text == null)
            {
                try
                {
                    NativeMethods.GetDiskFreeSpaceEx(expandedPath, out var lpFreeBytesAvailable, out var lpTotalNumberOfBytes, out var _);
                    text = $"{RootDrive} has {FormatSize(lpFreeBytesAvailable)} free of {FormatSize(lpTotalNumberOfBytes)}.";
                    flag = false;
                }
                catch
                {
                    
                    text = "Could not determine free disk space.";
                }
            }
            DiskSpaceMessage = text;
            HasInternalError = flag;
        }

        private static string FormatSize(double size)
        {
            string[] array = new string[6] { "bytes", "KB", "MB", "GB", "TB", "PB" };
            int num = 0;
            while (size > 1024.0 && num < array.Length - 1)
            {
                size /= 1024.0;
                num++;
            }
            return $"{size:G3} {array[num]}";
        }
    }
}