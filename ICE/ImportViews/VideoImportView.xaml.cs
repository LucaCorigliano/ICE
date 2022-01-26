using Microsoft.Research.ICE.Controls;
using Microsoft.Research.ICE.ViewModels;
using Microsoft.Research.VisionTools.Toolkit.Desktop;
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;

namespace Microsoft.Research.ICE.ImportViews
{
	public partial class VideoImportView : UserControl
	{
		private bool isRectangleCreationEnabled;

		private bool isDragging;

		private VideoRectangleViewModel newVideoRectangle;

		private Point dragStartPosition;

		private BindableMediaElement MediaElement
		{
			get
			{
				return videoTrimmer.MediaElement;
			}
		}

		private VideoImportViewModel ViewModel
		{
			get
			{
				return DataContext as VideoImportViewModel;
			}
		}

		public VideoImportView()
		{
			InitializeComponent();
            Unloaded += new RoutedEventHandler(VideoImportView_Unloaded);
			videoTrimmer.AddHandler(BindableMediaElement.MediaOpenedEvent, new RoutedEventHandler(MediaElement_MediaOpened));
			videoTrimmer.AddHandler(BindableMediaElement.MediaFailedEvent, new EventHandler<MediaFailedEventArgs>(MediaElement_MediaFailed));
			videoTrimmer.AddHandler(Selector.SelectionChangedEvent, new SelectionChangedEventHandler(VideoTimeline_SelectionChanged));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, new ExecutedRoutedEventHandler(DeleteCommand_Executed), new CanExecuteRoutedEventHandler(DeleteCommand_CanExecute)));
		}

		private void DeleteCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = (ViewModel == null ? false : ViewModel.SelectedVideoRectangles.Count<VideoRectangleViewModel>() > 0);
		}

		private void DeleteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (ViewModel != null)
			{

				ViewModel.RemoveSelectedVideoRectangles();
			}
		}

		private void DisableRectangleCreation()
		{
			if (isRectangleCreationEnabled)
			{
				MediaElement.MouseLeftButtonDown -= new MouseButtonEventHandler(MediaElement_MouseLeftButtonDown);
				MediaElement.MouseMove -= new MouseEventHandler(MediaElement_MouseMove);
				MediaElement.MouseLeftButtonUp -= new MouseButtonEventHandler(MediaElement_MouseLeftButtonUp);
				MediaElement.LostMouseCapture -= new MouseEventHandler(MediaElement_LostMouseCapture);
				isRectangleCreationEnabled = false;
			}
		}

		private void EnableRectangleCreation()
		{
			if (!isRectangleCreationEnabled)
			{
				MediaElement.MouseLeftButtonDown += new MouseButtonEventHandler(MediaElement_MouseLeftButtonDown);
				MediaElement.MouseMove += new MouseEventHandler(MediaElement_MouseMove);
				MediaElement.MouseLeftButtonUp += new MouseButtonEventHandler(MediaElement_MouseLeftButtonUp);
				MediaElement.LostMouseCapture += new MouseEventHandler(MediaElement_LostMouseCapture);
				isRectangleCreationEnabled = true;
			}
		}

		private void MediaElement_LostMouseCapture(object sender, MouseEventArgs e)
		{
			if (newVideoRectangle != null)
			{
				ViewModel.UpdateVideoRectangleImage(newVideoRectangle, DirtyFlags.None);
				newVideoRectangle = null;
			}
			isDragging = false;
		}

		private void MediaElement_MediaFailed(object sender, MediaFailedEventArgs e)
		{
			Dictionary<string, string> strs = new Dictionary<string, string>()
			{
				{ "format", Path.GetExtension(ViewModel.MediaFilename) }
			};

			ApplicationException errorException = e.ErrorException as ApplicationException;
			if (errorException == null)
			{
				ViewModel.ErrorMessage = "Cannot open video file.";
				return;
			}
			ViewModel.ErrorMessage = errorException.Message;
		}

		private void MediaElement_MediaOpened(object sender, EventArgs e)
		{
			Dictionary<string, string> strs = new Dictionary<string, string>()
			{
				{ "format", Path.GetExtension(ViewModel.MediaFilename) }
			};
			EnableRectangleCreation();
		}

		private void MediaElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			isDragging = MediaElement.CaptureMouse();
			dragStartPosition = e.GetPosition(videoRectangleOverlay.transformedRectangle);
		}

		private void MediaElement_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (isDragging)
			{
				MediaElement.ReleaseMouseCapture();
			}
		}

		private void MediaElement_MouseMove(object sender, MouseEventArgs e)
		{
			if (isDragging)
			{
				Point position = e.GetPosition(videoRectangleOverlay.transformedRectangle);
				if (newVideoRectangle != null || Math.Abs(position.X - dragStartPosition.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(position.Y - dragStartPosition.Y) > SystemParameters.MinimumVerticalDragDistance)
				{
					if (newVideoRectangle == null)
					{
						VideoRectangleViewModel videoRectangleViewModel = new VideoRectangleViewModel()
						{
							Time = ViewModel.CurrentTime,
							IsSelected = true
						};
						newVideoRectangle = videoRectangleViewModel;
						ViewModel.AddVideoRectangle(newVideoRectangle);
					}
					UpdateVideoRectangle(position);
				}
			}
		}

		private void RectangleListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ViewModel != null && e.AddedItems != null && e.AddedItems.Count > 0)
			{
				VideoRectangleViewModel item = e.AddedItems[0] as VideoRectangleViewModel;
				if (item != null)
				{
					ViewModel.CurrentTime = item.Time;
					rectangleListBox.ScrollIntoView(item);
				}
			}
		}

		private void UpdateVideoRectangle(Point position)
		{
			int num = (int)Math.Min(dragStartPosition.X, position.X);
			int num1 = (int)Math.Min(dragStartPosition.Y, position.Y);
			int num2 = (int)Math.Max(dragStartPosition.X, position.X);
			int num3 = (int)Math.Max(dragStartPosition.Y, position.Y);
			if (num == num2)
			{
				num2++;
			}
			if (num1 == num3)
			{
				num3++;
			}
			num = Math.Max(0, Math.Min(num, ViewModel.RawWidth - 1));
			num1 = Math.Max(0, Math.Min(num1, ViewModel.RawHeight - 1));
			num2 = Math.Max(1, Math.Min(num2, ViewModel.RawWidth));
			num3 = Math.Max(1, Math.Min(num3, ViewModel.RawHeight));
			if (num == num2)
			{
				num--;
			}
			if (num1 == num3)
			{
				num3--;
			}
			newVideoRectangle.Left = num;
			newVideoRectangle.Top = num1;
			newVideoRectangle.Right = num2;
			newVideoRectangle.Bottom = num3;
		}

		private void VideoImportView_Unloaded(object sender, RoutedEventArgs e)
		{
			DisableRectangleCreation();
		}

		private void VideoTimeline_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ViewModel != null && e.AddedItems != null && e.AddedItems.Count > 0 && e.AddedItems[0] is TimeSpan)
			{
				ViewModel.CurrentTime = (TimeSpan)e.AddedItems[0];
				if (!ViewModel.SelectedVideoRectangles.Any<VideoRectangleViewModel>((VideoRectangleViewModel r) => r.Time == MediaElement.CurrentTime))
				{
					VideoRectangleViewModel videoRectangleViewModel = ViewModel.VideoRectangles.FirstOrDefault<VideoRectangleViewModel>((VideoRectangleViewModel r) => r.Time == MediaElement.CurrentTime);
					if (videoRectangleViewModel != null)
					{
						videoRectangleViewModel.IsSelected = true;
					}
				}
			}
		}
	}
}