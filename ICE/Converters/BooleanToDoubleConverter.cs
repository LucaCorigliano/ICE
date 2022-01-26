namespace Microsoft.Research.ICE.Converters
{
    public sealed class BooleanToDoubleConverter : BooleanConverter<double>
    {
        public BooleanToDoubleConverter()
            : base(1.0, 0.0)
        {
        }
    }
}