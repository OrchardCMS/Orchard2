using System.Threading.Tasks;
using OrchardCore.AdminDashboard.Indexes;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using OrchardCore.Recipes.Services;
using YesSql.Sql;

namespace OrchardCore.AdminDashboard
{
    public class Migrations : DataMigration
    {
        private IContentDefinitionManager _contentDefinitionManager;
        private readonly IRecipeMigrator _recipeMigrator;

        public Migrations(IContentDefinitionManager contentDefinitionManager, IRecipeMigrator recipeMigrator)
        {
            _contentDefinitionManager = contentDefinitionManager;
            _recipeMigrator = recipeMigrator;
        }

        public async Task<int> CreateAsync()
        {
            SchemaBuilder.CreateMapIndexTable<DashboardPartIndex>(table => table
               .Column<double>("Position")
            );

            SchemaBuilder.AlterIndexTable<DashboardPartIndex>(table => table
                .CreateIndex("IDX_DashboardPart_DocumentId",
                    "DocumentId",
                    nameof(DashboardPartIndex.Position))
            );

            _contentDefinitionManager.AlterPartDefinition("DashboardPart", builder => builder
                .Attachable()
                .WithDescription("Provides a way to add widgets to a dashboard.")
                );

            await _recipeMigrator.ExecuteAsync("dashboard-widgets.recipe.json", this);

            // Shortcut other migration steps on new content definition schemas.
            return 3;
        }

        public async Task<int> UpdateFrom1Async()
        {
            await _recipeMigrator.ExecuteAsync("dashboard-widgets.recipe.json", this);

            return 2;
        }

        // This code can be removed in a later version.
        public int UpdateFrom2()
        {
            SchemaBuilder.AlterIndexTable<DashboardPartIndex>(table => table
                .CreateIndex("IDX_DashboardPart_DocumentId",
                    "DocumentId",
                    nameof(DashboardPartIndex.Position))
            );

            return 3;
        }

        public void Uninstall()
        {
            SchemaBuilder.DropMapIndexTable<DashboardPartIndex>();
        }
    }
}
