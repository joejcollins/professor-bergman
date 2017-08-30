using Dinmore.WebApp.Interfaces;
using Dinmore.WebApp.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dinmore.WebApp.Repositories
{
    public class ApiRepository : IApiRepository
    {
        private readonly AppSettings _appSettings;

        public ApiRepository(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        public async Task<string> StoreDevice(Device device)
        {
            return "ok";
        }

        public async Task<string> DeleteDevice(string deviceId)
        {
            return null;
        }

        public async Task<IEnumerable<Device>> GetDevices()
        {
            //mock data
            var devices = new List<Device>();
            devices.Add(new Device() { Id = Guid.Parse("09937208-5c7f-4ae0-a698-7beddeb2e272"), DeviceLabel = "Mason", Exhibit = "MakerSpace", Venue = "HarrisMuseum" });
            devices.Add(new Device() { Id = Guid.Parse("2e8ad036-4ebc-4555-8e3a-9502e456c768"), DeviceLabel = "Joe", Exhibit = "MakerSpace", Venue = "HarrisMuseum" });
            devices.Add(new Device() { Id = Guid.Parse("9a651e76-615b-4694-b7e5-466b3f53cbd3"), DeviceLabel = "MartinB", Exhibit = "MakerSpace", Venue = "HarrisMuseum" });
            devices.Add(new Device() { Id = Guid.Parse("de724d1e-85ba-416b-8784-93ab178b68a8"), DeviceLabel = "MartinK", Exhibit = "MakerSpace", Venue = "HarrisMuseum" });



            return devices;
        }
    }
}
