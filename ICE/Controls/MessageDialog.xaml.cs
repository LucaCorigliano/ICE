using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Microsoft.Research.VisionTools.Toolkit.Desktop;

namespace Microsoft.Research.ICE.Controls
{
    public partial class MessageDialog 
    {
        public enum MessageDialogResult
        {
            Yes,
            No,
            Cancel
        }

        private MessageDialogResult Result { get; set; }

        public static MessageDialogResult Show(Window parent, string message, string yesLabel, string noLabel, string cancelLabel)
        {
            MessageDialog messageDialog = new MessageDialog();
            messageDialog.Owner = parent;
            messageDialog.messageTextBlock.Text = message;
            messageDialog.yesButton.Content = yesLabel;
            if (string.IsNullOrEmpty(noLabel))
            {
                messageDialog.yesButton.IsCancel = true;
                messageDialog.noButton.Visibility = Visibility.Collapsed;
                messageDialog.cancelButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                messageDialog.noButton.Content = noLabel;
                if (string.IsNullOrEmpty(cancelLabel))
                {
                    messageDialog.noButton.IsCancel = true;
                    messageDialog.cancelButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    messageDialog.cancelButton.Content = cancelLabel;
                }
            }
            messageDialog.ShowDialog();
            return messageDialog.Result;
        }

        private MessageDialog()
        {
            InitializeComponent();
            yesButton.Click += YesButton_Click;
            noButton.Click += NoButton_Click;
            Result = MessageDialogResult.Cancel;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageDialogResult.Yes;
            Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageDialogResult.No;
            Close();
        }

     
    }
}