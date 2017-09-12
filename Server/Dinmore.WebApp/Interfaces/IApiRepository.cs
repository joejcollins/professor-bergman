using Dinmore.WebApp.Models;
using Dinmore.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dinmore.WebApp.Interfaces
{
    public interface IApiRepository
    {
        Task<string> StoreDevice(Device device);

        Task<string> DeleteDevice(Guid deviceId);

        Task<string> ReplaceDevice(Device device);

        Task<IEnumerable<Device>> GetDevices();
    }
}
