using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System.Windows.Input;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;

namespace Demo3
{
    internal class MapTool_Extracter : MapTool
    {
        protected override Task OnToolActivateAsync(bool active)
        {
            return base.OnToolActivateAsync(active);
        }

        protected override void OnToolMouseDown(MapViewMouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                int aantalVerdiepingen = 10;
                double verdiepingsHoogte;

                QueuedTask.Run(() =>
                {
                    var point = MapPointBuilder.CreateMapPoint(e.ClientPoint.X, e.ClientPoint.Y);
                    var selectedLayerFeatures = MapView.Active.GetFeatures(point, true);
                    var selectedItems = selectedLayerFeatures.ToDictionary(x => x.Key as MapMember, x => x.Value);

                    var op = new EditOperation();
                    foreach (KeyValuePair<MapMember, List<long>> item in selectedItems)
                    {
                        var layer = item.Key as BasicFeatureLayer;

                        foreach (long oid in item.Value)
                        {
                            var feature = layer.Inspect(oid);
                            verdiepingsHoogte = double.Parse(feature["PandHoogte"].ToString()) / aantalVerdiepingen;
                            feature["PandHoogte"] = verdiepingsHoogte;
                            feature["Verdieping"] = aantalVerdiepingen;

                            op.Modify(feature);
                            bool succes = op.Execute();

                            op = new EditOperation();
                            if (succes)
                            {
                                for (int i = 1; i < aantalVerdiepingen; i++)
                                {
                                    op.Duplicate(layer, feature.ObjectID, 0, 0, (i * (verdiepingsHoogte * 2)));
                                }
                            }
                        }
                    }
                    bool succeded = op.Execute();

                    MapView.Active.Map.SetSelection(null);
                });
            }
            base.OnToolMouseDown(e);
        }          
    }
}
