using Umbraco.Commerce.Core.Models;

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
    }
}
