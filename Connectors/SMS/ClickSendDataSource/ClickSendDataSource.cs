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
                string url = $"https://rest.clicksend.com/v3/{endpoint}";

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
    }
}