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
    public class EmotionApiRepository : IEmotionApiRepository
    {
        private readonly AppSettings _appSettings;

        public EmotionApiRepository(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        public async Task<IEnumerable<FaceWithEmotion>> GetFacesWithEmotion(byte[] image)
        {
            //call face 
            //call emotion api
            var emotionResponseString = string.Empty;
            using (var httpClient = new HttpClient())
            {
                //setup HttpClient with content
                httpClient.BaseAddress = new Uri(_appSettings.EmotionApiBaseUrl);
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _appSettings.EmotionApiKey);
                var content = new StreamContent(new MemoryStream(image));
                content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/octet-stream");

                //make request
                var responseMessage = await httpClient.PostAsync(_appSettings.EmotionApiBaseUrl, content);

                //read response as a json string
                emotionResponseString = await responseMessage.Content.ReadAsStringAsync();
            }

            //create emotion scores object. parse json string to object and enumerate
            var emotionResponseArray = JArray.Parse(emotionResponseString);
            var faces = new List<FaceWithEmotion>();
            foreach (var emotionFaceResponse in emotionResponseArray)
            {
                //deserialise json to face
                var face = JsonConvert.DeserializeObject<FaceWithEmotion>(emotionFaceResponse.ToString());

                //add face to faces list
                faces.Add(face);
            }

            return faces;
        }
    }
}
