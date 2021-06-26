using System.Collections.Generic;
using System.Threading.Tasks;
using OrchardCore.Security.Permissions;

namespace OrchardCore.Search.Elastic
{
    public class Permissions : IPermissionProvider
    {
        private readonly ElasticIndexSettingsService _elasticIndexSettingsService;

        public static readonly Permission ManageIndexes = new Permission("ManageIndexes", "Manage Indexes");
        public static readonly Permission QueryElasticApi = new Permission("QueryElasticApi", "Query Elastic Api", new[] { ManageIndexes });

        public Permissions(ElasticIndexSettingsService elasticIndexSettingsService)
        {
            _elasticIndexSettingsService = elasticIndexSettingsService;
        }

        public async Task<IEnumerable<Permission>> GetPermissionsAsync()
        {
            var elasticIndexSettings = await _elasticIndexSettingsService.GetSettingsAsync();
            var result = new List<Permission>();
            foreach (var index in elasticIndexSettings)
            {
                var permission = new Permission("QueryElastic" + index.IndexName + "Index", "Query Elastic " + index.IndexName + " Index", new[] { ManageIndexes });
                result.Add(permission);
            }

            result.Add(ManageIndexes);
            result.Add(QueryElasticApi);

            return result;
        }

        public IEnumerable<PermissionStereotype> GetDefaultStereotypes()
        {
            return new[]
            {
                new PermissionStereotype
                {
                    Name = "Administrator",
                    Permissions = new[] { ManageIndexes }
                },
                new PermissionStereotype
                {
                    Name = "Editor",
                    Permissions = new[] { QueryElasticApi }
                }
            };
        }
    }
}
