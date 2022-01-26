using System;

namespace Microsoft.Research.ICE.ViewModels
{
    public sealed class ExportCompletedEventArgs : EventArgs
    {
        public string PanoramaLocation { get; private set; }

        public ExportCompletedEventArgs(string panoramaLocation)
        {
            PanoramaLocation = panoramaLocation;
        }
    }
}