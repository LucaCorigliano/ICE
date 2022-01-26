using System;
using System.Collections.Generic;
using Microsoft.Research.ICE.Stitching;
using Microsoft.Research.VisionTools.Toolkit;

namespace Microsoft.Research.ICE.ViewModels
{
    public sealed class ImageExportFormatViewModel : Notifier
    {
        private const bool DefaultUseLayers = true;

        private const bool DefaultMaximizeCompatibility = false;

        private const uint DefaultMaximumDimension = 1073741823u;

        private const ulong DefaultMaximumArea = ulong.MaxValue;

        private bool includeAlphaChannel;

        private bool useLayers;

        private bool maximizeCompatibility;

        private bool canExport;

        public ExportFormat Format { get; private set; }

        public string Name { get; private set; }

        public string FileFilter { get; private set; }

        public string DefaultFileExtension { get; private set; }

        public uint MaximumDimension { get; private set; }

        public ulong MaximumArea { get; private set; }

        public bool HasQualitySetting => Quality != null;

        public bool SupportsAlphaChannel { get; private set; }

        public bool HasAlphaChannelSetting { get; private set; }

        public bool HasLayerSettings { get; private set; }

        public CompressionQualityViewModel Quality { get; private set; }

        public bool IncludeAlphaChannel
        {
            get
            {
                return includeAlphaChannel;
            }
            set
            {
                SetProperty(ref includeAlphaChannel, value, "IncludeAlphaChannel");
            }
        }

        public bool UseLayers
        {
            get
            {
                return useLayers;
            }
            set
            {
                SetProperty(ref useLayers, value, "UseLayers");
            }
        }

        public bool MaximizeCompatibility
        {
            get
            {
                return maximizeCompatibility;
            }
            set
            {
                SetProperty(ref maximizeCompatibility, value, "MaximizeCompatibility");
            }
        }

        public bool CanExport
        {
            get
            {
                return canExport;
            }
            set
            {
                SetProperty(ref canExport, value, "CanExport");
            }
        }

        private ImageExportFormatViewModel(ExportFormat format, string name, string fileFilter, string defaultFileExtension, bool hasQualitySetting = false, bool hasLosslessQualitySetting = false)
        {
            Format = format;
            Name = name;
            FileFilter = fileFilter;
            DefaultFileExtension = defaultFileExtension;
            UseLayers = true;
            MaximizeCompatibility = false;
            MaximumDimension = 1073741823u;
            MaximumArea = ulong.MaxValue;
            CanExport = true;
            if (hasQualitySetting)
            {
                Quality = new CompressionQualityViewModel(hasLosslessQualitySetting);
            }
        }

        public override string ToString()
        {
            return Name;
        }

        public OutputOptions CreateOutputOptions()
        {
            if (Format == ExportFormat.Invalid)
            {
                throw new ArgumentException("Selected output format not implemented in StitchEngineWrapper");
            }
            return new OutputOptions(Format, HasQualitySetting ? Quality.Value : 0, IncludeAlphaChannel, UseLayers, MaximizeCompatibility);
        }

        public static List<ImageExportFormatViewModel> GetImageExportFormats()
        {
            List<ImageExportFormatViewModel> list = new List<ImageExportFormatViewModel>();
            list.Add(new ImageExportFormatViewModel(ExportFormat.JPEG, "JPEG Image", "JPEG File (*.jpg, *.jpeg)|*.jpg;*.jpeg", "jpg", hasQualitySetting: true)
            {
                MaximumDimension = 65500u
            });
            list.Add(new ImageExportFormatViewModel(ExportFormat.JXR, "JPEG XR Image", "JPEG XR File (*.jxr)|*.jxr", "jxr", hasQualitySetting: true, hasLosslessQualitySetting: true)
            {
                HasAlphaChannelSetting = true,
                MaximumArea = 536870912uL,
                MaximumDimension = 1000000u
            });
            list.Add(new ImageExportFormatViewModel(ExportFormat.PSD, "Adobe Photoshop", "Photoshop File (*.psd, *.psb)|*.psd;*.psb", "psd")
            {
                IncludeAlphaChannel = true,
                MaximumDimension = 300000u,
                HasLayerSettings = true
            });
            list.Add(new ImageExportFormatViewModel(ExportFormat.TIFF, "TIFF Image", "TIFF File (*.tif, *.tiff)|*.tif;*.tiff", "tiff")
            {
                HasAlphaChannelSetting = true,
                MaximumArea = 536870912uL,
                MaximumDimension = 1000000u
            });
            list.Add(new ImageExportFormatViewModel(ExportFormat.PNG, "PNG Image", "PNG File (*.png)|*.png", "png")
            {
                HasAlphaChannelSetting = true,
                MaximumDimension = 65535u
            });
            list.Add(new ImageExportFormatViewModel(ExportFormat.BMP, "Windows Bitmap", "Bitmap File (*.bmp, *.dib)|*.bmp;*.dib", "bmp")
            {
                MaximumArea = 67108863uL,
                MaximumDimension = 1000000u
            });
            return list;
        }
    }
}