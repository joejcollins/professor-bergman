using dinmore.api.Interfaces;
using dinmore.api.Models;
using Microsoft.Extensions.Options;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
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
            await StoreBlob(patrons);
            await StoreTable(patrons);
        }

        private async Task StoreTable(List<Patron> patrons)
        {
            var storageAccount = CloudStorageAccount.Parse(_appSettings.TableStorageConnectionString);

            var blobClient = storageAccount.CreateCloudTableClient();

            var table = blobClient.GetTableReference(_appSettings.StoreContainerName);

            await table.CreateIfNotExistsAsync();

            //insert an entity (row) per patron
            foreach (var patron in patrons)
            {
                var persistedFaceId = patron.PersistedFaceId;
                var sightingId = Guid.NewGuid().ToString(); //This is a unique ID for the sighting

                PatronSighting patronSighting = new PatronSighting(persistedFaceId, sightingId);
                patronSighting.Device = patron.Device;
                patronSighting.Exhibit = patron.Exhibit;
                patronSighting.Gender = patron.FaceAttributes.gender;
                patronSighting.Age = Math.Round(patron.FaceAttributes.age,0);
                patronSighting.PrimaryEmotion = patron.PrimaryEmotion;
                patronSighting.TimeOfSighting = (DateTime)patron.Time;
                patronSighting.Smile = patron.FaceAttributes.smile;
                patronSighting.Glasses = patron.FaceAttributes.glasses;

                TableOperation insertOperation = TableOperation.Insert(patronSighting);

                await table.ExecuteAsync(insertOperation);
            }


        }

        private async Task StoreBlob(List<Patron> patrons)
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
