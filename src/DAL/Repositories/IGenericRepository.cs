using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DAL.Models;

namespace DAL.Repositories
{
    public interface IGenericRepository<TEntity> where TEntity : BaseModel
    {
        IQueryable<TEntity> GetAll();
        Task<TEntity> GetById(Guid id);
        Task<TEntity> Create(TEntity entity);
        Task Update(TEntity entity);
        Task Delete(Guid id);
        Task<bool> Any(Expression<Func<TEntity, bool>> expression);
    }
}