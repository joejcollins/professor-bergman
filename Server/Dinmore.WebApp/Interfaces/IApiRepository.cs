using Dinmore.WebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dinmore.WebApp.Interfaces
{
    public interface IApiRepository
    {
        Task<string> StoreDevice(Device device);

        Task<string> DeleteDevice(string deviceId);

        Task<IEnumerable<Device>> GetDevices();
    }
}
