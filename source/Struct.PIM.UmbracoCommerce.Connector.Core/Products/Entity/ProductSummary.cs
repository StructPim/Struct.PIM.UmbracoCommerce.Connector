﻿using Umbraco.Commerce.Core.Models;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity
{
    public class ProductSummary : IProductSummary
    {
        internal ProductSummary() { }
        public string Reference { get; set; }

        public string Sku { get; set; }

        public string Name { get; set; }

        public IEnumerable<ProductPrice> Prices { get; set; }

        public bool HasVariants { get; set; }
    }
}
