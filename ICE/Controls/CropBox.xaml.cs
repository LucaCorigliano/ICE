using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace Microsoft.Research.ICE.Controls
{
	public partial class CropBox : UserControl
	{
		public static readonly DependencyProperty ViewerTransformMatrixProperty = DependencyProperty.Register(nameof(ViewerTransformMatrix), typeof(Matrix), typeof(CropBox), new PropertyMetadata(new PropertyChangedCallback(OnPropertyChanged)));
		public static readonly DependencyProperty CropLeftProperty = DependencyProperty.Register(nameof(CropLeft), typeof(double), typeof(CropBox), (PropertyMetadata)new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnPropertyChanged)));
		public static readonly DependencyProperty CropTopProperty = DependencyProperty.Register(nameof(CropTop), typeof(double), typeof(CropBox), (PropertyMetadata)new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnPropertyChanged)));
		public static readonly DependencyProperty CropRightProperty = DependencyProperty.Register(nameof(CropRight), typeof(double), typeof(CropBox), (PropertyMetadata)new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnPropertyChanged)));
		public static readonly DependencyProperty CropBottomProperty = DependencyProperty.Register(nameof(CropBottom), typeof(double), typeof(CropBox), (PropertyMetadata)new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnPropertyChanged)));
		public static readonly DependencyProperty BitmapWidthProperty = DependencyProperty.Register(nameof(BitmapWidth), typeof(int), typeof(CropBox), new PropertyMetadata());
		public static readonly DependencyProperty BitmapHeightProperty = DependencyProperty.Register(nameof(BitmapHeight), typeof(int), typeof(CropBox), new PropertyMetadata());
		public static readonly DependencyProperty IsSelectableProperty = DependencyProperty.Register(nameof(IsSelectable), typeof(bool), typeof(CropBox), new PropertyMetadata(new PropertyChangedCallback(IsSelectablePropertyChanged)));
		public static readonly DependencyProperty AreHandlesVisibleProperty = DependencyProperty.Register(nameof(AreHandlesVisible), typeof(bool), typeof(CropBox), (PropertyMetadata)new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnPropertyChanged)));
		public static readonly DependencyProperty RotationProperty = DependencyProperty.Register(nameof(Rotation), typeof(double), typeof(CropBox), new PropertyMetadata(new PropertyChangedCallback(RotationPropertyChanged)));

		public Matrix ViewerTransformMatrix
		{
			get => (Matrix)GetValue(ViewerTransformMatrixProperty);
			set => SetValue(ViewerTransformMatrixProperty, value);
		}

		public double CropLeft
		{
			get => (double)GetValue(CropLeftProperty);
			set => SetValue(CropLeftProperty, value);
		}

		public double CropTop
		{
			get => (double)GetValue(CropTopProperty);
			set => SetValue(CropTopProperty, value);
		}

		public double CropRight
		{
			get => (double)GetValue(CropRightProperty);
			set => SetValue(CropRightProperty, value);
		}

		public double CropBottom
		{
			get => (double)GetValue(CropBottomProperty);
			set => SetValue(CropBottomProperty, value);
		}

		public int BitmapWidth
		{
			get => (int)GetValue(BitmapWidthProperty);
			set => SetValue(BitmapWidthProperty, value);
		}

		public int BitmapHeight
		{
			get => (int)GetValue(BitmapHeightProperty);
			set => SetValue(BitmapHeightProperty, value);
		}

		public bool IsSelectable
		{
			get => (bool)GetValue(IsSelectableProperty);
			set => SetValue(IsSelectableProperty, value);
		}

		private static void IsSelectablePropertyChanged(
		  DependencyObject obj,
		  DependencyPropertyChangedEventArgs e)
		{
			((CropBox)obj).IsSelectableChanged();
		}

		public bool AreHandlesVisible
		{
			get => (bool)GetValue(AreHandlesVisibleProperty);
			set => SetValue(AreHandlesVisibleProperty, value);
		}

		public double Rotation
		{
			get => (double)GetValue(RotationProperty);
			set => SetValue(RotationProperty, value);
		}

		private static void RotationPropertyChanged(
		  DependencyObject obj,
		  DependencyPropertyChangedEventArgs e)
		{
			((CropBox)obj).RotationChanged();
		}


		public CropBox()
		{
			InitializeComponent();
			foreach (Thumb thumb in gridCropBox.Children.OfType<Thumb>())
			{
				thumb.DragCompleted += new DragCompletedEventHandler(Thumb_DragCompleted);
			}
		}

		private void IsSelectableChanged()
		{
			System.Windows.Input.Cursor sizeAll;
			Brush transparent;
			Thumb thumb = moveThumb;
			if (!IsSelectable)
			{
				sizeAll = Cursors.SizeAll;
			}
			else if (AreHandlesVisible)
			{
				sizeAll = Cursors.SizeAll;
			}
			else
			{
				sizeAll = null;
			}
			thumb.Cursor = sizeAll;
			Thumb thumb1 = moveThumb;
			if (IsSelectable)
			{
				transparent = Brushes.Transparent;
			}
			else
			{
				transparent = null;
			}
			thumb1.Background = transparent;
		}

	
		private void MoveThumb_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (IsSelectable)
			{
				AreHandlesVisible = true;
			}
		}

		private static void OnPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			CropBox cropBox = obj as CropBox;
			if (cropBox != null)
			{
				cropBox.UpdateHandles();
			}
		}

		private void RectangleMove(object sender, DragDeltaEventArgs e)
		{
			Matrix viewerTransformMatrix = ViewerTransformMatrix;
			viewerTransformMatrix.Invert();
			Vector vector = new Vector(e.HorizontalChange, e.VerticalChange) * viewerTransformMatrix;
			vector.X = Math.Max(0.0 - CropLeft, Math.Min(vector.X, (double)BitmapWidth - CropRight));
			vector.Y = Math.Max(0.0 - CropTop, Math.Min(vector.Y, (double)BitmapHeight - CropBottom));
			if (vector.X > 0.0)
			{
				CropRight += vector.X;
				CropLeft += vector.X;
			}
			else
			{
				CropLeft += vector.X;
				CropRight += vector.X;
			}
			if (vector.Y > 0.0)
			{
				CropBottom += vector.Y;
				CropTop += vector.Y;
			}
			else
			{
				CropTop += vector.Y;
				CropBottom += vector.Y;
			}
		}

		private void RotationChanged()
		{
			bool flag = (int)Math.Round(Rotation / 90.0) % 2 != 0;
			Cursor[] cursorArray = new Cursor[4]
			{
				Cursors.SizeNWSE,
				Cursors.SizeNS,
				Cursors.SizeNESW,
				Cursors.SizeWE
			};
			int index = flag ? 2 : 0;
			thumb1.Cursor = thumb8.Cursor = cursorArray[index];
			thumb2.Cursor = thumb7.Cursor = cursorArray[index + 1];
			thumb3.Cursor = thumb6.Cursor = cursorArray[(index + 2) % 4];
			thumb4.Cursor = thumb5.Cursor = cursorArray[(index + 3) % 4];
		}



		private void SetVisible(Thumb thumb, bool isVisible)
		{
			thumb.Visibility = (thumb.IsMouseCaptured || AreHandlesVisible && isVisible ? Visibility.Visible : Visibility.Collapsed);
		}

		private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
		{
			if (e.HorizontalChange != 0 || e.VerticalChange != 0)
			{
				EventHandler eventHandler = DragCompleted;
				if (eventHandler != null)
				{
					eventHandler(this, EventArgs.Empty);
				}
			}
		}

		private void ThumbMove(object sender, DragDeltaEventArgs e)
		{
			Thumb thumb = (Thumb)sender;
			Matrix viewerTransformMatrix = ViewerTransformMatrix;
			viewerTransformMatrix.Invert();
			Vector vector = new Vector(e.HorizontalChange, e.VerticalChange) * viewerTransformMatrix;
			if (thumb.HorizontalAlignment == HorizontalAlignment.Left)
			{
				CropLeft = Math.Max(0.0, Math.Min(CropLeft + vector.X, CropRight - 1.0));



			}
			else if (thumb.HorizontalAlignment == HorizontalAlignment.Right)

			{
				CropRight = Math.Max(CropLeft + 1.0, Math.Min(CropRight + vector.X, BitmapWidth));
			}
			if (thumb.VerticalAlignment == VerticalAlignment.Top)
			{
				CropTop = Math.Max(0.0, Math.Min(CropTop + vector.Y, CropBottom - 1.0));

			}
			else if (thumb.VerticalAlignment == VerticalAlignment.Bottom)
			{
				CropBottom = Math.Max(CropTop + 1.0, Math.Min(CropBottom + vector.Y, BitmapHeight));
			}
		}

		private void UpdateHandles()
		{
			Point point = new Point(CropLeft, CropTop) * ViewerTransformMatrix;
			Point point2 = new Point(CropRight, CropBottom) * ViewerTransformMatrix;
			Canvas.SetLeft(gridCropBox, point.X);
			Canvas.SetTop(gridCropBox, point.Y);
			double num = Math.Max(1.0, point2.X - point.X);
			double num2 = Math.Max(1.0, point2.Y - point.Y);
			gridCropBox.Width = num;
			gridCropBox.Height = num2;
			SetVisible(thumb1, true);
			SetVisible(thumb2, num > 15.0);
			SetVisible(thumb3, true);
			SetVisible(thumb4, num2 > 15.0);
			SetVisible(thumb5, num2 > 15.0);
			SetVisible(thumb6, true);
			SetVisible(thumb7, num > 15.0);
			SetVisible(thumb8, true);
			moveThumb.Cursor = ((!IsSelectable) ? Cursors.SizeAll : (AreHandlesVisible ? Cursors.SizeAll : null));
		}

		public event EventHandler DragCompleted;
	}
}