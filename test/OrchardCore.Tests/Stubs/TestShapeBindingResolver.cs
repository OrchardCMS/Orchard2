using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Descriptors;

namespace OrchardCore.Tests.Stubs
{
    public class TestShapeBindingsDictionary : Dictionary<string, ShapeBinding>
    {
        public bool IsAdminShape { get; set; } = false;
        public TestShapeBindingsDictionary()
            : base(StringComparer.OrdinalIgnoreCase) { }
    }

    public class TestShapeBindingResolver : IShapeBindingResolver, IAdminTemplatesShapeBindingNameResolver, ISiteTemplatesShapeBindingNameResolver
    {
        private readonly TestShapeBindingsDictionary _shapeBindings;

        public TestShapeBindingResolver(TestShapeBindingsDictionary shapeBindings)
        {
            _shapeBindings = shapeBindings;
        }

        public Task<ShapeBinding> GetShapeBindingAsync(string shapeType)
        {
            if (_shapeBindings.TryGetValue(shapeType, out var binding))
            {
                return Task.FromResult(binding);
            }
            else
            {
                return Task.FromResult<ShapeBinding>(null);
            }
        }

        Task<IEnumerable<string>> ISiteTemplatesShapeBindingNameResolver.GetShapeBindingNamesAsync(Func<string, bool> predicate)
        {
            return GetShapeBindingNamesAsync(predicate,false);
        }

        Task<IEnumerable<string>> IAdminTemplatesShapeBindingNameResolver.GetShapeBindingNamesAsync(Func<string, bool> predicate)
        {
            return GetShapeBindingNamesAsync(predicate,true);
        }

        private async Task<IEnumerable<string>> GetShapeBindingNamesAsync(Func<string, bool> predicate, bool adminTemplate)
        {
            if (adminTemplate != _shapeBindings.IsAdminShape)
            {
                return null;
            }

            var shapeNames = new List<string>();
            foreach (var key in _shapeBindings.Keys)
            {
                if (predicate(key))
                {
                    shapeNames.Add(key);
                }
            }

            return await Task.FromResult(shapeNames);
        }
    }
}
