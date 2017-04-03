using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using dinmore.api.Models;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace dinmore.api.Controllers
{

    [Produces("application/json")]
    [Route("api/Patrons")]
    public class PatronsController : Controller
    {
        private readonly AppSettings _appSettings;

        public PatronsController(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        // POST: api/Patrons
        // To post in PostMan set Content-Type to application/octet-stream and attach a file as a binary body
        [HttpPost]
        public async Task<IActionResult> Post()
        {
            //read body of request into a byte array
            byte[] bytes = ReadFileStream(Request.Body);

            //call emotion api
            var emotionResponseString = string.Empty;
            using (var httpClient = new HttpClient())
            {
                //setup HttpClient with content
                httpClient.BaseAddress = new Uri(_appSettings.EmotionApiBaseUrl);
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _appSettings.EmotionApiKey);
                var content = new StreamContent(new MemoryStream(bytes));
                content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/octet-stream");

                //make request
                var responseMessage = await httpClient.PostAsync(_appSettings.EmotionApiBaseUrl, content);

                //read response as a json string
                emotionResponseString = await responseMessage.Content.ReadAsStringAsync();
            }

            //create emotion scores object. parse json string to object and enumerate
            var emotionResponseArray = JArray.Parse(emotionResponseString);
            var faces = new List<Face>();
            foreach (var emotionFaceResponse in emotionResponseArray)
            {
                //deserialise json to face
                var face = JsonConvert.DeserializeObject<Face>(emotionFaceResponse.ToString());

                //add face to faces list
                faces.Add(face);
            }

            //generate mock content for now
            var patrons = GenerateMockData(faces.FirstOrDefault().scores);
            return Json(patrons);
        }

        public static byte[] ReadFileStream(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        private IEnumerable<Patron> GenerateMockData(Scores emotionScores)
        {
            var patrons = new List<Patron>();

            patrons.Add(new Patron()
            {
                Age = 38,
                Gender = "Male",
                EmotionScores = emotionScores,
                FaceId = Guid.NewGuid().ToString(),
                FaceRectangle = new FaceRectangle()
                {
                    left = 488,
                    top = 263,
                    width = 148,
                    height = 148
                },
                PrimaryEmotion = "happiness"
            });

            //patrons.Add(new Patron()
            //{
            //    Age = 17,
            //    Gender = "Female",
            //    EmotionScores = new EmotionScores()
            //    {
            //        happiness = 0.05,
            //        anger = 0.01,
            //        contempt = 0.12,
            //        surprise = 0.38,
            //        disgust = 0.01,
            //        fear = 0.03,
            //        neutral = 0.84,
            //        sadness = 0.16
            //    },
            //    FaceId = Guid.NewGuid().ToString(),
            //    FaceRectangle = new FaceRectangle()
            //    {
            //        left = 153,
            //        top = 251,
            //        width = 133,
            //        height = 133
            //    },
            //    PrimaryEmotion = "neutral"
            //});

            return patrons;
        }
    }


}