# Social Media Connectors

## Overview

The Social Media connectors category provides integration with major social media platforms, enabling data retrieval, content management, analytics, and engagement tracking. All connectors inherit from `WebAPIDataSource` and leverage the `CommandAttribute` pattern to expose platform-specific functionality to the Beep framework.

## Architecture

- **Base Class**: All connectors inherit from `WebAPIDataSource`
- **Authentication**: Primarily OAuth 2.0 (some platforms support API keys or Bearer tokens)
- **Models**: Strongly-typed POCO classes with `[JsonPropertyName]` attributes
- **CommandAttribute**: Public methods decorated with `CommandAttribute` for framework discovery
- **Entity Registry**: Some connectors use entity registries to map entity names to CLR types

## Connectors

### Twitter (`TwitterDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Version**: Twitter API v2  
**Authentication**: OAuth 2.0 / Bearer Token

#### Models
- `TwitterTweet` - Tweet data with metrics, entities, attachments
- `TwitterUser` - User profiles with public metrics
- `TwitterMedia` - Media attachments (photos, videos, GIFs)
- `TwitterPlace` - Geographic location data
- `TwitterList` - Twitter lists
- `TwitterSpace` - Twitter Spaces

#### CommandAttribute Methods

**Read Operations:**
- `GetTweets(string query, int maxResults, string? nextToken)` - Search recent tweets
- `GetUserTimeline(string userId, int maxResults, string? nextToken)` - Get user's tweets
- `GetUserByUsername(string username)` - Get user by username
- `GetUserById(string userId)` - Get user by ID
- `GetFollowers(string userId, int maxResults, string? nextToken)` - Get user followers
- `GetFollowing(string userId, int maxResults, string? nextToken)` - Get users being followed
- `GetListTweets(string listId, int maxResults, string? nextToken)` - Get tweets from a list
- `SearchSpaces(string query, string state, int maxResults)` - Search Twitter Spaces

**Write Operations:**
- `CreateTweet(string text, string replyToTweetId, bool isReply)` - Create a new tweet
- `DeleteTweet(string tweetId)` - Delete a tweet
- `LikeTweet(string tweetId, string userId)` - Like a tweet
- `Retweet(string tweetId, string userId)` - Retweet a tweet
- `UpdateTweet(string tweetId, TwitterTweet tweet)` - Update a tweet

#### Configuration
```csharp
var props = new WebAPIConnectionProperties
{
    Url = "https://api.twitter.com/2",
    AuthType = AuthTypeEnum.Bearer,
    BearerToken = "your_bearer_token",
    // Or OAuth2
    ClientId = "your_client_id",
    ClientSecret = "your_client_secret",
    TokenUrl = "https://api.twitter.com/oauth2/token"
};
```

#### Example Usage
```csharp
var twitter = new TwitterDataSource("Twitter", logger, editor, DataSourceType.Twitter, errors);
twitter.Dataconnection.ConnectionProp = props;

// Search tweets
var tweets = await twitter.GetTweets("BeepDM", maxResults: 10);

// Get user timeline
var timeline = await twitter.GetUserTimeline("user123", maxResults: 20);

// Create a tweet
var newTweet = await twitter.CreateTweet("Hello from BeepDM!");
```

---

### Facebook (`FacebookDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Version**: Graph API v18+  
**Authentication**: OAuth 2.0

#### Models
- `FacebookPost` - Posts with engagement metrics
- `FacebookPage` - Page information
- `FacebookUser` - User profiles
- `FacebookEvent` - Events
- `FacebookPicture` - Photos

#### CommandAttribute Methods

**Read Operations:**
- `GetPosts(string pageId, int limit)` - Get posts from a page
- `GetPageInfo(string pageId)` - Get page information
- `GetGroupPosts(string groupId, int limit)` - Get posts from a group
- `GetUserProfile(string userId)` - Get user profile
- `GetEvents(string pageId, int limit)` - Get events from a page
- `GetPhotos(string albumId, int limit)` - Get photos from an album

**Write Operations:**
- `CreatePostAsync(FacebookPost post)` - Create a post
- `CreateEventAsync(FacebookEvent fbEvent)` - Create an event
- `UpdatePostAsync(FacebookPost post)` - Update a post
- `UpdateEventAsync(FacebookEvent fbEvent)` - Update an event

#### Configuration
```csharp
var props = new WebAPIConnectionProperties
{
    Url = "https://graph.facebook.com/v18.0",
    AuthType = AuthTypeEnum.OAuth2,
    ClientId = "your_app_id",
    ClientSecret = "your_app_secret",
    TokenUrl = "https://graph.facebook.com/v18.0/oauth/access_token"
};
```

---

### Instagram (`InstagramDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Version**: Instagram Graph API v18.0  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- User profile management
- Media posts retrieval
- Stories support
- Hashtag search
- Analytics and insights

---

### LinkedIn (`LinkedInDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Version**: LinkedIn Marketing API v2  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- Profile management
- Post creation and management
- Organization management
- Follower analytics
- Campaign management

---

### Pinterest (`PinterestDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Version**: Pinterest API v5  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- User profile management
- Board and pin management
- Analytics support
- Following/followers tracking

---

