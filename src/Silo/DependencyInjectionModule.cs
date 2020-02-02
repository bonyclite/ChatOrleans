using Microsoft.Extensions.DependencyInjection;

namespace Silo
{
    public class DependencyInjectionModule
    {
        public static void Load(IServiceCollection collection)
        {
            DAL.DependencyInjectionModule.Load(collection);
        }
    }
}