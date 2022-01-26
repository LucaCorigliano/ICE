using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Microsoft.Research.ICE.Helpers;
using Microsoft.Research.ICE.ViewModels;
using Microsoft.Research.VisionTools.Toolkit.Desktop.Telemetry;

namespace Microsoft.Research.ICE.ImportViews;

public class UnstructuredImportView2 : UserControl, IComponentConnector
{
	private CommandBinding deleteCommandBinding;

	internal ListBox imageListBox;

	private bool _contentLoaded;

	private MainViewModel ViewModel => (MainViewModel)base.DataContext;

	public UnstructuredImportView2()
	{
		deleteCommandBinding = new CommandBinding(ApplicationCommands.Delete, RemoveSelectedImages, CanRemoveSelectedImages);
		InitializeComponent();
		new DragDropHelper(imageListBox, HandleDrop);
		base.Loaded += UnstructuredImportView_Loaded;
		base.Unloaded += UnstructuredImportView_Unloaded;
	}

	private void UnstructuredImportView_Loaded(object sender, RoutedEventArgs e)
	{
		imageListBox.SelectionChanged += ImageListBox_SelectionChanged;
		Application.Current.MainWindow.CommandBindings.Replace(deleteCommandBinding);
	}

	private void UnstructuredImportView_Unloaded(object sender, RoutedEventArgs e)
	{
		imageListBox.SelectionChanged -= ImageListBox_SelectionChanged;
		Application.Current.MainWindow.CommandBindings.Remove(deleteCommandBinding);
	}

	private void CanRemoveSelectedImages(object sender, CanExecuteRoutedEventArgs e)
	{
		e.CanExecute = imageListBox.SelectedItems.Count > 0;
		e.Handled = true;
	}

	private void RemoveSelectedImages(object sender, ExecutedRoutedEventArgs e)
	{
		int selectedIndex = imageListBox.SelectedIndex;
		SourceFileViewModel[] array = imageListBox.SelectedItems.OfType<SourceFileViewModel>().ToArray();
		ViewModel.RemoveImages(array);
		Track.Event("remove unstructured images", null, new Dictionary<string, double> { { "images", array.Length } });
		if (imageListBox.HasItems)
		{
			imageListBox.SelectedIndex = Math.Min(selectedIndex, ViewModel.SortedSourceFiles.Count - 1);
			imageListBox.ScrollIntoView(imageListBox.SelectedItem);
		}
		e.Handled = true;
	}

	private void ImageListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		CommandManager.InvalidateRequerySuggested();
	}

	private void HandleDrop(IEnumerable<string> imageFiles)
	{
		Track.Event("add unstructured images from drag-and-drop", null, new Dictionary<string, double> { 
		{
			"images",
			imageFiles.Count()
		} });
		ViewModel.ImportImages(imageFiles);
		imageListBox.UnselectAll();
		foreach (string filePath in imageFiles)
		{
			IList selectedItems = imageListBox.SelectedItems;
			List<SourceFileViewModel> sortedSourceFiles = ViewModel.SortedSourceFiles;
			Func<SourceFileViewModel, bool> predicate = (SourceFileViewModel sourceFile) => sourceFile.FilePath == filePath;
			selectedItems.Add(sortedSourceFiles.LastOrDefault(predicate));
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/ICE;component/importviews/unstructuredimportview.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[EditorBrowsable(EditorBrowsableState.Never)]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		if (connectionId == 1)
		{
			imageListBox = (ListBox)target;
		}
		else
		{
			_contentLoaded = true;
		}
	}
}
