using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using dinmore.api.Interfaces;
using Dinmore.Api.Models;
using Dinmore.Api.Helpers;
using Dinmore.Domain;
using System.IO;

namespace dinmore.api.Controllers
{
    [Produces("application/json")]
    [Route("api/Devices")]
    public class DevicesController : Controller
    {
        private readonly IStoreRepository _storeRepository;

        public DevicesController(IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository;
        }

        /// <summary>
        /// Returns an array of all devices in the storage table
        /// </summary>
        /// <returns>An array of all devices in the storage table</returns>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            //log device data to storage
            var devices = await _storeRepository.GetDevices();

            return Ok(devices);
        }

        /// <summary>
        /// Stores device data in the device Azure Table device store
        /// </summary>
        /// <param name="device">A device object containing a complete set of data representing a device</param>
        /// <returns>200/OK with the guid for the newly created device attached</returns>
        [HttpPost]
        public async Task<IActionResult> Post(Device device)
        {
            var deviceId = Guid.NewGuid();
            device.Id = deviceId;

            //log device data to storage
            await _storeRepository.StoreDevice(device);

            return Ok(deviceId);
        }

        /// <summary>
        /// Updates an existing device in the Azure Table device store
        /// </summary>
        /// <param name="id">IThe ID (guid) of the device to be updated</param>
        /// <param name="device">Device object with new data for the device</param>
        /// <returns>200/OK with new device data attached</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, Device device)
        {
            // Get file if there is one
            byte[] voicePackage = Helpers.ReadFileStream(Request.Body);
            device.VoicePackage = voicePackage;

            // Update device data to storage
            await _storeRepository.ReplaceDevice(device);

            return Ok(device);
        }

        /// <summary>
        /// Deletes a device from storage
        /// </summary>
        /// <param name="deviceId">The ID (guid) of the device to be deleted</param>
        /// <returns>204/NoContent message indicating that the device was deleted and there is no content</returns>
        [HttpDelete]
        public async Task<IActionResult> Delete(string deviceId)
        {
            //remove the device from storage
            await _storeRepository.DeleteDevice(deviceId);

            return NoContent();
        }

    }
}