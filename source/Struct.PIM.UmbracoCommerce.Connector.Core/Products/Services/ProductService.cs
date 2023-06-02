using Struct.PIM.Api.Models.Language;
using Struct.PIM.Api.Models.Shared;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Helpers;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings.Entity;
using Umbraco.Commerce.Common.Models;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Core.Services;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Services
{
    public class ProductService
    {
        private readonly SettingsFacade _settingsFacade;
        private readonly PimApiHelper _pimApiHelper;
        private readonly PimAttributeHelper _pimAttributeHelper;

        public ProductService(SettingsFacade settingsFacade)
        {
            _settingsFacade = settingsFacade;
            _pimApiHelper = new PimApiHelper(settingsFacade);
            _pimAttributeHelper = new PimAttributeHelper(_pimApiHelper);
        }

        protected Struct.PIM.Api.Client.StructPIMApiClient PIMClient(IntegrationSettings integrationSettings)
        {
            if (string.IsNullOrEmpty(integrationSettings.Setup.PimApiUrl))
                throw new InvalidOperationException("StructPIM.ApiUrl must be set in settings to use Struct PIM Umbraco Commerce");

            if (string.IsNullOrEmpty(integrationSettings.Setup.PimApiKey))
                throw new InvalidOperationException("StructPIM.ApiKey must be set in settings to use Struct PIM Umbraco Commerce");

            return new Struct.PIM.Api.Client.StructPIMApiClient(integrationSettings.Setup.PimApiUrl, integrationSettings.Setup.PimApiKey);
        }

        public IProductSnapshot GetProductSnapshot(Guid storeId, string productReference, string languageIsoCode)
        {
            if (!int.TryParse(productReference, out var productId))
                throw new InvalidCastException("productReference must be integer");

            var language = _pimApiHelper.GetLanguage(languageIsoCode);

            if (!string.IsNullOrEmpty(productReference))
            {
                var product = GetProductValuesSnapshot(new List<int> { productId }, storeId, language)?.SingleOrDefault();
                if (product == null)
                {
                    throw new Exception("Product not found");
                }
                product.ProductReference = productReference;
                return product;
            }
            else
            {
                throw new Exception("productReference does not have a value.");
            }
        }

        public IProductSnapshot GetProductSnapshot(Guid storeId, string productReference, string productVariantReference, string languageIsoCode)
        {
            if (string.IsNullOrEmpty(productReference) && string.IsNullOrEmpty(productVariantReference))
            {
                return null;
            }

            if (!int.TryParse(productReference, out var productId))
                throw new InvalidCastException("productReference must be integer");

            if (!int.TryParse(productVariantReference, out var variantId))
            {
                if (!string.IsNullOrEmpty(productVariantReference))
                {
                    throw new InvalidCastException("productVariantReference must be integer");
                }
            }

            var integrationSettings = _settingsFacade.GetIntegrationSettings();
            var language = _pimApiHelper.GetLanguage(languageIsoCode);

            if (!string.IsNullOrEmpty(productVariantReference))
            {
                var variant = GetVariantValues(new List<int> { variantId }, productId, integrationSettings, storeId, language)?.SingleOrDefault();
                if (variant == null)
                {
                    throw new Exception("Variant not found");
                }
                var productSnapshot = new ProductSnapshot
                {
                    IsGiftCard = false,
                    Name = variant.Name,
                    Prices = variant.Prices,
                    Attributes = variant.Attributes,
                    ProductReference = productReference,
                    ProductVariantReference = productVariantReference,
                    Sku = variant.Sku,
                    StoreId = storeId,
                    TaxClassId = null,
                };
                return productSnapshot;
            }

            if (!string.IsNullOrEmpty(productReference))
            {
                var product = GetProductValuesSnapshot(new List<int> { productId }, storeId, language)?.SingleOrDefault();
                if (product == null)
                {
                    throw new Exception("Product not found");
                }
                product.ProductReference = productReference;
                return product;
            }

            throw new Exception("productReference or productVariantReference must have a value.");
        }

        public IEnumerable<global::Umbraco.Commerce.Core.Models.Attribute> GetProductVariantAttributes(Guid storeId, string productReference, string languageIsoCode)
        {
            var result = new List<global::Umbraco.Commerce.Core.Models.Attribute>();
            if (!int.TryParse(productReference, out var productId))
                throw new InvalidCastException("productReference must be integer");

            var integrationSettings = _settingsFacade.GetIntegrationSettings();
            var language = _pimApiHelper.GetLanguage(languageIsoCode);

            var storeSetting = integrationSettings?.GeneralSettings?.ShopSettings?.Where(s => s.Uid == storeId).FirstOrDefault();

            var product = _pimApiHelper.GetProduct(productId);

            if (product.VariationDefinitionUid.HasValue)
            {
                var definingAttributeUids = _pimApiHelper.GetVariationDefinitionDefiningAttributes(product.ProductStructureUid, product.VariationDefinitionUid.Value);

                if (definingAttributeUids.Any())
                {
                    var definingAttributes = _pimApiHelper.GetPimAttributes(definingAttributeUids);
                    var variantValues = _pimApiHelper.GetVariantsAttributeValuesByProductId(productId, definingAttributeUids, new List<string> { language.CultureCode });
                    foreach (var definingAttribute in definingAttributes)
                    {
                        var paths = _pimApiHelper.Map(new List<Api.Models.Attribute.Attribute> { definingAttribute });
                        var values = new Dictionary<string, AttributeValue>();
                        foreach (var variantValue in variantValues)
                        {
                            string renderValue = _pimAttributeHelper.RenderAttribute(definingAttribute, definingAttribute, variantValue, paths, language, definingAttribute.Uid.ToString());

                            if (!values.ContainsKey(renderValue.Trim()))
                            {
                                values.Add(renderValue.Trim(), new AttributeValue(variantValue.VariantId.ToString(), renderValue.Trim()));
                            }
                            else
                            {
                                values[renderValue.Trim()].Alias += ";" + variantValue.VariantId;
                            }
                        }
                        result.Add(new global::Umbraco.Commerce.Core.Models.Attribute(definingAttribute.Alias, definingAttribute.BackofficeName, values.Values));
                    }
                }
            }

            return result;

        }

        public PagedResult<IProductSummary> SearchProductSummaries(Guid storeId, string languageIsoCode, string searchTerm, long currentPage = 1, long itemsPerPage = 50)
        {
            var integrationSettings = _settingsFacade.GetIntegrationSettings();
            var language = _pimApiHelper.GetLanguage(languageIsoCode);

            var queryModel = new BooleanQueryModel
            {
                BooleanOperator = BooleanOperator.And,
                SubQueries = new List<QueryModel>
                {
                }
            };

            if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.PublishingAttributeUid))
            {
                var attribyteUids = integrationSettings.ProductMapping.PublishingAttributeUid.Split(".");
                var rootAttribute = _pimApiHelper.GetPimAttribute(Guid.Parse(attribyteUids[0]));
                var fieldUid = _pimApiHelper.GetAliasPath(rootAttribute, string.Empty, Guid.Parse(attribyteUids.Last()), language.CultureCode, true, false);

                queryModel.SubQueries.Add(
                    new SimpleQueryModel()
                    {
                        Filters = new List<FieldFilterModel> { new FieldFilterModel { FieldUid = fieldUid.ToString(), FilterValue = string.Empty, QueryOperator = QueryOperator.IsNotEmpty } }
                    });
            }

            if ((integrationSettings.ProductMapping?.SearchableAttributeUids?.Any() ?? false) && searchTerm != null)
            {
                var filters = new List<FieldFilterModel>();

                foreach (var attr in integrationSettings.ProductMapping.SearchableAttributeUids)
                {
                    var attribyteUids = attr.Split(".");
                    var rootAttribute = _pimApiHelper.GetPimAttribute(Guid.Parse(attribyteUids[0]));
                    var fieldUid = _pimApiHelper.GetAliasPath(rootAttribute, string.Empty, Guid.Parse(attribyteUids.Last()), language.CultureCode, true, false);
                    filters.Add(new FieldFilterModel { FieldUid = fieldUid, FilterValue = searchTerm, QueryOperator = QueryOperator.Contains });
                }

                queryModel.SubQueries.Add(
                new SimpleQueryModel()
                {
                    BooleanOperator = BooleanOperator.Or,
                    Filters = filters,
                });
            }

            //Sorting. For now we sort on title attribute
            var sortField = string.Empty;
            if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.TitleAttributeUid?.ToString()))
            {
                var attribyteUids = integrationSettings.ProductMapping.TitleAttributeUid.Split(".");
                var rootAttribute = _pimApiHelper.GetPimAttribute(Guid.Parse(attribyteUids[0]));
                sortField = _pimApiHelper.GetAliasPath(rootAttribute, string.Empty, Guid.Parse(attribyteUids.Last()), language.CultureCode, true, false);
            }
            var searchModel = new SearchPagedModel()
            {
                FieldUids = new List<string> { "PIM_VariationDefinition" },
                Page = (int)currentPage,
                PageSize = (int)itemsPerPage,
                IncludeArchived = false,
                SortByFieldUid = sortField,
                SortDescending = false,
                QueryModel = queryModel,
            };

            var productListItems = _pimApiHelper.SearchProductPaged(searchModel);

            var products = GetProductValuesLookup(productListItems, storeId, language);

            var result = new PagedResult<IProductSummary>(products.Count, currentPage, itemsPerPage)
            {
                Items = products,
            };

            return result;
        }

        private List<ProductSnapshot> GetProductValuesSnapshot(List<int> productIds, Guid storeId, LanguageModel? language)
        {
            var integrationSettings = _settingsFacade.GetIntegrationSettings();
            var storeSetting = integrationSettings.GeneralSettings?.ShopSettings?.Where(s => s.Uid == storeId).FirstOrDefault();

            var attributeValueUids = new List<Guid>();
            if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.TitleAttributeUid))
            {
                var attributeUids = integrationSettings.ProductMapping.TitleAttributeUid.Split(".");
                attributeValueUids.Add(Guid.Parse(attributeUids[0]));
            }
            if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.SkuAttributeUid))
            {
                var attributeUids = integrationSettings.ProductMapping.SkuAttributeUid.Split(".");
                attributeValueUids.Add(Guid.Parse(attributeUids[0]));
            }
            if (integrationSettings.ProductMapping?.PropertyAttributeUids != null)
            {
                foreach (var attribute in integrationSettings.ProductMapping.PropertyAttributeUids)
                {
                    if (!string.IsNullOrEmpty(attribute))
                    {
                        var attributeUids = attribute.Split(".");
                        attributeValueUids.Add(Guid.Parse(attributeUids[0]));
                    }
                }
            }
            var attributeScopeAttributeUids = new List<Guid>();
            if (integrationSettings.ProductMapping?.PropertyScopes != null)
            {
                var attributeScopeUids = integrationSettings.ProductMapping.PropertyScopes.Select(uid => Guid.Parse(uid));
                List<Guid> attributeuids = _pimApiHelper.GetAttributeUidsFromScopeUids(attributeScopeUids);
                attributeValueUids.AddRange(attributeuids);
            }
            if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.IsGiftcardAttributeUid))
            {
                var attributeUids = integrationSettings.ProductMapping.IsGiftcardAttributeUid.Split(".");
                attributeValueUids.Add(Guid.Parse(attributeUids[0]));
            }
            if (storeSetting?.PriceMapping != null)
            {
                foreach (var priceMapping in storeSetting.PriceMapping)
                {
                    if (priceMapping.PriceAttributeUid.HasValue)
                    {
                        //var attributeUids = priceMapping.PriceAttributeUid.Value.Split(".");
                        attributeValueUids.Add(priceMapping.PriceAttributeUid.Value);
                    }
                }
            }

            var productValues = _pimApiHelper.GetProductsAttributeValues(productIds, attributeValueUids, new List<string> { language.CultureCode }).ToDictionary(x => x.ProductId);

            var items = new List<ProductSnapshot>();
            foreach (var productId in productIds)
            {
                if (productValues.TryGetValue(productId, out var productValue))
                {
                    var product = new Entity.ProductSnapshot()
                    {
                        StoreId = storeId,
                    };
                    if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.TitleAttributeUid))
                    {
                        PimAttributeValueDTO value = _pimAttributeHelper.GetValueForAttribute(integrationSettings.ProductMapping.TitleAttributeUid, productValue, language);
                        product.Name = value.Value;
                    }
                    if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.SkuAttributeUid))
                    {
                        PimAttributeValueDTO value = _pimAttributeHelper.GetValueForAttribute(integrationSettings.ProductMapping.SkuAttributeUid, productValue, language);
                        product.Sku = value.Value;
                    }
                    if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.IsGiftcardAttributeUid))
                    {
                        PimAttributeValueDTO value = _pimAttributeHelper.GetValueForAttribute(integrationSettings.ProductMapping.IsGiftcardAttributeUid, productValue, language);
                        if (bool.TryParse(value.Value, out bool booleanValue))
                        {
                            product.IsGiftCard = booleanValue;
                        }
                    }

                    var properties = new Dictionary<string, string>();
                    if (integrationSettings?.ProductMapping?.PropertyAttributeUids != null)
                    {
                        foreach (var attribute in integrationSettings?.ProductMapping?.PropertyAttributeUids)
                        {
                            if (!string.IsNullOrEmpty(attribute))
                            {
                                PimAttributeValueDTO value = _pimAttributeHelper.GetValueForAttribute(attribute, productValue, language);
                                if (!string.IsNullOrEmpty(value.Alias))
                                {
                                    properties.Add(value.Alias, value.Value);
                                }
                            }
                        }
                    }
                    if (attributeScopeAttributeUids.Any())
                    {
                        foreach (var attribute in attributeScopeAttributeUids)
                        {
                            PimAttributeValueDTO value = _pimAttributeHelper.GetValueForAttribute(attribute.ToString(), productValue, language);
                            if (!string.IsNullOrEmpty(value.Alias))
                            {
                                properties.Add(value.Alias, value.Value);
                            }
                        }
                    }
                    product.Properties = properties;
                    //Attributes shown in cart after SKU. Not that important atm.
                    //product.Attributes = new List<AttributeCombination> { new AttributeCombination(new AttributeName("Alias1", "Name1"), new AttributeValue("ValueAlias1", "AttribteName1")), new AttributeCombination(new AttributeName("Alias2", "Name2"), new AttributeValue("ValueAlias2", "AttribteName2")) };
                    var prices = new List<ProductPrice>();
                    if (storeSetting?.PriceMapping != null)
                    {

                        foreach (var priceMapping in storeSetting.PriceMapping)
                        {
                            if (priceMapping.PriceAttributeUid.HasValue)
                            {
                                PimAttributeValueDTO value = _pimAttributeHelper.GetValueForAttribute(priceMapping.PriceAttributeUid.Value.ToString(), productValue, language);
                                if (!string.IsNullOrEmpty(value.Alias))
                                {
                                    prices.Add(new ProductPrice(Decimal.Parse(value.Value), priceMapping.Uid));
                                }
                            }
                        }
                    }
                    product.Prices = prices;
                    items.Add(product);
                }
            }
            return items;
        }

        private List<ProductLookup> GetProductValuesLookup(IEnumerable<ListItem> productListItem, Guid storeId, LanguageModel? language)
        {
            var integrationSettings = _settingsFacade.GetIntegrationSettings();
            var productIds = productListItem.Select(p => p.Id).ToList();
            var productIdsHasVariantsMap = productListItem.ToDictionary(p => p.Id, q => q.ShownValues.Any(v => !string.IsNullOrEmpty(v)));
            var storeSetting = integrationSettings.GeneralSettings?.ShopSettings?.Where(s => s.Uid == storeId).FirstOrDefault();

            var attributeValueUids = new List<Guid>();
            if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.TitleAttributeUid))
            {
                var attributeUids = integrationSettings.ProductMapping.TitleAttributeUid.Split(".");
                attributeValueUids.Add(Guid.Parse(attributeUids[0]));
            }
            if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.SkuAttributeUid))
            {
                var attributeUids = integrationSettings.ProductMapping.SkuAttributeUid.Split(".");
                attributeValueUids.Add(Guid.Parse(attributeUids[0]));
            }

            if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.IsGiftcardAttributeUid))
            {
                var attributeUids = integrationSettings.ProductMapping.IsGiftcardAttributeUid.Split(".");
                attributeValueUids.Add(Guid.Parse(attributeUids[0]));
            }
            if (storeSetting?.PriceMapping != null)
            {
                foreach (var priceMapping in storeSetting.PriceMapping)
                {
                    if (priceMapping.PriceAttributeUid.HasValue)
                    {
                        //var attributeUids = priceMapping.PriceAttributeUid.Value.Split(".");
                        attributeValueUids.Add(priceMapping.PriceAttributeUid.Value);
                    }
                }
            }

            var productValues = _pimApiHelper.GetProductsAttributeValues(productIds, attributeValueUids, new List<string> { language.CultureCode }).ToDictionary(x => x.ProductId);

            var items = new List<ProductLookup>();
            foreach (var productId in productIds)
            {
                if (productValues.TryGetValue(productId, out var productValue))
                {
                    productIdsHasVariantsMap.TryGetValue(productId, out bool hasVariants);
                    var product = new Entity.ProductLookup()
                    {
                        Reference = productValue.ProductId.ToString(),

                        HasVariants = hasVariants,

                    };
                    if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.TitleAttributeUid))
                    {
                        PimAttributeValueDTO value = _pimAttributeHelper.GetValueForAttribute(integrationSettings.ProductMapping.TitleAttributeUid, productValue, language);
                        product.Name = value.Value;
                    }
                    if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.SkuAttributeUid))
                    {
                        PimAttributeValueDTO value = _pimAttributeHelper.GetValueForAttribute(integrationSettings.ProductMapping.SkuAttributeUid, productValue, language);
                        product.Sku = value.Value;
                    }
                    var prices = new List<ProductPrice>();
                    if (storeSetting?.PriceMapping != null)
                    {

                        foreach (var priceMapping in storeSetting.PriceMapping)
                        {
                            if (priceMapping.PriceAttributeUid.HasValue)
                            {
                                PimAttributeValueDTO value = _pimAttributeHelper.GetValueForAttribute(priceMapping.PriceAttributeUid.Value.ToString(), productValue, language);
                                if (!string.IsNullOrEmpty(value.Alias))
                                {
                                    prices.Add(new ProductPrice(Decimal.Parse(value.Value), priceMapping.Uid));
                                }
                            }
                        }
                    }
                    product.Prices = prices;
                    items.Add(product);
                }
            }
            return items;
        }

        public PagedResult<IProductVariantSummary> SearchProductVariantSummaries(Guid storeId, string productReference, string languageIsoCode, string searchTerm, IDictionary<string, IEnumerable<string>> attributes, long currentPage = 1, long itemsPerPage = 50)
        {
            if (!int.TryParse(productReference, out var productId))
                throw new InvalidCastException("productReference must be integer");

            var integrationSettings = _settingsFacade.GetIntegrationSettings();
            var language = _pimApiHelper.GetLanguage(languageIsoCode);

            var queryModel = new BooleanQueryModel
            {
                BooleanOperator = BooleanOperator.And,
                SubQueries = new List<QueryModel>
            {
                new SimpleQueryModel()
                    {
                        Filters = new List<FieldFilterModel> { new FieldFilterModel { FieldUid = "PIM_ProductId", FilterValue = productReference, QueryOperator = QueryOperator.Equals } }
                    }
            }
            };

            if (!string.IsNullOrEmpty(integrationSettings.VariantMapping?.PublishingAttributeUid))
            {
                var attribyteUids = integrationSettings.VariantMapping.PublishingAttributeUid.Split(".");
                var rootAttribute = _pimApiHelper.GetPimAttribute(Guid.Parse(attribyteUids[0]));
                var fieldUid = _pimApiHelper.GetAliasPath(rootAttribute, string.Empty, Guid.Parse(attribyteUids.Last()), language.CultureCode, true, false);

                queryModel.SubQueries.Add(
                    new SimpleQueryModel()
                    {
                        Filters = new List<FieldFilterModel> { new FieldFilterModel { FieldUid = fieldUid.ToString(), FilterValue = string.Empty, QueryOperator = QueryOperator.IsNotEmpty } }
                    });
            }

            if ((integrationSettings.VariantMapping?.SearchableAttributeUids?.Any() ?? false) && searchTerm != null)
            {
                var filters = new List<FieldFilterModel>();

                foreach (var attr in integrationSettings.VariantMapping.SearchableAttributeUids)
                {
                    var attribyteUids = attr.Split(".");
                    var rootAttribute = _pimApiHelper.GetPimAttribute(Guid.Parse(attribyteUids[0]));
                    var fieldUid = _pimApiHelper.GetAliasPath(rootAttribute, string.Empty, Guid.Parse(attribyteUids.Last()), language.CultureCode, true, false);
                    filters.Add(new FieldFilterModel { FieldUid = fieldUid, FilterValue = searchTerm, QueryOperator = QueryOperator.Contains });
                }

                queryModel.SubQueries.Add(
                new SimpleQueryModel()
                {
                    BooleanOperator = BooleanOperator.Or,
                    Filters = filters,
                });
            }

            if (attributes?.Any() ?? false)
            {
                var filters = new List<FieldFilterModel>();

                foreach (var attr in attributes)
                {
                    foreach (var filterVariantIds in attr.Value)
                    {
                        foreach (var variantId in filterVariantIds.Split(";"))
                        {
                            filters.Add(new FieldFilterModel { FieldUid = "Id", FilterValue = variantId, QueryOperator = QueryOperator.Equals });
                        }
                    }

                }

                queryModel.SubQueries.Add(
                new SimpleQueryModel()
                {
                    BooleanOperator = BooleanOperator.Or,
                    Filters = filters,
                });
            }

            //Sorting. For now we sort on title attribute
            var sortField = string.Empty;
            if (!string.IsNullOrEmpty(integrationSettings.VariantMapping?.TitleAttributeUid?.ToString()))
            {
                var attribyteUids = integrationSettings.VariantMapping.TitleAttributeUid.Split(".");
                var rootAttribute = _pimApiHelper.GetPimAttribute(Guid.Parse(attribyteUids[0]));
                sortField = _pimApiHelper.GetAliasPath(rootAttribute, string.Empty, Guid.Parse(attribyteUids.Last()), language.CultureCode, true, false);
            }
            var searchModel = new SearchPagedModel()
            {
                FieldUids = integrationSettings.VariantMapping?.SearchableAttributeUids?.Select(a => a.ToString())?.ToList(),
                Page = (int)currentPage,
                PageSize = (int)itemsPerPage,
                IncludeArchived = false,
                SortByFieldUid = sortField,
                SortDescending = false,
                QueryModel = queryModel,
            };

            var variantListItems = _pimApiHelper.SearchVariantPaged(searchModel);

            var variants = GetVariantValues(variantListItems.Select(l => l.Id).ToList(), productId, integrationSettings, storeId, language);

            var result = new PagedResult<IProductVariantSummary>(variants.Count, currentPage, itemsPerPage)
            {
                Items = variants.Select(x => new ProductVariantSummary(x)).ToList(),
            };

            return result;
        }

        private List<ProductVariant> GetVariantValues(List<int> variantIds, int productId, IntegrationSettings integrationSettings, Guid storeId, LanguageModel? language)
        {
            var storeSetting = integrationSettings.GeneralSettings?.ShopSettings?.Where(s => s.Uid == storeId).FirstOrDefault();

            var attributeValueUids = new List<Guid>();
            if (!string.IsNullOrEmpty(integrationSettings.VariantMapping?.TitleAttributeUid))
            {
                var attributeUids = integrationSettings.VariantMapping.TitleAttributeUid.Split(".");
                attributeValueUids.Add(Guid.Parse(attributeUids[0]));
            }
            if (!string.IsNullOrEmpty(integrationSettings.VariantMapping?.SkuAttributeUid))
            {
                var attributeUids = integrationSettings.VariantMapping.SkuAttributeUid.Split(".");
                attributeValueUids.Add(Guid.Parse(attributeUids[0]));
            }

            var variationDefinitionAttributes = _pimApiHelper.GetVariationDefinitionAttributes(new List<int> { productId });

            if (variationDefinitionAttributes.Any())
            {
                attributeValueUids.AddRange(variationDefinitionAttributes);
            }
            if (integrationSettings.VariantMapping?.PropertyAttributeUids != null)
            {
                foreach (var attribute in integrationSettings.VariantMapping.PropertyAttributeUids)
                {
                    if (!string.IsNullOrEmpty(attribute))
                    {
                        var attributeUids = attribute.Split(".");
                        attributeValueUids.Add(Guid.Parse(attributeUids[0]));
                    }
                }
            }
            var attributeScopeAttributeUids = new List<Guid>();
            if (integrationSettings.VariantMapping?.PropertyScopes != null)
            {
                var attributeScopeUids = integrationSettings.VariantMapping.PropertyScopes.Select(uid => Guid.Parse(uid));
                attributeScopeAttributeUids = _pimApiHelper.GetAttributeUidsFromScopeUids(attributeScopeUids);
                attributeValueUids.AddRange(attributeScopeAttributeUids);
            }
            if (storeSetting?.PriceMapping != null)
            {
                foreach (var priceMapping in storeSetting.PriceMapping)
                {
                    if (priceMapping.PriceAttributeUid.HasValue)
                    {
                        //var attributeUids = priceMapping.PriceAttributeUid.Value.Split(".");
                        attributeValueUids.Add(priceMapping.PriceAttributeUid.Value);
                    }
                }
            }

            var variantValues = _pimApiHelper.GetVariantsAttributeValues(variantIds, attributeValueUids, new List<string> { language.CultureCode }).ToDictionary(x => x.VariantId);
            var attributes = _pimApiHelper.GetPimAttributes(attributeValueUids.Distinct().ToList()).ToDictionary(x => x.Uid);
            var items = new List<ProductVariant>();

            foreach (var variantId in variantIds)
            {
                if (variantValues.TryGetValue(variantId, out var variantValue))
                {
                    var variant = new Entity.ProductVariant()
                    {
                        Reference = variantValue.VariantId.ToString(),

                    };
                    if (!string.IsNullOrEmpty(integrationSettings?.VariantMapping?.TitleAttributeUid))
                    {
                        PimAttributeValueDTO value = _pimAttributeHelper.GetValueForAttribute(integrationSettings.VariantMapping.TitleAttributeUid, variantValue, language);
                        variant.Name = value.Value;
                    }
                    if (!string.IsNullOrEmpty(integrationSettings?.VariantMapping?.SkuAttributeUid))
                    {
                        PimAttributeValueDTO value = _pimAttributeHelper.GetValueForAttribute(integrationSettings.VariantMapping.SkuAttributeUid, variantValue, language);
                        variant.Sku = value.Value;
                    }

                    var attributeCombinations = new List<AttributeCombination>();
                    if (variationDefinitionAttributes.Any())
                    {
                        foreach (var attributeUid in variationDefinitionAttributes)
                        {
                            var attribute = attributes[attributeUid];
                            var attributeName = attribute.Name.ContainsKey(language.CultureCode) && !string.IsNullOrEmpty(attribute.Name[language.CultureCode]) ? attribute.Name[language.CultureCode] : attribute.BackofficeName;

                            var value = _pimAttributeHelper.RenderAttribute(attribute, attribute, variantValue, _pimApiHelper.Map(new List<Api.Models.Attribute.Attribute> { attribute }), language, attribute.Uid.ToString());
                            if (!string.IsNullOrEmpty(attribute.Alias))
                            {
                                attributeCombinations.Add(
                                    new AttributeCombination
                                    (
                                        new AttributeName
                                        (
                                            attribute.Alias.ToLower(),
                                            attributeName
                                        ),
                                        new AttributeValue
                                        (
                                            Guid.NewGuid().ToString(), 
                                            value?.ToLower())
                                        )
                                    );
                            }
                        }
                    }

                    variant.Attributes = attributeCombinations;

                    var properties = new Dictionary<string, string>();
                    if (integrationSettings?.VariantMapping?.PropertyAttributeUids != null)
                    {
                        foreach (var attribute in integrationSettings?.VariantMapping?.PropertyAttributeUids)
                        {
                            if (!string.IsNullOrEmpty(attribute))
                            {
                                PimAttributeValueDTO value = _pimAttributeHelper.GetValueForAttribute(attribute, variantValue, language);
                                if (!string.IsNullOrEmpty(value.Alias))
                                {
                                    properties.Add(value.Alias, value.Value);
                                }
                            }
                        }
                    }
                    if (attributeScopeAttributeUids.Any())
                    {
                        var attributeScopeAttributes = _pimApiHelper.GetPimAttributes(attributeScopeAttributeUids);
                        foreach (var attribute in attributeScopeAttributes)
                        {
                            var paths = _pimApiHelper.Map(new List<Api.Models.Attribute.Attribute> { attribute });
                            string renderValue = _pimAttributeHelper.RenderAttribute(attribute, attribute, variantValue, paths, language, attribute.Uid.ToString());
                            if (!string.IsNullOrEmpty(renderValue))
                            {
                                properties.Add(attribute.Alias, renderValue);
                            }
                        }
                    }
                    variant.Properties = properties;

                    var prices = new List<ProductPrice>();
                    if (storeSetting?.PriceMapping != null)
                    {

                        foreach (var priceMapping in storeSetting.PriceMapping)
                        {
                            if (priceMapping.PriceAttributeUid.HasValue)
                            {
                                PimAttributeValueDTO value = _pimAttributeHelper.GetValueForAttribute(priceMapping.PriceAttributeUid.Value.ToString(), variantValue, language);
                                if (!string.IsNullOrEmpty(value.Alias))
                                {
                                    prices.Add(new ProductPrice(Decimal.Parse(value.Value), priceMapping.Uid));
                                }
                            }
                        }
                    }
                    variant.Prices = prices;
                    items.Add(variant);
                }
            }
            return items;
        }

        public bool TryGetProductReference(Guid storeId, string sku, out string productReference, out string productVariantReference)
        {
            //Todo should we implement it. Not tested. Have not triggered this method yet.

            var integrationSettings = _settingsFacade.GetIntegrationSettings();

            productReference = null;
            productVariantReference = null;

            //Product
            if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.SkuAttributeUid))
            {
                var queryModel = new BooleanQueryModel
                {
                    BooleanOperator = BooleanOperator.And,
                    SubQueries = new List<QueryModel>
                    {
                    }
                };

                var filters = new List<FieldFilterModel>();

                var attribyteUids = integrationSettings.ProductMapping?.SkuAttributeUid.Split(".");
                var rootAttribute = _pimApiHelper.GetPimAttribute(Guid.Parse(attribyteUids[0]));
                var fieldUid = _pimApiHelper.GetAliasPath(rootAttribute, string.Empty, Guid.Parse(attribyteUids.Last()), null, true, false);
                filters.Add(new FieldFilterModel { FieldUid = fieldUid, FilterValue = sku, QueryOperator = QueryOperator.Equals });


                queryModel.SubQueries.Add(
                new SimpleQueryModel()
                {
                    BooleanOperator = BooleanOperator.Or,
                    Filters = filters,
                });

                var searchModel = new SearchPagedModel()
                {
                    FieldUids = new List<string>(),
                    Page = (int)1,
                    PageSize = (int)2,
                    IncludeArchived = false,
                    //SortByFieldUid = sortField,
                    SortDescending = false,
                    QueryModel = queryModel,
                };
                var productListItems = _pimApiHelper.SearchProductPaged(searchModel);

                if (productListItems != null && productListItems.Count() == 1)
                {
                    productReference = productListItems.First().Id.ToString();
                }
            }

            //Variant
            if (!string.IsNullOrEmpty(integrationSettings.VariantMapping?.SkuAttributeUid))
            {
                var queryModel = new BooleanQueryModel
                {
                    BooleanOperator = BooleanOperator.And,
                    SubQueries = new List<QueryModel>
                    {
                    }
                };

                var filters = new List<FieldFilterModel>();

                var attribyteUids = integrationSettings.VariantMapping?.SkuAttributeUid.Split(".");
                var rootAttribute = _pimApiHelper.GetPimAttribute(Guid.Parse(attribyteUids[0]));
                var fieldUid = _pimApiHelper.GetAliasPath(rootAttribute, string.Empty, Guid.Parse(attribyteUids.Last()), null, true, false);
                filters.Add(new FieldFilterModel { FieldUid = fieldUid, FilterValue = sku, QueryOperator = QueryOperator.Equals });


                queryModel.SubQueries.Add(
                new SimpleQueryModel()
                {
                    BooleanOperator = BooleanOperator.Or,
                    Filters = filters,
                });

                var searchModel = new SearchPagedModel()
                {
                    FieldUids = new List<string>(),
                    Page = (int)1,
                    PageSize = (int)2,
                    IncludeArchived = false,
                    //SortByFieldUid = sortField,
                    SortDescending = false,
                    QueryModel = queryModel,
                };
                var variantListItems = _pimApiHelper.SearchVariantPaged(searchModel);

                if (variantListItems != null && variantListItems.Count() == 1)
                {
                    productVariantReference = variantListItems.First().Id.ToString();
                }
            }

            return productReference != null || productVariantReference != null;
        }

        public List<Api.Models.Language.LanguageModel> GetLanguages()
        {
            return _pimApiHelper.GetLanguages();
        }

        public List<Api.Models.Attribute.AttributeScope> GetAttributeScopes()
        {
            return _pimApiHelper.GetPimAttributeScopes();
        }

        public List<Api.Models.Dimension.DimensionModel> GetDimensions()
        {
            return _pimApiHelper.GetPimDimensions();
        }

        public List<PimAttribute> GetAttributeWithProductReference()
        {
            return _pimApiHelper.GetAttributeWithProductReference();
        }

        public List<PimAttribute> GetAttributeWithVariantReference()
        {
            return _pimApiHelper.GetAttributeWithVariantReference();
        }
    }
}
