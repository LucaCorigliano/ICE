namespace Microsoft.Research.ICE.ViewModels
{
    public sealed class NamedPreset
    {
        public string Name { get; private set; }

        public int Value { get; private set; }

        public int MaxValue { get; private set; }

        public NamedPreset(string name, int value, int maxValue)
        {
            Name = name;
            Value = value;
            MaxValue = maxValue;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}