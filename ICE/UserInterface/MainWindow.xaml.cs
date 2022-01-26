using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using Microsoft.Research.ICE.Controls;
using Microsoft.Research.ICE.Helpers;
using Microsoft.Research.ICE.Stitching;
using Microsoft.Research.ICE.ViewModels;
using Microsoft.Research.VisionTools.Toolkit.Desktop;
using Microsoft.Research.ICE.Properties;
using static Microsoft.Research.ICE.Controls.MessageDialog;

namespace Microsoft.Research.ICE.UserInterface
{
    public partial class MainWindow 
    {
        private class PreventDragDrop : IDisposable
        {
            private UIElement element;

            private bool allowDrop;

            public PreventDragDrop(UIElement element)
            {
                this.element = element;
                allowDrop = this.element.AllowDrop;
                this.element.AllowDrop = false;
            }

            public void Dispose()
            {
                element.AllowDrop = allowDrop;
            }
        }

        private const string WarningNewVersionAvailable = "A newer version of Image Composite Editor is available.\nDo you want ICE to exit and install the latest version?";

        private const string WarningTileOverwite = "The selected folder contains one or more tile folders that will be overwritten. Do you want to continue?";

        private const string ErrorCouldNotLaunchInstaller = "Could not launch installer.";

        private const string ErrorCouldNotLaunchBrowser = "Could not launch browser.";

        private const string ErrorCouldNotSave = "Could not save project.";

        private const string ErrorUnableToOpenPanorama = "Unable to open exported panorama.";

        public const string ErrorInvalidFileType = "One or more of the files was of an unsupported type.";

        public static MainWindow Instance => (MainWindow)Application.Current.MainWindow;




        public MainViewModel ViewModel
        {
            get
            {
                return DataContext as MainViewModel;
            }
            private set
            {
                DataContext = value;
            }
        }

        private bool IsVideoStitchingSupported { get; set; }

        private DragDropHelper DragDropHelper { get; set; }

