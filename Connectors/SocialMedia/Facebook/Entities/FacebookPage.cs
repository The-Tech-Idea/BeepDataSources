using System;
using System.Collections.Generic;

namespace TheTechIdea.Beep.FacebookDataSource.Entities
{
    /// <summary>
    /// Represents a Facebook page
    /// </summary>
    public class FacebookPage
    {
        /// <summary>
        /// The page ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The page name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The page username
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The page about information
        /// </summary>
        public string About { get; set; }

        /// <summary>
        /// The page bio
        /// </summary>
        public string Bio { get; set; }

        /// <summary>
        /// The page category
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// The page category list
        /// </summary>
        public List<FacebookCategory> CategoryList { get; set; } = new();

        /// <summary>
        /// The page description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The page website
        /// </summary>
        public string Website { get; set; }

        /// <summary>
        /// The page link
        /// </summary>
        public string Link { get; set; }

        /// <summary>
        /// The page phone number
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// The page email
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// The page location
        /// </summary>
        public FacebookLocation Location { get; set; }

        /// <summary>
        /// The page hours
        /// </summary>
        public FacebookHours Hours { get; set; }

        /// <summary>
        /// The page parking information
        /// </summary>
        public FacebookParking Parking { get; set; }

        /// <summary>
        /// The page price range
        /// </summary>
        public string PriceRange { get; set; }

        /// <summary>
        /// The page payment options
        /// </summary>
        public FacebookPaymentOptions PaymentOptions { get; set; }

        /// <summary>
        /// The page restaurant services
        /// </summary>
        public FacebookRestaurantServices RestaurantServices { get; set; }

        /// <summary>
        /// The page restaurant specialties
        /// </summary>
        public FacebookRestaurantSpecialties RestaurantSpecialties { get; set; }

        /// <summary>
        /// The page cover photo
        /// </summary>
        public FacebookCover Cover { get; set; }

        /// <summary>
        /// The page profile picture
        /// </summary>
        public string Picture { get; set; }

        /// <summary>
        /// The page verification status
        /// </summary>
        public bool? IsVerified { get; set; }

        /// <summary>
        /// The page permanently closed status
        /// </summary>
        public bool? IsPermanentlyClosed { get; set; }

        /// <summary>
        /// The page unpublished status
        /// </summary>
        public bool? IsUnpublished { get; set; }

        /// <summary>
        /// The page can checkin status
        /// </summary>
        public bool? CanCheckin { get; set; }

        /// <summary>
        /// The page can post status
        /// </summary>
        public bool? CanPost { get; set; }

        /// <summary>
        /// The page likes count
        /// </summary>
        public long? Likes { get; set; }

        /// <summary>
        /// The page followers count
        /// </summary>
        public long? FollowersCount { get; set; }

        /// <summary>
        /// The page talking about count
        /// </summary>
        public long? TalkingAboutCount { get; set; }

        /// <summary>
        /// The page were here count
        /// </summary>
        public long? WereHereCount { get; set; }

        /// <summary>
        /// The page checkins count
        /// </summary>
        public long? Checkins { get; set; }

        /// <summary>
        /// The page fan count
        /// </summary>
        public long? FanCount { get; set; }

        /// <summary>
        /// The page overall star rating
        /// </summary>
        public double? OverallStarRating { get; set; }

        /// <summary>
        /// The page rating count
        /// </summary>
        public long? RatingCount { get; set; }

        /// <summary>
        /// The page new like count
        /// </summary>
        public long? NewLikeCount { get; set; }

        /// <summary>
        /// The page unread message count
        /// </summary>
        public long? UnreadMessageCount { get; set; }

        /// <summary>
        /// The page unread notification count
        /// </summary>
        public long? UnreadNotificationCount { get; set; }

        /// <summary>
        /// The page messaging settings
        /// </summary>
        public FacebookMessagingSettings MessagingSettings { get; set; }

        /// <summary>
        /// The page messenger ads quick replies type
        /// </summary>
        public string MessengerAdsQuickRepliesType { get; set; }

        /// <summary>
        /// The page access token
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// The page ad campaign
        /// </summary>
        public string AdCampaign { get; set; }

        /// <summary>
        /// The page affiliation
        /// </summary>
        public string Affiliation { get; set; }

        /// <summary>
        /// The page app ID
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// The page artists we like
        /// </summary>
        public string ArtistsWeLike { get; set; }

