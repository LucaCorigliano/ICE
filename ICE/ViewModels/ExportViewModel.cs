using Microsoft.Research.ICE.Stitching;
using Microsoft.Research.VisionTools.Toolkit;

namespace Microsoft.Research.ICE.ViewModels
{
    public abstract class ExportViewModel : Notifier
    {
        public abstract string FileFilter { get; }

        public abstract string DefaultFileExtension { get; }

        public abstract OutputOptions CreateOutputOptions();
    }
}