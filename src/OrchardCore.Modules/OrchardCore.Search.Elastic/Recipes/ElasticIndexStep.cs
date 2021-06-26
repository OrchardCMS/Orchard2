using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OrchardCore.Search.Elastic.Model;
using OrchardCore.Recipes.Models;
using OrchardCore.Recipes.Services;

namespace OrchardCore.Search.Elastic.Recipes
{
    /// <summary>
    /// This recipe step creates a Elastic index.
    /// </summary>
    public class ElasticIndexStep : IRecipeStepHandler
    {
        private readonly ElasticIndexingService _elasticIndexingService;
        private readonly ElasticIndexManager _elasticIndexManager;

        public ElasticIndexStep(
            ElasticIndexingService elasticIndexingService,
            ElasticIndexManager elasticIndexManager
            )
        {
            _elasticIndexManager = elasticIndexManager;
            _elasticIndexingService = elasticIndexingService;
        }

        public async Task ExecuteAsync(RecipeExecutionContext context)
        {
            if (!String.Equals(context.Name, "elastic-index", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var indices = context.Step["Indices"];
            if (indices != null)
            {
                foreach (var index in indices)
                {
                    var elasticIndexSettings = index.ToObject<Dictionary<string, ElasticIndexSettings>>().FirstOrDefault();

                    if (! await _elasticIndexManager.Exists(elasticIndexSettings.Key))
                    {
                        elasticIndexSettings.Value.IndexName = elasticIndexSettings.Key;
                        await _elasticIndexingService.CreateIndexAsync(elasticIndexSettings.Value);
                    }
                }
            }
        }
    }

    public class ContentStepModel
    {
        public JObject Data { get; set; }
    }
}
