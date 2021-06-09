# Body (`OrchardCore.Html`)

## Theming

### Shapes

The following shapes are rendered when the `HtmlBodyPart` is attached to a content type:

| Name | Display Type | Default Location | Model Type |
| ------| ------------ |----------------- | ---------- |
| `HtmlBodyPart` | `Detail` | `Content:5` | `HtmlBodyPartViewModel` |
| `HtmlBodyPart` | `Summary` | `Content:10` | `HtmlBodyPartViewModel` |

### HtmlBodyPartViewModel

The following properties are available on the `HtmlBodyPartViewModel` class:

| Property | Type | Description |
| --------- | ---- |------------ |
| `Body` | `string` | The content that was edited. It might contain tokens. |
| `Html` | `string` | The HTML content once all tokens have been processed. |
| `ContentItem` | `ContentItem` | The content item of the part. |
| `HtmlBodyPart` | `HtmlBodyPart` | The `HtmlBodyPart` instance. |
| `TypePartSettings` | `HtmlBodyPartSettings` | The settings of the part. |

### HtmlBodyPart

The following properties are available on `HtmlBodyPart`:

| Name | Type | Description |
| -----| ---- |------------ |
| `Body` | `string` | The HTML content in the body. It can contain Liquid tags so using it directly might result in unexpected results. Prefer rendering the `HtmlBodyPart` shape instead. |
| `Content` | The raw content of the part. |
| `ContentItem` | The content item containing this part. |

## Sanitization

By default all HTML input is sanitized when the `HtmlBodyPart` is saved.

You can disable this by unchecking the `Sanitize HTML` setting, or further configuring the [HTML Sanitizer](../../core/Sanitizer/README.md)

## Editors

The __HtmlBody Part__ editor can be different for each content type. In the __HtmlBody Part__ settings of a 
content type, just select the one that needs to be used.

There are three predefined editor names:

- `Default` is the editor that is used by default.
- `Wysiwyg` is the editor that provides a WYSIWYG experience.
- `Monaco` is the editor that provides a source code experience.

### Custom Editors

Customizing the editor can mean to replace the predefined one with different experiences, or to provide
new options for the user to choose from.

To create a new custom editor, it is required to provide two shape templates, one to provide
the name of the editor (optional if you want to override an existing one), and a shape to
render the actual HTML for the editor.

#### Declaration

To declare a new editor, create a shape named `HtmlBodyPart_Option__{Name}` where `{Name}` is a value 
of your choosing. This will be represented by a file named `HtmlBodyPart-{Name}.Option.cshtml`.

Sample content:

```csharp
@{
    string currentEditor = Model.Editor;
}
<option value="Wysiwyg" selected="@(currentEditor == "Wysiwyg")">@T["Wysiwyg editor"]</option>
```

#### HTML Editor

To define what HTML to render when the editor is selected from the settings, a shape named `HtmlBodyPart_Edit__{Name}` corresponding to a file `HtmlBodyPart-{Name}.Edit.cshtml` can be created.

Sample content:

```csharp
@using OrchardCore.Html.ViewModels;
@model HtmlBodyPartViewModel

<fieldset class="mb-3">
    <label asp-for="Body">@T["Body"]</label>
    <textarea asp-for="Body" rows="5" class="form-control"></textarea>
    <span class="hint">@T["The body of the content item."]</span>
</fieldset>
```

### Overriding the predefined editors

You can override the HTML editor for the `Default` editor by creating a shape file named  
`HtmlBodyPart.Edit.cshtml`. The Wysiwyg editor is defined by using the file named  
`HtmlBodyPart-Wysiwyg.Edit.cshtml`.
