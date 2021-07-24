using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Descriptors;
using OrchardCore.DisplayManagement.Implementation;
using OrchardCore.DisplayManagement.Shapes;
using OrchardCore.DisplayManagement.Theming;
using OrchardCore.Environment.Extensions;
using OrchardCore.Localization;
using OrchardCore.Tests.Stubs;
using Xunit;

namespace OrchardCore.Tests.DisplayManagement
{
    public class DefaultDisplayManagerTests
    {
        private ShapeTable _defaultShapeTable;
        private TestShapeBindingsDictionary _additionalBindings;
        private IServiceProvider _serviceProvider;

        public DefaultDisplayManagerTests()
        {
            _defaultShapeTable = new ShapeTable
            (
                new Dictionary<string, ShapeDescriptor>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, ShapeBinding>(StringComparer.OrdinalIgnoreCase)
            );
            _additionalBindings = new TestShapeBindingsDictionary();

            IServiceCollection serviceCollection = new ServiceCollection();

            serviceCollection.AddScoped<IThemeManager, ThemeManager>();
            serviceCollection.AddScoped<IHtmlDisplay, DefaultHtmlDisplay>();
            serviceCollection.AddScoped<IShapeTableManager, TestShapeTableManager>();
            serviceCollection.AddScoped<TestShapeBindingResolver>();
            serviceCollection.AddScoped<IShapeBindingResolver>( x=> x.GetRequiredService<TestShapeBindingResolver>());
            serviceCollection.AddScoped<IAdminTemplatesShapeBindingNameResolver>( x=> x.GetRequiredService<TestShapeBindingResolver>());

            serviceCollection.AddScoped<IShapeDisplayEvents, TestDisplayEvents>();
            serviceCollection.AddScoped<IExtensionManager, StubExtensionManager>();
            serviceCollection.AddSingleton<IStringLocalizerFactory, NullStringLocalizerFactory>();
            serviceCollection.AddTransient(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));

            serviceCollection.AddLogging();

            serviceCollection.AddSingleton(_defaultShapeTable);
            serviceCollection.AddSingleton(_additionalBindings);
            serviceCollection.AddWebEncoders();

            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        private class TestDisplayEvents : IShapeDisplayEvents
        {
            public Action<ShapeDisplayContext> Displaying = ctx => { };
            public Action<ShapeDisplayContext> Displayed = ctx => { };
            public Action<ShapeDisplayContext> Finalized = ctx => { };

            Task IShapeDisplayEvents.DisplayingAsync(ShapeDisplayContext context)
            {
                Displaying(context); return Task.CompletedTask;
            }
            Task IShapeDisplayEvents.DisplayedAsync(ShapeDisplayContext context)
            {
                Displayed(context); return Task.CompletedTask;
            }
            Task IShapeDisplayEvents.DisplayingFinalizedAsync(ShapeDisplayContext context)
            {
                Finalized(context); return Task.CompletedTask;
            }
        }

        private void AddShapeDescriptor(ShapeDescriptor shapeDescriptor)
        {
            _defaultShapeTable.Descriptors[shapeDescriptor.ShapeType] = shapeDescriptor;
            foreach (var binding in shapeDescriptor.Bindings)
            {
                _defaultShapeTable.Bindings[binding.Key] = binding.Value;
            }
        }

        private static DisplayContext CreateDisplayContext(Shape shape)
        {
            return new DisplayContext
            {
                Value = shape
            };
        }

        [Fact]
        public async Task RenderSimpleShape()
        {
            var displayManager = _serviceProvider.GetService<IHtmlDisplay>();

            var shape = new Shape();
            shape.Metadata.Type = "Foo";

            var descriptor = new ShapeDescriptor
            {
                ShapeType = "Foo"
            };
            descriptor.Bindings["Foo"] = new ShapeBinding
            {
                BindingName = "Foo",
                BindingAsync = ctx => Task.FromResult<IHtmlContent>(new HtmlString("Hi there!"))
            };
            AddShapeDescriptor(descriptor);

            var result = await displayManager.ExecuteAsync(CreateDisplayContext(shape));
            Assert.Equal("Hi there!", result.ToString());
        }

