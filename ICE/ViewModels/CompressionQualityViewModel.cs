namespace Microsoft.Research.ICE.ViewModels
{
    public sealed class CompressionQualityViewModel : NamedPresetViewModel
    {
        private const int MinQuality = 1;

        private const int MaxQuality = 100;

        private const int DefaultQuality = 75;

        public CompressionQualityViewModel(bool supportsLossless)
            : base(1, 100, 75)
        {
            if (supportsLossless)
            {
                Presets = new NamedPreset[5]
                {
                new NamedPreset("Low", 25, 30),
                new NamedPreset("Medium", 50, 60),
                new NamedPreset("High", 75, 80),
                new NamedPreset("Superb", 90, 99),
                new NamedPreset("Lossless", 100, 100)
                };
            }
            else
            {
                Presets = new NamedPreset[4]
                {
                new NamedPreset("Low", 25, 30),
                new NamedPreset("Medium", 50, 60),
                new NamedPreset("High", 75, 80),
                new NamedPreset("Superb", 90, 100)
                };
            }
        }
    }
}