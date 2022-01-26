using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shell;
using System.Windows.Threading;
using Microsoft.Research.ICE.Helpers;
using Microsoft.Research.ICE.Misc;
using Microsoft.Research.ICE.Stitching;
using Microsoft.Research.ICE.ThumbnailAnalyzer;
using Microsoft.Research.VisionTools.Toolkit;
using Microsoft.Research.ICE.Properties;

namespace Microsoft.Research.ICE.ViewModels
{
    public sealed class MainViewModel : Notifier
    {
        private const uint E_ABORT = 2147500036u;

        private const uint E_FAIL = 2147500037u;

        private const uint E_DISK_FULL = 2147942512u;

        private const uint E_BAD_FIT_MOTION_MODEL = 2818506993u;

        private const uint FACILITY_VISIONTOOLS = 2047u;

        private const string ProductName = "Image Composite Editor";

        private const string UntitledProjectFilename = "Untitled";

        private const string ErrorCannotOpenProject = "Could not open the project file.";

        private const string ErrorUnableToAnalyzeVideo = "Could not read or analyze the video file.";

        private const string ErrorNoVideoFrames = "Video analysis did not choose any frames. Please select a start time and end time that encompass a panorama in the video.";

        private const string ErrorOneVideoFrame = "Video analysis chose only one frame. Please select a start time and end time that encompass a panorama in the video.";

        private const string ErrorBadMotionModel = "Could not align any of the images using the selected camera motion. Please choose a different camera motion setting.";

        private const string ErrorNoAlignedImages = "Could not align any of the input images. Please make sure your images are taken from one location and overlap one another.";

        private const string ErrorUnableToStitch = "Could not stitch together any of the input images. Please make sure your images are taken from one location and overlap one another.";

        private const string ErrorUnableToProject = "Could not project the panorama.";

        private const string ErrorUnableToComplete = "Could not complete the panorama.";

        private const string ErrorUnableToReproject = "Could not reproject the completed panorama.";

        private const string ErrorUnableToExport = "Could not export an image.";



        private Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        private ThumbnailAnalyzerWrapper thumbnailAnalyzerWrapper;

        private StitchEngineWrapper stitchEngineWrapper;

   

    

        private DirtyFlags dirtyFlags;

        private DateTime lastProgressUpdateTime = DateTime.MinValue;

        private Stopwatch taskStopwatch = new Stopwatch();

        private Stopwatch cancellationStopwatch = new Stopwatch();

        private NavigationState navigationState;

        private string projectFilename;

        private bool isProjectDirty;

        private StitchProjectInfo projectInfo;

        private bool isVideoPanorama;

        private bool isStructuredPanorama;

        private List<SourceFileViewModel> sortedSourceFiles;

        private SortOrder sortOrder;

        private MotionModel preferredMotionModel = MotionModel.Automatic;

        private MotionModel actualMotionModel;

        private double viewPitch;

        private double viewYaw;

        private double viewRoll;

        private double xTranslation;

        private double yTranslation;

        private double zoom = 1.0;

        private bool shouldZoomToFit;

        private IntPtr compositedPanoRenderInfoPointer;

        private IntPtr projectedPanoRenderInfoPointer;

        private IntPtr croppedPanoRenderInfoPointer;

        private bool showCompletion;

        private int fullWidth;

        private int fullHeight;

        private double cropLeft;

        private double cropTop;

        private double cropRight;

        private double cropBottom;

        private TaskViewModel currentTask;

        private bool isCancelingTask;

        private string stitchStatusMessage;

        private ExportMode exportMode;

        private string exportFilename;

        public OptionsViewModel Options { get; private set; }

        public HelpViewModel Help { get; private set; }

        public WelcomeViewModel Welcome { get; private set; }

        public List<string> NavigationItems
        {
            get
            {
                List<string> list = new List<string>();
                list.Add("Import");
                list.Add("Stitch");
                list.Add("Crop");
                list.Add("Export");
                return list;
            }
        }

        public NavigationState NavigationState
        {
            get
            {
                return navigationState;
            }
            private set
            {
                if (SetProperty(ref navigationState, value, "NavigationState"))
                {
                    InvalidateNavigationCommands();
                }
            }
        }

        public string ProjectFilename
        {
            get
            {
                return projectFilename;
            }
            private set
            {
                if (SetProperty(ref projectFilename, value, "ProjectFilename"))
                {
                    NotifyPropertyChanged("WindowTitle");
                }
            }
        }

        public bool IsProjectDirty
        {
            get
            {
                return isProjectDirty;
            }
            private set
            {
                if (SetProperty(ref isProjectDirty, value, "IsProjectDirty"))
                {
                    NotifyPropertyChanged("WindowTitle");
                }
            }
        }

        private StitchProjectInfo ProjectInfo
        {
            get
            {
                return projectInfo;
            }
            set
            {
                if (SetProperty(ref projectInfo, value, "ProjectInfo"))
                {
                    NotifyPropertyChanged("Projection");
                    NotifyPropertyChanged("ViewRotation");
                    UpdateViewRotationFromQuaternion();
                }
            }
        }

        public string WindowTitle
        {
            get
            {
                string text = "Image Composite Editor";
                if (HasSomeSourceFiles)
                {
                    string path = (string.IsNullOrEmpty(ProjectFilename) ? "Untitled" : ProjectFilename);
                    text = Path.GetFileNameWithoutExtension(path) + (IsProjectDirty ? "*" : string.Empty) + " - " + text;
                }
                if (TaskProgressState == TaskbarItemProgressState.Normal)
                {
                    text = Math.Round(TaskProgress).ToString(CultureInfo.CurrentCulture) + "% - " + text;
                }
                return text;
            }
        }

        public bool IsVideoPanorama
        {
            get
            {
                return isVideoPanorama;
            }
            private set
            {
                if (SetProperty(ref isVideoPanorama, value, "IsVideoPanorama"))
                {
                    NotifyPropertyChanged("IsImagePanorama");
                    NotifyPropertyChanged("MinimumSourceFileCount");
                    NotifyPropertyChanged("HasSufficientSourceFiles");
                }
            }
        }

        public bool IsImagePanorama => !IsVideoPanorama;

        public bool IsStructuredPanorama
        {
            get
            {
                return isStructuredPanorama;
            }
            set
            {
                if (SetProperty(ref isStructuredPanorama, value, "IsStructuredPanorama"))
                {
    
                    CreateProjectInfoIfNeeded();
                    if (value)
                    {
                        StructuredImport.UpdateProject(ProjectInfo);
                    }
                    else
                    {
                        ProjectInfo.StructuredPanoramaSettings = null;
                    }
                    SetDirtyFlag(DirtyFlags.InitializationAndBeyond);
                    IsProjectDirty = true;
                }
            }
        }

        private List<SourceFileViewModel> UnsortedSourceFiles { get; set; }

        public List<SourceFileViewModel> SortedSourceFiles
        {
            get
            {
                return sortedSourceFiles;
            }
            private set
            {
                SetProperty(ref sortedSourceFiles, value, "SortedSourceFiles");
            }
        }

        public SortOrder SortOrder
        {
            get
            {
                return sortOrder;
            }
            set
            {
                if (SetProperty(ref sortOrder, value, "SortOrder"))
                {

                    UpdateSortedSourceFiles();
                }
            }
        }

