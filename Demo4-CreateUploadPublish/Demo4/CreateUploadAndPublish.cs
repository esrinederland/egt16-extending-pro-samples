using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Core.Geoprocessing;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Tests.APIHelpers.SharingDataContracts;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace Demo4
{
    class CreateUploadAndPublish
    {
        private CancelableProgressorSource _ps;
        private ProgressDialog _pd;
        private ArcGIS.Desktop.Mapping.Layer baseMapLayer;

        public void StartProcess()
        {
            baseMapLayer = MapView.Active.Map.Layers.First(x => (!(x is FeatureLayer)));
            QueuedTask.Run(() => baseMapLayer.SetVisibility(false));
            Geoprocessing.OpenToolDialog("management.CreateVectorTilePackage", null, null, false, CreateVectorTilePackage_Callback);
        }

        private async void CreateVectorTilePackage_Callback(string eventName, object o)
        {
            Debug.WriteLine("eventname:" + eventName);
            if (eventName == "OnEndExecute")
            {
                IGPResult res = (IGPResult)o;
                string packagePath = res.ReturnValue;

                if (!string.IsNullOrEmpty(packagePath) && File.Exists(packagePath))
                {
                    string summary = res.Parameters.Where(t => t.Item1 == "summary").First().Item3;
                    string tags = res.Parameters.Where(t => t.Item1 == "tags").First().Item3;

                    await QueuedTask.Run(() => baseMapLayer.SetVisibility(true));
                    await UploadAndPublishPackage(packagePath, summary, tags);

                    ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Uploaden en publiceren afgerond!", "Publiseren gereed.", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
        }

        private void ShowProgress(string message, string status, uint value)
        {
            if (_ps == null)
            {
                _pd = new ProgressDialog("Progress tracker");
                _pd.Show();
                _ps = new CancelableProgressorSource(_pd);
                _ps.Progressor.Max = 10;

                Debug.WriteLine("Progressorsources created");
            }

            Debug.WriteLine(string.Format("Progress updated: {0} - {1} - {2}/{3}", message, status, value, _ps.Progressor.Max));
            _ps.Progressor.Message = message;
            _ps.Progressor.Status = status;
            _ps.Progressor.Value = value;
        }

        public async Task UploadAndPublishPackage(string packagePath, string summary, string tags)
        {
            ShowProgress("Uploading File", "Uploading file", 1);
            var parameters = Geoprocessing.MakeValueArray(packagePath, "", "", summary, tags);
            await Geoprocessing.ExecuteToolAsync("management.SharePackage", parameters, null, null, myHandler);

            ShowProgress("Upload Complete", "Upload Complete", 2);

            var portalItem = SearchPortalForItem(Path.GetFileName(packagePath), "Vector Tile Package"); 

            ShowProgress("Upload Complete", string.Format("Item id: {0}", portalItem.Item2.id), 3);

            var result = await PublishService(portalItem.Item2.id, portalItem.Item2.title);

            if (_pd != null)
            {
                _pd.Hide();
            }
        }

        private async Task<object> PublishService(string id, string title)
        {
            string userName = GetLoggedInUser();
            string publishURL = String.Format("{0}/sharing/rest/content/users/{1}/publish", PortalManager.GetActivePortal(), userName);

            EsriHttpClient myClient = new EsriHttpClient();
            string publishParameters = string.Format("{{\"name\":\"{0}\"}}", title);
            var postData = new List<KeyValuePair<string, string>>();
            postData.Add(new KeyValuePair<string, string>("f", "json"));
            postData.Add(new KeyValuePair<string, string>("itemId", id));
            postData.Add(new KeyValuePair<string, string>("filetype", "vectortilepackage"));
            postData.Add(new KeyValuePair<string, string>("outputType", "VectorTiles"));
            postData.Add(new KeyValuePair<string, string>("publishParameters", publishParameters));
            HttpContent content = new FormUrlEncodedContent(postData);

            EsriHttpResponseMessage respMsg = myClient.Post(publishURL, content);
            PublishResult publishResult = JsonConvert.DeserializeObject<PublishResult>(await respMsg.Content.ReadAsStringAsync());

            if (publishResult.services.Count() == 1)
            {
                ShowProgress("Publish started", string.Format("Job id: {0}", publishResult.services[0].jobId), 3);
                string jobid = publishResult.services[0].jobId;
                string jobStatus = "processing";
                int i = 0;
                while (jobStatus == "processing")
                {
                    i += 1;
                    Task.Delay(5000).Wait();
                    jobStatus = GetJobStatus(jobid, "publish", userName, publishResult.services[0].serviceItemId);
                    ShowProgress("Publish in progress", string.Format("Job status: {0} - {1}", jobStatus, i), 3);
                }
                ShowProgress("Publish in progress", string.Format("Job status: {0} - {1}", jobStatus, i), 3);
            }

            return null;
        }

        private string GetJobStatus(string jobid, string jobtype, string userName, string itemdid)
        {
            string url = String.Format("http://www.arcgis.com/sharing/rest/content/users/{0}/items/{1}/status", userName, itemdid);
            var postData = new List<KeyValuePair<string, string>>();

            postData.Add(new KeyValuePair<string, string>("f", "json"));
            postData.Add(new KeyValuePair<string, string>("jobid", jobid));
            postData.Add(new KeyValuePair<string, string>("jobtype", jobtype));
            HttpContent content = new FormUrlEncodedContent(postData);

            string response = new EsriHttpClient().Post(url, content).Content.ReadAsStringAsync().Result;

            JobStatus publishResult = JsonConvert.DeserializeObject<JobStatus>(response);

            return publishResult.status;
        }

        private string GetLoggedInUser()
        {
            UriBuilder selfURL = new UriBuilder(PortalManager.GetActivePortal());
            selfURL.Path = "sharing/rest/portals/self";
            selfURL.Query = "f=json";
            EsriHttpClient client = new EsriHttpClient();
            EsriHttpResponseMessage response = client.Get(selfURL.Uri.ToString());

            dynamic portalSelf = JObject.Parse(response.Content.ReadAsStringAsync().Result);
            // if the response doesn't contain the user information then it is essentially
            // an anonymous request against the portal
            if (portalSelf.user == null)
                return null;
            string userName = portalSelf.user.username;
            return userName;
        }

        private void myHandler(string eventName, object o)
        {
            Debug.WriteLine(String.Format("eventname: {0}: {1}", eventName, o.ToString()));
        }

        public Tuple<bool, SDCItem> SearchPortalForItem(string itemName, string itemType)
        {
            string queryURL = String.Format(@"{0}/sharing/rest/search?q={1} AND type: {2}&f=json", PortalManager.GetActivePortal().ToString(), itemName, itemType);

            EsriHttpClient myClient = new EsriHttpClient();

            var response = myClient.Get(queryURL);
            if (response == null)
                return new Tuple<bool, SDCItem>(false, null);

            string outStr = response.Content.ReadAsStringAsync().Result;

            SearchResult searchResults = JsonConvert.DeserializeObject<SearchResult>(outStr);

            if (searchResults.results.Count() > 0)
            {
                for (int i = 0; i < searchResults.results.Count; i++)
                {
                    if ((searchResults.results[i].name == itemName) && (searchResults.results[i].type.Contains(itemType)))
                    {
                        return new Tuple<bool, SDCItem>(true, searchResults.results[i]);
                    }
                }

            }
            return null;
        }

    }
}
