using dinmore.api.Interfaces;
using dinmore.api.Models;
using Dinmore.Api.Helpers;
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
        /// </summary>
        /// <param name="deviceId">A string representing the device ID (guid) of the device making the post. Will return BadRequest if this is missing or does not map to a device in the devices table</param>
        /// <param name="returnFaceLandmarks">To return things like 'upperLipBottom', there are 27 landmarks in total. Defaults to true</param>
        /// <param name="returnFaceAttributes">To return specific attributes for each face. Accepts a comma-delimited list. Defaults to age,gender,headPose,smile,facialHair,glasses,emotion,hair,makeup,occlusion,accessories,blur,exposure,noise which is the full data set avaliable from Cognitive Services</param>
        /// <returns>An array of Patron objects in JSON format</returns>
        [HttpPost]
        public async Task<IActionResult> Post(string deviceId, bool returnFaceLandmarks = true, string returnFaceAttributes = "age,gender,headPose,smile,facialHair,glasses,emotion,hair,makeup,occlusion,accessories,blur,exposure,noise")
        {
            if (string.IsNullOrEmpty(deviceId)) return BadRequest();

            //get the device from storage based on the device id
            var device = await _storeRepository.GetDevice(deviceId);
            if (device == null) return BadRequest();

            //read body of request into a byte array
            byte[] bytes = Helpers.ReadFileStream(Request.Body);

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
                    DeviceLabel = device.DeviceLabel,
                    Exhibit = device.Exhibit,
                    Venue = device.Venue,
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
        /// Works out the highest scoring emotion from a range of emotion scores encapsulated in an Emotion object
        /// </summary>
        /// <param name="emotion">An Emotion object</param>
        /// <returns>A string representing the highest scoring emotion</returns>
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



        /// <summary>
        /// Converts a FaceRectangle to a comma delimited string of coordinates
        /// </summary>
        /// <param name="faceRectangle">A FaceRectangle object</param>
        /// <returns>Comma delimited string of coordinates</returns>
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