using System.Windows.Input;

namespace Microsoft.Research.ICE.UserInterface
{
    public static class Commands
    {
        public static readonly RoutedCommand NewImagePanorama = new RoutedCommand("NewImagePanorama", typeof(Commands), new InputGestureCollection
    {
        new KeyGesture(Key.N, ModifierKeys.Control, "Ctrl+N")
    });

        public static readonly RoutedCommand NewVideoPanorama = new RoutedCommand("NewVideoPanorama", typeof(Commands), new InputGestureCollection
    {
        new KeyGesture(Key.N, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl+Shift+N")
    });

        public static readonly RoutedCommand Options = new RoutedCommand("Options", typeof(Commands));

        public static readonly RoutedCommand AddImages = new RoutedCommand("AddImages", typeof(Commands));

        public static readonly RoutedCommand Export = new RoutedCommand("Export", typeof(Commands));


        public static readonly RoutedCommand ZoomOut = new RoutedCommand("ZoomOut", typeof(Commands), new InputGestureCollection
    {
        new KeyGesture(Key.Subtract, ModifierKeys.Control, "Ctrl+-"),
        new KeyGesture(Key.OemMinus, ModifierKeys.Control)
    });

        public static readonly RoutedCommand ZoomIn = new RoutedCommand("ZoomIn", typeof(Commands), new InputGestureCollection
    {
        new KeyGesture(Key.Add, ModifierKeys.Control, "Ctrl++"),
        new KeyGesture(Key.OemPlus, ModifierKeys.Control)
    });

        public static readonly RoutedCommand ZoomToFit = new RoutedCommand("ZoomToFit", typeof(Commands), new InputGestureCollection
    {
        new KeyGesture(Key.D0, ModifierKeys.Control, "Ctrl+0")
    });

        public static readonly RoutedCommand ZoomToActualSize = new RoutedCommand("ZoomToActualSize", typeof(Commands), new InputGestureCollection
    {
        new KeyGesture(Key.D1, ModifierKeys.Control, "Ctrl+1")
    });

        public static void Initialize()
        {
            ApplicationCommands.Delete.InputGestures.Add(new KeyGesture(Key.Back, ModifierKeys.None, "Backspace"));
            ApplicationCommands.SaveAs.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl+Shift+S"));
            ApplicationCommands.Close.InputGestures.Add(new KeyGesture(Key.W, ModifierKeys.Control, "Ctrl+W"));
        }
    }
}