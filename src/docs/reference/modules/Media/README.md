# Media (`OrchardCore.Media`)

The Media module provides a UI to upload and organize binary files that can be used while creating content.

The media-processing liquid filters can also create custom sized images.

## HTML filters

The following filters allow for media manipulation:

### `asset_url`

Returns the URL of a media file, based on its location in the media library.

#### Input

`{{ 'animals/kittens.jpg' | asset_url }}`

or when using your added content

`{{ Model.ContentItem.Content.YourContentType.YourMediaField.Paths.first | asset_url }}`

#### Output

`/media/animals/kittens.jpg`

### `img_tag`

Renders an `<img src />` HTML tag.

#### img_tag Input

`{{ 'animals/kittens.jpg' | asset_url | img_tag }}`

#### img_tag Output

`<img src="~/media/animals/kittens.jpg" />`

#### Options

You can add as many html attributes as you want with the img_tag.
`{{ 'animals/kittens.jpg' | asset_url | img_tag: alt: 'kittens', class: 'kittens black', data_order: some_var }}`

## Image resizing filters

### `resize_url`

Convert the input URL to create a resized image with the specified size arguments.

#### resize_url Input

`{{ 'animals/kittens.jpg' | asset_url | resize_url: width:100, height:240 | img_tag }}`

#### resize_url Output

`<img src="~/media/animals/kittens.jpg?width=100&height=240" />`

#### Arguments