        [Fact]
        public async Task RenderIShapeBindingResolverProvidedShapes()
        {
            var displayManager = _serviceProvider.GetService<IHtmlDisplay>();

            var shape = new Shape();
            shape.Metadata.Type = "Baz";

            _additionalBindings["Baz"] = new ShapeBinding
            {
                BindingName = "Baz",
                BindingAsync = ctx => Task.FromResult<IHtmlContent>(new HtmlString("Hi from IShapeBindingResolver."))
            };

            var result = await displayManager.ExecuteAsync(CreateDisplayContext(shape));

            // Cleanup
            _additionalBindings.Clear();

            Assert.Equal("Hi from IShapeBindingResolver.", result.ToString());
        }

        [Fact]
        public async Task RenderPreCalculatedShape()
        {
            var displayManager = _serviceProvider.GetService<IHtmlDisplay>();

            var shape = new Shape();
            shape.Metadata.Type = "Foo";
            shape.Metadata.OnDisplaying(
                context =>
                {
                    context.ChildContent = new HtmlString("Bar");
                });

            var descriptor = new ShapeDescriptor
            {
                ShapeType = "Foo"
            };
            descriptor.Bindings["Foo"] = new ShapeBinding
            {
                BindingName = "Foo",
                BindingAsync = ctx => Task.FromResult<IHtmlContent>(new HtmlString("Hi there!"))
            };
            AddShapeDescriptor(descriptor);

            var result = await displayManager.ExecuteAsync(CreateDisplayContext(shape));
            Assert.Equal("Bar", result.ToString());
        }

        [Fact]
        public async Task IShapeBindingResolverProvidedShapesDoesNotOverrideShapeDescriptor()
        {
            var displayManager = _serviceProvider.GetService<IHtmlDisplay>();

            var shape = new Shape();
            shape.Metadata.Type = "Foo";

            var descriptor = new ShapeDescriptor
            {
                ShapeType = "Foo",
                ProcessingAsync = new Func<ShapeDisplayContext, Task>[] {
                    context =>
                    {
                        dynamic dynamicShape = context.Shape;
                        dynamicShape.Data = "some data";
                        return Task.CompletedTask;
                    }
                }
            };
            descriptor.Bindings["Foo"] = new ShapeBinding
            {
                BindingName = "Foo",
                BindingAsync = ctx => Task.FromResult<IHtmlContent>(new HtmlString("Is there any data ?"))
            };
            AddShapeDescriptor(descriptor);

            _additionalBindings["Foo"] = new ShapeBinding
            {
                BindingName = "Foo",
                BindingAsync = ctx => Task.FromResult<IHtmlContent>(new HtmlString($"Yes there is { ((dynamic)ctx.Value).Data }."))
            };

            var result = await displayManager.ExecuteAsync(CreateDisplayContext(shape));

            // Cleanup
            _additionalBindings.Clear();

            Assert.Equal("Yes there is some data.", result.ToString());
        }

        [Fact]
        public async Task RenderFallbackShape()
        {
            var displayManager = _serviceProvider.GetService<IHtmlDisplay>();

            var shape = new Shape();
            shape.Metadata.Type = "Foo__2";

            var descriptor = new ShapeDescriptor
            {
                ShapeType = "Foo"
            };
            descriptor.Bindings["Foo"] = new ShapeBinding
            {
                BindingName = "Foo",
                BindingAsync = ctx => Task.FromResult<IHtmlContent>(new HtmlString("Hi there!"))
            };
            AddShapeDescriptor(descriptor);

            var result = await displayManager.ExecuteAsync(CreateDisplayContext(shape));
            Assert.Equal("Hi there!", result.ToString());
        }

        [Fact]
        public async Task AddAlternatesOnDisplaying()
        {
            var displayManager = _serviceProvider.GetService<IHtmlDisplay>();

            var shape = new Shape();
            shape.Metadata.Type = "Foo";

            var descriptor = new ShapeDescriptor
            {
                ShapeType = "Foo",
                DisplayingAsync = new Func<ShapeDisplayContext, Task>[] {
                    context =>
                    {
                            context.Shape.Metadata.Alternates.Add("Bar");
                            return Task.CompletedTask;
                    }
                }
            };
            descriptor.Bindings["Foo"] = new ShapeBinding
            {
                BindingName = "Foo",
                BindingAsync = ctx => Task.FromResult<IHtmlContent>(new HtmlString("Foo"))
            };
            descriptor.Bindings["Bar"] = new ShapeBinding
            {
                BindingName = "Bar",
                BindingAsync = ctx => Task.FromResult<IHtmlContent>(new HtmlString("Bar"))
            };
            AddShapeDescriptor(descriptor);

            var result = await displayManager.ExecuteAsync(CreateDisplayContext(shape));
            Assert.Equal("Bar", result.ToString());
        }

