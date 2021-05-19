using OrchardCore.Data.Migration;
using OrchardCore.Deployment.Indexes;
using YesSql.Sql;

namespace OrchardCore.Deployment
{
    public class Migrations : DataMigration
    {
        public int Create()
        {
            SchemaBuilder.CreateMapIndexTable<DeploymentPlanIndex>(table => table
                .Column<string>("Name")
            );

            return 1;
        }

        public void Uninstall()
        {
            SchemaBuilder.DropMapIndexTable<DeploymentPlanIndex>();
        }
    }
}
