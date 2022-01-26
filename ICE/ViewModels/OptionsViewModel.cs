using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Research.VisionTools.Toolkit;


namespace Microsoft.Research.ICE.ViewModels
{
    public sealed class OptionsViewModel : Notifier
    {
        private int memoryConsumptionLimit;

        private bool checkForUpdatesAtStartup;

        public int TotalMemory { get; private set; }

        public string TotalMemoryMessage { get; private set; }

        public int DefaultMemoryConsumptionLimit { get; private set; }

        public int MemoryConsumptionLimit
        {
            get
            {
                return memoryConsumptionLimit;
            }
            set
            {
                value = Math.Max(MinMemoryConsumptionLimit, Math.Min(value, MaxMemoryConsumptionLimit));
                SetProperty(ref memoryConsumptionLimit, value, "MemoryConsumptionLimit");
            }
        }

        private int MinMemoryConsumptionLimit => 64;

        public int MaxMemoryConsumptionLimit
        {
            get
            {
                int num = TotalMemory;
                if (Is32Bit)
                {
                    num = Math.Min(num, 2048);
                }
                return num;
            }
        }

        public bool Is32Bit => IntPtr.Size < 8;

        public ObservableCollection<CacheLocationViewModel> ImageCacheLocations { get; private set; }

        public bool CanResetCacheLocations => ImageCacheLocations.All((CacheLocationViewModel c) => c.IsEditable);

        public string CacheResetMessage
        {
            get
            {
                if (!CanResetCacheLocations)
                {
                    return "Cannot reset while project is open";
                }
                return "Use default location";
            }
        }

        public bool CheckForUpdatesAtStartup
        {
            get
            {
                return checkForUpdatesAtStartup;
            }
            set
            {
                SetProperty(ref checkForUpdatesAtStartup, value, "CheckForUpdatesAtStartup");
            }
        }

        public OptionsViewModel()
        {
            int physicalMemory = Helpers.SystemProperties.PhysicalMemory;
            if (physicalMemory > 0)
            {
                TotalMemory = physicalMemory;
                TotalMemoryMessage = $"{TotalMemory:N0} MB";
                DefaultMemoryConsumptionLimit = 4 * TotalMemory / 10;
                if (Is32Bit)
                {
                    DefaultMemoryConsumptionLimit = Math.Min(DefaultMemoryConsumptionLimit, 512);
                }
                DefaultMemoryConsumptionLimit = Math.Max(DefaultMemoryConsumptionLimit, 256);
            }
            else
            {
                TotalMemory = 1048576;
            TotalMemoryMessage = "unknown";
            DefaultMemoryConsumptionLimit = 256;
            }
            ImageCacheLocations = new ObservableCollection<CacheLocationViewModel>();
        }

        public void AddImageCacheLocation(string imageCacheLocation)
        {
            CacheLocationViewModel item = new CacheLocationViewModel(imageCacheLocation);
            ImageCacheLocations.Add(item);
            UpdateCacheLocations();
        }

        public void ChangeImageCacheLocation(int index, string imageCacheLocation)
        {
            ImageCacheLocations[index].DirectoryPath = imageCacheLocation;
            UpdateCacheLocations();
        }

        public void RemoveImageCacheLocation(int index)
        {
            ImageCacheLocations.RemoveAt(index);
            UpdateCacheLocations();
        }

        public void MoveImageCacheLocation(int fromIndex, int toIndex)
        {
            CacheLocationViewModel item = ImageCacheLocations[fromIndex];
            ImageCacheLocations.RemoveAt(fromIndex);
            ImageCacheLocations.Insert(toIndex, item);
            UpdateCacheLocations();
        }

        private void UpdateCacheLocations()
        {
            HashSet<string> hashSet = new HashSet<string>();
            for (int i = 0; i < ImageCacheLocations.Count; i++)
            {
                CacheLocationViewModel cacheLocationViewModel = ImageCacheLocations[i];
                bool hasExternalError = hashSet.Contains(cacheLocationViewModel.RootDrive);
                ImageCacheLocations[i].UpdateNumber(i + 1, hasExternalError);
                hashSet.Add(cacheLocationViewModel.RootDrive);
            }
        }
    }
}