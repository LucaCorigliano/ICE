using Microsoft.Research.ICE.ImportViews;
using Microsoft.Research.ICE.ViewModels;

using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Shapes;

namespace Microsoft.Research.ICE.UserInterface
{
	public partial class ImportPage : UserControl
	{
		private UIElement ImportView
		{
			get
			{
				return contentBorder.Child;
			}
			set
			{
				contentBorder.Child = value;
			}
		}

		private MainViewModel ViewModel
		{
			get
			{
				return (MainViewModel)DataContext;
			}
		}

		public ImportPage()
		{
			InitializeComponent();
		}

		public void UpdateState()
		{
			if (ViewModel.NavigationState == NavigationState.Import)
			{
				Type type = null;
				if (!ViewModel.IsVideoPanorama)
				{
					type = (!ViewModel.IsStructuredPanorama ? typeof(UnstructuredImportView) : typeof(StructuredImportView));
				}
				else
				{
					type = typeof(VideoImportView);
				}
				UIElement importView = ImportView;
				if (importView == null || importView.GetType() != type)
				{
					
					ImportView = (UIElement)Activator.CreateInstance(type);
				}
			}
		}
	}
}