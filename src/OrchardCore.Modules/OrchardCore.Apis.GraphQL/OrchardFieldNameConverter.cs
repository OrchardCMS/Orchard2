using System;
using GraphQL.Conversion;
using GraphQL.Types;

namespace OrchardCore.Apis.GraphQL
{
    public class OrchardFieldNameConverter : INameConverter
    {
        private readonly INameConverter _defaultConverter = new CamelCaseNameConverter();

        public string NameForArgument(string fieldName, IComplexGraphType parentGraphType, FieldType field)
        {
            return _defaultConverter.NameForArgument(fieldName, parentGraphType, field);
        }

        public string NameForField(string fieldName, IComplexGraphType parentGraphType)
        {
            return _defaultConverter.NameForField(fieldName, parentGraphType);
        }
    }
}
