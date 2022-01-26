using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Microsoft.Research.ICE.Helpers;
using Microsoft.Research.ICE.ViewModels;
using Microsoft.Research.VisionTools.Toolkit.Desktop;

namespace Microsoft.Research.ICE.Controls
{
    public partial class HelpDialog
    {
        private const string ErrorCouldNotLaunchBrowser = "Could not launch browser.";


        public HelpViewModel ViewModel
        {
            get
            {
                return DataContext as HelpViewModel;
            }
            set
            {
                DataContext = value;
            }
        }

        public HelpDialog()
        {
            InitializeComponent();
            CommandBindings.Add(new CommandBinding(NavigationCommands.GoToPage, GoToPageCommand_Executed));
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void GoToPageCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string text = e.Parameter as string;
            if (!string.IsNullOrEmpty(text))
            {
                LinkHelper.OpenLink(this, "launch help", text, "Could not launch browser.");
            }
        }

    }
}