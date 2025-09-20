using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;

namespace TheTechIdea.Beep.FacebookDataSource
{
    /// <summary>
    /// Facebook data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Facebook)]
    public class FacebookDataSource : WebAPIDataSource
    {
        private readonly FacebookDataSourceConfig _config;

        /// <summary>
        /// Initializes a new instance of the FacebookDataSource class
        /// </summary>
        public FacebookDataSource(FacebookDataSourceConfig config)
            : base(config?.BaseUrl ?? "https://graph.facebook.com/v18.0",
                   config?.AccessToken ?? throw new ArgumentNullException(nameof(config.AccessToken)),
                   config?.Logger,
                   config?.ConfigEditor,
                   config?.DMLogger,
                   config?.ErrorObject,
                   config?.VisManager,
                   config?.ProgressReport)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (!_config.IsValid())
            {
                throw new ArgumentException("Invalid Facebook configuration");
            }

            // Set up entity mappings
            EntityMappings = new Dictionary<string, string>
            {
                { "posts", "posts" },
                { "pages", "pages" },
                { "groups", "groups" },
                { "events", "events" },
                { "ads", "ads" },
                { "insights", "insights" }
            };

            // Set up default headers
            DefaultHeaders = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {_config.AccessToken}" },
                { "Content-Type", "application/json" }
            };
        }

        /// <summary>
        /// Gets the list of available entities
        /// </summary>
        public override List<string> GetEntities()
        {
            return EntityMappings.Keys.ToList();
        }

        /// <summary>
        /// Gets entity metadata
        /// </summary>
        public override EntityMetadata GetEntityMetadata(string entityName)
        {
            if (!EntityMappings.ContainsKey(entityName))
            {
                throw new ArgumentException($"Entity '{entityName}' not found");
            }

            return new EntityMetadata
            {
                EntityName = entityName,
                Fields = GetEntityFields(entityName)
            };
        }

        /// <summary>
        /// Gets entity data asynchronously
        /// </summary>
        public override async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!EntityMappings.ContainsKey(entityName))
            {
                throw new ArgumentException($"Entity '{entityName}' not found");
            }

            string endpoint = GetEntityEndpoint(entityName, parameters);
            string jsonResponse = await GetAsync(endpoint);

            return FacebookHelpers.ParseEntityData(jsonResponse, entityName);
        }

        /// <summary>
        /// Creates a new entity record
        /// </summary>
        public override async Task<bool> CreateAsync(string entityName, Dictionary<string, object> data)
        {
            if (!EntityMappings.ContainsKey(entityName))
            {
                throw new ArgumentException($"Entity '{entityName}' not found");
            }

            string endpoint = GetCreateEndpoint(entityName);
            string jsonData = JsonSerializer.Serialize(data);

            string response = await PostAsync(endpoint, jsonData);
            return !string.IsNullOrEmpty(response);
        }

        /// <summary>
        /// Updates an existing entity record
        /// </summary>
        public override async Task<bool> UpdateAsync(string entityName, Dictionary<string, object> data, string id)
        {
            if (!EntityMappings.ContainsKey(entityName))
            {
                throw new ArgumentException($"Entity '{entityName}' not found");
            }

            string endpoint = $"{GetCreateEndpoint(entityName)}/{id}";
            string jsonData = JsonSerializer.Serialize(data);

            string response = await PostAsync(endpoint, jsonData);
            return !string.IsNullOrEmpty(response);
        }

        /// <summary>
        /// Deletes an entity record
        /// </summary>
        public override async Task<bool> DeleteAsync(string entityName, string id)
        {
            if (!EntityMappings.ContainsKey(entityName))
            {
                throw new ArgumentException($"Entity '{entityName}' not found");
            }

            string endpoint = $"{GetCreateEndpoint(entityName)}/{id}";
            string response = await DeleteAsync(endpoint);

            return !string.IsNullOrEmpty(response);
        }

        /// <summary>
        /// Gets the entity endpoint
        /// </summary>
        private string GetEntityEndpoint(string entityName, Dictionary<string, object> parameters = null)
        {
            string baseEndpoint = $"{_config.UserId}/{EntityMappings[entityName]}";
            string fields = GetDefaultFields(entityName);

            string endpoint = $"{baseEndpoint}?fields={fields}";

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    endpoint += $"&{param.Key}={param.Value}";
                }
            }

            return endpoint;
        }

        /// <summary>
        /// Gets the create endpoint
        /// </summary>
        private string GetCreateEndpoint(string entityName)
        {
            return $"{_config.UserId}/{EntityMappings[entityName]}";
        }

        /// <summary>
        /// Gets default fields for an entity
        /// </summary>
        private string GetDefaultFields(string entityName)
        {
            return entityName switch
            {
                "posts" => "id,message,created_time,updated_time,likes.summary(true),comments.summary(true)",
                "pages" => "id,name,category,description,website,phone,emails,location",
                "groups" => "id,name,description,privacy,updated_time",
                "events" => "id,name,description,start_time,end_time,place,attending_count,interested_count",
                "ads" => "id,name,status,created_time,updated_time,insights",
                "insights" => "id,name,period,values,title,description",
                _ => "id,name"
            };
        }

        /// <summary>
        /// Gets entity fields
        /// </summary>
        private List<EntityField> GetEntityFields(string entityName)
        {
            return entityName switch
            {
                "posts" => new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "string" },
                    new EntityField { fieldname = "message", fieldtype = "string" },
                    new EntityField { fieldname = "created_time", fieldtype = "datetime" },
                    new EntityField { fieldname = "updated_time", fieldtype = "datetime" },
                    new EntityField { fieldname = "likes_count", fieldtype = "int" },
                    new EntityField { fieldname = "comments_count", fieldtype = "int" }
                },
                "pages" => new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "string" },
                    new EntityField { fieldname = "name", fieldtype = "string" },
                    new EntityField { fieldname = "category", fieldtype = "string" },
                    new EntityField { fieldname = "description", fieldtype = "string" },
                    new EntityField { fieldname = "website", fieldtype = "string" },
                    new EntityField { fieldname = "phone", fieldtype = "string" }
                },
                "groups" => new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "string" },
                    new EntityField { fieldname = "name", fieldtype = "string" },
                    new EntityField { fieldname = "description", fieldtype = "string" },
                    new EntityField { fieldname = "privacy", fieldtype = "string" },
                    new EntityField { fieldname = "updated_time", fieldtype = "datetime" }
                },
                "events" => new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "string" },
                    new EntityField { fieldname = "name", fieldtype = "string" },
                    new EntityField { fieldname = "description", fieldtype = "string" },
                    new EntityField { fieldname = "start_time", fieldtype = "datetime" },
                    new EntityField { fieldname = "end_time", fieldtype = "datetime" },
                    new EntityField { fieldname = "attending_count", fieldtype = "int" },
                    new EntityField { fieldname = "interested_count", fieldtype = "int" }
                },
                "ads" => new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "string" },
                    new EntityField { fieldname = "name", fieldtype = "string" },
                    new EntityField { fieldname = "status", fieldtype = "string" },
                    new EntityField { fieldname = "created_time", fieldtype = "datetime" },
                    new EntityField { fieldname = "updated_time", fieldtype = "datetime" }
                },
                "insights" => new List<EntityField>
                {
                    new EntityField { fieldname = "id", fieldtype = "string" },
                    new EntityField { fieldname = "name", fieldtype = "string" },
                    new EntityField { fieldname = "period", fieldtype = "string" },
                    new EntityField { fieldname = "values", fieldtype = "string" },
                    new EntityField { fieldname = "title", fieldtype = "string" },
                    new EntityField { fieldname = "description", fieldtype = "string" }
                },
                _ => new List<EntityField>()
            };
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            DisconnectAsync().Wait();
        }
    }
}