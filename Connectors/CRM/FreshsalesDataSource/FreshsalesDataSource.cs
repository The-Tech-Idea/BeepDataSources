using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.DataSources.CRM.Freshsales;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.Connectors.Freshsales
{
    /// <summary>
    /// Freshsales data source aligned with the shared WebAPIDataSource ("Twitter") pattern.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshsales)]
    public sealed class FreshsalesDataSource : WebAPIDataSource
    {
        private sealed record EntityDefinition(string Endpoint, Type ModelType, string[] RequiredFilters);

        private static readonly Dictionary<string, EntityDefinition> Map = new(StringComparer.OrdinalIgnoreCase)
        {
            ["leads"] = new("crm/sales/api/leads", typeof(FreshsalesLead), Array.Empty<string>()),
            ["contacts"] = new("crm/sales/api/contacts", typeof(FreshsalesContact), Array.Empty<string>()),
            ["accounts"] = new("crm/sales/api/sales_accounts", typeof(FreshsalesAccount), Array.Empty<string>()),
            ["deals"] = new("crm/sales/api/deals", typeof(FreshsalesDeal), Array.Empty<string>()),
            ["tasks"] = new("crm/sales/api/tasks", typeof(FreshsalesTask), Array.Empty<string>()),
            ["appointments"] = new("crm/sales/api/appointments", typeof(FreshsalesAppointment), Array.Empty<string>()),
            ["notes"] = new("crm/sales/api/notes", typeof(FreshsalesNote), Array.Empty<string>()),
            ["products"] = new("crm/sales/api/products", typeof(FreshsalesProduct), Array.Empty<string>()),
            ["sales_activities"] = new("crm/sales/api/sales_activities", typeof(FreshsalesSalesActivity), Array.Empty<string>()),
            ["users"] = new("crm/sales/api/users", typeof(FreshsalesUser), Array.Empty<string>()),
            ["territories"] = new("crm/sales/api/territories", typeof(FreshsalesTerritory), Array.Empty<string>()),
            ["teams"] = new("crm/sales/api/teams", typeof(FreshsalesTeam), Array.Empty<string>()),
            ["currencies"] = new("crm/sales/api/currencies", typeof(FreshsalesCurrency), Array.Empty<string>())
        };

        private static readonly List<string> KnownEntities = Map.Keys.ToList();

        public FreshsalesDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                {
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
                }
            }

            EntitiesNames = KnownEntities.ToList();
            Entities = EntitiesNames
                .Select(name => new EntityStructure { EntityName = name, DatasourceEntityName = name })
                .ToList();
        }

        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        public override EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            var definition = GetDefinition(EntityName);
            return FreshsalesHelpers.BuildEntityStructure(EntityName, definition.ModelType, DatasourceName);
        }

        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> Filter)
        {
            return GetEntityAsync(EntityName, Filter).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            var definition = GetDefinition(EntityName);
            var query = FreshsalesHelpers.FiltersToQuery(Filter);
            FreshsalesHelpers.RequireFilters(EntityName, query, definition.RequiredFilters);

            var endpoint = FreshsalesHelpers.ResolveEndpoint(definition.Endpoint, query);

            using var response = await GetAsync(endpoint, query).ConfigureAwait(false);
            if (response is null || !response.IsSuccessStatusCode)
            {
                return Array.Empty<object>();
            }

            var parsed = await FreshsalesHelpers.ParseResponseAsync(response, EntityName, definition.ModelType).ConfigureAwait(false);
            return parsed.Items;
        }

        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            var definition = GetDefinition(EntityName);
            var page = Math.Max(1, pageNumber);
            var size = Math.Max(1, Math.Min(pageSize, 200));

            var query = FreshsalesHelpers.FiltersToQuery(filter);
            FreshsalesHelpers.RequireFilters(EntityName, query, definition.RequiredFilters);

            query["page"] = page.ToString();
            query["per_page"] = size.ToString();

            var endpoint = FreshsalesHelpers.ResolveEndpoint(definition.Endpoint, query);

            using var response = CallFreshsales(endpoint, query).ConfigureAwait(false).GetAwaiter().GetResult();
            if (response is null || !response.IsSuccessStatusCode)
            {
                return new PagedResult
                {
                    Data = Array.Empty<object>(),
                    PageNumber = page,
                    PageSize = size,
                    TotalPages = page,
                    TotalRecords = 0,
                    HasNextPage = false,
                    HasPreviousPage = page > 1
                };
            }

            var parsed = FreshsalesHelpers.ParseResponseAsync(response, EntityName, definition.ModelType).ConfigureAwait(false).GetAwaiter().GetResult();

            var pagination = parsed.Meta?.Pagination;
            var totalRecords = pagination?.Total ?? parsed.Items.Count;
            var totalPages = pagination?.TotalPages ?? (pagination?.PerPage.HasValue == true && pagination.PerPage.Value > 0
                ? (int)Math.Ceiling((double)totalRecords / pagination.PerPage.Value)
                : Math.Max(page, parsed.Items.Count > 0 ? page : 0));

            return new PagedResult
            {
                Data = parsed.Items,
                PageNumber = page,
                PageSize = size,
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                HasPreviousPage = page > 1,
                HasNextPage = pagination?.CurrentPage.HasValue == true && pagination?.TotalPages.HasValue == true
                    ? pagination.CurrentPage.Value < pagination.TotalPages.Value
                    : parsed.Items.Count >= size
            };
        }

        private static EntityDefinition GetDefinition(string entityName)
        {
            if (!Map.TryGetValue(entityName, out var definition))
            {
                throw new InvalidOperationException($"Unknown Freshsales entity '{entityName}'.");
            }

            return definition;
        }

        private async Task<HttpResponseMessage> CallFreshsales(string endpoint, Dictionary<string, string> query, CancellationToken cancellationToken = default)
        {
            return await GetAsync(endpoint, query, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #region Command Methods

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshsales, PointType = EnumPointType.Function, ObjectType = "Leads", ClassName = "FreshsalesDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<FreshsalesLead>")]
        public IEnumerable<FreshsalesLead> GetLeads(List<AppFilter> filter) => GetEntity("leads", filter).Cast<FreshsalesLead>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshsales, PointType = EnumPointType.Function, ObjectType = "Contacts", ClassName = "FreshsalesDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<FreshsalesContact>")]
        public IEnumerable<FreshsalesContact> GetContacts(List<AppFilter> filter) => GetEntity("contacts", filter).Cast<FreshsalesContact>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshsales, PointType = EnumPointType.Function, ObjectType = "Accounts", ClassName = "FreshsalesDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<FreshsalesAccount>")]
        public IEnumerable<FreshsalesAccount> GetAccounts(List<AppFilter> filter) => GetEntity("accounts", filter).Cast<FreshsalesAccount>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshsales, PointType = EnumPointType.Function, ObjectType = "Deals", ClassName = "FreshsalesDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<FreshsalesDeal>")]
        public IEnumerable<FreshsalesDeal> GetDeals(List<AppFilter> filter) => GetEntity("deals", filter).Cast<FreshsalesDeal>();

        #endregion
    }
}