        [Fact]
        public async Task AddAlternatesOnProcessing()
        {
            var displayManager = _serviceProvider.GetService<IHtmlDisplay>();

            var shape = new Shape();
            shape.Metadata.Type = "Foo";

            var descriptor = new ShapeDescriptor
            {
                ShapeType = "Foo",
                ProcessingAsync = new Func<ShapeDisplayContext, Task>[] {
                    context =>
                    {
                            context.Shape.Metadata.Alternates.Add("Bar");
                            return Task.CompletedTask;
                    }
                }
            };
            descriptor.Bindings["Foo"] = new ShapeBinding
            {
                BindingName = "Foo",
                BindingAsync = ctx => Task.FromResult<IHtmlContent>(new HtmlString("Foo"))
            };
            descriptor.Bindings["Bar"] = new ShapeBinding
            {
                BindingName = "Bar",
                BindingAsync = ctx => Task.FromResult<IHtmlContent>(new HtmlString("Bar"))
            };
            AddShapeDescriptor(descriptor);

            var result = await displayManager.ExecuteAsync(CreateDisplayContext(shape));
            Assert.Equal("Bar", result.ToString());
        }

        [Fact]
        public async Task RenderAlternateShapeExplicitly()
        {
            var displayManager = _serviceProvider.GetService<IHtmlDisplay>();

            var shape = new Shape();
            shape.Metadata.Type = "Foo__2";

            var descriptor = new ShapeDescriptor
            {
                ShapeType = "Foo"
            };
            descriptor.Bindings["Foo"] = new ShapeBinding
            {
                BindingName = "Foo",
                BindingAsync = ctx => Task.FromResult<IHtmlContent>(new HtmlString("Hi there!"))
            };
            descriptor.Bindings["Foo__2"] = new ShapeBinding
            {
                BindingName = "Foo__2",
                BindingAsync = ctx => Task.FromResult<IHtmlContent>(new HtmlString("Hello again!"))
            };
            AddShapeDescriptor(descriptor);

            var result = await displayManager.ExecuteAsync(CreateDisplayContext(shape));
            Assert.Equal("Hello again!", result.ToString());
        }

        [Fact]
        public async Task RenderAlternateShapeByMostRecentlyAddedMatchingAlternate()
        {
            var displayManager = _serviceProvider.GetService<IHtmlDisplay>();

            var shape = new Shape();
            shape.Metadata.Type = "Foo";
            shape.Metadata.Alternates.Add("Foo__1");
            shape.Metadata.Alternates.Add("Foo__2");
            shape.Metadata.Alternates.Add("Foo__3");

            var descriptor = new ShapeDescriptor
            {
                ShapeType = "Foo"
            };
            AddBinding(descriptor, "Foo", ctx => Task.FromResult<IHtmlContent>(new HtmlString("Hi there!")));
            AddBinding(descriptor, "Foo__1", ctx => Task.FromResult<IHtmlContent>(new HtmlString("Hello (1)!")));
            AddBinding(descriptor, "Foo__2", ctx => Task.FromResult<IHtmlContent>(new HtmlString("Hello (2)!")));
            AddShapeDescriptor(descriptor);

            var result = await displayManager.ExecuteAsync(CreateDisplayContext(shape));
            Assert.Equal("Hello (2)!", result.ToString());
        }

        private static void AddBinding(ShapeDescriptor descriptor, string bindingName, Func<DisplayContext, Task<IHtmlContent>> binding)
        {
            descriptor.Bindings[bindingName] = new ShapeBinding
            {
                BindingName = bindingName,
                BindingAsync = binding
            };
        }

        [Fact]
        public async Task ShapeDescriptorDisplayingAndDisplayedAreCalled()
        {
            var displayManager = _serviceProvider.GetService<IHtmlDisplay>();

            var shape = new Shape();
            shape.Metadata.Type = "Foo";

            var descriptor = new ShapeDescriptor
            {
                ShapeType = "Foo"
            };
            AddBinding(descriptor, "Foo", ctx => Task.FromResult<IHtmlContent>(new HtmlString("yarg")));
            AddShapeDescriptor(descriptor);

            var displayingEventCount = 0;
            var displayedEventCount = 0;
            descriptor.DisplayingAsync = new Func<ShapeDisplayContext, Task>[] { ctx => { ++displayingEventCount; return Task.CompletedTask; } };
            descriptor.DisplayedAsync = new Func<ShapeDisplayContext, Task>[] { ctx => { ++displayedEventCount; ctx.ChildContent = new HtmlString("[" + ctx.ChildContent.ToString() + "]"); return Task.CompletedTask; } };

            var result = await displayManager.ExecuteAsync(CreateDisplayContext(shape));

            Assert.Equal(1, displayingEventCount);
            Assert.Equal(1, displayedEventCount);
            Assert.Equal("[yarg]", result.ToString());
        }

