﻿using Microsoft.AspNetCore.Mvc;
using Struct.PIM.UmbracoCommerce.Connector.App_Plugins.Models;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings.Entity;
using Umbraco.Cms.Web.BackOffice.Controllers;
using Umbraco.Commerce.Core.Services;

namespace Struct.PIM.UmbracoCommerce.Connector.App_Plugins.ApiControllers
{
    [Route("umbraco/backoffice/structpimumbracocommerce")]
    public class StructPIMUmbracoCommerceApiController : UmbracoAuthorizedJsonController
    {
        private readonly SettingsFacade _settingsFacade;
        private readonly Core.Products.Services.ProductService _productService;

        public StructPIMUmbracoCommerceApiController(SettingsFacade settingsFacade, Core.Products.Services.ProductService productService)
        {
            _settingsFacade = settingsFacade;
            _productService = productService;
        }

        [HttpGet("GetAttributes")]
        public IActionResult GetAttributes(string type, string attributeType)
        {
            if (type == "Product")
            {
                var pimAttributes = _productService.GetAttributeWithProductReference();

                if (!string.IsNullOrEmpty(attributeType))
                    pimAttributes = pimAttributes.Where(x => x.Type == attributeType).ToList();

                return Ok(pimAttributes.OrderBy(a => a.Alias));

            }
            else if(type == "Variant")
            {
                var pimAttributes = _productService.GetAttributeWithVariantReference();

                if (!string.IsNullOrEmpty(attributeType))
                    pimAttributes = pimAttributes.Where(x => x.Type == attributeType).ToList();

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

        [HttpGet("GetCatalogues")]
        public IActionResult GetCatalogues()
        {
            var catalogues = _productService.GetCatalogues();
            return Ok(catalogues);
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

        [HttpGet("GetFilterAttributeValues")]
        public IActionResult GetFilterAttributeValues(string filter)
        {
            var attributeValues = _productService.GetGlobalListAttributeValues(Guid.Parse(filter));
            return Ok(attributeValues);
        }
    }
}