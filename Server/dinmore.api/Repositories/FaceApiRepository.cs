using dinmore.api.Interfaces;
using dinmore.api.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace dinmore.api.Repositories
{
    public class FaceApiRepository : IFaceApiRepository
    {
        private readonly AppSettings _appSettings;

        public FaceApiRepository(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        public async Task<IEnumerable<Face>> DetectFaces(byte[] image, bool returnFaceLandmarks, string returnFaceAttributes)
        {
            //call face api
            var responseString = string.Empty;
            using (var httpClient = new HttpClient())
            {

                //setup HttpClient with content
                httpClient.BaseAddress = new Uri(_appSettings.FaceApiDetectBaseUrl);
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _appSettings.FaceApiKey);
                var content = new StreamContent(new MemoryStream(image));
                content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/octet-stream");

                //construct full API endpoint uri
                var parameters = new Dictionary<string, string> {
                    { "returnFaceId", "true"},
                    { "returnFaceLandmarks", returnFaceLandmarks.ToString() },
                    { "returnFaceAttributes", returnFaceAttributes },
                };
                var apiUri = QueryHelpers.AddQueryString(_appSettings.FaceApiDetectBaseUrl, parameters);

                //make request
                var responseMessage = await httpClient.PostAsync(apiUri, content);

                //read response as a json string
                responseString = await responseMessage.Content.ReadAsStringAsync();
            }

            //parse json string and cast to list of faces
            var responseArray = JArray.Parse(responseString);
            var faces = new List<Face>();
            foreach (var faceResponse in responseArray)
            {
                //deserialise json to face
                var face = JsonConvert.DeserializeObject<Face>(faceResponse.ToString());

                //add face to faces list
                faces.Add(face);
            }

            return faces;
        }

        public async Task<string> GetCurrentFaceListId()
        {
            var dateTimeLabel = DateTime.UtcNow.ToString("ddMMyyyy");

            //try to create face list - if it is already created we'll get a 409 and just carry on
            using (var httpClient = new HttpClient())
            {
                //setup HttpClient with content
                var apiUrlBase = _appSettings.FaceApiCreateFaceListBaseUrl.Replace("[FaceListId]", dateTimeLabel);
                httpClient.BaseAddress = new Uri(apiUrlBase);
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _appSettings.FaceApiKey);

                //construct and body
                var postData = new Dictionary<string, string>();
                postData.Add("name", dateTimeLabel);
                postData.Add("userData", $"Patrons for {DateTime.UtcNow.ToString("D")}");
                var postDataJson = JsonConvert.SerializeObject(postData);
                byte[] byteData = Encoding.UTF8.GetBytes(postDataJson);
                var content = new ByteArrayContent(byteData);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                //make request
                var responseMessage = await httpClient.PutAsync(apiUrlBase, content);
            }

            return dateTimeLabel;
        }

        public async Task<string> AddFaceToFaceList(byte[] image, string faceListId, string targetFace, string userData)
        {
            //call face api
            var responseString = string.Empty;
            using (var httpClient = new HttpClient())
            {
                //setup HttpClient with content
                var apiUrlBase = _appSettings.FaceApiFaceListsPersistedFacesBaseUrl.Replace("[FaceListId]", faceListId);
                httpClient.BaseAddress = new Uri(apiUrlBase);
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _appSettings.FaceApiKey);
                var content = new StreamContent(new MemoryStream(image));
                content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/octet-stream");

                //construct full API endpoint uri
                var parameters = new Dictionary<string, string> {
                    { "targetFace", targetFace},
                    { "userData", userData }
                };
                var apiUri = QueryHelpers.AddQueryString(apiUrlBase, parameters);

                //make request
                var responseMessage = await httpClient.PostAsync(apiUri, content);

                //read response as a json string
                responseString = await responseMessage.Content.ReadAsStringAsync();

                //parse to dynamic object and extract persisited face id
                dynamic d = JObject.Parse(responseString);
                responseString = d.persistedFaceId;
            }

            return responseString;
        }

        public async Task<IEnumerable<PersistedFace>> FindSimilarFaces(string faceListId, string faceId)
        {
            //call face api
            var responseString = string.Empty;
            using (var httpClient = new HttpClient())
            {
                //setup HttpClient with content
                httpClient.BaseAddress = new Uri(_appSettings.FaceApiFindSimilarBaseUrl);
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _appSettings.FaceApiKey);

                //construct and body
                var postData = new Dictionary<string, string>();
                postData.Add("faceId", faceId);
                postData.Add("faceListId", faceListId);
                postData.Add("maxNumOfCandidatesReturned", "10");
                postData.Add("mode", "matchPerson");
                var postDataJson = JsonConvert.SerializeObject(postData);
                byte[] byteData = Encoding.UTF8.GetBytes(postDataJson);
                var content = new ByteArrayContent(byteData);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                //make request
                var responseMessage = await httpClient.PostAsync(_appSettings.FaceApiFindSimilarBaseUrl, content);

                //read response as a json string
                responseString = await responseMessage.Content.ReadAsStringAsync();
            }

            //parse json string and cast to list of faces
            var responseArray = JArray.Parse(responseString);
            var persistedFaces = new List<PersistedFace>();
            foreach (var faceResponse in responseArray)
            {
                //deserialise json to face
                var persistedFace = JsonConvert.DeserializeObject<PersistedFace>(faceResponse.ToString());

                //add face to faces list
                persistedFaces.Add(persistedFace);
            }

            return persistedFaces;
        }
    }
}
