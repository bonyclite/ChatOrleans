using DAL.Models;

namespace DAL.Repositories
{
    internal interface IInternalGenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : BaseModel
    {
        ChatDbContext Context { get; }
    }
}