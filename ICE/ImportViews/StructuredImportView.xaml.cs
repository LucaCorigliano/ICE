using Microsoft.Research.ICE.Controls;
using Microsoft.Research.ICE.Converters;
using Microsoft.Research.ICE.Helpers;
using Microsoft.Research.ICE.UserInterface;
using Microsoft.Research.ICE.ViewModels;
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;

namespace Microsoft.Research.ICE.ImportViews
{
	public partial class StructuredImportView : UserControl
	{
		private const double ImageMargin = 10;

		private const double ImageGap = 2;

		private CommandBinding deleteCommandBinding;

		private List<StructuredPanoramaImage> images;

		private List<StructuredPanoramaImage> seamImages;

		private SourceFileViewModel anchorSourceFile;

		private DispatcherTimer overlapBlinkTimer;

		private DispatcherOperation arrangeOperation;

		private MultiBinding multibinding;

		private StructuredImportViewModel viewModel;

		public StructuredImportViewModel ViewModel
		{
			get
			{
				return viewModel;
			}
		}

		public StructuredImportView()
		{
			deleteCommandBinding = new CommandBinding(ApplicationCommands.Delete, new ExecutedRoutedEventHandler(RemoveSelectedImages), new CanExecuteRoutedEventHandler(CanRemoveSelectedImages));
			InitializeComponent();
			DragDropHelper dragDropHelper = new DragDropHelper(imageCanvas, new ImagesDroppedCallback(HandleDrop));
			images = new List<StructuredPanoramaImage>();
			seamImages = new List<StructuredPanoramaImage>();
			DispatcherTimer dispatcherTimer = new DispatcherTimer()
			{
				Interval = TimeSpan.FromSeconds(0.5)
			};
			overlapBlinkTimer = dispatcherTimer;
			overlapBlinkTimer.Tick += new EventHandler(OverlapBlinkTimer_Tick);
			multibinding = new MultiBinding();
			multibinding.Bindings.Add(new Binding("DataContext.IsReadingThumbnails")
			{
				Source = this
			});
			Collection<BindingBase> bindings = multibinding.Bindings;
			Binding binding = new Binding("IsMouseOver")
			{
				Source = imageCanvas
			};
			bindings.Add(binding);
			multibinding.Bindings.Add(new Binding("DataContext.PreviewOverlap")
			{
				Source = this
			});
			multibinding.Converter = new FirstOrSecondButNotThirdBooleanConverter();
            Loaded += new RoutedEventHandler(StructuredImportView_Loaded);
            Unloaded += new RoutedEventHandler(StructuredImportView_Unloaded);
            DataContextChanged += new DependencyPropertyChangedEventHandler(StructuredImportView_DataContextChanged);
		}

		private void AdjustImageListSize(List<StructuredPanoramaImage> imageList)
		{
			int imageCount = ViewModel.ImageCount;
			if (imageList.Count <= imageCount)
			{
				if (imageList.Count < imageCount)
				{
					imageList.AddRange(Enumerable.Repeat<StructuredPanoramaImage>(null, imageCount - imageList.Count));
				}
				return;
			}
			for (int i = imageList.Count - 1; i >= imageCount; i--)
			{
				if (imageList[i] != null)
				{
					imageCanvas.Children.Remove(imageList[i]);
					BindingOperations.ClearAllBindings(imageList[i]);
				}
			}
			imageList.RemoveRange(imageCount, imageList.Count - imageCount);
		}

