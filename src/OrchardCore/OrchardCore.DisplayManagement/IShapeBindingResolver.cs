using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrchardCore.DisplayManagement.Descriptors;

namespace OrchardCore.DisplayManagement
{
    /// <summary>
    /// An implementation of this interface is called whenever a shape template
    /// is seeked. it can be used to provide custom dynamic templates, for instance to override
    /// any view engine based ones.
    /// </summary>
    public interface IShapeBindingResolver
    {
        Task<ShapeBinding> GetShapeBindingAsync(string shapeType);
    }

    /// <summary>
    /// An implementation of this interface returns shape names of Admin Templates.
    /// </summary>
    public interface IAdminTemplatesShapeBindingNameResolver
    {
       Task<IEnumerable<string>> GetShapeBindingNamesAsync(Func<string, bool> predicate);
    }

    /// <summary>
    /// An implementation of this interface returns shape names for Site Templates.
    /// </summary>
    public interface ISiteTemplatesShapeBindingNameResolver
    {
       Task<IEnumerable<string>> GetShapeBindingNamesAsync(Func<string, bool> predicate);
    }
}
