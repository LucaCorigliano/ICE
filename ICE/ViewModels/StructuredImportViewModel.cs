using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using Microsoft.Research.ICE.Stitching;
using Microsoft.Research.ICE.ThumbnailAnalyzer;
using Microsoft.Research.VisionTools.Toolkit;

namespace Microsoft.Research.ICE.ViewModels
{
    public sealed class StructuredImportViewModel : Notifier
    {
        private ThumbnailAnalyzerWrapper thumbnailAnalyzerWrapper;

        private List<SourceFileViewModel> sortedSourceFiles;

        private double progress;

        private bool isReadingThumbnails;

        private double averageAspectRatio;

        private StartingCorner startingCorner;

        private PrimaryDirection primaryDirection;

        private int primaryDirectionImageCount;

        private MovementMethod movementMethod;

        private AngularRange angularRange;

        private double horizontalOverlap;

        private double verticalOverlap;

        private double seamOverlap;

        private double featureMatchingSearchRadius;

        private bool previewOverlap;

        public List<SourceFileViewModel> SortedSourceFiles
        {
            get
            {
                return sortedSourceFiles;
            }
            set
            {
                if (SetProperty(ref sortedSourceFiles, value, "SortedSourceFiles"))
                {
                    NotifyPropertyChanged("ImageCount");
                    if (PrimaryDirectionImageCount == 0 && sortedSourceFiles.Count > 0)
                    {
                        PrimaryDirectionImageCount = (int)Math.Round(Math.Sqrt(sortedSourceFiles.Count));
                    }
                    else
                    {
                        NotifyPropertyChanged("SecondaryDirectionImageCount");
                        NotifyPropertyChanged("ColumnCount");
                        NotifyPropertyChanged("RowCount");
                        NotifyPropertyChanged("HasMultipleColumns");
                        NotifyPropertyChanged("HasMultipleRows");
                    }
                    UpdateAverageAspectRatio();
                    ArrangeImages();
                }
            }
        }

        public bool AnalyzeThumbnailsAfterLoading { get; set; }

        public int ImageCount => SortedSourceFiles.Count;

        public double Progress
        {
            get
            {
                return progress;
            }
            set
            {
                SetProperty(ref progress, value, "Progress");
            }
        }

        public bool IsReadingThumbnails
        {
            get
            {
                return isReadingThumbnails;
            }
            set
            {
                SetProperty(ref isReadingThumbnails, value, "IsReadingThumbnails");
            }
        }

        public double AverageAspectRatio
        {
            get
            {
                return averageAspectRatio;
            }
            private set
            {
                if (SetProperty(ref averageAspectRatio, value, "AverageAspectRatio"))
                {
                    ArrangeImages();
                }
            }
        }

        public StartingCorner StartingCorner
        {
            get
            {
                return startingCorner;
            }
            set
            {
                if (SetProperty(ref startingCorner, value, "StartingCorner"))
                {
                    ArrangeImages();
                    OnSettingsChanged();
                }
            }
        }

        public PrimaryDirection PrimaryDirection
        {
            get
            {
                return primaryDirection;
            }
            set
            {
                if (SetProperty(ref primaryDirection, value, "PrimaryDirection"))
                {
                    NotifyPropertyChanged("ColumnCount");
                    NotifyPropertyChanged("RowCount");
                    NotifyPropertyChanged("HasMultipleColumns");
                    NotifyPropertyChanged("HasMultipleRows");
                    ArrangeImages();
                    OnSettingsChanged();
                }
            }
        }

        public int PrimaryDirectionImageCount
        {
            get
            {
                return primaryDirectionImageCount;
            }
            set
            {
                value = ClampValue(1, ImageCount, value);
                if (SetProperty(ref primaryDirectionImageCount, value, "PrimaryDirectionImageCount"))
                {
                    NotifyPropertyChanged("SecondaryDirectionImageCount");
                    NotifyPropertyChanged("ColumnCount");
                    NotifyPropertyChanged("RowCount");
                    NotifyPropertyChanged("HasMultipleColumns");
                    NotifyPropertyChanged("HasMultipleRows");
                    ArrangeImages();
                    OnSettingsChanged();
                }
            }
        }

