using StackExchange.Profiling.Internal;
using Struct.PIM.Api.Models.Language;
using Struct.PIM.Api.Models.Shared;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Helpers;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings.Entity;
using Umbraco.Cms.Core.Strings;
using Umbraco.Commerce.Core.Models;
using AttributeValue = Umbraco.Commerce.Core.Models.AttributeValue;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Services
{
    public class ProductService
    {
        private readonly SettingsFacade _settingsFacade;
        private readonly PimApiHelper _pimApiHelper;
        private readonly PimAttributeHelper _pimAttributeHelper;
        private readonly IShortStringHelper _shortStringHelper;


        public ProductService(SettingsFacade settingsFacade, IShortStringHelper shortStringHelper)
        {
            _settingsFacade = settingsFacade;
            _shortStringHelper = shortStringHelper;

            _pimApiHelper = new PimApiHelper(settingsFacade);
            _pimAttributeHelper = new PimAttributeHelper(_pimApiHelper);
        }

        public List<int> GetProductIds()
        {
            return _pimApiHelper.GetProductIds();
        }

        public List<int> GetVariantIds()
        {
            return _pimApiHelper.GetVariantIds();
        }

        public LanguageModel GetLanguage(string cultureCode)
        {
            return _pimApiHelper.GetLanguage(cultureCode);
        }

        public IEnumerable<global::Umbraco.Commerce.Core.Models.Attribute> GetProductVariantAttributes(Guid storeId, string productReference, string languageIsoCode)
        {
            var result = new List<global::Umbraco.Commerce.Core.Models.Attribute>();
            if (!int.TryParse(productReference, out var productId))
                throw new InvalidCastException("productReference must be integer");

            var integrationSettings = _settingsFacade.GetIntegrationSettings();
            var storeSetting = integrationSettings.GeneralSettings?.ShopSettings?.Where(s => s.Uid == storeId).FirstOrDefault();
            var language = _pimApiHelper.GetLanguage(languageIsoCode);
            var dimensionSegmentData = _pimApiHelper.GetDimensionSegmentData(storeSetting);

            var variantAttributes = GetVariantAttributes(new List<int> { productId }, integrationSettings, storeSetting);
            var product = _pimApiHelper.GetProduct(productId);

            if (variantAttributes.VariationDefinitionAttributes?.Any() ?? false && product.VariationDefinitionUid.HasValue)
            {
                var definingAttributes = _pimApiHelper.GetAttributes(variantAttributes.VariationDefinitionAttributes[product.VariationDefinitionUid.Value]);
                var variantValues = _pimApiHelper.GetVariantsAttributeValuesByProductId(productId, variantAttributes.VariationDefinitionAttributes[product.VariationDefinitionUid.Value], new List<string> { language.CultureCode });
                foreach (var definingAttribute in definingAttributes)
                {
                    var values = new Dictionary<string, AttributeValue>();
                    foreach (var variantValue in variantValues)
                    {
                        string renderValue = _pimAttributeHelper.RenderRootAttribute(definingAttribute, variantValue.Values, language, dimensionSegmentData);

                        if (!values.ContainsKey(renderValue.Trim()))
                            values.Add(renderValue.Trim(), new AttributeValue(variantValue.VariantId.ToString(), renderValue.Trim()));
                        else
                            values[renderValue.Trim()].Alias += ";" + variantValue.VariantId;
                    }
                    result.Add(new global::Umbraco.Commerce.Core.Models.Attribute(definingAttribute.Alias, definingAttribute.BackofficeName, values.Values));
                }                
            }

            return result;
        }

        public EntityAttributes GetProductAttributes(IntegrationSettings integrationSettings, StoreSettings? storeSetting = null)
        {
            var productAttributes = new EntityAttributes();

            if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.TitleAttributeUid))
            {
                var attributeUids = integrationSettings.ProductMapping.TitleAttributeUid.Split(".");
                productAttributes.AttributeUids.Add(Guid.Parse(attributeUids[0]));
            }
            if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.SkuAttributeUid))
            {
                var attributeUids = integrationSettings.ProductMapping.SkuAttributeUid.Split(".");
                productAttributes.AttributeUids.Add(Guid.Parse(attributeUids[0]));
            }
            if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.ImageAttributeUid))
            {
                var attributeUids = integrationSettings.ProductMapping.ImageAttributeUid.Split(".");
                productAttributes.AttributeUids.Add(Guid.Parse(attributeUids[0]));
            }
            if (integrationSettings.ProductMapping?.PropertyAttributeUids != null)
            {
                foreach (var attribute in integrationSettings.ProductMapping.PropertyAttributeUids)
                {
                    if (!string.IsNullOrEmpty(attribute))
                    {
                        var attributeUids = attribute.Split(".");
                        var attributeUid = Guid.Parse(attributeUids[0]);
                        productAttributes.AttributeUids.Add(attributeUid);
                        productAttributes.PropertyAttributeUids.Add(attribute);
                    }
                }
            }
            if (integrationSettings.ProductMapping?.SearchableAttributeUids != null)
            {
                foreach (var attribute in integrationSettings.ProductMapping.SearchableAttributeUids)
                {
                    if (!string.IsNullOrEmpty(attribute))
                    {
                        var attributeUids = attribute.Split(".");
                        var attributeUid = Guid.Parse(attributeUids[0]);
                        productAttributes.AttributeUids.Add(attributeUid);
                        productAttributes.SearchableAttributeUids.Add(attribute);
                    }
                }
            }
            if (integrationSettings.ProductMapping?.PropertyScopes != null)
            {
                var attributeScopeUids = integrationSettings.ProductMapping.PropertyScopes.Select(Guid.Parse);
                var attributeUids = _pimApiHelper.GetAttributeUidsFromScopeUids(attributeScopeUids);
                productAttributes.PropertyAttributeUids.AddRange(attributeUids.Select(x => x.ToString()));
                productAttributes.AttributeUids.AddRange(attributeUids);
            }
            if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.IsGiftcardAttributeUid))
            {
                var attributeUids = integrationSettings.ProductMapping.IsGiftcardAttributeUid.Split(".");
                productAttributes.AttributeUids.Add(Guid.Parse(attributeUids[0]));
            }

            if (storeSetting != null)
            {
                if (storeSetting.PriceMapping != null)
                {
                    foreach (var priceMapping in storeSetting.PriceMapping)
                    {
                        if (priceMapping.PriceAttributeUid.HasValue)
                            productAttributes.AttributeUids.Add(priceMapping.PriceAttributeUid.Value);
                    }
                }
            }
            else
            {
                foreach (var store in integrationSettings.GeneralSettings.ShopSettings)
                {
                    if (store.PriceMapping != null)
                    {
                        foreach (var priceMapping in store.PriceMapping)
                        {
                            if (priceMapping.PriceAttributeUid.HasValue)
                                productAttributes.AttributeUids.Add(priceMapping.PriceAttributeUid.Value);
                        }
                    }
                }
            }
            return productAttributes;
        }

        public EntityAttributes GetVariantAttributes(List<int> productIds, IntegrationSettings integrationSettings, StoreSettings? storeSetting = null)
        {
            var variantAttributes = new EntityAttributes();
            if (!string.IsNullOrEmpty(integrationSettings.VariantMapping?.TitleAttributeUid))
            {
                var attributeUids = integrationSettings.VariantMapping.TitleAttributeUid.Split(".");
                variantAttributes.AttributeUids.Add(Guid.Parse(attributeUids[0]));
            }
            if (!string.IsNullOrEmpty(integrationSettings.VariantMapping?.SkuAttributeUid))
            {
                var attributeUids = integrationSettings.VariantMapping.SkuAttributeUid.Split(".");
                variantAttributes.AttributeUids.Add(Guid.Parse(attributeUids[0]));
            }

            variantAttributes.VariationDefinitionAttributes = _pimApiHelper.GetVariationDefinitionAttributes(productIds);

            if (variantAttributes.VariationDefinitionAttributes.Any())
            {
                variantAttributes.AttributeUids.AddRange(variantAttributes.VariationDefinitionAttributes.Values.SelectMany(x => x).Distinct());
            }
            if (integrationSettings.VariantMapping?.PropertyAttributeUids != null)
            {
                foreach (var attribute in integrationSettings.VariantMapping.PropertyAttributeUids)
                {
                    if (!string.IsNullOrEmpty(attribute))
                    {
                        var attributeUids = attribute.Split(".");
                        var attributeUid = Guid.Parse(attributeUids[0]);
                        variantAttributes.AttributeUids.Add(attributeUid);
                        variantAttributes.PropertyAttributeUids.Add(attribute);
                    }
                }
            }
            if (integrationSettings.VariantMapping?.SearchableAttributeUids != null)
            {
                foreach (var attribute in integrationSettings.VariantMapping.SearchableAttributeUids)
                {
                    if (!string.IsNullOrEmpty(attribute))
                    {
                        var attributeUids = attribute.Split(".");
                        var attributeUid = Guid.Parse(attributeUids[0]);
                        variantAttributes.AttributeUids.Add(attributeUid);
                        variantAttributes.SearchableAttributeUids.Add(attribute);
                    }
                }
            }
            if (integrationSettings.VariantMapping?.PropertyScopes != null)
            {
                var attributeScopeUids = integrationSettings.VariantMapping.PropertyScopes.Select(Guid.Parse);
                var attributeUids = _pimApiHelper.GetAttributeUidsFromScopeUids(attributeScopeUids);
                variantAttributes.PropertyAttributeUids.AddRange(attributeUids.Select(x => x.ToString()));
                variantAttributes.AttributeUids.AddRange(attributeUids);
            }

            if (storeSetting != null)
            {
                if (storeSetting.PriceMapping != null)
                {
                    foreach (var priceMapping in storeSetting.PriceMapping)
                    {
                        if (priceMapping.PriceAttributeUid.HasValue)
                            variantAttributes.AttributeUids.Add(priceMapping.PriceAttributeUid.Value);
                    }
                }
            }
            else
            {
                foreach (var store in integrationSettings.GeneralSettings.ShopSettings)
                {
                    if (store.PriceMapping != null)
                    {
                        foreach (var priceMapping in store.PriceMapping)
                        {
                            if (priceMapping.PriceAttributeUid.HasValue)
                                variantAttributes.AttributeUids.Add(priceMapping.PriceAttributeUid.Value);
                        }
                    }
                }
            }

            if (variantAttributes.AttributeUids != null)
                variantAttributes.AttributeUids = variantAttributes.AttributeUids.Distinct().ToList();

            return variantAttributes;
        }

        internal List<Product> GetProducts(List<int> productIds, Guid? storeId, int? languageId)
        {
            var integrationSettings = _settingsFacade.GetIntegrationSettings();
            var attributeInfo = GetProductAttributes(integrationSettings);
            var products = _pimApiHelper.GetProducts(productIds).ToDictionary(x => x.Id);
            var productValues = _pimApiHelper.GetProductAttributeValues(productIds, attributeInfo.AttributeUids).ToDictionary(x => x.ProductId);
            var productStructures = _pimApiHelper.GetProductStructures();
            var classifications = _pimApiHelper.GetProductClassifications(productIds);

            var items = new List<Product>();

            foreach(var storeSetting in integrationSettings.GeneralSettings.ShopSettings.Where(x => storeId == null || x.Uid == storeId.Value))
            {
                var dimensionSegmentData = _pimApiHelper.GetDimensionSegmentData(storeSetting);

                foreach (var language in _pimApiHelper.GetLanguages().Where(x => languageId == null || x.Id == languageId))
                {
                    var filteredProductIds = FilterProducts(productIds, storeSetting.Uid, language.CultureCode);

                    foreach (var productId in filteredProductIds)
                    {
                        if (productValues.TryGetValue(productId, out var productValue) && products.TryGetValue(productId, out var p))
                        {
                            var product = new Entity.Product()
                            {
                                Id = p.Id,
                                CultureCode = language.CultureCode,
                                StoreId = storeSetting.Uid,
                                ConfigurationAlias = productStructures[p.ProductStructureUid].Alias,
                                ProductReference = productId.ToString(),
                                HasVariants = products[productId].VariationDefinitionUid.HasValue
                            };

                            // map primary properties of product
                            if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.TitleAttributeUid))
                                product.Name = _pimAttributeHelper.GetStringValue(integrationSettings.ProductMapping.TitleAttributeUid, productValue.Values, language, dimensionSegmentData);
                            if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.SkuAttributeUid))
                                product.Sku = _pimAttributeHelper.GetStringValue(integrationSettings.ProductMapping.SkuAttributeUid, productValue.Values, language, dimensionSegmentData);
                            if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.IsGiftcardAttributeUid))
                                product.IsGiftCard = _pimAttributeHelper.GetBoolValue(integrationSettings.ProductMapping.IsGiftcardAttributeUid, productValue.Values, language, dimensionSegmentData).GetValueOrDefault();
                            if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.ImageAttributeUid))
                                product.PrimaryImage = _pimAttributeHelper.GetImageValue(integrationSettings.ProductMapping.ImageAttributeUid, productValue.Values, language, dimensionSegmentData);

                            // map classifications
                            if (classifications.TryGetValue(product.Id, out var productClassifications))
                                product.Categories = productClassifications.OrderBy(x => x.IsPrimary).Select(x => x.CategoryId).ToList();

                            // map slug
                            if (!string.IsNullOrEmpty(product.Name))
                                product.Slug = product.Name.ToUrlSegment(_shortStringHelper);
                            if (string.IsNullOrEmpty(product.Slug) && !string.IsNullOrEmpty(product.Sku))
                                product.Slug = product.Name.ToUrlSegment(_shortStringHelper);
                            if (string.IsNullOrEmpty(product.Slug))
                                product.Slug = product.Id.ToString().ToUrlSegment(_shortStringHelper);

                            // map properties
                            product.Properties = new Dictionary<string, string>();
                            foreach (var attribute in attributeInfo.PropertyAttributeUids)
                            {
                                if (!string.IsNullOrEmpty(attribute))
                                {
                                    var value = _pimAttributeHelper.GetValue(attribute, productValue.Values, language, dimensionSegmentData);
                                    if (value.HasValue)
                                        product.Properties.Add(value.Alias, value.Value);
                                }
                            }

                            // map searchable properties
                            var searchableProperties = new Dictionary<string, string>();
                            foreach (var attribute in attributeInfo.SearchableAttributeUids)
                            {
                                var value = _pimAttributeHelper.GetValue(attribute, productValue.Values, language, dimensionSegmentData);
                                if (value.HasValue)
                                    searchableProperties.Add(value.Alias, value.Value);
                            }

                            product.SearchableProperties = searchableProperties;

                            // map prices
                            var prices = new List<ProductPrice>();
                            if (storeSetting?.PriceMapping != null)
                            {
                                foreach (var priceMapping in storeSetting.PriceMapping)
                                {
                                    if (priceMapping.PriceAttributeUid.HasValue)
                                    {
                                        var value = _pimAttributeHelper.GetDecimalValue(priceMapping.PriceAttributeUid.Value.ToString(), productValue.Values, language, dimensionSegmentData);
                                        if (value.HasValue)
                                            prices.Add(new ProductPrice(value.Value, priceMapping.Uid));
                                    }
                                }
                            }
                            product.Prices = prices;
                            items.Add(product);
                        }
                    }
                }
            }

            
            return items;
        }

        internal List<Variant> GetVariants(List<int> variantIds, Guid? storeId, int? languageId)
        {
            var integrationSettings = _settingsFacade.GetIntegrationSettings();
            var variants = _pimApiHelper.GetVariants(variantIds).ToDictionary(x => x.Id);
            var productIds = variants.Values.Select(x => x.ProductId).Distinct().ToList();
            var variantAttributes = GetVariantAttributes(productIds, integrationSettings);
            var variantValues = _pimApiHelper.GetVariantAttributeValues(variantIds, variantAttributes.AttributeUids).ToDictionary(x => x.VariantId);
            var attributes = _pimApiHelper.GetAttributes(variantAttributes.AttributeUids.Distinct().ToList()).ToDictionary(x => x.Uid);
            var productStructures = _pimApiHelper.GetProductStructures();
            var items = new List<Variant>();

            foreach (var storeSetting in integrationSettings.GeneralSettings.ShopSettings.Where(x => storeId == null || x.Uid == storeId.Value))
            {
                var dimensionSegmentData = _pimApiHelper.GetDimensionSegmentData(storeSetting);

                foreach (var language in _pimApiHelper.GetLanguages().Where(x => languageId == null || x.Id == languageId))
                {
                    var filteredVariantIds = FilterVariants(variantIds, storeSetting.Uid, language.CultureCode);

                    foreach (var variantId in filteredVariantIds)
                    {
                        if (variantValues.TryGetValue(variantId, out var variantValue) && variants.TryGetValue(variantId, out var v))
                        {
                            var variant = new Entity.Variant()
                            {
                                Id = v.Id,
                                CultureCode = language.CultureCode,
                                StoreId = storeSetting.Uid,
                                ConfigurationAlias = productStructures[v.ProductStructureUid].Alias,
                                Reference = variantValue.VariantId.ToString(),
                                ProductReference = v.ProductId.ToString()                                
                            };

                            // primary properties of variant
                            if (!string.IsNullOrEmpty(integrationSettings.VariantMapping?.TitleAttributeUid))
                                variant.Name = _pimAttributeHelper.GetStringValue(integrationSettings.VariantMapping.TitleAttributeUid, variantValue.Values, language, dimensionSegmentData);
                            if (!string.IsNullOrEmpty(integrationSettings.VariantMapping?.SkuAttributeUid))
                                variant.Sku = _pimAttributeHelper.GetStringValue(integrationSettings.VariantMapping.SkuAttributeUid, variantValue.Values, language, dimensionSegmentData);

                            // defining attributes
                            var attributeCombinations = new List<AttributeCombination>();
                            if (v.DefiningAttributes?.Any() ?? false)
                            {
                                foreach (var attributeUid in v.DefiningAttributes)
                                {
                                    var attribute = attributes[attributeUid];
                                    var attributeName = attribute.Name.ContainsKey(language.CultureCode) && !string.IsNullOrEmpty(attribute.Name[language.CultureCode]) ? attribute.Name[language.CultureCode] : attribute.BackofficeName;

                                    var value = _pimAttributeHelper.RenderRootAttribute(attribute, variantValue.Values, language, dimensionSegmentData);
                                    if (!string.IsNullOrWhiteSpace(value))
                                    {
                                        attributeCombinations.Add(
                                            new AttributeCombination
                                            (
                                                new AttributeName
                                                (
                                                    attribute.Alias,
                                                    attributeName
                                                ),
                                                new Umbraco.Commerce.Core.Models.AttributeValue
                                                (
                                                    Guid.NewGuid().ToString(),
                                                    value
                                                )
                                            )
                                        );
                                    }
                                }
                            }

                            variant.Attributes = attributeCombinations;

                            // map properties
                            var properties = new Dictionary<string, string>();
                            foreach (var attribute in variantAttributes.PropertyAttributeUids)
                            {
                                var value = _pimAttributeHelper.GetValue(attribute, variantValue.Values, language, dimensionSegmentData);
                                if (value.HasValue)
                                    properties.Add(value.Alias, value.Value);
                            }

                            variant.Properties = properties;

                            // map searchable properties
                            var searchableProperties = new Dictionary<string, string>();
                            foreach (var attribute in variantAttributes.SearchableAttributeUids)
                            {
                                var value = _pimAttributeHelper.GetValue(attribute, variantValue.Values, language, dimensionSegmentData);
                                if (value.HasValue)
                                    searchableProperties.Add(value.Alias, value.Value);
                            }

                            variant.SearchableProperties = searchableProperties;

                            // map prices
                            var prices = new List<ProductPrice>();
                            if (storeSetting?.PriceMapping != null)
                            {

                                foreach (var priceMapping in storeSetting.PriceMapping)
                                {
                                    if (priceMapping.PriceAttributeUid.HasValue)
                                    {
                                        var value = _pimAttributeHelper.GetDecimalValue(priceMapping.PriceAttributeUid.Value.ToString(), variantValue.Values, language, dimensionSegmentData);
                                        if (value.HasValue)
                                            prices.Add(new ProductPrice(value.Value, priceMapping.Uid));
                                    }
                                }
                            }
                            variant.Prices = prices;
                            items.Add(variant);
                        }
                    }
                }
            }

            return items;
        }

        public List<int> FilterProducts(List<int> productIds, Guid storeId, string cultureCode)
        {
            var integrationSettings = _settingsFacade.GetIntegrationSettings();
            var storeSetting = integrationSettings.GeneralSettings?.ShopSettings?.Where(s => s.Uid == storeId).FirstOrDefault();

            var queryModel = new BooleanQueryModel
            {
                BooleanOperator = BooleanOperator.And,
                SubQueries = new List<QueryModel>()
            };

            if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.PublishingAttributeUid))
            {
                var fieldUid = _pimAttributeHelper.GetAliasPath(integrationSettings.ProductMapping.PublishingAttributeUid, cultureCode);

                queryModel.SubQueries.Add(
                    new SimpleQueryModel()
                    {
                        Filters = new List<FieldFilterModel> { new FieldFilterModel { FieldUid = fieldUid.ToString(), FilterValue = string.Empty, QueryOperator = QueryOperator.IsNotEmpty } }
                    });
            }

            // Filter
            if (!string.IsNullOrEmpty(storeSetting?.FilterAttributeUid) && (storeSetting?.FilterAttributeGlobalListValueKeys?.Any() ?? false))
            {
                var fieldUid = _pimAttributeHelper.GetAliasPath(storeSetting.FilterAttributeUid, cultureCode, true);

                queryModel.SubQueries.Add(
                    new SimpleQueryModel()
                    {
                        Filters = new List<FieldFilterModel> { new FieldFilterModel { FieldUid = fieldUid.ToString(), FilterValue = string.Join(";", storeSetting.FilterAttributeGlobalListValueKeys), QueryOperator = QueryOperator.InList } }
                    });
            }

            // Catalogue
            if (storeSetting?.Catalogue != null)
            {
                var fieldUid = "PIM_Catalogue_" + storeSetting.Catalogue.Value.ToString();

                queryModel.SubQueries.Add(
                    new SimpleQueryModel()
                    {
                        Filters = new List<FieldFilterModel> { new FieldFilterModel { FieldUid = fieldUid.ToString(), FilterValue = string.Empty, QueryOperator = QueryOperator.IsNotEmpty } }
                    });
            }

            var searchModel = new SearchPagedModel()
            {
                FieldUids = new List<string> { "PIM_VariationDefinition" },
                Page = 1,
                PageSize = productIds.Count,
                IncludeArchived = false,
                SortDescending = false,
                QueryModel = queryModel,
            };

            return _pimApiHelper.SearchProductPaged(searchModel).ListItems.Select(x => x.Id).ToList();
        }

        public List<int> FilterVariants(List<int> variantIds, Guid storeId, string cultureCode)
        {
            var integrationSettings = _settingsFacade.GetIntegrationSettings();
            var storeSetting = integrationSettings.GeneralSettings?.ShopSettings?.Where(s => s.Uid == storeId).FirstOrDefault();

            var queryModel = new BooleanQueryModel
            {
                BooleanOperator = BooleanOperator.And,
                SubQueries = new List<QueryModel>()
            };

            if (!string.IsNullOrEmpty(integrationSettings.VariantMapping?.PublishingAttributeUid))
            {
                var fieldUid = _pimAttributeHelper.GetAliasPath(integrationSettings.VariantMapping.PublishingAttributeUid, cultureCode);

                queryModel.SubQueries.Add(
                    new SimpleQueryModel()
                    {
                        Filters = new List<FieldFilterModel> { new FieldFilterModel { FieldUid = fieldUid.ToString(), FilterValue = string.Empty, QueryOperator = QueryOperator.IsNotEmpty } }
                    });
            }

            // Catalogue
            if (storeSetting?.Catalogue != null)
            {
                var fieldUid = "PIM_Catalogue_" + storeSetting.Catalogue.Value.ToString();

                queryModel.SubQueries.Add(
                    new SimpleQueryModel()
                    {
                        Filters = new List<FieldFilterModel> { new FieldFilterModel { FieldUid = fieldUid.ToString(), FilterValue = string.Empty, QueryOperator = QueryOperator.IsNotEmpty } }
                    });
            }

            var searchModel = new SearchPagedModel()
            {
                FieldUids = integrationSettings.VariantMapping?.SearchableAttributeUids?.Select(a => a.ToString())?.ToList(),
                Page = 1,
                PageSize = variantIds.Count,
                IncludeArchived = false,
                SortDescending = false,
                QueryModel = queryModel,
            };

            return _pimApiHelper.SearchVariantPaged(searchModel).ListItems.Select(x => x.Id).ToList();
        }
    }
}
