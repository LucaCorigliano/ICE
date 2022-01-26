using Microsoft.Research.ICE.ViewModels;
using Microsoft.Research.VisionTools.Toolkit.Desktop;

using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;

namespace Microsoft.Research.ICE.UserInterface
{
	public partial class StructuredPanoramaSettings : UserControl
	{
		private MainViewModel ViewModel
		{
			get
			{
				return (MainViewModel)DataContext;
			}
		}

		public StructuredPanoramaSettings()
		{
			InitializeComponent();
		}

		private void AutoLayoutButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.StructuredImport.EstimateLayout();
		}

		private void AutoOverlapButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.StructuredImport.EstimateOverlap();
		}
	}
}