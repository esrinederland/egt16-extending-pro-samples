using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Framework.Threading.Tasks;

namespace Demo4
{
    internal class CreateAndPublishVT : Button
    {
        protected override void OnClick()
        {
                CreateUploadAndPublish cuap = new CreateUploadAndPublish();
                cuap.StartProcess();
        }
    }
}
