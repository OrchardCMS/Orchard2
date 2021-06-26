namespace OrchardCore.Search.Elastic.Model
{
   public class ElasticQueryModel
   {
      public string IndexName { set; get; }
      public string Query { set; get; }
      public string Parameters { set; get; }
   }
}
