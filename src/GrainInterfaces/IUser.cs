using System;
using System.Threading.Tasks;
using Orleans;

namespace GrainInterfaces
{
    public interface IUser : IGrainWithStringKey
    {
        Task<Guid> GetUserIdAsync();
    }
}