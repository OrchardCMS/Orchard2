using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.DataLoader;

namespace OrchardCore.Apis.GraphQL
{
    public static class DataLoaderExtensions
    {
        public static async Task<T[]> LoadAsync<TKey, T>(this IDataLoader<TKey, T> dataLoader, IEnumerable<TKey> keys)
        {
            var items = new List<T>();

            foreach (var key in keys)
            {
                items.Add(await dataLoader.LoadAsync(key));
            }

            return items.ToArray();
        }

        public static Task<T[]> LoadAsync<TKey, T>(this IDataLoader<TKey, T> dataLoader, params TKey[] keys)
        {
            return dataLoader.LoadAsync(keys.AsEnumerable());
        }
    }
}
