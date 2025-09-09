# Twitter Connector Refactoring Plan

## Overview
Refactor TwitterDataSource to inherit from WebAPIDataSource and implement proper IDataSource interface with POCO classes.

## Current Implementation Analysis
- **API**: Twitter API v2
- **Authentication**: Bearer Token / OAuth 2.0
- **Entities**: Tweets, Users, Spaces, Lists, Direct Messages
- **Current Status**: Standalone IDataSource implementation

## Target Architecture

### 1. POCO Classes (Entities/Twitter/)
```csharp
namespace BeepDataSources.Connectors.SocialMedia.Twitter.Entities
{
    public class Tweet
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; }
        public TwitterUser Author { get; set; }
        public TweetMetrics Metrics { get; set; }
        public List<Tweet> ReferencedTweets { get; set; }
        public List<Media> Media { get; set; }
        // ... other properties
    }

    public class TwitterUser
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserMetrics Metrics { get; set; }
        // ... other properties
    }

    public class TweetMetrics
    {
        public int RetweetCount { get; set; }
        public int LikeCount { get; set; }
        public int ReplyCount { get; set; }
        public int QuoteCount { get; set; }
        // ... other metrics
    }
}
```

### 2. Configuration Class (Config/TwitterConfig.cs)
```csharp
namespace BeepDataSources.Connectors.SocialMedia.Twitter.Config
{
    public class TwitterConfig
    {
        public string BearerToken { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public string AccessToken { get; set; }
        public string AccessTokenSecret { get; set; }
        public string ApiVersion { get; set; } = "2";
        public string BaseUrl => $"https://api.twitter.com/{ApiVersion}";
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetries { get; set; } = 3;
        public int RateLimitDelayMs { get; set; } = 1000;
    }
}
```

## Implementation Steps

### Phase 1: Project Structure Setup
1. ✅ Create `Entities/` folder
2. ✅ Create `Config/` folder
3. ✅ Update project file with dependencies

### Phase 2: POCO Classes Creation
1. ✅ Create Tweet.cs
2. ✅ Create TwitterUser.cs
3. ✅ Create TweetMetrics.cs
4. ✅ Create Space.cs
5. ✅ Create TwitterList.cs

### Phase 3: Configuration Class
1. ✅ Create TwitterConfig.cs
2. ✅ Implement OAuth 2.0 flow
3. ✅ Add token refresh logic

### Phase 4: DataSource Refactoring
1. ✅ Change inheritance to WebAPIDataSource
2. ✅ Override necessary methods
3. ✅ Implement Twitter API v2 calls
4. ✅ Update entity initialization

### Phase 5: Testing & Validation
1. ✅ Unit tests for POCO classes
2. ✅ Integration tests for API calls
3. ✅ OAuth flow testing
4. ✅ Rate limit handling

## API Endpoints Mapping

| Entity | Endpoint | Method | Description |
|--------|----------|--------|-------------|
| Tweets | /2/tweets | GET | Get tweets |
| Users | /2/users | GET | Get users |
| Spaces | /2/spaces | GET | Get spaces |
| Lists | /2/lists | GET | Get lists |
| DMs | /2/dm_conversations | GET | Get direct messages |

## Success Criteria

1. ✅ Inherits from WebAPIDataSource
2. ✅ Implements IDataSource interface properly
3. ✅ POCO classes created and functional
4. ✅ Twitter API v2 integration working
5. ✅ OAuth 2.0 authentication implemented
6. ✅ Unit tests pass
7. ✅ Documentation updated

---

**Last Updated**: September 8, 2025
**Version**: 1.0.0</content>
<parameter name="filePath">c:\Users\f_ald\source\repos\The-Tech-Idea\BeepDataSources\Connectors\SocialMedia\Twitter\TwitterRefactoringPlan.md
