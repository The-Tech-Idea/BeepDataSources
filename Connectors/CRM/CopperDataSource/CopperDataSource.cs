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
using TheTechIdea.Beep.DataSources.CRM.Copper;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Vis;

namespace TheTechIdea.Beep.Connectors.Copper
{
    /// <summary>
    /// Copper CRM data source aligned with the shared WebAPIDataSource ("Twitter") pattern.
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Copper)]
    public sealed class CopperDataSource : WebAPIDataSource
    {
        private sealed record EntityDefinition(string Endpoint, Type ModelType, string[] RequiredFilters);

        private static readonly Dictionary<string, EntityDefinition> Map = new(StringComparer.OrdinalIgnoreCase)
        {
            ["leads"] = new("leads/search", typeof(CopperLead), Array.Empty<string>()),
            ["contacts"] = new("people/search", typeof(CopperContact), Array.Empty<string>()),
            ["accounts"] = new("companies/search", typeof(CopperAccount), Array.Empty<string>()),
            ["deals"] = new("opportunities/search", typeof(CopperDeal), Array.Empty<string>()),
            ["tasks"] = new("tasks/search", typeof(CopperTask), Array.Empty<string>()),
            ["activities"] = new("activities/search", typeof(CopperActivity), Array.Empty<string>()),
            ["users"] = new("users/search", typeof(CopperUser), Array.Empty<string>())
        };

        private static readonly List<string> KnownEntities = Map.Keys.ToList();

        public CopperDataSource(
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

            // Configure Copper-specific authentication headers
            ConfigureCopperHeaders();

            EntitiesNames = KnownEntities.ToList();
            Entities = EntitiesNames
                .Select(name => new EntityStructure { EntityName = name, DatasourceEntityName = name })
                .ToList();
        }

        private void ConfigureCopperHeaders()
        {
            if (Dataconnection?.ConnectionProp is WebAPIConnectionProperties props)
            {
                // Set base URL for Copper API
                if (string.IsNullOrEmpty(props.ConnectionString))
                {
                    props.ConnectionString = "https://api.prosperworks.com/developer_api/v1";
                }

                // Add Copper-specific headers
                var copperHeaders = new List<WebApiHeader>
                {
                    new WebApiHeader { Headername = "X-PW-Application", Headervalue = "developer_api" },
                    new WebApiHeader { Headername = "Content-Type", Headervalue = "application/json" }
                };

                // Add API key and email if available
                if (!string.IsNullOrEmpty(props.ApiKey))
                {
                    copperHeaders.Add(new WebApiHeader { Headername = "X-PW-AccessToken", Headervalue = props.ApiKey });
                }

                if (!string.IsNullOrEmpty(props.UserID))
                {
                    copperHeaders.Add(new WebApiHeader { Headername = "X-PW-UserEmail", Headervalue = props.UserID });
                }

                // Merge with existing headers
                if (props.Headers == null)
                {
                    props.Headers = copperHeaders;
                }
                else
                {
                    // Remove any existing Copper headers to avoid duplicates
                    props.Headers.RemoveAll(h => h.Headername.StartsWith("X-PW-") || h.Headername == "Content-Type");

                    // Add Copper headers
                    props.Headers.AddRange(copperHeaders);
                }
            }
        }

        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        public override EntityStructure GetEntityStructure(string EntityName, bool refresh)
        {
            var definition = GetDefinition(EntityName);
            return CopperHelpers.BuildEntityStructure(EntityName, definition.ModelType, DatasourceName);
        }

        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> Filter)
        {
            return GetEntityAsync(EntityName, Filter).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            var definition = GetDefinition(EntityName);
            var query = CopperHelpers.FiltersToQuery(Filter);
            CopperHelpers.RequireFilters(EntityName, query, definition.RequiredFilters);

            var endpoint = CopperHelpers.ResolveEndpoint(definition.Endpoint, query);

            using var response = await GetAsync(endpoint, query).ConfigureAwait(false);
            if (response is null || !response.IsSuccessStatusCode)
            {
                return Array.Empty<object>();
            }

            var parsed = await CopperHelpers.ParseResponseAsync(response, EntityName, definition.ModelType).ConfigureAwait(false);
            return parsed.Items;
        }

        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            var definition = GetDefinition(EntityName);
            var page = Math.Max(1, pageNumber);
            var size = Math.Max(1, Math.Min(pageSize, 200));

            var query = CopperHelpers.FiltersToQuery(filter);
            CopperHelpers.RequireFilters(EntityName, query, definition.RequiredFilters);

            query["page_number"] = page.ToString();
            query["page_size"] = size.ToString();

            var endpoint = CopperHelpers.ResolveEndpoint(definition.Endpoint, query);

            using var response = CallCopper(endpoint, query).ConfigureAwait(false).GetAwaiter().GetResult();
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

            var parsed = CopperHelpers.ParseResponseAsync(response, EntityName, definition.ModelType).ConfigureAwait(false).GetAwaiter().GetResult();

            var pagination = parsed.Pagination;
            var totalRecords = pagination?.Total ?? parsed.Items.Count;
            var totalPages = pagination != null && pagination.PerPage.HasValue && pagination.PerPage.Value > 0
                ? (int)Math.Ceiling((double)totalRecords / pagination.PerPage.Value)
                : Math.Max(page, parsed.Items.Count > 0 ? page : 0);

            return new PagedResult
            {
                Data = parsed.Items,
                PageNumber = page,
                PageSize = size,
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                HasPreviousPage = page > 1,
                HasNextPage = pagination != null && pagination.Page.HasValue && pagination.PerPage.HasValue
                    ? (pagination.Page.Value * pagination.PerPage.Value) < totalRecords
                    : parsed.Items.Count >= size
            };
        }

        private static EntityDefinition GetDefinition(string entityName)
        {
            if (!Map.TryGetValue(entityName, out var definition))
            {
                throw new InvalidOperationException($"Unknown Copper entity '{entityName}'.");
            }

            return definition;
        }

        private async Task<HttpResponseMessage> CallCopper(string endpoint, Dictionary<string, string> query, CancellationToken cancellationToken = default)
        {
            return await GetAsync(endpoint, query, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        #region Command Methods

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Copper, PointType = EnumPointType.Function, ObjectType ="Leads", ClassName = "CopperDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<CopperLead>")]
        public IEnumerable<CopperLead> GetLeads(List<AppFilter> filter) => GetEntity("leads", filter).Cast<CopperLead>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Copper, PointType = EnumPointType.Function, ObjectType ="Contacts", ClassName = "CopperDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<CopperContact>")]
        public IEnumerable<CopperContact> GetContacts(List<AppFilter> filter) => GetEntity("contacts", filter).Cast<CopperContact>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Copper, PointType = EnumPointType.Function, ObjectType ="Accounts", ClassName = "CopperDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<CopperAccount>")]
        public IEnumerable<CopperAccount> GetAccounts(List<AppFilter> filter) => GetEntity("accounts", filter).Cast<CopperAccount>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Copper, PointType = EnumPointType.Function, ObjectType ="Deals", ClassName = "CopperDataSource", Showin = ShowinType.Both, misc = "ReturnType: IEnumerable<CopperDeal>")]
        public IEnumerable<CopperDeal> GetDeals(List<AppFilter> filter) => GetEntity("deals", filter).Cast<CopperDeal>();

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Copper, PointType = EnumPointType.Function, ObjectType ="CopperLead", ClassName = "CopperDataSource", Showin = ShowinType.Both, misc = "CopperLead")]
        public async Task<IEnumerable<CopperLead>> CreateLeadAsync(CopperLead lead)
        {
            if (lead == null) return Array.Empty<CopperLead>();
            using var resp = await PostAsync("leads", lead).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<CopperLead>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<CopperLead>(json, opts);
                return result != null ? new[] { result } : Array.Empty<CopperLead>();
            }
            catch
            {
                return Array.Empty<CopperLead>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Copper, PointType = EnumPointType.Function, ObjectType ="CopperLead", ClassName = "CopperDataSource", Showin = ShowinType.Both, misc = "CopperLead")]
        public async Task<IEnumerable<CopperLead>> UpdateLeadAsync(string leadId, CopperLead lead)
        {
            if (string.IsNullOrWhiteSpace(leadId) || lead == null) return Array.Empty<CopperLead>();
            var endpoint = $"leads/{Uri.EscapeDataString(leadId)}";
            using var resp = await PutAsync(endpoint, lead).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<CopperLead>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<CopperLead>(json, opts);
                return result != null ? new[] { result } : Array.Empty<CopperLead>();
            }
            catch
            {
                return Array.Empty<CopperLead>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Copper, PointType = EnumPointType.Function, ObjectType ="CopperContact", ClassName = "CopperDataSource", Showin = ShowinType.Both, misc = "CopperContact")]
        public async Task<IEnumerable<CopperContact>> CreateContactAsync(CopperContact contact)
        {
            if (contact == null) return Array.Empty<CopperContact>();
            using var resp = await PostAsync("people", contact).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<CopperContact>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<CopperContact>(json, opts);
                return result != null ? new[] { result } : Array.Empty<CopperContact>();
            }
            catch
            {
                return Array.Empty<CopperContact>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Copper, PointType = EnumPointType.Function, ObjectType ="CopperContact", ClassName = "CopperDataSource", Showin = ShowinType.Both, misc = "CopperContact")]
        public async Task<IEnumerable<CopperContact>> UpdateContactAsync(string contactId, CopperContact contact)
        {
            if (string.IsNullOrWhiteSpace(contactId) || contact == null) return Array.Empty<CopperContact>();
            var endpoint = $"people/{Uri.EscapeDataString(contactId)}";
            using var resp = await PutAsync(endpoint, contact).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<CopperContact>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<CopperContact>(json, opts);
                return result != null ? new[] { result } : Array.Empty<CopperContact>();
            }
            catch
            {
                return Array.Empty<CopperContact>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Copper, PointType = EnumPointType.Function, ObjectType ="CopperAccount", ClassName = "CopperDataSource", Showin = ShowinType.Both, misc = "CopperAccount")]
        public async Task<IEnumerable<CopperAccount>> CreateAccountAsync(CopperAccount account)
        {
            if (account == null) return Array.Empty<CopperAccount>();
            using var resp = await PostAsync("companies", account).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<CopperAccount>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<CopperAccount>(json, opts);
                return result != null ? new[] { result } : Array.Empty<CopperAccount>();
            }
            catch
            {
                return Array.Empty<CopperAccount>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Copper, PointType = EnumPointType.Function, ObjectType ="CopperAccount", ClassName = "CopperDataSource", Showin = ShowinType.Both, misc = "CopperAccount")]
        public async Task<IEnumerable<CopperAccount>> UpdateAccountAsync(string accountId, CopperAccount account)
        {
            if (string.IsNullOrWhiteSpace(accountId) || account == null) return Array.Empty<CopperAccount>();
            var endpoint = $"companies/{Uri.EscapeDataString(accountId)}";
            using var resp = await PutAsync(endpoint, account).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<CopperAccount>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<CopperAccount>(json, opts);
                return result != null ? new[] { result } : Array.Empty<CopperAccount>();
            }
            catch
            {
                return Array.Empty<CopperAccount>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Copper, PointType = EnumPointType.Function, ObjectType ="CopperDeal", ClassName = "CopperDataSource", Showin = ShowinType.Both, misc = "CopperDeal")]
        public async Task<IEnumerable<CopperDeal>> CreateDealAsync(CopperDeal deal)
        {
            if (deal == null) return Array.Empty<CopperDeal>();
            using var resp = await PostAsync("opportunities", deal).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<CopperDeal>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<CopperDeal>(json, opts);
                return result != null ? new[] { result } : Array.Empty<CopperDeal>();
            }
            catch
            {
                return Array.Empty<CopperDeal>();
            }
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Copper, PointType = EnumPointType.Function, ObjectType ="CopperDeal", ClassName = "CopperDataSource", Showin = ShowinType.Both, misc = "CopperDeal")]
        public async Task<IEnumerable<CopperDeal>> UpdateDealAsync(string dealId, CopperDeal deal)
        {
            if (string.IsNullOrWhiteSpace(dealId) || deal == null) return Array.Empty<CopperDeal>();
            var endpoint = $"opportunities/{Uri.EscapeDataString(dealId)}";
            using var resp = await PutAsync(endpoint, deal).ConfigureAwait(false);
            if (resp == null || !resp.IsSuccessStatusCode) return Array.Empty<CopperDeal>();
            var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var result = JsonSerializer.Deserialize<CopperDeal>(json, opts);
                return result != null ? new[] { result } : Array.Empty<CopperDeal>();
            }
            catch
            {
                return Array.Empty<CopperDeal>();
            }
        }

        #endregion
    }
}
