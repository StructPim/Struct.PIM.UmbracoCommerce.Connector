using System;
using System.Collections.Generic;
using global::Umbraco.Cms.Core.Actions;
using global::Umbraco.Cms.Core.Events;
using global::Umbraco.Cms.Core.Models.Trees;
using global::Umbraco.Cms.Core.Services;
using global::Umbraco.Cms.Core.Trees;
using global::Umbraco.Cms.Core;
using global::Umbraco.Cms.Web.BackOffice.Trees;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Polly;
using Umbraco.Cms.Web.Common.Attributes;
using System.Runtime.CompilerServices;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings;

namespace Struct.PIM.UmbracoCommerce.Connector.SectionTree
{
    [PluginController("StructPIMUmbracoCommerceConnector")]
    [Authorize(Policy = "SectionAccessCommerce")]
    [Tree("settings", "structpimsettings", TreeTitle = "StructPIM", TreeGroup = "pimSettingsGroup", SortOrder = 5)]
    public class StructPIMUmbracoCommerceConnectorTreeController : TreeController
    {

        private readonly IMenuItemCollectionFactory _menuItemCollectionFactory;
        private readonly SettingsFacade _settingsFacade;

        public StructPIMUmbracoCommerceConnectorTreeController(ILocalizedTextService localizedTextService,
            UmbracoApiControllerTypeCollection umbracoApiControllerTypeCollection,
            IMenuItemCollectionFactory menuItemCollectionFactory,
            IEventAggregator eventAggregator,
            SettingsFacade settingsFacade)
            : base(localizedTextService, umbracoApiControllerTypeCollection, eventAggregator)
        {
            _menuItemCollectionFactory = menuItemCollectionFactory ?? throw new ArgumentNullException(nameof(menuItemCollectionFactory));
            _settingsFacade = settingsFacade;
        }

        protected override ActionResult<TreeNodeCollection> GetTreeNodes(string id, FormCollection queryStrings)
        {
            var nodes = new TreeNodeCollection();

            if (id == Constants.System.Root.ToInvariantString())
            {
                if (!string.IsNullOrEmpty(_settingsFacade.GetIntegrationSettings()?.Setup?.PimApiUrl))
                {
                    nodes.Add(
                        CreateTreeNode(
                            "stores",
                            "-1",
                            queryStrings,
                            "Stores",
                            "pim-icon pim-icon-store",
                            false,
                            this.GetRoutePath("stores", "index")
                        )
                    );
                    nodes.Add(
                        CreateTreeNode(
                            "data-models",
                            "-1",
                            queryStrings,
                            "Data models",
                            "pim-icon pim-icon-3d-model",
                            false,
                            this.SectionAlias + "/" + this.TreeAlias + "/data-models"
                        )
                    );
                }

                nodes.Add(
                    CreateTreeNode(
                        "setup",
                        "-1",
                        queryStrings,
                        "Setup",
                        "pim-icon pim-icon-settings",
                        false,
                        this.SectionAlias + "/" + this.TreeAlias + "/setup"
                    )
                );
            }

            return nodes;
        }

        protected override ActionResult<MenuItemCollection> GetMenuForNode(string id, FormCollection queryStrings)
        {
            // create a Menu Item Collection to return so people can interact with the nodes in your tree
            var menu = _menuItemCollectionFactory.Create();

            return menu;
        }

        protected override ActionResult<TreeNode?> CreateRootNode(FormCollection queryStrings)
        {
            var rootResult = base.CreateRootNode(queryStrings);
            if (!(rootResult.Result is null))
            {
                return rootResult;
            }

            var root = rootResult.Value;

            // set the icon
            root.Icon = "icon-folder";
            // could be set to false for a custom tree with a single node.
            root.HasChildren = true;
            //url for menu
            root.MenuUrl = null;
            root.RoutePath = this.SectionAlias + "/" + this.TreeAlias + "/settings-view";

            return root;
        }

        protected string GetRoutePath(string nodeType, string view, params object[] ids)
        {
            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(4, 5);
            interpolatedStringHandler.AppendFormatted(this.SectionAlias);
            interpolatedStringHandler.AppendLiteral("/");
            interpolatedStringHandler.AppendFormatted(this.TreeAlias);
            interpolatedStringHandler.AppendLiteral("/");
            interpolatedStringHandler.AppendFormatted(nodeType);
            interpolatedStringHandler.AppendLiteral("-");
            interpolatedStringHandler.AppendFormatted(view);
            interpolatedStringHandler.AppendLiteral("/");
            interpolatedStringHandler.AppendFormatted(string.Join("_", ids));
            return interpolatedStringHandler.ToStringAndClear().ToLowerInvariant().TrimEnd('/');
        }
    }
}