        public int SecondaryDirectionImageCount
        {
            get
            {
                if (ImageCount != 0)
                {
                    return (ImageCount + primaryDirectionImageCount - 1) / primaryDirectionImageCount;
                }
                return 0;
            }
        }

        public int ColumnCount
        {
            get
            {
                if (PrimaryDirection != 0)
                {
                    return SecondaryDirectionImageCount;
                }
                return PrimaryDirectionImageCount;
            }
        }

        public int RowCount
        {
            get
            {
                if (PrimaryDirection != 0)
                {
                    return PrimaryDirectionImageCount;
                }
                return SecondaryDirectionImageCount;
            }
        }

        public bool HasMultipleColumns => ColumnCount > 1;

        public bool HasMultipleRows => RowCount > 1;

        public MovementMethod MovementMethod
        {
            get
            {
                return movementMethod;
            }
            set
            {
                if (SetProperty(ref movementMethod, value, "MovementMethod"))
                {
                    ArrangeImages();
                    OnSettingsChanged();
                }
            }
        }

        public AngularRange AngularRange
        {
            get
            {
                return angularRange;
            }
            set
            {
                if (SetProperty(ref angularRange, value, "AngularRange"))
                {
                    ArrangeImages();
                    OnSettingsChanged();
                }
            }
        }

        public double MinOverlap => 1.0;

        public double MaxOverlap => 99.0;

        public double HorizontalOverlap
        {
            get
            {
                return horizontalOverlap;
            }
            set
            {
                value = ClampValue(MinOverlap, MaxOverlap, value);
                if (SetProperty(ref horizontalOverlap, value, "HorizontalOverlap"))
                {
                    if (PreviewOverlap)
                    {
                        ArrangeImages();
                    }
                    OnSettingsChanged();
                }
            }
        }

        public double VerticalOverlap
        {
            get
            {
                return verticalOverlap;
            }
            set
            {
                value = ClampValue(MinOverlap, MaxOverlap, value);
                if (SetProperty(ref verticalOverlap, value, "VerticalOverlap"))
                {
                    if (PreviewOverlap)
                    {
                        ArrangeImages();
                    }
                    OnSettingsChanged();
                }
            }
        }

        public double SeamOverlap
        {
            get
            {
                return seamOverlap;
            }
            set
            {
                value = ClampValue(MinOverlap, MaxOverlap, value);
                if (SetProperty(ref seamOverlap, value, "SeamOverlap"))
                {
                    if (PreviewOverlap)
                    {
                        ArrangeImages();
                    }
                    OnSettingsChanged();
                }
            }
        }

        public double FeatureMatchingSearchRadius
        {
            get
            {
                return featureMatchingSearchRadius;
            }
            set
            {
                value = ClampValue(0.0, 100.0, value);
                if (SetProperty(ref featureMatchingSearchRadius, value, "FeatureMatchingSearchRadius"))
                {
                    OnSettingsChanged();
                }
            }
        }

        public bool PreviewOverlap
        {
            get
            {
                return previewOverlap;
            }
            set
            {
                if (SetProperty(ref previewOverlap, value, "PreviewOverlap"))
                {
                    ArrangeImages();
                }
            }
        }

        public event EventHandler ImageArrangementChanged;

        public event EventHandler<SettingsChangedEventArgs> SettingsChanged;

        public StructuredImportViewModel(ThumbnailAnalyzerWrapper thumbnailAnalyzerWrapper)
        {
            this.thumbnailAnalyzerWrapper = thumbnailAnalyzerWrapper;
            this.thumbnailAnalyzerWrapper.ProgressChanged += ThumbnailAnalyzerWrapper_ProgressChanged;
            this.thumbnailAnalyzerWrapper.Loaded += ThumbnailAnalyzerWrapper_Loaded;
            SortedSourceFiles = new List<SourceFileViewModel>();
            startingCorner = StartingCorner.TopLeft;
            primaryDirection = PrimaryDirection.Vertical;
            movementMethod = MovementMethod.Serpentine;
            angularRange = AngularRange.Non360;
            horizontalOverlap = 25.0;
            verticalOverlap = 25.0;
            seamOverlap = 25.0;
            featureMatchingSearchRadius = 10.0;
        }

