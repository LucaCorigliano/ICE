using Microsoft.Research.ICE.Controls;
using Microsoft.Research.ICE.Helpers;
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
	public partial class StitchPage : UserControl
	{
		private ZoomHelper zoomHelper;

		public StitchPage()
		{
			InitializeComponent();
			zoomHelper = new ZoomHelper(this, panoViewer, false);
		}

		private void GoToImportHyperlink_Click(object sender, RoutedEventArgs e)
		{
			NavigationCommands.BrowseBack.Execute(null, sender as IInputElement);
		}
	}
}