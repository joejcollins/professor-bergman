using Dinmore.WebApp.Interfaces;
using Dinmore.WebApp.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
            var responseString = string.Empty;
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_appSettings.ApiRoot + "/api/devices");

                //construct full API endpoint uri
                var parameters = GetDeviceQueryParams(device);
                var apiUri = QueryHelpers.AddQueryString(httpClient.BaseAddress.ToString(), parameters);

                var responseMessage = await httpClient.PostAsync(apiUri, null);
                responseString = await responseMessage.Content.ReadAsStringAsync();
            }

            return responseString;
        }

        public async Task<string> DeleteDevice(Guid deviceId)
        {
            var responseString = string.Empty;
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_appSettings.ApiRoot + "/api/devices");

                //construct full API endpoint uri
                var parameters = new Dictionary<string, string> {
                    { "DeviceId", deviceId.ToString() }
                };
                var apiUri = QueryHelpers.AddQueryString(httpClient.BaseAddress.ToString(), parameters);

                var responseMessage = await httpClient.DeleteAsync(apiUri);
                responseString = await responseMessage.Content.ReadAsStringAsync();
            }

            return responseString;
        }

        public async Task<string> ReplaceDevice(Device device)
        {
            var responseString = string.Empty;
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_appSettings.ApiRoot + "/api/devices/" +device.Id.ToString());

                //construct full API endpoint uri
                var parameters = GetDeviceQueryParams(device);
                var apiUri = QueryHelpers.AddQueryString(httpClient.BaseAddress.ToString(), parameters);

                var responseMessage = await httpClient.PutAsync(apiUri, null);
                responseString = await responseMessage.Content.ReadAsStringAsync();
            }

            return responseString;
        }

        public async Task<IEnumerable<Device>> GetDevices()
        {
            var responseString = string.Empty;
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_appSettings.ApiRoot + "/api/devices");
                var responseMessage = await httpClient.GetAsync(httpClient.BaseAddress);
                responseString = await responseMessage.Content.ReadAsStringAsync();
            }
            
            var responseArray = JArray.Parse(responseString);
            var devices = new List<Device>();
            foreach (var deviceResponse in responseArray)
            {
                var face = JsonConvert.DeserializeObject<Device>(deviceResponse.ToString());
                devices.Add(face);
            }

            return devices;
        }

        private Dictionary<string, string> GetDeviceQueryParams(Device device)
        {
            var parameters = new Dictionary<string, string> {
                    { "DeviceLabel", device.DeviceLabel},
                    { "Exhibit", device.Exhibit },
                    { "Venue", device.Venue },
                };

            return parameters;
        }

    }
}
