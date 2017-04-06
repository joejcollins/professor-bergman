using System.Collections.Generic;
using dinmore.api.Models;
using System.Threading.Tasks;

namespace dinmore.api.Interfaces
{
    public interface IStoreRepository
    {
        Task Store(List<Patron> patrons);
    }
}
