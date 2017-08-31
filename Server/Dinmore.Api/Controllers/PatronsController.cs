using dinmore.api.Interfaces;
using dinmore.api.Models;
using Dinmore.Api.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace dinmore.api.Controllers
{

    [Produces("application/json")]
    [Route("api/Patrons")]
    public class PatronsController : Controller
    {
        //private readonly AppSettings _appSettings;
        private readonly IFaceApiRepository _faceApiRepository;
        private readonly IStoreRepository _storeRepository;

        public PatronsController(IFaceApiRepository faceApiRepository, IStoreRepository storeRepository)
        {
            _faceApiRepository = faceApiRepository;
            _storeRepository = storeRepository;
        }

        /// <summary>
        /// POST: api/Patrons
        /// To post in PostMan set 'Content-Type' to 'application/octet-stream' and attach a file as a binary body
        /// 'device' is a unique identifier for the device that sent the photo 
        /// 'returnFaceLandmarks' to return things like 'upperLipBottom', there are 27 landmarks in total. Defaults to false
        /// 'returnFaceAttributes' to return specific attributes. Accepts a comma-delimited list. Defaults to age,gender,headPose,smile,facialHair,glasses,emotion
        /// </summary>
        /// <param name="device"></param>
        /// <param name="exhibit"></param>
        /// <param name="returnFaceLandmarks"></param>
        /// <param name="returnFaceAttributes"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Post(string deviceId, bool returnFaceLandmarks = true, string returnFaceAttributes = "age,gender,headPose,smile,facialHair,glasses,emotion,hair,makeup,occlusion,accessories,blur,exposure,noise")
        {
            if (string.IsNullOrEmpty(deviceId)) return BadRequest();

            //get the device from storage based on the device id
            var device = await _storeRepository.GetDevice(deviceId);
            if (device == null) return BadRequest();

            //read body of request into a byte array
            byte[] bytes = ReadFileStream(Request.Body);

            //setup patrons list
            var patrons = new List<Patron>();

            //get the current facelist id
            var currentFaceListId = await _faceApiRepository.GetCurrentFaceListId();

            //get faces 
            var faces = await _faceApiRepository.DetectFaces(bytes, returnFaceLandmarks, returnFaceAttributes);
            foreach (var face in faces)
            {
                //get similar faces from the current face list
                var similarPersistedFaces = await _faceApiRepository.FindSimilarFaces(currentFaceListId, face.faceId);

                //get persisted face id and confidence by using the closest match or creating one.
                var persistedFaceId = string.Empty;
                var persistedFaceConfidence = 0.0;
                if (similarPersistedFaces.Count() == 0)
                {
                    //this is a new face, add to face list
                    persistedFaceId = await _faceApiRepository.AddFaceToFaceList(bytes, currentFaceListId, FaceRectangleToString(face.faceRectangle), string.Empty);
                }
                else {
                    //get the closest matching face
                    var sortedPersistedFaces = similarPersistedFaces.OrderByDescending(f => f.confidence);
                    persistedFaceId = sortedPersistedFaces.FirstOrDefault().persistedFaceId;
                    persistedFaceConfidence = sortedPersistedFaces.FirstOrDefault().confidence;
                }

                //create a patron
                patrons.Add(new Patron()
                {
                    FaceId = face.faceId,
                    PersistedFaceId = persistedFaceId,
                    FaceRectangle = face.faceRectangle,
                    FaceAttributes = face.faceAttributes,
                    FaceLandmarks = face.faceLandmarks,
                    PrimaryEmotion = (face.faceAttributes.emotion != null) ?
                        GetTopEmotion(face.faceAttributes.emotion) :
                        null,
                    Time = DateTime.UtcNow,
                    Device = device.DeviceLabel,
                    Exhibit = device.Exhibit,
                    CurrentFaceListId = currentFaceListId,
                    IsInList = (similarPersistedFaces.Count() > 0),
                    FaceMatchConfidence = persistedFaceConfidence
                });
            }

            //log patron data to storage
            await _storeRepository.StorePatrons(patrons);

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

        private static string FaceRectangleToString(FaceRectangle faceRectangle)
        {
            var result = string.Empty;
            result += faceRectangle.left + ",";
            result += faceRectangle.top + ",";
            result += faceRectangle.width + ",";
            result += faceRectangle.height;
            return result;
        }

    }
}