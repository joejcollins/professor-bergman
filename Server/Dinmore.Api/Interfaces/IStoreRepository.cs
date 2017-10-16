using System.Collections.Generic;
using dinmore.api.Models;
using System.Threading.Tasks;
using Dinmore.Domain;

namespace dinmore.api.Interfaces
{
    public interface IStoreRepository
    {
        Task StorePatrons(List<Patron> patrons);

        Task StoreDevice(Device device);

        Task DeleteDevice(string deviceId);

        Task<Device> GetDevice(string deviceId);

        Task<IEnumerable<Device>> GetDevices();

        Task<Device> ReplaceDevice(Device device);

        Task<string> StoreVoicePackage(byte[] voicePackage, Device device);
    }
}
