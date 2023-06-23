using Examine;
using Examine.Lucene;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Indexing.CategoryIndex
{
    public class ConfigureCategoryIndexOptions : IConfigureNamedOptions<LuceneDirectoryIndexOptions>
    {
        private readonly IOptions<IndexCreatorSettings> _settings;

        public ConfigureCategoryIndexOptions(IOptions<IndexCreatorSettings> settings)
            => _settings = settings;

        public void Configure(string? name, LuceneDirectoryIndexOptions options)
        {
            if (name?.Equals(IndexReferences.Category) is false)
            {
                return;
            }

            options.Analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);

            options.FieldDefinitions = new(
                new("id", FieldDefinitionTypes.Raw),
                new("name", FieldDefinitionTypes.FullText),
                new("language", FieldDefinitionTypes.Raw),
                new("store", FieldDefinitionTypes.Raw),
                new("parent", FieldDefinitionTypes.Integer)
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