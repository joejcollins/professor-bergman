using dinmore.api.Models;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace dinmore.api.TableStorage
{

    public class StoreApiResults : IStoreApiResults
    {
        private readonly AppSettings _appSettings;

        public StoreApiResults(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        public async Task Store(List<Face> faces)
        {
            var storageAccount = CloudStorageAccount.Parse(_appSettings.TableStorageConnectionString);

            //Connect the client
            var blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            var container = blobClient.GetContainerReference("facesdata");
            //Create the container if it doesn't already exist.
            await container.CreateIfNotExistsAsync();

            foreach (var face in faces)
            {
                var blockBlob = container.GetBlockBlobReference(face.faceId.ToString());

                string output = JsonConvert.SerializeObject(face);
                await blockBlob.UploadTextAsync(output);
            }

        }



    }
}