Refer [Query string tokens](#query-string-tokens) to understand the valid values for a width or height command,
and when a query string must utilize a token.

!!! note
    You cannot mix named and indexed arguments. If any of the arguments is named, all arguments must be named.

#### `width` (or first argument)

The width of the new image. One of the allowed values.

#### `height` (or second argument)

The height of the new image. One of the allowed values.

#### `mode` (or third argument)

The resize mode.

##### `pad`

Pads the resized image to fit the bounds of its container.  
If only one dimension is passed, the original aspect ratio will be maintained.

##### `boxpad`

Pads the image to fit the bounds of the container without resizing the original source.  
When downscaling, performs the same functionality as `pad`.

##### `max` (Default)

Constrains the resized image to fit the bounds of its container maintaining the original aspect ratio.

##### `min`

Resizes the image until the shortest side reaches the given dimension. Upscaling is disabled in this mode and the original image will be returned if attempted.

##### `stretch`

Stretches the resized image to fit the bounds of its container.

##### `crop`

Resizes the image using the same functionality as `max` then removes any image area falling outside the bounds of its container.

#### mode Input

`{{ 'animals/kittens.jpg' | asset_url | resize_url: width:100, height:240, mode:'crop' }}`

#### mode Output

`<img src="~/media/animals/kittens.jpg?width=100&height=240&rmode=crop" />`

#### `quality` (or fourth argument)

The quality used when compressing the image.

!!! note
    The quality argument is only supported for `JPG` images, but can be combined with the `format` argument to convert to `JPG`

#### `format` (or fifth argument)

The image format to use when processing the ouput of an image.

Supported formats include `bmp`, `gif`, `jpg`, `png`, `tga`.

Can be combined with the `quality` argument to convert an image to a `JPG` and reduce the quality.

#### quality/format Input

`{{ 'animals/kittens.jpg' | asset_url | resize_url: width:100, height:240, mode:'crop', quality: 50, format:'jpg' }}`

#### quality/format Output

`<img src="~/media/animals/kittens.jpg?width=100&height=240&rmode=crop&quality=50&format=jpg" />`

### `anchor` (or sixth argument)

The anchor of the new image.

#### anchor Input

```
{% assign anchor = Model.ContentItem.Content.Blog.Image.Anchors.first %}
{{ 'animals/kittens.jpg' | asset_url | resize_url: width:100, height:240, mode:'crop', anchor:anchor }}
```

#### anchor Output

`<img src="~/media/animals/kittens.jpg?width=100&height=240&rmode=crop&rxy=0.5,0.5" />`

### `bgcolor` (or seventh argument)

The background color of the new image when `mode` is `pad` or `boxpad`. Examples of valid values: `white`, `ffff00`, `ffff0080`, `128,64,32` and `128,64,32,16`.

#### bgcolor Input

```
{{ 'animals/kittens.jpg' | asset_url | resize_url: width:100, height:240, mode:'pad', bgcolor:'white' }}
```

#### bgcolor Output

`<img src="~/media/animals/kittens.jpg?width=100&height=240&rmode=pad&bgcolor=white" />`

### `profile` (named argument)

A [Media Profile](#media-profiles) can be specified as a named argument to provide preset formatting commands.

#### `profile` Input

`{{ 'animals/kittens.jpg' | asset_url | resize_url: profile : 'medium' }}`

#### `profile` Output

`<img src="~/media/animals/kittens.jpg?width=240&height=240" />`

### `append_version`

Appends a version hash for an asset. Can be piped together with the other media filters.

#### version Input

`{{ 'animals/kittens.jpg' | asset_url | append_version | img_tag }}`

#### version Output

`<img src="~/media/animals/kittens.jpg?v=Ailxbj_jQtYc9LRXKa21DygRzmQqc3OfN1XxSaQ3UWE" />`

## Razor Helpers

To obtain the correct URL for an asset, use the `AssetUrl` helper extension method on the view's base `Orchard` property, e.g.:

`@Orchard.AssetUrl(Model.Paths[0])`

To obtain the correct URL for a resized asset use `AssetUrl` with the optional width, height and resizeMode parameters, e.g.:

`@Orchard.AssetUrl(Model.Paths[0], width: 100 , height: 240, resizeMode: ResizeMode.Crop)`

To obtain the correct URL for a resized asset use `AssetUrl` with the optional width, height, resizeMode, quality and format parameters, e.g.:

`@Orchard.AssetUrl(Model.Paths[0], width: 100 , height: 240, resizeMode: ResizeMode.Crop, quality: 50, format: Format.Jpg)`

To obtain the correct URL for a resized asset use `AssetUrl` with the optional width, height, resizeMode and bgcolor, e.g.:

`@Orchard.AssetUrl(Model.Paths[0], width: 100 , height: 240, resizeMode: ResizeMode.Pad, bgcolor: "white")`

To append a version hash for an asset use `AssetUrl` with the append version parameter, e.g.:

`@Orchard.AssetUrl(Model.Paths[0], appendVersion: true)`

or with resizing options as well, noting that the version hash is based on the source image

`@Orchard.AssetUrl(Model.Paths[0], width: 100 , height: 240, resizeMode: ResizeMode.Crop, appendVersion: true)`

To use a [Media Profile](#media-profiles), use the `AssetProfileUrlAsync` helper extension method on the view's base `Orchard` property, e.g.:

`@await Orchard.AssetProfileUrlAsync(Model.Paths[0], "medium")`

To use [Image Anchors](#image-anchors), use the `GetAnchors` helper extension method on the media field, e.g.:

`@await Orchard.AssetUrl(Model.Paths[0], , width: 100 , height: 240, resizeMode: ResizeMode.Crop, @Model.Field.GetAnchors()[0])`

### Razor image resizing tag helpers

To use the image tag helpers add `@addTagHelper *, OrchardCore.Media` to `_ViewImports.cshtml`, and take a direct reference to the `OrchardCore.Media` nuget package.

`asset-src` is used to obtain the correct URL for the asset and set the `src` attribute. Width, height, resize mode, quality and format can be set using `img-width`, `img-height`, `img-resize-mode`, `img-quality`, and `img-format` respectively. e.g.:

`<img asset-src="Model.Paths[0]" alt="..." img-width="100" img-height="240" img-resize-mode="Crop" img-quality="50" img-format="Jpg" />`

Alternatively the Asset Url can be resolved independently and the `src` attribute used:

`<img src="@Orchard.AssetUrl(Model.Paths[0])" alt="..." img-width="100" img-height="240" img-resize-mode="Crop" img-quality="50" img-format="Jpg" />`

When resize mode is `pad` or `boxpad`, the background color can be set using `img-bgcolor`, e.g.:

`<img asset-src="Model.Paths[0]" alt="..." img-width="100" img-height="240" img-resize-mode="Pad" img-bgcolor="white" />`

To use a [Media Profile](#media-profiles) set the `asset-src` property and the `img-profile` attribute.

`<img asset-src="Model.Paths[0]" alt="..." img-profile="medium" />`

You can optionally include more formatting information, or override the profiles properties.

`<img asset-src="Model.Paths[0]" alt="..." img-profile="medium" img-quality="50" img-format="Jpg" />`

To use a [Media Text](#media-text) set the `alt` attribute.

`<img asset-src="Model.Paths[0]" alt="@Model.MediaTexts[0]" />`

To use a [Image Anchor](#image-anchors) set the `asset-src` property and the `img-anchor` attribute.

`<img asset-src="Model.Paths[0]" alt="..." img-width="100" img-height="240" img-profile="medium" img-anchor="@Model.GetAnchors()[0]" />`

### Razor append version

`asp-append-version` support is available on the OrchardCore tag helpers and MVC tag helpers.

`<img asset-src="Model.Paths[0]" alt="..." asp-append-version="true" />`

Alternatively the Asset Url can be resolved independently and the `src` attribute used:

`<img src="@Orchard.AssetUrl(Model.Paths[0])" alt="..." asp-append-version="true" />`

Or when using the MVC tag helpers and the image is resolved from static assets, i.e. wwwroot

`<img src="/favicon.ico" asp-append-version="true"/>`

> The Razor Helper is accessible on the `Orchard` property if the view is using Orchard Core's Razor base class, or by injecting `OrchardCore.IOrchardHelper` in all other cases.

!!! note
    When using tag helpers in Razor, you must take a direct reference to the `OrchardCore.Media` nuget package in each theme or module that uses the tag helpers. This is not required when using Liquid.

## Deployment Step Editor

Keep these things in mind when working with the deployment step editor:

- Selecting "Include all media." will ensure that all media is added to the package when this deployment plan executes, regardless of what you see here now.
- Selecting a file will ensure that only that file is added to the package when this deployment plan executes, regardless of what you see here now.
- Selecting a directory will ensure that all the files in that directory at the time this deployment plan executes, are added to the package during execution, regardless of what you see here now.
- Selecting all files in a directory will ensure that only those files are added to the package when this deployment plan executes, even if at that time, that directory has more files than what you see here now.

## Configuration

The following configuration values are used by default and can be customized:

```json
    "OrchardCore_Media": {

      // The accepted sizes for custom width and height.
      // When the 'UseTokenizedQueryString' is True (default) all sizes are valid.
      "SupportedSizes": [ 16, 32, 50, 100, 160, 240, 480, 600, 1024, 2048 ],

      // The number of days to store images in the browser cache.
      // NB: To control cache headers for module static assets, refer to the Orchard Core Modules Section.
      "MaxBrowserCacheDays": 30,

      // The number of days a cached resized media item will be valid for, before being rebuilt on request.
      "MaxCacheDays": 365,

      // The maximum size of an uploaded file in bytes. 
      // NB: You might still need to configure the limit in IIS (https://docs.microsoft.com/en-us/iis/configuration/system.webserver/security/requestfiltering/requestlimits/)
      "MaxFileSize": 30000000,

      // A CDN base url that will be prefixed to the request path when serving images.
      "CdnBaseUrl": "https://your-cdn.com",

      // The path used when serving media assets.
      "AssetsRequestPath": "/media",

      // The path used to store media assets. The path can be relative to the tenant's App_Data folder, or absolute.
      "AssetsPath": "Media",

      // Whether to use a token in the query string to prevent disc filling.
      "UseTokenizedQueryString": true,

      // The list of allowed file extensions
      "AllowedFileExtensions": [

            // Images
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".ico",
            ".svg",

            // Documents
            ".pdf", // Portable Document Format; Adobe Acrobat
            ".doc", // Microsoft Word Document
            ".docx",
            ".ppt", // Microsoft PowerPoint Presentation
            ".pptx",
            ".pps",
            ".ppsx",
            ".odt", // OpenDocument Text Document
            ".xls", // Microsoft Excel Document
            ".xlsx",
            ".psd", // Adobe Photoshop Document

            // Audio
            ".mp3",
            ".m4a",
            ".ogg",
            ".wav",

            // Video
            ".mp4", // MPEG-4
            ".m4v",
            ".mov", // QuickTime
            ".wmv", // Windows Media Video
            ".avi",
            ".mpg",
            ".ogv", // Ogg
            ".3gp", // 3GPP
        ],

      // The Content Security Policy to apply to assets served from the media library.
      "ContentSecurityPolicy" : "default-src 'self'; style-src 'unsafe-inline'"
    }
```

To configure the `StaticFileOptions` in more detail, including event handlers, for the Media Library `StaticFileMiddleware` apply:

```
services.PostConfigure<MediaOptions>(o => ...);
```

To configure the `ImageSharpMiddleware` in more detail, including event handlers, apply:

```
services.PostConfigure<ImageSharpMiddlewareOptions>(o => ...);
```

!!! note
    The Media Library `StaticFileOptions` configuration is separated from the configuration for static files contained in module `wwwroot` folders.

To configure `wwwroot` static file options apply:

```
services.Configure<StaticFileOptions>(o => ...);
```

## Media Profiles

Media profiles allow you to defined preset image resizing and formatting commands.

You can create a media profile from the _Configuration -> Media -> Media Profiles_ menu.

When specifying a media profile with either the liquid, razor helper, or tag helper you provide the profile name, and any additional commands which you want to apply to the media item.

=== "Liquid"

    ``` liquid
    {% resize_url profile: 'medium' %}
    {% resize_url profile: 'medium', mode: 'crop' %}
    ```

=== "Razor"

    ``` html
    @await Orchard.AssetProfileUrlAsync(Model.Paths[0], "medium");
    @await Orchard.AssetProfileUrlAsync(Model.Paths[0], "medium", resizeMode: ResizeMode.Crop);
    ```

=== "Tag"

    ``` html
    <img asset-src="Model.Paths[0]" img-profile="medium" />
    <img asset-src="Model.Paths[0]" img-profile="medium" img-resize-mode="Crop"/>
    ```

!!! note
    Media Profiles are only available from the [Preview Feed](../../../getting-started/preview-package-source)

## Media Text

Media text is an optional setting, on by default, on the `MediaField`.

When provided it allows the editor of the field to include a text value for each selected media item.

This can be used for the `alt` tag of an image.

When the setting is enabled the template must read and provide the value to the `img` tag.

The `MediaTexts[]` is kept in sync with the `Paths[]` array and the index for a given path represents the index of a `MediaText` value.

## Image Anchors

Image anchors are an optional setting, off by default, on the `MediaField`.

When enabled they allow a media field to provide an anchor point, or x and y value for use when cropping, or padding the image.

The anchor value provided can be used to specify the center point of a crop or pad.

When the setting is enabled the template must read and provide the value to the resizing helpers or filters.

The `Anchors[]` is a less well known property of a `MediaField` and can be accessed via the `GetAnchors()` extension, or directly.

=== "Liquid"

    ``` liquid
    {% assign anchor = Model.ContentItem.Content.Blog.Image.Anchors.first %}
    ```

=== "Razor"

    ``` html
    var anchors = @Model.Field.GetAnchors();
    var anchors = (Anchor[])Model.ContentItem.Content.Blog.Image.Anchors.ToObject<Anchor[]>();
    ```

The `Anchors[]` is kept in sync with the `Paths[]` array and the index for a given path represents the index of a `Anchor` value.

!!! note
    Anchors are only available from the [Preview Feed](../../../getting-started/preview-package-source)

## Query string tokens

When resizing images, the query string command values are, by default, signed with an HMAC signature that is unique to the tenant.

This prevents prevent malicious clients from creating too many variations of the same image. 

If the `UseTokenizedQueryString` is set to `false` the following features will be removed.

- Cache busting, or query string versioning.
- Anchors.
- The width or height must match a value from the `SupportedSizes` configuration.
- Background color.

When the query string is signed with a token any width, height value may be used.

`<img src="/media/kittens.jpg?width=101&height=241&token=0J3hyv6jIPEsSdlvTCrf30fIdygkpmrF6mphqgYQyas%3D">`

!!! note
    Tokens are only available from the [Preview Feed](../../../getting-started/preview-package-source)
    Prior to this the width or height values are limited to `16`, `32`, `50`, `100`, `160`, `240`, `480`, `600`, `1024`, `2048`.

## Media Indexing

Media can be optionally indexed for search as well if files are referenced via Media Fields. The following data can be indexed for each file referenced from a Media Field:

- Media Text
- Textual content of PDF files

!!! note
    Standalone files, i.e. files that are just uploaded to the Media Library but never referenced from a content item via a Media Field, can't be indexed.

!!! note
    You need an indexing implementation enabled for Media Indexing and search to work. The below guide assumes you're using [Lucene](../Lucene/README.md).

To set up indexing for Media do the following:

1. For each Media Field open the field's editor from under the given content type's or content part's editor, and tick "Include this element in the index".
2. When a content item of that type is published next time, Media content will be indexed as well. Check the name of the new Lucene field. You can do this by running a Lucene query for the given content type (can be done from the admin from under Search, Run Lucene Query): You should be able to see a new field named with the pattern "ContentPart.FieldTechnicalName", e.g. "BlogPost.Image".
3. Configure the new field to be used for search. You can do this from the admin under Search, Settings, Search, and adding the name of the new field under "Default search fields" (arriving at something like "Content.ContentItem.FullText, BlogPost.Image").
4. Try searching for content only available in the Media Text of selected media files, or referenced PDF files. You should see corresponding results.

## Videos

<iframe width="560" height="315" src="https://www.youtube.com/embed/BQHUlvPFRR4" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

<iframe width="560" height="315" src="https://www.youtube.com/embed/K0_i4vj00yM" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

<iframe width="560" height="315" src="https://www.youtube-nocookie.com/embed/bDxL2LPJPzk" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

## Credits

To index PDF files the [PdfPig library](https://github.com/UglyToad/PdfPig/) is used.

