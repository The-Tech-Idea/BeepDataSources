// File: Connectors/SocialMedia/Pinterest/PinterestDataSource.cs
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
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Connectors.Pinterest.Models;

namespace TheTechIdea.Beep.PinterestDataSource
{
    /// <summary>
    /// Pinterest data source implementation using WebAPIDataSource as base class
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Pinterest)]
    public class PinterestDataSource : WebAPIDataSource
    {
        // Fixed, supported entities
        private static readonly List<string> KnownEntities = new()
        {
            "pins",
            "boards",
            "users",
            "analytics"
        };

        /// <summary>
        /// Initializes a new instance of the PinterestDataSource class
        /// </summary>
        public PinterestDataSource(
            string datasourcename,
            IDMLogger logger,
            IDMEEditor dmeEditor,
            DataSourceType databasetype,
            IErrorsInfo errorObject)
            : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI props (Url/Auth) exist (configure outside this class)
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();

            // Register entities
            InitializeEntities();
        }

        public override string ColumnDelimiter { get => ","; set => base.ColumnDelimiter = value; }
        public override string ParameterDelimiter { get => ":"; set => base.ParameterDelimiter = value; }
        public override string RowDelimiter { get => Environment.NewLine; set => base.RowDelimiter = value; }

        // Register entities
        private void InitializeEntities()
        {
            EntitiesNames = KnownEntities.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // Return the fixed list
        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        // -------------------- Overrides (same signatures) --------------------

        // Sync
        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
            return data ?? Array.Empty<object>();
        }

        // Async
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            try
            {
                if (!KnownEntities.Contains(EntityName, StringComparer.OrdinalIgnoreCase))
                {
                    ErrorObject.Flag = Errors.Failed;
                    ErrorObject.Message = $"Unknown Pinterest entity: {EntityName}";
                    return Array.Empty<object>();
                }

                // Pinterest API endpoints would be implemented here
                // For now, return empty as this is a stub
                await Task.CompletedTask;
                return Array.Empty<object>();
            }
            catch (Exception ex)
            {
                ErrorObject.Flag = Errors.Failed;
                ErrorObject.Message = ex.Message;
                return Array.Empty<object>();
            }
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

        // POST methods for creating entities
        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Pinterest, PointType = EnumPointType.Function, ObjectType = "PinterestPin", Name = "CreatePin", Caption = "Create Pinterest Pin", ClassType = "PinterestDataSource", Showin = ShowinType.Both, Order = 10, iconimage = "pinterest.png", misc = "ReturnType: IEnumerable<PinterestPin>")]
        public async Task<IEnumerable<PinterestPin>> CreatePinAsync(PinterestPin pin)
        {
            try
            {
                var result = await PostAsync("pins", pin);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdPin = JsonSerializer.Deserialize<PinterestPin>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<PinterestPin> { createdPin }.Select(p => p.Attach<PinterestPin>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating pin: {ex.Message}");
            }
            return new List<PinterestPin>();
        }

        [CommandAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.Pinterest, PointType = EnumPointType.Function, ObjectType = "PinterestBoard", Name = "CreateBoard", Caption = "Create Pinterest Board", ClassType = "PinterestDataSource", Showin = ShowinType.Both, Order = 11, iconimage = "pinterest.png", misc = "ReturnType: IEnumerable<PinterestBoard>")]
        public async Task<IEnumerable<PinterestBoard>> CreateBoardAsync(PinterestBoard board)
        {
            try
            {
                var result = await PostAsync("boards", board);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync();
                    var createdBoard = JsonSerializer.Deserialize<PinterestBoard>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return new List<PinterestBoard> { createdBoard }.Select(b => b.Attach<PinterestBoard>(this));
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error creating board: {ex.Message}");
            }
            return new List<PinterestBoard>();
        }
    }
}