        /// <summary>
        /// The page attire
        /// </summary>
        public string Attire { get; set; }

        /// <summary>
        /// The page awards
        /// </summary>
        public string Awards { get; set; }

        /// <summary>
        /// The page band interests
        /// </summary>
        public string BandInterests { get; set; }

        /// <summary>
        /// The page band members
        /// </summary>
        public string BandMembers { get; set; }

        /// <summary>
        /// The page best page
        /// </summary>
        public FacebookPage BestPage { get; set; }

        /// <summary>
        /// The page birthday
        /// </summary>
        public string Birthday { get; set; }

        /// <summary>
        /// The page booking agent
        /// </summary>
        public string BookingAgent { get; set; }

        /// <summary>
        /// The page built
        /// </summary>
        public string Built { get; set; }

        /// <summary>
        /// The page business
        /// </summary>
        public FacebookBusiness Business { get; set; }

        /// <summary>
        /// The page call to actions
        /// </summary>
        public List<FacebookCallToAction> CallToActions { get; set; } = new();

        /// <summary>
        /// The page company overview
        /// </summary>
        public string CompanyOverview { get; set; }

        /// <summary>
        /// The page contact address
        /// </summary>
        public FacebookContactAddress ContactAddress { get; set; }

        /// <summary>
        /// The page copyright whitelisted ig partners
        /// </summary>
        public List<string> CopyrightWhitelistedIgPartners { get; set; } = new();

        /// <summary>
        /// The page country page likes
        /// </summary>
        public long? CountryPageLikes { get; set; }

        /// <summary>
        /// The page culinary team
        /// </summary>
        public string CulinaryTeam { get; set; }

        /// <summary>
        /// The page current location
        /// </summary>
        public FacebookLocation CurrentLocation { get; set; }

        /// <summary>
        /// The page delivery and pickup options info
        /// </summary>
        public FacebookDeliveryAndPickupOptionsInfo DeliveryAndPickupOptionsInfo { get; set; }

        /// <summary>
        /// The page description html
        /// </summary>
        public string DescriptionHtml { get; set; }

        /// <summary>
        /// The page differently able friendly
        /// </summary>
        public bool? DifferentlyAbleFriendly { get; set; }

        /// <summary>
        /// The page directed by
        /// </summary>
        public string DirectedBy { get; set; }

        /// <summary>
        /// The page display subtext
        /// </summary>
        public string DisplaySubtext { get; set; }

        /// <summary>
        /// The page displayed message response time
        /// </summary>
        public string DisplayedMessageResponseTime { get; set; }

        /// <summary>
        /// The page emails
        /// </summary>
        public List<string> Emails { get; set; } = new();

        /// <summary>
        /// The page engagement
        /// </summary>
        public FacebookEngagement Engagement { get; set; }

        /// <summary>
        /// The page features
        /// </summary>
        public string Features { get; set; }

        /// <summary>
        /// The page food styles
        /// </summary>
        public List<string> FoodStyles { get; set; } = new();

        /// <summary>
        /// The page founded
        /// </summary>
        public string Founded { get; set; }

        /// <summary>
        /// The page general info
        /// </summary>
        public string GeneralInfo { get; set; }

        /// <summary>
        /// The page general manager
        /// </summary>
        public string GeneralManager { get; set; }

        /// <summary>
        /// The page genre
        /// </summary>
        public string Genre { get; set; }

        /// <summary>
        /// The page global brand page name
        /// </summary>
        public string GlobalBrandPageName { get; set; }

        /// <summary>
        /// The page global brand root id
        /// </summary>
        public string GlobalBrandRootId { get; set; }

        /// <summary>
        /// The page has added app
        /// </summary>
        public bool? HasAddedApp { get; set; }

        /// <summary>
        /// The page has whatsapp business number
        /// </summary>
        public bool? HasWhatsappBusinessNumber { get; set; }

        /// <summary>
        /// The page has whatsapp number
        /// </summary>
        public bool? HasWhatsappNumber { get; set; }

        /// <summary>
        /// The page hometown
        /// </summary>
        public string Hometown { get; set; }

        /// <summary>
        /// The page impressum
        /// </summary>
        public string Impressum { get; set; }

        /// <summary>
        /// The page influences
        /// </summary>
        public string Influences { get; set; }

        /// <summary>
        /// The page instagram business account
        /// </summary>
        public FacebookInstagramBusinessAccount InstagramBusinessAccount { get; set; }

