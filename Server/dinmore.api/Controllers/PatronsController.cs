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
using dinmore.api.Interfaces;

namespace dinmore.api.Controllers
{

    [Produces("application/json")]
    [Route("api/Patrons")]
    public class PatronsController : Controller
    {
        //private readonly AppSettings _appSettings;
        private readonly IEmotionApiRepository _emotionApiRepository;

        public PatronsController(IEmotionApiRepository emotionApiRepository)
        {
            _emotionApiRepository = emotionApiRepository;
        }

        // POST: api/Patrons
        // To post in PostMan set Content-Type to application/octet-stream and attach a file as a binary body
        [HttpPost]
        public async Task<IActionResult> Post()
        {
            //read body of request into a byte array
            byte[] bytes = ReadFileStream(Request.Body);

            //setup patrons list
            var patrons = new List<Patron>();

            //get faces with emotion
            var facesWithEmotion = await _emotionApiRepository.GetFacesWithEmotion(bytes);
            foreach (var faceWithEmotion in facesWithEmotion)
            {
                patrons.Add(new Patron()
                {
                    Age = 38,
                    Gender = "Male",
                    EmotionScores = faceWithEmotion.scores,
                    FaceId = Guid.NewGuid().ToString(),
                    FaceRectangle = faceWithEmotion.faceRectangle,
                    PrimaryEmotion = GetTopEmotion(faceWithEmotion.scores)
                });
            }
            
            return Json(patrons);
        }

        /// <summary>
        /// What's the primary emotion then?
        /// </summary>
        /// <param name="scores"></param>
        /// <returns></returns>
        private static string GetTopEmotion(Scores scores)
        {
            var scoresList = new Dictionary<string, double>();
            scoresList.Add("anger", scores.anger);
            scoresList.Add("happiness", scores.happiness );
            scoresList.Add("contempt", scores.contempt );
            scoresList.Add("disgust", scores.disgust );
            scoresList.Add("fear",  scores.fear );
            scoresList.Add("neutral", scores.neutral );
            scoresList.Add("sadness", scores.sadness );
            scoresList.Add("surprise", scores.surprise );

            //sort by scores
            var sortedScoresList = scoresList.ToList();
            sortedScoresList.Sort((x, y) => x.Value.CompareTo(y.Value));
            sortedScoresList.Reverse();

            //get the emotion label fopr top scoring EmotionScore
            var key = sortedScoresList.First().Key;

            return key;
        }

        private static byte[] ReadFileStream(Stream input)
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

    }
}