        public int MinimumSourceFileCount
        {
            get
            {
                if (!IsVideoPanorama)
                {
                    return 2;
                }
                return 1;
            }
        }

        public bool HasSufficientSourceFiles => UnsortedSourceFiles.Count >= MinimumSourceFileCount;

        public bool HasSomeSourceFiles => UnsortedSourceFiles.Count > 0;

        public bool HasNoSourceFiles => UnsortedSourceFiles.Count == 0;

        public MotionModel PreferredMotionModel
        {
            get
            {
                return preferredMotionModel;
            }
            set
            {
                if (SetProperty(ref preferredMotionModel, value, "PreferredMotionModel"))
                {
                    stitchEngineWrapper.PreferredMotionModel = value;
                    Projection = MapType.Unknown;
                    ViewRotation = Quaternion.Identity;
                    SetDirtyFlag(DirtyFlags.AlignmentAndBeyond);
                    IsProjectDirty = true;
                }
            }
        }

        public MotionModel ActualMotionModel
        {
            get
            {
                return actualMotionModel;
            }
            private set
            {
                if (SetProperty(ref actualMotionModel, value, "ActualMotionModel"))
                {

                    UpdateExportMode();
                    NotifyPropertyChanged("CanExportTileset");
                    NotifyPropertyChanged("ExportTilesetDisabledMessage");
                }
            }
        }

        public bool CanExportTileset
        {
            get
            {
                if (ActualMotionModel == MotionModel.Rotation3D && Projection != MapType.Cylindrical && Projection != MapType.TransverseCylindrical && Projection != MapType.Spherical && Projection != MapType.TransverseSpherical)
                {
                    return Projection == MapType.Perspective;
                }
                return true;
            }
        }

        public string ExportTilesetDisabledMessage
        {
            get
            {
                if (!CanExportTileset)
                {
                    return "Deep Zoom export is only supported for\ncylindrical, spherical, and perspective projections.";
                }
                return null;
            }
        }

    
        public StructuredImportViewModel StructuredImport { get; private set; }

        public VideoImportViewModel VideoImport { get; private set; }

        public MapType Projection
        {
            get
            {
                if (ProjectInfo == null)
                {
                    return MapType.Unknown;
                }
                return ProjectInfo.Projection;
            }
            set
            {
                if (Projection != value)
                {

                    MapType mapType3 = (stitchEngineWrapper.Projection = (ProjectInfo.Projection = value));
                    ProjectInfo.ViewRotation = stitchEngineWrapper.Rotation;
                    NotifyPropertyChanged("ViewRotation");
                    NotifyPropertyChanged("CanExportTileset");
                    NotifyPropertyChanged("ExportTilesetDisabledMessage");

                    UpdateExportMode();
                    InvalidateProjection();
                    IsProjectDirty = true;
                    NotifyPropertyChanged("Projection");
                }
            }
        }

        public Quaternion ViewRotation
        {
            get
            {
                if (ProjectInfo == null)
                {
                    return Quaternion.Identity;
                }
                return ProjectInfo.ViewRotation;
            }
            set
            {
                if (ViewRotation != value)
                {
                    Quaternion quaternion3 = (stitchEngineWrapper.Rotation = (ProjectInfo.ViewRotation = value));
                    InvalidateProjection();
                    IsProjectDirty = true;
                    NotifyPropertyChanged("ViewRotation");
                    UpdateViewRotationFromQuaternion();
                }
            }
        }

        public double ViewPitch
        {
            get
            {
                return viewPitch;
            }
            set
            {
                if (!double.IsNaN(value) && SetProperty(ref viewPitch, value, "ViewPitch"))
                {
                    
                    UpdateViewRotationFromEulerAngles();
                }
            }
        }

        public double ViewYaw
        {
            get
            {
                return viewYaw;
            }
            set
            {
                if (!double.IsNaN(value) && SetProperty(ref viewYaw, value, "ViewYaw"))
                {
                    
                    UpdateViewRotationFromEulerAngles();
                }
            }
        }

        public double ViewRoll
        {
            get
            {
                return viewRoll;
            }
            set
            {
                if (!double.IsNaN(value) && SetProperty(ref viewRoll, value, "ViewRoll"))
                {
                    
                    UpdateViewRotationFromEulerAngles();
                }
            }
        }

        public double XTranslation
        {
            get
            {
                return xTranslation;
            }
            set
            {
                SetProperty(ref xTranslation, value, "XTranslation");
            }
        }

        public double YTranslation
        {
            get
            {
                return yTranslation;
            }
            set
            {
                SetProperty(ref yTranslation, value, "YTranslation");
            }
        }

        public double Zoom
        {
            get
            {
                return zoom;
            }
            set
            {
                SetProperty(ref zoom, value, "Zoom");
            }
        }

        public bool ShouldZoomToFit
        {
            get
            {
                return shouldZoomToFit;
            }
            set
            {
                SetProperty(ref shouldZoomToFit, value, "ShouldZoomToFit");
            }
        }

        public IntPtr CompositedPanoRenderInfoPointer
        {
            get
            {
                return compositedPanoRenderInfoPointer;
            }
            private set
            {
                SetProperty(ref compositedPanoRenderInfoPointer, value, "CompositedPanoRenderInfoPointer");
            }
        }

        public IntPtr ProjectedPanoRenderInfoPointer
        {
            get
            {
                return projectedPanoRenderInfoPointer;
            }
            private set
            {
                SetProperty(ref projectedPanoRenderInfoPointer, value, "ProjectedPanoRenderInfoPointer");
            }
        }

        public IntPtr CroppedPanoRenderInfoPointer
        {
            get
            {
                return croppedPanoRenderInfoPointer;
            }
            private set
            {
                SetProperty(ref croppedPanoRenderInfoPointer, value, "CroppedPanoRenderInfoPointer");
            }
        }

        public bool ShowCompletion
        {
            get
            {
                return showCompletion;
            }
            set
            {
                if (SetProperty(ref showCompletion, value, "ShowCompletion"))
                {

                }
            }
        }

        public bool CanShowCompletion => !IsDirty(DirtyFlags.Completion);

        public int FullWidth
        {
            get
            {
                return fullWidth;
            }
            private set
            {
                SetProperty(ref fullWidth, value, "FullWidth");
            }
        }

        public int FullHeight
        {
            get
            {
                return fullHeight;
            }
            private set
            {
                SetProperty(ref fullHeight, value, "FullHeight");
            }
        }

        public double CropLeft
        {
            get
            {
                return cropLeft;
            }
            set
            {
                value = Math.Round(value).Clamp(0.0, (double)FullWidth - CropRight - 1.0);
                if (!double.IsNaN(value) && SetProperty(ref cropLeft, value, "CropLeft"))
                {
                    NotifyPropertyChanged("CropWidth");
                    UpdateCropRect();
                }
            }
        }

        public double CropTop
        {
            get
            {
                return cropTop;
            }
            set
            {
                value = Math.Round(value).Clamp(0.0, (double)FullHeight - CropBottom - 1.0);
                if (!double.IsNaN(value) && SetProperty(ref cropTop, value, "CropTop"))
                {
                    NotifyPropertyChanged("CropHeight");
                    UpdateCropRect();
                }
            }
        }

