# ContentLocalization (`OrchardCore.ContentLocalization`)

This module allows you to localize your content items.

## LocalizationPart

Attach this part to a content type to manage multiple localized versions of a content item.

## ContentCulturePicker (`OrchardCore.ContentLocalization.ContentCulturePicker`)

## ContentCulturePicker Feature

The `ContentCulturePicker` feature helps you manage cultures for the frontend.

Enabling this module results in

- A `ContentRequestCultureProvider` being added as the first method used to determine the current thread culture.
    This Provider will set the thread culture based on the ContentItem that matches the current url.
- 2 shapes (described below) are available to the frontend theme.

The `ContentCulturePicker` selects the url to redirect using the following rules

- If the `ContentItem` has a related ContentItem for the selected culture, it redirects to that Item.
- OR If a HomePage is specified, attempts to find a Localization of the Homepage `ContentItem` for the current culture.
- OR redirects to the current page.

### Localization Cookie

By default, the `ContentCulturePicker` sets a cookie for the `CookieRequestCultureProvider`. This can be disabled in the  `Configuration/Settings/ContentCulturePicker` settings page.

#### Recipe Step

The cookie can be set during recipes using the settings step. Here is a sample step:

```json
{
  "name": "settings",
  "ContentCulturePickerSettings": {
    "SetCookie": true
  },
},
```

### Shapes

#### `ContentCulturePicker`

The `ContentCulturePicker` shape loads data for the `ContentCulturePickerContainer` shape.  
You should always render this shape in your theme:

=== "Liquid"

    ``` liquid
    {% shape "ContentCulturePicker" %}
    ```

=== "Razor"

    ``` html
    <shape type="ContentCulturePicker" />
    ```

#### `ContentCulturePickerContainer`

The `ContentCulturePickerContainer` shape is used to render the CulturePicker.
You should override this shape in your theme.

| Property                  | Description                                                 |
| ------------------------- | ----------------------------------------------------------- |
| `Model.CurrentCulture`    | CultureViewModel {Name, DisplayName} representing the current thread culture. |
| `Model.SupportedCultures` | A list of CultureViewModel objects for all supported cultures.   |

##### ContentCulturePickerContainer Example

=== "Liquid"

    ``` liquid
    <ul>
        <li class="nav-item dropdown">
            <a class="nav-link dropdown-toggle" href="#" id="oc-culture-picker" data-bs-toggle="dropdown" aria-haspopup="true" aria-expanded="false">{{Model.CurrentCulture.DisplayName}}</a>
            <div class="dropdown-menu" aria-labelledby="oc-culture-picker">
            {% for culture in Model.SupportedCultures %}
                {% if culture.Name != Model.CurrentCulture.Name  %}
                <a class="dropdown-item" href="{{culture.Name | switch_culture_url }}">{{culture.DisplayName}}</a>
                {% endif %}
            {% endfor %}
            </div>
        </li>
    </ul>
    ```

=== "Razor"

    ``` html
    <ul>
        <li class="nav-item dropdown">
            <a class="nav-link dropdown-toggle" href="#" id="oc-culture-picker" data-bs-toggle="dropdown" aria-haspopup="true" aria-expanded="false">@Model.CurrentCulture.DisplayName</a>
            <div class="dropdown-menu" aria-labelledby="oc-culture-picker">
                @foreach (var culture in Model.SupportedCultures)
                {
                    if (!string.Equals((string)culture.Name, (string)Model.CurrentCulture.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        <a asp-route="RedirectToLocalizedContent"
                        asp-route-area="OrchardCore.ContentLocalization"
                        asp-route-targetculture="@culture.Name"
                        asp-route-contentItemUrl="@Context.Request.Path"
                        class="dropdown-item">@culture.DisplayName</a>
                    }
                }
            </div>
        </li>
    </ul>
    ```

## Liquid filters

### `switch_culture_url`

Returns the URL of the Action that switches cultures.

Input

```liquid
{{ Model.Culture.Name | switch_culture_url }}
```

Output

```text
/Loc1/RedirectToLocalizedContent?targetculture=fr&contentItemUrl=%2Fblog
```

### `localization_set`

Returns the content item in the specified culture (defaults to request culture).

Input

```liquid
{{ Model.ContentItem.Content.LocalizationPart.LocalizationSet | localization_set: "en" }}
```

Output

```text
Title
```

## Configuration

The following configuration is used by default and can be customized:

```json
{
   "OrchardCore": {
    "OrchardCore_ContentLocalization_CulturePicker": {
     "CookieLifeTime": 14 // Set the culture picker cookie life time (in days).
    }
  }
}
```
