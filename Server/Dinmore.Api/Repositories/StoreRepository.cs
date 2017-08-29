﻿using dinmore.api.Interfaces;
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

            DeviceTE deviceTe = new DeviceTE(device.Id.ToString(), device.Venue);
            deviceTe.Device = device.Label;
            deviceTe.Exhibit = device.Exhibit;
            deviceTe.Venue = device.Venue;

            TableOperation insertOperation = TableOperation.Insert(deviceTe);

            await table.ExecuteAsync(insertOperation);
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
