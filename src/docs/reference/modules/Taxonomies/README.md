# Taxonomies (`OrchardCore.Taxonomies`)

This module provides a Taxonomy content type that is used to define managed vocabularies (categories) of any type.  
Taxonomy content items are made of terms organized as a hierarchy. Using the Taxonomy Field allows any content item
to be associated with one or many terms of a taxonomy.

## Shapes

### TaxonomyPart

Display for a taxonomy is routable by enabling `Container routing` feature on the `AutoroutePart` settings for the `Taxonomy`.

The display for the `Taxonomy` is then rendered by the `TaxonomyPart` shape.

This uses the `TermShape` to display a hierarchy of the `Terms`

### TermPart

The `TermPart` is rendered when a `Term` is displayed with the `Container routing` feature of the `AutoroutePart`.

It renders a list of all content items that have been categorized by the `TaxonomyField` as part of that `Term` hierarchy.

### Term Shape

The `TermShape` is used by the `TaxonomyPart` display to render the list of term hierarchies for the `Taxonomy`.

It is a reusable shape which may also be called from a content item, similar to that of a `MenuShape`, to render either the entire `Taxonomy` and term hierarchy, or a part of the `Term` hierarchy.

You might invoke the `TermShape` from a content template to render a sidebar of associated taxonomy terms.

=== "Liquid"

    ``` liquid
    {% shape "term", alias: "alias:categories" %}
    ```

=== "Razor"

    ``` html
    <shape type="Term" alias="Categories" />
    ```

You can also specify a `TermContentItemId` to render a part of the term hierarchy.

=== "Liquid"

    ``` liquid
    {% shape "term", TaxonomyContentItemId: "taxonomyContentItemId" TermContentItemId: "termContentItemId" %}
    ```

=== "Razor"

    ``` html
    <shape type="Term" TaxonomyContentItemId="taxonomyContentItemId" TermContentItemId="termContentItemId" />
    ```

| Property | Description |
| --------- | ------------ |
| `Model.TaxonomyContentItemId` | If defined, contains the content item identifier of the taxonomy to render. |
| `Model.Items` | The list of term items shapes for the taxonomy. These are shapes of type `TermItem`. |
| `Model.Differentiator` | If defined, contains the formatted name of the taxonomy (display text). For instance `Categories`. |
| `Model.TermContentItemId` | If defined, contains the content item identifier of the term to start rendering the hierarchy. |

#### Term Alternates

| Definition | Template | Filename|
| ---------- | --------- | ------------ |
| `Term__[Differentiator]` | `Term__Categories` | `Term-Categories.cshtml` |
| `Term__[ContentType]` | `Term__Category` | `Term-Category.cshtml` |

#### Term Examples

=== "Liquid"

    ``` liquid
    <ul class="list-group list-group-flush {{ Model.Classes | join: " " }}">
        {% for item in Model.Items %}
            {% shape_add_classes item "list-group-item border-0 pb-0" %}
            {{ item | shape_render }}
        {% endfor %}
    </ul>
    ```

=== "Razor"

    ``` html
    @model dynamic
    @if ((bool)Model.HasItems)
    {
        TagBuilder tag = Tag(Model, "ul");

        foreach (var item in Model.Items)
        {
            tag.InnerHtml.AppendHtml(await DisplayAsync(item));
        }

        @tag
    }
    else
    {
        <p class="alert alert-warning">@T["The list is empty"]</p>
    }
    @tag
    ```

### TermItem

The `TermItem` shape is used to render a term item.

| Property | Description |
| --------- | ------------ |
| `Model.Term` | The `Term` shape owning this item. |
| `Model.TaxonomyContentItem` | The `TaxonomyContentItem`. |
| `Model.TermContentItem` | The `TermContentItem` for this item. |
| `Model.Level` | The level of the term item. `0` for top level term items. |
| `Model.Items` | The list of sub term items shapes. These are shapes of type `TermItem`. |
| `Model.Terms` | The list of term content items for the lower items in the hierarchy. |
| `Model.Differentiator` | If defined, contains the formatted name of the taxonomy. For instance `Categories`. |

!!! note
    When rendering a partial hierarchy of terms using the `TermContentItemId` property, the level is always based of the taxonomy root.

#### TermItem Alternates

| Definition | Template | Filename|
| ---------- | --------- | ------------ |
| `TermItem__level__[level]` | `TermItem__level__2` | `TermItem-level-2.cshtml` |
| `TermItem__[ContentType]` | `TermItem__Category` | `TermItem-Category.cshtml` |
| `TermItem__[ContentType]__level__[level]` | `TermItem__Category__level__2` | `TermItem-Category-level-2.cshtml` |
| `TermItem__[Differentiator]` | `TermItem__Categories` | `TermItem-Categories.cshtml` |
| `TermItem__[Differentiator]__level__[level]` | `TermItem__Categories__level__2` | `TermItem-Categories-level-2.cshtml` |
| `TermItem__[Differentiator]__[ContentType]` | `TermItem__Categories__Category` | `TermItem-Categories-Category.cshtml` |
| `TermItem__[Differentiator]__[ContentType]__level__[level]` | `TermItem__Categories__ContentType__level__2` | `TermItem-Categories-Category-level-2.cshtml` |

