using System;
using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Research.ICE.Stitching;
using Microsoft.Research.VisionTools.Toolkit;

namespace Microsoft.Research.ICE.ViewModels
{
    public sealed class SourceFileViewModel : Notifier
    {
        private BitmapSource thumbnail;

        private bool isSelected;

        private int gridColumn = -1;

        private int gridRow = -1;

        public string FilePath { get; private set; }

        public string Name { get; private set; }

        public DateTime? CaptureTime { get; private set; }

        public double? Latitude { get; private set; }

        public double? Longitude { get; private set; }

        public BitmapSource Thumbnail
        {
            get
            {
                return thumbnail;
            }
            set
            {
                SetProperty(ref thumbnail, value, "Thumbnail");
            }
        }

        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
            set
            {
                SetProperty(ref isSelected, value, "IsSelected");
            }
        }

        public int GridColumn
        {
            get
            {
                return gridColumn;
            }
            set
            {
                SetProperty(ref gridColumn, value, "GridColumn");
            }
        }

        public int GridRow
        {
            get
            {
                return gridRow;
            }
            set
            {
                SetProperty(ref gridRow, value, "GridRow");
            }
        }

        public SourceFileViewModel(string filePath)
        {
            FilePath = Path.GetFullPath(filePath);
            Name = Path.GetFileName(filePath);
            DateTime? captureTime = null;
            double? latitude = null;
            double? longitude = null;
            ImageWrapper.GetShellProperties(FilePath, ref captureTime, ref latitude, ref longitude);
            CaptureTime = captureTime;
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}