        public double CropRight
        {
            get
            {
                return cropRight;
            }
            set
            {
                value = Math.Round(value).Clamp(0.0, (double)FullWidth - CropLeft - 1.0);
                if (!double.IsNaN(value) && SetProperty(ref cropRight, value, "CropRight"))
                {
                    NotifyPropertyChanged("CropRightPosition");
                    NotifyPropertyChanged("CropWidth");
                    UpdateCropRect();
                }
            }
        }

        public double CropBottom
        {
            get
            {
                return cropBottom;
            }
            set
            {
                value = Math.Round(value).Clamp(0.0, (double)FullHeight - CropTop - 1.0);
                if (!double.IsNaN(value) && SetProperty(ref cropBottom, value, "CropBottom"))
                {
                    NotifyPropertyChanged("CropBottomPosition");
                    NotifyPropertyChanged("CropHeight");
                    UpdateCropRect();
                }
            }
        }

        public double CropRightPosition
        {
            get
            {
                return (double)FullWidth - CropRight;
            }
            set
            {
                CropRight = (double)FullWidth - value;
            }
        }

        public double CropBottomPosition
        {
            get
            {
                return (double)FullHeight - CropBottom;
            }
            set
            {
                CropBottom = (double)FullHeight - value;
            }
        }

        public double CropWidth
        {
            get
            {
                return (double)FullWidth - CropLeft - CropRight;
            }
            set
            {
                value = Math.Round(value).Clamp(1.0, FullWidth);
                if (!double.IsNaN(value) && CropWidth != value)
                {
                    double num = (double)FullWidth - CropLeft - value - CropRight;
                    cropLeft = Math.Round(CropLeft + num / 2.0).Clamp(0.0, FullWidth - 1);
                    cropRight = ((double)FullWidth - CropLeft - value).Clamp(0.0, (double)FullWidth - CropLeft - 1.0);
                    NotifyPropertyChanged("CropLeft");
                    NotifyPropertyChanged("CropRight");
                    NotifyPropertyChanged("CropRightPosition");
                    NotifyPropertyChanged("CropWidth");
                    UpdateCropRect();
                }
            }
        }

        public double CropHeight
        {
            get
            {
                return (double)FullHeight - CropTop - CropBottom;
            }
            set
            {
                value = Math.Round(value).Clamp(1.0, FullHeight);
                if (!double.IsNaN(value) && CropHeight != value)
                {
                    double num = (double)FullHeight - CropTop - value - CropBottom;
                    cropTop = Math.Round(CropTop + num / 2.0).Clamp(0.0, FullHeight - 1);
                    cropBottom = ((double)FullHeight - CropTop - value).Clamp(0.0, (double)FullHeight - CropTop - 1.0);
                    NotifyPropertyChanged("CropTop");
                    NotifyPropertyChanged("CropBottom");
                    NotifyPropertyChanged("CropBottomPosition");
                    NotifyPropertyChanged("CropHeight");
                    UpdateCropRect();
                }
            }
        }

        public ObservableCollection<TaskViewModel> Tasks { get; private set; }

        public TaskViewModel CurrentTask
        {
            get
            {
                return currentTask;
            }
            private set
            {
                TaskViewModel taskViewModel = currentTask;
                if (SetProperty(ref currentTask, value, "CurrentTask"))
                {
                    if (taskViewModel != null)
                    {
                        taskViewModel.TaskState = ((taskViewModel.Progress == 100.0) ? TaskState.Completed : TaskState.NotStarted);
                        taskViewModel.PropertyChanged -= CurrentTask_PropertyChanged;
                    }
                    if (currentTask != null)
                    {
                        currentTask.TaskState = TaskState.InProgress;
                        currentTask.PropertyChanged += CurrentTask_PropertyChanged;
                    }
                    NotifyPropertyChanged("HasTask");
                    NotifyPropertyChanged("WindowTitle");
                    NotifyPropertyChanged("TaskProgress");
                }
            }
        }

        public bool HasTask => CurrentTask != null;

        public double TaskProgress
        {
            get
            {
                if (Tasks.Count <= 0)
                {
                    return 0.0;
                }
                return Tasks.Average((TaskViewModel task) => task.Progress);
            }
        }

        public bool IsCancelingTask
        {
            get
            {
                return isCancelingTask;
            }
            private set
            {
                SetProperty(ref isCancelingTask, value, "IsCancelingTask");
            }
        }

        public TaskbarItemProgressState TaskProgressState
        {
            get
            {
                if (Tasks.Count != 0)
                {
                    if (!Tasks.Any((TaskViewModel task) => task.IsProgressIndeterminate))
                    {
                        return TaskbarItemProgressState.Normal;
                    }
                    return TaskbarItemProgressState.Indeterminate;
                }
                return TaskbarItemProgressState.None;
            }
        }

        public string StitchStatusMessage
        {
            get
            {
                return stitchStatusMessage;
            }
            private set
            {
                if (SetProperty(ref stitchStatusMessage, value, "StitchStatusMessage"))
                {
                    
                }
            }
        }

        public ExportMode ExportMode
        {
            get
            {
                return exportMode;
            }
            set
            {
                if (SetProperty(ref exportMode, value, "ExportMode"))
                {

                    NotifyPropertyChanged("CurrentExport");
                }
            }
        }



        public TilesetExportViewModel TilesetExport { get; private set; }

        public ImageExportViewModel ImageExport { get; private set; }

        public ExportViewModel CurrentExport
        {
            get
            {
                if (ExportMode != ExportMode.Tileset)
                {
                    return ImageExport;
                }
                return TilesetExport;
            }
        }

        public string DefaultExportFilename
        {
            get
            {
                if (stitchEngineWrapper.ImageCount == 0)
                {
                    return null;
                }
                string filename = stitchEngineWrapper.GetFilename(0);
                return Path.GetFileNameWithoutExtension(filename) + "_stitch." + CurrentExport.DefaultFileExtension;
            }
        }

        public string ExportFilename
        {
            get
            {
                return exportFilename;
            }
            set
            {
                SetProperty(ref exportFilename, value, "ExportFilename");
            }
        }

        public ICommand AutoOrientationCommand { get; private set; }

        public ICommand AutoCompleteCommand { get; private set; }

        public ICommand AutoCropCommand { get; private set; }

        public ICommand NoCropCommand { get; private set; }

        public ICommand GoToStateCommand { get; private set; }

        public ICommand GoBackCommand { get; private set; }

        public ICommand GoToNextCommand { get; private set; }

        public ICommand CancelTaskCommand { get; private set; }

        public bool ShouldSaveBeforeGoingBack
        {
            get
            {
                if (NavigationState == NavigationState.Import)
                {
                    return IsProjectDirty;
                }
                return false;
            }
        }

        public event EventHandler<ErrorEventArgs> ErrorOccurred;

        public event EventHandler<ExportCompletedEventArgs> ExportCompleted;

        private bool IsValidPath(string path, bool allowRelativePaths = false)
        {
            bool isValid = true;

            try
            {
                string fullPath = Path.GetFullPath(path);

                if (allowRelativePaths)
                {
                    isValid = Path.IsPathRooted(path);
                }
                else
                {
                    string root = Path.GetPathRoot(path);
                    isValid = string.IsNullOrEmpty(root.Trim(new char[] { '\\', '/' })) == false;
                }
            }
            catch 
            {
                isValid = false;
            }

            return isValid;
        }

