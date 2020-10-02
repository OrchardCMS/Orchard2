using System;
using System.Data;
using OrchardCore.Data.Migration;
using OrchardCore.OpenId.YesSql.Indexes;
using YesSql.Sql;

namespace OrchardCore.OpenId.YesSql.Migrations
{
    public class OpenIdMigrations : DataMigration
    {
        public int Create()
        {
            SchemaBuilder.CreateMapIndexTable<OpenIdApplicationIndex>(table => table
                .Column<string>(nameof(OpenIdApplicationIndex.ApplicationId), column => column.WithLength(48))
                .Column<string>(nameof(OpenIdApplicationIndex.ClientId), column => column.Unique()));

            SchemaBuilder.CreateReduceIndexTable<OpenIdAppByLogoutUriIndex>(table => table
                .Column<string>(nameof(OpenIdAppByLogoutUriIndex.LogoutRedirectUri))
                .Column<int>(nameof(OpenIdAppByLogoutUriIndex.Count)));

            SchemaBuilder.CreateReduceIndexTable<OpenIdAppByRedirectUriIndex>(table => table
                .Column<string>(nameof(OpenIdAppByRedirectUriIndex.RedirectUri))
                .Column<int>(nameof(OpenIdAppByRedirectUriIndex.Count)));

            SchemaBuilder.CreateReduceIndexTable<OpenIdAppByRoleNameIndex>(table => table
                .Column<string>(nameof(OpenIdAppByRoleNameIndex.RoleName))
                .Column<int>(nameof(OpenIdAppByRoleNameIndex.Count)));

            SchemaBuilder.CreateMapIndexTable<OpenIdAuthorizationIndex>(table => table
                .Column<string>(nameof(OpenIdAuthorizationIndex.AuthorizationId), column => column.WithLength(48))
                .Column<string>(nameof(OpenIdAuthorizationIndex.ApplicationId), column => column.WithLength(48))
                .Column<string>(nameof(OpenIdAuthorizationIndex.Status))
                .Column<string>(nameof(OpenIdAuthorizationIndex.Subject))
                .Column<string>(nameof(OpenIdAuthorizationIndex.Type)));

            SchemaBuilder.CreateMapIndexTable<OpenIdScopeIndex>(table => table
                .Column<string>(nameof(OpenIdScopeIndex.Name), column => column.Unique())
                .Column<string>(nameof(OpenIdScopeIndex.ScopeId), column => column.WithLength(48)));

            SchemaBuilder.CreateReduceIndexTable<OpenIdScopeByResourceIndex>(table => table
                .Column<string>(nameof(OpenIdScopeByResourceIndex.Resource))
                .Column<int>(nameof(OpenIdScopeByResourceIndex.Count)));

            SchemaBuilder.CreateMapIndexTable<OpenIdTokenIndex>(table => table
                .Column<string>(nameof(OpenIdTokenIndex.TokenId), column => column.WithLength(48))
                .Column<string>(nameof(OpenIdTokenIndex.ApplicationId), column => column.WithLength(48))
                .Column<string>(nameof(OpenIdTokenIndex.AuthorizationId), column => column.WithLength(48))
                .Column<DateTimeOffset>(nameof(OpenIdTokenIndex.ExpirationDate))
                .Column<string>(nameof(OpenIdTokenIndex.ReferenceId))
                .Column<string>(nameof(OpenIdTokenIndex.Status))
                .Column<string>(nameof(OpenIdTokenIndex.Subject))
                .Column<string>(nameof(OpenIdTokenIndex.Type)));

            return 3;
        }

        public int UpdateFrom1()
        {
            SchemaBuilder.AlterTable(nameof(OpenIdTokenIndex), table => table
                .AddColumn<string>(nameof(OpenIdTokenIndex.Type)));

            return 2;
        }

        private class OpenIdApplicationByPostLogoutRedirectUriIndex { }
        private class OpenIdApplicationByRedirectUriIndex { }
        private class OpenIdApplicationByRoleNameIndex { }

        public int UpdateFrom2()
        {
            SchemaBuilder.DropReduceIndexTable<OpenIdApplicationByPostLogoutRedirectUriIndex>(null);
            SchemaBuilder.DropReduceIndexTable<OpenIdApplicationByRedirectUriIndex>(null);
            SchemaBuilder.DropReduceIndexTable<OpenIdApplicationByRoleNameIndex>(null);

            SchemaBuilder.CreateReduceIndexTable<OpenIdAppByLogoutUriIndex>(table => table
                .Column<string>(nameof(OpenIdAppByLogoutUriIndex.LogoutRedirectUri))
                .Column<int>(nameof(OpenIdAppByLogoutUriIndex.Count)));

            SchemaBuilder.CreateReduceIndexTable<OpenIdAppByRedirectUriIndex>(table => table
                .Column<string>(nameof(OpenIdAppByRedirectUriIndex.RedirectUri))
                .Column<int>(nameof(OpenIdAppByRedirectUriIndex.Count)));

            SchemaBuilder.CreateReduceIndexTable<OpenIdAppByRoleNameIndex>(table => table
                .Column<string>(nameof(OpenIdAppByRoleNameIndex.RoleName))
                .Column<int>(nameof(OpenIdAppByRoleNameIndex.Count)));

            return 3;
        }