        public void ImportSettings(StructuredPanoramaInfo structuredPanoramaSettings)
        {
            AnalyzeThumbnailsAfterLoading = false;
            startingCorner = structuredPanoramaSettings.StartingCorner;
            primaryDirection = structuredPanoramaSettings.PrimaryDirection;
            primaryDirectionImageCount = structuredPanoramaSettings.PrimaryDirectionImageCount;
            movementMethod = structuredPanoramaSettings.MovementMethod;
            angularRange = structuredPanoramaSettings.AngularRange;
            horizontalOverlap = structuredPanoramaSettings.HorizontalOverlap;
            verticalOverlap = structuredPanoramaSettings.VerticalOverlap;
            seamOverlap = structuredPanoramaSettings.SeamOverlap;
            featureMatchingSearchRadius = structuredPanoramaSettings.FeatureMatchingSearchRadius;
            NotifyPropertyChanged("StartingCorner");
            NotifyPropertyChanged("PrimaryDirection");
            NotifyPropertyChanged("PrimaryDirectionImageCount");
            NotifyPropertyChanged("SecondaryDirectionImageCount");
            NotifyPropertyChanged("ColumnCount");
            NotifyPropertyChanged("RowCount");
            NotifyPropertyChanged("HasMultipleColumns");
            NotifyPropertyChanged("HasMultipleRows");
            NotifyPropertyChanged("MovementMethod");
            NotifyPropertyChanged("AngularRange");
            NotifyPropertyChanged("HorizontalOverlap");
            NotifyPropertyChanged("VerticalOverlap");
            NotifyPropertyChanged("SeamOverlap");
            NotifyPropertyChanged("FeatureMatchingSearchRadius");
            ArrangeImages();
        }

        public void EstimateLayout()
        {
            int period = 0;
            bool isSerpentine = false;
            bool startsMovingVertically = false;
            bool startsAtRight = false;
            bool startsAtBottom = false;
            float num = 0f;
            float num2 = 0f;
            thumbnailAnalyzerWrapper.AnalyzeLayout(ref period, ref isSerpentine, ref startsMovingVertically, ref startsAtRight, ref startsAtBottom, ref num, ref num2);
            if (period != 0)
            {
                primaryDirectionImageCount = period;
                movementMethod = ((!isSerpentine) ? MovementMethod.Zigzag : MovementMethod.Serpentine);
                primaryDirection = (startsMovingVertically ? PrimaryDirection.Vertical : PrimaryDirection.Horizontal);
                startingCorner = ((!startsAtRight) ? (startsAtBottom ? StartingCorner.BottomLeft : StartingCorner.TopLeft) : ((!startsAtBottom) ? StartingCorner.TopRight : StartingCorner.BottomRight));
                NotifyPropertyChanged("StartingCorner");
                NotifyPropertyChanged("PrimaryDirection");
                NotifyPropertyChanged("PrimaryDirectionImageCount");
                NotifyPropertyChanged("SecondaryDirectionImageCount");
                NotifyPropertyChanged("ColumnCount");
                NotifyPropertyChanged("RowCount");
                NotifyPropertyChanged("HasMultipleColumns");
                NotifyPropertyChanged("HasMultipleRows");
                NotifyPropertyChanged("MovementMethod");
                ArrangeImages();
                OnSettingsChanged();
            }
        }

