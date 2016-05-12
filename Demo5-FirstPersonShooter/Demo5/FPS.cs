using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Core.Geometry;
using System.Timers;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing.Attributes;

namespace Demo5
{
    internal class FPS : MapTool
    {
        #region members
        private int _counter;
        private Timer _timer;
        private bool _active;
        #endregion

        #region constructor
        public FPS()
        {
            _counter = 0;
            _timer = new Timer(250);
            _timer.Elapsed += OnTimer_Elapsed;
            _timer.Enabled = false;
            _active = false;
        }
        #endregion

        #region events        
        protected override Task OnToolActivateAsync(bool active)
        {
            return base.OnToolActivateAsync(active);
        }
        protected override Task OnToolDeactivateAsync(bool hasMapViewChanged)
        {
            _active = false;
            return base.OnToolDeactivateAsync(hasMapViewChanged);
        }
        protected override void OnToolMouseDown(MapViewMouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.Left: BlowUpBuilding(e);
                    break;
                case MouseButton.Right: MoveWalls(e);
                    break;
                case MouseButton.Middle: CutBuilding(e);
                    break;
            }            

            e.Handled = true;
        }                               
        protected void OnExtractWalls(long oid, BasicFeatureLayer layer, Geometry geometry, double verdiepingsHoogte, int verdieping)
        {
            var feature = layer.Inspect(oid);
            feature["PandHoogte"] = verdiepingsHoogte;
            feature["Verdieping"] = verdieping;
            EditOperation editOperation = new EditOperation();
            QueuedTask.Run(() =>
            {
                editOperation.Clip(layer, oid, geometry, ClipMode.DiscardArea);
                editOperation.Modify(feature);
            });
            editOperation.Execute();
        }        
        protected override Task OnSelectionChangedAsync(MapSelectionChangedEventArgs e)
        {
            return base.OnSelectionChangedAsync(e);
        }
        private void OnTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Enabled = false;
            var camera = MapView.Active.Camera;
            camera.Pitch -= 5;
            camera.Z -= 5;

