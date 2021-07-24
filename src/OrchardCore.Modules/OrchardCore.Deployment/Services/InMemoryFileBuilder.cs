using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace OrchardCore.Deployment.Services
{
    public class InMemoryFileBuilder : IFileBuilder
    {
        public byte[] PlanInMemory { get; private set; }
        public async Task SetFileAsync(string subpath, Stream stream)
        {
            //The passed in stream is a MemoryStream which means it is a seekable stream. So the Length always available
            PlanInMemory = new byte[stream.Length];
            await stream.ReadAsync(PlanInMemory, 0, (int)stream.Length);
        }
    }
}
