using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories.Implementation
{
    internal class GenericRepository<TEntity>: IInternalGenericRepository<TEntity> where TEntity : BaseModel
    {
        public ChatDbContext Context { get; }

        public GenericRepository(ChatDbContext context)
        {
            Context = context;
        }

        public virtual IQueryable<TEntity> GetAll()
        {
            return Context.Set<TEntity>();
        }

        public IQueryable<TEntity> GetAll(Expression<Func<TEntity, bool>> expression)
        {
            return GetAll().Where(expression);
        }

        public virtual async Task<TEntity> GetById(Guid id)
        {
            return await Context.Set<TEntity>().FindAsync(id);
        }

        public virtual async Task<TEntity> Create(TEntity entity)
        {
            if (entity.CreateDate == DateTime.MinValue)
            {
                entity.CreateDate = DateTime.UtcNow;
            }
            
            var result = await Context.Set<TEntity>().AddAsync(entity);
            await Context.SaveChangesAsync();

            return result.Entity;
        }

        public virtual async Task Update(TEntity entity)
        { 
            Context.Set<TEntity>().Update(entity);
            await Context.SaveChangesAsync();
        }

        public virtual async Task Delete(Guid id)
        {
            var entity = await GetById(id);
            Context.Set<TEntity>().Remove(entity);
            await Context.SaveChangesAsync();
        }

        public virtual async Task DeleteWhere(Expression<Func<TEntity, bool>> expression)
        {
            var entities = await GetAll(expression)
                .ToArrayAsync();

            foreach (var entity in entities)
            {
                Context.Entry(entity).State = EntityState.Deleted;
            }

            await Context.SaveChangesAsync();
        }

        public async Task<bool> Any(Expression<Func<TEntity, bool>> expression)
        {
            return await Context.Set<TEntity>().AnyAsync(expression);
        }
    }
}