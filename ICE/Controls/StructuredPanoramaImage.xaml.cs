using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Microsoft.Research.ICE.Controls
{
	public partial class StructuredPanoramaImage : UserControl
	{
		private const double FontSizeFraction = 0.35;

		private const double MinFontSize = 6;

		private const double MaxFontSize = 20;

		public readonly static DependencyProperty ImageNumberProperty;

		public readonly static DependencyProperty ThumbnailImageProperty;

		public readonly static DependencyProperty ShowBorderProperty;

		public readonly static DependencyProperty IsShowingNumberProperty;

		public readonly static DependencyProperty IsSelectedProperty;

		public int ImageNumber
		{
			get
			{
				return (int)GetValue(ImageNumberProperty);
			}
			set
			{
                SetValue(ImageNumberProperty, value);
			}
		}

		public Size ImageSize
		{
			get
			{
				BitmapSource source = image.Source as BitmapSource;
				if (source == null)
				{
					return Size.Empty;
				}
				return new Size((double)source.PixelWidth, (double)source.PixelHeight);
			}
		}

		public bool IsSelected
		{
			get
			{
				return (bool)GetValue(IsSelectedProperty);
			}
			set
			{
                SetValue(IsSelectedProperty, value);
			}
		}

		public bool IsShowingNumber
		{
			get
			{
				return (bool)GetValue(IsShowingNumberProperty);
			}
			set
			{
                SetValue(IsShowingNumberProperty, value);
			}
		}

		public bool ShowBorder
		{
			get
			{
				return (bool)GetValue(ShowBorderProperty);
			}
			set
			{
                SetValue(ShowBorderProperty, value);
			}
		}

		public BitmapSource ThumbnailImage
		{
			get
			{
				return (BitmapSource)GetValue(ThumbnailImageProperty);
			}
			set
			{
                SetValue(ThumbnailImageProperty, value);
			}
		}

		static StructuredPanoramaImage()
		{
            ImageNumberProperty = DependencyProperty.Register("ImageNumber", typeof(int), typeof(StructuredPanoramaImage), new UIPropertyMetadata((object)0));
            ThumbnailImageProperty = DependencyProperty.Register("ThumbnailImage", typeof(BitmapSource), typeof(StructuredPanoramaImage), new PropertyMetadata(new PropertyChangedCallback(ThumbnailImagePropertyChanged)));
            ShowBorderProperty = DependencyProperty.Register("ShowBorder", typeof(bool), typeof(StructuredPanoramaImage), new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnShowBorderChanged)));
            IsShowingNumberProperty = DependencyProperty.Register("IsShowingNumber", typeof(bool), typeof(StructuredPanoramaImage), new PropertyMetadata(true));
            IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(StructuredPanoramaImage), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(IsSelectedPropertyChanged)));
		}

		public StructuredPanoramaImage()
		{
			InitializeComponent();
			TextBlock textBlock = label;
			DependencyProperty textProperty = TextBlock.TextProperty;
			Binding binding = new Binding("ImageNumber")
			{
				Source = this
			};
			textBlock.SetBinding(textProperty, binding);
			BooleanToVisibilityConverter booleanToVisibilityConverter = new BooleanToVisibilityConverter();
			TextBlock textBlock1 = label;
			DependencyProperty visibilityProperty = VisibilityProperty;
			Binding binding1 = new Binding("IsShowingNumber")
			{
				Source = this,
				Converter = booleanToVisibilityConverter
			};
			textBlock1.SetBinding(visibilityProperty, binding1);
		}

		private static void IsSelectedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			StructuredPanoramaImage structuredPanoramaImage = (StructuredPanoramaImage)d;
			bool newValue = (bool)e.NewValue;
			structuredPanoramaImage.border.Visibility = (newValue ? Visibility.Visible : Visibility.Collapsed);
		}

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			if (sizeInfo.NewSize.Width > 0 && sizeInfo.NewSize.Height > 0)
			{
				double num = Math.Min(sizeInfo.NewSize.Width, sizeInfo.NewSize.Height);
				label.FontSize = Math.Max(6, Math.Min(num * 0.35, 20));
			}
		}

		private static void OnShowBorderChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			StructuredPanoramaImage structuredPanoramaImage = obj as StructuredPanoramaImage;
			structuredPanoramaImage.border.BorderThickness = new Thickness(structuredPanoramaImage.ShowBorder ? 2.0 : 0.0);

		}

		private static void ThumbnailImagePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			StructuredPanoramaImage newValue = obj as StructuredPanoramaImage;
			if (newValue != null)
			{
				newValue.image.Source = e.NewValue as ImageSource;
			}
		}
	}
}