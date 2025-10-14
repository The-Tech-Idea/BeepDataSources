using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
using TheTechIdea.Beep.Connectors.Kudosity.Models;
using TheTechIdea.Beep.Connectors.Kudosity.Models;

namespace TheTechIdea.Beep.Connectors.Kudosity
{
    /// <summary>
    /// Kudosity SMS data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Kudosity)]
    public class KudosityDataSource : WebAPIDataSource
    {
        /// <summary>
        /// Initializes a new instance of the KudosityDataSource class
        /// </summary>
        public KudosityDataSource(string datasourcename, IDMLogger logger, IDMEEditor DMEEditor, DataSourceType databasetype, IErrorsInfo per)
            : base(datasourcename, logger, DMEEditor, databasetype, per)
        {
            DatasourceType = DataSourceType.Kudosity;
            Category = DatasourceCategory.Connector;

            // Ensure WebAPI connection props exist
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }
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
                    ErrorObject.Message = $"Unknown Kudosity entity: {EntityName}";
                    return Array.Empty<object>();
                }

                // Build the API URL
                string url = $"https://api.kudosity.com{endpoint}";

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
                    ErrorObject.Message = $"Kudosity API error: {response?.StatusCode.ToString() ?? "Unknown error"}";
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
                "sms" => "/api/v1/sms",
                "campaigns" => "/api/v1/campaigns",
                "contacts" => "/api/v1/contacts",
                "contactlists" => "/api/v1/contact-lists",
                "messagehistory" => "/api/v1/message-history",
                "account" => "/api/v1/account",
                "webhooks" => "/api/v1/webhooks",
                "templates" => "/api/v1/templates",
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
                    "sms" => JsonSerializer.Deserialize<KudosityPaginationResponse<KudosityMessageHistory>>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.Data ?? new List<KudosityMessageHistory>(),
                    "campaigns" => JsonSerializer.Deserialize<KudosityPaginationResponse<KudosityCampaign>>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.Data ?? new List<KudosityCampaign>(),
                    "contacts" => JsonSerializer.Deserialize<KudosityPaginationResponse<KudosityContact>>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.Data ?? new List<KudosityContact>(),
                    "contactlists" => JsonSerializer.Deserialize<KudosityPaginationResponse<KudosityContactList>>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.Data ?? new List<KudosityContactList>(),
                    "messagehistory" => JsonSerializer.Deserialize<KudosityPaginationResponse<KudosityMessageHistory>>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.Data ?? new List<KudosityMessageHistory>(),
                    "account" => JsonSerializer.Deserialize<KudosityAccount>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) != null
                        ? new[] { JsonSerializer.Deserialize<KudosityAccount>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) }
                        : Array.Empty<KudosityAccount>(),
                    "webhooks" => JsonSerializer.Deserialize<KudosityPaginationResponse<KudosityWebhook>>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.Data ?? new List<KudosityWebhook>(),
                    "templates" => JsonSerializer.Deserialize<KudosityPaginationResponse<KudosityTemplate>>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.Data ?? new List<KudosityTemplate>(),
                    _ => Array.Empty<object>()
                };
            }
            catch
            {
                // If deserialization fails, return empty
                return Array.Empty<object>();
            }
        }

        [CommandAttribute(ObjectType = "KudosityMessageHistory", PointType = EnumPointType.Function, Name = "GetMessageHistory", Caption = "Get Message History", ClassName = "KudosityDataSource")]
        public async Task<List<KudosityMessageHistory>> GetMessageHistory()
        {
            var result = await GetEntityAsync("messagehistory", new List<AppFilter>());
            return result.Select(item => JsonSerializer.Deserialize<KudosityMessageHistory>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<KudosityMessageHistory>().ToList();
        }

        [CommandAttribute(ObjectType = "KudosityCampaign", PointType = EnumPointType.Function, Name = "GetCampaigns", Caption = "Get Campaigns", ClassName = "KudosityDataSource")]
        public async Task<List<KudosityCampaign>> GetCampaigns()
        {
            var result = await GetEntityAsync("campaigns", new List<AppFilter>());
            return result.Select(item => JsonSerializer.Deserialize<KudosityCampaign>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<KudosityCampaign>().ToList();
        }

        [CommandAttribute(ObjectType = "KudosityContact", PointType = EnumPointType.Function, Name = "GetContacts", Caption = "Get Contacts", ClassName = "KudosityDataSource")]
        public async Task<List<KudosityContact>> GetContacts()
        {
            var result = await GetEntityAsync("contacts", new List<AppFilter>());
            return result.Select(item => JsonSerializer.Deserialize<KudosityContact>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<KudosityContact>().ToList();
        }

        [CommandAttribute(ObjectType = "KudosityContactList", PointType = EnumPointType.Function, Name = "GetContactLists", Caption = "Get Contact Lists", ClassName = "KudosityDataSource")]
        public async Task<List<KudosityContactList>> GetContactLists()
        {
            var result = await GetEntityAsync("contactlists", new List<AppFilter>());
            return result.Select(item => JsonSerializer.Deserialize<KudosityContactList>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<KudosityContactList>().ToList();
        }

        [CommandAttribute(ObjectType = "KudosityAccount", PointType = EnumPointType.Function, Name = "GetAccount", Caption = "Get Account", ClassName = "KudosityDataSource")]
        public async Task<KudosityAccount> GetAccount()
        {
            var result = await GetEntityAsync("account", new List<AppFilter>());
            return result.FirstOrDefault() as KudosityAccount;
        }

        [CommandAttribute(ObjectType = "KudosityWebhook", PointType = EnumPointType.Function, Name = "GetWebhooks", Caption = "Get Webhooks", ClassName = "KudosityDataSource")]
        public async Task<List<KudosityWebhook>> GetWebhooks()
        {
            var result = await GetEntityAsync("webhooks", new List<AppFilter>());
            return result.Select(item => JsonSerializer.Deserialize<KudosityWebhook>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<KudosityWebhook>().ToList();
        }

        [CommandAttribute(ObjectType = "KudosityTemplate", PointType = EnumPointType.Function, Name = "GetTemplates", Caption = "Get Templates", ClassName = "KudosityDataSource")]
        public async Task<List<KudosityTemplate>> GetTemplates()
        {
            var result = await GetEntityAsync("templates", new List<AppFilter>());
            return result.Select(item => JsonSerializer.Deserialize<KudosityTemplate>(JsonSerializer.Serialize(item))).Where(x => x != null).Cast<KudosityTemplate>().ToList();
        }

        [CommandAttribute(ObjectType = "KudositySMS", PointType = EnumPointType.Function, Name = "SendSMS", Caption = "Send SMS Message", ClassName = "KudosityDataSource", misc = "ReturnType: IEnumerable<KudositySMSResponse>")]
        public async Task<IEnumerable<KudositySMSResponse>> SendSMSAsync(KudositySMS sms)
        {
            try
            {
                var url = "https://api.kudosity.com/api/v1/sms";
                var response = await PostAsync(url, sms);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var smsResponse = JsonSerializer.Deserialize<KudositySMSResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (smsResponse != null)
                    {
                        return new[] { smsResponse };
                    }
                }
                else
                {
                    Logger?.LogError($"Failed to send SMS: {json}");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error sending SMS: {ex.Message}");
            }
            return Array.Empty<KudositySMSResponse>();
        }

        [CommandAttribute(ObjectType = "KudosityContact", PointType = EnumPointType.Function, Name = "CreateContact", Caption = "Create Contact", ClassName = "KudosityDataSource", misc = "ReturnType: IEnumerable<KudosityContact>")]
        public async Task<IEnumerable<KudosityContact>> CreateContactAsync(KudosityContact contact)
        {
            try
            {
                var url = "https://api.kudosity.com/api/v1/contacts";
                var response = await PostAsync(url, contact);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var createdContact = JsonSerializer.Deserialize<KudosityContact>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (createdContact != null)
                    {
                        return new[] { createdContact };
                    }
                }
                else
                {
                    Logger?.LogError($"Failed to create contact: {json}");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating contact: {ex.Message}");
            }
            return Array.Empty<KudosityContact>();
        }

        [CommandAttribute(ObjectType = "KudosityCampaign", PointType = EnumPointType.Function, Name = "CreateCampaign", Caption = "Create Campaign", ClassName = "KudosityDataSource", misc = "ReturnType: IEnumerable<KudosityCampaign>")]
        public async Task<IEnumerable<KudosityCampaign>> CreateCampaignAsync(KudosityCampaign campaign)
        {
            try
            {
                var url = "https://api.kudosity.com/api/v1/campaigns";
                var response = await PostAsync(url, campaign);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var createdCampaign = JsonSerializer.Deserialize<KudosityCampaign>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (createdCampaign != null)
                    {
                        return new[] { createdCampaign };
                    }
                }
                else
                {
                    Logger?.LogError($"Failed to create campaign: {json}");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating campaign: {ex.Message}");
            }
            return Array.Empty<KudosityCampaign>();
        }
    }
}