        public void EstimateOverlap()
        {
            float num = 0f;
            float num2 = 0f;
            if (thumbnailAnalyzerWrapper.ReanalyzeOverlap(PrimaryDirectionImageCount, MovementMethod == MovementMethod.Serpentine, PrimaryDirection == PrimaryDirection.Vertical, StartingCorner == StartingCorner.TopRight || StartingCorner == StartingCorner.BottomRight, StartingCorner == StartingCorner.BottomLeft || StartingCorner == StartingCorner.BottomRight, ref num, ref num2))
            {
                horizontalOverlap = ClampValue(MinOverlap, MaxOverlap, Math.Round(100f * num));
                verticalOverlap = ClampValue(MinOverlap, MaxOverlap, Math.Round(100f * num2));
                seamOverlap = ((angularRange == AngularRange.Vertical360) ? verticalOverlap : horizontalOverlap);
                NotifyPropertyChanged("HorizontalOverlap");
                NotifyPropertyChanged("VerticalOverlap");
                NotifyPropertyChanged("SeamOverlap");
                if (PreviewOverlap)
                {
                    ArrangeImages();
                }
                OnSettingsChanged();
            }
        }

        public void UpdateProject(StitchProjectInfo projectInfo)
        {
            projectInfo.StructuredPanoramaSettings = new StructuredPanoramaInfo
            {
                StartingCorner = StartingCorner,
                PrimaryDirection = PrimaryDirection,
                PrimaryDirectionImageCount = PrimaryDirectionImageCount,
                MovementMethod = MovementMethod,
                AngularRange = AngularRange,
                HorizontalOverlap = HorizontalOverlap,
                VerticalOverlap = VerticalOverlap,
                SeamOverlap = SeamOverlap,
                FeatureMatchingSearchRadius = FeatureMatchingSearchRadius
            };
            float tolerance = (float)(2.0 * FeatureMatchingSearchRadius / 100.0);
            double imageWidth = 2.0 * Math.Min(1.0, AverageAspectRatio);
            double imageHeight = 2.0 * Math.Min(1.0, 1.0 / AverageAspectRatio);
            IEnumerable<ImageInfo> enumerable = null;
            switch (AngularRange)
            {
                case AngularRange.Horizontal360:
                    enumerable = GetImagesForHorizontal360(imageWidth, imageHeight, tolerance);
                    break;
                case AngularRange.Vertical360:
                    enumerable = GetImagesForVertical360(imageWidth, imageHeight, tolerance);
                    break;
                default:
                    enumerable = GetImagesForNon360(imageWidth, imageHeight, tolerance); 
                    break;
            };
            projectInfo.SourceImages.AddRange(enumerable);
            projectInfo.IsPoseApproximate = true;
        }

        private void ThumbnailAnalyzerWrapper_ProgressChanged(object sender, EventArgs e)
        {
            Progress = thumbnailAnalyzerWrapper.Progress;
            UpdateAverageAspectRatio();
        }

        private void ThumbnailAnalyzerWrapper_Loaded(object sender, EventArgs e)
        {
            Progress = 100.0;
            IsReadingThumbnails = false;
            thumbnailAnalyzerWrapper.CancelLoading = false;
            if (ImageCount >= 2 && AnalyzeThumbnailsAfterLoading)
            {
                AnalyzeThumbnailsAfterLoading = false;
                int period = 0;
                bool isSerpentine = false;
                bool startsMovingVertically = false;
                bool startsAtRight = false;
                bool startsAtBottom = false;
                float num = 0f;
                float num2 = 0f;
                thumbnailAnalyzerWrapper.AnalyzeLayout(ref period, ref isSerpentine, ref startsMovingVertically, ref startsAtRight, ref startsAtBottom, ref num, ref num2);
                if (period != 0)
                {
                    primaryDirectionImageCount = period;
                    movementMethod = ((!isSerpentine) ? MovementMethod.Zigzag : MovementMethod.Serpentine);
                    primaryDirection = (startsMovingVertically ? PrimaryDirection.Vertical : PrimaryDirection.Horizontal);
                    startingCorner = ((!startsAtRight) ? (startsAtBottom ? StartingCorner.BottomLeft : StartingCorner.TopLeft) : ((!startsAtBottom) ? StartingCorner.TopRight : StartingCorner.BottomRight));
                    horizontalOverlap = ClampValue(MinOverlap, MaxOverlap, Math.Round(100f * num));
                    verticalOverlap = ClampValue(MinOverlap, MaxOverlap, Math.Round(100f * num2));
                    seamOverlap = ((angularRange == AngularRange.Vertical360) ? verticalOverlap : horizontalOverlap);
                    NotifyPropertyChanged("StartingCorner");
                    NotifyPropertyChanged("PrimaryDirection");
                    NotifyPropertyChanged("PrimaryDirectionImageCount");
                    NotifyPropertyChanged("SecondaryDirectionImageCount");
                    NotifyPropertyChanged("ColumnCount");
                    NotifyPropertyChanged("RowCount");
                    NotifyPropertyChanged("HasMultipleColumns");
                    NotifyPropertyChanged("HasMultipleRows");
                    NotifyPropertyChanged("MovementMethod");
                    NotifyPropertyChanged("HorizontalOverlap");
                    NotifyPropertyChanged("VerticalOverlap");
                    NotifyPropertyChanged("SeamOverlap");
                    UpdateAverageAspectRatio();
                    ArrangeImages();
                }
            }
        }

