using Struct.PIM.Api.Models.Attribute;
using Struct.PIM.Api.Models.DataConfiguration;
using Struct.PIM.Api.Models.GlobalList;
using Struct.PIM.Api.Models.Language;
using Struct.PIM.Api.Models.Product;
using Struct.PIM.Api.Models.Shared;
using Struct.PIM.Api.Models.Variant;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings.Entity;
using System.Globalization;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Helpers
{
    public class PimApiHelper
    {
        private readonly SettingsFacade _settingsFacade;
        private AsyncLocal<Dictionary<Guid, Api.Models.Attribute.Attribute>> _attributes = new AsyncLocal<Dictionary<Guid, Api.Models.Attribute.Attribute>>();
        private AsyncLocal<List<Api.Models.Attribute.AttributeScope>> _attributeScopes = new AsyncLocal<List<Api.Models.Attribute.AttributeScope>>();
        private AsyncLocal<Dictionary<Guid, List<Guid>>> _attributesByScope = new AsyncLocal<Dictionary<Guid, List<Guid>>>();
        private AsyncLocal<List<Api.Models.Dimension.DimensionModel>> _dimensions = new AsyncLocal<List<Api.Models.Dimension.DimensionModel>>();
        private AsyncLocal<List<Api.Models.Language.LanguageModel>> _languages = new AsyncLocal<List<Api.Models.Language.LanguageModel>>();
        
        public PimApiHelper(SettingsFacade settingsFacade)
        {
            _settingsFacade = settingsFacade;
        }

        protected PIM.Api.Client.StructPIMApiClient PIMClient()
        {
            var integrationSettings = _settingsFacade.GetIntegrationSettings();
            if (string.IsNullOrEmpty(integrationSettings.Setup.PimApiUrl))
                throw new InvalidOperationException("StructPIM.ApiUrl must be set in settings to use Struct PIM Vendr");

            if (string.IsNullOrEmpty(integrationSettings.Setup.PimApiKey))
                throw new InvalidOperationException("StructPIM.ApiKey must be set in settings to use Struct PIM Vendr");

            return new PIM.Api.Client.StructPIMApiClient(integrationSettings.Setup.PimApiUrl, integrationSettings.Setup.PimApiKey);
        }

        public bool IsValid()
        {
            try
            {
                return GetLanguages().Any();
            }
            catch
            {
                return false;
            }
        }

        public Dictionary<Guid, Api.Models.Attribute.Attribute> GetAttributes()
        {
            if(_attributes.Value == null)
                _attributes.Value = PIMClient().Attributes.GetAttributes().ToDictionary(x => x.Uid);

            return _attributes.Value;
        }

        internal List<Api.Models.Attribute.Attribute> GetAttributes(List<Guid> attributeUids)
        {
            var attributes = GetAttributes();
            var matchedAttributes = new List<Api.Models.Attribute.Attribute>();

            foreach(var uid in attributeUids)
            {
                if(attributes.TryGetValue(uid, out var attr))
                    matchedAttributes.Add(attr);
            }

            return matchedAttributes;
        }

        internal Api.Models.Attribute.Attribute? GetAttribute(Guid attributeUid)
        {
            var attributes = GetAttributes();

            if (attributes.TryGetValue(attributeUid, out var attribute))
                return attribute;

            return null;
        }


        public List<Api.Models.Language.LanguageModel> GetLanguages()
        {
            if(_languages.Value == null)
                _languages.Value = PIMClient().Languages.GetLanguages();

            return _languages.Value;
        }

        public List<Api.Models.Attribute.AttributeScope> GetAttributeScopes()
        {
            if (_attributeScopes.Value == null)
                _attributeScopes.Value = PIMClient().Attributes.GetAttributeScopes().OrderBy(x => x.Alias).ToList();

            return _attributeScopes.Value;
        }

        public List<Api.Models.Dimension.DimensionModel> GetDimensions()
        {
            if(_dimensions.Value == null)
                _dimensions.Value = PIMClient().Dimensions.GetDimensions().OrderBy(x => x.Alias).ToList();

            return _dimensions.Value;
        }

        public List<Api.Models.Catalogue.CatalogueModel> GetCatalogues()
        {
            return PIMClient().Catalogues.GetCatalogues().OrderBy(x => x.Label).ToList();
        }

        public List<Entity.Attribute> GetAttributeWithProductReference()
        {
            var attributes = GetAttributes().Values.ToList();

            var tabSetups = PIMClient().ProductStructures.GetProductStructures().Where(x => x.ProductConfiguration.Tabs?.Any() ?? false).SelectMany(x => x.ProductConfiguration.Tabs).ToList();
            var productAttributeUids = GetAttributesFromConfigurationTabs(tabSetups);

            if (productAttributeUids?.Any() ?? false)
            {
                attributes = attributes.Where(x => productAttributeUids.Contains(x.Uid)).ToList();
            }

            var mappedAttributes = Map(attributes);
            return mappedAttributes;
        }

        public List<Guid> GetAttributesFromConfigurationTabs(List<Api.Models.DataConfiguration.TabSetup> tabSetups)
        {
            var attributes = new List<Guid>();

            foreach(var tab in tabSetups)
            {
                var dynamicTab = tab as DynamicTabSetup;
                if(dynamicTab?.Sections?.Any() ?? false)
                {
                    foreach(var section in dynamicTab.Sections)
                    {
                        var dynamicSection = section as DynamicSectionSetup;

                        if(dynamicSection?.Properties?.Any() ?? false)
                        {
                            foreach(var property in dynamicSection.Properties)
                            {
                                var dynamicProperty = property as AttributeSetup;

                                if (dynamicProperty != null)
                                    attributes.Add(dynamicProperty.AttributeUid);
                            }
                        }
                    }
                }
            }
            
            return attributes;
        }

        public List<Entity.Attribute> GetAttributeWithVariantReference()
        {
            var attributes = GetAttributes().Values.ToList();

            var tabSetups = PIMClient().ProductStructures.GetProductStructures().Where(x => x.HasVariants && (x.VariantConfiguration.Tabs?.Any() ?? false)).SelectMany(x => x.VariantConfiguration.Tabs).ToList();
            var variantAttributeUids = GetAttributesFromConfigurationTabs(tabSetups);

            if (variantAttributeUids?.Any() ?? false)
            {
                attributes = attributes.Where(x => variantAttributeUids.Contains(x.Uid)).ToList();
            }

            var mappedAttributes = Map(attributes);
            return mappedAttributes;
        }

        public List<Api.Models.GlobalList.GlobalListValue> GetGlobalListAttributeValues(Guid uid)
        {
            var client = PIMClient();
            return client.GlobalLists.GetGlobalListValues(uid).GlobalListValues.ToList();
        }


        public GlobalList GetGlobalList(Guid uid)
        {
            var client = PIMClient();
            return client.GlobalLists.GetGlobalList(uid);
        }

        public Dictionary<string, Tuple<string, string>> GetDimensionSegmentData(StoreSettings? storeSetting)
        {
            var dimensionSegmentData = new Dictionary<string, Tuple<string, string>>();
            var dimensions = storeSetting?.DimensionSettings;
            if (dimensions == null)
                return dimensionSegmentData;

            var dimensionsPim = GetDimensions();
            
            foreach (var dim in dimensions)
            {
                var dPim = dimensionsPim.Where(d => d.Uid == Guid.Parse(dim.Key)).FirstOrDefault();
                var segment = dPim?.Segments.Where(s => s.Uid == Guid.Parse(dim.Value)).FirstOrDefault();
                if (segment != null)
                {
                    dimensionSegmentData.Add(dPim.Uid.ToString().ToLower(), new Tuple<string, string>(dPim.Alias, segment.Identifier));
                }
            }
            return dimensionSegmentData;
        }

        internal List<Entity.Attribute> Map(List<Api.Models.Attribute.Attribute> attributes)
        {
            var result = new List<Entity.Attribute>();
            foreach (var attribute in attributes)
            {
                if (attribute is ListAttribute)
                {
                    continue;
                }
                
                result.Add(new Entity.Attribute
                {
                    Alias = attribute.Alias,
                    Uid = attribute.Uid.ToString(),
                    Type = attribute.AttributeType
                });

                if (attribute is ComplexAttribute || attribute is FixedListAttribute)
                {
                    result.AddRange(GetAliasPaths(attribute, string.Empty, string.Empty));
                }
            }
            return result;
        }

        private List<Entity.Attribute> GetAliasPaths(Api.Models.Attribute.Attribute attribute, string path, string pathUserFriendly)
        {
            var result = new List<Entity.Attribute>();
            var delimiter = string.IsNullOrEmpty(path) ? string.Empty : ".";
            if (attribute is FixedListAttribute fixedListAttribute)
            {
                result.AddRange(GetAliasPaths(fixedListAttribute.ReferencedAttribute, path + delimiter + fixedListAttribute.Uid, pathUserFriendly + delimiter + fixedListAttribute.Alias));
            }
            else if (attribute is ComplexAttribute complexAttribute)
            {
                result.Add(new Entity.Attribute
                {
                    Alias = pathUserFriendly + delimiter + attribute.Alias,
                    Uid = path + delimiter + attribute.Uid,
                });

                foreach (var subAttribute in complexAttribute.SubAttributes)
                {
                    result.AddRange(GetAliasPaths(subAttribute, path + delimiter + complexAttribute.Uid, pathUserFriendly + delimiter + complexAttribute.Alias));
                }
            }
            else
            {
                result.Add(new Entity.Attribute
                {
                    Alias = pathUserFriendly + delimiter + attribute.Alias,
                    Uid = path + delimiter + attribute.Uid,
                });
            }

            return result;
        }

        internal IEnumerable<ListItem> SearchProductPaged(SearchPagedModel searchModel)
        {
            return PIMClient().Products.SearchPaged(searchModel).ListItems;
        }

        internal IEnumerable<ListItem> SearchVariantPaged(SearchPagedModel searchModel)
        {
            return PIMClient().Variants.SearchPaged(searchModel).ListItems;
        }

        internal List<ProductAttributeValuesModel> GetProductsAttributeValues(List<int> productIds, List<Guid> attributeValueUids, List<string> cultureCodes = null, List<string> segments = null)
        {
            var productValuesRequestModel = new ProductValuesRequestModel()
            {
                ProductIds = productIds,
                Uids = attributeValueUids,
                IncludeValues = ValueIncludeMode.Uids,
                LimitToCultureCodes = cultureCodes,
                LimitToSegments = segments,
            };
            return PIMClient().Products.GetProductAttributeValues(productValuesRequestModel);
        }

        internal List<VariantAttributeValuesModel> GetVariantsAttributeValues(List<int> variantIds, List<Guid> attributeValueUids, List<string> cultureCodes = null, List<string> segments = null)
        {
            var variantValuesRequestModel = new VariantValuesRequestModel()
            {
                VariantIds = variantIds,
                Uids = attributeValueUids,
                IncludeValues = ValueIncludeMode.Uids,
                LimitToCultureCodes = cultureCodes,
                LimitToSegments = segments,
            };
            return PIMClient().Variants.GetVariantAttributeValues(variantValuesRequestModel);
        }

        internal LanguageModel GetLanguage(string languageIsoCode)
        {
            var languages = GetLanguages();
            LanguageModel? language = null;

            if (string.IsNullOrEmpty(languageIsoCode))
            {
                var integrationSettings = _settingsFacade.GetIntegrationSettings();
                language = languages.FirstOrDefault(x => x.CultureCode.Equals(integrationSettings.Setup?.DefaultLanguage, StringComparison.InvariantCultureIgnoreCase));
            }
            else if (!string.IsNullOrEmpty(languageIsoCode))
            {
                language = languages.FirstOrDefault(x => x.CultureCode.Equals(languageIsoCode, StringComparison.InvariantCultureIgnoreCase));
                
                if (language == null)
                {
                    var culture = CultureInfo.GetCultureInfo(languageIsoCode);
                    language = languages.FirstOrDefault(x => x.Name.Equals(culture.DisplayName, StringComparison.InvariantCultureIgnoreCase));
                }
            }

            if (language == null)
            {
                language = languages.First();
            }

            return language;
        }

        internal List<Guid> GetAttributeUidsFromScopeUids(IEnumerable<Guid> attributeScopeUids)
        {
            var result = new List<Guid>();
            var missingScopes = new List<Guid>();

            if (_attributesByScope.Value != null)
            {
                foreach (var scope in attributeScopeUids)
                {
                    if (!_attributesByScope.Value.ContainsKey(scope))
                        missingScopes.Add(scope);
                }
            }
            else
            {
                _attributesByScope.Value = new Dictionary<Guid, List<Guid>>();
                missingScopes.AddRange(attributeScopeUids);
            }

            if (missingScopes.Any())
            {
                foreach(var scope in PIMClient().Attributes.GetAttributeScopesAttributes(new AttributeScopeAttributesModel { AttributeScopeUids = missingScopes }))
                {
                    _attributesByScope.Value.Add(scope.Key, scope.Value);
                }
            }

            foreach (var scope in attributeScopeUids)
            {
                if (_attributesByScope.Value.TryGetValue(scope, out var attributeUids))
                    result.AddRange(attributeUids);
            }

            return result.Distinct().ToList();
        }

        internal ProductModel GetProduct(int productId)
        {
            var product = PIMClient().Products.GetProduct(productId);
            return product;
        }

        internal List<ProductModel> GetProducts(List<int> productIds)
        {
            var products = PIMClient().Products.GetProducts(productIds);
            return products;
        }

        internal List<Guid> GetVariationDefinitionDefiningAttributes(Guid productStructureUid, Guid variationDefinitionUid)
        {
            var productStructure = PIMClient().ProductStructures.GetProductStructure(productStructureUid);
            var variationDefinition = productStructure.VariationDefinitions.Where(vd => vd.Uid == variationDefinitionUid).FirstOrDefault();
            if (variationDefinition != null)
            {
                return variationDefinition.DefiningAttributes;
            }
            return new List<Guid> { };
        }

        internal List<Guid> GetVariationDefinitionAttributes(List<int> productIds)
        {
            var products = PIMClient().Products.GetProducts(productIds);
            var allowedProductStructures = products.Select(x => x.ProductStructureUid).Distinct().ToHashSet();
            var allowedVariationDefinitons = products.Where(x => x.VariationDefinitionUid.HasValue).Select(x => x.VariationDefinitionUid).Distinct().ToHashSet();

            var productStructures = PIMClient().ProductStructures.GetProductStructures().Where(x => allowedProductStructures.Contains(x.Uid)).ToList();
            var variationDefinitions = productStructures.Where(x => x.VariationDefinitions != null).SelectMany(x => x.VariationDefinitions.Where(y => allowedVariationDefinitons.Contains(y.Uid))).ToList();

            if (variationDefinitions.Any())
                return variationDefinitions.SelectMany(x => x.DefiningAttributes).Distinct().ToList();

            return new List<Guid> { };
        }

        internal List<VariantAttributeValuesModel> GetVariantsAttributeValuesByProductId(int productId, List<Guid> definingAttributeUids, List<string> cultureCodes = null, List<string> segments = null)
        {
            var variantIds = PIMClient().Products.GetVariantIds(productId);
            var variantValuesRequestModel = new VariantValuesRequestModel()
            {
                VariantIds = variantIds,
                Uids = definingAttributeUids,
                IncludeValues = ValueIncludeMode.Uids,
                LimitToCultureCodes = cultureCodes,
                LimitToSegments = segments,
            };
            return PIMClient().Variants.GetVariantAttributeValues(variantValuesRequestModel);
        }
    }
}
