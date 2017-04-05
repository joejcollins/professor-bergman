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
            var scoresList = new List<EmotionScore>();
            scoresList.Add(new EmotionScore() { Emotion = "anger", Score = scores.anger });
            scoresList.Add(new EmotionScore() { Emotion = "happiness", Score = scores.happiness });
            scoresList.Add(new EmotionScore() { Emotion = "contempt", Score = scores.contempt });
            scoresList.Add(new EmotionScore() { Emotion = "disgust", Score = scores.disgust });
            scoresList.Add(new EmotionScore() { Emotion = "fear", Score = scores.fear });
            scoresList.Add(new EmotionScore() { Emotion = "neutral", Score = scores.neutral });
            scoresList.Add(new EmotionScore() { Emotion = "sadness", Score = scores.sadness });
            scoresList.Add(new EmotionScore() { Emotion = "surprise", Score = scores.surprise });

            //sort by scores
            scoresList.Sort((x, y) => x.Score.CompareTo(y.Score));
            scoresList.Reverse();

            //get the emotion label fopr top scoring EmotionScore
            var key = scoresList.First().Emotion;

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