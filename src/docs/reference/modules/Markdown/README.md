# Markdown (`OrchardCore.Markdown`)

## Theming

### Shapes

The following shapes are rendered when the `MarkdownBodyPart` is attached to a content type:

| Name | Display Type | Default Location | Model Type |
| ------| ------------ |----------------- | ---------- |
| `MarkdownBodyPart` | `Detail` | `Content:5` | `MarkdownBodyPartViewModel` |
| `MarkdownBodyPart` | `Summary` | `Content:10` | `MarkdownBodyPartViewModel` |

### `BodyPartViewModel`

The following properties are available on the `MarkdownBodyPartViewModel` class.

| Property | Type | Description |
| --------- | ---- |------------ |
| `Markdown` | `string` | The Markdown value after all tokens have been processed. |
| `Html` | `string` | The HTML content resulting from the Markdown source. |
| `ContentItem` | `ContentItem` | The content item of the part. |
| `MarkdownBodyPart` | `MarkdownBodyPart` | The `MarkdownBodyPart` instance. |
| `TypePartSettings` | `MarkdownBodyPartSettings` | The settings of the part. |

### `MarkdownBodyPart`

The following properties are available on `MarkdownBodyPart`:

| Name | Type | Description |
| -----| ---- |------------ |
| `Markdown` | The Markdown content. It can contain Liquid tags so using it directly might result in unexpected results. Prefer rendering the `MarkdownBodyPart` shape instead. |
| `Content` | The raw content of the part. |
| `ContentItem` | The content item containing this part. |

### `MarkdownField`

This shape is rendered when a `MarkdownField` is attached to a content part.
The shape base class is of type `MarkdownFieldViewModel`.

The following properties are available on the `MarkdownFieldViewModel` class.

| Property | Type | Description |
| --------- | ---- |------------ |
| `Markdown` | `string` | The Markdown value once all tokens have been processed. |
| `Html` | `string` | The HTML content resulting from the Markdown source. |
| `Field` | `MarkdownField` | The `MarkdownField` instance. |
| `Part` | `ContentPart` | The part this field is attached to. |
| `PartFieldDefinition` | `ContentPartFieldDefinition` | The part field definition. |

## Sanitization

Markdown output is sanitized during the rendering of content with Display Management.

You can disable this by unchecking the `Sanitize HTML` setting, or further configuring the [HTML Sanitizer](../../core/Sanitizer/README.md)

When rendering content directly you can disable sanitization by passing a boolean to the helper.

## Editors

The __Markdown Part__ editor can be different for each content type. In the __Markdown Part__ settings of a 
content type, just select the one that needs to be used.

There are two predefined editor names:

- `Default` is the editor that is used by default.
- `Wysiwyg` is the editor that provides a WYSIWYG experience.

### Custom Editors

Customizing the editor can mean to replace the predefined one with different experiences, or to provide
new options for the user to choose from.

To create a new custom editor, it is required to provide two shape templates, one to provide
the name of the editor (optional if you want to override an existing one), and a shape to
render the actual HTML for the editor.

#### Declaration

To declare a new editor, create a shape named `Markdown_Option__{Name}` where `{Name}` is a value 
of your choosing. This will be represented by a file named `Markdown-{Name}.Option.cshtml`.

Sample content:

```csharp
@{
    string currentEditor = Model.Editor;
}
<option value="Wysiwyg" selected="@(currentEditor == "Wysiwyg")">@T["Wysiwyg editor"]</option>
```

#### HTML Editor

To define what HTML to render when the editor is selected from the settings, a shape named 
`Markdown_Edit__{Name}` corresponding to a file `Markdown-{Name}.Edit.cshtml` can be created.

Sample content:

```csharp
@using OrchardCore.Markdown.ViewModels;
@model MarkdownBodyPartViewModel

<fieldset class="mb-3">
    <label asp-for="Markdown">@T["Markdown"]</label>
    <textarea asp-for="Markdown" rows="5" class="form-control"></textarea>
    <span class="hint">@T["The markdown of the content item."]</span>
</fieldset>
```

