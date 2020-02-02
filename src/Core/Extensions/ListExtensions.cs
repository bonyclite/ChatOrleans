using System.Collections.Generic;

namespace Core.Extensions
{
    public static class ListExtensions
    {
        public static void RemoveAll<T>(this List<T> self)
        {
            self.RemoveAll(obj => true);
        }
    }
}