using System;
using System.Threading.Tasks;
using OrchardCore.Data.Migration;
using OrchardCore.Layers.Indexes;
using OrchardCore.Layers.Services;
using OrchardCore.Rules;
using YesSql.Sql;

namespace OrchardCore.Layers
{
    public class Migrations : DataMigration
    {
        private readonly ILayerService _layerService;
        private readonly IConditionIdGenerator _conditionIdGenerator;
        private readonly IRuleMigrator _ruleMigrator;

        public Migrations(
            ILayerService layerService,
            IConditionIdGenerator conditionIdGenerator,
            IRuleMigrator ruleMigrator)
        {
            _layerService = layerService;
            _conditionIdGenerator = conditionIdGenerator;
            _ruleMigrator = ruleMigrator;
        }

        public int Create()
        {
            SchemaBuilder.CreateMapIndexTable<LayerMetadataIndex>(table => table
               .Column<string>("Zone", c => c.WithLength(64))
            );

            SchemaBuilder.AlterIndexTable<LayerMetadataIndex>(table => table
                .CreateIndex("IDX_LayerMetadataIndex_DocumentId", "DocumentId", "Zone")
            );

            // Shortcut other migration steps on new content definition schemas.
            return 3;
        }

        // This code can be removed in a later version.
        public int UpdateFrom1()
        {
            SchemaBuilder.AlterIndexTable<LayerMetadataIndex>(table => table
                .CreateIndex("IDX_LayerMetadataIndex_DocumentId", "DocumentId", "Zone")
            );

            return 2;
        }

        public async Task<int> UpdateFrom2Async()
        {
            var layers = await _layerService.LoadLayersAsync();
            foreach (var layer in layers.Layers)
            {
                layer.LayerRule = new Rule();
                _conditionIdGenerator.GenerateUniqueId(layer.LayerRule);

#pragma warning disable 0618
                _ruleMigrator.Migrate(layer.Rule, layer.LayerRule);

                layer.Rule = String.Empty;
#pragma warning restore 0618
            }

            await _layerService.UpdateAsync(layers);

            return 3;
        }

        public void Uninstall()
        {
            SchemaBuilder.DropMapIndexTable<LayerMetadataIndex>();
        }
    }
}
