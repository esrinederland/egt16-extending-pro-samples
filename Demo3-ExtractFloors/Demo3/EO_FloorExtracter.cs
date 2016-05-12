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

namespace Demo3
{
    internal class EO_FloorExtracter : Button
    {
        protected override void OnClick()
        {            
            if (Module1.Current.SelectedItems == null)
                return;

            int aantalVerdiepingen;
            double verdiepingsHoogte;

            QueuedTask.Run(() =>
            {
                var op = new EditOperation();
                foreach (KeyValuePair<MapMember, List<long>> item in Module1.Current.SelectedItems)
                {
                    var layer = item.Key as BasicFeatureLayer;

                    foreach (long oid in item.Value)
                    {
                        var feature = layer.Inspect(oid);
                        aantalVerdiepingen = new Random().Next(3, 10);
                        verdiepingsHoogte = double.Parse(feature["PandHoogte"].ToString()) / aantalVerdiepingen;
                        feature["PandHoogte"] = verdiepingsHoogte;

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

                Module1.Current.SelectedItems.Clear();
                MapView.Active.Map.SetSelection(null);
            });            
        }
    }
}
