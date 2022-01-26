using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using Microsoft.Research.ICE.Controls;
using Microsoft.Research.ICE.Converters;
using Microsoft.Research.ICE.Helpers;
using Microsoft.Research.ICE.Stitching;
using Microsoft.Research.ICE.UserInterface;
using Microsoft.Research.ICE.ViewModels;
using Microsoft.Research.VisionTools.Toolkit.Desktop.Telemetry;

namespace Microsoft.Research.ICE.ImportViews;

public class StructuredImportView : UserControl, IComponentConnector
{
	private const double ImageMargin = 10.0;

	private const double ImageGap = 2.0;

	private CommandBinding deleteCommandBinding;

	private List<StructuredPanoramaImage> images;

	private List<StructuredPanoramaImage> seamImages;

	private SourceFileViewModel anchorSourceFile;

	private DispatcherTimer overlapBlinkTimer;

	private DispatcherOperation arrangeOperation;

	private MultiBinding multibinding;

	private StructuredImportViewModel viewModel;

	internal Canvas imageCanvas;

	private bool _contentLoaded;

	public StructuredImportViewModel ViewModel => viewModel;

	public StructuredImportView()
	{
		deleteCommandBinding = new CommandBinding(ApplicationCommands.Delete, RemoveSelectedImages, CanRemoveSelectedImages);
		InitializeComponent();
		new DragDropHelper(imageCanvas, HandleDrop);
		images = new List<StructuredPanoramaImage>();
		seamImages = new List<StructuredPanoramaImage>();
		overlapBlinkTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(0.5)
		};
		overlapBlinkTimer.Tick += OverlapBlinkTimer_Tick;
		multibinding = new MultiBinding();
		multibinding.Bindings.Add(new Binding("DataContext.IsReadingThumbnails")
		{
			Source = this
		});
		multibinding.Bindings.Add(new Binding("IsMouseOver")
		{
			Source = imageCanvas
		});
		multibinding.Bindings.Add(new Binding("DataContext.PreviewOverlap")
		{
			Source = this
		});
		multibinding.Converter = new FirstOrSecondButNotThirdBooleanConverter();
		base.Loaded += StructuredImportView_Loaded;
		base.Unloaded += StructuredImportView_Unloaded;
		base.DataContextChanged += StructuredImportView_DataContextChanged;
	}

	private void StructuredImportView_Loaded(object sender, RoutedEventArgs e)
	{
		Application.Current.MainWindow.CommandBindings.Replace(deleteCommandBinding);
		ArrangeImages();
	}

	private void StructuredImportView_Unloaded(object sender, RoutedEventArgs e)
	{
		Application.Current.MainWindow.CommandBindings.Remove(deleteCommandBinding);
		if (viewModel != null)
		{
			viewModel.ImageArrangementChanged -= ViewModel_ImageArrangementChanged;
			viewModel.PreviewOverlap = false;
		}
		overlapBlinkTimer.Stop();
	}

	private void StructuredImportView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (viewModel != null)
		{
			viewModel.ImageArrangementChanged -= ViewModel_ImageArrangementChanged;
		}
		viewModel = e.NewValue as StructuredImportViewModel;
		if (viewModel != null)
		{
			viewModel.ImageArrangementChanged += ViewModel_ImageArrangementChanged;
		}
	}

	private void ViewModel_ImageArrangementChanged(object sender, EventArgs e)
	{
		if (arrangeOperation == null)
		{
			arrangeOperation = base.Dispatcher.BeginInvoke((Action)delegate
			{
				ArrangeImages();
			}, DispatcherPriority.Background);
		}
	}

	private void ImageCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		ArrangeImages();
	}

	private void OverlapBlinkTimer_Tick(object sender, EventArgs e)
	{
		foreach (UIElement child in imageCanvas.Children)
		{
			Panel.SetZIndex(child, -Panel.GetZIndex(child));
		}
	}

	private void ArrangeImages()
	{
		if (ViewModel == null)
		{
			return;
		}
		int imageCount = ViewModel.ImageCount;
		Size renderSize = imageCanvas.RenderSize;
		if (base.IsLoaded && renderSize.Width > 0.0 && renderSize.Height > 0.0 && imageCount > 0)
		{
			double num = 2.0 * Math.Min(1.0, ViewModel.AverageAspectRatio);
			double num2 = 2.0 * Math.Min(1.0, 1.0 / ViewModel.AverageAspectRatio);
			double num3 = num;
			double num4 = num2;
			int columnCount = ViewModel.ColumnCount;
			int rowCount = ViewModel.RowCount;
			double num5 = num * (double)columnCount;
			double num6 = num2 * (double)rowCount;
			if (ViewModel.PreviewOverlap)
			{
				double num7 = num * ViewModel.HorizontalOverlap / 100.0;
				double num8 = num2 * ViewModel.VerticalOverlap / 100.0;
				num5 -= (double)(columnCount - 1) * num7;
				num6 -= (double)(rowCount - 1) * num8;
				num3 -= num7;
				num4 -= num8;
			}
			double num9 = (ViewModel.PreviewOverlap ? 0.0 : ((double)(columnCount - 1) * 2.0));
			double num10 = (ViewModel.PreviewOverlap ? 0.0 : ((double)(rowCount - 1) * 2.0));
			double num11 = ((ViewModel.AngularRange == AngularRange.Horizontal360) ? 30.0 : 10.0);
			double num12 = ((ViewModel.AngularRange == AngularRange.Vertical360) ? 30.0 : 10.0);
			Size size = new Size(Math.Max(0.0, renderSize.Width - 2.0 * num11 - num9), Math.Max(0.0, renderSize.Height - 2.0 * num12 - num10));
			double num13 = Math.Min(size.Width / num5, size.Height / num6);
			num5 = num5 * num13 + num9;
			num6 = num6 * num13 + num10;
			double num14 = (renderSize.Width - num5) / 2.0;
			double num15 = (renderSize.Height - num6) / 2.0;
			num *= num13;
			num2 *= num13;
			num3 = num3 * num13 + (ViewModel.PreviewOverlap ? 0.0 : 2.0);
			num4 = num4 * num13 + (ViewModel.PreviewOverlap ? 0.0 : 2.0);
			double num16 = num + (ViewModel.PreviewOverlap ? ((0.0 - num) * ViewModel.SeamOverlap / 100.0) : 2.0);
			double num17 = num2 + (ViewModel.PreviewOverlap ? ((0.0 - num2) * ViewModel.SeamOverlap / 100.0) : 2.0);
			AdjustImageListSize(images);
			AdjustImageListSize(seamImages);
			for (int i = 0; i < imageCount; i++)
			{
				SourceFileViewModel sourceFileViewModel = ViewModel.SortedSourceFiles[i];
				int column = sourceFileViewModel.GridColumn;
				int row = sourceFileViewModel.GridRow;
				StructuredPanoramaImage orCreateImage = GetOrCreateImage(images, i);
				orCreateImage.Width = num;
				orCreateImage.Height = num2;
				Canvas.SetLeft(orCreateImage, num14 + (double)column * num3);
				Canvas.SetTop(orCreateImage, num15 + (double)row * num4);
				Panel.SetZIndex(orCreateImage, column % 2 + 2 * (row % 2));
				if (ShouldShowSeamImage(ref column, ref row))
				{
					double length = num14 + (double)column * num3;
					if (column == -1)
					{
						length = num14 - num16;
					}
					else if (column == ViewModel.ColumnCount)
					{
						length = num14 + (double)(column - 1) * num3 + num16;
					}
					double length2 = num15 + (double)row * num4;
					if (row == -1)
					{
						length2 = num15 - num17;
					}
					else if (row == ViewModel.RowCount)
					{
						length2 = num15 + (double)(row - 1) * num4 + num17;
					}
					orCreateImage = GetOrCreateImage(seamImages, i);
					Canvas.SetLeft(orCreateImage, length);
					Canvas.SetTop(orCreateImage, length2);
					orCreateImage.Width = num;
					orCreateImage.Height = num2;
					Panel.SetZIndex(orCreateImage, (column + 2) % 2 + 2 * ((row + 2) % 2));
				}
				else if (seamImages[i] != null)
				{
					imageCanvas.Children.Remove(seamImages[i]);
					BindingOperations.ClearAllBindings(seamImages[i]);
					seamImages[i] = null;
				}
			}
		}
		overlapBlinkTimer.IsEnabled = ViewModel.PreviewOverlap;
		arrangeOperation = null;
	}

	private void AdjustImageListSize(List<StructuredPanoramaImage> imageList)
	{
		int imageCount = ViewModel.ImageCount;
		if (imageList.Count > imageCount)
		{
			for (int num = imageList.Count - 1; num >= imageCount; num--)
			{
				if (imageList[num] != null)
				{
					imageCanvas.Children.Remove(imageList[num]);
					BindingOperations.ClearAllBindings(imageList[num]);
				}
			}
			imageList.RemoveRange(imageCount, imageList.Count - imageCount);
		}
		else if (imageList.Count < imageCount)
		{
			imageList.AddRange(Enumerable.Repeat<StructuredPanoramaImage>(null, imageCount - imageList.Count));
		}
	}

	private StructuredPanoramaImage GetOrCreateImage(List<StructuredPanoramaImage> imageList, int i)
	{
		SourceFileViewModel dataContext = ViewModel.SortedSourceFiles[i];
		StructuredPanoramaImage structuredPanoramaImage = imageList[i];
		if (structuredPanoramaImage == null)
		{
			structuredPanoramaImage = new StructuredPanoramaImage();
			structuredPanoramaImage.MouseLeftButtonDown += Image_MouseLeftButtonDown;
			structuredPanoramaImage.DataContext = dataContext;
			structuredPanoramaImage.ImageNumber = i + 1;
			structuredPanoramaImage.SetBinding(FrameworkElement.ToolTipProperty, new Binding("FilePath"));
			structuredPanoramaImage.SetBinding(StructuredPanoramaImage.IsShowingNumberProperty, multibinding);
			structuredPanoramaImage.SetBinding(StructuredPanoramaImage.ThumbnailImageProperty, new Binding("Thumbnail"));
			structuredPanoramaImage.SetBinding(StructuredPanoramaImage.ShowBorderProperty, new Binding("DataContext.PreviewOverlap")
			{
				Source = this,
				Converter = new NegatedBooleanConverter()
			});
			structuredPanoramaImage.SetBinding(StructuredPanoramaImage.IsSelectedProperty, new Binding("IsSelected"));
			imageCanvas.Children.Add(structuredPanoramaImage);
			imageList[i] = structuredPanoramaImage;
		}
		else
		{
			structuredPanoramaImage.DataContext = dataContext;
		}
		return structuredPanoramaImage;
	}

	private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (ViewModel.PreviewOverlap)
		{
			return;
		}
		StructuredPanoramaImage structuredPanoramaImage = (StructuredPanoramaImage)sender;
		SourceFileViewModel sourceFileViewModel = (SourceFileViewModel)structuredPanoramaImage.DataContext;
		if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
		{
			sourceFileViewModel.IsSelected = !sourceFileViewModel.IsSelected;
			anchorSourceFile = sourceFileViewModel;
		}
		else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && ViewModel.SortedSourceFiles.Contains(anchorSourceFile))
		{
			int num = ViewModel.SortedSourceFiles.IndexOf(anchorSourceFile);
			int num2 = ViewModel.SortedSourceFiles.IndexOf(sourceFileViewModel);
			if (num >= 0 && num2 >= 0)
			{
				int num3 = Math.Min(num, num2);
				int num4 = Math.Max(num, num2);
				for (int i = 0; i < ViewModel.SortedSourceFiles.Count; i++)
				{
					ViewModel.SortedSourceFiles[i].IsSelected = num3 <= i && i <= num4;
				}
			}
		}
		else
		{
			foreach (SourceFileViewModel sortedSourceFile in ViewModel.SortedSourceFiles)
			{
				sortedSourceFile.IsSelected = sortedSourceFile == sourceFileViewModel;
			}
			anchorSourceFile = sourceFileViewModel;
		}
		CommandManager.InvalidateRequerySuggested();
		e.Handled = true;
	}

	private bool ShouldShowSeamImage(ref int column, ref int row)
	{
		if (ViewModel.AngularRange == AngularRange.Horizontal360)
		{
			if (column == 0)
			{
				column = ViewModel.ColumnCount;
				return true;
			}
			if (column == ViewModel.ColumnCount - 1)
			{
				column = -1;
				return true;
			}
		}
		else if (ViewModel.AngularRange == AngularRange.Vertical360)
		{
			if (row == 0)
			{
				row = ViewModel.RowCount;
				return true;
			}
			if (row == ViewModel.RowCount - 1)
			{
				row = -1;
				return true;
			}
		}
		return false;
	}

	private void CanRemoveSelectedImages(object sender, CanExecuteRoutedEventArgs e)
	{
		e.CanExecute = ViewModel.SortedSourceFiles.Any((SourceFileViewModel s) => s.IsSelected);
		e.Handled = true;
	}

	private void RemoveSelectedImages(object sender, ExecutedRoutedEventArgs e)
	{
		SourceFileViewModel[] array = ViewModel.SortedSourceFiles.Where((SourceFileViewModel s) => s.IsSelected).ToArray();
		Track.Event("remove structured images", null, new Dictionary<string, double> { { "images", array.Length } });
		int count = ViewModel.SortedSourceFiles.IndexOf(array.Last());
		SourceFileViewModel sourceFileViewModel = ViewModel.SortedSourceFiles.Take(count).LastOrDefault((SourceFileViewModel s) => !s.IsSelected);
		MainWindow.Instance.ViewModel.RemoveImages(array);
		if (sourceFileViewModel != null)
		{
			sourceFileViewModel.IsSelected = true;
		}
		e.Handled = true;
	}

	private void HandleDrop(IEnumerable<string> imageFiles)
	{
		Track.Event("add structured images from drag-and-drop", null, new Dictionary<string, double> { 
		{
			"images",
			imageFiles.Count()
		} });
		MainWindow.Instance.ViewModel.ImportImages(imageFiles);
		foreach (SourceFileViewModel sortedSourceFile in ViewModel.SortedSourceFiles)
		{
			sortedSourceFile.IsSelected = imageFiles.Contains(sortedSourceFile.FilePath);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/ICE;component/importviews/structuredimportview.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	[DebuggerNonUserCode]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			imageCanvas = (Canvas)target;
			imageCanvas.SizeChanged += ImageCanvas_SizeChanged;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