        public MainViewModel()
        {
            if (!Settings.Default.HasCheckedForImageCacheEnvironmentVariable)
            {
                string environmentVariable = Environment.GetEnvironmentVariable("ICE_CACHE_LOCATION");
                if (environmentVariable != null)
                {
                    Settings.Default.ImageCacheLocations = environmentVariable;
                }
                Settings.Default.HasCheckedForImageCacheEnvironmentVariable = true;
                Settings.Default.Save();
            }
            Options = new OptionsViewModel();


            foreach(var path in Settings.Default.ImageCacheLocations.Split('|'))
            {

                if(IsValidPath(path))
                    Options.AddImageCacheLocation(path);
  
                
            }

            Options.MemoryConsumptionLimit = ((Settings.Default.MemoryConsumptionLimit == 0) ? Options.DefaultMemoryConsumptionLimit : Settings.Default.MemoryConsumptionLimit);
            Options.CheckForUpdatesAtStartup = Settings.Default.CheckForUpdatesAtStartup;
            Help = new HelpViewModel();
            Welcome = new WelcomeViewModel();
            thumbnailAnalyzerWrapper = new ThumbnailAnalyzerWrapper();
            thumbnailAnalyzerWrapper.ProgressChanged += ThumbnailAnalyzerWrapper_ProgressChanged;
            thumbnailAnalyzerWrapper.Loaded += ThumbnailAnalyzerWrapper_Loaded;
            stitchEngineWrapper = new StitchEngineWrapper();
            stitchEngineWrapper.ProgressChanged += StitchEngineWrapper_ProgressChanged;
            stitchEngineWrapper.TaskCompleted += StitchEngineWrapper_TaskCompleted;
            UpdateImageCacheSettings();
            UnsortedSourceFiles = new List<SourceFileViewModel>();
            SortedSourceFiles = new List<SourceFileViewModel>();
            StructuredImport = new StructuredImportViewModel(thumbnailAnalyzerWrapper);
            StructuredImport.SettingsChanged += Import_SettingsChanged;
            VideoImport = new VideoImportViewModel();
            VideoImport.SettingsChanged += Import_SettingsChanged;
            Tasks = new ObservableCollection<TaskViewModel>();
            Tasks.CollectionChanged += Tasks_CollectionChanged;
            TilesetExport = new TilesetExportViewModel();
            ImageExport = new ImageExportViewModel();
            AutoOrientationCommand = new Command(AutoOrientation);
            AutoCompleteCommand = new Command(AutoComplete, CanAutoComplete);
            AutoCropCommand = new Command(AutoCrop);
            NoCropCommand = new Command(NoCrop);
            GoToStateCommand = new Command<string>(GoToState, CanGoToState);
            GoBackCommand = new Command(GoBack, CanGoBack);
            GoToNextCommand = new Command(GoToNext, CanGoToNext);
            CancelTaskCommand = new Command(CancelTask);
        }

