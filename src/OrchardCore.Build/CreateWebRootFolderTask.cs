using System;
using System.IO;
using Microsoft.Build.Utilities;

namespace OrchardCore.Build
{
    public class CreateWebRootFolderTask : Task
    {
        private const string WebRootFolderName = "wwwroot";

        public override bool Execute()
        {
            var placeholderFileName = ".placeholder";
            var placeholderFilePath = Path.Combine(WebRootFolderName, placeholderFileName);
            if (File.Exists(placeholderFilePath))
            {
                Log.LogWarning("Web Root folder isn't created.");
            }
            else
            {
                Directory.CreateDirectory(WebRootFolderName);
                File.WriteAllText(placeholderFilePath, String.Empty);

                Log.LogWarning("Web Root folder is created.");
            }

            return true;
        }
    }
}
