namespace Microsoft.Research.ICE.Converters
{
    public sealed class BooleanToInt32Converter : BooleanConverter<int>
    {
        public BooleanToInt32Converter()
            : base(1, 0)
        {
        }
    }
}