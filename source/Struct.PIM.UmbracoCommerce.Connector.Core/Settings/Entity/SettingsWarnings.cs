using System.Collections.Generic;

namespace Struct.PIM.ShopifyConnector.Settings.Entity
{

    public class SettingsWarnings
    {
        public List<string> Infos { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
