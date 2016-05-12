using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using System.Windows;

namespace Demo2_Loading
{
    internal class Button2 : Button
    {
        public Button2()
        {
            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Button 2 is nu geladen", "Button 2 geladen", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        protected override void OnClick()
        {
        }
    }
}
