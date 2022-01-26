using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Microsoft.Research.D3DCore;
using Microsoft.Research.ICE.PanoViewing;
using Microsoft.Research.ICE.UserInterface;


namespace Microsoft.Research.ICE.Helpers
{
    public sealed class ZoomHelper
    {
        private PanoViewer panoViewer;

        private bool allowZoomToActualSize;

        private CommandBinding zoomOutCommandBinding;

        private CommandBinding zoomInCommandBinding;

        private CommandBinding zoomToFitCommandBinding;

        private CommandBinding zoomToActualSizeCommandBinding;

        public ZoomHelper(FrameworkElement element, PanoViewer panoViewer, bool allowZoomToActualSize = true)
        {
            this.panoViewer = panoViewer;
            this.allowZoomToActualSize = allowZoomToActualSize;
            zoomOutCommandBinding = new CommandBinding(Commands.ZoomOut, ZoomOut_Executed);
            zoomInCommandBinding = new CommandBinding(Commands.ZoomIn, ZoomIn_Executed);
            zoomToFitCommandBinding = new CommandBinding(Commands.ZoomToFit, ZoomToFit_Executed);
            zoomToActualSizeCommandBinding = new CommandBinding(Commands.ZoomToActualSize, ZoomToActualSize_Executed);
            element.Loaded += Element_Loaded;
            element.Unloaded += Element_Unloaded;
        }

        private void Element_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.CommandBindings.Replace(zoomOutCommandBinding);
            Application.Current.MainWindow.CommandBindings.Replace(zoomInCommandBinding);
            Application.Current.MainWindow.CommandBindings.Replace(zoomToFitCommandBinding);
            if (allowZoomToActualSize)
            {
                Application.Current.MainWindow.CommandBindings.Replace(zoomToActualSizeCommandBinding);
            }
            else
            {
                Application.Current.MainWindow.CommandBindings.Remove(zoomToActualSizeCommandBinding);
            }
            panoViewer.ErrorOccurred += PanoViewer_ErrorOccurred;
        }

        private void Element_Unloaded(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.CommandBindings.Remove(zoomOutCommandBinding);
            Application.Current.MainWindow.CommandBindings.Remove(zoomInCommandBinding);
            Application.Current.MainWindow.CommandBindings.Remove(zoomToFitCommandBinding);
            Application.Current.MainWindow.CommandBindings.Remove(zoomToActualSizeCommandBinding);
            panoViewer.ErrorOccurred -= PanoViewer_ErrorOccurred;
        }

        private void ZoomOut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            
            panoViewer.ZoomOut();
        }

        private void ZoomIn_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            
            panoViewer.ZoomIn();
        }

        private void ZoomToFit_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            
            panoViewer.ZoomToFit();
        }

        private void ZoomToActualSize_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (allowZoomToActualSize)
            {
                
                panoViewer.Zoom = 1.0;
            }
        }

        private void PanoViewer_ErrorOccurred(object sender, D3DErrorEventArgs e)
        {

        }
    }
}