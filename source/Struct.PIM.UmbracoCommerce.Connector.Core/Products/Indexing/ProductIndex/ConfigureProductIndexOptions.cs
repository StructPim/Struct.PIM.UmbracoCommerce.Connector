using Examine;
using Examine.Lucene;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Indexing.ProductIndex
{
    public class ConfigureProductIndexOptions : IConfigureNamedOptions<LuceneDirectoryIndexOptions>
    {
        private readonly IOptions<IndexCreatorSettings> _settings;

        public ConfigureProductIndexOptions(IOptions<IndexCreatorSettings> settings)
            => _settings = settings;

        public void Configure(string? name, LuceneDirectoryIndexOptions options)
        {
            if (name?.Equals(IndexReferences.Product) is false)
            {
                return;
            }

            options.Analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);

            options.FieldDefinitions = new(
                new("id", FieldDefinitionTypes.Raw),
                new("name", FieldDefinitionTypes.FullText),
                new("language", FieldDefinitionTypes.Raw),
                new("productReference", FieldDefinitionTypes.Raw),
                new("store", FieldDefinitionTypes.Raw),
                new("sku", FieldDefinitionTypes.Raw),
                new("slug", FieldDefinitionTypes.Raw),
                new("primaryImageUrl", FieldDefinitionTypes.Raw),
                new("hasVariants", FieldDefinitionTypes.Raw),
                new("isGiftCard", FieldDefinitionTypes.Raw),
                new("searchableText", FieldDefinitionTypes.FullText), 
                new("stock", FieldDefinitionTypes.Integer),
                new("prices", FieldDefinitionTypes.Raw),
                new("properties", FieldDefinitionTypes.Raw),
                new("attributes", FieldDefinitionTypes.Raw),
                new("categories", FieldDefinitionTypes.Raw)
            );

            options.UnlockIndex = true;

            if (_settings.Value.LuceneDirectoryFactory == LuceneDirectoryFactory.SyncedTempFileSystemDirectoryFactory)
            {
                // if this directory factory is enabled then a snapshot deletion policy is required
                options.IndexDeletionPolicy = new SnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy());
            }
        }

        // not used
        public void Configure(LuceneDirectoryIndexOptions options) => throw new NotImplementedException();
    }
}