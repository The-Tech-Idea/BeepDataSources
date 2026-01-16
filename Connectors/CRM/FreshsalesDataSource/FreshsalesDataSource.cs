using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
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

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshsales, PointType = EnumPointType.Function, ObjectType ="Leads", ClassName = "FreshsalesDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<FreshsalesLead>")]
        public IEnumerable<FreshsalesLead> GetLeads(List<AppFilter> filter) => GetEntity("leads", filter).Cast<FreshsalesLead>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshsales, PointType = EnumPointType.Function, ObjectType ="Contacts", ClassName = "FreshsalesDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<FreshsalesContact>")]
        public IEnumerable<FreshsalesContact> GetContacts(List<AppFilter> filter) => GetEntity("contacts", filter).Cast<FreshsalesContact>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshsales, PointType = EnumPointType.Function, ObjectType ="Accounts", ClassName = "FreshsalesDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<FreshsalesAccount>")]
        public IEnumerable<FreshsalesAccount> GetAccounts(List<AppFilter> filter) => GetEntity("accounts", filter).Cast<FreshsalesAccount>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshsales, PointType = EnumPointType.Function, ObjectType ="Deals", ClassName = "FreshsalesDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<FreshsalesDeal>")]
        public IEnumerable<FreshsalesDeal> GetDeals(List<AppFilter> filter) => GetEntity("deals", filter).Cast<FreshsalesDeal>();

        // -------------------- Create / Update (POST/PUT) methods --------------------

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshsales, PointType = EnumPointType.Function, ObjectType ="Lead", ClassName = "FreshsalesDataSource", Showin = ShowinType.Both, misc = "FreshsalesLead")]
        public async Task<IEnumerable<FreshsalesLead>> CreateLeadAsync(FreshsalesLead lead)
        {
            if (lead == null) return Array.Empty<FreshsalesLead>();
            using var resp = await PostAsync("crm/sales/api/leads", lead).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<FreshsalesLead>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<FreshsalesLead>(json, opts);
                return result != null ? new[] { result } : Array.Empty<FreshsalesLead>();
            }
            catch
            {
                return Array.Empty<FreshsalesLead>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshsales, PointType = EnumPointType.Function, ObjectType ="Lead", ClassName = "FreshsalesDataSource", Showin = ShowinType.Both, misc = "FreshsalesLead")]
        public async Task<IEnumerable<FreshsalesLead>> UpdateLeadAsync(string leadId, FreshsalesLead lead)
        {
            if (string.IsNullOrWhiteSpace(leadId) || lead == null) return Array.Empty<FreshsalesLead>();
            var endpoint = $"crm/sales/api/leads/{Uri.EscapeDataString(leadId)}";
            using var resp = await PutAsync(endpoint, lead).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<FreshsalesLead>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<FreshsalesLead>(json, opts);
                return result != null ? new[] { result } : Array.Empty<FreshsalesLead>();
            }
            catch
            {
                return Array.Empty<FreshsalesLead>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshsales, PointType = EnumPointType.Function, ObjectType ="Contact", ClassName = "FreshsalesDataSource", Showin = ShowinType.Both, misc = "FreshsalesContact")]
        public async Task<IEnumerable<FreshsalesContact>> CreateContactAsync(FreshsalesContact contact)
        {
            if (contact == null) return Array.Empty<FreshsalesContact>();
            using var resp = await PostAsync("crm/sales/api/contacts", contact).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<FreshsalesContact>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<FreshsalesContact>(json, opts);
                return result != null ? new[] { result } : Array.Empty<FreshsalesContact>();
            }
            catch
            {
                return Array.Empty<FreshsalesContact>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshsales, PointType = EnumPointType.Function, ObjectType ="Contact", ClassName = "FreshsalesDataSource", Showin = ShowinType.Both, misc = "FreshsalesContact")]
        public async Task<IEnumerable<FreshsalesContact>> UpdateContactAsync(string contactId, FreshsalesContact contact)
        {
            if (string.IsNullOrWhiteSpace(contactId) || contact == null) return Array.Empty<FreshsalesContact>();
            var endpoint = $"crm/sales/api/contacts/{Uri.EscapeDataString(contactId)}";
            using var resp = await PutAsync(endpoint, contact).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<FreshsalesContact>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<FreshsalesContact>(json, opts);
                return result != null ? new[] { result } : Array.Empty<FreshsalesContact>();
            }
            catch
            {
                return Array.Empty<FreshsalesContact>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshsales, PointType = EnumPointType.Function, ObjectType ="Account", ClassName = "FreshsalesDataSource", Showin = ShowinType.Both, misc = "FreshsalesAccount")]
        public async Task<IEnumerable<FreshsalesAccount>> CreateAccountAsync(FreshsalesAccount account)
        {
            if (account == null) return Array.Empty<FreshsalesAccount>();
            using var resp = await PostAsync("crm/sales/api/sales_accounts", account).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<FreshsalesAccount>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<FreshsalesAccount>(json, opts);
                return result != null ? new[] { result } : Array.Empty<FreshsalesAccount>();
            }
            catch
            {
                return Array.Empty<FreshsalesAccount>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshsales, PointType = EnumPointType.Function, ObjectType ="Account", ClassName = "FreshsalesDataSource", Showin = ShowinType.Both, misc = "FreshsalesAccount")]
        public async Task<IEnumerable<FreshsalesAccount>> UpdateAccountAsync(string accountId, FreshsalesAccount account)
        {
            if (string.IsNullOrWhiteSpace(accountId) || account == null) return Array.Empty<FreshsalesAccount>();
            var endpoint = $"crm/sales/api/sales_accounts/{Uri.EscapeDataString(accountId)}";
            using var resp = await PutAsync(endpoint, account).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<FreshsalesAccount>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<FreshsalesAccount>(json, opts);
                return result != null ? new[] { result } : Array.Empty<FreshsalesAccount>();
            }
            catch
            {
                return Array.Empty<FreshsalesAccount>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshsales, PointType = EnumPointType.Function, ObjectType ="Deal", ClassName = "FreshsalesDataSource", Showin = ShowinType.Both, misc = "FreshsalesDeal")]
        public async Task<IEnumerable<FreshsalesDeal>> CreateDealAsync(FreshsalesDeal deal)
        {
            if (deal == null) return Array.Empty<FreshsalesDeal>();
            using var resp = await PostAsync("crm/sales/api/deals", deal).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<FreshsalesDeal>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<FreshsalesDeal>(json, opts);
                return result != null ? new[] { result } : Array.Empty<FreshsalesDeal>();
            }
            catch
            {
                return Array.Empty<FreshsalesDeal>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Freshsales, PointType = EnumPointType.Function, ObjectType ="Deal", ClassName = "FreshsalesDataSource", Showin = ShowinType.Both, misc = "FreshsalesDeal")]
        public async Task<IEnumerable<FreshsalesDeal>> UpdateDealAsync(string dealId, FreshsalesDeal deal)
        {
            if (string.IsNullOrWhiteSpace(dealId) || deal == null) return Array.Empty<FreshsalesDeal>();
            var endpoint = $"crm/sales/api/deals/{Uri.EscapeDataString(dealId)}";
            using var resp = await PutAsync(endpoint, deal).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<FreshsalesDeal>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<FreshsalesDeal>(json, opts);
                return result != null ? new[] { result } : Array.Empty<FreshsalesDeal>();
            }
            catch
            {
                return Array.Empty<FreshsalesDeal>();
            }
        }

        #endregion
    }
}