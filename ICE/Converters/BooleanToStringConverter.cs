namespace Microsoft.Research.ICE.Converters
{
    public sealed class BooleanToStringConverter : BooleanConverter<string>
    {
        public BooleanToStringConverter()
            : base((string)null, (string)null)
        {
        }
    }
}