#### TermItem Example

=== "Liquid"

    ``` liquid
    <li class="list-group-item border-0 pb-0">
        {% shape_clear_alternates Model %}
        {% shape_type Model "TermContentItem" %}
        {{ Model | shape_render }}
        {% if Model.HasItems %}
            <ul class="list-group list-group-flush">
                {% for item in Model.Items %}
                    {% shape_add_classes item "list-group-item border-0 pb-0" %}
                    {{ item | shape_render }}
                {% endfor %}
            </ul>
        {% endif %}
    </li>
    ```

=== "Razor"

    ``` html
    @model dynamic
    @{
        // Morphing the shape to keep Model untouched
        Model.Metadata.Alternates.Clear();
        Model.Metadata.Type = "TermContentItem";
    }
    <li>
        @await DisplayAsync(Model)
        @if ((bool)Model.HasItems)
        {
            <ul>
                @foreach (var item in Model.Items)
                {
                    @await DisplayAsync(item)
                }
            </ul>
        }
    </li>
    ```

### TermContentItem

The `TermContentItem` shape is used to render the term content item.
This shape is created by morphing a `TermItem` shape into a `TermContentItem`. Hence all the properties
available on the `TermItem` shape are still available.

| Property | Description |
| --------- | ------------ |
| `Model.Term` | The `Term` shape owning this item. |
| `Model.TaxonomyContentItem` | The `TaxonomyContentItem`. |
| `Model.TermContentItem` | The `TermContentItem` for this item. |
| `Model.Level` | The level of the term item. `0` for top level term items. |
| `Model.Items` | The list of sub term items shapes. These are shapes of type `TermItem`. |
| `Model.Terms` | The list of term content items for the lower items in the hierarchy. |
| `Model.Differentiator` | If defined, contains the formatted name of the term. For instance `Travel`. |

#### TermContentItem Alternates

| Definition | Template | Filename|
| ---------- | --------- | ------------ |
| `TermContentItem__level__[level]` | `TermContentItem__level__2` | `TermContentItem-level-2.cshtml` |
| `TermContentItem__[ContentType]` | `TermContentItem__Category` | `TermContentItem-Category.cshtml` |
| `TermContentItem__[ContentType]__level__[level]` | `TermContentItem__Category__level__2` | `TermContentItem-Category-level-2.cshtml` |
| `TermContentItem__[Differentiator]` | `TermContentItem__Categories` | `TermContentItem-Categories.cshtml` |
| `TermContentItem__[Differentiator]__level__[level]` | `TermContentItem__Categories__level__2` | `TermContentItem-Categories-level-2.cshtml` |
| `TermContentItem__[Differentiator]__[ContentType]` | `TermContentItem__Categories__Category` | `TermContentItem-Categories-Category.cshtml` |
| `TermContentItem__[Differentiator]__[ContentType]__level__[level]` | `TermContentItem__Categories__Category__level__2` | `TermContentItem-Categories-Category-level-2.cshtml` |

#### TermContentItem Example

=== "Liquid"

    ``` liquid
    {{ Model.TermContentItem | shape_build_display: "Summary" | shape_render }}
    ```

=== "Razor"

    ``` html
    @model dynamic

    @await Orchard.DisplayAsync(Model.TermContentItem as ContentItem, "Summary")
    ```

### TaxonomyField

This shape is rendered when a `TaxonomyField` is attached to a content part.
The shape base class is of type `DisplayTaxonomyFieldViewModel`.

The following properties are available on the `TaxonomyField` class.

| Property | Type | Description |
| --------- | ---- |------------ |
| `TaxonomyContentItemId` | `string` | The Content Item id of the taxonomy associated with the field. |
| `TermContentItemIds` | `string[]` | The list of Content Item ids of the terms selected for this field. |
| `TermContentItemOrder` | `Dictionary<string, int>` | Used when ordering is enabled. For each selected term, contains the order of the categorized Content Item. |

### DisplayTaxonomyFieldViewModel

This class is used when displaying a field.

The following properties are available on the `DisplayTaxonomyFieldViewModel` class.

| Property | Type | Description |
| --------- | ---- |------------ |
| `Field` | `TaxonomyField` | The `TaxonomyField` instance|
| `Part` | `ContentPart` | The part this field attached to |
| `PartFieldDefinition` | `ContentPartFieldDefinition` | The part field definition |

## Orchard Helpers

### GetTaxonomyTermAsync

Returns a term from its content item id and taxonomy.

```csharp
@foreach(var termId in Model.TermContentItemIds)
{
    @await Orchard.GetTaxonomyTermAsync(Model.TaxonomyContentItemId, termId);
}
```

### GetInheritedTermsAsync

Returns the list of terms including their parents.

