using System.Linq;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace OrchardCore.Apis.GraphQL.ValidationRules
{
    public class RequiresPermissionValidationRule : IValidationRule
    {
        public static readonly string ErrorCode = "Unauthorized";

        public Task<INodeVisitor> ValidateAsync(ValidationContext validationContext)
        {
            var context = (GraphQLUserContext)validationContext.UserContext;

            // Todo: EnterLeaveListener has been removed and the signatures of INodeVisitor.Enter and INodeVisitor.Leave have changed. NodeVisitors class has been added in its place.
            // https://graphql-dotnet.github.io/docs/migrations/migration4/
            // Ex: https://github.com/graphql-dotnet/graphql-dotnet/issues/2406
            INodeVisitor result = new NodeVisitors();
            return Task.FromResult(result);

            //return new EnterLeaveListener(_ =>
            //{
            //    _.Match<Operation>(op =>
            //    {
            //        if (op.OperationType == OperationType.Mutation)
            //        {
            //            var authorizationManager = context.ServiceProvider.GetService<IAuthorizationService>();

            //            if (!authorizationManager.AuthorizeAsync(context.User, Permissions.ExecuteGraphQLMutations).GetAwaiter().GetResult())
            //            {
            //                var localizer = context.ServiceProvider.GetService<IStringLocalizer<RequiresPermissionValidationRule>>();

            //                validationContext.ReportError(new ValidationError(
            //                    validationContext.OriginalQuery,
            //                    ErrorCode,
            //                    localizer["Authorization is required to access {0}.", op.Name],
            //                    op));
            //            }
            //        }
            //    });

            //    _.Match<Field>(fieldAst =>
            //    {
            //        var fieldDef = validationContext.TypeInfo.GetFieldDef();

            //        if (fieldDef.HasPermissions() && !Authorize(fieldDef, context))
            //        {
            //            var localizer = context.ServiceProvider.GetService<IStringLocalizer<RequiresPermissionValidationRule>>();

            //            validationContext.ReportError(new ValidationError(
            //                validationContext.OriginalQuery,
            //                ErrorCode,
            //                localizer["Authorization is required to access the field. {0}", fieldAst.Name],
            //                fieldAst));
            //        }
            //    });
            //});
        }

        //private static bool Authorize(IProvideMetadata type, GraphQLUserContext context)
        //{
        //    //var authorizationManager = context.ServiceProvider.GetService<IAuthorizationService>();

        //    //// awaitable IValidationRule in graphql dotnet is coming soon:
        //    //// https://github.com/graphql-dotnet/graphql-dotnet/issues/1140
        //    //return type.GetPermissions().All(x => authorizationManager.AuthorizeAsync(context.User, x.Permission, x.Resource).GetAwaiter().GetResult());


        //}
    }
}
