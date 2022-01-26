using System;

namespace Microsoft.Research.ICE.ViewModels
{
    public class SettingsChangedEventArgs : EventArgs
    {
        public DirtyFlags DirtyFlags { get; private set; }

        public SettingsChangedEventArgs(DirtyFlags dirtyFlags)
        {
            DirtyFlags = dirtyFlags;
        }
    }
}