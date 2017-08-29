using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using dinmore.api.Interfaces;
using Dinmore.Api.Models;

namespace Dinmore.Api.Controllers
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
        /// Adds a device to the store for devices
        /// </summary>
        /// <param name="Label">The label for the device inte context of the exhibit. Normally 001, 002, 003 etc</param>
        /// <param name="Exhibit">The label for the exhibit where the device is housed. i.e. ShrewsburyPanoramic</param>
        /// <param name="Venue">The label for venue where the exhibit is shoused. i.e. the museum name</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Post(Device device)
        {
            var deviceId = Guid.NewGuid();
            device.Id = deviceId;

            //log device data to storage
            await _storeRepository.StoreDevice(device);

            return Json(deviceId);
        }

    }
}