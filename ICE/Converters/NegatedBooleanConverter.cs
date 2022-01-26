namespace Microsoft.Research.ICE.Converters
{
    public sealed class NegatedBooleanConverter : BooleanConverter<bool>
    {
        public NegatedBooleanConverter()
            : base(trueValue: false, falseValue: true)
        {
        }
    }
}