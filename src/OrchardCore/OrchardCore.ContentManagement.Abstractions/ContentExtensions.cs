using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using OrchardCore.ContentManagement.Metadata.Builders;

namespace OrchardCore.ContentManagement
{
    public static class ContentExtensions
    {
        public const string WeldedPartSettingsName = "@WeldedPartSettings";

        /// <summary>
        /// These settings instruct merge to replace current value, even for null values.
        /// </summary>
        private static readonly JsonMergeSettings JsonMergeSettings = new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace, MergeNullValueHandling = MergeNullValueHandling.Merge };

        /// <summary>
        /// Gets a content element by its name.
        /// </summary>
        /// <param name="contentElement">The <see cref="ContentElement"/>.</param>
        /// <param name="name">The name of the content element.</param>
        /// <typeparam name="TElement">The expected type of the content element.</typeparam>
        /// <returns>The content element instance or <code>null</code> if it doesn't exist.</returns>
        public static TElement Get<TElement>(this ContentElement contentElement, string name) where TElement : ContentElement
        {
            return (TElement)contentElement.Get(typeof(TElement), name);
        }

        /// <summary>
        /// Gets whether a content element has a specific element attached.
        /// </summary>
        /// <param name="contentElement">The <see cref="ContentElement"/>.</param>
        /// <typeparam name="TElement">The expected type of the content element.</typeparam>
        public static bool Has<TElement>(this ContentElement contentElement) where TElement : ContentElement
        {
            return contentElement.Has(typeof(TElement).Name);
        }

        /// <summary>
        /// Gets a content element by its name.
        /// </summary>
        /// <param name="contentElement">The <see cref="ContentElement"/>.</param>
        /// <param name="contentElementType">The expected type of the content element.</param>
        /// <param name="name">The name of the content element.</param>
        /// <returns>The content element instance or <code>null</code> if it doesn't exist.</returns>
        public static ContentElement Get(this ContentElement contentElement, Type contentElementType, string name)
        {
            if (contentElement.Elements.TryGetValue(name, out var element))
            {
                return element;
            }

            var elementData = contentElement.Data[name] as JObject;

            if (elementData == null)
            {
                return null;
            }

            var result = (ContentElement)elementData.ToObject(contentElementType);
            result.Data = elementData;
            result.ContentItem = contentElement.ContentItem;

            contentElement.Elements[name] = result;

            return result;
        }

        /// <summary>
        /// Removes a content element by its name.
        /// </summary>
        /// <param name="contentElement">The <see cref="ContentElement"/>.</param>
        /// <param name="name">The name of the content element.</param>
        /// <returns><see langword="True"/> is successfully found and removed; otherwise. <see langword="False"/> This method returns false if the name is not found.</returns>
        public static bool Remove(this ContentElement contentElement, string name)
        {
            contentElement.Elements.Remove(name);
            return contentElement.Data.Remove(name);
        }

        /// <summary>
        /// Gets a content element by its name or create a new one.
        /// </summary>
        /// <param name="contentElement">The <see cref="ContentElement"/>.</param>
        /// <param name="name">The name of the content element.</param>
        /// <typeparam name="TElement">The expected type of the content element.</typeparam>
        /// <returns>The content element instance or a new one if it doesn't exist.</returns>
        public static TElement GetOrCreate<TElement>(this ContentElement contentElement, string name) where TElement : ContentElement, new()
        {
            var existing = contentElement.Get<TElement>(name);

            if (existing == null)
            {
                var newElement = new TElement();
                newElement.ContentItem = contentElement.ContentItem;
                contentElement.Data[name] = newElement.Data;
                contentElement.Elements[name] = newElement;
                return newElement;
            }

            return existing;
        }

        /// <summary>
        /// Adds a content element by name.
        /// </summary>
        /// <param name="contentElement">The <see cref="ContentElement"/>.</param>
        /// <param name="name">The name of the content element.</param>
        /// <param name="element">The element to add to the <see cref="ContentItem"/>.</param>
        /// <returns>The current <see cref="ContentItem"/> instance.</returns>
        public static ContentElement Weld(this ContentElement contentElement, string name, ContentElement element)
        {
            if (!contentElement.Data.ContainsKey(name))
            {
                element.Data = JObject.FromObject(element);
                element.ContentItem = contentElement.ContentItem;

                contentElement.Data[name] = element.Data;
                contentElement.Elements[name] = element;
            }

            return contentElement;
        }

