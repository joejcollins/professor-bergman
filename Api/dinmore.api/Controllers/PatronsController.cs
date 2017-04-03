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

namespace dinmore.api.Controllers
{

    public class DataHolder {
        public byte[] Bytes { get; set; }
    }
    [Produces("application/json")]
    [Route("api/Patrons")]
    public class PatronsController : Controller
    {
        public const string _emotionApiKey = "1dd1f4e23a5743139399788aa30a7153";
        public const string _emotionApiUrl = "https://api.projectoxford.ai/emotion/v1.0/recognize";

        // POST: api/Patrons
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
                httpClient.BaseAddress = new Uri(_emotionApiUrl);
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _emotionApiKey);
                var content = new StreamContent(new MemoryStream(bytes));
                content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/octet-stream");

                //make request
                var responseMessage = await httpClient.PostAsync(_emotionApiUrl, content);

                //read response as a json string
                emotionResponseString = await responseMessage.Content.ReadAsStringAsync();
            }

            //generate mock content for now
            var patrons = GenerateMockData();
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

        private IEnumerable<Patron> GenerateMockData()
        {
            var patrons = new List<Patron>();

            patrons.Add(new Patron()
            {
                Age = 38,
                Gender = "Male",
                EmotionScores = new EmotionScores()
                {
                    happiness = 0.87,
                    anger = 0.01,
                    contempt = 0.06,
                    surprise = 0.34,
                    disgust = 0.04,
                    fear = 0.01,
                    neutral = 0.23,
                    sadness = 0.02
                },
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

            patrons.Add(new Patron()
            {
                Age = 17,
                Gender = "Female",
                EmotionScores = new EmotionScores()
                {
                    happiness = 0.05,
                    anger = 0.01,
                    contempt = 0.12,
                    surprise = 0.38,
                    disgust = 0.01,
                    fear = 0.03,
                    neutral = 0.84,
                    sadness = 0.16
                },
                FaceId = Guid.NewGuid().ToString(),
                FaceRectangle = new FaceRectangle()
                {
                    left = 153,
                    top = 251,
                    width = 133,
                    height = 133
                },
                PrimaryEmotion = "neutral"
            });

            return patrons;
        }
    }


}