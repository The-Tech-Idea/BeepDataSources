using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Vis;

namespace BeepDataSources.Connectors.SocialMedia.Snapchat
{
    /// <summary>
    /// Snapchat configuration class
    /// </summary>
    public class SnapchatConfig
    {
        /// <summary>
        /// Client ID for Snapchat API
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Client Secret for Snapchat API
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Access token for Snapchat API
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Refresh token for Snapchat API
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Organization ID for Snapchat API
        /// </summary>
        public string OrganizationId { get; set; } = string.Empty;

        /// <summary>
        /// Ad Account ID for Snapchat API
        /// </summary>
        public string AdAccountId { get; set; } = string.Empty;

        /// <summary>
        /// API version for Snapchat API
        /// </summary>
        public string ApiVersion { get; set; } = "v1";

        /// <summary>
        /// Base URL for Snapchat API
        /// </summary>
        public string BaseUrl { get; set; } = "https://adsapi.snapchat.com";

        /// <summary>
        /// Timeout in seconds for API calls
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum number of retries for failed requests
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Rate limit delay in milliseconds between requests
        /// </summary>
        public int RateLimitDelayMs { get; set; } = 1000;
    }

    /// <summary>
    /// Snapchat data source implementation for Beep framework
    /// Supports Snapchat Marketing API
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Snapchat)]
    public class SnapchatDataSource : WebAPIDataSource
    {

    /// <summary>
    /// Constructor for SnapchatDataSource
    /// </summary>
    public SnapchatDataSource(string datasourcename, IDMLogger logger, IDMEEditor editor, DataSourceType type, IErrorsInfo errors)
        : base(datasourcename, logger, editor, type, errors)
    {
        InitializeEntities();
    }

    /// <summary>
    /// Initialize entities for Snapchat data source
    /// </summary>
    private void InitializeEntities()
    {
        // Organizations
        Entities["organizations"] = new EntityStructure
        {
            EntityName = "organizations",
            ViewID = 1,
            Fields = new List<EntityField>
            {
                new EntityField { fieldname = "id", fieldtype = "string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { fieldname = "name", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "type", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "country", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "timezone", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "created_at", fieldtype = "datetime", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "updated_at", fieldtype = "datetime", ValueRetrievedFromParent = false }
            }
        };

        // Ad Accounts
        Entities["adaccounts"] = new EntityStructure
        {
            EntityName = "adaccounts",
            ViewID = 2,
            Fields = new List<EntityField>
            {
                new EntityField { fieldname = "id", fieldtype = "string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { fieldname = "name", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "organization_id", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "currency", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "timezone", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "status", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "created_at", fieldtype = "datetime", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "updated_at", fieldtype = "datetime", ValueRetrievedFromParent = false }
            }
        };

        // Campaigns
        Entities["campaigns"] = new EntityStructure
        {
            EntityName = "campaigns",
            ViewID = 3,
            Fields = new List<EntityField>
            {
                new EntityField { fieldname = "id", fieldtype = "string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { fieldname = "name", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "ad_account_id", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "status", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "objective", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "start_time", fieldtype = "datetime", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "end_time", fieldtype = "datetime", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "budget_micro", fieldtype = "long", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "created_at", fieldtype = "datetime", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "updated_at", fieldtype = "datetime", ValueRetrievedFromParent = false }
            }
        };

        // Ads
        Entities["ads"] = new EntityStructure
        {
            EntityName = "ads",
            ViewID = 4,
            Fields = new List<EntityField>
            {
                new EntityField { fieldname = "id", fieldtype = "string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { fieldname = "name", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "campaign_id", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "ad_squad_id", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "status", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "type", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "creative_id", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "created_at", fieldtype = "datetime", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "updated_at", fieldtype = "datetime", ValueRetrievedFromParent = false }
            }
        };

        // Ad Squads
        Entities["adsquads"] = new EntityStructure
        {
            EntityName = "adsquads",
            ViewID = 5,
            Fields = new List<EntityField>
            {
                new EntityField { fieldname = "id", fieldtype = "string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { fieldname = "name", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "campaign_id", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "status", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "targeting", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "billing_event", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "bid_micro", fieldtype = "long", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "created_at", fieldtype = "datetime", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "updated_at", fieldtype = "datetime", ValueRetrievedFromParent = false }
            }
        };

        // Creatives
        Entities["creatives"] = new EntityStructure
        {
            EntityName = "creatives",
            ViewID = 6,
            Fields = new List<EntityField>
            {
                new EntityField { fieldname = "id", fieldtype = "string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { fieldname = "name", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "ad_account_id", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "type", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "headline", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "call_to_action", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "media", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "created_at", fieldtype = "datetime", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "updated_at", fieldtype = "datetime", ValueRetrievedFromParent = false }
            }
        };

        // Analytics
        Entities["analytics"] = new EntityStructure
        {
            EntityName = "analytics",
            ViewID = 7,
            Fields = new List<EntityField>
            {
                new EntityField { fieldname = "date", fieldtype = "datetime", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { fieldname = "campaign_id", fieldtype = "string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { fieldname = "impressions", fieldtype = "integer", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "swipes", fieldtype = "integer", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "spend", fieldtype = "decimal", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "clicks", fieldtype = "integer", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "conversions", fieldtype = "integer", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "frequency", fieldtype = "decimal", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "reach", fieldtype = "integer", ValueRetrievedFromParent = false }
            }
        };

        // Audiences
        Entities["audiences"] = new EntityStructure
        {
            EntityName = "audiences",
            ViewID = 8,
            Fields = new List<EntityField>
            {
                new EntityField { fieldname = "id", fieldtype = "string", ValueRetrievedFromParent = false, IsKey = true },
                new EntityField { fieldname = "name", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "ad_account_id", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "size", fieldtype = "integer", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "status", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "type", fieldtype = "string", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "created_at", fieldtype = "datetime", ValueRetrievedFromParent = false },
                new EntityField { fieldname = "updated_at", fieldtype = "datetime", ValueRetrievedFromParent = false }
            }
        };

        // Update EntitiesNames collection
        EntitiesNames.AddRange(Entities.Keys);
    }    /// <summary>
    /// Connect to Snapchat API
    /// </summary>
    public override async Task<IErrorsInfo> ConnectAsync(WebAPIConnectionProperties properties)
    {
        try
        {
            if (string.IsNullOrEmpty(properties.AccessToken))
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = "Access token is required for Snapchat connection";
                return ErrorObject;
            }

            // Test connection by getting organizations
            var testUrl = $"{properties.BaseUrl}/organizations";
            var response = await HttpClient.GetAsync(testUrl);

            if (response.IsSuccessStatusCode)
            {
                ErrorObject.Flag = Errors.Ok;
                ErrorObject.Message = "Successfully connected to Snapchat API";
                return ErrorObject;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Snapchat API connection failed: {response.StatusCode} - {errorContent}";
                return ErrorObject;
            }
        }
        catch (Exception ex)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = $"Failed to connect to Snapchat API: {ex.Message}";
            return ErrorObject;
        }
    }

    /// <summary>
    /// Disconnect from Snapchat API
    /// </summary>
    public override async Task<IErrorsInfo> DisconnectAsync()
    {
        ErrorObject.Flag = Errors.Ok;
        ErrorObject.Message = "Successfully disconnected from Snapchat API";
        return ErrorObject;
    }

    /// <summary>
    /// Get data from Snapchat API
    /// </summary>
    public override async Task<IErrorsInfo> GetEntityAsync(string entityName, List<AppFilter> filters = null)
    {
        try
        {
            filters ??= new List<AppFilter>();

            string url;

            switch (entityName.ToLower())
            {
                case "organizations":
                    url = $"{ConnectionProperties.BaseUrl}/organizations";
                    break;

                case "adaccounts":
                    var orgId = filters.FirstOrDefault(f => f.FieldName == "organization_id")?.FieldValue?.ToString() ?? ConnectionProperties.GetPropertyValue("OrganizationId")?.ToString();
                    url = $"{ConnectionProperties.BaseUrl}/organizations/{orgId}/adaccounts";
                    break;

                case "campaigns":
                    var adAccountId = filters.FirstOrDefault(f => f.FieldName == "ad_account_id")?.FieldValue?.ToString() ?? ConnectionProperties.GetPropertyValue("AdAccountId")?.ToString();
                    url = $"{ConnectionProperties.BaseUrl}/adaccounts/{adAccountId}/campaigns";
                    break;

                case "ads":
                    var campaignId = filters.FirstOrDefault(f => f.FieldName == "campaign_id")?.FieldValue?.ToString() ?? "";
                    url = $"{ConnectionProperties.BaseUrl}/campaigns/{campaignId}/ads";
                    break;

                case "adsquads":
                    var adsquadCampaignId = filters.FirstOrDefault(f => f.FieldName == "campaign_id")?.FieldValue?.ToString() ?? "";
                    url = $"{ConnectionProperties.BaseUrl}/campaigns/{adsquadCampaignId}/adsquads";
                    break;

                case "creatives":
                    var creativeAdAccountId = filters.FirstOrDefault(f => f.FieldName == "ad_account_id")?.FieldValue?.ToString() ?? ConnectionProperties.GetPropertyValue("AdAccountId")?.ToString();
                    url = $"{ConnectionProperties.BaseUrl}/adaccounts/{creativeAdAccountId}/creatives";
                    break;

                case "analytics":
                    var analyticsAdAccountId = filters.FirstOrDefault(f => f.FieldName == "ad_account_id")?.FieldValue?.ToString() ?? ConnectionProperties.GetPropertyValue("AdAccountId")?.ToString();
                    var startTime = filters.FirstOrDefault(f => f.FieldName == "start_time")?.FieldValue?.ToString() ?? DateTime.Now.AddDays(-30).ToString("yyyy-MM-ddTHH:mm:ssZ");
                    var endTime = filters.FirstOrDefault(f => f.FieldName == "end_time")?.FieldValue?.ToString() ?? DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    url = $"{ConnectionProperties.BaseUrl}/adaccounts/{analyticsAdAccountId}/stats?start_time={startTime}&end_time={endTime}";
                    break;

                case "audiences":
                    var audienceAdAccountId = filters.FirstOrDefault(f => f.FieldName == "ad_account_id")?.FieldValue?.ToString() ?? ConnectionProperties.GetPropertyValue("AdAccountId")?.ToString();
                    url = $"{ConnectionProperties.BaseUrl}/adaccounts/{audienceAdAccountId}/audiences";
                    break;

                default:
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"Unsupported entity: {entityName}";
                    return ErrorObject;
            }

            // Rate limiting delay
            var rateLimitDelay = ConnectionProperties.GetPropertyValue("RateLimitDelayMs");
            if (rateLimitDelay != null && int.TryParse(rateLimitDelay.ToString(), out var delay) && delay > 0)
            {
                await Task.Delay(delay);
            }

            var response = await HttpClient.GetAsync(url);
            var jsonContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = $"Snapchat API request failed: {response.StatusCode} - {jsonContent}";
                return ErrorObject;
            }

            // Parse and store the data
            var dataTable = ParseJsonToDataTable(jsonContent, entityName);
            if (Entities.ContainsKey(entityName))
            {
                Entities[entityName].EntityData = dataTable;
            }

            ErrorObject.Flag = Errors.Ok;
            ErrorObject.Message = $"Successfully retrieved {entityName} data";
            return ErrorObject;
        }
        catch (Exception ex)
        {
            ErrorObject.Flag = Errors.Failed;
            ErrorObject.Message = $"Failed to get {entityName} data: {ex.Message}";
            return ErrorObject;
        }
    }

        /// <summary>
        /// Parse JSON response to DataTable
        /// </summary>
        private DataTable ParseJsonToDataTable(string jsonContent, string entityName)
        {
            var dataTable = new DataTable(entityName);
            var entityStructure = Entities.ContainsKey(entityName.ToLower()) ? Entities[entityName.ToLower()] : null;

            try
            {
                using var document = JsonDocument.Parse(jsonContent);
                var root = document.RootElement;

                // Handle Snapchat API response structure
                JsonElement dataElement;
                if (root.TryGetProperty("organizations", out var organizationsProp))
                {
                    dataElement = organizationsProp;
                }
                else if (root.TryGetProperty("adaccounts", out var adaccountsProp))
                {
                    dataElement = adaccountsProp;
                }
                else if (root.TryGetProperty("campaigns", out var campaignsProp))
                {
                    dataElement = campaignsProp;
                }
                else if (root.TryGetProperty("ads", out var adsProp))
                {
                    dataElement = adsProp;
                }
                else if (root.TryGetProperty("adsquads", out var adsquadsProp))
                {
                    dataElement = adsquadsProp;
                }
                else if (root.TryGetProperty("creatives", out var creativesProp))
                {
                    dataElement = creativesProp;
                }
                else if (root.TryGetProperty("stats", out var statsProp))
                {
                    dataElement = statsProp;
                }
                else if (root.TryGetProperty("audiences", out var audiencesProp))
                {
                    dataElement = audiencesProp;
                }
                else
                {
                    // Single object response
                    dataElement = root;
                }

                // Create columns based on metadata or first object
                if (entityStructure != null)
                {
                    foreach (var field in entityStructure.Fields)
                    {
                        dataTable.Columns.Add(field.fieldname, GetFieldType(field.fieldtype));
                    }
                }
                else if (dataElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in dataElement.EnumerateObject())
                    {
                        dataTable.Columns.Add(property.Name, typeof(string));
                    }
                }
                else if (dataElement.ValueKind == JsonValueKind.Array && dataElement.GetArrayLength() > 0)
                {
                    var firstItem = dataElement[0];
                    foreach (var property in firstItem.EnumerateObject())
                    {
                        dataTable.Columns.Add(property.Name, typeof(string));
                    }
                }

                // Add rows
                if (dataElement.ValueKind == JsonValueKind.Object)
                {
                    var row = dataTable.NewRow();
                    foreach (var property in dataElement.EnumerateObject())
                    {
                        if (dataTable.Columns.Contains(property.Name))
                        {
                            row[property.Name] = GetJsonValue(property.Value);
                        }
                    }
                    dataTable.Rows.Add(row);
                }
                else if (dataElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataElement.EnumerateArray())
                    {
                        var row = dataTable.NewRow();
                        foreach (var property in item.EnumerateObject())
                        {
                            if (dataTable.Columns.Contains(property.Name))
                            {
                                row[property.Name] = GetJsonValue(property.Value);
                            }
                        }
                        dataTable.Rows.Add(row);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse JSON response: {ex.Message}", ex);
            }

            return dataTable;
        }

        /// <summary>
        /// Get .NET type from field type string
        /// </summary>
        private Type GetFieldType(string fieldType)
        {
            return fieldType.ToLower() switch
            {
                "string" => typeof(string),
                "integer" => typeof(int),
                "long" => typeof(long),
                "decimal" => typeof(decimal),
                "boolean" => typeof(bool),
                "datetime" => typeof(DateTime),
                _ => typeof(string)
            };
        }

        /// <summary>
        /// Get value from JSON element
        /// </summary>
        private object GetJsonValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.GetRawText()
            };
        }

        [CommandAttribute(ObjectType = "SnapchatOrganization", PointType = EnumPointType.Function, Name = "GetOrganizations", Caption = "Get Snapchat Organizations", ClassName = "SnapchatDataSource")]
        public async Task<SnapchatResponse<SnapchatOrganization>> GetOrganizations()
        {
            var url = $"{ConnectionProperties.BaseUrl}/organizations";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SnapchatResponse<SnapchatOrganization>>(json);
        }

        [CommandAttribute(ObjectType = "SnapchatAdAccount", PointType = EnumPointType.Function, Name = "GetAdAccounts", Caption = "Get Snapchat Ad Accounts", ClassName = "SnapchatDataSource")]
        public async Task<SnapchatResponse<SnapchatAdAccount>> GetAdAccounts(string organizationId = null)
        {
            var orgParam = string.IsNullOrEmpty(organizationId) ? "" : $"?organization_id={organizationId}";
            var url = $"{ConnectionProperties.BaseUrl}/adaccounts{orgParam}";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SnapchatResponse<SnapchatAdAccount>>(json);
        }

        [CommandAttribute(ObjectType = "SnapchatCampaign", PointType = EnumPointType.Function, Name = "GetCampaigns", Caption = "Get Snapchat Campaigns", ClassName = "SnapchatDataSource")]
        public async Task<SnapchatResponse<SnapchatCampaign>> GetCampaigns(string adAccountId)
        {
            var url = $"{ConnectionProperties.BaseUrl}/campaigns?ad_account_id={adAccountId}";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SnapchatResponse<SnapchatCampaign>>(json);
        }

        [CommandAttribute(ObjectType = "SnapchatAdSquad", PointType = EnumPointType.Function, Name = "GetAdSquads", Caption = "Get Snapchat Ad Squads", ClassName = "SnapchatDataSource")]
        public async Task<SnapchatResponse<SnapchatAdSquad>> GetAdSquads(string campaignId)
        {
            var url = $"{ConnectionProperties.BaseUrl}/adsquads?campaign_id={campaignId}";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SnapchatResponse<SnapchatAdSquad>>(json);
        }

        [CommandAttribute(ObjectType = "SnapchatAd", PointType = EnumPointType.Function, Name = "GetAds", Caption = "Get Snapchat Ads", ClassName = "SnapchatDataSource")]
        public async Task<SnapchatResponse<SnapchatAd>> GetAds(string adSquadId)
        {
            var url = $"{ConnectionProperties.BaseUrl}/ads?ad_squad_id={adSquadId}";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SnapchatResponse<SnapchatAd>>(json);
        }

        [CommandAttribute(ObjectType = "SnapchatCreative", PointType = EnumPointType.Function, Name = "GetCreatives", Caption = "Get Snapchat Creatives", ClassName = "SnapchatDataSource")]
        public async Task<SnapchatResponse<SnapchatCreative>> GetCreatives(string adAccountId)
        {
            var url = $"{ConnectionProperties.BaseUrl}/creatives?ad_account_id={adAccountId}";
            var response = await GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SnapchatResponse<SnapchatCreative>>(json);
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Snapchat, PointType = EnumPointType.Function, ObjectType = "SnapchatCampaign", Name = "CreateCampaign", Caption = "Create Snapchat Campaign", ClassType = "SnapchatDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "snapchat.png", misc = "ReturnType: IEnumerable<SnapchatCampaign>")]
        public async Task<IEnumerable<SnapchatCampaign>> CreateCampaignAsync(SnapchatCampaign campaign)
        {
            try
            {
                var result = await PostAsync($"{ConnectionProperties.BaseUrl}/campaigns", campaign);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var response = JsonSerializer.Deserialize<SnapchatResponse<SnapchatCampaign>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    var createdCampaign = response?.Campaigns?.FirstOrDefault();
                    if (createdCampaign != null)
                    {
                        return new List<SnapchatCampaign> { createdCampaign }.Select(c => c.Attach<SnapchatCampaign>(this));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating campaign: {ex.Message}");
            }
            return new List<SnapchatCampaign>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Snapchat, PointType = EnumPointType.Function, ObjectType = "SnapchatCampaign", Name = "UpdateCampaign", Caption = "Update Snapchat Campaign", ClassType = "SnapchatDataSource", Showin = ShowinType.Both, Order = 11, iconimage = "snapchat.png", misc = "ReturnType: IEnumerable<SnapchatCampaign>")]
        public async Task<IEnumerable<SnapchatCampaign>> UpdateCampaignAsync(SnapchatCampaign campaign)
        {
            try
            {
                var result = await PutAsync($"{ConnectionProperties.BaseUrl}/campaigns/{campaign.Id}", campaign);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var response = JsonSerializer.Deserialize<SnapchatResponse<SnapchatCampaign>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    var updatedCampaign = response?.Campaigns?.FirstOrDefault();
                    if (updatedCampaign != null)
                    {
                        return new List<SnapchatCampaign> { updatedCampaign }.Select(c => c.Attach<SnapchatCampaign>(this));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error updating campaign: {ex.Message}");
            }
            return new List<SnapchatCampaign>();
        }
    }
}
