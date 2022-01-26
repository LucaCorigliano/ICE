namespace Microsoft.Research.ICE.ViewModels
{

	public sealed class ZipArchiveSizeViewModel : NamedPresetViewModel
	{
		private const int MinSize = 1;

		private const int MaxSize = 2047;

		private const int DefaultSize = 2047;

		public ZipArchiveSizeViewModel()
			: base(1, 2047, 2047)
		{
            Presets = new NamedPreset[5]
			{
			new NamedPreset("Small", 5, 27),
			new NamedPreset("Medium", 50, 127),
			new NamedPreset("Large", 200, 600),
			new NamedPreset("Extra-large", 1024, 2046),
			new NamedPreset("Maximum", 2047, 2047)
			};
		}
	}

}