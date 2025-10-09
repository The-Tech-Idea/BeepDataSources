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
    }
}