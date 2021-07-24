# Orchard Core

Orchard Core is a redevelopment of [Orchard CMS](https://github.com/OrchardCMS/Orchard) on [ASP.NET Core](https://docs.microsoft.com/aspnet/core/). 

Orchard Core consists of two different targets:

- **Orchard Core Framework**: An application framework for building **modular**, **multi-tenant** applications on ASP.NET Core.
- **Orchard Core CMS**: A Web Content Management System (CMS) built on top of the Orchard Core Framework.

It’s important to note the differences between the framework and the CMS. Some developers who want to develop SaaS applications will only be interested in the modular framework. Others who want to build administrable websites will focus on the CMS and build modules to enhance their sites or the whole ecosystem.

[![Orchard Core CN 中文讨论组](https://pub.idqqimg.com/wpa/images/group.png)](//shang.qq.com/wpa/qunwpa?idkey=48721591a71ee7586316604a7a4ee99d26fd977c6120370a06585085a5936f62)
[![Join the chat at https://gitter.im/OrchardCMS/OrchardCore](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/OrchardCMS/OrchardCore?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![BSD-3-Clause License](https://img.shields.io/badge/license-BSD--3--Clause-blue.svg)](https://github.com/OrchardCMS/OrchardCore/blob/master/LICENSE)
[![Documentation](https://readthedocs.org/projects/orchardcore/badge/)](https://docs.orchardcore.net/)
[![Crowdin](https://badges.crowdin.net/orchard-core/localized.svg)](https://crowdin.com/project/orchard-core)

## Building Software as a Service (SaaS) solutions with the Orchard Core Framework

It’s very important to understand the Orchard Core Framework is distributed independently from the CMS on nuget.org. We’ve made some sample applications on <https://github.com/OrchardCMS/OrchardCore.Samples> that will guide you on how to build **modular** and **multi-tenant** applications using just Orchard Core Framework without any of the CMS specific features.

One of our goals is to enable community-based ecosystems of hosted applications which can be extended with modules, like e-commerce systems, blog engines and more. The Orchard Core Framework enables a modular environment that allows different teams to work on separate parts of an application and make components reusable across projects.

## Building Website with Orchard Core CMS

Orchard Core CMS is a complete rewrite of Orchard CMS on ASP.NET Core. It’s not just a port as we wanted to improve the performance drastically and align as close as possible to the development models of ASP.NET Core.

- **Performance**. This might the most obvious change when you start using Orchard Core CMS. It’s extremely fast for a CMS. So fast that we haven’t even cared about working on an output cache module. To give you an idea, without caching Orchard Core CMS is around 20 times faster than the previous version.

- **Portable**. You can now develop and deploy Orchard Core CMS on Windows, Linux and macOS. We also have Docker images ready for use.

- **Document database** abstraction. Orchard Core CMS still requires a relational database, and is compatible with SQL Server, MySQL, PostgreSQL and SQLite, but it’s now using a document abstraction (YesSql) that provides a document database API to store and query documents. This is a much better approach for CMS systems and helps performance significantly.

- **NuGet Packages**. Modules and themes are now shared as NuGet packages. Creating a new website with Orchard Core CMS is actually as simple as referencing a single meta package from the NuGet gallery. It also means that updating to a newer version only involves updating the version number of this package.

- **Live preview**. When editing a content item, you can now see live how it will look like on your site, even before saving your content. And it also works for templates, where you can browse any page to inspect the impact of a change on templates as you type it.

- **Liquid templates support**. Editors can safely change the HTML templates with the Liquid template language. It was chosen as it’s both very well documented (Jekyll, Shopify, …) and secure.

- **Custom queries**. We wanted to provide a way for developers to access all their data as simply as possible. We created a module that lets you create custom ad-hoc SQL and Lucene queries that can be re-used to display custom content, or exposed as API endpoints. You can use it to create efficient queries, or expose your data to SPA applications.

- **Deployment plans**. Deployment plans are scripts that can contain content and metadata to build a website. You can now include binary files, and even use them to deploy your sites remotely from a staging to a production environment for instance. They can also be part of NuGet Packages, allowing you to ship predefined websites.

- **Scalability**. Because Orchard Core is a multi-tenant system, you can host as many websites as you want with a single deployment. A typical cloud machine can then host thousands of sites in parallel, with database, content, theme and user isolation.

- **Workflows**. Create content approval workflows, react to webhooks, take actions when forms are submitted, and any other process you'd like to implement with a user friendly UI.

- **GraphQL**. We provide a very flexible GraphQL API, such that any authorized external application can reuse your content, like SPA applications or static site generators.

### Different website building strategies

Orchard Core CMS supports all major site building strategies:

- **Full CMS**. In this mode, the website uses a theme and templates to render your content, aiming for little to no custom development at all.

- **Decoupled CMS**. The site starts off blank, apart from the content management back-end. You create all the templates you need with Razor Pages or MVC actions and access your content via the content services. Ref: <https://www.youtube.com/watch?v=yWpz8p-oaKg>

- **Headless CMS**. The site only manages the content, and you create a separate application that will fetch the managed content using GraphQL or REST APIs. Ref: <https://www.youtube.com/watch?v=4o9zG17cfa0>

## Status

The latest released version of Orchard Core is `1.0.0`.
The release notes can be found on <https://github.com/OrchardCMS/OrchardCore/releases/tag/1.0.0>

Here is a more detailed [roadmap](https://github.com/OrchardCMS/OrchardCore/wiki/Roadmap).

## Getting Started

- Clone the repository using the command `git clone https://github.com/OrchardCMS/OrchardCore.git` and checkout the `master` branch for the latest release, or the `dev` branch for the cutting-edge version.

- Watch the ASP.NET Community Standup video where Orchard Core was demonstrated: <https://www.youtube.com/watch?v=HeDjv3blBjQ&t=2246s&list=PL1rZQsJPBU2StolNg0aqvQswETPcYnNKL&index=24>

- Follow the samples on <https://github.com/OrchardCMS/OrchardCore.Samples> that will guide you on how to build **modular** and **multi-tenant** applications

- Follow the tutorial inside the [Training Demo Module](https://github.com/Lombiq/Orchard-Training-Demo-Module) to learn how to develop Orchard Core modules.

### Command line

- Install the latest version of the .NET SDK from this page <https://dotnet.microsoft.com/download>
- Next, navigate to `D:\OrchardCore\src\OrchardCore.Cms.Web` or wherever your folder is on the commandline in Administrator mode.
- Call `dotnet run`.
- Then open the `http://localhost:5000` URL in your browser.

You can also read the [Code Generation Templates documentation](docs/getting-started/templates/README.md) to create new applications from predefined templates.

### Visual Studio

For more details on the various development tools we recommend for using with Orchard Core check out [the Development Tools documentation page](docs/resources/development-tools/README.md).

- Download Visual Studio 2019 (any edition) from <https://www.visualstudio.com/downloads/>.
- Open `OrchardCore.sln` and wait for Visual Studio to restore all Nuget packages.
- Ensure `OrchardCore.Cms.Web` is the startup project and run it.
- Optionally install the [Lombiq Orchard Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=LombiqVisualStudioExtension.LombiqOrchardVisualStudioExtension) to add some useful utilities to your Visual Studio such as an error log watcher or a dependency injector.
- Optionally install the [code snippets from the Orchard Dojo Library](https://orcharddojo.net/orchard-resources/CoreLibrary/Utilities/VisualStudioSnippets/) to quickly generate code in some common scenarios during module and theme development.

### Docker

- Run `docker run --name orchardcms orchardproject/orchardcore-cms-linux:latest`

Docker images and parameters can be found at <https://hub.docker.com/u/orchardproject/>
