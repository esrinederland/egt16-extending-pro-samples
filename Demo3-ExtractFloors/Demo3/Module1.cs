﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using System.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ArcGIS.Desktop.Framework.Threading.Tasks;

namespace Demo3
{
    internal class Module1 : Module
    {
        private static Module1 _this = null;

        public Dictionary<MapMember, List<long>> SelectedItems { get; set; }

        private  Module1()
        {            
            MapSelectionChangedEvent.Subscribe(OnMapSelectionChanged);
        }

        private void OnMapSelectionChanged(MapSelectionChangedEventArgs obj)
        {
            if (MapView.Active == null)
                return;

            SelectedItems = obj.Selection;
        }

        /// <summary>
        /// Retrieve the singleton instance to this module here
        /// </summary>
        public static Module1 Current
        {
            get
            {
                return _this ?? (_this = (Module1)FrameworkApplication.FindModule("Demo3_Module"));
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