using System.Collections.Generic;
using GraphQL.Types;

namespace OrchardCore.ContentManagement.GraphQL
{
    public static class FieldTypeExtensions
    {
        public static FieldType WithPartCollapsedMetaData(this FieldType fieldType, bool collapsed = true)
        {
            return fieldType.WithMetaData("PartCollapsed", collapsed);
        }

        public static FieldType WithPartNameMetaData(this FieldType fieldType, string partName)
        {
            return fieldType.WithMetaData("PartName", partName);
        }

        internal static FieldType WithMetaData(this FieldType fieldType, string name, object value)
        {
            if (fieldType == null)
            {
                return null;
            }

            if (!fieldType.HasMetadata(name))
            {
                fieldType.Metadata.Add(name, value);
            }

            return fieldType;
        }
    }
}
