using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OrchardCore.Apis.GraphQL.ValidationRules
{
    public class MaxNumberOfResultsValidationRule : IValidationRule
    {
        private readonly int _maxNumberOfResults;
        private readonly MaxNumberOfResultsValidationMode _maxNumberOfResultsValidationMode;

        public MaxNumberOfResultsValidationRule(IOptions<GraphQLSettings> options)
        {
            var settings = options.Value;
            _maxNumberOfResults = settings.MaxNumberOfResults;
            _maxNumberOfResultsValidationMode = settings.MaxNumberOfResultsValidationMode;
        }

        public Task<INodeVisitor> ValidateAsync(ValidationContext validationContext)
        {
            // Todo: EnterLeaveListener has been removed and the signatures of INodeVisitor.Enter and INodeVisitor.Leave have changed. NodeVisitors class has been added in its place.
            // https://graphql-dotnet.github.io/docs/migrations/migration4/
            // Ex: https://github.com/graphql-dotnet/graphql-dotnet/issues/2406
            INodeVisitor result = new NodeVisitors();
            return Task.FromResult(result);

            //return new EnterLeaveListener(_ =>
            //{
            //    _.Match<Argument>(arg =>
            //    {
            //        if ((arg.Name == "first" || arg.Name == "last") && arg.Value != null)
            //        {
            //            var context = (GraphQLUserContext)validationContext.UserContext;

            //            int? value = null;

            //            if (arg.Value is IntValue)
            //            {
            //                value = ((IntValue)arg.Value)?.Value;
            //            }
            //            else
            //            {
            //                if (validationContext.Inputs.TryGetValue(arg.Value.ToString(), out var input))
            //                {
            //                    value = (int?)input;
            //                }
            //            }

            //            if (value.HasValue && value > _maxNumberOfResults)
            //            {
            //                var localizer = context.ServiceProvider.GetService<IStringLocalizer<MaxNumberOfResultsValidationRule>>();
            //                var errorMessage = localizer["'{0}' exceeds the maximum number of results for '{1}' ({2})", value.Value, arg.Name, _maxNumberOfResults];

            //                if (_maxNumberOfResultsValidationMode == MaxNumberOfResultsValidationMode.Enabled)
            //                {
            //                    validationContext.ReportError(new ValidationError(
            //                        validationContext.OriginalQuery,
            //                        "ArgumentInputError",
            //                        errorMessage,
            //                        arg));
            //                }
            //                else
            //                {
            //                    var logger = context.ServiceProvider.GetService<ILogger<MaxNumberOfResultsValidationMode>>();
            //                    logger.LogInformation(errorMessage);
            //                    arg.Value = new IntValue(_maxNumberOfResults); // if disabled mode we just log info and override the arg to be maxvalue
            //                }
            //            }
            //        }
            //    });
            //});
        }
    }
}
