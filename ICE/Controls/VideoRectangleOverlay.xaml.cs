using Microsoft.Research.ICE.ViewModels;
using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Microsoft.Research.ICE.Controls
{
	public partial class VideoRectangleOverlay : UserControl
	{
		public readonly static DependencyProperty VideoWidthProperty;

		public readonly static DependencyProperty VideoHeightProperty;

		private readonly static DependencyPropertyKey VideoTransformMatrixPropertyKey;

		public readonly static DependencyProperty VideoTransformMatrixProperty;

		public int VideoHeight
		{
			get
			{
				return (int)GetValue(VideoHeightProperty);
			}
			set
			{
                SetValue(VideoHeightProperty, value);
			}
		}

		public Matrix VideoTransformMatrix
		{
			get
			{
				return (Matrix)GetValue(VideoTransformMatrixProperty);
			}
			private set
			{
                SetValue(VideoTransformMatrixPropertyKey, value);
			}
		}

		public int VideoWidth
		{
			get
			{
				return (int)GetValue(VideoWidthProperty);
			}
			set
			{
                SetValue(VideoWidthProperty, value);
			}
		}

		public VideoImportViewModel ViewModel
		{
			get
			{
				return DataContext as VideoImportViewModel;
			}
		}

		static VideoRectangleOverlay()
		{
            VideoWidthProperty = DependencyProperty.Register("VideoWidth", typeof(int), typeof(VideoRectangleOverlay));
            VideoHeightProperty = DependencyProperty.Register("VideoHeight", typeof(int), typeof(VideoRectangleOverlay));
            VideoTransformMatrixPropertyKey = DependencyProperty.RegisterReadOnly("VideoTransformMatrix", typeof(Matrix), typeof(VideoRectangleOverlay), new PropertyMetadata());
            VideoTransformMatrixProperty = VideoTransformMatrixPropertyKey.DependencyProperty;
		}

		public VideoRectangleOverlay()
		{
			InitializeComponent();
			transformedRectangle.SizeChanged += new SizeChangedEventHandler((object param0, SizeChangedEventArgs param1) => UpdateVideoTransformMatrix());
		}

		private void CropBox_DragCompleted(object sender, EventArgs e)
		{
			
			VideoRectangleViewModel dataContext = (VideoRectangleViewModel)((FrameworkElement)sender).DataContext;
			ViewModel.UpdateVideoRectangleImage(dataContext, DirtyFlags.CompositingAndBeyond);
		}

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
			UpdateVideoTransformMatrix();
		}

		private void UpdateVideoTransformMatrix()
		{
			Transform ancestor = transformedRectangle.TransformToAncestor(this) as Transform;
			if (ancestor != null)
			{
				VideoTransformMatrix = ancestor.Value;
			}
		}
	}
}