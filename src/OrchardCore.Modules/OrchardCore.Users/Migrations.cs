using OrchardCore.Data.Migration;
using OrchardCore.Users.Indexes;
using YesSql.Sql;

namespace OrchardCore.Users
{
    public class Migrations : DataMigration
    {
        public int Create()
        {
            SchemaBuilder.CreateMapIndexTable<UserIndex>(table => table
                .Column<string>("NormalizedUserName")
                .Column<string>("NormalizedEmail")
            );

            SchemaBuilder.CreateReduceIndexTable<UserByRoleNameIndex>(table => table
               .Column<string>("RoleName")
               .Column<int>("Count")
            );

            return UpdateFrom1();
        }

        public int UpdateFrom1()
        {
            SchemaBuilder.CreateMapIndexTable<UserByLoginInfoIndex>(table => table
                .Column<string>("LoginProvider")
                .Column<string>("ProviderKey"));
            return 2;
        }

        public int UpdateFrom2()
        {
            SchemaBuilder.CreateMapIndexTable<UserByClaimIndex>(table => table
               .Column<string>(nameof(UserByClaimIndex.ClaimType))
               .Column<string>(nameof(UserByClaimIndex.ClaimValue)),
                null);
            return 3;
        }

        public int UpdateFrom3()
        {
            SchemaBuilder.AlterTable(nameof(UserIndex), table => table
                .AddColumn<bool>(nameof(UserIndex.IsEnabled), c => c.NotNull().WithDefault(true)));

            SchemaBuilder.AlterTable(nameof(UserIndex), table => table
                .CreateIndex($"IDX_{nameof(UserIndex)}_{nameof(UserIndex.IsEnabled)}", nameof(UserIndex.IsEnabled))
            );

            return 4;
        }
        public int UpdateFrom4()
        {
            SchemaBuilder.AlterTable(nameof(UserIndex), table =>
            {
                table.CreateIndex($"IDX_{nameof(UserIndex)}_{nameof(UserIndex.NormalizedUserName)}", nameof(UserIndex.NormalizedUserName));
                table.CreateIndex($"IDX_{nameof(UserIndex)}_{nameof(UserIndex.NormalizedEmail)}", nameof(UserIndex.NormalizedEmail));
            });
            SchemaBuilder.AlterTable(nameof(UserByRoleNameIndex), table =>
            {
                table.CreateIndex($"IDX_{nameof(UserByRoleNameIndex)}_{nameof(UserByRoleNameIndex.RoleName)}", nameof(UserByRoleNameIndex.RoleName));
            });
            SchemaBuilder.AlterTable(nameof(UserByLoginInfoIndex), table =>
            {
                table.CreateIndex($"IDX_{nameof(UserByLoginInfoIndex)}_{nameof(UserByLoginInfoIndex.LoginProvider)}_{nameof(UserByLoginInfoIndex.ProviderKey)}",
                    new[] { nameof(UserByLoginInfoIndex.LoginProvider), nameof(UserByLoginInfoIndex.ProviderKey) });
            });
            SchemaBuilder.AlterTable(nameof(UserByClaimIndex), table =>
            {
                table.CreateIndex($"IDX_{nameof(UserByClaimIndex)}_{nameof(UserByClaimIndex.ClaimType)}_{nameof(UserByClaimIndex.ClaimValue)}",
                    new[] { nameof(UserByClaimIndex.ClaimType), nameof(UserByClaimIndex.ClaimValue) });
            });
            return 5;
        }
    }
}
