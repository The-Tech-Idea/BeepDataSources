using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TheTechIdea.Beep.WebAPI;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Utilities;

namespace BeepDM.Connectors.Communication.Slack
{
    public class SlackConfig
    {
        public string? BotToken { get; set; }
        public string? UserToken { get; set; }
        public string? TeamId { get; set; }
    }

    public class SlackDataSource : WebAPIDataSource
    {
        private readonly SlackConfig _config;

        public SlackDataSource(string datasourcename, IDMLogger logger, IDMEEditor pDMEEditor, DataSourceType databasetype, IErrorsInfo per, SlackConfig config)
            : base(datasourcename, logger, pDMEEditor, databasetype, per)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            // Ensure Dataconnection and ConnectionProp are the WebAPI types
            if (!(Dataconnection is WebAPIDataConnection))
            {
                Dataconnection = new WebAPIDataConnection()
                {
                    Logger = Logger,
                    ErrorObject = ErrorObject,
                    DMEEditor = DMEEditor
                };
            }

            if (!(Dataconnection.ConnectionProp is WebAPIConnectionProperties))
            {
                Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }

            if (Dataconnection?.ConnectionProp is WebAPIConnectionProperties webApiProps)
            {
                webApiProps.ApiKey = _config.BotToken ?? _config.UserToken;
                webApiProps.ConnectionString = "https://slack.com/api/";
                webApiProps.TimeoutMs = 30000;
                webApiProps.MaxRetries = 3;
                webApiProps.EnableRateLimit = true;
                webApiProps.RateLimitRequestsPerMinute = 120;
            }

            // Provide basic entity list so base schema helpers can use it
            EntitiesNames = new List<string> { "channels", "messages", "users", "files", "reactions", "teams", "groups", "im", "mpim", "bots", "apps", "auth", "conversations", "pins", "reminders", "search", "stars", "team", "usergroups" };
            Entities = new List<EntityStructure>();
        }
    }
}
