using Struct.PIM.Api.Models.Attribute;
using Struct.PIM.Api.Models.Language;
using Struct.PIM.Api.Models.Product;
using Struct.PIM.Api.Models.Shared;
using Struct.PIM.Api.Models.Variant;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings;
using System.Globalization;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Helpers
{
    public class PimApiHelper
    {
        private readonly SettingsFacade _settingsFacade;

        public PimApiHelper(SettingsFacade settingsFacade)
        {
            _settingsFacade = settingsFacade;
        }

        protected PIM.Api.Client.StructPIMApiClient PIMClient()
        {
            var integrationSettings = _settingsFacade.GetIntegrationSettings();
            if (string.IsNullOrEmpty(integrationSettings.Setup.PimApiUrl))
                throw new InvalidOperationException("StructPIM.ApiUrl must be set in settings to use Struct PIM Umbraco Commerce");

            if (string.IsNullOrEmpty(integrationSettings.Setup.PimApiKey))
                throw new InvalidOperationException("StructPIM.ApiKey must be set in settings to use Struct PIM Umbraco Commerce");
            
            return new PIM.Api.Client.StructPIMApiClient(integrationSettings.Setup.PimApiUrl, integrationSettings.Setup.PimApiKey);
        }

        public List<Api.Models.Attribute.Attribute> GetPimAttributes()
        {
            var attributes = PIMClient().Attributes.GetAttributes();

            return attributes;
        }

        public List<Api.Models.Language.LanguageModel> GetLanguages()
        {
            var languages = PIMClient().Languages.GetLanguages();

            return languages;
        }

        public List<Api.Models.Attribute.AttributeScope> GetPimAttributeScopes()
        {
            var attributeScopes = PIMClient().Attributes.GetAttributeScopes();

            return attributeScopes.OrderBy(x => x.Alias).ToList();
        }

        public List<Api.Models.Dimension.DimensionModel> GetPimDimensions()
        {
            var dimensions = PIMClient().Dimensions.GetDimensions();

            return dimensions.OrderBy(x => x.Alias).ToList();
        }

        public List<PimAttribute> GetAttributeWithProductReference()
        {
            var attributes = GetPimAttributes();

            //var productAttributeUids = PIMClient().ProductStructures.GetProductStructures()
            //    .SelectMany(x => x.ProductConfiguration.Tabs?
            //        .SelectMany(y => (y as DynamicTabSetup)?.Sections?
            //            .SelectMany(z => (z as DynamicSectionSetup)?.Properties?
            //                .Select(n => (n as AttributeSetup)?.AttributeUid))))?.ToHashSet();
                            
            //if(productAttributeUids?.Any() ?? false)
            //{
            //    attributes = attributes.Where(x => productAttributeUids.Contains(x.Uid)).ToList();
            //}

            var mappedAttributes = Map(attributes);
            return mappedAttributes;
        }

        public List<PimAttribute> GetAttributeWithVariantReference()
        {
            var attributes = GetPimAttributes();

            //var variantAttributeUids = PIMClient().ProductStructures.GetProductStructures()
            //    .SelectMany(x => x.VariantConfiguration.Tabs?
            //        .SelectMany(y => (y as DynamicTabSetup)?.Sections?
            //            .SelectMany(z => (z as DynamicSectionSetup)?.Properties?
            //                .Select(n => (n as AttributeSetup)?.AttributeUid))))?.ToHashSet();

            //if (variantAttributeUids?.Any() ?? false)
            //{
            //    attributes = attributes.Where(x => variantAttributeUids.Contains(x.Uid)).ToList();
            //}

            var mappedAttributes = Map(attributes);
            return mappedAttributes;
        }

        internal List<PimAttribute> Map(List<Api.Models.Attribute.Attribute> attributes)
        {
            var result = new List<PimAttribute>();
            foreach (var attribute in attributes)
            {
                // vi kigger ikke på list attibutter til at starte med.
                if (attribute is ListAttribute)
                {
                    continue;
                }
                else if (attribute is ComplexAttribute || attribute is FixedListAttribute)
                {
                    result.AddRange(GetAliasPaths(attribute, string.Empty, string.Empty));
                }
                else
                {
                    result.Add(new PimAttribute
                    {
                        Alias = attribute.Alias,
                        Uid = attribute.Uid.ToString(),
                    });
                }
            }
            return result;
        }

        private List<PimAttribute> GetAliasPaths(Api.Models.Attribute.Attribute attribute, string path, string pathUserFreindly)
        {
            var result = new List<PimAttribute>();
            var delimiter = string.IsNullOrEmpty(path) ? string.Empty : ".";
            if (attribute is FixedListAttribute fixedListAttribute)
            {
                result.AddRange(GetAliasPaths(fixedListAttribute.ReferencedAttribute, path + delimiter + fixedListAttribute.Uid, pathUserFreindly + delimiter + fixedListAttribute.Alias));
            }
            else if (attribute is ComplexAttribute complexAttribute)
            {
                foreach (var subAttribute in complexAttribute.SubAttributes)
                {
                    result.AddRange(GetAliasPaths(subAttribute, path + delimiter + complexAttribute.Uid, pathUserFreindly + delimiter + complexAttribute.Alias));
                }
            }
            else
            {
                result.Add(new PimAttribute
                {
                    Alias = pathUserFreindly + delimiter + attribute.Alias,
                    Uid = path + delimiter + attribute.Uid,
                });
            }

            return result;
        }

        public string GetAliasPath(Api.Models.Attribute.Attribute attribute, string pathUserFreindly, Guid targetAttributeUid, string language, bool allLevels, bool previousIsFixedList)
        {
            var delimiter = string.IsNullOrEmpty(pathUserFreindly) ? string.Empty : ".";

            if (attribute is FixedListAttribute fixedListAttribute)
            {
                if (attribute.Uid == targetAttributeUid)
                    return pathUserFreindly + delimiter + attribute.Alias;

                var path = GetAliasPath(fixedListAttribute.ReferencedAttribute, pathUserFreindly + delimiter + fixedListAttribute.Alias, targetAttributeUid, language, allLevels, true);

                if (!string.IsNullOrEmpty(path))
                {
                    return path;
                }
            }
            else if (attribute is ComplexAttribute complexAttribute)
            {
                if (attribute.Uid == targetAttributeUid)
                    return pathUserFreindly + delimiter + attribute.Alias;

                foreach (var subAttribute in complexAttribute.SubAttributes)
                {
                    var path = GetAliasPath(subAttribute, allLevels && !previousIsFixedList ? pathUserFreindly + delimiter + complexAttribute.Alias : pathUserFreindly, targetAttributeUid, language, allLevels, false);

                    if (!string.IsNullOrEmpty(path))
                    {
                        return path;
                    }

                }
            }
            else
            {
                if (attribute.Uid == targetAttributeUid)
                {
                    if (!attribute.Localized)
                    {
                        language = null;
                    }
                    string segmentUid = null;
                    if (attribute.DimensionUid != null)
                    {
                        segmentUid = attribute.DimensionUid.ToString();
                    }
                    var languageSegment = $"_{language ?? "NA"}_{segmentUid?.ToString() ?? "NA"}";
                    return pathUserFreindly + delimiter + attribute.Alias + languageSegment;
                }
                return string.Empty;
            }

            return string.Empty;
        }

        internal Api.Models.Attribute.Attribute GetPimAttribute(Guid attributeUid)
        {
            return PIMClient().Attributes.GetAttribute(attributeUid);
        }

        internal Api.Models.Attribute.Attribute? GetPimAttribute(string alias)
        {
            return PIMClient().Attributes.GetAttributes().FirstOrDefault(x => x.Alias == alias);
        }

        internal List<Api.Models.Attribute.Attribute> GetPimAttributes(List<Guid> attributeUids)
        {
            return PIMClient().Attributes.GetAttributes(attributeUids);
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

        internal LanguageModel? GetLanguage(string languageIsoCode)
        {
            var languages = PIMClient().Languages.GetLanguages();
            LanguageModel language = null;

            if (languageIsoCode == null)
            {
                language = languages.FirstOrDefault();
            }
            if (language == null)
            {
                language = languages.FirstOrDefault(x => x.CultureCode.Equals(languageIsoCode, StringComparison.InvariantCultureIgnoreCase));
            }
            if (language == null)
            {
                var culture = CultureInfo.GetCultureInfo(languageIsoCode);
                language = languages.FirstOrDefault(x => x.Name.Equals(culture.DisplayName, StringComparison.InvariantCultureIgnoreCase));
            }
            if (language == null)
            {
                var integrationSettings = _settingsFacade.GetIntegrationSettings();
                language = languages.FirstOrDefault(x => x.CultureCode.Equals(integrationSettings.Setup?.DefaultLanguage, StringComparison.InvariantCultureIgnoreCase));
            }
            if (language == null)
            {
                language = languages.FirstOrDefault();
            }

            return language;
        }

        internal List<Guid> GetAttributeUidsFromScopeUids(IEnumerable<Guid> attributeScopeUids)
        {
            var reuslt = PIMClient().Attributes.GetAttributeScopesAttributes(new AttributeScopeAttributesModel { AttributeScopeUids = attributeScopeUids.ToList() });
            var items = reuslt.SelectMany(d => d.Value).Distinct().ToList();
            return items;
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
            
            if(variationDefinitions.Any())
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
