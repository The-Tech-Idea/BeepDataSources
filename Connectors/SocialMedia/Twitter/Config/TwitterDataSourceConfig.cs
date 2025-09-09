using System;
using System.Collections.Generic;
using TheTechIdea.Beep.WebAPI;


namespace TheTechIdea.Beep.TwitterDataSource.Config
{
    /// <summary>
    /// Twitter-specific configuration extending WebAPIConnectionProperties
    /// </summary>
    public class TwitterDataSourceConfig : WebAPIConnectionProperties
    {
        /// <summary>
        /// Twitter API Key (Consumer Key) - maps to ClientId
        /// </summary>
        public string ConsumerKey
        {
            get => ClientId;
            set => ClientId = value;
        }

        /// <summary>
        /// Twitter API Secret (Consumer Secret) - maps to ClientSecret
        /// </summary>
        public string ConsumerSecret
        {
            get => ClientSecret;
            set => ClientSecret = value;
        }

        /// <summary>
        /// Access Token - maps to UserID
        /// </summary>
        public string AccessToken
        {
            get => UserID;
            set => UserID = value;
        }

        /// <summary>
        /// Access Token Secret - maps to Password
        /// </summary>
        public string AccessTokenSecret
        {
            get => Password;
            set => Password = value;
        }

        /// <summary>
        /// Bearer Token for API v2 - maps to ApiKey
        /// </summary>
        public string BearerToken
        {
            get => ApiKey;
            set => ApiKey = value;
        }

        /// <summary>
        /// Request timeout in milliseconds - maps to TimeoutMs
        /// </summary>
        public int TimeoutMs
        {
            get => base.TimeoutMs;
            set => base.TimeoutMs = value;
        }

        /// <summary>
        /// Maximum number of retries - maps to MaxRetries
        /// </summary>
        public int MaxRetries
        {
            get => base.MaxRetries;
            set => base.MaxRetries = value;
        }

        /// <summary>
        /// Twitter User ID
        /// </summary>
        public string? TwitterUserId { get; set; }

        /// <summary>
        /// Twitter Username
        /// </summary>
        public string? TwitterUsername { get; set; }

        /// <summary>
        /// API Version to use
        /// </summary>
        public string ApiVersion { get; set; } = "2";

        /// <summary>
        /// Whether to use rate limiting
        /// </summary>
        public bool UseRateLimiting { get; set; } = true;

        /// <summary>
        /// The rate limit requests per 15 minutes
        /// </summary>
        public int RateLimitPer15Min { get; set; } = 300;

        /// <summary>
        /// The rate limit requests per hour
        /// </summary>
        public int RateLimitPerHour { get; set; } = 300;

        /// <summary>
        /// The maximum cache size in MB
        /// </summary>
        public int MaxCacheSizeMB { get; set; } = 100;

        /// <summary>
        /// Whether to enable request logging
        /// </summary>
        public bool EnableLogging { get; set; } = true;

        /// <summary>
        /// The log level (Debug, Info, Warning, Error)
        /// </summary>
        public string LogLevel { get; set; } = "Info";

        /// <summary>
        /// Custom configuration settings
        /// </summary>
        public Dictionary<string, object> CustomSettings { get; set; } = new();

        /// <summary>
        /// Validates the configuration
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(BearerToken) ||
                   (!string.IsNullOrEmpty(ConsumerKey) && !string.IsNullOrEmpty(ConsumerSecret) &&
                    !string.IsNullOrEmpty(AccessToken) && !string.IsNullOrEmpty(AccessTokenSecret));
        }
    }
}
