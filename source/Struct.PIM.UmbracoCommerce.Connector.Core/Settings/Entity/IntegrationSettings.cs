using Newtonsoft.Json;
using Struct.PIM.Api.Client;
using Struct.PIM.Api.Models.Attribute;
using System.Globalization;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Settings.Entity
{
    public class IntegrationSettings
    {
        public GeneralSettings GeneralSettings { get; set; }
        public ProductMapping ProductMapping { get; set; }
        public VariantMapping VariantMapping { get; set; }
        public CategoryMapping CategoryMapping { get; set; }
        public Setup Setup { get; set; }

        public string CurrentUmbracoCommerceVersion { get; set; }
    }

    public class ProductMapping
    {
        public string TitleAttributeUid { get; set; } = string.Empty;

        //Attribute that must be filled out to publish to Umbraco Commerce
        public string PublishingAttributeUid { get; set; } = string.Empty;

        public string IsGiftcardAttributeUid { get; set; } = string.Empty;

        public string SkuAttributeUid { get; set; } = string.Empty;

        public string ImageAttributeUid { get; set; } = string.Empty;

        public List<string> PropertyAttributeUids { get; set; } = new List<string>();
        public List<string> PropertyScopes { get; set; } = new List<string>();

        public string TaxClassAttributeUid { get; set; } = string.Empty;
        //Attributes to search in
        public List<string> SearchableAttributeUids { get; set; } = new List<string>();

    }
    public class VariantMapping
    {
        public string TitleAttributeUid { get; set; } = string.Empty;

        //Attribute that must be filled out to publish to Umbraco Commerce
        public string PublishingAttributeUid { get; set; } = string.Empty;

        public string SkuAttributeUid { get; set; } = string.Empty;
        
        public List<string> PropertyAttributeUids { get; set; } = new List<string>();
        public List<string> PropertyScopes { get; set; } = new List<string>();

        //Attributes to search in
        public List<string> SearchableAttributeUids { get; set; } = new List<string>();
    }

    public class CategoryMapping
    {
        public string TitleAttributeUid { get; set; } = string.Empty;
    }

    public class GeneralSettings
    {
        public List<StoreSettings> ShopSettings { get; set; } = new List<StoreSettings>();
    }

    public class Setup
    {
        public string PimApiUrl { get; set; }
        public string PimApiKey { get; set; }
        public string DefaultLanguage { get; set; }
    }

    public class StoreSettings
    {
        public Guid Uid { get; set; }
        public string Name { get; set; }
        public int? LanguageId { get; set; }
        public Dictionary<string, string> DimensionSettings { get; set; }
        public string StockAttributeUid { get; set; }

        public List<PriceInfo> PriceMapping { get; set; } = new List<PriceInfo>();

        public string FilterAttributeUid { get; set; }
        public List<string> FilterAttributeGlobalListValueKeys { get; set; }

        public Guid? Catalogue { get; set; }
    }

    public class PriceInfo
    {
        public Guid Uid { get; set; }
        public string Currency { get; set; }
        public Guid? PriceAttributeUid { get; set; }
    }
}
