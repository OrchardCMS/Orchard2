using System;
using System.Linq;
using System.Threading.Tasks;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Settings;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Data.Migration;
using OrchardCore.Taxonomies.Fields;
using OrchardCore.Taxonomies.Indexing;
using OrchardCore.Taxonomies.Settings;
using YesSql;
using YesSql.Sql;

namespace OrchardCore.Taxonomies
{
    public class Migrations : DataMigration
    {
        private readonly ISession _session;
        private IContentDefinitionManager _contentDefinitionManager;

        public Migrations(IContentDefinitionManager contentDefinitionManager, ISession session)
        {
            _contentDefinitionManager = contentDefinitionManager;
            _session = session;
        }

        public int Create()
        {
            _contentDefinitionManager.AlterTypeDefinition("Taxonomy", taxonomy => taxonomy
                .Draftable()
                .Versionable()
                .Creatable()
                .Listable()
                .WithPart("TitlePart", part => part.WithPosition("1"))
                .WithPart("AliasPart", part => part
                    .WithPosition("2")
                    .WithSettings(new AliasPartSettings
                    {
                        Pattern = "{{ Model.ContentItem | display_text | slugify }}"
                    }))
                .WithPart("AutoroutePart", part => part
                    .WithPosition("3")
                    .WithSettings(new AutoroutePartSettings
                    {
                        Pattern = "{{ Model.ContentItem | display_text | slugify }}",
                        AllowRouteContainedItems = true
                    }))
                .WithPart("TaxonomyPart", part => part.WithPosition("4"))
            );

            SchemaBuilder.CreateMapIndexTable<TaxonomyIndex>(table => table
                .Column<string>("TaxonomyContentItemId", c => c.WithLength(26))
                .Column<string>("ContentItemId", c => c.WithLength(26))
                .Column<string>("ContentType", column => column.WithLength(ContentItemIndex.MaxContentTypeSize))
                .Column<string>("ContentPart", column => column.WithLength(ContentItemIndex.MaxContentPartSize))
                .Column<string>("ContentField", column => column.WithLength(ContentItemIndex.MaxContentFieldSize))
                .Column<string>("TermContentItemId", column => column.WithLength(26))
                .Column<bool>("Published", c => c.WithDefault(true))
                .Column<bool>("Latest", c => c.WithDefault(false))
                .Column<DateTime>("CreatedUtc")
                .Column<int>("Order", c => c.WithDefault(0))
            );

            SchemaBuilder.AlterIndexTable<TaxonomyIndex>(table => table
                .CreateIndex("IDX_TaxonomyIndex_DocumentId",
                    "DocumentId",
                    "TaxonomyContentItemId",
                    "ContentItemId",
                    "TermContentItemId",
                    "CreatedUtc",
                    "Order",
                    "Published",
                    "Latest")
            );

            SchemaBuilder.AlterIndexTable<TaxonomyIndex>(table => table
                .CreateIndex("IDX_TaxonomyIndex_DocumentId_ContentType",
                    "DocumentId",
                    "ContentType",
                    "ContentPart",
                    "ContentField",
                    "CreatedUtc",
                    "Order",
                    "Published",
                    "Latest")
            );

            // Shortcut other migration steps on new content definition schemas.
            return 6;
        }

        // Migrate FieldSettings. This only needs to run on old content definition schemas.
        // This code can be removed in a later version.
        public int UpdateFrom1()
        {
            _contentDefinitionManager.MigrateFieldSettings<TaxonomyField, TaxonomyFieldSettings>();
            return 2;
        }

        // This code can be removed in a later version.
        public int UpdateFrom2()
        {
            _contentDefinitionManager.AlterTypeDefinition("Taxonomy", taxonomy => taxonomy
                .WithPart("AutoroutePart", part => part
                    .WithPosition("3")
                    .WithSettings(new AutoroutePartSettings
                    {
                        Pattern = "{{ Model.ContentItem | display_text | slugify }}",
                        AllowRouteContainedItems = true
                    }))
                .WithPart("TaxonomyPart", part => part.WithPosition("4"))
            );

            return 3;
        }

