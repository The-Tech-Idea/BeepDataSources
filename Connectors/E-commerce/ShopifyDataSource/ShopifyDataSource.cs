using System.Collections.Generic;
using System.Linq;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;

namespace TheTechIdea.Beep.Connectors.Ecommerce.Shopify
{
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Shopify)]
    public class ShopifyDataSource : WebAPIDataSource
    {
        // Fixed, known Shopify entities (extend as needed)
        private static readonly List<string> KnownEntities = new()
        {
            "Products","Variants","Orders","Customers",
            "InventoryItems","InventoryLevels","Locations",
            "CustomCollections","SmartCollections"
        };

        public ShopifyDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor editor,
            DataSourceType type,
            IErrorsInfo errors)
            : base(datasourcename, logger, editor, type, errors)
        {
            // Ensure we’re on WebAPI connection properties (do NOT set any auth defaults here)
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            // Register fixed entities so design-time and GetEntitesList() are stable
            EntitiesNames = KnownEntities.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // If base method isn't virtual, hide it so callers get the fixed list for this connector
        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        // All other methods (Open/Close, GetEntity, GetEntityAsync, paged GetEntity, CRUD, RunQuery, etc.)
        // are inherited AS-IS from WebAPIDataSource and will:
        // - resolve endpoints from WebAPIConnectionProperties (Endpoints.{Entity}.{Action})
        // - inject auth/headers per WebAPIAuthenticationHelper
        // - handle retries, caching, rate limits, and errors via the base helpers
    }
}
