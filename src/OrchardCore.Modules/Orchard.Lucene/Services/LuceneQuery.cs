using Newtonsoft.Json.Linq;
using Orchard.Queries;

namespace Orchard.Lucene
{
    public class LuceneQuery : Query
    {
        public LuceneQuery() : base("Lucene")
        {
        }

        public string Index { get; set; }
        public string Template { get; set; }
    }
}
