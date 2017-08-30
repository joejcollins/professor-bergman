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
                var device = new Device()
                {
                    DeviceLabel = collection["DeviceLabel"],
                    Exhibit = collection["Exhibit"],
                    Venue = collection["Venue"],
                    Id = Guid.NewGuid(),
                };

                var result = _apiRepository.StoreDevice(device);

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
            var data = await _apiRepository.GetDevices();
            var device = from d in data.ToList()
                         where d.Id == id
                         select d;

            return View(device);
        }

        // POST: Devices/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(Guid id, IFormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}