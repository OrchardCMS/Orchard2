using OrchardCore.Alias.Indexes;
using OrchardCore.Alias.Models;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.Data.Migration;
using YesSql.Sql;

namespace OrchardCore.Alias
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
            _contentDefinitionManager.AlterPartDefinition(nameof(AliasPart), builder => builder
                .Attachable()
                .WithDescription("Provides a way to define custom aliases for content items."));

            // NOTE: The Alias Length has been upgraded from 64 characters to 767.
            // For existing SQL databases update the AliasPartIndex tables Alias column length manually.
            // INFO: The Alias Length is now of 740 chars, but this is only used on a new installation.
            SchemaBuilder.CreateMapIndexTable<AliasPartIndex>(table => table
                .Column<string>("Alias", col => col.WithLength(AliasPart.MaxAliasLength))
                .Column<string>("ContentItemId", c => c.WithLength(26))
                .Column<bool>("Latest", c => c.WithDefault(false))
                .Column<bool>("Published", c => c.WithDefault(true))
            );

            SchemaBuilder.AlterIndexTable<AliasPartIndex>(table => table
                .CreateIndex("IDX_AliasPartIndex_DocumentId",
                    "DocumentId",
                    "Alias",
                    "ContentItemId",
                    "Published",
                    "Latest")
            );

            // Shortcut other migration steps on new content definition schemas.
            return 4;
        }

        // This code can be removed in a later version as Latest and Published are alterations.
        public int UpdateFrom1()
        {
            SchemaBuilder.AlterIndexTable<AliasPartIndex>(table => table
                .AddColumn<bool>("Latest", c => c.WithDefault(false))
            );

            SchemaBuilder.AlterIndexTable<AliasPartIndex>(table => table
                .AddColumn<bool>("Published", c => c.WithDefault(true))
            );

            return 2;
        }

        // This code can be removed in a later version.
        public int UpdateFrom2()
        {
            // Can't be fully created on existing databases where the 'Alias' may be of 767 chars.
            SchemaBuilder.AlterIndexTable<AliasPartIndex>(table => table
                //.CreateIndex("IDX_AliasPartIndex_DocumentId",
                //    "DocumentId",
                //    "Alias",
                //    "ContentItemId",
                //    "Latest",
                //    "Published")

                .CreateIndex("IDX_AliasPartIndex_DocumentId",
                    "DocumentId",
                    "Alias")
            );

            return 3;
        }

        // This code can be removed in a later version.
        public int UpdateFrom3()
        {
            // In the previous migration step, an index was not fully created,
            // but here, we can create a separate one for the missing columns.
            SchemaBuilder.AlterIndexTable<AliasPartIndex>(table => table
                .CreateIndex("IDX_AliasPartIndex_DocumentId_ContentItemId",
                    "DocumentId",
                    "ContentItemId",
                    "Published",
                    "Latest")
            );

            return 4;
        }

        public void Uninstall()
        {
            SchemaBuilder.DropMapIndexTable<AliasPartIndex>();
        }
    }
}
