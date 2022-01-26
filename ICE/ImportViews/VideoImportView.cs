using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using Microsoft.Research.ICE.Controls;
using Microsoft.Research.ICE.ViewModels;
using Microsoft.Research.VisionTools.Toolkit.Desktop;
using Microsoft.Research.VisionTools.Toolkit.Desktop.Telemetry;

namespace Microsoft.Research.ICE.ImportViews;

public class VideoImportView : UserControl, IComponentConnector
{
	private bool isRectangleCreationEnabled;

	private bool isDragging;

	private VideoRectangleViewModel newVideoRectangle;

	private Point dragStartPosition;

	internal VideoTrimmer videoTrimmer;

	internal VideoRectangleOverlay videoRectangleOverlay;

	internal Button deleteButton;

	internal Grid rectangleArea;

	internal ListBox rectangleListBox;

	private bool _contentLoaded;

	private VideoImportViewModel ViewModel => base.DataContext as VideoImportViewModel;

	private BindableMediaElement MediaElement => videoTrimmer.MediaElement;

	public VideoImportView()
	{
		InitializeComponent();
		base.Unloaded += VideoImportView_Unloaded;
		videoTrimmer.AddHandler(BindableMediaElement.MediaOpenedEvent, new RoutedEventHandler(MediaElement_MediaOpened));
		videoTrimmer.AddHandler(BindableMediaElement.MediaFailedEvent, new EventHandler<MediaFailedEventArgs>(MediaElement_MediaFailed));
		videoTrimmer.AddHandler(Selector.SelectionChangedEvent, new SelectionChangedEventHandler(VideoTimeline_SelectionChanged));
		base.CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, DeleteCommand_Executed, DeleteCommand_CanExecute));
	}

	private void VideoImportView_Unloaded(object sender, RoutedEventArgs e)
	{
		DisableRectangleCreation();
	}

	private void EnableRectangleCreation()
	{
		if (!isRectangleCreationEnabled)
		{
			MediaElement.MouseLeftButtonDown += MediaElement_MouseLeftButtonDown;
			MediaElement.MouseMove += MediaElement_MouseMove;
			MediaElement.MouseLeftButtonUp += MediaElement_MouseLeftButtonUp;
			MediaElement.LostMouseCapture += MediaElement_LostMouseCapture;
			isRectangleCreationEnabled = true;
		}
	}

	private void DisableRectangleCreation()
	{
		if (isRectangleCreationEnabled)
		{
			MediaElement.MouseLeftButtonDown -= MediaElement_MouseLeftButtonDown;
			MediaElement.MouseMove -= MediaElement_MouseMove;
			MediaElement.MouseLeftButtonUp -= MediaElement_MouseLeftButtonUp;
			MediaElement.LostMouseCapture -= MediaElement_LostMouseCapture;
			isRectangleCreationEnabled = false;
		}
	}

	private void MediaElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		isDragging = MediaElement.CaptureMouse();
		dragStartPosition = e.GetPosition(videoRectangleOverlay.transformedRectangle);
	}

	private void MediaElement_MouseMove(object sender, MouseEventArgs e)
	{
		if (!isDragging)
		{
			return;
		}
		Point position = e.GetPosition(videoRectangleOverlay.transformedRectangle);
		if (newVideoRectangle != null || Math.Abs(position.X - dragStartPosition.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(position.Y - dragStartPosition.Y) > SystemParameters.MinimumVerticalDragDistance)
		{
			if (newVideoRectangle == null)
			{
				newVideoRectangle = new VideoRectangleViewModel
				{
					Time = ViewModel.CurrentTime,
					IsSelected = true
				};
				ViewModel.AddVideoRectangle(newVideoRectangle);
			}
			UpdateVideoRectangle(position);
		}
	}

	private void MediaElement_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (isDragging)
		{
			MediaElement.ReleaseMouseCapture();
		}
	}

	private void MediaElement_LostMouseCapture(object sender, MouseEventArgs e)
	{
		if (newVideoRectangle != null)
		{
			Microsoft.Research.VisionTools.Toolkit.Desktop.Telemetry.Track.Event("video rectangle created");
			ViewModel.UpdateVideoRectangleImage(newVideoRectangle, DirtyFlags.None);
			newVideoRectangle = null;
		}
		isDragging = false;
	}

	private void UpdateVideoRectangle(Point position)
	{
		int num = (int)Math.Min(dragStartPosition.X, position.X);
		int num2 = (int)Math.Min(dragStartPosition.Y, position.Y);
		int num3 = (int)Math.Max(dragStartPosition.X, position.X);
		int num4 = (int)Math.Max(dragStartPosition.Y, position.Y);
		if (num == num3)
		{
			num3++;
		}
		if (num2 == num4)
		{
			num4++;
		}
		num = Math.Max(0, Math.Min(num, ViewModel.RawWidth - 1));
		num2 = Math.Max(0, Math.Min(num2, ViewModel.RawHeight - 1));
		num3 = Math.Max(1, Math.Min(num3, ViewModel.RawWidth));
		num4 = Math.Max(1, Math.Min(num4, ViewModel.RawHeight));
		if (num == num3)
		{
			num--;
		}
		if (num2 == num4)
		{
			num4--;
		}
		newVideoRectangle.Left = num;
		newVideoRectangle.Top = num2;
		newVideoRectangle.Right = num3;
		newVideoRectangle.Bottom = num4;
	}

	private void MediaElement_MediaOpened(object sender, EventArgs e)
	{
		Microsoft.Research.VisionTools.Toolkit.Desktop.Telemetry.Track.Event("video opened", new Dictionary<string, string> { 
		{
			"format",
			Path.GetExtension(ViewModel.MediaFilename)
		} });
		EnableRectangleCreation();
	}

	private void MediaElement_MediaFailed(object sender, MediaFailedEventArgs e)
	{
		Microsoft.Research.VisionTools.Toolkit.Desktop.Telemetry.Track.Event("video failed to open", new Dictionary<string, string> { 
		{
			"format",
			Path.GetExtension(ViewModel.MediaFilename)
		} });
		if (e.ErrorException != null)
		{
			Microsoft.Research.VisionTools.Toolkit.Desktop.Telemetry.Track.Exception(e.ErrorException);
		}
		if (e.ErrorException is ApplicationException ex)
		{
			ViewModel.ErrorMessage = ex.Message;
		}
		else
		{
			ViewModel.ErrorMessage = "Cannot open video file.";
		}
	}

	private void VideoTimeline_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ViewModel == null || e.AddedItems == null || e.AddedItems.Count <= 0 || !(e.AddedItems[0] is TimeSpan))
		{
			return;
		}
		ViewModel.CurrentTime = (TimeSpan)e.AddedItems[0];
		if (!ViewModel.SelectedVideoRectangles.Any((VideoRectangleViewModel r) => r.Time == MediaElement.CurrentTime))
		{
			VideoRectangleViewModel videoRectangleViewModel = ViewModel.VideoRectangles.FirstOrDefault((VideoRectangleViewModel r) => r.Time == MediaElement.CurrentTime);
			if (videoRectangleViewModel != null)
			{
				videoRectangleViewModel.IsSelected = true;
			}
		}
	}

	private void DeleteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
	{
		if (ViewModel != null)
		{
			Microsoft.Research.VisionTools.Toolkit.Desktop.Telemetry.Track.Event("video rectangle deleted");
			ViewModel.RemoveSelectedVideoRectangles();
		}
	}

	private void DeleteCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
	{
		e.CanExecute = ViewModel != null && ViewModel.SelectedVideoRectangles.Count() > 0;
	}

	private void RectangleListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ViewModel != null && e.AddedItems != null && e.AddedItems.Count > 0 && e.AddedItems[0] is VideoRectangleViewModel videoRectangleViewModel)
		{
			Microsoft.Research.VisionTools.Toolkit.Desktop.Telemetry.Track.Event("video rectangle selected");
			ViewModel.CurrentTime = videoRectangleViewModel.Time;
			rectangleListBox.ScrollIntoView(videoRectangleViewModel);
		}
	}

	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/ICE;component/importviews/videoimportview.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[DebuggerNonUserCode]
	internal Delegate _CreateDelegate(Type delegateType, string handler)
	{
		return Delegate.CreateDelegate(delegateType, this, handler);
	}

	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	[DebuggerNonUserCode]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			videoTrimmer = (VideoTrimmer)target;
			break;
		case 2:
			videoRectangleOverlay = (VideoRectangleOverlay)target;
			break;
		case 3:
			deleteButton = (Button)target;
			break;
		case 4:
			rectangleArea = (Grid)target;
			break;
		case 5:
			rectangleListBox = (ListBox)target;
			rectangleListBox.SelectionChanged += RectangleListBox_SelectionChanged;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
