using System.Linq;
using System.Threading.Tasks;
using OrchardCore.Autoroute.Models;
using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentManagement;
using OrchardCore.Lists.Models;
using OrchardCore.Tests.Apis.Context;
using Xunit;
using GraphQLApi = OrchardCore.Apis.GraphQL;

namespace OrchardCore.Tests.Apis.GraphQL
{
    public class BlogPostTests
    {
        [Fact]

        public async Task ShouldListAllBlogs()
        {
            using (var context = new BlogContext())
            {
                await context.InitializeAsync();

                var result = await context
                    .GraphQLClient
                    .Content
                    .Query("Blog", builder =>
                    {
                        builder
                            .WithField("contentItemId");
                    });

                Assert.Single(result["data"]["blog"].Children()["contentItemId"]
                    .Where(b => b.ToString() == context.BlogContentItemId));
            }
        }

        [Fact]
        public async Task ShouldQueryByBlogPostAutoroutePart()
        {
            using (var context = new BlogContext())
            {
                await context.InitializeAsync();

                var blogPostContentItemId1 = await context
                    .CreateContentItem("BlogPost", builder =>
                    {
                        builder
                            .DisplayText = "Some sorta blogpost!";

                        builder
                            .Weld(new AutoroutePart
                            {
                                Path = "Path1"
                            });

                        builder
                            .Weld(new ContainedPart
                            {
                                ListContentItemId = context.BlogContentItemId
                            });
                    });

                var blogPostContentItemId2 = await context
                    .CreateContentItem("BlogPost", builder =>
                    {
                        builder
                            .DisplayText = "Some sorta other blogpost!";

                        builder
                            .Weld(new AutoroutePart
                            {
                                Path = "Path2"
                            });

                        builder
                            .Weld(new ContainedPart
                            {
                                ListContentItemId = context.BlogContentItemId
                            });
                    });

                var result = await context
                    .GraphQLClient
                    .Content
                    .Query("BlogPost", builder =>
                    {
                        builder
                            .WithQueryStringArgument("where", "path", "Path1");

                        builder
                            .WithField("DisplayText");
                    });

                Assert.Equal(
                    "Some sorta blogpost!",
                    result["data"]["blogPost"][0]["displayText"].ToString());
            }
        }

        [Fact]
        public async Task WhenThePartHasTheSameNameAsTheContentTypeShouldCollapseFieldsToContentType()
        {
            using (var context = new BlogContext())
            {
                await context.InitializeAsync();

                var result = await context
                    .GraphQLClient
                    .Content
                    .Query("BlogPost", builder =>
                    {
                        builder.WithField("Subtitle");
                    });

                Assert.Equal(
                    "Problems look mighty small from 150 miles up",
                    result["data"]["blogPost"][0]["subtitle"].ToString());
            }
        }

        [Fact]
        public async Task WhenCreatingABlogPostShouldBeAbleToPopulateField()
        {
            using (var context = new BlogContext())
            {
                await context.InitializeAsync();

                var blogPostContentItemId = await context
                    .CreateContentItem("BlogPost", builder =>
                    {
                        builder
                            .DisplayText = "Some sorta blogpost!";

                        builder
                            .Weld("BlogPost", new ContentPart());

                        builder
                            .Alter<ContentPart>("BlogPost", (cp) =>
                            {
                                cp.Weld("Subtitle", new TextField());

                                cp.Alter<TextField>("Subtitle", tf =>
                                {
                                    tf.Text = "Hey - Is this working!?!?!?!?";
                                });
                            });

                        builder
                            .Weld(new ContainedPart
                            {
                                ListContentItemId = context.BlogContentItemId
                            });
                    });

                var result = await context
                    .GraphQLClient
                    .Content
                    .Query("BlogPost", builder =>
                    {
                        builder
                            .WithQueryStringArgument("where", "ContentItemId", blogPostContentItemId);

                        builder
                            .WithField("Subtitle");
                    });

                Assert.Equal(
                    "Hey - Is this working!?!?!?!?",
                    result["data"]["blogPost"][0]["subtitle"].ToString());
            }
        }

        [Fact]
        public async Task ShouldQueryByStatus()
        {
            using (var context = new BlogContext())
            {
                await context.InitializeAsync();

                var draft = await context
                    .CreateContentItem("BlogPost", builder =>
                    {
                        builder.DisplayText = "Draft blog post";
                        builder.Published = false;
                        builder.Latest = true;

                        builder
                            .Weld(new ContainedPart
                            {
                                ListContentItemId = context.BlogContentItemId
                            });
                    }, draft: true);

                var result = await context.GraphQLClient.Content
                    .Query("blogPost(status: PUBLISHED) { displayText, published }");

                Assert.Single(result["data"]["blogPost"]);
                Assert.Equal(true, result["data"]["blogPost"][0]["published"]);

                result = await context.GraphQLClient.Content
                    .Query("blogPost(status: DRAFT) { displayText, published }");

                Assert.Single(result["data"]["blogPost"]);
                Assert.Equal(false, result["data"]["blogPost"][0]["published"]);

                result = await context.GraphQLClient.Content
                    .Query("blogPost(status: LATEST) { displayText, published }");

                Assert.Equal(2, result["data"]["blogPost"].Count());
            }
        }

        [Fact]
        public async Task ShouldNotBeAbleToExecuteAnyQueriesWithoutPermission()
        {
            using var context = new SiteContext()
                .WithPermissionsContext(new PermissionsContext { UsePermissionsContext = true });

            await context.InitializeAsync();

            var response = await context.GraphQLClient.Client.GetAsync("api/graphql");
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ShouldReturnBlogsWithViewBlogContentPermission()
        {
            using var context = new SiteContext()
                .WithPermissionsContext(new PermissionsContext
                {
                    UsePermissionsContext = true,
                    AuthorizedPermissions = new[]
                    {
                        GraphQLApi.Permissions.ExecuteGraphQL,
                        Contents.Permissions.ViewContent
                    }
                });

            await context.InitializeAsync();

            var result = await context.GraphQLClient.Content
                .Query("blog", builder =>
                {
                    builder.WithField("contentItemId");
                });

            Assert.NotEmpty(result["data"]["blog"]);
        }

        [Fact]
        public async Task ShouldNotReturnBlogsWithoutViewBlogContentPermission()
        {
            using var context = new SiteContext()
                .WithPermissionsContext(new PermissionsContext
                {
                    UsePermissionsContext = true,
                    AuthorizedPermissions = new[]
                    {
                        GraphQLApi.Permissions.ExecuteGraphQL
                    }
                });

            await context.InitializeAsync();

            var result = await context.GraphQLClient.Content
                .Query("blog", builder =>
                {
                    builder.WithField("contentItemId");
                });

            Assert.Equal(GraphQLApi.ValidationRules.RequiresPermissionValidationRule.ErrorCode, result["errors"][0]["extensions"]["code"]);
        }
    }
}