        ~MainViewModel()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (thumbnailAnalyzerWrapper != null)
                {
                    thumbnailAnalyzerWrapper.ProgressChanged -= ThumbnailAnalyzerWrapper_ProgressChanged;
                    thumbnailAnalyzerWrapper.Loaded -= ThumbnailAnalyzerWrapper_Loaded;
                    thumbnailAnalyzerWrapper.Dispose();
                    thumbnailAnalyzerWrapper = null;
                }
                if (stitchEngineWrapper != null)
                {
                    stitchEngineWrapper.ProgressChanged -= StitchEngineWrapper_ProgressChanged;
                    stitchEngineWrapper.TaskCompleted -= StitchEngineWrapper_TaskCompleted;
                    stitchEngineWrapper.Dispose();
                    stitchEngineWrapper = null;
                }
            }
        }

        private void CurrentTask_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Progress")
            {
                NotifyPropertyChanged("WindowTitle");
                NotifyPropertyChanged("TaskProgress");
            }
        }


        public void UpdateImageCacheSettings()
        {
            ulong imageCacheMemoryLimit = (ulong)((long)Options.MemoryConsumptionLimit * 1024L * 1024);
            stitchEngineWrapper.SetImageCacheMemoryLimit(imageCacheMemoryLimit);
            if (Options.ImageCacheLocations.All((CacheLocationViewModel c) => c.IsEditable))
            {
                string[] imageCacheLocations = Options.ImageCacheLocations.Select((CacheLocationViewModel c) => c.ExpandedPath).ToArray();
                stitchEngineWrapper.ClearImageCacheLocations();
                stitchEngineWrapper.AddImageCacheLocations(imageCacheLocations);
                return;
            }
            string[] imageCacheLocations2 = (from c in Options.ImageCacheLocations
                                             where c.IsEditable
                                             select c.ExpandedPath).ToArray();
            stitchEngineWrapper.AddImageCacheLocations(imageCacheLocations2);
        }

        public void NewProject()
        {
            UnsortedSourceFiles.Clear();
            sortOrder = SortOrder.Filename;
            UpdateSortedSourceFiles();
            ProjectFilename = null;
            IsVideoPanorama = false;
            IsStructuredPanorama = false;
            PreferredMotionModel = MotionModel.Automatic;
            Projection = MapType.Unknown;
            ViewRotation = Quaternion.Identity;
            XTranslation = 0.0;
            YTranslation = 0.0;
            Zoom = 1.0;
            CompositedPanoRenderInfoPointer = IntPtr.Zero;
            ProjectedPanoRenderInfoPointer = IntPtr.Zero;
            CroppedPanoRenderInfoPointer = IntPtr.Zero;
            ShowCompletion = false;
            Tasks.Clear();
            CurrentTask = null;
            SetDirtyFlag(DirtyFlags.InitializationAndBeyond);
            IsProjectDirty = false;
        }

        public void OpenProject(string projectFilename)
        {
            NewProject();
            StitchProjectInfo stitchProjectInfo;
            try
            {
                stitchProjectInfo = StitchProjectFile.Load(projectFilename);
            }
            catch (Exception ex)
            {
                
                OnError("Could not open the project file." + Environment.NewLine + ex.Message);
                return;
            }
            if (!stitchEngineWrapper.InitializeFromProjectInfo(stitchProjectInfo))
            {
                OnError("Could not open the project file.");
                return;
            }
            VideoInfo videoInfo = stitchProjectInfo.SourceVideos.FirstOrDefault();
            if (videoInfo != null)
            {
                ImportVideo(videoInfo, isNewProject: false);
                if (videoInfo.RequiredFrames.Any((VideoFrameInfo f) => f.IsAutoSelected))
                {
                    ClearDirtyFlag(DirtyFlags.VideoFrameSelection);
                }
            }
            else
            {
                IEnumerable<string> filenames = stitchProjectInfo.SourceImages.Select((ImageInfo sourceImage) => sourceImage.FilePath);
                ImportImages(filenames);
                if (stitchProjectInfo.StructuredPanoramaSettings != null)
                {
                    StructuredImport.ImportSettings(stitchProjectInfo.StructuredPanoramaSettings);
                    isStructuredPanorama = true;
                    NotifyPropertyChanged("IsStructuredPanorama");
                }
            }
            MotionModel motionModel = (preferredMotionModel = (stitchEngineWrapper.PreferredMotionModel = stitchProjectInfo.CameraMotion));
            NotifyPropertyChanged("PreferredMotionModel");
            ActualMotionModel = stitchEngineWrapper.ActualMotionModel;
            ClearDirtyFlag(DirtyFlags.Initialization);
            if (stitchEngineWrapper.AlignedCount > 0)
            {
                ClearDirtyFlag(DirtyFlags.Alignment);
            }
            ProjectFilename = projectFilename;
            ProjectInfo = stitchProjectInfo;
            IsProjectDirty = false;
        }

        public bool SaveProject(string projectFilename)
        {
            try
            {
                CreateProjectInfoIfNeeded();
                StitchProjectFile.Save(projectFilename, ProjectInfo);
                ProjectFilename = projectFilename;
                IsProjectDirty = false;
                return true;
            }
            catch
            {
                
                return false;
            }
        }

        public void NewProjectFromImages(IEnumerable<string> filenames)
        {
            NewProject();
            ImportImages(filenames);
        }

        public void ImportImages(IEnumerable<string> filenames)
        {
            if (IsVideoPanorama)
            {
                UnsortedSourceFiles.Clear();
                IsVideoPanorama = false;
            }
            StructuredImport.AnalyzeThumbnailsAfterLoading = UnsortedSourceFiles.Count == 0;
            foreach (string filename in filenames)
            {
                UnsortedSourceFiles.Add(new SourceFileViewModel(filename));
            }
            UpdateSortedSourceFiles();
        }

        public void RemoveImages(IEnumerable<SourceFileViewModel> sourceFiles)
        {
            foreach (SourceFileViewModel sourceFile in sourceFiles)
            {
                UnsortedSourceFiles.Remove(sourceFile);
                sourceFile.Thumbnail = null;
            }
            UpdateSortedSourceFiles();
        }

        public void NewProjectFromVideo(string filename)
        {
            NewProject();
            ImportVideo(new VideoInfo(filename, -1.0, -1.0, 0, 0, new VideoFrameInfo[0]), isNewProject: true);
        }

        public void ExportImage(string filename)
        {
            ExportFilename = filename;
            AddTasks(TaskPurpose.Export);
        }


        public void CancelTasksAndWait()
        {
            stitchEngineWrapper.CancelTasksAndWait();
        }

        private bool IsDirty(DirtyFlags value)
        {
            return dirtyFlags.HasFlag(value);
        }

        private void SetDirtyFlag(DirtyFlags value)
        {
            dirtyFlags |= value;
            NotifyPropertyChanged("CanShowCompletion");
            ((Command)AutoCompleteCommand).RaiseCanExecuteChanged();
        }

        private void ClearDirtyFlag(DirtyFlags value)
        {
            dirtyFlags &= ~value;
            NotifyPropertyChanged("CanShowCompletion");
            ((Command)AutoCompleteCommand).RaiseCanExecuteChanged();
        }

        private void Import_SettingsChanged(object sender, SettingsChangedEventArgs e)
        {
            SetDirtyFlag(DirtyFlags.Initialization | e.DirtyFlags);
            IsProjectDirty = true;
        }

        private void ImportVideo(VideoInfo videoInfo, bool isNewProject)
        {
            UnsortedSourceFiles.Clear();
            VideoImport.ImportVideo(videoInfo, isNewProject);
            if (VideoImport.ErrorMessage != null)
            {
                OnError(VideoImport.ErrorMessage);
                return;
            }
            IsVideoPanorama = true;
            UnsortedSourceFiles.Add(new SourceFileViewModel(videoInfo.FilePath));
            UpdateSortedSourceFiles();
        }

        private void ThumbnailAnalyzerWrapper_ProgressChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < SortedSourceFiles.Count; i++)
            {
                if (SortedSourceFiles[i].Thumbnail == null)
                {
                    IntPtr thumbnail = thumbnailAnalyzerWrapper.GetThumbnail(i);
                    if (thumbnail != IntPtr.Zero)
                    {
                        IntSize thumbnailSize = thumbnailAnalyzerWrapper.GetThumbnailSize(i);
                        int num = 4 * thumbnailSize.Width;
                        BitmapSource bitmapSource = BitmapSource.Create(thumbnailSize.Width, thumbnailSize.Height, 96.0, 96.0, PixelFormats.Bgra32, null, thumbnail, num * thumbnailSize.Height, num);
                        bitmapSource.Freeze();
                        SortedSourceFiles[i].Thumbnail = bitmapSource;
                    }
                }
            }
        }

        private void ThumbnailAnalyzerWrapper_Loaded(object sender, EventArgs e)
        {
        }

        private void CancelTask()
        {
            if (CurrentTask != null)
            {
                
                cancellationStopwatch.Restart();
                IsCancelingTask = true;
                switch (CurrentTask.TaskPurpose)
                {
                    case TaskPurpose.CheckForUpgrade:
                        break;
                    case TaskPurpose.SelectVideoFrames:
                    case TaskPurpose.Align:
                    case TaskPurpose.Composite:
                    case TaskPurpose.Project:
                    case TaskPurpose.Complete:
                    case TaskPurpose.Reproject:
                    case TaskPurpose.Export:
                        stitchEngineWrapper.CancelTask = true;
                        break;
                }
            }
        }

        private void UpdateSortedSourceFiles()
        {
            if (SortOrder == SortOrder.Filename)
            {
                SortedSourceFiles = UnsortedSourceFiles.OrderBy((SourceFileViewModel sourceFile) => sourceFile.Name).ToList();
            }
            else
            {
                SortedSourceFiles = (from sourceFile in UnsortedSourceFiles
                                     orderby sourceFile.CaptureTime, sourceFile.Name
                                     select sourceFile).ToList();
            }
            StructuredImport.SortedSourceFiles = SortedSourceFiles;
            NotifyPropertyChanged("HasNoSourceFiles");
            NotifyPropertyChanged("HasSomeSourceFiles");
            NotifyPropertyChanged("HasSufficientSourceFiles");
            NotifyPropertyChanged("WindowTitle");
            SetDirtyFlag(DirtyFlags.InitializationAndBeyond);
            IsProjectDirty = HasSomeSourceFiles;
            if (HasNoSourceFiles)
            {
                IsStructuredPanorama = false;
                IsVideoPanorama = false;
            }
            NavigationState = (HasSomeSourceFiles ? NavigationState.Import : NavigationState.Welcome);
            InvalidateNavigationCommands();
            thumbnailAnalyzerWrapper.Initialize(SortedSourceFiles.Select((SourceFileViewModel sourceFile) => sourceFile.FilePath));
            thumbnailAnalyzerWrapper.StartLoading();
        }

        private void UpdateExportMode()
        {
            if (!CanExportTileset && ExportMode == ExportMode.Tileset)
            {
                ExportMode = ExportMode.Image;
            }

        }

        private void UpdateViewRotationFromQuaternion()
        {
            Vector3D vector3D = ViewRotation.ToEulerAngles();
            viewPitch = vector3D.X;
            viewYaw = vector3D.Y;
            viewRoll = vector3D.Z;
            NotifyPropertyChanged("ViewPitch");
            NotifyPropertyChanged("ViewYaw");
            NotifyPropertyChanged("ViewRoll");
        }

        private void UpdateViewRotationFromEulerAngles()
        {
            Vector3D eulerAngles = new Vector3D(viewPitch, viewYaw, viewRoll);
            ViewRotation = eulerAngles.ToQuaternion();
        }

        private void Tasks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("WindowTitle");
            NotifyPropertyChanged("TaskProgressState");
        }

        private void AutoOrientation()
        {
            
            ViewRotation = stitchEngineWrapper.DefaultRotation;
        }

        private void AutoComplete()
        {
            if (CanAutoComplete())
            {
                AddTasks(TaskPurpose.Complete);
            }
        }

        private bool CanAutoComplete()
        {
            if (IsDirty(DirtyFlags.Completion))
            {
                return !IsDirty(DirtyFlags.Compositing);
            }
            return false;
        }

        private void AutoCrop()
        {
            
            Int32Rect cropRectangle = (ShowCompletion ? ((Int32Rect)(object)stitchEngineWrapper.ResetCropRect) : ((Int32Rect)(object)stitchEngineWrapper.AutomaticCropRect));
            SetCropRectangle(cropRectangle);
        }

        private void NoCrop()
        {
            
            SetCropRectangle((Int32Rect)(object)stitchEngineWrapper.ResetCropRect);
        }

        private bool CanGoToState(string stateName)
        {
            if (Enum.TryParse<NavigationState>(stateName, out var result))
            {
                return CanGoToState(result);
            }
            return false;
        }

        private bool CanGoToState(NavigationState state)
        {
            switch (state)
            {
                case NavigationState.Welcome:
                    return true;
                case NavigationState.Import:
                    return HasSomeSourceFiles;
                case NavigationState.Stitch:
                case NavigationState.Crop:
                case NavigationState.Export:
                    return HasSufficientSourceFiles;
                default:
                    return false;
            }
        }

        private void GoToState(string stateName)
        {
            if (Enum.TryParse<NavigationState>(stateName, out var result))
            {
                GoToState(result);
            }
        }

        private void GoToState(NavigationState state)
        {
            if (!CanGoToState(state) || navigationState == state)
            {
                return;
            }
            NavigationState = state;
            if (state == NavigationState.Welcome)
            {
                NewProject();
            }
            List<TaskPurpose> list = new List<TaskPurpose>();
            if (NavigationState >= NavigationState.Stitch)
            {
                if (IsDirty(DirtyFlags.VideoFrameSelection) && IsVideoPanorama)
                {
                    list.Add(TaskPurpose.SelectVideoFrames);
                }
                if (IsDirty(DirtyFlags.Alignment))
                {
                    list.Add(TaskPurpose.Align);
                }
                if (IsDirty(DirtyFlags.Compositing))
                {
                    list.Add(TaskPurpose.Composite);
                }
            }
            if (NavigationState >= NavigationState.Crop && IsDirty(DirtyFlags.Projection))
            {
                list.Add(TaskPurpose.Project);
            }
            if (ShowCompletion && NavigationState >= NavigationState.Crop && IsDirty(DirtyFlags.Completion))
            {
                list.Add(TaskPurpose.Complete);
            }
            if (list.Count > 0)
            {
                AddTasks(list.ToArray());
            }
        }

        public bool CanGoBack()
        {
            return CanGoToState(NavigationState - 1);
        }

        public void GoBack()
        {
            GoToState(NavigationState - 1);
        }

        public bool CanGoToNext()
        {
            return CanGoToState(NavigationState + 1);
        }

        public void GoToNext()
        {
            GoToState(NavigationState + 1);
        }

        private void InvalidateNavigationCommands()
        {
            ((Command<string>)GoToStateCommand).RaiseCanExecuteChanged();
            ((Command)GoBackCommand).RaiseCanExecuteChanged();
            ((Command)GoToNextCommand).RaiseCanExecuteChanged();
        }

        private void AddTasks(params TaskPurpose[] taskPurposes)
        {
            foreach (TaskPurpose taskPurpose in taskPurposes)
            {
                ObservableCollection<TaskViewModel> tasks = Tasks;
                Func<TaskViewModel, bool> predicate = (TaskViewModel t) => t.TaskPurpose == taskPurpose;
                if (!tasks.Any(predicate))
                {
                    Tasks.Add(new TaskViewModel(taskPurpose));
                }
            }
            if (CurrentTask == null)
            {
                StartNextTask();
            }
        }

        private void StartNextTask()
        {
            TaskViewModel taskViewModel = Tasks.FirstOrDefault((TaskViewModel t) => t.TaskState == TaskState.NotStarted);
            if (taskViewModel != null)
            {
                
                taskStopwatch.Restart();
                CurrentTask = taskViewModel;
                switch (taskViewModel.TaskPurpose)
                {
                    case TaskPurpose.SelectVideoFrames:
                        StartSelectingVideoFrames();
                        break;
                    case TaskPurpose.Align:
                        StartAligning();
                        break;
                    case TaskPurpose.Composite:
                        StartCompositing();
                        break;
                    case TaskPurpose.Project:
                        StartProjecting();
                        break;
                    case TaskPurpose.Complete:
                        StartCompleting();
                        break;
                    case TaskPurpose.Reproject:
                        StartReprojecting();
                        break;
                    case TaskPurpose.Export:
                        StartExporting();
                        break;
                }
            }
            else
            {
                Tasks.Clear();
                CurrentTask = null;
            }
        }

        private bool EnsureStitchEngineIsInitialized()
        {
            bool flag = true;
            if (CreateProjectInfoIfNeeded())
            {
                flag = stitchEngineWrapper.InitializeFromProjectInfo(ProjectInfo);
                if (flag)
                {
                    ClearDirtyFlag(DirtyFlags.Initialization);
                }
            }
            return flag;
        }

        private bool CreateProjectInfoIfNeeded()
        {
            if (IsDirty(DirtyFlags.Initialization))
            {
                ProjectInfo = new StitchProjectInfo
                {
                    CameraMotion = PreferredMotionModel,
                    ViewRotation = ViewRotation
                };
                if (IsVideoPanorama)
                {
                    VideoImport.UpdateProject(ProjectInfo);
                }
                else if (IsStructuredPanorama)
                {
                    StructuredImport.UpdateProject(ProjectInfo);
                }
                else
                {
                    ProjectInfo.SourceImages.AddRange(SortedSourceFiles.Select((SourceFileViewModel sourceFile) => new ImageInfo(sourceFile.FilePath, null)));
                }
                return true;
            }
            return false;
        }

        private void StartSelectingVideoFrames()
        {
            bool flag = EnsureStitchEngineIsInitialized();
            if (flag)
            {
                flag = stitchEngineWrapper.StartSelectingVideoFrames();
            }
            if (!flag)
            {
                TaskCanceled();
                OnError("Could not read or analyze the video file.");
            }
        }

        private void StartAligning()
        {
            CompositedPanoRenderInfoPointer = IntPtr.Zero;
            ProjectedPanoRenderInfoPointer = IntPtr.Zero;
            CroppedPanoRenderInfoPointer = IntPtr.Zero;
            bool flag = EnsureStitchEngineIsInitialized();
            if (flag)
            {
                flag = stitchEngineWrapper.StartAligning();
            }
            if (!flag)
            {
                TaskCanceled();
                OnError("Could not stitch together any of the input images. Please make sure your images are taken from one location and overlap one another.");
            }
        }

        private void StartCompositing()
        {
            CompositedPanoRenderInfoPointer = IntPtr.Zero;
            ProjectedPanoRenderInfoPointer = IntPtr.Zero;
            CroppedPanoRenderInfoPointer = IntPtr.Zero;
            UpdateStitchStatusMessage();
            if (!stitchEngineWrapper.StartCompositing())
            {
                TaskCanceled();
                OnError("Could not stitch together any of the input images. Please make sure your images are taken from one location and overlap one another.");
            }
        }

        private void StartProjecting()
        {
            ProjectedPanoRenderInfoPointer = IntPtr.Zero;
            CroppedPanoRenderInfoPointer = IntPtr.Zero;
            if (!stitchEngineWrapper.StartProjecting())
            {
                TaskCanceled();
                OnError("Could not project the panorama.");
            }
        }

        private void StartCompleting()
        {
            if (!stitchEngineWrapper.StartCompleting())
            {
                TaskCanceled();
                OnError("Could not complete the panorama.");
            }
        }

        private void StartReprojecting()
        {
            if (!stitchEngineWrapper.StartReprojecting())
            {
                TaskCanceled();
                OnError("Could not reproject the completed panorama.");
            }
        }

        private void StartExporting()
        {
            Int32Rect int32Rect = new Int32Rect((int)Math.Floor(ImageExport.CropRect.X), (int)Math.Floor(ImageExport.CropRect.Y), (int)Math.Round(ImageExport.CropRect.Width), (int)Math.Round(ImageExport.CropRect.Height));
            if (!stitchEngineWrapper.StartExporting(ExportFilename, int32Rect, (float)ImageExport.ExportScale, CurrentExport.CreateOutputOptions(), ShowCompletion))
            {
                TaskCanceled();
                OnError("Could not export an image.");
            }
        }

       
     
     

        private void UpdateStitchStatusMessage()
        {
            StringBuilder stringBuilder = new StringBuilder();
            MotionModel motionModel = stitchEngineWrapper.ActualMotionModel;
            stringBuilder.AppendFormat(CultureInfo.CurrentCulture, "Camera motion: {0}.", new object[1] { GetMotionModelDescription(motionModel) });
            stringBuilder.AppendFormat(CultureInfo.CurrentCulture, " Stitched {0} of {1} images.", new object[2] { stitchEngineWrapper.AlignedCount, stitchEngineWrapper.ImageCount });
            if (motionModel == MotionModel.Rotation3D)
            {
                stringBuilder.AppendFormat(CultureInfo.CurrentCulture, " Spans {0:0.0}° horizontally, {1:0.0}° vertically.", new object[2]
                {
                (double)((stitchEngineWrapper.ThetaMax - stitchEngineWrapper.ThetaMin) * 180f) / Math.PI,
                (double)((stitchEngineWrapper.PhiMax - stitchEngineWrapper.PhiMin) * 180f) / Math.PI
                });
            }
            StitchStatusMessage = stringBuilder.ToString();
        }

        private static string GetMotionModelDescription(MotionModel motionModel)
        {
            switch (motionModel)
            {
                case MotionModel.RigidScale: return "planar motion";
                case MotionModel.Affine: return "planar motion with skew";
                case MotionModel.Homography: return "planar motion with perspective";
                case MotionModel.Rotation3D: return "rotating motion";
            }
            return motionModel.ToString();
        }

        private void SetCropRectangle(Int32Rect rectangle)
        {
            cropLeft = rectangle.X;
            cropTop = rectangle.Y;
            cropRight = FullWidth - rectangle.X - rectangle.Width;
            cropBottom = FullHeight - rectangle.Y - rectangle.Height;
            UpdateCropRect();
            NotifyPropertyChanged("CropLeft");
            NotifyPropertyChanged("CropTop");
            NotifyPropertyChanged("CropRight");
            NotifyPropertyChanged("CropBottom");
            NotifyPropertyChanged("CropRightPosition");
            NotifyPropertyChanged("CropBottomPosition");
            NotifyPropertyChanged("CropWidth");
            NotifyPropertyChanged("CropHeight");
        }

        private void UpdateCropRect()
        {
            ImageExport.CropRect = new Rect(CropLeft, CropTop, CropWidth, CropHeight);
            Int32Rect int32Rect = new Int32Rect((int)Math.Floor(CropLeft), (int)Math.Floor(CropTop), (int)Math.Round(CropWidth), (int)Math.Round(CropHeight));
            stitchEngineWrapper.SetCropRect(int32Rect);
        }

        private void StitchEngineWrapper_ProgressChanged(object sender, EventArgs e)
        {
            if (CurrentTask != null)
            {
                float progress = stitchEngineWrapper.Progress;
                DateTime now = DateTime.Now;
                if (progress >= 100f || (now - lastProgressUpdateTime).TotalSeconds > 0.1)
                {
                    CurrentTask.Progress = progress;
                    lastProgressUpdateTime = now;
                }
            }
        }

        private void StitchEngineWrapper_TaskCompleted(object sender, EventArgs e)
        {
            dispatcher.BeginInvoke((Action)delegate
            {
                if (stitchEngineWrapper.CancelTask && stitchEngineWrapper.LastError == 2147500036u)
                {
                    TaskCanceled();
                }
                else
                {
                    TaskCompleted();
                }
            });
        }

        private void TaskCanceled()
        {
            if (CurrentTask != null)
            {
                taskStopwatch.Stop();
                cancellationStopwatch.Stop();

            }
            stitchEngineWrapper.CancelTask = false;
            stitchEngineWrapper.ClearLastError();
            if (IsDirty(DirtyFlags.Compositing) && NavigationState > NavigationState.Import)
            {
                GoToState(NavigationState.Import);
            }
            else if (IsDirty(DirtyFlags.Projection) && NavigationState > NavigationState.Stitch)
            {
                GoToState(NavigationState.Stitch);
            }
            else if (CurrentTask.TaskPurpose == TaskPurpose.Complete && IsDirty(DirtyFlags.Completion))
            {
                InvalidateCompletion();
            }
            Tasks.Clear();
            CurrentTask = null;
            IsCancelingTask = false;
        }

        private void TaskCompleted()
        {
            if (CurrentTask != null)
            {
                CurrentTask.Progress = 100.0;
                CurrentTask.TaskState = TaskState.Completed;
                bool flag = false;
                switch (CurrentTask.TaskPurpose)
                {
                    case TaskPurpose.SelectVideoFrames:
                        flag = SelectingVideoFramesDone();
                        break;
                    case TaskPurpose.Align:
                        flag = AligningDone();
                        break;
                    case TaskPurpose.Composite:
                        flag = CompositingDone();
                        break;
                    case TaskPurpose.Project:
                        flag = ProjectingDone();
                        break;
                    case TaskPurpose.Complete:
                        flag = CompletingDone();
                        break;
                    case TaskPurpose.Reproject:
                        flag = ReprojectingDone();
                        break;
                    case TaskPurpose.Export:
                        flag = ExportingDone();
                        break;
    
                }
                taskStopwatch.Stop();

                if (flag)
                {
                    StartNextTask();
                    return;
                }
                Tasks.Clear();
                CurrentTask = null;
            }
        }

        private bool SelectingVideoFramesDone()
        {
            string text = null;
            if (stitchEngineWrapper.HasLastError)
            {
                text = "Could not read or analyze the video file.";
            }
            else if (stitchEngineWrapper.ImageCount == 0)
            {
                text = "Video analysis did not choose any frames. Please select a start time and end time that encompass a panorama in the video.";
            }
            else if (stitchEngineWrapper.ImageCount == 1)
            {
                text = "Video analysis chose only one frame. Please select a start time and end time that encompass a panorama in the video.";
            }
            if (text != null)
            {
                SetDirtyFlag(DirtyFlags.InitializationAndBeyond);
                NavigationState = NavigationState.Import;
                OnError(text);
                return false;
            }
            ProjectInfo = stitchEngineWrapper.GetProjectInfo(VideoImport.SystemQuarterRotationCount);
            VideoImport.StoreRequiredFrames(ProjectInfo.SourceVideos.FirstOrDefault());
            ClearDirtyFlag(DirtyFlags.VideoFrameSelection);
            return true;
        }

        private bool AligningDone()
        {
            IntSize intSize = default(IntSize);
            bool hasLastError = stitchEngineWrapper.HasLastError;
            if (!hasLastError)
            {
                intSize = stitchEngineWrapper.CompositeSize;
            }
            if (hasLastError || intSize.Width == 0 || intSize.Height == 0)
            {
                NavigationState = NavigationState.Import;
                if (stitchEngineWrapper.LastError == 2818506993u)
                {
                    OnError("Could not align any of the images using the selected camera motion. Please choose a different camera motion setting.");
                }
                else if (stitchEngineWrapper.LastError != 2147500036u)
                {
                    OnError("Could not stitch together any of the input images. Please make sure your images are taken from one location and overlap one another.");
                }
                return false;
            }
            if (stitchEngineWrapper.AlignedCount < 2)
            {
                NavigationState = NavigationState.Import;
                OnError("Could not align any of the input images. Please make sure your images are taken from one location and overlap one another.");
                return false;
            }
            ActualMotionModel = stitchEngineWrapper.ActualMotionModel;
            Projection = stitchEngineWrapper.Projection;
            StructuredPanoramaInfo structuredPanoramaSettings = ProjectInfo.StructuredPanoramaSettings;
            ProjectInfo = stitchEngineWrapper.GetProjectInfo(VideoImport.SystemQuarterRotationCount);
            ProjectInfo.StructuredPanoramaSettings = structuredPanoramaSettings;
            ClearDirtyFlag(DirtyFlags.Alignment);
            return true;
        }

        private bool CompositingDone()
        {
            if (stitchEngineWrapper.HasLastError)
            {
                NavigationState = NavigationState.Import;
                OnError("Could not stitch together any of the input images. Please make sure your images are taken from one location and overlap one another.");
                return false;
            }
            ShouldZoomToFit = true;
            CompositedPanoRenderInfoPointer = stitchEngineWrapper.CompositedPanoRenderInfoPointer;
            ClearDirtyFlag(DirtyFlags.Compositing);
            return true;
        }

        private bool ProjectingDone()
        {
            if (stitchEngineWrapper.HasLastError)
            {
                if (NavigationState > NavigationState.Stitch)
                {
                    NavigationState = NavigationState.Stitch;
                }
                OnError("Could not project the panorama.");
                return false;
            }
            IntSize projectedSize = stitchEngineWrapper.ProjectedSize;
            FullWidth = projectedSize.Width;
            FullHeight = projectedSize.Height;
            NoCrop();
            ProjectedPanoRenderInfoPointer = stitchEngineWrapper.ProjectedPanoRenderInfoPointer;
            CroppedPanoRenderInfoPointer = stitchEngineWrapper.CroppedPanoRenderInfoPointer;
            ClearDirtyFlag(DirtyFlags.Projection);
            return true;
        }

        private bool ReprojectingDone()
        {
            if (stitchEngineWrapper.HasLastError)
            {
                OnError("Could not reproject the completed panorama.");
                return false;
            }
            ClearDirtyFlag(DirtyFlags.Reprojection);
            return true;
        }

        private bool CompletingDone()
        {
            if (stitchEngineWrapper.HasLastError)
            {
                OnError("Could not complete the panorama.");
                return false;
            }
            NoCrop();
            ProjectedPanoRenderInfoPointer = IntPtr.Zero;
            CroppedPanoRenderInfoPointer = IntPtr.Zero;
            ProjectedPanoRenderInfoPointer = stitchEngineWrapper.ProjectedPanoRenderInfoPointer;
            CroppedPanoRenderInfoPointer = stitchEngineWrapper.CroppedPanoRenderInfoPointer;
            ClearDirtyFlag(DirtyFlags.Completion);
            ShowCompletion = true;
            return true;
        }

        private bool ExportingDone()
        {
            if (stitchEngineWrapper.CancelTask)
            {
                stitchEngineWrapper.CancelTask = false;
                stitchEngineWrapper.ClearLastError();
                return false;
            }
            if (stitchEngineWrapper.HasLastError)
            {
                OnError("Could not export an image.");
                return false;
            }
            if (ExportMode == ExportMode.Tileset && TilesetExport.OpenAfterExport)
            {
                string panoramaLocation = Path.ChangeExtension(ExportFilename, ".html");
                OnExportCompleted(panoramaLocation);
            }
            return true;
        }


        private void OnError(string message)
        {
            EventHandler<ErrorEventArgs> errorOccurred = ErrorOccurred;
            if (errorOccurred != null)
            {
                uint lastError = stitchEngineWrapper.LastError;
                string lastErrorMessage = stitchEngineWrapper.LastErrorMessage;
                if (!string.IsNullOrEmpty(lastErrorMessage) && lastErrorMessage != "Unknown: n/a")
                {
                    lastErrorMessage = lastErrorMessage.Trim();
                    if (lastErrorMessage.Last() != '.')
                    {
                        lastErrorMessage += '.';
                    }
                    switch (lastError)
                    {
                        case 2147942512u:
                            message = lastErrorMessage;
                            break;
                        default:
                            message = message + "\n\n" + lastErrorMessage;
                            break;
                        case 2147500037u:
                        case 0u:
                            break;
                    }
                }
                uint num = (lastError >> 16) & 0x7FFu;
                if (lastError != 0 && lastError != 2147500037u && num != 2047)
                {
                    message = string.Format(CultureInfo.CurrentCulture, "{0} (Error code 0x{1:X8} at {2:0.#}% progress.)", new object[3] { message, lastError, TaskProgress });
                }
                if (lastError != 0)
                {
                    
                }
                errorOccurred(this, new ErrorEventArgs(new ApplicationException(message)));
            }
            stitchEngineWrapper.ClearLastError();
        }

        private void OnExportCompleted(string panoramaLocation)
        {
            ExportCompleted?.Invoke(this, new ExportCompletedEventArgs(panoramaLocation));
        }

        private void InvalidateProjection()
        {
            InvalidateCompletion();
            SetDirtyFlag(DirtyFlags.ProjectionAndBeyond);
        }

        private void InvalidateCompletion()
        {
            ShowCompletion = false;
            SetDirtyFlag(DirtyFlags.CompletionAndBeyond);
        }
    }
}