        // This code can be removed in a later version.
        public int UpdateFrom3()
        {
            // This step has been updated to also add these new columns.
            SchemaBuilder.AlterIndexTable<TaxonomyIndex>(table => table
                .AddColumn<bool>("Published", c => c.WithDefault(true))
            );

            SchemaBuilder.AlterIndexTable<TaxonomyIndex>(table => table
                .AddColumn<bool>("Latest", c => c.WithDefault(false))
            );

            // So that the new indexes can be fully created.
            SchemaBuilder.AlterIndexTable<TaxonomyIndex>(table => table
                .CreateIndex("IDX_TaxonomyIndex_DocumentId",
                    "DocumentId",
                    "TaxonomyContentItemId",
                    "ContentItemId",
                    "TermContentItemId",
                    "Published",
                    "Latest")
            );

            SchemaBuilder.AlterIndexTable<TaxonomyIndex>(table => table
                .CreateIndex("IDX_TaxonomyIndex_DocumentId_ContentType",
                    "DocumentId",
                    "ContentType",
                    "ContentPart",
                    "ContentField",
                    "Published",
                    "Latest")
            );

            // We then shortcut the next migration step.
            return 5;
        }

        // This code can be removed in a later version.
        public int UpdateFrom4()
        {
            // This step run only if the previous one was executed before
            // it was updated, so here we also add the following columns.
            SchemaBuilder.AlterIndexTable<TaxonomyIndex>(table => table
                .AddColumn<bool>("Published", c => c.WithDefault(true))
            );

            SchemaBuilder.AlterIndexTable<TaxonomyIndex>(table => table
                .AddColumn<bool>("Latest", c => c.WithDefault(false))
            );

            // But we create a separate index for these new columns.
            SchemaBuilder.AlterIndexTable<TaxonomyIndex>(table => table
                .CreateIndex("IDX_TaxonomyIndex_DocumentId_Published",
                    "DocumentId",
                    "Published",
                    "Latest")
            );

            return 5;
        }

        public async Task<int> UpdateFrom5()
        {
            SchemaBuilder.AlterTable(nameof(TaxonomyIndex), table => table
                .AddColumn<DateTime>("CreatedUtc")
            );

            SchemaBuilder.AlterTable(nameof(TaxonomyIndex), table => table
                .AddColumn<int>("Order", column => column.WithDefault(0))
            );

            SchemaBuilder.AlterIndexTable<TaxonomyIndex>(table => table
                .DropIndex("IDX_TaxonomyIndex_DocumentId")
            );
            SchemaBuilder.AlterIndexTable<TaxonomyIndex>(table => table
                .CreateIndex("IDX_TaxonomyIndex_DocumentId",
                    "DocumentId",
                    "TaxonomyContentItemId",
                    "ContentItemId",
                    "TermContentItemId",
                    "CreatedUtc",
                    "Order",
                    "Published",
                    "Latest")
            );

            SchemaBuilder.AlterIndexTable<TaxonomyIndex>(table => table
                .DropIndex("IDX_TaxonomyIndex_DocumentId_ContentType")
            );
            SchemaBuilder.AlterIndexTable<TaxonomyIndex>(table => table
                .CreateIndex("IDX_TaxonomyIndex_DocumentId_ContentType",
                    "DocumentId",
                    "ContentType",
                    "ContentPart",
                    "ContentField",
                    "CreatedUtc",
                    "Order",
                    "Published",
                    "Latest")
            );

            // Save all content item versions, to update the new CreatedUtc entry in the TaxonomyIndex
            // This step doesn't need to be executed for a brand new site
            var lastDocumentId = 0;

            for (; ; )
            {
                var contentItemVersions = await _session.Query<ContentItem, ContentItemIndex>(x => x.DocumentId > lastDocumentId).Take(10).ListAsync();

                if (!contentItemVersions.Any())
                {
                    // No more content item versions to process
                    break;
                }

                foreach (var contentItemVersion in contentItemVersions)
                {
                    _session.Save(contentItemVersion);
                    lastDocumentId = contentItemVersion.Id;
                }

                await _session.SaveChangesAsync();
            }

            return 6;
        }
    }

    internal class AliasPartSettings
    {
        public string Pattern { get; set; }
    }

    internal class AutoroutePartSettings
    {
        public string Pattern { get; set; }
        public bool AllowRouteContainedItems { get; set; }
    }
}
