using Microsoft.Extensions.Logging;
using NPoco;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Migrations
{
    public class VendrIntegrationSettingsComposer : ComponentComposer<VendrIntegrationSetting>
    {
    }

    public class VendrIntegrationSetting : IComponent
    {
        private readonly ICoreScopeProvider _coreScopeProvider;
        private readonly IMigrationPlanExecutor _migrationPlanExecutor;
        private readonly IKeyValueService _keyValueService;
        private readonly IRuntimeState _runtimeState;

        public VendrIntegrationSetting(
            ICoreScopeProvider coreScopeProvider,
            IMigrationPlanExecutor migrationPlanExecutor,
            IKeyValueService keyValueService,
            IRuntimeState runtimeState)
        {
            _coreScopeProvider = coreScopeProvider;
            _migrationPlanExecutor = migrationPlanExecutor;
            _keyValueService = keyValueService;
            _runtimeState = runtimeState;
        }

        public void Initialize()
        {
            if (_runtimeState.Level < RuntimeLevel.Run)
            {
                return;
            }

            // Create a migration plan for a specific project/feature
            // We can then track that latest migration state/step for this project/feature
            var migrationPlan = new MigrationPlan("structPIMIntegrationSettings");

            // This is the steps we need to take
            // Each step in the migration adds a unique value
            migrationPlan.From(string.Empty)
                .To<AddVendrIntegrationSettingsTable>("structPIMintegrationsettings-db");

            // Go and upgrade our site (Will check if it needs to do the work or not)
            // Based on the current/latest step
            var upgrader = new Upgrader(migrationPlan);
            upgrader.Execute(_migrationPlanExecutor, _coreScopeProvider, _keyValueService);
        }

        public void Terminate()
        {
        }
    }

    public class AddVendrIntegrationSettingsTable : MigrationBase
    {
        public AddVendrIntegrationSettingsTable(IMigrationContext context) : base(context)
        {
        }
        protected override void Migrate()
        {
            Logger.LogDebug("Running migration {MigrationStep}", "AddStructPIMIntegrationSettingsTable");

            // Lots of methods available in the MigrationBase class - discover with this.
            if (TableExists("StructPIMIntegrationSettings") == false)
            {
                Create.Table<VendrIntegrationSettingsSchema>().Do();
            }
            else
            {
                Logger.LogDebug("The database table {DbTable} already exists, skipping", "StructPIMIntegrationSettings");
            }
        }

        [TableName("StructPIMIntegrationSettings")]
        [PrimaryKey("Key")]
        [ExplicitColumns]
        public class VendrIntegrationSettingsSchema
        {
            [Column("Key")]
            [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
            public string Key { get; set; }

            [Column("Value")]
            [SpecialDbType(SpecialDbTypes.NVARCHARMAX)]
            public string Value { get; set; }
        }
    }
}