        /// <summary>
        /// The page is always open
        /// </summary>
        public bool? IsAlwaysOpen { get; set; }

        /// <summary>
        /// The page is chain
        /// </summary>
        public bool? IsChain { get; set; }

        /// <summary>
        /// The page is community page
        /// </summary>
        public bool? IsCommunityPage { get; set; }

        /// <summary>
        /// The page is eligible for branded content
        /// </summary>
        public bool? IsEligibleForBrandedContent { get; set; }

        /// <summary>
        /// The page is messenger bot get started enabled
        /// </summary>
        public bool? IsMessengerBotGetStartedEnabled { get; set; }

        /// <summary>
        /// The page is messenger platform bot
        /// </summary>
        public bool? IsMessengerPlatformBot { get; set; }

        /// <summary>
        /// The page is owned
        /// </summary>
        public bool? IsOwned { get; set; }

        /// <summary>
        /// The page is webhooks subscribed
        /// </summary>
        public bool? IsWebhooksSubscribed { get; set; }

        /// <summary>
        /// The page leadgen has crm integration
        /// </summary>
        public bool? LeadgenHasCrmIntegration { get; set; }

        /// <summary>
        /// The page leadgen has facebook pixel integration
        /// </summary>
        public bool? LeadgenHasFacebookPixelIntegration { get; set; }

        /// <summary>
        /// The page leadgen tos accepting user
        /// </summary>
        public string LeadgenTosAcceptingUser { get; set; }

        /// <summary>
        /// The page leadgen tos accepted time
        /// </summary>
        public DateTime? LeadgenTosAcceptedTime { get; set; }

        /// <summary>
        /// The page legal id
        /// </summary>
        public string LegalId { get; set; }

        /// <summary>
        /// The page member count
        /// </summary>
        public long? MemberCount { get; set; }

        /// <summary>
        /// The page members
        /// </summary>
        public string Members { get; set; }

        /// <summary>
        /// The page merchant id
        /// </summary>
        public string MerchantId { get; set; }

        /// <summary>
        /// The page merchant review status
        /// </summary>
        public string MerchantReviewStatus { get; set; }

        /// <summary>
        /// The page messenger ads default icebreakers
        /// </summary>
        public List<FacebookMessengerAdIcebreaker> MessengerAdsDefaultIcebreakers { get; set; } = new();

        /// <summary>
        /// The page messenger ads default page welcome message
        /// </summary>
        public FacebookMessengerAdDefaultPageWelcomeMessage MessengerAdsDefaultPageWelcomeMessage { get; set; }

        /// <summary>
        /// The page messenger ads default quick replies
        /// </summary>
        public List<FacebookMessengerAdQuickReply> MessengerAdsDefaultQuickReplies { get; set; } = new();

        /// <summary>
        /// The page messenger ads quick replies
        /// </summary>
        public List<FacebookMessengerAdQuickReply> MessengerAdsQuickReplies { get; set; } = new();

        /// <summary>
        /// The page mission
        /// </summary>
        public string Mission { get; set; }

        /// <summary>
        /// The page mpg
        /// </summary>
        public string Mpg { get; set; }

        /// <summary>
        /// The page name with location descriptor
        /// </summary>
        public string NameWithLocationDescriptor { get; set; }

        /// <summary>
        /// The page network
        /// </summary>
        public string Network { get; set; }

        /// <summary>
        /// The page new like count
        /// </summary>
        public long? NewLikeCount { get; set; }

        /// <summary>
        /// The page offer eligible
        /// </summary>
        public bool? OfferEligible { get; set; }

        /// <summary>
        /// The page overall star rating
        /// </summary>
        public double? OverallStarRating { get; set; }

        /// <summary>
        /// The page owner business
        /// </summary>
        public FacebookBusiness OwnerBusiness { get; set; }

        /// <summary>
        /// The page parent page
        /// </summary>
        public FacebookPage ParentPage { get; set; }

        /// <summary>
        /// The page personal info
        /// </summary>
        public string PersonalInfo { get; set; }

        /// <summary>
        /// The page personal interests
        /// </summary>
        public string PersonalInterests { get; set; }

        /// <summary>
        /// The page pharma safety info
        /// </summary>
        public string PharmaSafetyInfo { get; set; }

        /// <summary>
        /// The page phone
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// The page pickup options
        /// </summary>
        public List<string> PickupOptions { get; set; } = new();