            MapView.Active.ZoomToAsync(camera, new TimeSpan(0, 0, 0, 0, 250));
        }
        protected override void OnToolKeyDown(MapViewKeyEventArgs k)
        {            
            //We will do some basic key handling to allow jumping and rolling
            if ((k.Key == Key.J) ||
                (k.Key == Key.R) ||
                (k.Key == Key.L) ||
                (k.Key == Key.Q))
                    k.Handled = true;
            else
                base.OnToolKeyDown(k);
        }
        protected override Task HandleKeyDownAsync(MapViewKeyEventArgs k)
        {
            var camera = MapView.Active.Camera;
            switch (k.Key)
            {
                case Key.J:
                    {
                        camera.Pitch += 5;
                        camera.Z += 5;
                        _timer.Enabled = true;
                    }
                    break;
                case Key.R: camera.Roll -= 45;
                    break;
                case Key.L: camera.Roll += 45;
                    break;
                case Key.Q: _active = !_active;
                    break;                
            }

            return MapView.Active.ZoomToAsync(camera, new TimeSpan(0, 0, 0, 0, 250));
        }
        protected override void OnToolMouseMove(MapViewMouseEventArgs e)
        {
            if (_active)
            {
                QueuedTask.Run(() =>
                {
                    _counter++;
                    if (_counter % 10 == 0)
                    {
                        MapView.Active.LookAtAsync(ActiveMapView.ClientToMap(e.ClientPoint), new TimeSpan(0, 0, 0, 0, 15));
                        _counter = 0;
                    }
                });
            }

            e.Handled = true;
        }
        protected void OnNewFloorCreated(long obj, BasicFeatureLayer layer, double verdiepingsHoogte, double buildingZMinValue)
        {
            EditOperation editOperation = new EditOperation();
            var feature = layer.Inspect(obj);

            double verdieping = Math.Round(((feature.Shape.Extent.ZMin - buildingZMinValue) / (verdiepingsHoogte * 2)), 0);
            feature["PandHoogte"] = verdiepingsHoogte;
            feature["Verdieping"] = verdieping;
            editOperation.Modify(feature);
            editOperation.Execute();

            MapView.Active.Map.SetSelection(null);
        }
        #endregion

        #region private methods                       
        private void BlowUpBuilding(MapViewMouseButtonEventArgs e)
        {
            int aantalVerdiepingen;
            double verdiepingsHoogte;

            QueuedTask.Run(() =>
            {
                Dictionary<MapMember, List<long>> selectedItems = GetSelectedItems(e);
                EditOperation editOperation = new EditOperation();
                foreach (KeyValuePair<MapMember, List<long>> item in selectedItems)
                {
                    BasicFeatureLayer layer = item.Key as BasicFeatureLayer;
                    if (layer.ShapeType != ArcGIS.Core.CIM.esriGeometryType.esriGeometryPolygon)
                        continue;

                    foreach (long oid in item.Value)
                    {
                        var feature = layer.Inspect(oid);                        
                        aantalVerdiepingen = new Random().Next(3, 10);
                        verdiepingsHoogte = double.Parse(feature["PandHoogte"].ToString()) / aantalVerdiepingen;

                        double buildingZMinValue = feature.Shape.Extent.ZMin;
                        feature["PandHoogte"] = verdiepingsHoogte;
                        editOperation.Modify(feature);
                        for (int i = 1; i < aantalVerdiepingen; i++)
                        {
                            var newFloorGeometry = GeometryEngine.Move(feature.Shape, 0, 0, (i * (verdiepingsHoogte * 2)));
                            editOperation.Create(layer, newFloorGeometry, new Action<long>(x => OnNewFloorCreated(x, layer, verdiepingsHoogte, buildingZMinValue)));
                        }
                    }
                }
                bool succeded = editOperation.Execute();
            });
        }
        private void MoveWalls(MapViewMouseButtonEventArgs e)
        {
            QueuedTask.Run(() =>
            {
                Dictionary<MapMember, List<long>> selectedItems = GetSelectedItems(e);

                EditOperation editOperation = new EditOperation();
                foreach (KeyValuePair<MapMember, List<long>> item in selectedItems)
                {
                    BasicFeatureLayer layer = item.Key as BasicFeatureLayer;
                    if (layer.ShapeType != ArcGIS.Core.CIM.esriGeometryType.esriGeometryPolygon)
                        continue;

                    foreach (long oid in item.Value)
                    {
                        var feature = layer.Inspect(oid);                        

                        double hoogte = double.Parse(feature["PandHoogte"].ToString());
                        int verdieping = int.Parse(feature["Verdieping"].ToString());

                        Geometry geom = feature.Shape.Clone();
                        Geometry removeGeometry = GeometryEngine.Scale(geom, geom.Extent.Center, 1.2, 1.2);
                        Geometry wallGeometry = GeometryEngine.Scale(geom, geom.Extent.Center, 1.3, 1.3);
                        editOperation.Scale(selectedItems, feature.Shape.Extent.Center, 0.9, 0.9);
                        editOperation.Create(layer, wallGeometry, new Action<long>(x => OnExtractWalls(x, layer, removeGeometry, hoogte, verdieping)));
                    }
                    editOperation.Execute();
                }

                MapView.Active.Map.SetSelection(null);
            });
        }        
        private void CutBuilding(MapViewMouseButtonEventArgs e)
        {
            QueuedTask.Run(() =>
            {
                Dictionary<MapMember, List<long>> selectedItems = GetSelectedItems(e);
                EditOperation editOperation = new EditOperation();
                foreach (KeyValuePair<MapMember, List<long>> item in selectedItems)
                {
                    BasicFeatureLayer layer = item.Key as BasicFeatureLayer;
                    if (layer.ShapeType != ArcGIS.Core.CIM.esriGeometryType.esriGeometryPolygon)
                        continue;

                    foreach (long oid in item.Value)
                    {
                        var feature = layer.Inspect(oid);
                    
                        Geometry geometry = feature.Shape.Clone();
                        Polyline polyLine = GetCutPolyLine(geometry);

                        var splitItems = GeometryEngine.Cut(geometry, polyLine);
                        feature.Shape = splitItems.First();
                        editOperation.Modify(feature);

                        Layer pointLayer = MapView.Active.Map.Layers[0];
                        editOperation.Create(pointLayer, geometry.Extent.Center);
                    }
                    editOperation.Execute();
                }              
                MapView.Active.Map.SetSelection(null);
            });
        }
        private static Dictionary<MapMember, List<long>> GetSelectedItems(MapViewMouseButtonEventArgs e)
        {
            var point = MapPointBuilder.CreateMapPoint(e.ClientPoint.X, e.ClientPoint.Y);
            var selectedLayerFeatures = MapView.Active.GetFeatures(point, true);
            var selectedItems = selectedLayerFeatures.ToDictionary(x => x.Key as MapMember, x => x.Value);
            return selectedItems;
        }
        private static Polyline GetCutPolyLine(Geometry geometry)
        {
            IList<Coordinate> cutLine = new List<Coordinate>();
            cutLine.Add(new Coordinate(geometry.Extent.XMin, geometry.Extent.YMin, geometry.Extent.ZMin));
            cutLine.Add(new Coordinate(geometry.Extent.Center));
            cutLine.Add(new Coordinate(geometry.Extent.XMax, geometry.Extent.YMax, geometry.Extent.ZMin));
            var polyLine = PolylineBuilder.CreatePolyline(cutLine, geometry.SpatialReference);
            return polyLine;
        }
        #endregion
    }
}
