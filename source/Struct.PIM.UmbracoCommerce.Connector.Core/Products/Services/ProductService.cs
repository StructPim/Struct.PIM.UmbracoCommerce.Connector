using Lucene.Net.Util;
using Struct.PIM.Api.Models.Language;
using Struct.PIM.Api.Models.Shared;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Helpers;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings.Entity;
using Umbraco.Commerce.Common.Models;
using Umbraco.Commerce.Core.Events.Domain.Handlers.Order;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Core.Services;
using AttributeValue = Umbraco.Commerce.Core.Models.AttributeValue;

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

        public List<Api.Models.Catalogue.CatalogueModel> GetCatalogues()
        {
            return _pimApiHelper.GetCatalogues();
        }

        public IProductSnapshot GetProductSnapshot(Guid storeId, string productReference, string languageIsoCode)
        {
            return GetProductSnapshot(storeId, productReference, string.Empty, languageIsoCode);
        }

        public IProductSnapshot GetProductSnapshot(Guid storeId, string productReference, string productVariantReference, string languageIsoCode)
        {
            if (!int.TryParse(productReference, out var productId))
                throw new InvalidCastException("productReference must be integer");

            if (!int.TryParse(productVariantReference, out var variantId))
                if (!string.IsNullOrEmpty(productVariantReference))
                    throw new InvalidCastException("productVariantReference must be integer");
            
            var integrationSettings = _settingsFacade.GetIntegrationSettings();
            var language = _pimApiHelper.GetLanguage(languageIsoCode);

            if (!string.IsNullOrEmpty(productVariantReference))
            {
                var variant = GetVariants(new List<int> { variantId }, productId, integrationSettings, storeId, language)?.SingleOrDefault();
                
                if (variant == null)
                    throw new Exception($"Variant not found [{productVariantReference}]");
                
                return variant.AsSnapshot();
            }

            if (!string.IsNullOrEmpty(productReference))
            {
                var product = GetProducts(new List<int> { productId }, storeId, language)?.SingleOrDefault();
                
                if (product == null)
                    throw new Exception($"Product not found [{productReference}]");
                
                return product.AsSnapShot();
            }

            throw new Exception("productReference or productVariantReference must have a value.");
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

            var variantAttributes = GetVariantAttributes(productId, integrationSettings, storeSetting);

            if (variantAttributes.VariationDefinitionAttributes?.Any() ?? false)
            {
                var definingAttributes = _pimApiHelper.GetAttributes(variantAttributes.VariationDefinitionAttributes);
                var variantValues = _pimApiHelper.GetVariantsAttributeValuesByProductId(productId, variantAttributes.VariationDefinitionAttributes, new List<string> { language.CultureCode });
                foreach (var definingAttribute in definingAttributes)
                {
                    var paths = _pimApiHelper.Map(new List<Api.Models.Attribute.Attribute> { definingAttribute });
                    var values = new Dictionary<string, AttributeValue>();
                    foreach (var variantValue in variantValues)
                    {
                        string renderValue = _pimAttributeHelper.RenderAttribute(definingAttribute, definingAttribute, variantValue, paths, language, definingAttribute.Uid.ToString(), dimensionSegmentData);

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

            return result;

        }

        public PagedResult<IProductSummary> SearchProductSummaries(Guid storeId, string languageIsoCode, string searchTerm, long currentPage = 1, long itemsPerPage = 50)
        {
            var integrationSettings = _settingsFacade.GetIntegrationSettings();
            var language = _pimApiHelper.GetLanguage(languageIsoCode);
            var storeSetting = integrationSettings.GeneralSettings?.ShopSettings?.Where(s => s.Uid == storeId).FirstOrDefault();
            
            var queryModel = new BooleanQueryModel
            {
                BooleanOperator = BooleanOperator.And,
                SubQueries = new List<QueryModel>()
            };

            if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.PublishingAttributeUid))
            {
                var attribyteUids = integrationSettings.ProductMapping.PublishingAttributeUid.Split(".");
                var rootAttribute = _pimApiHelper.GetAttribute(Guid.Parse(attribyteUids[0]));
                var fieldUid = _pimAttributeHelper.GetAliasPath(rootAttribute, string.Empty, Guid.Parse(attribyteUids.Last()), language.CultureCode, true, false, false);

                queryModel.SubQueries.Add(
                    new SimpleQueryModel()
                    {
                        Filters = new List<FieldFilterModel> { new FieldFilterModel { FieldUid = fieldUid.ToString(), FilterValue = string.Empty, QueryOperator = QueryOperator.IsNotEmpty } }
                    });
            }

            // catalogue

            //if (!string.IsNullOrEmpty(storeSetting?.FilterAttributeUid))
            //{
            //    var attribyteUids = storeSetting.FilterAttributeUid.Split(".");
            //    var rootAttribute = _pimApiHelper.GetPimAttribute(Guid.Parse(attribyteUids[0]));
            //    var fieldUid = _pimAttributeHelper.GetAliasPath(rootAttribute, string.Empty, Guid.Parse(attribyteUids.Last()), language.CultureCode, true, false, false);

            //    queryModel.SubQueries.Add(
            //        new SimpleQueryModel()
            //        {
            //            Filters = new List<FieldFilterModel> { new FieldFilterModel { FieldUid = fieldUid.ToString(), FilterValue = string.Empty, QueryOperator = QueryOperator.IsNotEmpty } }
            //        });
            //}

            if ((integrationSettings.ProductMapping?.SearchableAttributeUids?.Any() ?? false) && searchTerm != null)
            {
                var filters = new List<FieldFilterModel>();

                foreach (var attr in integrationSettings.ProductMapping.SearchableAttributeUids)
                {
                    var attribyteUids = attr.Split(".");
                    var rootAttribute = _pimApiHelper.GetAttribute(Guid.Parse(attribyteUids[0]));
                    var fieldUid = _pimAttributeHelper.GetAliasPath(rootAttribute, string.Empty, Guid.Parse(attribyteUids.Last()), language.CultureCode, true, false, false);
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
                var rootAttribute = _pimApiHelper.GetAttribute(Guid.Parse(attribyteUids[0]));
                sortField = _pimAttributeHelper.GetAliasPath(rootAttribute, string.Empty, Guid.Parse(attribyteUids.Last()), language.CultureCode, true, false, false);
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

            var productListItems = _pimApiHelper.SearchProductPaged(searchModel).Select(x => x.Id).ToList();

            var products = GetProducts(productListItems, storeId, language);

            var result = new PagedResult<IProductSummary>(products.Count, currentPage, itemsPerPage)
            {
                Items = products.Select(x => x.AsSummary()),
            };

            return result;
        }

        private List<Product> GetProducts(List<int> productIds, Guid storeId, LanguageModel? language)
        {
            var integrationSettings = _settingsFacade.GetIntegrationSettings();
            var storeSetting = integrationSettings.GeneralSettings?.ShopSettings?.Where(s => s.Uid == storeId).FirstOrDefault();
            var dimensionSegmentData = _pimApiHelper.GetDimensionSegmentData(storeSetting);
            var attributeInfo = GetProductAttributes(integrationSettings, storeSetting);
            var products = _pimApiHelper.GetProducts(productIds).ToDictionary(x => x.Id);
            var productValues = _pimApiHelper.GetProductsAttributeValues(productIds, attributeInfo.AttributeUids, new List<string> { language.CultureCode }).ToDictionary(x => x.ProductId);
            
            var items = new List<Product>();
            foreach (var productId in productIds)
            {
                if (productValues.TryGetValue(productId, out var productValue))
                {
                    var product = new Entity.Product()
                    {
                        ProductReference = productId.ToString(),
                        StoreId = storeId,
                        HasVariants = products[productId].VariationDefinitionUid.HasValue
                    };

                    if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.TitleAttributeUid))
                        product.Name = _pimAttributeHelper.GetStringValue(integrationSettings.ProductMapping.TitleAttributeUid, productValue, language, dimensionSegmentData);
                    if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.SkuAttributeUid))
                        product.Sku = _pimAttributeHelper.GetStringValue(integrationSettings.ProductMapping.SkuAttributeUid, productValue, language, dimensionSegmentData);
                    if (!string.IsNullOrEmpty(integrationSettings.ProductMapping?.IsGiftcardAttributeUid))
                        product.IsGiftCard = _pimAttributeHelper.GetBoolValue(integrationSettings.ProductMapping.IsGiftcardAttributeUid, productValue, language, dimensionSegmentData).GetValueOrDefault();
                
                    product.Properties = new Dictionary<string, string>();
                    foreach (var attribute in attributeInfo.PropertyAttributeUids)
                    {
                        if (!string.IsNullOrEmpty(attribute))
                        {
                            var value = _pimAttributeHelper.GetValue(attribute, productValue, language, dimensionSegmentData);
                            if (value.HasValue)
                                product.Properties.Add(value.Alias, value.Value);
                        }
                    }

                    var prices = new List<ProductPrice>();
                    if (storeSetting?.PriceMapping != null)
                    {
                        foreach (var priceMapping in storeSetting.PriceMapping)
                        {
                            if (priceMapping.PriceAttributeUid.HasValue)
                            {
                                var value = _pimAttributeHelper.GetDecimalValue(priceMapping.PriceAttributeUid.Value.ToString(), productValue, language, dimensionSegmentData);
                                if (value.HasValue)
                                    prices.Add(new ProductPrice(value.Value, priceMapping.Uid));
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
                var rootAttribute = _pimApiHelper.GetAttribute(Guid.Parse(attribyteUids[0]));
                var fieldUid = _pimAttributeHelper.GetAliasPath(rootAttribute, string.Empty, Guid.Parse(attribyteUids.Last()), language.CultureCode, true, false, false);

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
                    var rootAttribute = _pimApiHelper.GetAttribute(Guid.Parse(attribyteUids[0]));
                    var fieldUid = _pimAttributeHelper.GetAliasPath(rootAttribute, string.Empty, Guid.Parse(attribyteUids.Last()), language.CultureCode, true, false, false);
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
                var allowedVariants = new List<List<string>>();

                foreach (var attr in attributes)
                {
                    var allowedVariantsByAttribute = new List<List<string>>();

                    foreach (var filterVariantIds in attr.Value)
                    {
                        allowedVariantsByAttribute.Add(filterVariantIds.Split(";").ToList());
                    }

                    allowedVariants.Add(allowedVariantsByAttribute.SelectMany(x => x).Distinct().ToList());
                }

                foreach(var variantId in allowedVariants.IntersectAll())
                {
                    filters.Add(new FieldFilterModel { FieldUid = "Id", FilterValue = variantId, QueryOperator = QueryOperator.Equals });
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
                var rootAttribute = _pimApiHelper.GetAttribute(Guid.Parse(attribyteUids[0]));
                sortField = _pimAttributeHelper.GetAliasPath(rootAttribute, string.Empty, Guid.Parse(attribyteUids.Last()), language.CultureCode, true, false, false);
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

            var variants = GetVariants(variantListItems.Select(l => l.Id).ToList(), productId, integrationSettings, storeId, language);

            var result = new PagedResult<IProductVariantSummary>(variants.Count, currentPage, itemsPerPage)
            {
                Items = variants.Select(x => x.AsSummary()).ToList(),
            };

            return result;
        }

        private List<Variant> GetVariants(List<int> variantIds, int productId, IntegrationSettings integrationSettings, Guid storeId, LanguageModel language)
        {
            var storeSetting = integrationSettings.GeneralSettings?.ShopSettings?.Where(s => s.Uid == storeId).FirstOrDefault();
            
            var dimensionSegmentData = _pimApiHelper.GetDimensionSegmentData(storeSetting);
            var variantAttributes = GetVariantAttributes(productId, integrationSettings, storeSetting);
            var variantValues = _pimApiHelper.GetVariantsAttributeValues(variantIds, variantAttributes.AttributeUids, new List<string> { language.CultureCode }).ToDictionary(x => x.VariantId);
            var attributes = _pimApiHelper.GetAttributes(variantAttributes.AttributeUids.Distinct().ToList()).ToDictionary(x => x.Uid);
            var items = new List<Variant>();

            foreach (var variantId in variantIds)
            {
                if (variantValues.TryGetValue(variantId, out var variantValue))
                {
                    var variant = new Entity.Variant()
                    {
                        Reference = variantValue.VariantId.ToString(),
                        StoreId = storeId,
                        ProductReference = productId.ToString()
                    };

                    if (!string.IsNullOrEmpty(integrationSettings.VariantMapping?.TitleAttributeUid))
                        variant.Name = _pimAttributeHelper.GetStringValue(integrationSettings.VariantMapping.TitleAttributeUid, variantValue, language, dimensionSegmentData);
                    if (!string.IsNullOrEmpty(integrationSettings.VariantMapping?.SkuAttributeUid))
                        variant.Sku = _pimAttributeHelper.GetStringValue(integrationSettings.VariantMapping.SkuAttributeUid, variantValue, language, dimensionSegmentData);
                    
                    var attributeCombinations = new List<AttributeCombination>();
                    if (variantAttributes.VariationDefinitionAttributes?.Any() ?? false)
                    {
                        foreach (var attributeUid in variantAttributes.VariationDefinitionAttributes)
                        {
                            var attribute = attributes[attributeUid];
                            var attributeName = attribute.Name.ContainsKey(language.CultureCode) && !string.IsNullOrEmpty(attribute.Name[language.CultureCode]) ? attribute.Name[language.CultureCode] : attribute.BackofficeName;

                            var value = _pimAttributeHelper.RenderAttribute(attribute, attribute, variantValue, _pimApiHelper.Map(new List<Api.Models.Attribute.Attribute> { attribute }), language, attribute.Uid.ToString(), dimensionSegmentData);
                            if (!string.IsNullOrEmpty(attribute.Alias))
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

                    var properties = new Dictionary<string, string>();
                    foreach (var attribute in variantAttributes.PropertyAttributeUids)
                    {
                        var value = _pimAttributeHelper.GetValue(attribute, variantValue, language, dimensionSegmentData);
                        if (value.HasValue)
                            properties.Add(value.Alias, value.Value);
                    }
                    
                    variant.Properties = properties;

                    var prices = new List<ProductPrice>();
                    if (storeSetting?.PriceMapping != null)
                    {

                        foreach (var priceMapping in storeSetting.PriceMapping)
                        {
                            if (priceMapping.PriceAttributeUid.HasValue)
                            {
                                var value = _pimAttributeHelper.GetDecimalValue(priceMapping.PriceAttributeUid.Value.ToString(), variantValue, language, dimensionSegmentData);
                                if (value.HasValue)
                                    prices.Add(new ProductPrice(value.Value, priceMapping.Uid));
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

                var attribyteUids = integrationSettings.ProductMapping.SkuAttributeUid.Split(".");
                var rootAttribute = _pimApiHelper.GetAttribute(Guid.Parse(attribyteUids[0]));
                var fieldUid = _pimAttributeHelper.GetAliasPath(rootAttribute, string.Empty, Guid.Parse(attribyteUids.Last()), null, true, false, false);
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
                    Page = 1,
                    PageSize = 2,
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

                var attribyteUids = integrationSettings.VariantMapping.SkuAttributeUid.Split(".");
                var rootAttribute = _pimApiHelper.GetAttribute(Guid.Parse(attribyteUids[0]));
                var fieldUid = _pimAttributeHelper.GetAliasPath(rootAttribute, string.Empty, Guid.Parse(attribyteUids.Last()), null, true, false, false);
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

        public List<GlobalListValue> GetGlobalListAttributeValues(Guid uid)
        {
            var values = _pimApiHelper.GetGlobalListAttributeValues(uid);
            var globalList = _pimApiHelper.GetGlobalList(uid);
            var result = new List<GlobalListValue>();

            foreach(var val in values)
            {
                new GlobalListValue
                {
                    Uid = val.Uid.ToString(),
                    Value = null //_pimAttributeHelper.RenderAttribute(globalList.Attribute, globalList.Attribute, null, )
                };
            }

            return result;
        }

        public EntityAttributes GetProductAttributes(IntegrationSettings integrationSettings, StoreSettings storeSetting)
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
            if (storeSetting?.PriceMapping != null)
            {
                foreach (var priceMapping in storeSetting.PriceMapping)
                {
                    if (priceMapping.PriceAttributeUid.HasValue)
                    {
                        //var attributeUids = priceMapping.PriceAttributeUid.Value.Split(".");
                        productAttributes.AttributeUids.Add(priceMapping.PriceAttributeUid.Value);
                    }
                }
            }

            return productAttributes;
        }

        public EntityAttributes GetVariantAttributes(int productId, IntegrationSettings integrationSettings, StoreSettings storeSetting)
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

            variantAttributes.VariationDefinitionAttributes = _pimApiHelper.GetVariationDefinitionAttributes(new List<int> { productId });

            if (variantAttributes.VariationDefinitionAttributes.Any())
            {
                variantAttributes.AttributeUids.AddRange(variantAttributes.VariationDefinitionAttributes);
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
            if (integrationSettings.VariantMapping?.PropertyScopes != null)
            {
                var attributeScopeUids = integrationSettings.VariantMapping.PropertyScopes.Select(Guid.Parse);
                var attributeUids = _pimApiHelper.GetAttributeUidsFromScopeUids(attributeScopeUids);
                variantAttributes.PropertyAttributeUids.AddRange(attributeUids.Select(x => x.ToString()));
                variantAttributes.AttributeUids.AddRange(attributeUids);
            }
            if (storeSetting?.PriceMapping != null)
            {
                foreach (var priceMapping in storeSetting.PriceMapping)
                {
                    if (priceMapping.PriceAttributeUid.HasValue)
                    {
                        //var attributeUids = priceMapping.PriceAttributeUid.Value.Split(".");
                        variantAttributes.AttributeUids.Add(priceMapping.PriceAttributeUid.Value);
                    }
                }
            }

            if (variantAttributes.AttributeUids != null)
                variantAttributes.AttributeUids = variantAttributes.AttributeUids.Distinct().ToList();

            return variantAttributes;
        }
    }
}
