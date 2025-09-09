using System;
using System.Collections.Generic;
using TheTechIdea.Beep.WebAPI;

namespace TheTechIdea.Beep.FacebookDataSource.Config
{
    /// <summary>
    /// Facebook-specific configuration extending WebAPIConnectionProperties
    /// </summary>
    public class FacebookDataSourceConfig : WebAPIConnectionProperties
    {
        /// <summary>
        /// The Facebook App ID
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// The Facebook App Secret
        /// </summary>
        public string AppSecret { get; set; }

        /// <summary>
        /// The Facebook Access Token (use ApiKey property from base class for this)
        /// </summary>
        public string AccessToken
        {
            get => ApiKey;
            set => ApiKey = value;
        }

        /// <summary>
        /// The Facebook Page Access Token
        /// </summary>
        public string PageAccessToken { get; set; }

        /// <summary>
        /// The Facebook User ID
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// The Facebook Page ID
        /// </summary>
        public string PageId { get; set; }

        /// <summary>
        /// The Facebook API version to use
        /// </summary>
        public string ApiVersion { get; set; } = "v18.0";

        /// <summary>
        /// Whether to use rate limiting
        /// </summary>
        public bool UseRateLimiting { get; set; } = true;

        /// <summary>
        /// The rate limit requests per hour
        /// </summary>
        public int RateLimitPerHour { get; set; } = 200;

        /// <summary>
        /// The rate limit requests per day
        /// </summary>
        public int RateLimitPerDay { get; set; } = 5000;

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
        /// The webhook settings for real-time updates
        /// </summary>
        public FacebookWebhookConfig Webhook { get; set; } = new();

        /// <summary>
        /// The permissions required for the application
        /// </summary>
        public List<string> Permissions { get; set; } = new();

        /// <summary>
        /// The fields to retrieve for different entities
        /// </summary>
        public FacebookFieldsConfig Fields { get; set; } = new();

        /// <summary>
        /// Custom configuration settings
        /// </summary>
        public Dictionary<string, object> CustomSettings { get; set; } = new();

        /// <summary>
        /// Constructor - initializes Facebook-specific defaults
        /// </summary>
        public FacebookDataSourceConfig()
        {
            // Set Facebook-specific defaults
            ConnectionName = "Facebook Data Source";
            ConnectionString = "https://graph.facebook.com";
            DatabaseType = DataSourceType.WebApi;
            Category = DatasourceCategory.WEBAPI;
            DriverName = "FacebookGraphAPI";
            DriverVersion = "v18.0";
            UserAgent = "BeepDM-Facebook/1.0";

            // Set authentication type to Bearer (for access token)
            AuthType = AuthTypeEnum.Bearer;

            // Set default headers
            Headers.Add(new WebApiHeader { Headername = "Accept", Headervalue = "application/json" });

            // Initialize permissions with common Facebook permissions
            Permissions.AddRange(new[]
            {
                "email", "public_profile", "pages_read_engagement", "pages_manage_posts",
                "pages_show_list", "read_insights", "ads_read", "groups_access_member_info"
            });
        }

        /// <summary>
        /// Validates the Facebook-specific configuration
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(AccessToken) ||
                   (!string.IsNullOrEmpty(AppId) && !string.IsNullOrEmpty(AppSecret));
        }

        /// <summary>
        /// Gets the authorization header value
        /// </summary>
        public string GetAuthorizationHeader()
        {
            return $"Bearer {AccessToken}";
        }

