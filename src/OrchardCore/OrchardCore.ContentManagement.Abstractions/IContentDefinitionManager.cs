using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using OrchardCore.ContentManagement.Metadata.Builders;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentManagement.Utilities;

namespace OrchardCore.ContentManagement.Metadata
{
    /// <summary>
    /// This interface provides each client with
    /// a different copy of <see cref="ContentTypeDefinition"/> to work with in case
    /// multiple clients do modifications.
    /// </summary>
    public interface IContentDefinitionManager
    {
        IEnumerable<ContentTypeDefinition> ListTypeDefinitions();
        IEnumerable<ContentPartDefinition> ListPartDefinitions();

        ContentTypeDefinition GetTypeDefinition(string name);
        ContentPartDefinition GetPartDefinition(string name);
        void DeleteTypeDefinition(string name);
        void DeletePartDefinition(string name);

        void StoreTypeDefinition(ContentTypeDefinition contentTypeDefinition);
        void StorePartDefinition(ContentPartDefinition contentPartDefinition);

        /// <summary>
        /// Returns a serial number representing the list of types and settings for the current tenant.
        /// </summary>
        /// <returns>
        /// An <see cref="int"/> value that changes every time the list of types changes.
        /// The implementation is efficient in order to be called frequently.
        /// </returns>
        Task<int> GetTypesHashAsync();

        IChangeToken ChangeToken { get; }
    }

    public static class ContentDefinitionManagerExtensions
    {
        public static void AlterTypeDefinition(this IContentDefinitionManager manager, string name, Action<ContentTypeDefinitionBuilder> alteration)
        {
            var typeDefinition = manager.GetTypeDefinition(name) ?? new ContentTypeDefinition(name, name.CamelFriendly());
            var builder = new ContentTypeDefinitionBuilder(typeDefinition);
            alteration(builder);
            manager.StoreTypeDefinition(builder.Build());
        }
        public static void AlterPartDefinition(this IContentDefinitionManager manager, string name, Action<ContentPartDefinitionBuilder> alteration)
        {
            var partDefinition = manager.GetPartDefinition(name) ?? new ContentPartDefinition(name);
            var builder = new ContentPartDefinitionBuilder(partDefinition);
            alteration(builder);
            manager.StorePartDefinition(builder.Build());
        }

        /// <summary>
        /// Migrate existing ContentPart settings to WithSettings<typeparamref name="TSettings"/> 
        /// This method will be removed in a future release.
        /// </summary>
        /// <typeparam name="TPart"></typeparam>
        /// <typeparam name="TSettings"></typeparam>
        /// <param name="manager"></param>
        public static void MigratePartSettings<TPart, TSettings>(this IContentDefinitionManager manager)
            where TPart : ContentPart where TSettings : class
        {
            var contentTypes = manager.ListTypeDefinitions();

            foreach (var contentType in contentTypes)
            {
                var partDefinition = contentType.Parts.FirstOrDefault(x => x.PartDefinition.Name == typeof(TPart).Name);
                if (partDefinition != null)
                {
                    var existingSettings = partDefinition.Settings.ToObject<TSettings>();

                    // Remove existing properties from JObject
                    var properties = typeof(TSettings).GetProperties();
                    foreach (var property in properties)
                    {
                        partDefinition.Settings.Remove(property.Name);
                    }

                    // Apply existing settings to type definition WithSettings<T>
                    manager.AlterTypeDefinition(contentType.Name, typeBuilder =>
                    {
                        typeBuilder.WithPart(partDefinition.Name, partBuilder =>
                        {
                            partBuilder.WithSettings(existingSettings);
                        });
                    });
                }
            }
        }

        /// <summary>
        /// Migrate existing ContentField settings to WithSettings<typeparamref name="TSettings"/> 
        /// This method will be removed in a future release.
        /// </summary>
        /// <typeparam name="TField"></typeparam>
        /// <typeparam name="TSettings"></typeparam>
        /// <param name="manager"></param>
        public static void MigrateFieldSettings<TField, TSettings>(this IContentDefinitionManager manager)
            where TField : ContentField where TSettings : class
        {
            var partDefinitions = manager.ListPartDefinitions();
            foreach (var partDefinition in partDefinitions)
            {
                manager.AlterPartDefinition(partDefinition.Name, partBuilder =>
                {
                    foreach (var fieldDefinition in partDefinition.Fields.Where(x => x.FieldDefinition.Name == typeof(TField).Name))
                    {
                        var existingFieldSettings = fieldDefinition.Settings.ToObject<TSettings>();

                        // Do this before creating builder, so settings are removed from the builder settings object.
                        // Remove existing properties from JObject
                        var fieldSettingsProperties = existingFieldSettings.GetType().GetProperties();
                        foreach (var property in fieldSettingsProperties)
                        {
                            fieldDefinition.Settings.Remove(property.Name);
                        }

                        partBuilder.WithField(fieldDefinition.Name, fieldBuilder =>
                        {
                            fieldBuilder.WithSettings(existingFieldSettings);
                        });
                    }
                });
            }
        }
    }
}
