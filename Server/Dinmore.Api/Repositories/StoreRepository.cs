using dinmore.api.Interfaces;
using dinmore.api.Models;
using Dinmore.Api.Models;
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

        public async Task StoreDevice(Device device)
        {
            var storageAccount = CloudStorageAccount.Parse(_appSettings.TableStorageConnectionString);

            var blobClient = storageAccount.CreateCloudTableClient();

            var table = blobClient.GetTableReference(_appSettings.StoreDeviceContainerName);

            await table.CreateIfNotExistsAsync();

            DeviceStorageTableEntity deviceStorageTableEntity = new DeviceStorageTableEntity(device.Id.ToString(), _appSettings.StoreDevicePartitionKey);
            deviceStorageTableEntity.Device = device.Label;
            deviceStorageTableEntity.Exhibit = device.Exhibit;
            deviceStorageTableEntity.Venue = device.Venue;

            TableOperation insertOperation = TableOperation.Insert(deviceStorageTableEntity);

            await table.ExecuteAsync(insertOperation);
        }

        public async Task DeleteDevice(string deviceId)
        {
            var storageAccount = CloudStorageAccount.Parse(_appSettings.TableStorageConnectionString);

            var tableClient = storageAccount.CreateCloudTableClient();
            
            var table = tableClient.GetTableReference(_appSettings.StoreDeviceContainerName);

            // Create a retrieve operation that expects a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<DeviceStorageTableEntity>(_appSettings.StoreDevicePartitionKey, deviceId);

            // Execute the operation.
            TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);

            // Assign the result to a CustomerEntity.
            var deleteEntity = (DeviceStorageTableEntity)retrievedResult.Result;

            // Create the Delete TableOperation.
            if (deleteEntity != null)
            {
                TableOperation deleteOperation = TableOperation.Delete(deleteEntity);

                // Execute the operation.
                await table.ExecuteAsync(deleteOperation);
            }
        }

        public async Task StorePatrons(List<Patron> patrons)
        {
            var storageAccount = CloudStorageAccount.Parse(_appSettings.TableStorageConnectionString);

            var blobClient = storageAccount.CreateCloudTableClient();

            var table = blobClient.GetTableReference(_appSettings.StorePatronContainerName);

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
                patronSighting.FaceMatchConfidence = (double)patron.FaceMatchConfidence;

                TableOperation insertOperation = TableOperation.Insert(patronSighting);

                await table.ExecuteAsync(insertOperation);
            }
        }



    }
}