        /// <summary>
        /// Welds a new part to the content item. If a part of the same type is already welded nothing is done.
        /// This part can be not defined in Content Definitions.
        /// </summary>
        /// <typeparam name="TElement">The type of the part to be welded.</typeparam>
        public static ContentElement Weld<TElement>(this ContentElement contentElement, object settings = null) where TElement : ContentElement, new()
        {
            var elementName = typeof(TElement).Name;

            var elementData = contentElement.Data[elementName] as JObject;

            if (elementData == null)
            {
                // build and welded the part
                var part = new TElement();
                contentElement.Weld(elementName, part);
            }

            JToken result;
            if (!contentElement.Data.TryGetValue(WeldedPartSettingsName, out result))
            {
                contentElement.Data[WeldedPartSettingsName] = result = new JObject();
            }

            var weldedPartSettings = (JObject)result;

            weldedPartSettings[elementName] = settings == null ? new JObject() : JObject.FromObject(settings, ContentBuilderSettings.IgnoreDefaultValuesSerializer);

            return contentElement;
        }

        /// <summary>
        /// Updates the content element with the specified name.
        /// </summary>
        /// <param name="contentElement">The <see cref="ContentElement"/>.</param>
        /// <param name="name">The name of the element to update.</param>
        /// <param name="element">The content element instance to update.</param>
        /// <returns>The current <see cref="ContentItem"/> instance.</returns>
        public static ContentElement Apply(this ContentElement contentElement, string name, ContentElement element)
        {
            var elementData = contentElement.Data[name] as JObject;

            if (elementData != null)
            {
                elementData.Merge(JObject.FromObject(element), JsonMergeSettings);
            }
            else
            {
                elementData = JObject.FromObject(element);
                contentElement.Data[name] = elementData;
            }

            element.Data = elementData;
            element.ContentItem = contentElement.ContentItem;

            // Replace the existing content element with the new one
            contentElement.Elements[name] = element;

            if (element is ContentField)
            {
                contentElement.ContentItem?.Elements.Clear();
            }

            return contentElement;
        }

        /// <summary>
        /// Updates the whole content.
        /// </summary>
        /// <param name="contentElement">The <see cref="ContentElement"/>.</param>
        /// <param name="element">The content element instance to update.</param>
        /// <returns>The current <see cref="ContentItem"/> instance.</returns>
        public static ContentElement Apply(this ContentElement contentElement, ContentElement element)
        {
            if (contentElement.Data != null)
            {
                contentElement.Data.Merge(JObject.FromObject(element.Data), JsonMergeSettings);
            }
            else
            {
                contentElement.Data = JObject.FromObject(element.Data, ContentBuilderSettings.IgnoreDefaultValuesSerializer);
            }

            contentElement.Elements.Clear();
            return contentElement;
        }

        /// <summary>
        /// Modifies a new or existing content element by name.
        /// </summary>
        /// <param name="contentElement">The <see cref="ContentElement"/>.</param>
        /// <param name="name">The name of the content element to update.</param>
        /// <param name="action">An action to apply on the content element.</param>
        /// <typeparam name="TElement">The type of the part to be altered.</typeparam>
        /// <returns>The current <see cref="ContentElement"/> instance.</returns>
        public static ContentElement Alter<TElement>(this ContentElement contentElement, string name, Action<TElement> action) where TElement : ContentElement, new()
        {
            var element = contentElement.GetOrCreate<TElement>(name);
            action(element);
            contentElement.Apply(name, element);

            return contentElement;
        }

        /// <summary>
        /// Updates the content item data.
        /// </summary>
        /// <returns>The current <see cref="ContentPart"/> instance.</returns>
        public static ContentPart Apply(this ContentPart contentPart)
        {
            contentPart.ContentItem.Apply(contentPart.GetType().Name, contentPart);
            return contentPart;
        }

        /// <summary>
        /// Whether the content element is published or not.
        /// </summary>
        /// <param name="content">The content to check.</param>
        /// <returns><c>True</c> if the content is published, <c>False</c> otherwise.</returns>
        public static bool IsPublished(this IContent content)
        {
            return content.ContentItem != null && content.ContentItem.Published;
        }

        /// <summary>
        /// Whether the content element has a draft or not.
        /// </summary>
        /// <param name="content">The content to check.</param>
        /// <returns><c>True</c> if the content has a draft, <c>False</c> otherwise.</returns>
        public static bool HasDraft(this IContent content)
        {
            return content.ContentItem != null && (!content.ContentItem.Published || !content.ContentItem.Latest);
        }

        /// <summary>
        /// Gets all content elements of a specific type.
        /// </summary>
        /// <typeparam name="TElement">The expected type of the content elements.</typeparam>
        /// <returns>The content element instances or empty sequence if no entries exist.</returns>
        public static IEnumerable<TElement> OfType<TElement>(this ContentElement contentElement) where TElement : ContentElement
        {
            foreach (var part in contentElement.Elements)
            {
                var result = part.Value as TElement;

                if (result != null)
                {
                    yield return result;
                }
            }
        }
    }
}