```csharp
@foreach(var termId in Model.TermContentItemIds)
{
    <div>
    @foreach(var parent in await Orchard.GetInheritedTermsAsync(Model.TaxonomyContentItemId, termId))
    {
        @parent
    }
    </div>
}
```

### QueryCategorizedContentItemsAsync

Provides a way to query content items that are categorized with specific terms.

#### QueryCategorizedContentItemsAsync Example 

The following example queries content items that are related to the current content item 
by the term category, but excluding the current content item.

```csharp
@using YesSql.Services;
@{
    var termContentItemIds = Model.TermContentItemIds;
    var contentItems = await Orchard.QueryCategorizedContentItemsAsync(
        query => query.Where(index => index.TermContentItemId.IsIn(termContentItemIds) &&
            index.ContentItemId != Model.Part.ContentItem.ContentItemId));

    foreach(var contentItem in contentItems)
    {
        ...
    }
}
```

## Liquid Tags

### taxonomy_terms

The `taxonomy_terms` filter loads the specified term content items.

#### taxonomy_terms Example 

The following example lists all the terms related to the **Colors** field on the **BlogPost**
content type, then renders them.

```liquid
{% assign colors = Model.ContentItem.Content.BlogPost.Colors | taxonomy_terms %}
{% for c in colors %}
  {{ c }}
{% endfor %}
```

The `taxonomy_terms` also accepts term content item ids as input, as long as the first
argument is a taxonomy content item id.

#### Example

The following example displays all the colors and their hierarchy:

```liquid
{% assign taxonomyId = Model.ContentItem.Content.BlogPost.Colors.TaxonomyContentItemId %}

{% for colorId in Model.ContentItem.Content.BlogPost.Colors.TermContentItemIds %}
  <div>
    {% assign parentColors = colorId | inherited_terms: taxonomyId %}
    {% for c in  parentColors %}
      {{ c }}
    {% endfor %}
  </div>
{% endfor %}
```

### inherited_terms

The `inherited_terms` filter loads all the parents of a given term.  
The input must be a term content item or content item id.  
The first argument must be the taxonomy content item or content item id.

## Taxonomy Index

The `TaxonomyIndex` SQL table contains a list of all content items that are associated with a Taxonomy field.  
Each record corresponds to a selected term for a field.

| Column | Type | Description |
| --------- | ---- |------------ |
| TaxonomyContentItemId | `string` | The content item id of the Taxonomy |
| ContentItemId | `string` | The content item id of the categorized content |
| ContentType | `string` | The content type of the categorized content |
| ContentPart | `string` | The content part containing the field |
| ContentField | `string` | The name of the field in the content part |
| TermContentItemId | `string` | The content item id of the categorized Term |
| Order | `int` | The order of the categorized content |

For instance if a field has two selected terms, there will be two records with all identical column values except for the `TermContentItemId` and the `Order`.

## Tags

Tags are a editor and display mode option for taxonomies to allow tagging of content items while editing.

When using the `Tags` mode the display text property of the tag is stored as well as the `TermContentItemId`.

You can access the `TagNames` property directly with the following accessor:

=== "Liquid"

    ``` liquid
    {% for tagName in Model.ContentItem.Content.BlogPost.Tags.TagNames %}
        <span class="badge bg-secondary">
            <i class="fas fa-tag fa-xs fa-rotate-90 align-middle"></i>
            <span class="align-middle"> {{ tagName }} </span> 
        </span>
    {% endfor %}
    ```

=== "Razor"

    ``` html
    @foreach (var tagName in Model.ContentItem.Content.BlogPost.Tags.TagNames)
    {
        <span class="taxonomy-tag-term">@tagName</span>
    }
    ```

!!! note
    If the display text property of the term is updated any content items will need to be republished to reflect this change.

## Taxonomies Contents List Filters

Provides taxonomy filters in the admin contents list

## Manual Ordering of Categorized Content Items

For each taxonomy item, you can enable drag&drop ordering of content items categorized with its terms. This option is disabled by default.

#### To enable ordering:

1. Edit the taxonomy content item.
2. Select “Enable Ordering”.
3. Save (publish) the taxonomy.

#### To modify the order of categorized content items for a term:

1. Edit the taxonomy content item.
2. Click “List Items”, to the right of the `Term` whose categorized content items you want to order.
3. Drag&Drop the categorized content items to the desired positions (grabbing them by the handler icon). There is no need to save.

Classifying a content item with a term will make the content item appear at the first position, on the term's list of categorized content items. Cloned and imported content items will also appear at the top.

## Videos

<iframe width="560" height="315" src="https://www.youtube.com/embed/DpaN02c2sDI" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

<iframe width="560" height="315" src="https://www.youtube.com/embed/nyPgQMwizbU" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

<iframe width="560" height="315" src="https://www.youtube.com/embed/G9lkGRD9G_E" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

<iframe width="560" height="315" src="https://www.youtube.com/embed/NVjRz5ru7N4" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
