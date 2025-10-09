# Plan for Adding Specific Functions to Datasource Connectors

## Overview

This plan outlines the addition of strongly typed, POCO-based functions for each datasource connector in the BeepDataSources project. Each connector will have specific methods tailored to its API, using strongly typed return types instead of generic objects.

## General Guidelines

- Use strong typing with POCO classes
- Never return `object` or `IEnumerable<object>`
- If a POCO class doesn't exist in Models.cs, create it in the datasource project
- Follow the pattern established in TwitterDataSource.cs
- Use CommandAttribute with correct ObjectType pointing to POCO class names
- Use EnumPointType.Function for method attributes
- Add misc property with "ReturnType: IEnumerable<POCOClass>" for each method

## Connectors to Update

### 1. Twitter
- **Status**: Already updated with strong typing
- **Current Functions**: GetTweets, GetUserTimeline, GetUserByUsername, GetUserById, GetFollowers, GetFollowing, GetListTweets, SearchSpaces
- **POCOs**: TwitterTweet, TwitterUser, TwitterSpace, TwitterList
- **Action**: None required

### 2. Facebook
- **API**: Facebook Graph API
- **Main Entities**: Posts, Pages, Groups, Users, Events, Photos
- **Functions to Add**:
  - GetPosts(string pageId, int limit = 10)
  - GetPageInfo(string pageId)
  - GetGroupPosts(string groupId, int limit = 10)
  - GetUserProfile(string userId)
  - GetEvents(string pageId, int limit = 10)
  - GetPhotos(string albumId, int limit = 10)
- **POCOs to Create**: FacebookPost, FacebookPage, FacebookGroup, FacebookUser, FacebookEvent, FacebookPhoto
- **Action**: Create POCOs in Models.cs, add methods with CommandAttribute

### 3. Instagram
- **API**: Instagram Basic Display API / Graph API
- **Main Entities**: Posts, Stories, Reels, Users, Media
- **Functions to Add**:
  - GetUserMedia(string userId, int limit = 10)
  - GetUserProfile(string userId)
  - GetMediaComments(string mediaId)
  - GetMediaInsights(string mediaId)
- **POCOs to Create**: InstagramMedia, InstagramUser, InstagramComment, InstagramInsight
- **Action**: Create POCOs, add methods

### 4. LinkedIn
- **API**: LinkedIn API
- **Main Entities**: Posts, Companies, People, Shares
- **Functions to Add**:
  - GetCompanyPosts(string companyId, int limit = 10)
  - GetPersonProfile(string personId)
  - GetShares(string personId, int limit = 10)
  - GetCompanyInfo(string companyId)
- **POCOs to Create**: LinkedInPost, LinkedInCompany, LinkedInPerson, LinkedInShare
- **Action**: Create POCOs, add methods

### 5. YouTube
- **API**: YouTube Data API
- **Main Entities**: Videos, Channels, Playlists, Comments
- **Functions to Add**:
  - GetChannelVideos(string channelId, int maxResults = 10)
  - GetVideoDetails(string videoId)
  - GetPlaylistItems(string playlistId, int maxResults = 10)
  - GetChannelInfo(string channelId)
  - GetVideoComments(string videoId, int maxResults = 10)
- **POCOs to Create**: YouTubeVideo, YouTubeChannel, YouTubePlaylist, YouTubeComment
- **Action**: Create POCOs, add methods

### 6. TikTok
- **API**: TikTok for Developers API
- **Main Entities**: Videos, Users, Comments
- **Functions to Add**:
  - GetUserVideos(string userId, int limit = 10)
  - GetVideoDetails(string videoId)
  - GetUserInfo(string userId)
  - GetVideoComments(string videoId, int limit = 10)
- **POCOs to Create**: TikTokVideo, TikTokUser, TikTokComment
- **Action**: Create POCOs, add methods

### 7. Pinterest
- **API**: Pinterest API
- **Main Entities**: Pins, Boards, Users
- **Functions to Add**:
  - GetBoardPins(string boardId, int limit = 10)
  - GetUserBoards(string userId)
  - GetPinDetails(string pinId)
  - GetUserInfo(string userId)
- **POCOs to Create**: PinterestPin, PinterestBoard, PinterestUser
- **Action**: Create POCOs, add methods

### 8. Reddit
- **API**: Reddit API
- **Main Entities**: Posts, Comments, Subreddits, Users
- **Functions to Add**:
  - GetSubredditPosts(string subreddit, string sort = "hot", int limit = 10)
  - GetPostComments(string postId, int limit = 10)
  - GetUserPosts(string username, int limit = 10)
  - GetSubredditInfo(string subreddit)
- **POCOs to Create**: RedditPost, RedditComment, RedditSubreddit, RedditUser
- **Action**: Create POCOs, add methods

### 9. Snapchat
- **API**: Snapchat Marketing API
- **Main Entities**: Ads, Campaigns, Organizations
- **Functions to Add**:
  - GetCampaigns(string organizationId)
  - GetAds(string campaignId)
  - GetAdDetails(string adId)
  - GetOrganizationInfo(string organizationId)
- **POCOs to Create**: SnapchatCampaign, SnapchatAd, SnapchatOrganization
- **Action**: Create POCOs, add methods

