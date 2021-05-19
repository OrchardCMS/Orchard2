using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using OrchardCore.Lists.Indexes;
using OrchardCore.Lists.Models;
using YesSql.Sql;

namespace OrchardCore.Lists
{
    public class Migrations : DataMigration
    {
        private IContentDefinitionManager _contentDefinitionManager;

        public Migrations(IContentDefinitionManager contentDefinitionManager)
        {
            _contentDefinitionManager = contentDefinitionManager;
        }

        public int Create()
        {
            _contentDefinitionManager.AlterPartDefinition("ListPart", builder => builder
                .Attachable()
                .WithDescription("Add a list behavior."));

            SchemaBuilder.CreateMapIndexTable<ContainedPartIndex>(table => table
                .Column<string>("ListContentItemId", c => c.WithLength(26))
                .Column<int>("Order")
            );

            SchemaBuilder.AlterIndexTable<ContainedPartIndex>(table => table
                .CreateIndex("IDX_ContainedPartIndex_DocumentId", "DocumentId", "ListContentItemId", "Order")
            );

            // Shortcut other migration steps on new content definition schemas.
            return 3;
        }

        // Migrate PartSettings. This only needs to run on old content definition schemas.
        // This code can be removed in a later version.
        public int UpdateFrom1()
        {
            _contentDefinitionManager.MigratePartSettings<ListPart, ListPartSettings>();

            return 2;
        }

        // This code can be removed in a later version.
        public int UpdateFrom2()
        {
            SchemaBuilder.AlterIndexTable<ContainedPartIndex>(table => table
                .CreateIndex("IDX_ContainedPartIndex_DocumentId", "DocumentId", "ListContentItemId", "Order")
            );

            return 3;
        }

        public void Uninstall()
        {
            SchemaBuilder.DropMapIndexTable<ContainedPartIndex>();
        }
    }
}
