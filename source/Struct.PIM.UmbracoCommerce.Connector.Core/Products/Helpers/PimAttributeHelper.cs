using Newtonsoft.Json.Linq;
using Struct.PIM.Api.Models.Attribute;
using Struct.PIM.Api.Models.Language;
using Struct.PIM.Api.Models.Shared;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity;
using Umbraco.Commerce.Common.Logging;
using Attribute = Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity.Attribute;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Helpers
{
    public class PimAttributeHelper
    {
        private readonly PimApiHelper _pimApiHelper;

        public PimAttributeHelper(PimApiHelper pimApiHelper)
        {
            _pimApiHelper = pimApiHelper;
        }

        internal AttributeValue<T> GetValue<T>(string attributeUid, Dictionary<string, dynamic> entityValue, LanguageModel language, Dictionary<string, Tuple<string, string>> dimensionSegmentData, Api.Models.Attribute.Attribute rootAttribute = null)
        {
            var attributeUids = attributeUid.Split(".");
            var attributes = _pimApiHelper.GetAttributes();
            if (!attributes.TryGetValue(Guid.Parse(attributeUids[0]), out var thisAttribute))
                return new AttributeValue<T>();

            if (rootAttribute == null)
            {
                rootAttribute = thisAttribute;
            }
            var targetAttributeUid = attributeUids.Last();

            var fieldUid = GetAliasPath(rootAttribute, string.Empty, Guid.Parse(targetAttributeUid), language.CultureCode, true, rootAttribute is FixedListAttribute, true);
            var fieldAlias = fieldUid.Split(".").ToList();
            var alias = fieldAlias.First().Split("_").First();
            if (entityValue.TryGetValue(alias, out dynamic values))
            {
                AttributeValue<T> value = new AttributeValue<T>();
                if (values != null)
                {
                    if (fieldAlias.Count > 1)
                    {
                        fieldAlias.RemoveAt(0);
                        var valuesDictionary = ((JObject)values).ToObject<Dictionary<string, object>>();
                        value = FindValue<T>(fieldAlias.First(), valuesDictionary, fieldAlias, language.CultureCode, dimensionSegmentData);
                    }
                    else
                    {
                        var valuesDictionary = new Dictionary<string, object>();
                        var data = fieldAlias.First().Split("_");
                        valuesDictionary.Add(alias, values);
                        value = FindValue<T>(fieldAlias.First(), valuesDictionary, fieldAlias, language.CultureCode, dimensionSegmentData);

                    }
                }
                return value;
            }
            else
                return new AttributeValue<T>();
        }

        internal string RenderRootAttribute(Api.Models.Attribute.Attribute rootAttribute, Dictionary<string, dynamic> variantValue, LanguageModel language, Dictionary<string, Tuple<string, string>> dimensionSegmentData)
        {
            return RenderAttribute(rootAttribute, rootAttribute, variantValue, _pimApiHelper.Map(new List<Api.Models.Attribute.Attribute> { rootAttribute }), language, rootAttribute.Uid.ToString(), dimensionSegmentData);
        }

        internal string RenderAttribute(Api.Models.Attribute.Attribute rootAttribute, Api.Models.Attribute.Attribute attribute, Dictionary<string, dynamic> variantValue, IEnumerable<Attribute> paths, LanguageModel language, string rootPath, Dictionary<string, Tuple<string, string>> dimensionSegmentData)
        {
            string renderValue = string.Empty;
            if (attribute is ComplexAttribute complexAttribute)
            {
                var renderForAttributeFieldUids = new List<Guid>();

                if (complexAttribute.RenderValuesForAttributeFieldUids != null && complexAttribute.RenderValuesForAttributeFieldUids.Any())
                {
                    renderForAttributeFieldUids = complexAttribute.RenderValuesForAttributeFieldUids;
                }
                else
                {
                    renderForAttributeFieldUids = complexAttribute.SubAttributes.Select(x => x.Uid).ToList();
                }

                foreach (var renderAttributeUid in renderForAttributeFieldUids)
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

                            var value = GetValue<string>(foundUid.Uid, variantValue, language, dimensionSegmentData, rootAttribute);
                            renderValue += value?.Value + " ";
                        }
                    }
                }
                //}
                //else
                //{
                //    foreach (var path in paths)
                //    {
                //        //Todo need some more
                //        var value = GetValue(path.Uid, variantValue, language, dimensionSegmentData, rootAttribute);
                //        renderValue += value?.Value + " ";
                //    }
                //}

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
                        var value = GetValue<string>(path.Uid, variantValue, language, dimensionSegmentData, rootAttribute);
                        renderValue += value?.Value + " ";
                    }
                }
            }
            else
            {
                var value = GetValue<string>(attribute.Uid.ToString(), variantValue, language, dimensionSegmentData, rootAttribute);
                renderValue = value?.Value;
            }

            return renderValue;
        }

        private AttributeValue<T?> FindValue<T>(string alias, Dictionary<string, object> values, List<string> fieldAlias, string cultureCode, Dictionary<string, Tuple<string, string>> dimensionSegmentData)
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
                            return new AttributeValue<T?>();
                        }
                        // valueIsLocalized and valueIsSegmentedBydimensionUid
                        if (data[1] != "NA" && data[2] != "NA")
                        {
                            if (dimensionSegmentData.TryGetValue(data[2].ToLower(), out var datas))
                            {
                                var valueSegmentedBydimensionUid = ((JArray)value).Children<JObject>().FirstOrDefault(o => o["CultureCode"]?.ToString() == cultureCode && o["Dimension"]?.ToString() == datas.Item1 && o["Segment"]?.ToString() == datas.Item2)?.GetValue("Data");
                                fieldAlias.RemoveAt(0);
                                var valuesDictionary2 = ((JObject)valueSegmentedBydimensionUid).ToObject<Dictionary<string, object>>();
                                return FindValue<T>(fieldAlias.First(), valuesDictionary2, fieldAlias, cultureCode, dimensionSegmentData);
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
                                return FindValue<T>(fieldAlias.First(), valuesDictionary2, fieldAlias, cultureCode, dimensionSegmentData);
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
                                return FindValue<T?>(fieldAlias.First(), valuesDictionary2, fieldAlias, cultureCode, dimensionSegmentData);
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
                            return FindValue<T?>(fieldAlias.First(), valuesDictionary, fieldAlias, cultureCode, dimensionSegmentData);
                        }
                    }
                }
                else
                {
                    if (values.TryGetValue(alias, out var value))
                    {
                        if (value == null)
                        {
                            return new AttributeValue<T?>();
                        }
                        fieldAlias.RemoveAt(0);
                        var valuesDictionary = ((JObject)value).ToObject<Dictionary<string, object>>();
                        return FindValue<T>(fieldAlias.First(), valuesDictionary, fieldAlias, cultureCode, dimensionSegmentData);
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
                            var valueSegmentedBydimensionUid = ((JArray)value).Children<JObject>().FirstOrDefault(o => o["CultureCode"]?.ToString() == cultureCode && o["Dimension"]?.ToString() == datas.Item1 && o["Segment"]?.ToString() == datas.Item2)?.GetValue("Data");
                            return new AttributeValue<T?>
                            {
                                Value = valueSegmentedBydimensionUid != null ? valueSegmentedBydimensionUid.ToObject<T>() : default,
                                Alias = Guid.NewGuid().ToString(),
                            };
                        }
                        else
                        {
                            return new AttributeValue<T?>
                            {
                                Value = default,
                                Alias = Guid.NewGuid().ToString(),
                            };
                        }
                    }
                    // valueIsLocalized
                    else if (data[1] != "NA")
                    {
                        var valueLocalized = ((JArray)value).Children<JObject>().FirstOrDefault(o => o["CultureCode"]?.ToString() == cultureCode)?.GetValue("Data");
                        return new AttributeValue<T?>
                        {
                            Value = valueLocalized != null ? valueLocalized.ToObject<T>() : default,
                            Alias = Guid.NewGuid().ToString(),
                        };
                    }
                    //valueIsSegmentedBydimensionUid
                    else if (data[2] != "NA")
                    {
                        if (dimensionSegmentData.TryGetValue(data[2].ToLower(), out var datas))
                        {
                            var valueSegmentedBydimensionUid = ((JArray)value).Children<JObject>().FirstOrDefault(o => o["Dimension"]?.ToString() == datas.Item1 && o["Segment"]?.ToString() == datas.Item2)?.GetValue("Data");
                            return new AttributeValue<T?>
                            {
                                Value = valueSegmentedBydimensionUid != null ? valueSegmentedBydimensionUid.ToObject<T>() : default,
                                Alias = Guid.NewGuid().ToString(),
                            };
                        }
                        else
                        {
                            return new AttributeValue<T?>
                            {
                                Value = default,
                                Alias = Guid.NewGuid().ToString(),
                            };
                        }

                    }
                    //standard value
                    else
                    {
                        return new AttributeValue<T?>
                        {
                            Value = value != null && value is T ? value.SafeCast<T>() : default,
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

        public string GetAliasPath(string attributeUidPath, string cultureCode, bool byGlobalListValueUid = false)
        {
            var attributeUids = attributeUidPath.Split(".");
            var rootAttribute = _pimApiHelper.GetAttribute(Guid.Parse(attributeUids[0]));
            var path = GetAliasPath(rootAttribute, string.Empty, Guid.Parse(attributeUids.Last()), cultureCode, true, false, false);

            if (byGlobalListValueUid) path += "_GlobalListUids";

            return path;
        }

        private string GetAliasPath(Api.Models.Attribute.Attribute attribute, string pathUserFriendly, Guid targetAttributeUid, string? language, bool allLevels, bool previousIsFixedList, bool showLanguageAndSegmentAllLevels)
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
                    return path;
            }
            else if (attribute is ComplexAttribute complexAttribute)
            {
                if (attribute.Uid == targetAttributeUid)
                {
                    if (previousIsFixedList)
                        return pathUserFriendly;

                    return pathUserFriendly + delimiter + attribute.Alias;
                }

                foreach (var subAttribute in complexAttribute.SubAttributes)
                {
                    var path = GetAliasPath(subAttribute, allLevels && !previousIsFixedList ? pathUserFriendly + delimiter + complexAttribute.Alias + (showLanguageAndSegmentAllLevels ? languageSegment : string.Empty) : pathUserFriendly, targetAttributeUid, language, allLevels, false, showLanguageAndSegmentAllLevels);

                    if (!string.IsNullOrEmpty(path))
                        return path;
                }
            }
            else
            {
                if (attribute.Uid == targetAttributeUid)
                {
                    if(previousIsFixedList)
                        return pathUserFriendly;
                    
                    return pathUserFriendly + delimiter + attribute.Alias + languageSegment;
                }
                return string.Empty;
            }

            return string.Empty;
        }

        private string AppendUidPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            return path + "_uids";
        }
    }
}
