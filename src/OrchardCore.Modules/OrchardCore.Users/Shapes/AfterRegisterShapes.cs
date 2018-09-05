using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Descriptors;
using OrchardCore.DisplayManagement.Shapes;

namespace OrchardCore.Users.Shapes
{
    public class AfterRegisterShapes : IShapeTableProvider, IShapeAttributeProvider
    {

        public void Discover(ShapeTableBuilder builder)
        {
            builder
                .Describe("AfterRegister");
        }

        [Shape]
        public async Task<IHtmlContent> AfterRegister(Shape shape, dynamic displayAsync)
        {
            var tag = Shape.GetTagBuilder(shape);
            foreach (var item in shape.Items)
            {
                tag.InnerHtml.AppendHtml(await displayAsync(item));
            }
            return tag;
        }
    }
}