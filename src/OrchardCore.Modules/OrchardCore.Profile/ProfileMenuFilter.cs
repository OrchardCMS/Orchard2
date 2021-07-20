using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Layout;
using OrchardCore.Profile.Navigation;

namespace OrchardCore.Profile
{
    public class ProfileMenuFilter : IAsyncResultFilter
    {
        private readonly IProfileNavigationManager _navigationManager;
        private readonly ILayoutAccessor _layoutAccessor;
        private readonly IShapeFactory _shapeFactory;

        public ProfileMenuFilter(IProfileNavigationManager navigationManager,
            ILayoutAccessor layoutAccessor,
            IShapeFactory shapeFactory)
        {

            _navigationManager = navigationManager;
            _layoutAccessor = layoutAccessor;
            _shapeFactory = shapeFactory;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext filterContext, ResultExecutionDelegate next)
        {
            // Should only run on a full view rendering result
            if (!(filterContext.Result is ViewResult))
            {
                await next();
                return;
            }

            // Don't create the menu if the status code is 3xx
            var statusCode = filterContext.HttpContext.Response.StatusCode;
            if (statusCode >= 300 && statusCode < 400)
            {
                await next();
                return;
            }

            // Populate main nav
            IShape menuShape = await _shapeFactory.CreateAsync("ProfileNavigation",
                Arguments.From(new
                {
                    MenuName = "profile",
                    RouteData = filterContext.RouteData,
                }));

            dynamic layout = await _layoutAccessor.GetLayoutAsync();
            //layout.ProfileNavigation.Add(menuShape);

            var navigation = layout.Zones["ProfileNavigation"];

            if (navigation is Shape shape)
            {
                await shape.AddAsync(menuShape);
            }

            await next();
        }
    }
}
