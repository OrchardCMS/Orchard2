using System;
using OrchardCore.Data.Migration;
using OrchardCore.Workflows.Indexes;
using YesSql.Sql;

namespace OrchardCore.Workflows
{
    public class Migrations : DataMigration
    {
        public int Create()
        {
            SchemaBuilder.CreateMapIndexTable<WorkflowTypeIndex>(table => table
                .Column<string>("WorkflowTypeId")
                .Column<string>("Name")
                .Column<bool>("IsEnabled")
                .Column<bool>("HasStart")
            );

            SchemaBuilder.AlterIndexTable<WorkflowTypeIndex>(table => table
                .CreateIndex("IDX_WorkflowTypeIndex_DocumentId",
                    "DocumentId",
                    "WorkflowTypeId",
                    "Name",
                    "IsEnabled",
                    "HasStart")
            );

            SchemaBuilder.CreateMapIndexTable<WorkflowTypeStartActivitiesIndex>(table => table
                .Column<string>("WorkflowTypeId")
                .Column<string>("Name")
                .Column<bool>("IsEnabled")
                .Column<string>("StartActivityId")
                .Column<string>("StartActivityName")
            );

            SchemaBuilder.AlterIndexTable<WorkflowTypeStartActivitiesIndex>(table => table
                .CreateIndex("IDX_WorkflowTypeStartActivitiesIndex_DocumentId",
                    "DocumentId",
                    "WorkflowTypeId",
                    "StartActivityId",
                    "StartActivityName",
                    "IsEnabled")
            );

            SchemaBuilder.CreateMapIndexTable<WorkflowIndex>(table => table
                .Column<string>("WorkflowTypeId")
                .Column<string>("WorkflowId")
                .Column<string>("WorkflowStatus")
                .Column<DateTime>("CreatedUtc")
            );

            SchemaBuilder.AlterIndexTable<WorkflowIndex>(table => table
                .CreateIndex("IDX_WorkflowIndex_DocumentId",
                    "DocumentId",
                    "WorkflowTypeId",
                    "WorkflowId",
                    "WorkflowStatus",
                    "CreatedUtc")
            );

            SchemaBuilder.CreateMapIndexTable<WorkflowBlockingActivitiesIndex>(table => table
                .Column<string>("ActivityId")
                .Column<string>("ActivityName")
                .Column<bool>("ActivityIsStart")
                .Column<string>("WorkflowTypeId")
                .Column<string>("WorkflowId")
                .Column<string>("WorkflowCorrelationId")
            );

            SchemaBuilder.AlterIndexTable<WorkflowBlockingActivitiesIndex>(table => table
                .CreateIndex("IDX_WFBlockingActivities_DocumentId_ActivityId",
                    "DocumentId",
                    "ActivityId",
                    "WorkflowTypeId",
                    "WorkflowId")
            );

            SchemaBuilder.AlterIndexTable<WorkflowBlockingActivitiesIndex>(table => table
                .CreateIndex("IDX_WFBlockingActivities_DocumentId_ActivityName",
                    "DocumentId",
                    "ActivityName",
                    "WorkflowTypeId",
                    "WorkflowCorrelationId")
            );

            // Shortcut other migration steps on new content definition schemas.
            return 3;
        }

        // This code can be removed in a later version.
        public int UpdateFrom1()
        {
            SchemaBuilder.AlterIndexTable<WorkflowIndex>(table =>
            {
                table.AddColumn<string>("WorkflowStatus");
            });

            return 2;
        }

        // This code can be removed in a later version.
        public int UpdateFrom2()
        {
            SchemaBuilder.AlterIndexTable<WorkflowTypeIndex>(table => table
                .CreateIndex("IDX_WorkflowTypeIndex_DocumentId",
                    "DocumentId",
                    "WorkflowTypeId",
                    "Name",
                    "IsEnabled",
                    "HasStart")
            );

            SchemaBuilder.AlterIndexTable<WorkflowTypeStartActivitiesIndex>(table => table
                .CreateIndex("IDX_WorkflowTypeStartActivitiesIndex_DocumentId",
                    "DocumentId",
                    "WorkflowTypeId",
                    "StartActivityId",
                    "StartActivityName",
                    "IsEnabled")
            );

            SchemaBuilder.AlterIndexTable<WorkflowIndex>(table => table
                .CreateIndex("IDX_WorkflowIndex_DocumentId",
                    "DocumentId",
                    "WorkflowTypeId",
                    "WorkflowId",
                    "WorkflowStatus",
                    "CreatedUtc")
            );

            SchemaBuilder.AlterIndexTable<WorkflowBlockingActivitiesIndex>(table => table
                .CreateIndex("IDX_WFBlockingActivities_DocumentId_ActivityId",
                    "DocumentId",
                    "ActivityId",
                    "WorkflowTypeId",
                    "WorkflowId")
            );

            SchemaBuilder.AlterIndexTable<WorkflowBlockingActivitiesIndex>(table => table
                .CreateIndex("IDX_WFBlockingActivities_DocumentId_ActivityName",
                    "DocumentId",
                    "ActivityName",
                    "WorkflowTypeId",
                    "WorkflowCorrelationId")
            );

            return 3;
        }

        public void Uninstall()
        {
            SchemaBuilder.DropMapIndexTable<WorkflowBlockingActivitiesIndex>();
            SchemaBuilder.DropMapIndexTable<WorkflowIndex>();
            SchemaBuilder.DropMapIndexTable<WorkflowTypeIndex>();
            SchemaBuilder.DropMapIndexTable<WorkflowTypeStartActivitiesIndex>();
        }
    }
}