### Overriding the predefined editors

You can override the HTML editor for the `Default` editor by creating a shape file named 
`Markdown.Edit.cshtml`. The WYSIWYG editor is defined by using the file named 
`Markdown-Wysiwyg.Edit.cshtml`.

## Razor Helper

To render a Markdown string to HTML within Razor use the `MarkdownToHtmlAsync` helper extension method on the view's base `Orchard` property, e.g.:

```csharp
@await Orchard.MarkdownToHtmlAsync((string)Model.ContentItem.Content.MarkdownParagraph.Content.Markdown)
```

In this example we assume that `Model.ContentItem.Content.MarkdownParagraph.Content` represents an `MarkdownField`, and `Markdown` is the field value, and we cast to a string, as extension methods do not support dynamic dispatching.

This helper will also parse any liquid included in the Markdown.

By default this helper will also sanitize the Markdown. 

To disable sanitization:

```csharp
@await Orchard.MarkdownToHtmlAsync((string)Model.ContentItem.Content.MarkdownParagraph.Content.Markdown, false)
```

## Markdown Configuration

The following configuration values are used by default and can be customized:

```json
    "OrchardCore_Markdown": {
      "Extensions": "nohtml+advanced"
    }
```

The supported extensions described as following:

| Extension | Description |
| --- | --- |
| `advanced` | Enable advanced markdown extensions |
| `pipetables` | Adds a pipe table |
| `gfm-pipetables` | Adds a pipe table with using header for column count |
| `hardlinebreak` | Uses the softline break as hardline break |
| `footnotes` | Allows a footnotes |
| `footers` | Adds footer block |
| `citations` | Adds citation |
| `attributes` |  Allows to attach HTML attributes |
| `gridtables` | Adds grid table |
| `abbreviations` | Stores an abbreviation object at the document level |
| `emojis` | Supports the emojis and smileys |
| `definitionlists` | Adds a definition list |
| `customcontainers` | Adds a block custom container |
| `figures` | Adds figure |
| `mathematics` | Enable mathematics symbols |
| `bootstrap` | Enable bootstrap classes |
| `medialinks` | Extends image Markdown links in case a video or an audio file is linked and output proper link |
| `smartypants` | Uses the SmartyPants |
| `autoidentifiers` | Uses the auto-identifier |
| `tasklists` | Adds the task list |
| `diagrams` | Allows diagrams |
| `nofollowlinks` | Add rel=nofollow to all links rendered to HTML |
| `noopenerlinks` |  |
| `noreferrerlinks` | Adds rel=nofollow to all links rendered to HTML |
| `nohtml` | Disables html support |
| `yaml` | Parses a YAML format into the MarkdownDocument |
| `nonascii-noescape` | Disables URI escape with % characters for non-US-ASCII characters |
| `autolinks` | Enable autolinks from text `http://`, `https://`, `ftp://`, `mailto:`, `www.xxx.yyy` |
| `globalization` | Adds support for right-to-left content by adding appropriate html attribtues |

## Markdown Pipeline

The markdown pipeline is configurable using `IOptions<MarkdownPipelineOptions>` during service registration with a configuration 
extension method `ConfigureMarkdownPipeline`.

By default the pipeline enables some markdown advanced features and disables HTML by converting any HTML found in the Markdown content to escaped HTML entities.

You may call this extension method multiple times during the startup pipeline to alter configurations.

To clear this configuration:

```
services
    .AddOrchardCms()
    .ConfigureServices(tenantServices =>
        tenantServices.PostConfigure<MarkdownPipelineOptions>(o =>
            {
                o.Configure.Clear();
            }));
```

To include other `MarkdownPipelineOptions` such as emojis and smileys we could use:

```
services
    .AddOrchardCms()
    .ConfigureServices(tenantServices =>
        tenantServices.ConfigureMarkdownPipeline((pipeline) => 
        { 
            pipeline.UseEmojiAndSmiley();
        }));
```
