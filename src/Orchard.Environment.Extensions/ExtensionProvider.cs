﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Orchard.Environment.Extensions.Features;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Orchard.Environment.Extensions
{
    public class ExtensionProvider : IExtensionProvider
    {
        private readonly IFileProvider _fileProvider;
        private readonly IFeaturesProvider _featuresProvider;

        /// <summary>
        /// Initializes a new instance of a ExtensionProvider at the given root directory.
        /// </summary>
        /// <param name="hostingEnvironment">hostingEnvironment containing the fileproviders.</param>
        /// <param name="featureManager">The feature manager.</param>
        public ExtensionProvider(
            IHostingEnvironment hostingEnvironment,
            IEnumerable<IFeaturesProvider> featureProviders)
        {
            _fileProvider = hostingEnvironment.ContentRootFileProvider;
            _featuresProvider = new CompositeFeaturesProvider(featureProviders);
        }

        public double Order { get { return 100D; } }

        /// <summary>
        /// Locate an extension at the given path by directly mapping path segments to physical directories.
        /// </summary>
        /// <param name="subpath">A path under the root directory</param>
        /// <returns>The extension information. null returned if extension does not exist</returns>
        public IExtensionInfo GetExtensionInfo(IManifestInfo manifestInfo, string subPath)
        {
            var path = Path.GetDirectoryName(subPath);
            var name = Path.GetFileName(subPath);

            var extension = _fileProvider
                .GetDirectoryContents(path)
                .First(content => content.Name == name);

            return new ExtensionInfo(extension.Name, extension, subPath, manifestInfo, (mi, ei) => {
                return _featuresProvider.GetFeatures(ei, mi);
            });
        }
    }
}
