using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using System.Threading.Tasks;
using ArcGIS.Desktop.Mapping.Events;
using ArcGIS.Desktop.Mapping;

namespace Demo1
{
    internal class Module1 : Module
    {
        private static Module1 _this = null;
        public const string MyStateID = "state";
        public Module1()
        {
            MapViewCameraChangedEvent.Subscribe(OnCameraChanged);
        }
        /// <summary>
        /// Handle camera changed events
        /// </summary>
        /// <param name="obj"></param>
        private void OnCameraChanged(MapViewCameraChangedEventArgs obj)
        {
            if (MapView.Active == null)
                return;

            var camera = MapView.Active.Camera;
            if (camera != null)
            {
                if (camera.Heading != 0)
                {
                    FrameworkApplication.State.Activate(MyStateID); //activates the state 
                }
                else
                {
                    FrameworkApplication.State.Deactivate(MyStateID); //deactivates the state 
                }
            }
        }

        /// <summary>
        /// Retrieve the singleton instance to this module here
        /// </summary>
        public static Module1 Current
        {
            get
            {
                return _this ?? (_this = (Module1)FrameworkApplication.FindModule("Demo1_Module"));
            }
        }

        #region Overrides
        /// <summary>
        /// Called by Framework when ArcGIS Pro is closing
        /// </summary>
        /// <returns>False to prevent Pro from closing, otherwise True</returns>
        protected override bool CanUnload()
        {
            //TODO - add your business logic
            //return false to ~cancel~ Application close
            return true;
        }

        #endregion Overrides

    }
}
