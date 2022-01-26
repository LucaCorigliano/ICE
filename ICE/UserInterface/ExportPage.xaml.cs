using Microsoft.Research.ICE.Helpers;
using Microsoft.Research.ICE.PanoViewing;
using Microsoft.Research.VisionTools.Toolkit.Desktop;
using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;

namespace Microsoft.Research.ICE.UserInterface
{
	public partial class ExportPage : UserControl
	{
		private ZoomHelper zoomHelper;

		public ExportPage()
		{
			InitializeComponent();
			zoomHelper = new ZoomHelper(this, panoViewer, true);
		}

		private void ExportButton_Click(object sender, RoutedEventArgs e)
		{
			Commands.Export.Execute(null, Application.Current.MainWindow);
		}

		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			NavigationCommands.GoToPage.Execute(null, sender as IInputElement);
		}

	}
}