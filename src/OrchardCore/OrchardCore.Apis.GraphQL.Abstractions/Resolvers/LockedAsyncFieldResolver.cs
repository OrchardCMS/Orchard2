using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace OrchardCore.Apis.GraphQL.Resolvers
{
    public class LockedAsyncFieldResolver<TReturnType> : IFieldResolver<Task<TReturnType>>
    {
        private readonly Func<IResolveFieldContext, Task<TReturnType>> _resolver;

        public LockedAsyncFieldResolver(Func<IResolveFieldContext, Task<TReturnType>> resolver)
        {
            _resolver = resolver;
        }

        public async Task<TReturnType> Resolve(IResolveFieldContext context)
        {
            var graphContext = (GraphQLUserContext)context.UserContext;

            await graphContext.ExecutionContextLock.WaitAsync();

            try
            {
                return await _resolver(context);
            }
            finally
            {
                graphContext.ExecutionContextLock.Release();
            }
        }

        object IFieldResolver.Resolve(IResolveFieldContext context)
        {
            return Resolve(context);
        }
    }

    public class LockedAsyncFieldResolver<TSourceType, TReturnType> : AsyncFieldResolver<TSourceType, TReturnType>, IFieldResolver<Task<TReturnType>>
    {
        public LockedAsyncFieldResolver(Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolver) : base(resolver)
        {
        }

        public new async Task<TReturnType> Resolve(IResolveFieldContext context)
        {
            var graphContext = (GraphQLUserContext)context.UserContext;
            await graphContext.ExecutionContextLock.WaitAsync();

            try
            {
                return await base.Resolve(context);
            }
            finally
            {
                graphContext.ExecutionContextLock.Release();
            }
        }

        object IFieldResolver.Resolve(IResolveFieldContext context)
        {
            return Resolve(context);
        }
    }
}
