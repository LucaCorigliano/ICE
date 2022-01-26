using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using Microsoft.Research.ICE.Stitching;

namespace Microsoft.Research.ICE.ViewModels
{
    public sealed class ImageExportViewModel : ExportViewModel
    {
        private const double DefaultExportScale = 1.0;

        private Rect cropRect;

        private double exportScale;

        private ImageExportFormatViewModel currentImageExportFormat;

        public Rect CropRect
        {
            get
            {
                return cropRect;
            }
            set
            {
                if (SetProperty(ref cropRect, value, "CropRect"))
                {
                    ConstrainExportScale();
                    ConstrainImageExportFormats();
                    NotifyPropertyChanged("ExportWidth");
                    NotifyPropertyChanged("ExportHeight");
                    NotifyPropertyChanged("ExportPixelSummary");
                }
            }
        }

        private double MinExportScale { get; set; }

        public double ExportScale
        {
            get
            {
                return exportScale;
            }
            set
            {
                value = Math.Max(MinExportScale, Math.Min(value, 1.0));
                if (SetProperty(ref exportScale, value, "ExportScale"))
                {
                    ConstrainImageExportFormats();
                    NotifyPropertyChanged("ExportWidth");
                    NotifyPropertyChanged("ExportHeight");
                    NotifyPropertyChanged("ExportPixelSummary");
                }
            }
        }

        public int ExportWidth
        {
            get
            {
                return GetWidthAtScale(ExportScale);
            }
            set
            {
                value = Math.Max(1, value);
                if (ExportWidth != value)
                {
                    ExportScale = GetExportScaleWithFewestDecimalPlaces(value, useWidth: true);
                }
            }
        }

        public int ExportHeight
        {
            get
            {
                return GetHeightAtScale(ExportScale);
            }
            set
            {
                value = Math.Max(1, value);
                if (ExportHeight != value)
                {
                    ExportScale = GetExportScaleWithFewestDecimalPlaces(value, useWidth: false);
                }
            }
        }

        public string ExportPixelSummary
        {
            get
            {
                double num = (double)ExportWidth * (double)ExportHeight;
                string[] array = new string[4]
                {
                string.Empty,
                "mega",
                "giga",
                "tera"
                };
                int num2 = 0;
                if (num >= 100000.0)
                {
                    num /= 1000000.0;
                    num2++;
                    while (num >= 1000.0 && num2 < array.Length - 1)
                    {
                        num /= 1000.0;
                        num2++;
                    }
                }
                num = Math.Round(num, 2);
                return string.Format(CultureInfo.CurrentCulture, "{0:#,##0.##} {1}pixel{2}", new object[3]
                {
                num,
                array[num2],
                (num == 1.0) ? string.Empty : "s"
                });
            }
        }

        public IEnumerable<ImageExportFormatViewModel> ImageExportFormats { get; private set; }

        public ImageExportFormatViewModel CurrentImageExportFormat
        {
            get
            {
                return currentImageExportFormat;
            }
            set
            {
                SetProperty(ref currentImageExportFormat, value, "CurrentImageExportFormat");
            }
        }

        public override string FileFilter => CurrentImageExportFormat.FileFilter;

        public override string DefaultFileExtension => currentImageExportFormat.DefaultFileExtension;

        public ImageExportViewModel()
        {
            ImageExportFormats = ImageExportFormatViewModel.GetImageExportFormats();
            currentImageExportFormat = ImageExportFormats.FirstOrDefault();
            exportScale = 1.0;
        }

        public override OutputOptions CreateOutputOptions()
        {
            return CurrentImageExportFormat.CreateOutputOptions();
        }

        private void ConstrainExportScale()
        {
            double width = CropRect.Width;
            double height = CropRect.Height;
            MinExportScale = GetExportScaleWithFewestDecimalPlaces(1, width < height);
            if (ExportScale < MinExportScale)
            {
                ExportScale = MinExportScale;
            }
        }

        private void ConstrainImageExportFormats()
        {
            int exportWidth = ExportWidth;
            int exportHeight = ExportHeight;
            double num = (double)exportWidth * (double)exportHeight;
            foreach (ImageExportFormatViewModel imageExportFormat in ImageExportFormats)
            {
                imageExportFormat.CanExport = exportWidth <= imageExportFormat.MaximumDimension && exportHeight <= imageExportFormat.MaximumDimension && num <= (double)imageExportFormat.MaximumArea;
            }
            if (!CurrentImageExportFormat.CanExport)
            {
                CurrentImageExportFormat = ImageExportFormats.First((ImageExportFormatViewModel imageExportFormat) => imageExportFormat.CanExport);
            }
        }

        private double GetExportScaleWithFewestDecimalPlaces(int desiredSize, bool useWidth)
        {
            double num = (useWidth ? CropRect.Width : CropRect.Height);
            if ((double)desiredSize >= num)
            {
                return 1.0;
            }
            double num2 = 0.0;
            double num3 = 10.0;
            for (int i = -1; i <= 4; i++)
            {
                int num4 = 0;
                int num5 = 10;
                while (num5 > num4)
                {
                    int num6 = (num4 + num5) / 2;
                    double num7 = (num2 * num3 + (double)num6) / num3;
                    int num8 = (useWidth ? GetWidthAtScale(num7) : GetHeightAtScale(num7));
                    if (num8 == desiredSize)
                    {
                        return num7;
                    }
                    if (num5 - num4 == 1)
                    {
                        num2 = num7;
                        break;
                    }
                    if (num8 < desiredSize)
                    {
                        num4 = num6;
                    }
                    else if (num8 > desiredSize)
                    {
                        num5 = num6;
                    }
                }
                num3 *= 10.0;
            }
            return num2;
        }

        private int GetWidthAtScale(double scale)
        {
            return (int)Math.Round(CropRect.Right * scale) - (int)Math.Round(CropRect.Left * scale);
        }

        private int GetHeightAtScale(double scale)
        {
            return (int)Math.Round(CropRect.Bottom * scale) - (int)Math.Round(CropRect.Top * scale);
        }
    }
}