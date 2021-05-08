using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Builders;
using GraphQL.Types;
using OrchardCore.Apis.GraphQL.Resolvers;

namespace OrchardCore.Apis.GraphQL
{
    public static class FieldBuilderResolverExtensions
    {
        public static FieldBuilder<TSourceType, TReturnType> ResolveLockedAsync<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> builder, Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolve)
        {
            return builder.Resolve(new LockedAsyncFieldResolver<TSourceType, TReturnType>(resolve));
        }
    }
}
