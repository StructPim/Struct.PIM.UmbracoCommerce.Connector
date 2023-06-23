using Microsoft.AspNetCore.Mvc;
using Struct.PIM.UmbracoCommerce.Connector.App_Plugins.Models;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Services;
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
        private readonly ConfigurationService _configurationService;
        private readonly AttributeService _attributeService;
        private readonly CategoryService _categoryService;
        private readonly GlobalListService _globalListService;

        public StructPIMUmbracoCommerceApiController(SettingsFacade settingsFacade, Core.Products.Services.ProductService productService, CategoryService categoryService, ConfigurationService configurationService, AttributeService attributeService, GlobalListService globalListService)
        {
            _settingsFacade = settingsFacade;
            _productService = productService;
            _categoryService = categoryService;
            _configurationService = configurationService;
            _attributeService = attributeService;
            _globalListService = globalListService;
        }

        [HttpGet("GetAttributes")]
        public IActionResult GetAttributes(string type, string attributeType)
        {
            if (type == "Product")
            {
                var pimAttributes = _attributeService.GetAttributeWithProductReference();

                if (!string.IsNullOrEmpty(attributeType))
                    pimAttributes = pimAttributes.Where(x => x.Type == attributeType).ToList();

                return Ok(pimAttributes.OrderBy(a => a.Alias));

            }
            else if(type == "Variant")
            {
                var pimAttributes = _attributeService.GetAttributeWithVariantReference();

                if (!string.IsNullOrEmpty(attributeType))
                    pimAttributes = pimAttributes.Where(x => x.Type == attributeType).ToList();

                return Ok(pimAttributes.OrderBy(a => a.Alias));
            }
            else if (type == "Category")
            {
                var pimAttributes = _attributeService.GetAttributeWithCategoryReference();

                if (!string.IsNullOrEmpty(attributeType))
                    pimAttributes = pimAttributes.Where(x => x.Type == attributeType).ToList();

                return Ok(pimAttributes.OrderBy(a => a.Alias));
            }
            else
            {
                return BadRequest("StructureRef must be product or variant or category");
            }
        }

        [HttpGet("GetAttributeScopes")]
        public IActionResult GetAttributeScopes()
        {
            var pimScopes = _attributeService.GetAttributeScopes();
            return Ok(pimScopes);
        }

        [HttpGet("GetDimensions")]
        public IActionResult GetDimensions()
        {
            var dimensions = _configurationService.GetDimensions();
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
            var catalogues = _categoryService.GetCatalogues();
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

        [HttpPost("SaveCategoryMapping")]
        public IActionResult SaveCategoryMapping(VariantMapping model)
        {
            _settingsFacade.SaveCategoryMapping(model);
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
            var languages = _configurationService.GetLanguages();
            return Ok(languages);
        }

        [HttpPost("SyncProductAttributes")]
        public IActionResult SyncProductAttributes()
        {
            //_productService.SyncTaxClasses();
            return Ok();
        }

        [HttpGet("GetFilterAttributeValues")]
        public IActionResult GetFilterAttributeValues(string filter, Guid storeId)
        {
            var attributeValues = _globalListService.GetGlobalListAttributeValues(Guid.Parse(filter), storeId);
            return Ok(attributeValues);
        }

        [HttpGet("IsSetupValid")]
        public IActionResult IsSetupValid()
        {
            return Ok(_settingsFacade.IsSetupValid());
        }

    }
}