### 10. Buffer
- **API**: Buffer API
- **Main Entities**: Posts, Profiles, Updates
- **Functions to Add**:
  - GetProfilePosts(string profileId, int page = 1, int count = 10)
  - GetProfiles()
  - GetUpdateDetails(string updateId)
  - CreatePost(string profileId, string text)
- **POCOs to Create**: BufferPost, BufferProfile, BufferUpdate
- **Action**: Create POCOs, add methods

### 11. Hootsuite
- **API**: Hootsuite API
- **Main Entities**: Posts, SocialProfiles, Messages
- **Functions to Add**:
  - GetSocialProfiles()
  - GetMessages(string socialProfileId, int limit = 10)
  - GetMessageDetails(string messageId)
  - ScheduleMessage(string socialProfileId, string text, DateTime scheduledTime)
- **POCOs to Create**: HootsuiteSocialProfile, HootsuiteMessage
- **Action**: Create POCOs, add methods

### 12. TikTokAds
- **API**: TikTok Ads API
- **Main Entities**: Campaigns, AdGroups, Ads, Reports
- **Functions to Add**:
  - GetCampaigns(string advertiserId)
  - GetAdGroups(string campaignId)
  - GetAds(string adGroupId)
  - GetAdReports(string adId, DateTime startDate, DateTime endDate)
- **POCOs to Create**: TikTokAdCampaign, TikTokAdGroup, TikTokAd, TikTokAdReport
- **Action**: Create POCOs, add methods

### 13. Outlook (MailServices)
- **API**: Microsoft Graph API
- **Main Entities**: Messages, Contacts, Events, MailFolders, Calendars
- **Functions to Add**:
  - GetMessages(string folderId = "inbox", int top = 10)
  - GetContacts(int top = 10)
  - GetEvents(string calendarId, DateTime start, DateTime end)
  - GetMailFolders()
  - GetCalendars()
- **POCOs to Create**: OutlookMessage, OutlookContact, OutlookEvent, OutlookMailFolder, OutlookCalendar
- **Action**: Create POCOs, add methods

### 14. Yahoo (MailServices)
- **API**: Yahoo Mail API
- **Main Entities**: Messages, Contacts, Folders
- **Functions to Add**:
  - GetMessages(string folderId = "Inbox", int count = 10)
  - GetContacts(int count = 10)
  - GetFolders()
  - GetMessageDetails(string messageId)
- **POCOs to Create**: YahooMessage, YahooContact, YahooFolder
- **Action**: Create POCOs, add methods

### 15. Freshdesk (CustomerSupport)
- **API**: Freshdesk API
- **Main Entities**: Tickets, Contacts, Agents, Companies
- **Functions to Add**:
  - GetTickets(int page = 1, int perPage = 10)
  - GetTicketDetails(int ticketId)
  - GetContacts(int page = 1, int perPage = 10)
  - GetAgents()
  - GetCompanies(int page = 1, int perPage = 10)
- **POCOs to Create**: FreshdeskTicket, FreshdeskContact, FreshdeskAgent, FreshdeskCompany
- **Action**: Create POCOs, add methods

### 16. LiveAgent (CustomerSupport)
- **API**: LiveAgent API
- **Main Entities**: Tickets, Customers, Agents
- **Functions to Add**:
  - GetTickets(string status = "open", int limit = 10)
  - GetTicketDetails(string ticketId)
  - GetCustomers(int limit = 10)
  - GetAgents()
- **POCOs to Create**: LiveAgentTicket, LiveAgentCustomer, LiveAgentAgent
- **Action**: Create POCOs, add methods

### 17. ZohoDesk (CustomerSupport)
- **API**: Zoho Desk API
- **Main Entities**: Tickets, Contacts, Agents, Organizations
- **Functions to Add**:
  - GetTickets(string status = "open", int limit = 10)
  - GetTicketDetails(string ticketId)
  - GetContacts(int limit = 10)
  - GetAgents()
  - GetOrganizations(int limit = 10)
- **POCOs to Create**: ZohoDeskTicket, ZohoDeskContact, ZohoDeskAgent, ZohoDeskOrganization
- **Action**: Create POCOs, add methods

## Implementation Order

1. Start with Facebook (most popular social media)
2. Then Instagram, LinkedIn, YouTube (major platforms)
3. Continue with remaining social media: TikTok, Pinterest, Reddit, Snapchat
4. Then Buffer, Hootsuite, TikTokAds
5. Mail services: Outlook, Yahoo
6. Customer support: Freshdesk, LiveAgent, ZohoDesk

## Steps for Each Connector

1. Review existing Models.cs for existing POCOs
2. Research API documentation for main endpoints
3. Create missing POCO classes with JsonPropertyName attributes
4. Add specific methods with CommandAttribute
5. Update ObjectType to match POCO class names
6. Set PointType to EnumPointType.Function
7. Add misc with ReturnType information
8. Test compilation and functionality

## Validation

- Ensure no IEnumerable<object> returns
- All methods have proper CommandAttribute
- POCOs are sealed classes with JsonPropertyName
- Methods follow async Task<IEnumerable<POCO>> pattern
- Compilation succeeds without errors