		private void ArrangeImages()
		{
			if (ViewModel == null)
			{
				return;
			}
			int imageCount = ViewModel.ImageCount;
			Size renderSize = imageCanvas.RenderSize;
			if (IsLoaded && renderSize.Width > 0 && renderSize.Height > 0 && imageCount > 0)
			{
				double num = 2 * Math.Min(1, ViewModel.AverageAspectRatio);
				double num1 = 2 * Math.Min(1, 1 / ViewModel.AverageAspectRatio);
				double num2 = num;
				double num3 = num1;
				int columnCount = ViewModel.ColumnCount;
				int rowCount = ViewModel.RowCount;
				double num4 = num * (double)columnCount;
				double num5 = num1 * (double)rowCount;
				if (ViewModel.PreviewOverlap)
				{
					double horizontalOverlap = num * ViewModel.HorizontalOverlap / 100;
					double verticalOverlap = num1 * ViewModel.VerticalOverlap / 100;
					num4 = num4 - (double)(columnCount - 1) * horizontalOverlap;
					num5 = num5 - (double)(rowCount - 1) * verticalOverlap;
					num2 -= horizontalOverlap;
					num3 -= verticalOverlap;
				}
				double num6 = (ViewModel.PreviewOverlap ? 0 : (double)(columnCount - 1) * 2);
				double num7 = (ViewModel.PreviewOverlap ? 0 : (double)(rowCount - 1) * 2);
				double num8 = (ViewModel.AngularRange == (Stitching.AngularRange)1 ? 30 : 10);
				double num9 = (ViewModel.AngularRange == (Stitching.AngularRange)2 ? 30 : 10);
				Size size = new Size(Math.Max(0, renderSize.Width - 2 * num8 - num6), Math.Max(0, renderSize.Height - 2 * num9 - num7));
				double num10 = Math.Min(size.Width / num4, size.Height / num5);
				num4 = num4 * num10 + num6;
				num5 = num5 * num10 + num7;
				double width = (renderSize.Width - num4) / 2;
				double height = (renderSize.Height - num5) / 2;
				num *= num10;
				num1 *= num10;
				num2 = num2 * num10 + (ViewModel.PreviewOverlap ? 0 : 2);
				num3 = num3 * num10 + (ViewModel.PreviewOverlap ? 0 : 2);
				double num11 = num + (ViewModel.PreviewOverlap ? -num * ViewModel.SeamOverlap / 100 : 2);
				double num12 = num1 + (ViewModel.PreviewOverlap ? -num1 * ViewModel.SeamOverlap / 100 : 2);
				AdjustImageListSize(images);
				AdjustImageListSize(seamImages);
				for (int i = 0; i < imageCount; i++)
				{
					SourceFileViewModel item = ViewModel.SortedSourceFiles[i];
					int gridColumn = item.GridColumn;
					int gridRow = item.GridRow;
					StructuredPanoramaImage orCreateImage = GetOrCreateImage(images, i);
					orCreateImage.Width = num;
					orCreateImage.Height = num1;
					Canvas.SetLeft(orCreateImage, width + (double)gridColumn * num2);
					Canvas.SetTop(orCreateImage, height + (double)gridRow * num3);
					Panel.SetZIndex(orCreateImage, gridColumn % 2 + 2 * (gridRow % 2));
					if (ShouldShowSeamImage(ref gridColumn, ref gridRow))
					{
						double num13 = width + (double)gridColumn * num2;
						if (gridColumn == -1)
						{
							num13 = width - num11;
						}
						else if (gridColumn == ViewModel.ColumnCount)
						{
							num13 = width + (double)(gridColumn - 1) * num2 + num11;
						}
						double num14 = height + (double)gridRow * num3;
						if (gridRow == -1)
						{
							num14 = height - num12;
						}
						else if (gridRow == ViewModel.RowCount)
						{
							num14 = height + (double)(gridRow - 1) * num3 + num12;
						}
						orCreateImage = GetOrCreateImage(seamImages, i);
						Canvas.SetLeft(orCreateImage, num13);
						Canvas.SetTop(orCreateImage, num14);
						orCreateImage.Width = num;
						orCreateImage.Height = num1;
						Panel.SetZIndex(orCreateImage, (gridColumn + 2) % 2 + 2 * ((gridRow + 2) % 2));
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

		private void CanRemoveSelectedImages(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = ViewModel.SortedSourceFiles.Any<SourceFileViewModel>((SourceFileViewModel s) => s.IsSelected);
			e.Handled = true;
		}

		private StructuredPanoramaImage GetOrCreateImage(List<StructuredPanoramaImage> imageList, int i)
		{
			SourceFileViewModel item = ViewModel.SortedSourceFiles[i];
			StructuredPanoramaImage structuredPanoramaImage = imageList[i];
			if (structuredPanoramaImage != null)
			{
				structuredPanoramaImage.DataContext = item;
			}
			else
			{
				structuredPanoramaImage = new StructuredPanoramaImage();
				structuredPanoramaImage.MouseLeftButtonDown += new MouseButtonEventHandler(Image_MouseLeftButtonDown);
				structuredPanoramaImage.DataContext = item;
				structuredPanoramaImage.ImageNumber = i + 1;
				structuredPanoramaImage.SetBinding(ToolTipProperty, new Binding("FilePath"));
				structuredPanoramaImage.SetBinding(StructuredPanoramaImage.IsShowingNumberProperty, multibinding);
				structuredPanoramaImage.SetBinding(StructuredPanoramaImage.ThumbnailImageProperty, new Binding("Thumbnail"));
				DependencyProperty showBorderProperty = StructuredPanoramaImage.ShowBorderProperty;
				Binding binding = new Binding("DataContext.PreviewOverlap")
				{
					Source = this,
					Converter = new NegatedBooleanConverter()
				};
				structuredPanoramaImage.SetBinding(showBorderProperty, binding);
				structuredPanoramaImage.SetBinding(StructuredPanoramaImage.IsSelectedProperty, new Binding("IsSelected"));
				imageCanvas.Children.Add(structuredPanoramaImage);
				imageList[i] = structuredPanoramaImage;
			}
			return structuredPanoramaImage;
		}

		private void HandleDrop(IEnumerable<string> imageFiles)
		{
			Dictionary<string, double> strs = new Dictionary<string, double>()
			{
				{ "images", (double)imageFiles.Count<string>() }
			};
			
			MainWindow.Instance.ViewModel.ImportImages(imageFiles);
			foreach (SourceFileViewModel sortedSourceFile in ViewModel.SortedSourceFiles)
			{
				sortedSourceFile.IsSelected = imageFiles.Contains<string>(sortedSourceFile.FilePath);
			}
		}

		private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (!ViewModel.PreviewOverlap)
			{
				SourceFileViewModel dataContext = (SourceFileViewModel)((StructuredPanoramaImage)sender).DataContext;
				if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
				{
					dataContext.IsSelected = !dataContext.IsSelected;
					anchorSourceFile = dataContext;
				}
				else if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || !ViewModel.SortedSourceFiles.Contains(anchorSourceFile))
				{
					foreach (SourceFileViewModel sortedSourceFile in ViewModel.SortedSourceFiles)
					{
						sortedSourceFile.IsSelected = (object)sortedSourceFile == (object)dataContext;
					}
					anchorSourceFile = dataContext;
				}
				else
				{
					int num = ViewModel.SortedSourceFiles.IndexOf(anchorSourceFile);
					int num1 = ViewModel.SortedSourceFiles.IndexOf(dataContext);
					if (num >= 0 && num1 >= 0)
					{
						int num2 = Math.Min(num, num1);
						int num3 = Math.Max(num, num1);
						for (int i = 0; i < ViewModel.SortedSourceFiles.Count; i++)
						{
							ViewModel.SortedSourceFiles[i].IsSelected = (num2 > i ? false : i <= num3);
						}
					}
				}
				CommandManager.InvalidateRequerySuggested();
				e.Handled = true;
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

		private void RemoveSelectedImages(object sender, ExecutedRoutedEventArgs e)
		{
			SourceFileViewModel[] array = (
				from s in ViewModel.SortedSourceFiles
				where s.IsSelected
				select s).ToArray<SourceFileViewModel>();
			Dictionary<string, double> strs = new Dictionary<string, double>()
			{
				{ "images", (double)((int)array.Length) }
			};
			
			int num = ViewModel.SortedSourceFiles.IndexOf(array.Last<SourceFileViewModel>());
			SourceFileViewModel sourceFileViewModel = ViewModel.SortedSourceFiles.Take<SourceFileViewModel>(num).LastOrDefault<SourceFileViewModel>((SourceFileViewModel s) => !s.IsSelected);
			MainWindow.Instance.ViewModel.RemoveImages(array);
			if (sourceFileViewModel != null)
			{
				sourceFileViewModel.IsSelected = true;
			}
			e.Handled = true;
		}

		private bool ShouldShowSeamImage(ref int column, ref int row)
		{
			if (ViewModel.AngularRange == (Stitching.AngularRange)1)
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
			else if (ViewModel.AngularRange == (Stitching.AngularRange)2)
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

		private void StructuredImportView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (viewModel != null)
			{
				viewModel.ImageArrangementChanged -= new EventHandler(ViewModel_ImageArrangementChanged);
			}
			viewModel = e.NewValue as StructuredImportViewModel;
			if (viewModel != null)
			{
				viewModel.ImageArrangementChanged += new EventHandler(ViewModel_ImageArrangementChanged);
			}
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
				viewModel.ImageArrangementChanged -= new EventHandler(ViewModel_ImageArrangementChanged);
				viewModel.PreviewOverlap = false;
			}
			overlapBlinkTimer.Stop();
		}

		private void ViewModel_ImageArrangementChanged(object sender, EventArgs e)
		{
			if (arrangeOperation == null)
			{
				arrangeOperation = Dispatcher.BeginInvoke(new Action(() => ArrangeImages()), DispatcherPriority.Background, new object[0]);
			}
		}
	}
}