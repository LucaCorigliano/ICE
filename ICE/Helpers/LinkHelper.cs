using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using Microsoft.Research.ICE.Controls;


namespace Microsoft.Research.ICE.Helpers
{
    public static class LinkHelper
    {
        public static bool OpenLink(Window window, string eventName, string link, string errorMessage)
        {
            
            
            try
            {
                Process.Start(link);
                return true;
            }
            catch 
            {
                
                MessageDialog.Show(window, errorMessage, "OK", null, null);
                return false;
            }
        }
    }
}