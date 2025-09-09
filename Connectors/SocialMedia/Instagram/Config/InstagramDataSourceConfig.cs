using System;
using System.Collections.Generic;
using TheTechIdea.Beep.WebAPI;

namespace TheTechIdea.Beep.InstagramDataSource.Config
{
    /// <summary>
    /// Instagram-specific configuration extending WebAPIConnectionProperties
    /// </summary>
    public class InstagramDataSourceConfig : WebAPIConnectionProperties
    {
        /// <summary>
        /// Instagram App ID - maps to ClientId
        /// </summary>
        public string AppId
        {
            get => ClientId;
            set => ClientId = value;
        }

        /// <summary>
        /// Instagram App Secret - maps to ClientSecret
        /// </summary>
        public string AppSecret
        {
            get => ClientSecret;
            set => ClientSecret = value;
        }

        /// <summary>
        /// Access token for Instagram Basic Display API - maps to ApiKey
        /// </summary>
        public string AccessToken
        {
            get => ApiKey;
            set => ApiKey = value;
        }

        /// <summary>
        /// User ID for the Instagram account - maps to UserID
        /// </summary>
        public string? UserId
        {
            get => UserID;
            set => UserID = value;
        }

        /// <summary>
        /// API version for Instagram Graph API
        /// </summary>
        public string ApiVersion { get; set; } = "v18.0";

        /// <summary>
        /// Base URL for Instagram Graph API
        /// </summary>
        public string GraphApiUrl => $"https://graph.instagram.com/{ApiVersion}";

        /// <summary>
        /// Base URL for Instagram Basic Display API
        /// </summary>
        public string BasicDisplayUrl => "https://graph.instagram.com";

        /// <summary>
        /// Rate limit delay between requests in milliseconds
        /// </summary>
        public int RateLimitDelayMs { get; set; } = 1000;

        /// <summary>
        /// Whether to use Instagram Graph API (true) or Basic Display API (false)
        /// </summary>
        public bool UseGraphApi { get; set; } = true;

        /// <summary>
        /// Custom configuration settings
        /// </summary>
        public Dictionary<string, object> CustomSettings { get; set; } = new();

        /// <summary>
        /// Validates the configuration
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(AccessToken) &&
                   (!string.IsNullOrEmpty(AppId) || !string.IsNullOrEmpty(AppSecret));
        }
    }
}
