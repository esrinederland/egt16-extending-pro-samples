using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using System.Windows;

namespace Demo2_Loading
{
    internal class Button1 : Button
    {
        public Button1()
        {
            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Button 1 is nu geladen", "Button 1 geladen", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        protected override void OnClick()
        {
        }
    }
}