        private void UpdateAverageAspectRatio()
        {
            double num = 0.0;
            int num2 = 0;
            foreach (SourceFileViewModel sortedSourceFile in SortedSourceFiles)
            {
                if (sortedSourceFile.Thumbnail != null)
                {
                    num += (double)sortedSourceFile.Thumbnail.PixelWidth / (double)sortedSourceFile.Thumbnail.PixelHeight;
                    num2++;
                }
            }
            AverageAspectRatio = ((num2 == 0) ? 1.0 : (num / (double)num2));
        }

        private void ArrangeImages()
        {
            if (ImageCount > 0)
            {
                int num = PrimaryDirectionImageCount;
                int secondaryDirectionImageCount = SecondaryDirectionImageCount;
                int num2;
                int num3;
                if ((PrimaryDirection == PrimaryDirection.Horizontal && (StartingCorner == StartingCorner.TopLeft || StartingCorner == StartingCorner.BottomLeft)) || (PrimaryDirection == PrimaryDirection.Vertical && (StartingCorner == StartingCorner.TopLeft || StartingCorner == StartingCorner.TopRight)))
                {
                    num2 = 0;
                    num3 = 1;
                }
                else
                {
                    num2 = num - 1;
                    num3 = -1;
                }
                int num4;
                int num5;
                if ((PrimaryDirection == PrimaryDirection.Horizontal && (StartingCorner == StartingCorner.TopLeft || StartingCorner == StartingCorner.TopRight)) || (PrimaryDirection == PrimaryDirection.Vertical && (StartingCorner == StartingCorner.TopLeft || StartingCorner == StartingCorner.BottomLeft)))
                {
                    num4 = 0;
                    num5 = 1;
                }
                else
                {
                    num4 = secondaryDirectionImageCount - 1;
                    num5 = -1;
                }
                int num6 = num2;
                int num7 = num4;
                int num8 = 0;
                for (int i = 0; i < secondaryDirectionImageCount; i++)
                {
                    for (int j = 0; j < num; j++)
                    {
                        if (num8 < ImageCount)
                        {
                            SourceFileViewModel sourceFileViewModel = SortedSourceFiles[num8];
                            sourceFileViewModel.GridColumn = ((PrimaryDirection == PrimaryDirection.Horizontal) ? num6 : num7);
                            sourceFileViewModel.GridRow = ((PrimaryDirection == PrimaryDirection.Horizontal) ? num7 : num6);
                            num8++;
                            num6 += num3;
                        }
                    }
                    num7 += num5;
                    if (MovementMethod == MovementMethod.Serpentine)
                    {
                        num3 *= -1;
                        num6 += num3;
                    }
                    else
                    {
                        num6 = num2;
                    }
                }
            }
            ImageArrangementChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnSettingsChanged()
        {
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(DirtyFlags.InitializationAndBeyond));
        }

