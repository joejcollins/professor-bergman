using dinmore.api.Interfaces;
using dinmore.api.Models;
using Dinmore.Domain;
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
            //get cloudtable
            var table = await GetCloudTable(_appSettings.TableStorageConnectionString, _appSettings.StoreDeviceContainerName);

            TableEntityAdapter<Device> entity = new TableEntityAdapter<Device>(device, _appSettings.StoreDevicePartitionKey, device.Id.ToString());

            TableOperation insertOperation = TableOperation.Insert(entity);

            await table.ExecuteAsync(insertOperation);
        }

        public async Task DeleteDevice(string deviceId)
        {
            //get cloudtable
            var table = await GetCloudTable(_appSettings.TableStorageConnectionString, _appSettings.StoreDeviceContainerName);

            // Create a retrieve operation that expects a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<TableEntityAdapter<Device>>(_appSettings.StoreDevicePartitionKey, deviceId);

            // Execute the operation.
            TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);

            // Assign the result to a CustomerEntity.
            var deleteEntity = (TableEntityAdapter<Device>)retrievedResult.Result;

            // Create the Delete TableOperation.
            if (deleteEntity != null)
            {
                // Create the Delete TableOperation.
                TableOperation deleteOperation = TableOperation.Delete(deleteEntity);

                // Execute the operation.
                await table.ExecuteAsync(deleteOperation);
            }

        }

        public async Task<Device> GetDevice(string deviceId)
        {
            //get cloudtable
            var table = await GetCloudTable(_appSettings.TableStorageConnectionString, _appSettings.StoreDeviceContainerName);

            // Create a retrieve operation that takes a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<TableEntityAdapter<Device>>(_appSettings.StoreDevicePartitionKey, deviceId);

            // Execute the retrieve operation.
            TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);

            if (retrievedResult.Result != null)
            {
                // get the result and create a new device from the data
                var deviceResult = (TableEntityAdapter<Device>)retrievedResult.Result;

                return deviceResult.OriginalEntity;
            }
            else
            {
                return null;
            }
        }

        public async Task<IEnumerable<Device>> GetDevices()
        {
            //get cloudtable
            var table = await GetCloudTable(_appSettings.TableStorageConnectionString, _appSettings.StoreDeviceContainerName);

            TableContinuationToken token = null;

            var entities = new List<TableEntityAdapter<Device>>();
            do
            {
                var queryResult = await table.ExecuteQuerySegmentedAsync(new TableQuery<TableEntityAdapter<Device>>(), token);
                entities.AddRange(queryResult.Results);
                token = queryResult.ContinuationToken;
            } while (token != null);

            // create list of device objects from the storage entities
            var devices = new List<Device>();
            foreach (var deviceStorageTableEntity in entities)
            {
                devices.Add(deviceStorageTableEntity.OriginalEntity);
            }

            return devices;
        }

        public async Task<Device> ReplaceDevice(Device device)
        {
            // Get cloudtable
            var table = await GetCloudTable(_appSettings.TableStorageConnectionString,_appSettings.StoreDeviceContainerName);

            // Create the entity based on passed in device
            TableEntityAdapter<Device> entity = new TableEntityAdapter<Device>(device, _appSettings.StoreDevicePartitionKey, device.Id.ToString());

            // Create the InsertOrReplace operation
            TableOperation updateOperation = TableOperation.InsertOrMerge(entity);

            // Execute the operation.
            await table.ExecuteAsync(updateOperation);

            return device;
        }

        public async Task StorePatrons(List<Patron> patrons)
        {
            //get cloudtable
            var table = await GetCloudTable(_appSettings.TableStorageConnectionString, _appSettings.StorePatronContainerName);

            //insert an entity (row) per patron
            foreach (var patron in patrons)
            {
                var persistedFaceId = patron.PersistedFaceId;
                var sightingId = Guid.NewGuid().ToString(); //This is a unique ID for the sighting

                // TO DO: This should be updated to use the TableEntityAdapter<Patron> approach like we've done for device
                PatronStorageTableEntity patronStorageTableEntity = new PatronStorageTableEntity(persistedFaceId, sightingId);
                patronStorageTableEntity.Device = patron.DeviceLabel;
                patronStorageTableEntity.Exhibit = patron.Exhibit;
                patronStorageTableEntity.Venue = patron.Venue;
                patronStorageTableEntity.Gender = patron.FaceAttributes.gender;
                patronStorageTableEntity.Age = Math.Round(patron.FaceAttributes.age, 0);
                patronStorageTableEntity.PrimaryEmotion = patron.PrimaryEmotion;
                patronStorageTableEntity.TimeOfSighting = (DateTime)patron.Time;
                patronStorageTableEntity.Smile = patron.FaceAttributes.smile;
                patronStorageTableEntity.Glasses = patron.FaceAttributes.glasses;
                patronStorageTableEntity.FaceMatchConfidence = (double)patron.FaceMatchConfidence;

                TableOperation insertOperation = TableOperation.Insert(patronStorageTableEntity);

                await table.ExecuteAsync(insertOperation);
            }
        }

        public async Task<string> StoreVoicePackage(byte[] voicePackage, Device device)
        {
            //upload voice package if there is one
            var fileName = string.Empty;

            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_appSettings.TableStorageConnectionString);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference(_appSettings.StoreVoicePackageContainerName);

            // TO DO: Should we delete existing packages of matching file name of {device.Exhibit}-{device.DeviceLabel}-*.zip here

            // Retrieve reference to a blob named "myblob".
            fileName = $"{device.Exhibit}-{device.DeviceLabel}-{DateTime.Now.ToString("yyyyMMddHHmmss")}.zip";
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName);
            blockBlob.Properties.ContentType = "application/zip";

            // Create or overwrite the blob with contents from a local file.
            await blockBlob.UploadFromByteArrayAsync(voicePackage, 0, voicePackage.Length);

            var fullUrl = $"{_appSettings.StoreVoicePackagesRootUrl}{fileName}";

            return fullUrl;
        }


        private async Task<CloudTable> GetCloudTable(string tableConnectionString, string containerName)
        {
            var storageAccount = CloudStorageAccount.Parse(tableConnectionString);

            var blobClient = storageAccount.CreateCloudTableClient();

            var table = blobClient.GetTableReference(containerName);

            await table.CreateIfNotExistsAsync();

            return table;
        }

    }
}