### YouTube (`YouTubeDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Version**: YouTube Data API v3  
**Authentication**: API Key + OAuth 2.0

#### CommandAttribute Methods
- Channel management
- Video operations
- Playlist management
- Comment threads
- Search functionality

---

### TikTok (`TikTokDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Version**: TikTok for Developers API v2  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- User info retrieval
- Video management
- Comments
- Analytics
- Follower tracking

---

### Snapchat (`SnapchatDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Version**: Snapchat Marketing API  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods

**Read Operations:**
- `GetOrganizations()` - Get organizations
- `GetAdAccounts(string organizationId)` - Get ad accounts
- `GetCampaigns(string adAccountId)` - Get campaigns
- `GetAdSquads(string campaignId)` - Get ad squads
- `GetAds(string adSquadId)` - Get ads
- `GetCreatives(string adAccountId)` - Get creatives

**Write Operations:**
- `CreateCampaignAsync(SnapchatCampaign campaign)` - Create campaign
- `UpdateCampaignAsync(SnapchatCampaign campaign)` - Update campaign

---

### Reddit (`RedditDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Version**: Reddit API  
**Authentication**: Script/Client Credentials

#### CommandAttribute Methods

**Read Operations:**
- `GetPosts(string subreddit, string sort, int limit)` - Get posts
- `GetSubredditInfo(string subreddit)` - Get subreddit info
- `GetUserInfo(string username)` - Get user info
- `GetComments(string postId, string sort, int limit)` - Get comments
- `GetSearchResults(string query, string subreddit, int limit)` - Search
- `GetHotPosts(string subreddit, int limit)` - Get hot posts

**Write Operations:**
- `CreatePostAsync(RedditPost post)` - Create post
- `UpdatePostAsync(RedditPost post)` - Update post

---

### Buffer (`BufferDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Version**: Buffer API v1  
**Authentication**: API Key + OAuth

#### CommandAttribute Methods

**Read Operations:**
- `GetPosts()` - Get all posts
- `GetPendingPosts()` - Get pending posts
- `GetSentPosts()` - Get sent posts
- `GetProfiles()` - Get profiles
- `GetAnalytics(string profileId)` - Get analytics
- `GetCampaigns()` - Get campaigns

**Write Operations:**
- `CreatePostAsync(BufferPost post)` - Create post

---

### Hootsuite (`HootsuiteDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Version**: Hootsuite API v1  
**Authentication**: API Key + OAuth

#### CommandAttribute Methods
- Post management
- Social profile management
- Analytics
- Organization/team management

---

### TikTokAds (`TikTokAdsDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Version**: TikTok Ads API  
**Authentication**: Access Token

#### CommandAttribute Methods
- Campaign management
- Ad group operations
- Ad management
- Analytics
- Advertiser management

---

### Loomly (`LoomlyDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Version**: Loomly API  
**Authentication**: API Key

#### CommandAttribute Methods
- Post management
- Calendar operations
- Content scheduling

---

## Common Patterns

### CommandAttribute Structure

All connectors use the `CommandAttribute` to expose methods to the framework:

```csharp
[CommandAttribute(
    Name = "MethodName",
    Caption = "User-Friendly Description",
    Category = DatasourceCategory.Connector,
    DatasourceType = DataSourceType.Platform,
    PointType = EnumPointType.Function,
    ObjectType = "ModelClassName",
    ClassType = "DataSourceClassName",
    Showin = ShowinType.Both,
    Order = 1,
    iconimage = "platform.png",
    misc = "ReturnType: IEnumerable<ModelClass>"
)]
public async Task<IEnumerable<ModelClass>> MethodName(...)
{
    // Implementation
}
```

### Entity Registry Pattern

Some connectors (like Twitter) use entity registries to map entity names to types:

```csharp
public static class TwitterEntityRegistry
{
    public static readonly Dictionary<string, Type> Types = new()
    {
        ["tweets.search"] = typeof(TwitterTweet),
        ["users.by_username"] = typeof(TwitterUser),
        // ...
    };
}
```

### Configuration Pattern

All connectors follow this configuration pattern:

```csharp
public ConnectorDataSource(string datasourcename, IDMLogger logger, 
    IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject)
    : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
{
    // Ensure WebAPI connection props exist
    if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
    {
        if (Dataconnection != null)
            Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
    }
    
    // Register entities
    EntitiesNames = KnownEntities.ToList();
    Entities = EntitiesNames
        .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
        .ToList();
}
```

## Best Practices

1. **Strong Typing**: Always use strongly-typed POCO models, avoid `object` types
2. **CommandAttribute**: Decorate all public methods with `CommandAttribute` for framework discovery
3. **Error Handling**: Use base class error handling and logging
4. **Pagination**: Support pagination via cursor tokens or page numbers
5. **Rate Limiting**: Respect platform rate limits using base class helpers
6. **Authentication**: Configure OAuth 2.0 or appropriate auth method via `WebAPIConnectionProperties`

## Reference Implementation

See `TwitterDataSource` for a comprehensive example of:
- Entity registry pattern
- CommandAttribute usage
- Pagination handling
- CRUD operations
- Model definitions

## Status

All Social Media connectors are **✅ Completed** and ready for use. See `progress.md` for detailed implementation status.

