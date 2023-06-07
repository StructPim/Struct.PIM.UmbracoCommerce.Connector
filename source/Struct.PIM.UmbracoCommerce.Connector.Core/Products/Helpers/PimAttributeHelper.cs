using Newtonsoft.Json.Linq;
using Struct.PIM.Api.Models.Attribute;
using Struct.PIM.Api.Models.Language;
using Struct.PIM.Api.Models.Shared;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Helpers
{
    public class PimAttributeHelper
    {
        private readonly PimApiHelper _pimApiHelper;

        public PimAttributeHelper(PimApiHelper pimApiHelper)
        {
            _pimApiHelper = pimApiHelper;
        }

        internal PimAttributeValueDTO GetValueForAttribute(string attributeUid, HasAttributeValues entityValue, LanguageModel language, Dictionary<string, Tuple<string, string>> dimensionSegmentData, Api.Models.Attribute.Attribute rootAttribute = null)
        {
            var attributeUids = attributeUid.Split(".");
            if (rootAttribute == null)
            {
                rootAttribute = _pimApiHelper.GetPimAttribute(Guid.Parse(attributeUids[0]));
            }
            var thisAttribute = _pimApiHelper.GetPimAttribute(Guid.Parse(attributeUids[0]));
            var targetAttributeUid = attributeUids.Last();

            var fieldUid = GetAliasPath(rootAttribute, string.Empty, Guid.Parse(targetAttributeUid), language.CultureCode, true, rootAttribute is FixedListAttribute, true);
            var fieldAlias = fieldUid.Split(".").ToList();
            var alias = fieldAlias.First().Split("_").First();
            if (entityValue.Values.TryGetValue(alias, out dynamic values))
            {
                PimAttributeValueDTO value = new PimAttributeValueDTO();
                if (values != null)
                {
                    if (fieldAlias.Count > 1)
                    {
                        fieldAlias.RemoveAt(0);
                        var valuesDictionary = ((JObject)values).ToObject<Dictionary<string, object>>();
                        value = FindValue(fieldAlias.First(), valuesDictionary, fieldAlias, language.CultureCode, dimensionSegmentData);
                    }
                    else
                    {
                        var valuesDictionary = new Dictionary<string, object>();
                        var data = fieldAlias.First().Split("_");
                        valuesDictionary.Add(alias, values);
                        value = FindValue(fieldAlias.First(), valuesDictionary, fieldAlias, language.CultureCode, dimensionSegmentData);

                    }
                }
                return value;
            }
            else
            {
                throw new Exception("Mapping error. Could not find Attribute in PIM");
            }
        }

        internal string RenderAttribute(Api.Models.Attribute.Attribute rootAttribute, Api.Models.Attribute.Attribute attribute, HasAttributeValues variantValue, IEnumerable<PimAttribute> paths, LanguageModel language, string rootPath, Dictionary<string, Tuple<string, string>> dimensionSegmentData)
        {
            string renderValue = string.Empty;
            if (attribute is ComplexAttribute complexAttribute)
            {
                if (complexAttribute.RenderValuesForAttributeFieldUids != null && complexAttribute.RenderValuesForAttributeFieldUids.Any())
                {
                    foreach (var renderAttributeUid in complexAttribute.RenderValuesForAttributeFieldUids)
                    {
                        var renderAttribute = complexAttribute.SubAttributes.Where(sa => sa.Uid == renderAttributeUid).FirstOrDefault();
                        //var filteredPaths = paths.Where(p => paths.Any(pa => pa.Uid.EndsWith()));
                        if (renderAttribute != null && (renderAttribute is ComplexAttribute || renderAttribute is FixedListAttribute))
                        {
                            var newRootPath = !string.IsNullOrEmpty(rootPath) ? rootPath + "." + renderAttribute.Uid : renderAttribute.Uid.ToString();
                            renderValue += RenderAttribute(rootAttribute, renderAttribute, variantValue, paths.Where(p => p.Uid.StartsWith(newRootPath)), language, newRootPath, dimensionSegmentData);
                        }
                        else
                        {
                            var foundUid = paths.Where(t => t.Uid.Contains(complexAttribute.Uid + "." + renderAttributeUid.ToString())).FirstOrDefault();
                            if (foundUid != null)
                            {

                                PimAttributeValueDTO value = GetValueForAttribute(foundUid.Uid, variantValue, language, dimensionSegmentData, rootAttribute);
                                renderValue += value.Value + " ";
                            }
                        }
                    }
                }
                else
                {
                    foreach (var path in paths)
                    {
                        //Todo need some more
                        PimAttributeValueDTO value = GetValueForAttribute(path.Uid, variantValue, language, dimensionSegmentData, rootAttribute);
                        renderValue += value.Value + " ";
                    }
                }

            }
            else if (attribute is FixedListAttribute fixedListAttribute)
            {
                if (fixedListAttribute.ReferencedAttribute is ComplexAttribute)
                {
                    var newRootPath = !string.IsNullOrEmpty(rootPath) ? rootPath + "." + fixedListAttribute.ReferencedAttribute.Uid : fixedListAttribute.ReferencedAttribute.Uid.ToString();
                    renderValue += RenderAttribute(rootAttribute, fixedListAttribute.ReferencedAttribute, variantValue, paths.Where(p => p.Uid.StartsWith(newRootPath)), language, newRootPath, dimensionSegmentData);
                }
                else
                {
                    foreach (var path in paths)
                    {
                        //Todo need some more
                        PimAttributeValueDTO value = GetValueForAttribute(path.Uid, variantValue, language, dimensionSegmentData, rootAttribute);
                        renderValue += value.Value + " ";
                    }
                }
            }
            else
            {
                PimAttributeValueDTO value = GetValueForAttribute(attribute.Uid.ToString(), variantValue, language, dimensionSegmentData, rootAttribute);
                renderValue = value.Value;
            }

            return renderValue;
        }

        private PimAttributeValueDTO FindValue(string alias, Dictionary<string, object> values, List<string> fieldAlias, string cultureCode, Dictionary<string, Tuple<string, string>> dimensionSegmentData)
        {
            if (fieldAlias.Count > 1)
            {
                var data = alias.Split("_");
                if (data.Length == 3)
                {
                    if (values.TryGetValue(data[0], out var value))
                    {
                        if (value == null)
                        {
                            return new PimAttributeValueDTO();
                        }
                        // valueIsLocalized and valueIsSegmentedBydimensionUid
                        if (data[1] != "NA" && data[2] != "NA")
                        {
                            if (dimensionSegmentData.TryGetValue(data[2].ToLower(), out var datas))
                            {
                                var valueSegmentedBydimensionUid = ((JArray)value).Children<JObject>().FirstOrDefault(o => o["CultureCode"]?.ToString() == cultureCode && o["Dimension"]?.ToString() == datas.Item1 && o["Segment"]?.ToString() == datas.Item2)?.GetValue("Data");
                                fieldAlias.RemoveAt(0);
                                var valuesDictionary2 = ((JObject)valueSegmentedBydimensionUid).ToObject<Dictionary<string, object>>();
                                return FindValue(fieldAlias.First(), valuesDictionary2, fieldAlias, cultureCode, dimensionSegmentData);
                            }
                            else
                            {
                            }
                        }
                        // valueIsLocalized
                        else if (data[1] != "NA")
                        {
                            if (dimensionSegmentData.TryGetValue(data[2].ToLower(), out var datas))
                            {
                                var valueSegmentedBydimensionUid = ((JArray)value).Children<JObject>().FirstOrDefault(o => o["CultureCode"]?.ToString() == cultureCode)?.GetValue("Data");
                                fieldAlias.RemoveAt(0);
                                var valuesDictionary2 = ((JObject)valueSegmentedBydimensionUid).ToObject<Dictionary<string, object>>();
                                return FindValue(fieldAlias.First(), valuesDictionary2, fieldAlias, cultureCode, dimensionSegmentData);
                            }
                            else
                            {
                            }
                        }
                        //valueIsSegmentedBydimensionUid
                        else if (data[2] != "NA")
                        {
                            if (dimensionSegmentData.TryGetValue(data[2].ToLower(), out var datas))
                            {
                                var valueSegmentedBydimensionUid = ((JArray)value).Children<JObject>().FirstOrDefault(o => o["Dimension"]?.ToString() == datas.Item1 && o["Segment"]?.ToString() == datas.Item2)?.GetValue("Data");
                                fieldAlias.RemoveAt(0);
                                var valuesDictionary2 = ((JObject)valueSegmentedBydimensionUid).ToObject<Dictionary<string, object>>();
                                return FindValue(fieldAlias.First(), valuesDictionary2, fieldAlias, cultureCode, dimensionSegmentData);
                            }
                            else
                            {
                            }

                        }
                        //standard value
                        else
                        {
                            fieldAlias.RemoveAt(0);
                            var valuesDictionary = ((JObject)value).ToObject<Dictionary<string, object>>();
                            return FindValue(fieldAlias.First(), valuesDictionary, fieldAlias, cultureCode, dimensionSegmentData);
                        }
                    }
                }
                else
                {
                    if (values.TryGetValue(alias, out var value))
                    {
                        if (value == null)
                        {
                            return new PimAttributeValueDTO();
                        }
                        fieldAlias.RemoveAt(0);
                        var valuesDictionary = ((JObject)value).ToObject<Dictionary<string, object>>();
                        return FindValue(fieldAlias.First(), valuesDictionary, fieldAlias, cultureCode, dimensionSegmentData);
                    }
                }
            }
            else
            {
                var data = alias.Split("_");
                if (data.Length < 3)
                {
                    throw new Exception("Error in data. Must have 3 values");
                }
                if (values.TryGetValue(data[0], out var value))
                {
                    // valueIsLocalized and valueIsSegmentedBydimensionUid
                    if (data[1] != "NA" && data[2] != "NA")
                    {
                        if (dimensionSegmentData.TryGetValue(data[2].ToLower(), out var datas))
                        {
                            var valueSegmentedBydimensionUid = ((JArray)value).Children<JObject>().FirstOrDefault(o => o["CultureCode"]?.ToString() == cultureCode && o["Dimension"]?.ToString() == datas.Item1 && o["Segment"]?.ToString() == datas.Item2)?.GetValue("Data")?.ToString();
                            return new PimAttributeValueDTO
                            {
                                Value = valueSegmentedBydimensionUid,
                                Alias = Guid.NewGuid().ToString(),
                            };
                        }
                        else
                        {
                            return new PimAttributeValueDTO
                            {
                                Value = null,
                                Alias = Guid.NewGuid().ToString(),
                            };
                        }
                    }
                    // valueIsLocalized
                    else if (data[1] != "NA")
                    {
                        var valueLocalized = ((JArray)value).Children<JObject>().FirstOrDefault(o => o["CultureCode"]?.ToString() == cultureCode)?.GetValue("Data")?.ToString();
                        return new PimAttributeValueDTO
                        {
                            Value = valueLocalized,
                            Alias = Guid.NewGuid().ToString(),
                        };
                    }
                    //valueIsSegmentedBydimensionUid
                    else if (data[2] != "NA")
                    {
                        if (dimensionSegmentData.TryGetValue(data[2].ToLower(), out var datas))
                        {
                            var valueSegmentedBydimensionUid = ((JArray)value).Children<JObject>().FirstOrDefault(o => o["Dimension"]?.ToString() == datas.Item1 && o["Segment"]?.ToString() == datas.Item2)?.GetValue("Data")?.ToString();
                            return new PimAttributeValueDTO
                            {
                                Value = valueSegmentedBydimensionUid,
                                Alias = Guid.NewGuid().ToString(),
                            };
                        }
                        else
                        {
                            return new PimAttributeValueDTO
                            {
                                Value = null,
                                Alias = Guid.NewGuid().ToString(),
                            };
                        }

                    }
                    //standard value
                    else
                    {
                        return new PimAttributeValueDTO
                        {
                            Value = value?.ToString() ?? throw new Exception($"Error in data. value not found for alias {alias}"),
                            Alias = Guid.NewGuid().ToString(),
                        };
                    }
                }
                else
                {
                    throw new Exception($"Error in data. value not found for alias {alias}");
                }
            }


            throw new Exception("Mapping not found for product");
        }

        public string GetAliasPath(Api.Models.Attribute.Attribute attribute, string pathUserFriendly, Guid targetAttributeUid, string? language, bool allLevels, bool previousIsFixedList, bool showLanguageAndSegmentAllLevels)
        {
            var delimiter = string.IsNullOrEmpty(pathUserFriendly) ? string.Empty : ".";
            var attributeLanguage = language;
            if (!attribute.Localized)
            {
                attributeLanguage = null;
            }
            string segmentUid = null;
            if (attribute.DimensionUid != null)
            {
                segmentUid = attribute.DimensionUid.ToString();
            }
            var languageSegment = $"_{attributeLanguage ?? "NA"}_{segmentUid?.ToString() ?? "NA"}";

            if (attribute is FixedListAttribute fixedListAttribute)
            {
                if (attribute.Uid == targetAttributeUid)
                    return pathUserFriendly + delimiter + attribute.Alias;

                var path = GetAliasPath(fixedListAttribute.ReferencedAttribute, pathUserFriendly + delimiter + fixedListAttribute.Alias + (showLanguageAndSegmentAllLevels ? languageSegment : string.Empty), targetAttributeUid, language, allLevels, true, showLanguageAndSegmentAllLevels);

                if (!string.IsNullOrEmpty(path))
                {
                    return path;
                }
            }
            else if (attribute is ComplexAttribute complexAttribute)
            {
                if (attribute.Uid == targetAttributeUid)
                    return pathUserFriendly + delimiter + attribute.Alias;

                foreach (var subAttribute in complexAttribute.SubAttributes)
                {
                    var path = GetAliasPath(subAttribute, allLevels && !previousIsFixedList ? pathUserFriendly + delimiter + complexAttribute.Alias + (showLanguageAndSegmentAllLevels ? languageSegment : string.Empty) : pathUserFriendly, targetAttributeUid, language, allLevels, false, showLanguageAndSegmentAllLevels);

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
                    return pathUserFriendly + delimiter + attribute.Alias + languageSegment;
                }
                return string.Empty;
            }

            return string.Empty;
        }
    }
}
