using Dinmore.Uwp.Constants;
using Dinmore.Uwp.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace Dinmore.Uwp.Helpers
{
    public static class Api
    {
        public static async Task<Device> GetDevice(ResourceLoader appSettings, string deviceId)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var responseMessage = await httpClient.GetAsync(appSettings.GetString("DeviceApiUrl"));

                    if (!responseMessage.IsSuccessStatusCode)
                    {
                        //LogStatusMessage($"The Device API returned a non-sucess status {responseMessage.ReasonPhrase}", StatusSeverity.Error, false);
                        return null;
                    }

                    //This will return all devices in the Azure table store because there is not yet an API call to get a specific device
                    //TO DO: Add and API call to return a speicfic device and update this code to use that call
                    var response = await responseMessage.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<List<Device>>(response);

                    //filter for this device. We won't need to do this if/when the API gets updated with the capability to return a specific device
                    return result.Where(d => d.Id.ToString() == deviceId).FirstOrDefault();
                }
            }
            catch
            {
                return null;
            }
        }

        public static async Task<string> UpdateDevice(ResourceLoader appSettings, Device newDevice)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(appSettings.GetString("DeviceApiUrl") + "/" + newDevice.Id.ToString());

                    //construct full API endpoint uri
                    var fullUrl = $"{httpClient.BaseAddress}?DeviceLabel={newDevice.DeviceLabel}&Exhibit={newDevice.Exhibit}&Venue={newDevice.Venue}&Interactive={newDevice.Interactive}&VerbaliseSystemInformationOnBoot={newDevice.VerbaliseSystemInformationOnBoot}&SoundOn={newDevice.SoundOn}&ResetOnBoot=false&VoicePackageUrl={newDevice.VoicePackageUrl}&QnAKnowledgeBaseId={newDevice.QnAKnowledgeBaseId}";

                    var responseMessage = await httpClient.PutAsync(fullUrl, null);

                    if (!responseMessage.IsSuccessStatusCode)
                    {
                        //LogStatusMessage($"The Device API returned a non-sucess status {responseMessage.ReasonPhrase}", StatusSeverity.Error, false);
                        return null;
                    }

                    return await responseMessage.Content.ReadAsStringAsync();
                }
            }
            catch
            {
                return null;
            }
        }

        public static async Task<List<Face>> PostPatron(ResourceLoader appSettings, byte[] image)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var content = new StreamContent(new MemoryStream(image));
                    content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/octet-stream");

                    //build url to pass to api
                    var url = appSettings.GetString("FaceApiUrl");
                    var returnFaceLandmarks = appSettings.GetString("ReturnFaceLandmarks");
                    var returnFaceAttributes = appSettings.GetString("ReturnFaceAttributes");
                    url = $"{url}?deviceid={Settings.GetString(DeviceSettingKeys.DeviceIdKey)}&returnFaceLandmarks={returnFaceLandmarks}&returnFaceAttributes={returnFaceAttributes}";

                    var responseMessage = await httpClient.PostAsync(url, content);

                    if (!responseMessage.IsSuccessStatusCode)
                    {
                        switch (responseMessage.StatusCode.ToString())
                        {
                            case "BadRequest":
                                //LogStatusMessage("The API returned a 400 Bad Request. This is caused by either a missing DeviceId parameter or one containig a GUID that is not already registered with the device API.", StatusSeverity.Error, false);
                                break;
                            default:
                                //LogStatusMessage($"The API returned a non-sucess status {responseMessage.ReasonPhrase}", StatusSeverity.Error, false);
                                break;
                        }
                        return null;
                    }

                    var response = await responseMessage.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<List<Face>>(response);

                    return result;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
