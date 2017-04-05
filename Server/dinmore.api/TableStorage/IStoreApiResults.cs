using System.Collections.Generic;
using dinmore.api.Models;
using System.Threading.Tasks;

namespace dinmore.api.TableStorage
{
    public interface IStoreApiResults
    {
        Task Store(List<Face> faces);
    }
}
