using dinmore.api.Interfaces;
using dinmore.api.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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

        public async Task<IEnumerable<Face>> DetectFaces(byte[] image)
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

                //make request
                var responseMessage = await httpClient.PostAsync(_appSettings.FaceApiDetectBaseUrl + "?returnFaceId=true&returnFaceLandmarks=true&returnFaceAttributes=age,gender,headPose,smile,facialHair,glasses,emotion", content);

                //read response as a json string
                responseString = await responseMessage.Content.ReadAsStringAsync();
            }

            //create emotion scores object. parse json string to object and enumerate
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
    }
}
