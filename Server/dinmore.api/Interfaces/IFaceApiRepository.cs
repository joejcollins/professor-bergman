using dinmore.api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dinmore.api.Interfaces
{
    public interface IFaceApiRepository
    {
        Task<IEnumerable<Face>> DetectFaces(byte[] image, bool returnFaceLandmarks, string returnFaceAttributes);


        Task<string> AddFaceToFaceList(byte[] image, string faceListId, string targetFace, string userData);


        Task<string> GetCurrentFaceListId();
    }
}
