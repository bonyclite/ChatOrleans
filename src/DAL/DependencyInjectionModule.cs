using DAL.Repositories;
using DAL.Repositories.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace DAL
{
    public class DependencyInjectionModule
    {
        public static void Load(IServiceCollection collection)
        {
            collection.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        }
    }
}