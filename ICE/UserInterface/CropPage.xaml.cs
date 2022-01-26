using Microsoft.Research.ICE.Controls;
using Microsoft.Research.ICE.Helpers;
using Microsoft.Research.VisionTools.Toolkit.Desktop;
using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Microsoft.Research.ICE.UserInterface
{
	public partial class CropPage : UserControl
	{
		private ZoomHelper zoomHelper;

		public CropPage()
		{
			InitializeComponent();
			zoomHelper = new ZoomHelper(this, panoViewer, true);
		}
	}
}