using OrchardCore.Autoroute.Models;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Data.Migration;
using YesSql.Sql;

namespace OrchardCore.Autoroute
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
            _contentDefinitionManager.AlterPartDefinition("AutoroutePart", builder => builder
                .Attachable()
                .WithDescription("Provides a custom url for your content item."));

            SchemaBuilder.CreateMapIndexTable<AutoroutePartIndex>(table => table
                .Column<string>("ContentItemId", c => c.WithLength(26))
                .Column<string>("ContainedContentItemId", c => c.WithLength(26))
                .Column<string>("JsonPath", c => c.Unlimited())
                .Column<string>("Path", col => col.WithLength(AutoroutePart.MaxPathLength))
                .Column<bool>("Published")
                .Column<bool>("Latest")
            );

            // Shortcut other migration steps on new content definition schemas.
            return 5;
        }

        // Migrate PartSettings. This only needs to run on old content definition schemas.
        // This code can be removed in a later version.
        public int UpdateFrom1()
        {
            _contentDefinitionManager.MigratePartSettings<AutoroutePart, AutoroutePartSettings>();

            return 2;
        }

        // This code can be removed in a later version.
        public int UpdateFrom2()
        {
            return 3;
        }

        // This code can be removed in a later version.
        public int UpdateFrom3()
        {
            SchemaBuilder.AlterIndexTable<AutoroutePartIndex>(table => table
                .AddColumn<string>("ContainedContentItemId", c => c.WithLength(26))
            );

            SchemaBuilder.AlterIndexTable<AutoroutePartIndex>(table => table
                .AddColumn<string>("JsonPath", c => c.Unlimited())
            );

            SchemaBuilder.AlterIndexTable<AutoroutePartIndex>(table => table
                .AddColumn<bool>("Latest", c => c.WithDefault(false))
            );

            return 4;
        }

        // This code can be removed in a later version.
        public int UpdateFrom4()
        {
            return 5;
        }

        public void Uninstall()
        {
            SchemaBuilder.DropMapIndexTable<AutoroutePartIndex>();
        }
    }
}