        [Fact]
        public async Task DisplayingEventFiresEarlyEnoughToAddAlternateShapeBindingNames()
        {
            var htmlDisplay = _serviceProvider.GetService<IHtmlDisplay>();

            var shapeFoo = new Shape();
            shapeFoo.Metadata.Type = "Foo";

            var descriptorFoo = new ShapeDescriptor
            {
                ShapeType = "Foo"
            };
            AddBinding(descriptorFoo, "Foo", ctx => Task.FromResult<IHtmlContent>(new HtmlString("alpha")));
            AddShapeDescriptor(descriptorFoo);

            var descriptorBar = new ShapeDescriptor
            {
                ShapeType = "Bar"
            };
            AddBinding(descriptorBar, "Bar", ctx => Task.FromResult<IHtmlContent>(new HtmlString("beta")));
            AddShapeDescriptor(descriptorBar);

            var resultNormally = await htmlDisplay.ExecuteAsync(CreateDisplayContext(shapeFoo));

            shapeFoo = new Shape();
            shapeFoo.Metadata.Type = "Foo";
            descriptorFoo.DisplayingAsync = new Func<ShapeDisplayContext, Task>[] { ctx => { ctx.Shape.Metadata.Alternates.Add("Bar"); return Task.CompletedTask; } };
            var resultWithOverride = await htmlDisplay.ExecuteAsync(CreateDisplayContext(shapeFoo));

            Assert.Equal("alpha", resultNormally.ToString());
            Assert.Equal("beta", resultWithOverride.ToString());
        }

        [Fact]
        public async Task ShapeTypeAndBindingNamesAreNotCaseSensitive()
        {
            var displayManager = _serviceProvider.GetService<IHtmlDisplay>();

            var shapeFoo = new Shape();
            shapeFoo.Metadata.Type = "Foo";

            var descriptorFoo = new ShapeDescriptor
            {
                ShapeType = "Foo"
            };
            AddBinding(descriptorFoo, "Foo", ctx => Task.FromResult<IHtmlContent>(new HtmlString("alpha")));
            AddShapeDescriptor(descriptorFoo);

            var result = await displayManager.ExecuteAsync(CreateDisplayContext(shapeFoo));

            Assert.Equal("alpha", result.ToString());
        }

        [Fact]
        public async Task FilteringAdminShapesIShapeBindingResolverProvidedShapeNames()
        {
            var displayManager = _serviceProvider.GetService<IHtmlDisplay>();

            var resolvers = _serviceProvider.GetService<IEnumerable<IAdminTemplatesShapeBindingNameResolver>>();

            // Set as Admin template
            _additionalBindings.IsAdminShape = true;

            _additionalBindings["Baz_DisplayOption"] = new ShapeBinding
            {
                BindingName = "Baz_DisplayOption",
                BindingAsync = ctx => Task.FromResult<IHtmlContent>(new HtmlString("Hi Baz, from IShapeBindingResolver."))
            };

            _additionalBindings["Foo_Edit"] = new ShapeBinding
            {
                BindingName = "Foo_Edit",
                BindingAsync = ctx => Task.FromResult<IHtmlContent>(new HtmlString("Hi Foo, from IShapeBindingResolver."))
            };

            var resolverEnumerator = resolvers.GetEnumerator();
            resolverEnumerator.MoveNext();
            var resolver = resolverEnumerator.Current; 

            var shapeNames = await resolver.GetShapeBindingNamesAsync(t => t.EndsWith("_DisplayOption", StringComparison.OrdinalIgnoreCase));
            var shapeEnumerator = shapeNames.GetEnumerator();
            shapeEnumerator.MoveNext();
            var shapeName = shapeEnumerator.Current;

            var shape = new Shape();
            shape.Metadata.Type = shapeName;

            var result = await displayManager.ExecuteAsync(CreateDisplayContext(shape));
            Assert.Equal("Hi Baz, from IShapeBindingResolver.", result.ToString());
            
            // Cleanup
            _additionalBindings.Clear();
            // Reset
            _additionalBindings.IsAdminShape = false;


        }
    }
}
