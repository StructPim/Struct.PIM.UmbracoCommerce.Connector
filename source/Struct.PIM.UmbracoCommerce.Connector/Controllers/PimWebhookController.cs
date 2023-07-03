using Examine;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Indexing;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Indexing.CategoryIndex;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Indexing.ProductIndex;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Services;
using Struct.PIM.UmbracoCommerce.Connector.Models.PimWebhook;
using System.Net;
using Umbraco.Cms.Web.Common.Controllers;

namespace Struct.PIM.UmbracoCommerce.Connector.Controllers
{
    [AllowAnonymous]
    public class PimWebhookController : UmbracoApiController
    {
        private IndexService _indexService;
        
        public PimWebhookController(IndexService indexService)
        {
            _indexService = indexService;
        }

        [HttpPost]
        public bool HandleProductUpdate(ProductModel model)
        {
            _indexService.UpdateProducts(model.ProductIds);

            return true;
        }

        [HttpPost]
        public bool HandleVariantUpdate(VariantModel model)
        {
            _indexService.UpdateVariants(model.VariantIds);
            
            return true;
        }

        [HttpPost]
        public bool HandleCategoryUpdate(CategoryModel model)
        {
            _indexService.UpdateCategories(model.CategoryIds);

            return true;
        }

        [HttpPost]
        public bool HandleGlobalListUpdate(List<GlobalListValueModel> model)
        {
            var globalListValues = model.GroupBy(x => x.GlobalListId).ToDictionary(x => x.Key, x => x.Select(y => y.Uid).ToList());
            _indexService.UpdateIndexesByGlobalListValues(globalListValues);

            return true;
        }
    }
}
