using Examine;
using Umbraco.Cms.Infrastructure.Examine;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity
{
    public class Category
    {
        public int Id { get; set; }
        public Guid StoreId { get; set; }
        public string CultureCode { get; set; }
        public string CatalogueAlias { get; set; }
        public int? ParentId { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }

        public ValueSet AsValueSet()
        {
            var indexValues = new Dictionary<string, object>
            {
                [UmbracoExamineFieldNames.NodeNameFieldName] = Name,
                ["name"] = Name,
                ["slug"] = Slug,
                ["id"] = Id,
                ["language"] = CultureCode,
                ["store"] = StoreId,
                ["parent"] = ParentId.GetValueOrDefault()
            };

            return new ValueSet($"{Id}_{StoreId}_{CultureCode}", Indexing.IndexTypes.Category, CatalogueAlias, indexValues);    
        }
    }
}
