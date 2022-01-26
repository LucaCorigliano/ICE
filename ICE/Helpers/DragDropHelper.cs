using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Research.ICE.Controls;
using static Microsoft.Research.ICE.Controls.MessageDialog;

namespace Microsoft.Research.ICE.Helpers
{
    public sealed class DragDropHelper
    {
        public FrameworkElement Element { get; private set; }

        public ImagesDroppedCallback ImagesDroppedCallback { get; private set; }

        public ImagesOrVideoOrProjectDroppedCallback ImagesOrVideoOrProjectDroppedCallback { get; private set; }

        public bool AllowVideoAndProjectFiles => ImagesOrVideoOrProjectDroppedCallback != null;

        public DragDropHelper(FrameworkElement element, ImagesDroppedCallback imagesDroppedCallback)
        {
            ImagesDroppedCallback = imagesDroppedCallback;
            Attach(element);
        }

        public DragDropHelper(FrameworkElement element, ImagesOrVideoOrProjectDroppedCallback imagesOrVideoOrProjectDroppedCallback)
        {
            ImagesOrVideoOrProjectDroppedCallback = imagesOrVideoOrProjectDroppedCallback;
            Attach(element);
        }

        private void Attach(FrameworkElement element)
        {
            Element = element;
            Element.Loaded += Element_Loaded;
            Element.Unloaded += Element_Unloaded;
        }

        private void Element_Loaded(object sender, RoutedEventArgs e)
        {
            Element.DragOver += Element_DragOver;
            Element.Drop += Element_Drop;
        }

        private void Element_Unloaded(object sender, RoutedEventArgs e)
        {
            Element.DragOver -= Element_DragOver;
            Element.Drop -= Element_Drop;
        }

        private void Element_DragOver(object sender, DragEventArgs e)
        {
            DragDropEffects effects = DragDropEffects.None;
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop, autoConvert: true) is string[] source && source.Any(IsAllowedFile))
            {
                effects = DragDropEffects.Copy;
            }
            e.Effects = effects;
            e.Handled = true;
        }

        private bool IsAllowedFile(string filePath)
        {
            if (!FileHelper.Instance.IsImageFile(filePath))
            {
                if (AllowVideoAndProjectFiles)
                {
                    if (!FileHelper.Instance.IsVideoFile(filePath))
                    {
                        return FileHelper.Instance.IsProjectFile(filePath);
                    }
                    return true;
                }
                return false;
            }
            return true;
        }

        private void Element_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }
            if (e.Data.GetData(DataFormats.FileDrop, autoConvert: true) is string[] droppedFiles)
            {
                if (AllowVideoAndProjectFiles)
                {
                    ProcessImagesOrVideoOrProject(droppedFiles, "drag-and-drop");
                }
                else
                {
                    ProcessImages(droppedFiles);
                }
            }
            e.Handled = true;
        }

        public void ProcessImagesOrVideoOrProject(string[] droppedFiles, string source)
        {
            if (droppedFiles.Length <= 0)
            {
                return;
            }
            string[] source2 = droppedFiles.Where(File.Exists).ToArray();
            string[] array = source2.Where(FileHelper.Instance.IsImageFile).ToArray();
            string text = source2.FirstOrDefault(FileHelper.Instance.IsVideoFile);
            string text2 = source2.FirstOrDefault(FileHelper.Instance.IsProjectFile);
            bool flag = false;
            if (!string.IsNullOrEmpty(text2))
            {
                flag = true;
                int num = droppedFiles.Length - 1;
                if (num > 0)
                {
                    
                    string message = string.Format(CultureInfo.CurrentCulture, "Opening \"{0}\" and ignoring {1} other file{2}.", new object[3]
                    {
                    Path.GetFileName(text2),
                    num,
                    (num > 1) ? "s" : string.Empty
                    });
                    flag = Show(Window.GetWindow(Element), message, "OK", "Cancel", null) == MessageDialogResult.Yes;
                }
            }
            else if (!string.IsNullOrEmpty(text))
            {
                flag = true;
                int num2 = droppedFiles.Length - 1;
                if (num2 > 0)
                {
                    
                    string message2 = string.Format(CultureInfo.CurrentCulture, "Opening \"{0}\" and ignoring {1} other file{2}.", new object[3]
                    {
                    Path.GetFileName(text),
                    num2,
                    (num2 > 1) ? "s" : string.Empty
                    });
                    flag = Show(Window.GetWindow(Element), message2, "OK", "Cancel", null) == MessageDialogResult.Yes;
                }
            }
            else if (array.Length > 0)
            {
                flag = true;
                int num3 = droppedFiles.Length - array.Length;
                if (num3 > 0)
                {
                    
                    string message3 = string.Format(CultureInfo.CurrentCulture, "Opening {0} image file{1} and ignoring {2} non-image file{3}.", array.Length, (array.Length > 1) ? "s" : string.Empty, num3, (num3 > 1) ? "s" : string.Empty);
                    flag = Show(Window.GetWindow(Element), message3, "OK", "Cancel", null) == MessageDialogResult.Yes;
                }
            }
            else
            {
                Show(Window.GetWindow(Element), "None of the specified files are valid images, videos, or project files.", "OK", null, null);
            }
            if (flag && ImagesOrVideoOrProjectDroppedCallback != null)
            {
                ImagesOrVideoOrProjectDroppedCallback(array, text, text2, source);
            }
        }

        private void ProcessImages(string[] droppedFiles)
        {
            string[] array = droppedFiles.Where((string path) => File.Exists(path) && FileHelper.Instance.IsImageFile(path)).ToArray();
            if (array.Length > 0)
            {
                bool flag = true;
                int num = droppedFiles.Length - array.Length;
                if (num > 0)
                {
                    
                    string message = string.Format(CultureInfo.CurrentCulture, "Importing {0} image file{1} and ignoring {2} non-image file{3}.", array.Length, (array.Length > 1) ? "s" : string.Empty, num, (num > 1) ? "s" : string.Empty);
                    flag = Show(Window.GetWindow(Element), message, "OK", "Cancel", null) == MessageDialogResult.Yes;
                }
                if (flag && ImagesDroppedCallback != null)
                {
                    ImagesDroppedCallback(array);
                }
            }
        }
    }
}