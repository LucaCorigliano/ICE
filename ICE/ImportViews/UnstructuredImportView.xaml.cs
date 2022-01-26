using Microsoft.Research.ICE.Helpers;
using Microsoft.Research.ICE.ViewModels;
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;

namespace Microsoft.Research.ICE.ImportViews
{
	public partial class UnstructuredImportView : UserControl
	{
		private CommandBinding deleteCommandBinding;

		private MainViewModel ViewModel
		{
			get
			{
				return (MainViewModel)DataContext;
			}
		}

		public UnstructuredImportView()
		{
			deleteCommandBinding = new CommandBinding(ApplicationCommands.Delete, new ExecutedRoutedEventHandler(RemoveSelectedImages), new CanExecuteRoutedEventHandler(CanRemoveSelectedImages));
			InitializeComponent();
			DragDropHelper dragDropHelper = new DragDropHelper(imageListBox, new ImagesDroppedCallback(HandleDrop));
            Loaded += new RoutedEventHandler(UnstructuredImportView_Loaded);
            Unloaded += new RoutedEventHandler(UnstructuredImportView_Unloaded);
		}

		private void CanRemoveSelectedImages(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = imageListBox.SelectedItems.Count > 0;
			e.Handled = true;
		}

		private void HandleDrop(IEnumerable<string> imageFiles)
		{
			Dictionary<string, double> strs = new Dictionary<string, double>()
			{
				{ "images", (double)imageFiles.Count<string>() }
			};

			ViewModel.ImportImages(imageFiles);
			imageListBox.UnselectAll();
			foreach (string imageFile in imageFiles)
			{
				imageListBox.SelectedItems.Add(ViewModel.SortedSourceFiles.LastOrDefault<SourceFileViewModel>((SourceFileViewModel sourceFile) => sourceFile.FilePath == imageFile));
			}
		}

		private void ImageListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			CommandManager.InvalidateRequerySuggested();
		}

		private void RemoveSelectedImages(object sender, ExecutedRoutedEventArgs e)
		{
			int selectedIndex = imageListBox.SelectedIndex;
			SourceFileViewModel[] array = imageListBox.SelectedItems.OfType<SourceFileViewModel>().ToArray<SourceFileViewModel>();
			ViewModel.RemoveImages(array);
			Dictionary<string, double> strs = new Dictionary<string, double>()
			{
				{ "images", (double)((int)array.Length) }
			};

			if (imageListBox.HasItems)
			{
				imageListBox.SelectedIndex = Math.Min(selectedIndex, ViewModel.SortedSourceFiles.Count - 1);
				imageListBox.ScrollIntoView(imageListBox.SelectedItem);
			}
			e.Handled = true;
		}

		private void UnstructuredImportView_Loaded(object sender, RoutedEventArgs e)
		{
			imageListBox.SelectionChanged += new SelectionChangedEventHandler(ImageListBox_SelectionChanged);
			Application.Current.MainWindow.CommandBindings.Replace(deleteCommandBinding);
		}

		private void UnstructuredImportView_Unloaded(object sender, RoutedEventArgs e)
		{
			imageListBox.SelectionChanged -= new SelectionChangedEventHandler(ImageListBox_SelectionChanged);
			Application.Current.MainWindow.CommandBindings.Remove(deleteCommandBinding);
		}
	}
}