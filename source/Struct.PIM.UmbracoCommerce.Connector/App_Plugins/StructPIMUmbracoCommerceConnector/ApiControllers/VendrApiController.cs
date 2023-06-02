using Microsoft.AspNetCore.Mvc;
using Struct.PIM.UmbracoCommerce.Connector.App_Plugins.StructPIMVendr.Models;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Helpers;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Services;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings.Entity;
using Umbraco.Cms.Web.BackOffice.Controllers;
using Vendr.Core.Services;

namespace Struct.PIM.UmbracoCommerce.Connector.App_Plugins.StructPIMShopifyConnector.ApiControllers
{
    [Route("umbraco/backoffice/vendr")]
    public class VendrApiController : UmbracoAuthorizedJsonController
    {
        private readonly SettingsFacade _settingsFacade;
        private readonly Core.Products.Services.ProductService _productService;

        public VendrApiController(SettingsFacade settingsFacade, Core.Products.Services.ProductService productService)
        {
            _settingsFacade = settingsFacade;
            _productService = productService;
        }

        [HttpGet("GetAttributes")]
        public IActionResult GetAttributes(string type)
        {
            if (type == "Product")
            {
                var pimAttributes = _productService.GetAttributeWithProductReference();
                return Ok(pimAttributes.OrderBy(a => a.Alias));

            }
            else if(type == "Variant")
            {
                var pimAttributes = _productService.GetAttributeWithVariantReference();
                return Ok(pimAttributes.OrderBy(a => a.Alias));
            }
            else
            {
                return BadRequest("StructureRef must be product or variant");
            }
        }

        [HttpGet("GetAttributeScopes")]
        public IActionResult GetAttributeScopes()
        {
            var pimScopes = _productService.GetAttributeScopes();
            return Ok(pimScopes);
        }

        [HttpGet("GetDimensions")]
        public IActionResult GetDimensions()
        {
            var dimensions = _productService.GetDimensions();
            return Ok(dimensions);
        }

        [HttpGet("GetIntegrationSettings")]
        public IActionResult GetIntegrationSettings()
        {
            var integrationSettings = _settingsFacade.GetIntegrationSettings(true);


            return Ok(integrationSettings);
        }

        [HttpPost("SaveGeneralSettings")]
        public IActionResult SaveGeneralSettings(GeneralSettingsSaveModel model)
        {
            _settingsFacade.SaveGeneralSettings(model.GeneralSettings, model.ShopSettingUid);
            return Ok();
        }

        [HttpPost("SaveProductMapping")]
        public IActionResult SaveProductMapping(ProductMapping model)
        {
            _settingsFacade.SaveProductMapping(model);
            return Ok();
        }

        [HttpPost("SaveVariantMapping")]
        public IActionResult SaveVariantMapping(VariantMapping model)
        {
            _settingsFacade.SaveVariantMapping(model);
            return Ok();
        }

        [HttpPost("SaveSetup")]
        public IActionResult SaveSetup(Setup model)
        {
            _settingsFacade.SaveSetup(model);
            return Ok();
        }

        [HttpGet("GetLanguages")]
        public IActionResult GetLanguages()
        {
            var languages = _productService.GetLanguages();
            return Ok(languages);
        }

        [HttpPost("SyncProductAttributes")]
        public IActionResult SyncProductAttributes()
        {
            //_productService.SyncTaxClasses();
            return Ok();
        }
    }
}