        /// <summary>
        /// The page place type
        /// </summary>
        public string PlaceType { get; set; }

        /// <summary>
        /// The page plot outline
        /// </summary>
        public string PlotOutline { get; set; }

        /// <summary>
        /// The page press contact
        /// </summary>
        public string PressContact { get; set; }

        /// <summary>
        /// The page produced by
        /// </summary>
        public string ProducedBy { get; set; }

        /// <summary>
        /// The page products
        /// </summary>
        public string Products { get; set; }

        /// <summary>
        /// The page promotion eligible
        /// </summary>
        public bool? PromotionEligible { get; set; }

        /// <summary>
        /// The page promotion ineligible reason
        /// </summary>
        public string PromotionIneligibleReason { get; set; }

        /// <summary>
        /// The page public transit
        /// </summary>
        public string PublicTransit { get; set; }

        /// <summary>
        /// The page rating count
        /// </summary>
        public long? RatingCount { get; set; }

        /// <summary>
        /// The page recipient
        /// </summary>
        public string Recipient { get; set; }

        /// <summary>
        /// The page record label
        /// </summary>
        public string RecordLabel { get; set; }

        /// <summary>
        /// The page release date
        /// </summary>
        public string ReleaseDate { get; set; }

        /// <summary>
        /// The page restaurant services
        /// </summary>
        public FacebookRestaurantServices RestaurantServices { get; set; }

        /// <summary>
        /// The page restaurant specialties
        /// </summary>
        public FacebookRestaurantSpecialties RestaurantSpecialties { get; set; }

        /// <summary>
        /// The page schedule
        /// </summary>
        public string Schedule { get; set; }

        /// <summary>
        /// The page screenplay by
        /// </summary>
        public string ScreenplayBy { get; set; }

        /// <summary>
        /// The page season
        /// </summary>
        public string Season { get; set; }

        /// <summary>
        /// The page single line address
        /// </summary>
        public string SingleLineAddress { get; set; }

        /// <summary>
        /// The page starring
        /// </summary>
        public string Starring { get; set; }

        /// <summary>
        /// The page start info
        /// </summary>
        public FacebookStartInfo StartInfo { get; set; }

        /// <summary>
        /// The page store code
        /// </summary>
        public string StoreCode { get; set; }

        /// <summary>
        /// The page store location descriptor
        /// </summary>
        public string StoreLocationDescriptor { get; set; }

        /// <summary>
        /// The page store number
        /// </summary>
        public long? StoreNumber { get; set; }

        /// <summary>
        /// The page studio
        /// </summary>
        public string Studio { get; set; }

        /// <summary>
        /// The page supports donate button in live video
        /// </summary>
        public bool? SupportsDonateButtonInLiveVideo { get; set; }

        /// <summary>
        /// The page supports instant articles
        /// </summary>
        public bool? SupportsInstantArticles { get; set; }

        /// <summary>
        /// The page talking about count
        /// </summary>
        public long? TalkingAboutCount { get; set; }

        /// <summary>
        /// The page temporary status
        /// </summary>
        public string TemporaryStatus { get; set; }

        /// <summary>
        /// The page unread message count
        /// </summary>
        public long? UnreadMessageCount { get; set; }

