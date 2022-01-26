using System.Windows.Media;

namespace Microsoft.Research.ICE.Converters
{
    public sealed class BooleanToBrushConverter : BooleanConverter<Brush>
    {
        public BooleanToBrushConverter()
            : base((Brush)null, (Brush)null)
        {
        }
    }
}