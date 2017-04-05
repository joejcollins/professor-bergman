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
        private readonly IFaceApiRepository _faceApiRepository;

        public PatronsController(IFaceApiRepository faceApiRepository)
        {
            _faceApiRepository = faceApiRepository;
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

            //get faces 
            var faces = await _faceApiRepository.DetectFaces(bytes);
            foreach (var face in faces)
            {
                patrons.Add(new Patron()
                {
                    FaceId = Guid.NewGuid().ToString(),
                    FaceRectangle = face.faceRectangle,
                    FaceAttributes = face.faceAttributes,
                    FaceLandmarks = face.faceLandmarks,
                    PrimaryEmotion = GetTopEmotion(face.faceAttributes.emotion)
                });
            }

            return Json(patrons);
        }

        /// <summary>
        /// What's the primary emotion then?
        /// </summary>
        /// <param name="emotion"></param>
        /// <returns></returns>
        private static string GetTopEmotion(Emotion emotion)
        {
            var scoresList = new Dictionary<string, double>();
            scoresList.Add("anger", emotion.anger);
            scoresList.Add("happiness", emotion.happiness );
            scoresList.Add("contempt", emotion.contempt );
            scoresList.Add("disgust", emotion.disgust );
            scoresList.Add("fear",  emotion.fear );
            scoresList.Add("neutral", emotion.neutral );
            scoresList.Add("sadness", emotion.sadness );
            scoresList.Add("surprise", emotion.surprise );

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