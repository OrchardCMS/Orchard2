# Working on a Site with a Development Team

If multiple people will be working on the same website (development team approach), or if you plan to work on multiple versions of the same website (development / staging / production), then continue reading.

## App_Data

In OrchardCore, the App_Data folder is used to store tenant details for the system `tenants.json`, the log files `\logs` and also details for each tenant in a tenant specific folder `\sites\{tenant name}`.

A tenant folder, by default, stores "Keys", "Media" (uploaded content) and the `app.settings` file. It also stores the definition of each Content Type in the `ContentDefinition.json` file.

It is possible to store the Content Types in the database instead.

## Development Team Approach

Possibly the first choice to make using the development team approach is whether you can share a database with real-time access during development. If so, then it is a good idea to move to using the Database storage approach for Content Types. If not, then leaving them in the file system and sharing them through git commits is likely the easiest way.

A good starting point then is to keep the `Sites/Default/appsettings.json` file stored locally, and shared through git commits. It can contain only the `SqlProvider` string in it to prevent the setup routine from running when cloning a project to a new machine.

Providing a central database is relatively straightforward using a shared SQL server or Azure SQL instance.

[Site configuration](https://docs.orchardcore.net/en/dev/docs/reference/core/Configuration/) can easily be managed through the use of User Secrets / `appsettings.json` Similarly, the `tenants.json` and related `appsettings.json` for the Default shell and any configured tenants can be [stored in Azure or in a Database](https://docs.orchardcore.net/en/dev/docs/reference/core/Shells/).

• Data protection in Azure ???

The Media folder for each tenant can also be configured to [live centrally in Azure](https://docs.orchardcore.net/en/dev/docs/reference/modules/Media.Azure/) as opposed to on the disk in the App_Data folder.

With the setup mentioned above, you can now work with multiple developers on the same site. Each can begin creating centrally stored content types. When doing this, though, templates should be prepared for situations where the template and the data are out of sync. This means that it's best to write templates that deal well with null values.

## Multiple Servers (development / staging / production) Approach

When it's time to deploy, the built in ["Deployment Plans" feature](https://docs.orchardcore.net/en/dev/docs/guides/content-definitions/#step-one-create-a-deployment-plan) helps quickly automate the task.
