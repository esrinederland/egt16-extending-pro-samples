using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace Demo1
{
    internal class btnNoord : Button
    {
        protected override void OnClick()
        {
            QueuedTask.Run(() =>
            {
                //Get the active map view.
                var mapView = MapView.Active;
                if (mapView == null)
                    return false;

                //Get the camera for the view, adjust the heading and zoom to the new camera position.
                var camera = mapView.Camera;
                camera.Heading = 0;
                camera.Pitch = -90;
                return mapView.ZoomTo(camera, TimeSpan.Zero);
            });
        }
    }
}
