namespace Microsoft.Research.ICE.ViewModels
{

	public sealed class NamedValue<T>
	{
		public string Name { get; private set; }

		public T Value { get; private set; }

		public NamedValue(string name, T value)
		{
			Name = name;
			Value = value;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}