        /// <summary>
        /// The page unread notification count
        /// </summary>
        public long? UnreadNotificationCount { get; set; }
    }
}

        /// <summary>
        /// The page username
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The page verification status
        /// </summary>
        public bool? VerificationStatus { get; set; }

        /// <summary>
        /// The page voip info
        /// </summary>
        public FacebookVoipInfo VoipInfo { get; set; }

        /// <summary>
        /// The page website
        /// </summary>
        public string Website { get; set; }

        /// <summary>
        /// The page were here count
        /// </summary>
        public long? WereHereCount { get; set; }

        /// <summary>
        /// The page whatsapp number
        /// </summary>
        public string WhatsappNumber { get; set; }

        /// <summary>
        /// The page written by
        /// </summary>
        public string WrittenBy { get; set; }

        /// <summary>
        /// Custom fields for extensibility
        /// </summary>
        public Dictionary<string, object> CustomFields { get; set; } = new();
    }

    /// <summary>
    /// Represents a Facebook category
    /// </summary>
    public class FacebookCategory
    {
        /// <summary>
        /// The category ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The category name
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// Represents Facebook hours
    /// </summary>
    public class FacebookHours
    {
        /// <summary>
        /// Monday hours
        /// </summary>
        public string Monday { get; set; }

        /// <summary>
        /// Tuesday hours
        /// </summary>
        public string Tuesday { get; set; }

        /// <summary>
        /// Wednesday hours
        /// </summary>
        public string Wednesday { get; set; }

        /// <summary>
        /// Thursday hours
        /// </summary>
        public string Thursday { get; set; }

        /// <summary>
        /// Friday hours
        /// </summary>
        public string Friday { get; set; }

        /// <summary>
        /// Saturday hours
        /// </summary>
        public string Saturday { get; set; }

        /// <summary>
        /// Sunday hours
        /// </summary>
        public string Sunday { get; set; }
    }

    /// <summary>
    /// Represents Facebook parking information
    /// </summary>
    public class FacebookParking
    {
        /// <summary>
        /// Street parking
        /// </summary>
        public bool? Street { get; set; }

        /// <summary>
        /// Lot parking
        /// </summary>
        public bool? Lot { get; set; }

        /// <summary>
        /// Valet parking
        /// </summary>
        public bool? Valet { get; set; }
    }

    /// <summary>
    /// Represents Facebook payment options
    /// </summary>
    public class FacebookPaymentOptions
    {
        /// <summary>
        /// Cash only
        /// </summary>
        public bool? CashOnly { get; set; }

        /// <summary>
        /// Visa
        /// </summary>
        public bool? Visa { get; set; }

        /// <summary>
        /// Amex
        /// </summary>
        public bool? Amex { get; set; }

        /// <summary>
        /// MasterCard
        /// </summary>
        public bool? MasterCard { get; set; }

        /// <summary>
        /// Discover
        /// </summary>
        public bool? Discover { get; set; }
    }

    /// <summary>
    /// Represents Facebook restaurant services
    /// </summary>
    public class FacebookRestaurantServices
    {
        /// <summary>
        /// Catering
        /// </summary>
        public bool? Catering { get; set; }

        /// <summary>
        /// Delivery
        /// </summary>
        public bool? Delivery { get; set; }

        /// <summary>
        /// Groups
        /// </summary>
        public bool? Groups { get; set; }

        /// <summary>
        /// Kids
        /// </summary>
        public bool? Kids { get; set; }

        /// <summary>
        /// Outdoor
        /// </summary>
        public bool? Outdoor { get; set; }

        /// <summary>
        /// Pickup
        /// </summary>
        public bool? Pickup { get; set; }

        /// <summary>
        /// Reserve
        /// </summary>
        public bool? Reserve { get; set; }

        /// <summary>
        /// Takeout
        /// </summary>
        public bool? Takeout { get; set; }

        /// <summary>
        /// Waiter
        /// </summary>
        public bool? Waiter { get; set; }

        /// <summary>
        /// Walkins
        /// </summary>
        public bool? Walkins { get; set; }
    }

    /// <summary>
    /// Represents Facebook restaurant specialties
    /// </summary>
    public class FacebookRestaurantSpecialties
    {
        /// <summary>
        /// Breakfast
        /// </summary>
        public bool? Breakfast { get; set; }

        /// <summary>
        /// Coffee
        /// </summary>
        public bool? Coffee { get; set; }

        /// <summary>
        /// Dinner
        /// </summary>
        public bool? Dinner { get; set; }

        /// <summary>
        /// Drinks
        /// </summary>
        public bool? Drinks { get; set; }

        /// <summary>
        /// Lunch
        /// </summary>
        public bool? Lunch { get; set; }
    }

    /// <summary>
    /// Represents Facebook cover photo
    /// </summary>
    public class FacebookCover
    {
        /// <summary>
        /// The cover ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The cover source URL
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// The cover offset X
        /// </summary>
        public int? OffsetX { get; set; }

        /// <summary>
        /// The cover offset Y
        /// </summary>
        public int? OffsetY { get; set; }
    }

    /// <summary>
    /// Represents Facebook messaging settings
    /// </summary>
    public class FacebookMessagingSettings
    {
        /// <summary>
        /// The messaging enabled status
        /// </summary>
        public bool? IsMessagingEnabled { get; set; }

        /// <summary>
        /// The messaging CTA enabled status
        /// </summary>
        public bool? IsMessagingCtaEnabled { get; set; }
    }

    /// <summary>
    /// Represents Facebook business information
    /// </summary>
    public class FacebookBusiness
    {
        /// <summary>
        /// The business ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The business name
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// Represents Facebook call to action
    /// </summary>
    public class FacebookCallToAction
    {
        /// <summary>
        /// The CTA type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The CTA value
        /// </summary>
        public FacebookCallToActionValue Value { get; set; }
    }

    /// <summary>
    /// Represents Facebook call to action value
    /// </summary>
    public class FacebookCallToActionValue
    {
        /// <summary>
        /// The CTA link
        /// </summary>
        public string Link { get; set; }

        /// <summary>
        /// The CTA page
        /// </summary>
        public string Page { get; set; }

        /// <summary>
        /// The CTA app link
        /// </summary>
        public string AppLink { get; set; }
    }

    /// <summary>
    /// Represents Facebook contact address
    /// </summary>
    public class FacebookContactAddress
    {
        /// <summary>
        /// The street address
        /// </summary>
        public string Street1 { get; set; }

        /// <summary>
        /// The street address line 2
        /// </summary>
        public string Street2 { get; set; }

        /// <summary>
        /// The city
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// The state/province
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// The country
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// The zip code
        /// </summary>
        public string Zip { get; set; }
    }

    /// <summary>
    /// Represents Facebook delivery and pickup options info
    /// </summary>
    public class FacebookDeliveryAndPickupOptionsInfo
    {
        /// <summary>
        /// The delivery enabled status
        /// </summary>
        public bool? DeliveryEnabled { get; set; }

        /// <summary>
        /// The pickup enabled status
        /// </summary>
        public bool? PickupEnabled { get; set; }
    }

    /// <summary>
    /// Represents Facebook engagement
    /// </summary>
    public class FacebookEngagement
    {
        /// <summary>
        /// The engagement count
        /// </summary>
        public long? Count { get; set; }

        /// <summary>
        /// The engagement count string
        /// </summary>
        public string CountString { get; set; }

        /// <summary>
        /// The engagement count string with like
        /// </summary>
        public string CountStringWithLike { get; set; }

        /// <summary>
        /// The engagement count string without like
        /// </summary>
        public string CountStringWithoutLike { get; set; }

        /// <summary>
        /// The social sentence
        /// </summary>
        public string SocialSentence { get; set; }

        /// <summary>
        /// The social sentence with like
        /// </summary>
        public string SocialSentenceWithLike { get; set; }

        /// <summary>
        /// The social sentence without like
        /// </summary>
        public string SocialSentenceWithoutLike { get; set; }
    }

    /// <summary>
    /// Represents Facebook Instagram business account
    /// </summary>
    public class FacebookInstagramBusinessAccount
    {
        /// <summary>
        /// The Instagram business account ID
        /// </summary>
        public string Id { get; set; }
    }

    /// <summary>
    /// Represents Facebook messenger ad icebreaker
    /// </summary>
    public class FacebookMessengerAdIcebreaker
    {
        /// <summary>
        /// The icebreaker question
        /// </summary>
        public string Question { get; set; }

        /// <summary>
        /// The icebreaker payload
        /// </summary>
        public string Payload { get; set; }
    }

    /// <summary>
    /// Represents Facebook messenger ad default page welcome message
    /// </summary>
    public class FacebookMessengerAdDefaultPageWelcomeMessage
    {
        /// <summary>
        /// The welcome message
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// Represents Facebook messenger ad quick reply
    /// </summary>
    public class FacebookMessengerAdQuickReply
    {
        /// <summary>
        /// The quick reply title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The quick reply payload
        /// </summary>
        public string Payload { get; set; }

        /// <summary>
        /// The quick reply image URL
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// The quick reply content type
        /// </summary>
        public string ContentType { get; set; }
    }

    /// <summary>
    /// Represents Facebook start info
    /// </summary>
    public class FacebookStartInfo
    {
        /// <summary>
        /// The start type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The start date
        /// </summary>
        public DateTime? Date { get; set; }
    }

    /// <summary>
    /// Represents Facebook VoIP info
    /// </summary>
    public class FacebookVoipInfo
    {
        /// <summary>
        /// The VoIP enabled status
        /// </summary>
        public bool? IsVoipEnabled { get; set; }

        /// <summary>
        /// The VoIP callable status
        /// </summary>
        public bool? IsCallableVoip { get; set; }

        /// <summary>
        /// The VoIP pushable status
        /// </summary>
        public bool? IsPushableVoip { get; set; }
    }
}