        public MainWindow()
        {
            Commands.Initialize();
            ViewModel = new MainViewModel();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.ErrorOccurred += ViewModel_ErrorOccurred;
            ViewModel.ExportCompleted += ViewModel_ExportCompleted;
            IsVideoStitchingSupported = StitchEngineWrapper.IsVideoStitchingSupported;
            InitializeComponent();
            double num = SystemParameters.MaximizedPrimaryScreenHeight - 15.0;
            if (num < MinHeight)
            {
                MinHeight = num;
            }
            DragDropHelper = new DragDropHelper(this, HandleDrop);
            NavigationStateChanged();
            CommandBindings.Add(new CommandBinding(Commands.NewImagePanorama, NewImagePanoramaCommand_Executed));
            CommandBindings.Add(new CommandBinding(Commands.NewVideoPanorama, NewVideoPanoramaCommand_Executed, NewVideoPanoramaCommand_CanExecute));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, OpenCommand_Executed));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, SaveCommand_Executed, SaveCommand_CanExecute));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.SaveAs, SaveAsCommand_Executed, SaveCommand_CanExecute));
            CommandBindings.Add(new CommandBinding(Commands.Options, OptionsCommand_Executed));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Help, HelpCommand_Executed));
            CommandBindings.Add(new CommandBinding(Commands.AddImages, AddImagesCommand_Executed));
            CommandBindings.Add(new CommandBinding(Commands.Export, ExportCommand_Executed));
            CommandBindings.Add(new CommandBinding(NavigationCommands.GoToPage, GoToPageCommand_Executed));
            CommandBindings.Add(new CommandBinding(NavigationCommands.BrowseBack, BrowseBackCommand_Executed));
            CommandBindings.Add(new CommandBinding(NavigationCommands.BrowseForward, BrowseForwardCommand_Executed, BrowseForwardCommand_CanExecute));
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, EventArgs e)
        {
            if (Settings.Default.CheckForUpdatesAtStartup)
            {
                /*string installerLink = await ViewModel.CheckForUpgradeAsync();
                if (IsUpgradeDesired(installerLink))
                {
                    return;
                }*/
            }
            ProcessCommandLineArguments();
        }

        private bool IsUpgradeDesired(string installerLink)
        {
            if (installerLink != null)
            {
                
                if (ShowMessageDialog("A newer version of Image Composite Editor is available.\nDo you want ICE to exit and install the latest version?", "_Yes", "_No", null) == MessageDialogResult.Yes && SaveAnyChanges() && LinkHelper.OpenLink(this, "launch upgrade", installerLink, "Could not launch installer."))
                {
                    Application.Current.Shutdown();
                    return true;
                }
            }
            return false;
        }

        private void ProcessCommandLineArguments()
        {
            string[] droppedFiles = Environment.GetCommandLineArgs().Skip(1).ToArray();
            DragDropHelper.ProcessImagesOrVideoOrProject(droppedFiles, "command line");
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = !SaveAnyChanges() || !StopAnyWorkInProgress();
        }

        private void HandleDrop(IEnumerable<string> imageFiles, string videoFile, string projectFile, string source)
        {
            if (!string.IsNullOrEmpty(projectFile))
            {
                
                ViewModel.OpenProject(projectFile);
            }
            else if (!string.IsNullOrEmpty(videoFile))
            {
                
                ViewModel.NewProjectFromVideo(videoFile);
            }
            else if (imageFiles.Count() > 0)
            {

                ViewModel.NewProjectFromImages(imageFiles);
            }
        }

        private void NewImagePanoramaCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SaveAnyChanges())
            {
                FilePicker openFilePicker = FilePicker.GetOpenFilePicker(GetWindow(this), "Select overlapping images", FileHelper.Instance.ImageFileFilter, multiselect: true);
                if (ShowFilePicker(openFilePicker))
                {

                    ViewModel.NewProjectFromImages(openFilePicker.FileNames);
                }
            }
        }

        private void AddImagesCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            FilePicker openFilePicker = FilePicker.GetOpenFilePicker(GetWindow(this), "Select overlapping images", FileHelper.Instance.ImageFileFilter, multiselect: true);
            if (ShowFilePicker(openFilePicker))
            {

                ViewModel.ImportImages(openFilePicker.FileNames);
            }
        }

        private void NewVideoPanoramaCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = IsVideoStitchingSupported;
        }

        private void NewVideoPanoramaCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SaveAnyChanges())
            {
                FilePicker openFilePicker = FilePicker.GetOpenFilePicker(GetWindow(this), "Select a video", FileHelper.Instance.VideoFileFilter, multiselect: false);
                if (ShowFilePicker(openFilePicker))
                {
                    
                    ViewModel.NewProjectFromVideo(openFilePicker.FileName);
                }
            }
        }

        private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SaveAnyChanges())
            {
                FilePicker openFilePicker = FilePicker.GetOpenFilePicker(this, "Open Panorama Project", "Panorama Projects (*.spj)|*.spj", multiselect: false);
                if (ShowFilePicker(openFilePicker))
                {
                    
                    ViewModel.OpenProject(openFilePicker.FileName);
                }
            }
        }

        private void SaveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ViewModel.HasSomeSourceFiles;
        }

        private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            
            SaveProject();
        }

        private void SaveAsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            
            SaveProjectAs();
        }

        private bool SaveAnyChanges()
        {
            if (ViewModel.IsProjectDirty)
            {
                switch (ShowMessageDialog("The current ICE project hasn't been saved.\nDo you want to save it?", "_Save", "_Discard", "Cancel"))
                {
                    case MessageDialogResult.Yes:
                        
                        return SaveProject();
                    case MessageDialogResult.No:
                        
                        return true;
                    default:
                        
                        return false;
                }
            }
            return true;
        }

        private bool SaveProject()
        {
            if (ViewModel.ProjectFilename == null)
            {
                return SaveProjectAs();
            }
            return ReallySaveProject(ViewModel.ProjectFilename);
        }

        private bool SaveProjectAs()
        {
            FilePicker saveFilePicker = FilePicker.GetSaveFilePicker(this, "Save Panorama Project", "Panorama Projects (*.spj)|*.spj", Path.GetFileName(ViewModel.ProjectFilename));
            if (ShowFilePicker(saveFilePicker))
            {
                return ReallySaveProject(saveFilePicker.FileName);
            }
            return false;
        }

        private bool ReallySaveProject(string filename)
        {
            if (ViewModel.SaveProject(filename))
            {
                return true;
            }
            ShowError("Could not save project.");
            return false;
        }

        private bool StopAnyWorkInProgress()
        {
            if (ViewModel.HasTask)
            {
                if (ShowMessageDialog("Do you want to cancel the current operation and exit?", "_Exit", "Don't exit", null) == MessageDialogResult.Yes)
                {
                    
                    ViewModel.CancelTasksAndWait();
                    return true;
                }
                return false;
            }
            return true;
        }

        private void OptionsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            
            OptionsViewModel options = ViewModel.Options;
            bool checkForUpdatesAtStartup = options.CheckForUpdatesAtStartup;
            int memoryConsumptionLimit = options.MemoryConsumptionLimit;
            string[] array = options.ImageCacheLocations.Select((CacheLocationViewModel c) => c.DirectoryPath).ToArray();
            bool hasNoSourceFiles = ViewModel.HasNoSourceFiles;
            foreach (CacheLocationViewModel imageCacheLocation2 in options.ImageCacheLocations)
            {
                imageCacheLocation2.IsEditable = hasNoSourceFiles;
            }
            OptionsDialog optionsDialog = new OptionsDialog();
            optionsDialog.ViewModel = options;
            OptionsDialog dialog = optionsDialog;
            if (ShowModalDialog(dialog).GetValueOrDefault())
            {

                string cachePaths = "";
                foreach(var path in options.ImageCacheLocations)
                {
                    cachePaths += path.DirectoryPath + "|";
                }
                cachePaths.TrimEnd('|');

                Settings.Default.CheckForUpdatesAtStartup = options.CheckForUpdatesAtStartup;
                Settings.Default.MemoryConsumptionLimit = options.MemoryConsumptionLimit;
                Settings.Default.ImageCacheLocations = cachePaths;
                Settings.Default.Save();
                ViewModel.UpdateImageCacheSettings();
            }
            else
            {
                
                options.CheckForUpdatesAtStartup = checkForUpdatesAtStartup;
                options.MemoryConsumptionLimit = memoryConsumptionLimit;
                options.ImageCacheLocations.Clear();
                string[] array2 = array;
                foreach (string imageCacheLocation in array2)
                {
                    options.AddImageCacheLocation(imageCacheLocation);
                }
            }
        }

        private void HelpCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            
            HelpViewModel help = ViewModel.Help;
            HelpDialog helpDialog = new HelpDialog();
            helpDialog.ViewModel = help;
            HelpDialog dialog = helpDialog;
            ShowModalDialog(dialog);
            
        }

        private void ExportCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            FilePicker saveFilePicker = FilePicker.GetSaveFilePicker(this, "Export Panorama", ViewModel.CurrentExport.FileFilter, ViewModel.DefaultExportFilename);
            if (!ShowFilePicker(saveFilePicker))
            {
                return;
            }
            if (ViewModel.ExportMode == ExportMode.Tileset)
            {
                string path = Path.GetFileNameWithoutExtension(saveFilePicker.FileName) + "_files";
                path = Path.Combine(Path.GetDirectoryName(saveFilePicker.FileName), path);
                if (Directory.Exists(path) && ShowMessageDialog("The selected folder contains one or more tile folders that will be overwritten. Do you want to continue?", "_Overwrite", "Cancel", null) != 0)
                {
                    return;
                }
            }

            ViewModel.ExportImage(ForceAcceptableExtension(saveFilePicker.FileName));
        }

        private string ForceAcceptableExtension(string originalFilePath)
        {
            string fileFilter = ViewModel.CurrentExport.FileFilter;
            string[] array = fileFilter.Substring(fileFilter.LastIndexOf("|", StringComparison.InvariantCulture) + 1).Split(new char[3] { '*', ';', '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (array.Length == 0)
            {
                return originalFilePath;
            }
            string[] array2 = array;
            foreach (string value in array2)
            {
                if (originalFilePath.EndsWith(value, StringComparison.InvariantCultureIgnoreCase))
                {
                    return originalFilePath;
                }
            }
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}", new object[2]
            {
            originalFilePath,
            ViewModel.CurrentExport.DefaultFileExtension
            });
        }



        private void GoToPageCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is Hyperlink hyperlink && hyperlink.NavigateUri != null && hyperlink.NavigateUri.IsAbsoluteUri)
            {
                LinkHelper.OpenLink(this, "launch link", hyperlink.NavigateUri.AbsoluteUri, "Could not launch browser.");
            }
        }

        private void BrowseBackCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (ViewModel.CanGoBack() && (!ViewModel.ShouldSaveBeforeGoingBack || SaveAnyChanges()))
            {
                
                ViewModel.GoBack();
            }
        }

        private void BrowseForwardCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ViewModel.CanGoToNext();
        }

        private void BrowseForwardCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (ViewModel.CanGoToNext())
            {
                
                ViewModel.GoToNext();
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "NavigationState":
                    NavigationStateChanged();
                    break;
                case "HasNoSourceFiles":
                case "IsVideoPanorama":
                case "IsStructuredPanorama":
                    ImportViewChanged();
                    break;
            }
        }

        private void ViewModel_ErrorOccurred(object sender, ErrorEventArgs e)
        {
            ShowError(e.GetException().Message);
        }

        private void ViewModel_ExportCompleted(object sender, ExportCompletedEventArgs e)
        {
            string eventName =  "launch exported deep zoom";
            LinkHelper.OpenLink(this, eventName, e.PanoramaLocation, "Unable to open exported panorama.");
        }

        private void NavigationStateChanged()
        {

            AllowDrop = ViewModel.NavigationState == NavigationState.Welcome;
            FrameworkElement child = null;
            switch (ViewModel.NavigationState)
            {
                case NavigationState.Welcome:
                    child = new WelcomePage();
                    break;
                case NavigationState.Import:
                    child = new ImportPage();
                    break;
                case NavigationState.Stitch:
                    child = new StitchPage();
                    break;
                case NavigationState.Crop:
                    child = new CropPage();
                    break;
                case NavigationState.Export:
                    child = new ExportPage();
                    break;
            }
            contentHolder.Child = child;
            ImportViewChanged();
        }

        private void ImportViewChanged()
        {
            if (contentHolder.Child is ImportPage importPage)
            {
                importPage.UpdateState();
            }
        }

        private MessageDialogResult ShowMessageDialog(string message, string yesLabel, string noLabel, string cancelLabel)
        {
            using (new PreventDragDrop(this))
            {
                return MessageDialog.Show(this, message, yesLabel, noLabel, cancelLabel);
            }
        }

        private void ShowError(string message)
        {

            Dispatcher.BeginInvoke((Action)delegate
            {
                ShowMessageDialog(message, "OK", null, null);
            });
        }

        private bool ShowFilePicker(FilePicker filePicker)
        {
            using (new PreventDragDrop(this))
            {
                return filePicker.ShowDialog();
            }
        }

        private bool? ShowModalDialog(Window dialog)
        {
            using (new PreventDragDrop(this))
            {
                dialog.Owner = this;
                return dialog.ShowDialog();
            }
        }

     
    }
}