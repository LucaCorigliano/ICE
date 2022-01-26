using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Microsoft.Research.ICE.ViewModels;
using Microsoft.Research.VisionTools.Toolkit.Desktop;
using Microsoft.Research.ICE.Properties;

namespace Microsoft.Research.ICE.Controls
{
    public partial class OptionsDialog 
    {



        public OptionsViewModel ViewModel
        {
            get
            {
                return DataContext as OptionsViewModel;
            }
            set
            {
                DataContext = value;
            }
        }

        public OptionsDialog()
        {
            InitializeComponent();
            memoryResetButton.Click += MemoryResetButton_Click;
            cacheResetButton.Click += CacheResetButton_Click;
            okButton.Click += OKButton_Click;


        }

        private void MemoryResetButton_Click(object sender, RoutedEventArgs e)
        {
            
            ViewModel.MemoryConsumptionLimit = ViewModel.DefaultMemoryConsumptionLimit;
        }

        private void CacheResetButton_Click(object sender, RoutedEventArgs e)
        {
            
            ViewModel.ImageCacheLocations.Clear();
            ViewModel.AddImageCacheLocation(Settings.Default.DefaultImageCacheLocation);
            cacheLocationsListBox.SelectedIndex = 0;
        }

        private void Add_Executed(object sender, RoutedEventArgs e)
        {
            if (SelectFolder(null, null))
            {
                
                cacheLocationsListBox.SelectedIndex = ViewModel.ImageCacheLocations.Count - 1;
                cacheLocationsListBox.ScrollIntoView(cacheLocationsListBox.SelectedItem);
            }
        }

        private void CacheLocationListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                ChangeCacheLocation();
            }
        }

        private void Change_Executed(object sender, RoutedEventArgs e)
        {
            ChangeCacheLocation();
        }

        private void ChangeCacheLocation()
        {
            CacheLocationViewModel cacheLocationViewModel = ViewModel.ImageCacheLocations[cacheLocationsListBox.SelectedIndex];
            if (SelectFolder(cacheLocationViewModel.ExpandedPath, cacheLocationsListBox.SelectedIndex))
            {
                
                cacheLocationsListBox.ScrollIntoView(cacheLocationsListBox.SelectedItem);
            }
        }
        private void Delete_Executed(object sender, RoutedEventArgs e)
        {
            
            int selectedIndex = cacheLocationsListBox.SelectedIndex;
            ViewModel.RemoveImageCacheLocation(selectedIndex);
            selectedIndex = Math.Min(selectedIndex, ViewModel.ImageCacheLocations.Count - 1);
            if (selectedIndex >= 0 && !ViewModel.ImageCacheLocations[selectedIndex].IsEditable)
            {
                selectedIndex = -1;
            }
            cacheLocationsListBox.SelectedIndex = selectedIndex;
            cacheLocationsListBox.ScrollIntoView(cacheLocationsListBox.SelectedItem);
        }



        private void MoveUp_Executed(object sender, RoutedEventArgs e)
        {
            
            int selectedIndex = cacheLocationsListBox.SelectedIndex;
            ViewModel.MoveImageCacheLocation(selectedIndex, selectedIndex - 1);
            cacheLocationsListBox.SelectedIndex = selectedIndex - 1;
            cacheLocationsListBox.ScrollIntoView(cacheLocationsListBox.SelectedItem);
        }

        private void MoveDown_Executed(object sender, RoutedEventArgs e)
        {
            
            int selectedIndex = cacheLocationsListBox.SelectedIndex;
            ViewModel.MoveImageCacheLocation(selectedIndex, selectedIndex + 1);
            cacheLocationsListBox.SelectedIndex = selectedIndex + 1;
            cacheLocationsListBox.ScrollIntoView(cacheLocationsListBox.SelectedItem);
        }

        private bool SelectFolder(string initialFolder, int? indexOfCacheLocationToChange)
        {
            string text = FolderPicker.ChooseFolder(this, "Choose a folder for temporary files", initialFolder, Environment.ExpandEnvironmentVariables(Settings.Default.DefaultImageCacheLocation));
            if (text != null)
            {
                string text2 = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
                if (text.StartsWith(text2))
                {
                    text = text.Replace(text2, "%TEMP%");
                }
                if (indexOfCacheLocationToChange.HasValue)
                {
                    ViewModel.ChangeImageCacheLocation(indexOfCacheLocationToChange.Value, text);
                }
                else
                {
                    ViewModel.AddImageCacheLocation(text);
                }
                return true;
            }
            return false;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CacheFolder_SelectionChanged(object sender, RoutedEventArgs e)
        {
            int selectedIndex = cacheLocationsListBox.SelectedIndex;
            Down.IsEnabled = selectedIndex >= 0 && selectedIndex < ViewModel.ImageCacheLocations.Count - 1;
            Remove.IsEnabled = cacheLocationsListBox.SelectedItem != null && ViewModel.ImageCacheLocations.Count > 1;
            Up.IsEnabled = selectedIndex > 0 && ViewModel.ImageCacheLocations[selectedIndex - 1].IsEditable;
            Change.IsEnabled = cacheLocationsListBox.SelectedItem != null;
        }


    }
}