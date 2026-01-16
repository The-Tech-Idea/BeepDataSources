using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.ClickSend.Models;
using TheTechIdea.Beep.Connectors.ClickSend.Models;

namespace TheTechIdea.Beep.Connectors.ClickSend
{
    /// <summary>
    /// ClickSend SMS data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.ClickSend)]
    public class ClickSendDataSource : WebAPIDataSource
    {
        /// <summary>
        /// Initializes a new instance of the ClickSendDataSource class
        /// </summary>
        public ClickSendDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per)
            : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
            DatasourceType = DataSourceType.ClickSend;
            Category = DatasourceCategory.Connector;

            // Ensure WebAPI connection props exist
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }

            // Register entities
            EntitiesNames = new List<string> { "sms_history", "account", "contacts" };
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // Return the fixed list
        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        // Sync
        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
            return data ?? Array.Empty<object>();
        }

        // Paged
        public override PagedResult GetEntity(string EntityName, List<AppFilter> filter, int pageNumber, int pageSize)
        {
            var items = GetEntity(EntityName, filter).ToList();
            var totalRecords = items.Count;
            var pagedItems = items.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return new PagedResult
            {
                Data = pagedItems,
                PageNumber = Math.Max(1, pageNumber),
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber * pageSize < totalRecords
            };
        }

        /// <summary>
        /// Asynchronously retrieves an entity based on the provided name and filters.
        /// </summary>
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            try
            {
                string endpoint = GetEndpointForEntity(EntityName);
                if (string.IsNullOrEmpty(endpoint))
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"Unknown ClickSend entity: {EntityName}";
                    return Array.Empty<object>();
                }

                // Build the API URL
                var baseUrl = Dataconnection?.ConnectionProp?.Url ?? "https://rest.clicksend.com/v3";
                if (!baseUrl.EndsWith("/"))
                    baseUrl = baseUrl.TrimEnd('/');
                string url = $"{baseUrl}/{endpoint}";

                // Make the request using base class method (handles authentication automatically)
                var response = await GetAsync(url);
                if (response != null && response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    // Process the response based on entity type
                    return ProcessApiResponse(EntityName, json);
                }
                else
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"ClickSend API error: {response?.StatusCode.ToString() ?? "Unknown error"}";
                    return Array.Empty<object>();
                }
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return Array.Empty<object>();
            }
        }

        private string GetEndpointForEntity(string entityName)
        {
            return entityName.ToLower() switch
            {
                "sms_history" => "sms/history",
                "account" => "account",
                "contacts" => "lists/{list_id}/contacts", // This would need a list_id parameter
                _ => null
            };
        }

        private IEnumerable<object> ProcessApiResponse(string entityName, string jsonResponse)
        {
            if (string.IsNullOrEmpty(jsonResponse))
                return Array.Empty<object>();

            try
            {
                return entityName.ToLower() switch
                {
                    "sms_history" => JsonSerializer.Deserialize<ClickSendSMSResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.Data?.Messages ?? new List<ClickSendSMSMessage>(),
                    "account" => JsonSerializer.Deserialize<ClickSendAccountResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.Data != null
                        ? new[] { JsonSerializer.Deserialize<ClickSendAccountResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }).Data }
                        : Array.Empty<ClickSendAccount>(),
                    "contacts" => JsonSerializer.Deserialize<ClickSendContactResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.Data?.Data ?? new List<ClickSendContact>(),
                    _ => Array.Empty<object>()
                };
            }
            catch
            {
                // If deserialization fails, return empty
                return Array.Empty<object>();
            }
        }

        [CommandAttribute(ObjectType ="ClickSendSMSMessage", PointType = EnumPointType.Function, Name = "GetSMSHistory", Caption = "Get SMS History", ClassName = "ClickSendDataSource")]
        public async Task<List<ClickSendSMSMessage>> GetSMSHistory()
        {
            var result = await GetEntityAsync("sms_history", new List<AppFilter>());
            return result.Select(item => JsonSerializer.Deserialize<ClickSendSMSMessage>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<ClickSendSMSMessage>().ToList();
        }

        [CommandAttribute(ObjectType ="ClickSendAccount", PointType = EnumPointType.Function, Name = "GetAccount", Caption = "Get Account", ClassName = "ClickSendDataSource")]
        public async Task<ClickSendAccount> GetAccount()
        {
            var result = await GetEntityAsync("account", new List<AppFilter>());
            return result.FirstOrDefault() as ClickSendAccount;
        }

        [CommandAttribute(ObjectType ="ClickSendContact", PointType = EnumPointType.Function, Name = "GetContacts", Caption = "Get Contacts", ClassName = "ClickSendDataSource")]
        public async Task<List<ClickSendContact>> GetContacts(string listId)
        {
            var filters = new List<AppFilter> { new AppFilter { FieldName = "list_id", FilterValue = listId, Operator = "=" } };
            var result = await GetEntityAsync("contacts", filters);
            return result.Select(item => JsonSerializer.Deserialize<ClickSendContact>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<ClickSendContact>().ToList();
        }

        // POST/PUT methods for creating and updating entities
        [CommandAttribute(ObjectType ="ClickSendSMS", PointType = EnumPointType.Function, Name = "SendSMS", Caption = "Send SMS", ClassName = "ClickSendDataSource")]
        public async Task<ClickSendSMSResponse> SendSMS(ClickSendSMS sms)
        {
            var endpoint = "sms/send";
            var response = await PostAsync<ClickSendSMSResponse>(endpoint, sms);
            return response;
        }

        [CommandAttribute(ObjectType ="ClickSendContact", PointType = EnumPointType.Function, Name = "CreateContact", Caption = "Create Contact", ClassName = "ClickSendDataSource")]
        public async Task<ClickSendContact> CreateContact(string listId, ClickSendContact contact)
        {
            var endpoint = $"lists/{listId}/contacts";
            var response = await PostAsync<ClickSendContact>(endpoint, contact);
            return response;
        }

        [CommandAttribute(ObjectType ="ClickSendContact", PointType = EnumPointType.Function, Name = "UpdateContact", Caption = "Update Contact", ClassName = "ClickSendDataSource")]
        public async Task<ClickSendContact> UpdateContact(string listId, string contactId, ClickSendContact contact)
        {
            var endpoint = $"lists/{listId}/contacts/{contactId}";
            var response = await PutAsync<ClickSendContact>(endpoint, contact);
            return response;
        }
    }
}