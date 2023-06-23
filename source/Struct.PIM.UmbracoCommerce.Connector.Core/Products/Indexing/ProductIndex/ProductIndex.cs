using Examine.Lucene;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;
using IHostingEnvironment = Umbraco.Cms.Core.Hosting.IHostingEnvironment;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Indexing.ProductIndex
{
    public class ProductIndex : UmbracoExamineIndex
    {
        public ProductIndex(
            ILoggerFactory loggerFactory,
            string name,
            IOptionsMonitor<LuceneDirectoryIndexOptions> indexOptions,
            IHostingEnvironment hostingEnvironment,
            IRuntimeState runtimeState)
            : base(loggerFactory,
            name,
            indexOptions,
            hostingEnvironment,
            runtimeState)
        {
        }
    }
}