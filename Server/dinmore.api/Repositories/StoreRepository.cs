using dinmore.api.Interfaces;
using dinmore.api.Models;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace dinmore.api.Repositories
{

    public class StoreRepository : IStoreRepository
    {
        private readonly AppSettings _appSettings;

        public StoreRepository(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        public async Task Store(List<Patron> patrons)
        {
            var storageAccount = CloudStorageAccount.Parse(_appSettings.TableStorageConnectionString);

            //Connect the client
            var blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            var container = blobClient.GetContainerReference(_appSettings.StoreContainerName);

            //Create the container if it doesn't already exist.
            await container.CreateIfNotExistsAsync();

            //add the json blob for each patron as a new blob in storage
            foreach (var patron in patrons)
            {
                var blockBlob = container.GetBlockBlobReference(patron.PersistedFaceId.ToString());

                string output = JsonConvert.SerializeObject(patron);

                await blockBlob.UploadTextAsync(output);
            }

        }

    }
}
