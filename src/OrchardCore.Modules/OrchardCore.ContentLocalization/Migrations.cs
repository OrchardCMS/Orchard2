using OrchardCore.ContentLocalization.Models;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using YesSql.Sql;

namespace OrchardCore.ContentLocalization.Records
{
    public class Migrations : DataMigration
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;

        public Migrations(IContentDefinitionManager contentDefinitionManager)
        {
            _contentDefinitionManager = contentDefinitionManager;
        }

        public int Create()
        {
            _contentDefinitionManager.AlterPartDefinition(nameof(LocalizationPart), builder => builder
                .Attachable()
                .WithDescription("Provides a way to create localized version of content."));

            SchemaBuilder.CreateMapIndexTable<LocalizedContentItemIndex>(table => table
                .Column<string>("LocalizationSet", col => col.WithLength(26))
                .Column<string>("Culture", col => col.WithLength(16))
                .Column<string>("ContentItemId", c => c.WithLength(26))
                .Column<bool>("Published")
                .Column<bool>("Latest")
            );

            SchemaBuilder.AlterIndexTable<LocalizedContentItemIndex>(table => table
                .CreateIndex("IDX_LocalizationPartIndex_DocumentId",
                "DocumentId",
                "LocalizationSet",
                "Culture",
                "ContentItemId",
                "Published",
                "Latest")
            );

            // Shortcut other migration steps on new content definition schemas.
            return 3;
        }

        // This code can be removed in a later version.
        public int UpdateFrom1()
        {
            SchemaBuilder.AlterIndexTable<LocalizedContentItemIndex>(table => table
                .AddColumn<bool>(nameof(LocalizedContentItemIndex.Published)));

            return 2;
        }

        // This code can be removed in a later version.
        public int UpdateFrom2()
        {
            SchemaBuilder.AlterIndexTable<LocalizedContentItemIndex>(table => table
                .AddColumn<bool>(nameof(LocalizedContentItemIndex.Latest))
            );

            SchemaBuilder.AlterIndexTable<LocalizedContentItemIndex>(table => table
                .CreateIndex("IDX_LocalizationPartIndex_DocumentId",
                "DocumentId",
                "LocalizationSet",
                "Culture",
                "ContentItemId",
                "Published",
                "Latest")
            );

            return 3;
        }

        public void Uninstall()
        {
            SchemaBuilder.DropMapIndexTable<LocalizedContentItemIndex>();
        }
    }
}
