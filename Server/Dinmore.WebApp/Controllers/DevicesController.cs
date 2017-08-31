using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Dinmore.WebApp.Interfaces;
using Dinmore.WebApp.Models;

namespace Dinmore.WebApp.Controllers
{
    public class DevicesController : Controller
    {
        private readonly IApiRepository _apiRepository;

        public DevicesController(IApiRepository apiRepository)
        {
            _apiRepository = apiRepository;
        }

        // GET: Devices/Details/5
        public async Task<ActionResult> Details(Guid id)
        {
            var device = await GetDeviceById(id);

            return View(device);
        }

        // GET: Devices
        public async Task<ActionResult> Index()
        {
            var data = await _apiRepository.GetDevices();

            return View(data);
        }


        // GET: Devices/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Devices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                var device = CastFormCollectionToDevice(collection);

                var result = _apiRepository.StoreDevice(device);

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Devices/Edit/5
        public async Task<ActionResult> Edit(Guid id)
        {
            var device = await GetDeviceById(id);

            return View(device);
        }

        // POST: Devices/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Guid id, IFormCollection collection)
        {
            try
            {
                var device = CastFormCollectionToDevice(collection);

                var result = _apiRepository.ReplaceDevice(device);

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }


        // GET: Devices/Delete/5
        public async Task<ActionResult> Delete(Guid id)
        {
            var device = await GetDeviceById(id);

            return View(device);
        }

        // POST: Devices/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(Guid id, IFormCollection collection)
        {
            try
            {
                var result = _apiRepository.DeleteDevice(id);

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        private async Task<Device> GetDeviceById(Guid id)
        {
            var data = await _apiRepository.GetDevices();
            var dataList = data.ToList();
            var device = dataList.Where(d => d.Id == id).FirstOrDefault();
            return device;
        }

        private Device CastFormCollectionToDevice(IFormCollection collection)
        {
            var id = (string.IsNullOrEmpty(collection["Id"])) ?
                Guid.NewGuid() :
                Guid.Parse(collection["Id"]);

            var device = new Device()
            {
                DeviceLabel = collection["DeviceLabel"],
                Exhibit = collection["Exhibit"],
                Venue = collection["Venue"],
                Interactive = CheckboxToBool(collection["Interactive"]),
                VerbaliseSystemInformationOnBoot = CheckboxToBool(collection["VerbaliseSystemInformationOnBoot"]),
                SoundOn = CheckboxToBool(collection["SoundOn"]),
                ResetOnBoot = CheckboxToBool(collection["ResetOnBoot"]),
                VoicePackageUrl = collection["VoicePackageUrl"],
                Id = id,
            };

            return device;
        }

        private bool CheckboxToBool(string checkValue)
        {
            return checkValue.Contains("true");
        }
    }
}