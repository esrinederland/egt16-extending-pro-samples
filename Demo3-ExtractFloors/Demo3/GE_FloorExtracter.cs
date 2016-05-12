using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping.Events;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Editing;
using ArcGIS.Core.Geometry;

namespace Demo3
{
    internal class GE_FloorExtracter : Button
    {        
        protected override void OnClick()
        {
            if (Module1.Current.SelectedItems == null)
                return;

            int aantalVerdiepingen;
            double verdiepingsHoogte;

            QueuedTask.Run(() =>
            {
                var editOperation = new EditOperation();
                foreach (KeyValuePair<MapMember, List<long>> item in Module1.Current.SelectedItems)
                {
                    var layer = item.Key as BasicFeatureLayer;

                    foreach (long oid in item.Value)
                    {
                        var feature = layer.Inspect(oid);
                        aantalVerdiepingen = new Random().Next(3, 10);
                        verdiepingsHoogte = double.Parse(feature["PandHoogte"].ToString()) / aantalVerdiepingen;

                        feature["PandHoogte"] = verdiepingsHoogte;
                        editOperation.Modify(feature);
                        for (int i = 1; i < aantalVerdiepingen; i++)
                        {
                            editOperation.Create(layer, GeometryEngine.Move(feature.Shape, 0, 0, (i * (verdiepingsHoogte*2))), new Action<long>(x => newFloorCreated(x, layer, verdiepingsHoogte, feature.Shape.Extent.ZMin)));
                        }
                    }
                }
                bool succeded = editOperation.Execute();
            });
        }

        protected void newFloorCreated(long obj, BasicFeatureLayer layer, double verdiepingsHoogte, double buildingZMinValue)
        {
            EditOperation editOperation = new EditOperation();
            var feature = layer.Inspect(obj);

            double verdieping = Math.Round(((feature.Shape.Extent.ZMin - buildingZMinValue) / (verdiepingsHoogte * 2)), 0);
            feature["PandHoogte"] = verdiepingsHoogte;
            feature["Verdieping"] = verdieping;
            editOperation.Modify(feature);
            editOperation.Execute();

            Module1.Current.SelectedItems.Clear();
            MapView.Active.Map.SetSelection(null);
        }
    }
}
