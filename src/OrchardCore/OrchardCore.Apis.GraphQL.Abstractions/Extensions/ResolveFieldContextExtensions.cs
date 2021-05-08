using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL;
using GraphQL.Builders;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace OrchardCore.Apis.GraphQL
{
    public static class ResolveFieldContextExtensions
    {
        public static bool HasPopulatedArgument(this IResolveFieldContext source, string argumentName)
        {
            if (source.Arguments?.ContainsKey(argumentName) ?? false)
            {
                return !string.IsNullOrEmpty((source.Arguments[argumentName].Value ?? string.Empty).ToString());
            };

            return false;
        }

        public static bool HasPopulatedArgument<TSource>(this IResolveFieldContext<TSource> source, string argumentName)
        {
            if (source.Arguments?.ContainsKey(argumentName) ?? false)
            {
                return !string.IsNullOrEmpty((source.Arguments[argumentName].Value ?? string.Empty).ToString());
            };

            return false;
        }

        public static FieldBuilder<TArgumentGraphType, TArgumentType> PagingArguments<TArgumentGraphType, TArgumentType>(this FieldBuilder<TArgumentGraphType, TArgumentType> field)
        {
            return field
                .Argument<IntGraphType, int>("first", "the first n elements", 0)
                .Argument<IntGraphType, int>("last", "the last n elements", 0)
                .Argument<IntGraphType, int>("skip", "the number of elements to skip", 0);
        }

        public static IEnumerable<TSource> Page<T, TSource>(this IResolveFieldContext<T> context, IEnumerable<TSource> source)
        {
            var skip = context.GetArgument<int>("skip");
            var first = context.GetArgument<int>("first");
            var last = context.GetArgument<int>("last");

            if (last == 0 && first == 0)
            {
                first = context.RequestServices.GetService<IOptions<GraphQLSettings>>().Value.DefaultNumberOfResults;
            }

            if (last > 0)
            {
                source = source.Skip(Math.Max(0, source.Count() - last));
            }
            else
            {
                if (skip > 0)
                {
                    source = source.Skip(skip);
                }

                if (first > 0)
                {
                    source = source.Take(first);
                }
            }

            return source;
        }
    }
}
