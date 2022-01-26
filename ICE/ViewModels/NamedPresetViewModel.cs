using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Research.VisionTools.Toolkit;

namespace Microsoft.Research.ICE.ViewModels
{
    public abstract class NamedPresetViewModel : Notifier
    {
        private int value;

        public int MinValue { get; private set; }

        public int MaxValue { get; private set; }

        public int Value
        {
            get
            {
                return value;
            }
            set
            {
                value = Math.Max(MinValue, Math.Min(value, MaxValue));
                if (SetProperty(ref this.value, value, "Value"))
                {
                    NotifyPropertyChanged("Preset");
                }
            }
        }

        public IEnumerable<NamedPreset> Presets { get; protected set; }

        public NamedPreset Preset
        {
            get
            {
                foreach (NamedPreset preset in Presets)
                {
                    if (Value <= preset.MaxValue)
                    {
                        return preset;
                    }
                }
                return Presets.LastOrDefault();
            }
            set
            {
                Value = value.Value;
            }
        }

        protected NamedPresetViewModel(int minValue, int maxValue, int defaultValue)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            Value = defaultValue;
        }
    }
}