        /// <summary>
        /// Gets the API endpoint URL
        /// </summary>
        public string GetApiUrl(string path = "")
        {
            var baseUrl = string.IsNullOrEmpty(ConnectionString) ? "https://graph.facebook.com" : ConnectionString;
            return $"{baseUrl}/{ApiVersion}/{path.TrimStart('/')}";
        }
    }

    /// <summary>
    /// Proxy configuration for Facebook API requests
    /// </summary>
    public class FacebookProxyConfig
    {
        /// <summary>
        /// Whether to use a proxy
        /// </summary>
        public bool UseProxy { get; set; } = false;

        /// <summary>
        /// The proxy URL
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The proxy username
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The proxy password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// The proxy domain
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Whether to bypass proxy for local addresses
        /// </summary>
        public bool BypassProxyOnLocal { get; set; } = true;
    }

    /// <summary>
    /// Webhook configuration for Facebook real-time updates
    /// </summary>
    public class FacebookWebhookConfig
    {
        /// <summary>
        /// Whether webhooks are enabled
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// The webhook verify token
        /// </summary>
        public string VerifyToken { get; set; }

        /// <summary>
        /// The webhook callback URL
        /// </summary>
        public string CallbackUrl { get; set; }

        /// <summary>
        /// The webhook secret
        /// </summary>
        public string Secret { get; set; }

        /// <summary>
        /// The subscription fields
        /// </summary>
        public List<string> SubscriptionFields { get; set; } = new();
    }

    /// <summary>
    /// Fields configuration for Facebook API requests
    /// </summary>
    public class FacebookFieldsConfig
    {
        /// <summary>
        /// Fields to retrieve for posts
        /// </summary>
        public string PostFields { get; set; } = "id,message,created_time,from,type,status_type,link,name,caption,description,picture,full_picture,source,icon,privacy,updated_time,is_published,is_hidden,is_expired,permalink_url,story,story_tags,with_tags,message_tags,attachments,properties,insights,comments,reactions,shares";

        /// <summary>
        /// Fields to retrieve for users
        /// </summary>
        public string UserFields { get; set; } = "id,name,first_name,last_name,middle_name,email,picture,cover,gender,birthday,location,hometown,bio,website,relationship_status,political,religion,quotes,about,username,link,timezone,locale,verified,updated_time,age_range,currency,payment_pricepoints,favorite_athletes,favorite_teams,inspirational_people,languages,sports,work,education,friends.limit(0),followers_count";

        /// <summary>
        /// Fields to retrieve for pages
        /// </summary>
        public string PageFields { get; set; } = "id,name,username,about,bio,category,category_list,description,website,link,phone,email,location,hours,parking,price_range,payment_options,restaurant_services,restaurant_specialties,cover,picture,is_verified,is_permanently_closed,is_unpublished,can_checkin,can_post,is_always_open,is_chain,is_community_page,is_eligible_for_branded_content,is_messenger_bot_get_started_enabled,is_messenger_platform_bot,is_owned,is_webhooks_subscribed,likes,followers_count,talking_about_count,were_here_count,checkins,fan_count,overall_star_rating,rating_count,new_like_count,unread_message_count,unread_notification_count,access_token,ad_campaign,affiliation,app_id,artists_we_like,attire,awards,band_interests,band_members,best_page,birthday,booking_agent,built,business,call_to_actions,company_overview,contact_address,copyright_whitelisted_ig_partners,country_page_likes,culinary_team,current_location,delivery_and_pickup_options_info,description_html,differently_able_friendly,directed_by,display_subtext,displayed_message_response_time,emails,engagement,features,food_styles,founded,general_info,general_manager,genre,global_brand_page_name,global_brand_root_id,has_added_app,has_whatsapp_business_number,has_whatsapp_number,hometown,impressum,influences,instagram_business_account,is_messenger_bot_get_started_enabled,is_messenger_platform_bot,is_owned,is_webhooks_subscribed,leadgen_has_crm_integration,leadgen_has_facebook_pixel_integration,leadgen_tos_accepting_user,leadgen_tos_accepted_time,legal_id,member_count,members,merchant_id,merchant_review_status,messenger_ads_default_icebreakers,messenger_ads_default_page_welcome_message,messenger_ads_default_quick_replies,messenger_ads_quick_replies,messenger_ads_quick_replies_type,mission,mpg,name_with_location_descriptor,network,new_like_count,offer_eligible,overall_star_rating,owner_business,parent_page,personal_info,personal_interests,pharma_safety_info,pickup_options,place_type,plot_outline,press_contact,produced_by,products,promotion_eligible,promotion_ineligible_reason,public_transit,rating_count,recipient,record_label,release_date,restaurant_services,restaurant_specialties,schedule,screenplay_by,season,starring,start_info,store_code,store_location_descriptor,store_number,studio,supports_donate_button_in_live_video,supports_instant_articles,talking_about_count,temporary_status,unread_message_count,unread_notification_count,username,verification_status,voip_info,website,were_here_count,whatsapp_number,written_by";

        /// <summary>
        /// Fields to retrieve for comments
        /// </summary>
        public string CommentFields { get; set; } = "id,message,created_time,from,like_count,can_like,can_comment,can_hide,can_remove,can_reply,is_hidden,is_private,permalink_url,attachment,parent{id},object{id},user_likes,message_tags,comments";

        /// <summary>
        /// Fields to retrieve for events
        /// </summary>
        public string EventFields { get; set; } = "id,name,description,start_time,end_time,timezone,location,venue,cover,ticket_uri,type,category,owner,parent_group,privacy,updated_time,attending_count,declined_count,interested_count,maybe_count,noreply_count,can_guests_invite,guest_list_enabled,is_canceled,is_draft,is_page_owned,online_event_format,online_event_third_party_url";

        /// <summary>
        /// Fields to retrieve for groups
        /// </summary>
        public string GroupFields { get; set; } = "id,name,description,owner,privacy,icon,cover,email,website,member_count,member_request_count,updated_time,archived";

        /// <summary>
        /// Fields to retrieve for photos
        /// </summary>
        public string PhotoFields { get; set; } = "id,name,source,height,width,images,link,picture,created_time,updated_time,from,album,backdated_time,backdated_time_granularity,can_delete,can_tag,event,icon,page_story_id,place,target,webp_images";

        /// <summary>
        /// Fields to retrieve for albums
        /// </summary>
        public string AlbumFields { get; set; } = "id,name,description,location,link,cover_photo,privacy,count,type,created_time,updated_time,from,place,can_upload";

        /// <summary>
        /// Fields to retrieve for insights
        /// </summary>
        public string InsightFields { get; set; } = "name,period,values,title,description,id";
    }

    /// <summary>
    /// Authentication helper for Facebook API
    /// </summary>
    public static class FacebookAuthHelper
    {
        /// <summary>
        /// Gets the login URL for Facebook OAuth
        /// </summary>
        public static string GetLoginUrl(string appId, string redirectUri, List<string> permissions)
        {
            var baseUrl = "https://www.facebook.com/v18.0/dialog/oauth";
            var scope = string.Join(",", permissions);
            return $"{baseUrl}?client_id={appId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={scope}&response_type=code";
        }

        /// <summary>
        /// Gets the access token URL for Facebook OAuth
        /// </summary>
        public static string GetAccessTokenUrl(string appId, string appSecret, string code, string redirectUri)
        {
            return $"https://graph.facebook.com/v18.0/oauth/access_token?client_id={appId}&client_secret={appSecret}&code={code}&redirect_uri={Uri.EscapeDataString(redirectUri)}";
        }

        /// <summary>
        /// Gets the long-lived access token URL
        /// </summary>
        public static string GetLongLivedTokenUrl(string appId, string appSecret, string shortLivedToken)
        {
            return $"https://graph.facebook.com/v18.0/oauth/access_token?grant_type=fb_exchange_token&client_id={appId}&client_secret={appSecret}&fb_exchange_token={shortLivedToken}";
        }

        /// <summary>
        /// Gets the app access token URL
        /// </summary>
        public static string GetAppAccessTokenUrl(string appId, string appSecret)
        {
            return $"https://graph.facebook.com/v18.0/oauth/access_token?client_id={appId}&client_secret={appSecret}&grant_type=client_credentials";
        }

        /// <summary>
        /// Gets the debug token URL for token inspection
        /// </summary>
        public static string GetDebugTokenUrl(string accessToken)
        {
            return $"https://graph.facebook.com/v18.0/debug_token?input_token={accessToken}&access_token={accessToken}";
        }
    }
}