        private IEnumerable<ImageInfo> GetImagesForNon360(double imageWidth, double imageHeight, float tolerance)
        {
            imageWidth *= 1.0 - HorizontalOverlap / 100.0;
            imageHeight *= 1.0 - VerticalOverlap / 100.0;
            List<ImageInfo> list = new List<ImageInfo>(ImageCount);
            foreach (SourceFileViewModel sortedSourceFile in SortedSourceFiles)
            {
                double translationX = (double)sortedSourceFile.GridColumn * imageWidth;
                double translationY = (double)(RowCount - 1 - sortedSourceFile.GridRow) * imageHeight;
                ImagePose pose = ImagePose.CreateImagePose(0.0, 1.0, translationX, translationY, tolerance);
                list.Add(new ImageInfo(sortedSourceFile.FilePath, pose));
            }
            return list;
        }

        private IEnumerable<ImageInfo> GetImagesForHorizontal360(double imageWidth, double imageHeight, float tolerance)
        {
            double num = imageWidth * (1.0 - HorizontalOverlap / 100.0);
            double num2 = imageWidth * (1.0 - SeamOverlap / 100.0);
            double num3 = ((double)(ColumnCount - 1) * num + num2) / (Math.PI * 2.0);
            double num4 = num / num3;
            double num5 = imageHeight * (1.0 - VerticalOverlap / 100.0);
            double num6 = num5 / num3;
            List<ImageInfo> list = new List<ImageInfo>(ImageCount);
            foreach (SourceFileViewModel sortedSourceFile in SortedSourceFiles)
            {
                double num7 = ((double)sortedSourceFile.GridColumn - 0.5 * (double)(ColumnCount - 1)) * num4;
                double num8 = ((double)sortedSourceFile.GridRow - 0.5 * (double)(RowCount - 1)) * num6;
                Quaternion cameraRotation = new Quaternion(new Vector3D(0.0, 1.0, 0.0), num7 * 180.0 / Math.PI);
                cameraRotation *= new Quaternion(new Vector3D(1.0, 0.0, 0.0), num8 * 180.0 / Math.PI);
                ImagePose pose = ImagePose.CreateImagePose(cameraRotation, num3, tolerance);
                list.Add(new ImageInfo(sortedSourceFile.FilePath, pose));
            }
            return list;
        }

        private IEnumerable<ImageInfo> GetImagesForVertical360(double imageWidth, double imageHeight, float tolerance)
        {
            double num = imageHeight * (1.0 - VerticalOverlap / 100.0);
            double num2 = imageHeight * (1.0 - SeamOverlap / 100.0);
            double num3 = ((double)(RowCount - 1) * num + num2) / (Math.PI * 2.0);
            double num4 = num / num3;
            double num5 = imageWidth * (1.0 - HorizontalOverlap / 100.0);
            double num6 = num5 / num3;
            List<ImageInfo> list = new List<ImageInfo>(ImageCount);
            foreach (SourceFileViewModel sortedSourceFile in SortedSourceFiles)
            {
                double num7 = ((double)sortedSourceFile.GridColumn - 0.5 * (double)(ColumnCount - 1)) * num6;
                double num8 = ((double)sortedSourceFile.GridRow - 0.5 * (double)(RowCount - 1)) * num4;
                Quaternion cameraRotation = new Quaternion(new Vector3D(1.0, 0.0, 0.0), num8 * 180.0 / Math.PI);
                cameraRotation *= new Quaternion(new Vector3D(0.0, 1.0, 0.0), num7 * 180.0 / Math.PI);
                ImagePose pose = ImagePose.CreateImagePose(cameraRotation, num3, tolerance);
                list.Add(new ImageInfo(sortedSourceFile.FilePath, pose));
            }
            return list;
        }

        private static int ClampValue(int valueMin, int valueMax, int value)
        {
            return Math.Max(valueMin, Math.Min(value, valueMax));
        }

        private static double ClampValue(double valueMin, double valueMax, double value)
        {
            return Math.Max(valueMin, Math.Min(value, valueMax));
        }
    }
}