        public int UpdateFrom3()
        {
            SchemaBuilder.AlterTable(nameof(OpenIdApplicationIndex), table =>
            {
                table.AlterColumn(nameof(OpenIdApplicationIndex.ClientId), column => column.WithType(DbType.String, 100));
                table.CreateIndex($"IX_{nameof(OpenIdApplicationIndex)}_{nameof(OpenIdApplicationIndex.ApplicationId)}", nameof(OpenIdApplicationIndex.ApplicationId));
                table.CreateIndex($"IX_{nameof(OpenIdApplicationIndex)}_{nameof(OpenIdApplicationIndex.ClientId)}", nameof(OpenIdApplicationIndex.ClientId));
            });
            SchemaBuilder.AlterTable(nameof(OpenIdAppByLogoutUriIndex), table =>
            {
                table.CreateIndex($"IX_{nameof(OpenIdAppByLogoutUriIndex)}_{nameof(OpenIdAppByLogoutUriIndex.LogoutRedirectUri)}", nameof(OpenIdAppByLogoutUriIndex.LogoutRedirectUri));
            });
            SchemaBuilder.AlterTable(nameof(OpenIdAppByRedirectUriIndex), table =>
            {
                table.CreateIndex($"IX_{nameof(OpenIdAppByRedirectUriIndex)}_{nameof(OpenIdAppByRedirectUriIndex.RedirectUri)}", nameof(OpenIdAppByRedirectUriIndex.RedirectUri));
            });
            SchemaBuilder.AlterTable(nameof(OpenIdAppByRoleNameIndex), table =>
            {
                table.CreateIndex($"IX_{nameof(OpenIdAppByRoleNameIndex)}_{nameof(OpenIdAppByRoleNameIndex.RoleName)}", nameof(OpenIdAppByRoleNameIndex.RoleName));
            });

            SchemaBuilder.AlterTable(nameof(OpenIdAuthorizationIndex), table =>
            {
                table.AlterColumn(nameof(OpenIdAuthorizationIndex.Status), column => column.WithType(DbType.String, 25));
                table.AlterColumn(nameof(OpenIdAuthorizationIndex.Subject), column => column.WithType(DbType.String, 330));
                table.AlterColumn(nameof(OpenIdAuthorizationIndex.Type), column => column.WithType(DbType.String, 25));

                table.CreateIndex($"IX_{nameof(OpenIdAuthorizationIndex)}_{nameof(OpenIdAuthorizationIndex.Subject)}", nameof(OpenIdAuthorizationIndex.Subject));
                table.CreateIndex($"IX_{nameof(OpenIdAuthorizationIndex)}_{nameof(OpenIdAuthorizationIndex.AuthorizationId)}", nameof(OpenIdAuthorizationIndex.AuthorizationId));
                table.CreateIndex($"IX_{nameof(OpenIdAuthorizationIndex)}_{nameof(OpenIdAuthorizationIndex.ApplicationId)}", nameof(OpenIdAuthorizationIndex.ApplicationId));
                table.CreateIndex($"IX_{nameof(OpenIdAuthorizationIndex)}_{nameof(OpenIdAuthorizationIndex.ApplicationId)}_{nameof(OpenIdAuthorizationIndex.Subject)}",
                    new[] { nameof(OpenIdAuthorizationIndex.ApplicationId), nameof(OpenIdAuthorizationIndex.Subject) });
                table.CreateIndex($"IX_{nameof(OpenIdAuthorizationIndex)}_{nameof(OpenIdAuthorizationIndex.ApplicationId)}_{nameof(OpenIdAuthorizationIndex.Subject)}_{nameof(OpenIdAuthorizationIndex.Status)}",
                    new[] { nameof(OpenIdAuthorizationIndex.ApplicationId), nameof(OpenIdAuthorizationIndex.Subject), nameof(OpenIdAuthorizationIndex.Status) });
                table.CreateIndex($"IX_{nameof(OpenIdAuthorizationIndex)}_{nameof(OpenIdAuthorizationIndex.Status)}_{nameof(OpenIdAuthorizationIndex.Type)}_{nameof(OpenIdAuthorizationIndex.AuthorizationId)}",
                    new[] { nameof(OpenIdAuthorizationIndex.Status), nameof(OpenIdAuthorizationIndex.Type), nameof(OpenIdAuthorizationIndex.AuthorizationId) });
                table.CreateIndex($"IX_{nameof(OpenIdAuthorizationIndex)}_{nameof(OpenIdAuthorizationIndex.ApplicationId)}_{nameof(OpenIdAuthorizationIndex.Subject)}_{nameof(OpenIdAuthorizationIndex.Status)}_{nameof(OpenIdAuthorizationIndex.Type)}",
                    new[] { nameof(OpenIdAuthorizationIndex.ApplicationId), nameof(OpenIdAuthorizationIndex.Subject), nameof(OpenIdAuthorizationIndex.Status), nameof(OpenIdAuthorizationIndex.Type) });
            });

            SchemaBuilder.AlterTable(nameof(OpenIdScopeIndex), table =>
            {
                table.AlterColumn(nameof(OpenIdScopeIndex.Name), column => column.WithType(DbType.String, 200));
                table.CreateIndex($"IX_{nameof(OpenIdScopeIndex)}_{nameof(OpenIdScopeIndex.ScopeId)}", nameof(OpenIdScopeIndex.ScopeId));
                table.CreateIndex($"IX_{nameof(OpenIdScopeIndex)}_{nameof(OpenIdScopeIndex.Name)}", nameof(OpenIdScopeIndex.Name));
            });
            SchemaBuilder.AlterTable(nameof(OpenIdScopeByResourceIndex), table =>
            {
                table.CreateIndex($"IX_{nameof(OpenIdScopeByResourceIndex)}_{nameof(OpenIdScopeByResourceIndex.Resource)}", nameof(OpenIdScopeByResourceIndex.Resource));
            });
            SchemaBuilder.AlterTable(nameof(OpenIdTokenIndex), table =>
            {
                table.AlterColumn(nameof(OpenIdTokenIndex.Status), column => column.WithType(DbType.String, 25));
                table.AlterColumn(nameof(OpenIdTokenIndex.Subject), column => column.WithType(DbType.String, 330));
                table.AlterColumn(nameof(OpenIdTokenIndex.Type), column => column.WithType(DbType.String, 25));

                table.CreateIndex($"IX_{nameof(OpenIdTokenIndex)}_{nameof(OpenIdTokenIndex.ApplicationId)}", nameof(OpenIdTokenIndex.ApplicationId));
                table.CreateIndex($"IX_{nameof(OpenIdTokenIndex)}_{nameof(OpenIdTokenIndex.AuthorizationId)}", nameof(OpenIdTokenIndex.AuthorizationId));
                table.CreateIndex($"IX_{nameof(OpenIdTokenIndex)}_{nameof(OpenIdTokenIndex.ReferenceId)}", nameof(OpenIdTokenIndex.ReferenceId));
                table.CreateIndex($"IX_{nameof(OpenIdTokenIndex)}_{nameof(OpenIdTokenIndex.TokenId)}", nameof(OpenIdTokenIndex.TokenId));
                table.CreateIndex($"IX_{nameof(OpenIdTokenIndex)}_{nameof(OpenIdTokenIndex.Subject)}", nameof(OpenIdTokenIndex.Subject));
                table.CreateIndex($"IX_{nameof(OpenIdTokenIndex)}_{nameof(OpenIdTokenIndex.ApplicationId)}_{nameof(OpenIdTokenIndex.Subject)}",
                    new[] { nameof(OpenIdTokenIndex.ApplicationId), nameof(OpenIdTokenIndex.Subject) });
                table.CreateIndex($"IX_{nameof(OpenIdTokenIndex)}_{nameof(OpenIdTokenIndex.ApplicationId)}_{nameof(OpenIdTokenIndex.Subject)}_{nameof(OpenIdTokenIndex.Status)}",
                    new[] { nameof(OpenIdTokenIndex.ApplicationId), nameof(OpenIdTokenIndex.Subject), nameof(OpenIdTokenIndex.Status) });
                table.CreateIndex($"IX_{nameof(OpenIdTokenIndex)}_{nameof(OpenIdTokenIndex.Status)}_{nameof(OpenIdTokenIndex.ExpirationDate)}",
                    new[] { nameof(OpenIdTokenIndex.Status), nameof(OpenIdTokenIndex.ExpirationDate) });
                table.CreateIndex($"IX_{nameof(OpenIdTokenIndex)}_{nameof(OpenIdTokenIndex.ApplicationId)}_{nameof(OpenIdTokenIndex.Subject)}_{nameof(OpenIdTokenIndex.Status)}_{nameof(OpenIdTokenIndex.Type)}",
                new[] { nameof(OpenIdTokenIndex.ApplicationId), nameof(OpenIdTokenIndex.Subject), nameof(OpenIdTokenIndex.Status), nameof(OpenIdTokenIndex.Type) });
            });
            return 4;
        }
    }
}
