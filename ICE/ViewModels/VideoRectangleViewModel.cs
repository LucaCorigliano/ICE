using System;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Research.VisionTools.Toolkit;

namespace Microsoft.Research.ICE.ViewModels
{
    public sealed class VideoRectangleViewModel : Notifier
    {
        private TimeSpan time;

        private int left;

        private int top;

        private int right;

        private int bottom;

        private bool isSelected;

        private BitmapSource image;

        public TimeSpan Time
        {
            get
            {
                return time;
            }
            set
            {
                SetProperty(ref time, value, "Time");
            }
        }

        public int Left
        {
            get
            {
                return left;
            }
            set
            {
                SetProperty(ref left, value, "Left");
            }
        }

        public int Top
        {
            get
            {
                return top;
            }
            set
            {
                SetProperty(ref top, value, "Top");
            }
        }

        public int Right
        {
            get
            {
                return right;
            }
            set
            {
                SetProperty(ref right, value, "Right");
            }
        }

        public int Bottom
        {
            get
            {
                return bottom;
            }
            set
            {
                SetProperty(ref bottom, value, "Bottom");
            }
        }

        public Int32Rect Rect => new Int32Rect(Left, Top, Right - Left, Bottom - Top);

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

        public BitmapSource Image
        {
            get
            {
                return image;
            }
            set
            {
                SetProperty(ref image, value, "Image");
            }
        }
    }
}