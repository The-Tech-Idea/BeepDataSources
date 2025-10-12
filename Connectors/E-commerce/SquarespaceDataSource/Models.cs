// File: BeepDM/Connectors/Ecommerce/SquarespaceDataSource/Models/SquarespaceModels.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Ecommerce.SquarespaceDataSource.Models
{
    // Base
    public abstract class SquarespaceEntityBase
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("createdOn")] public DateTimeOffset? CreatedOn { get; set; }
        [JsonPropertyName("modifiedOn")] public DateTimeOffset? ModifiedOn { get; set; }

        [JsonIgnore] public IDataSource? DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : SquarespaceEntityBase { DataSource = ds; return (T)this; }
    }

    // ---------- Core Commerce objects ----------

    public sealed class SquarespaceProduct : SquarespaceEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("urlId")] public string? UrlId { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("excerpt")] public string? Excerpt { get; set; }
        [JsonPropertyName("visible")] public bool? Visible { get; set; }
        [JsonPropertyName("featured")] public bool? Featured { get; set; }
        [JsonPropertyName("price")] public SquarespacePrice? Price { get; set; }
        [JsonPropertyName("salePrice")] public SquarespacePrice? SalePrice { get; set; }
        [JsonPropertyName("onSale")] public bool? OnSale { get; set; }
        [JsonPropertyName("variants")] public List<SquarespaceProductVariant>? Variants { get; set; } = new();
        [JsonPropertyName("variantOptions")] public List<SquarespaceVariantOption>? VariantOptions { get; set; } = new();
        [JsonPropertyName("images")] public List<SquarespaceProductImage>? Images { get; set; } = new();
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("weight")] public SquarespaceWeight? Weight { get; set; }
        [JsonPropertyName("dimensions")] public SquarespaceDimensions? Dimensions { get; set; }
        [JsonPropertyName("shipping")] public SquarespaceShipping? Shipping { get; set; }
        [JsonPropertyName("tags")] public List<string>? Tags { get; set; } = new();
        [JsonPropertyName("categoryId")] public string? CategoryId { get; set; }
        [JsonPropertyName("storePageId")] public string? StorePageId { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("additionalFields")] public List<SquarespaceAdditionalField>? AdditionalFields { get; set; } = new();
    }

    public sealed class SquarespacePrice
    {
        [JsonPropertyName("value")] public decimal? Value { get; set; }
        [JsonPropertyName("currency")] public string? Currency { get; set; }
    }

    public sealed class SquarespaceProductVariant : SquarespaceEntityBase
    {
        [JsonPropertyName("productId")] public string? ProductId { get; set; }
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("price")] public SquarespacePrice? Price { get; set; }
        [JsonPropertyName("salePrice")] public SquarespacePrice? SalePrice { get; set; }
        [JsonPropertyName("onSale")] public bool? OnSale { get; set; }
        [JsonPropertyName("weight")] public SquarespaceWeight? Weight { get; set; }
        [JsonPropertyName("dimensions")] public SquarespaceDimensions? Dimensions { get; set; }
        [JsonPropertyName("shipping")] public SquarespaceShipping? Shipping { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("unlimited")] public bool? Unlimited { get; set; }
        [JsonPropertyName("attributes")] public Dictionary<string, string>? Attributes { get; set; } = new();
    }

    public sealed class SquarespaceVariantOption
    {
        [JsonPropertyName("optionName")] public string? OptionName { get; set; }
        [JsonPropertyName("values")] public List<string>? Values { get; set; } = new();
    }

    public sealed class SquarespaceProductImage
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("altText")] public string? AltText { get; set; }
        [JsonPropertyName("caption")] public string? Caption { get; set; }
    }

    public sealed class SquarespaceWeight
    {
        [JsonPropertyName("value")] public decimal? Value { get; set; }
        [JsonPropertyName("unit")] public string? Unit { get; set; }
    }

    public sealed class SquarespaceDimensions
    {
        [JsonPropertyName("length")] public decimal? Length { get; set; }
        [JsonPropertyName("width")] public decimal? Width { get; set; }
        [JsonPropertyName("height")] public decimal? Height { get; set; }
        [JsonPropertyName("unit")] public string? Unit { get; set; }
    }

    public sealed class SquarespaceShipping
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("carrierName")] public string? CarrierName { get; set; }
        [JsonPropertyName("serviceName")] public string? ServiceName { get; set; }
    }

    public sealed class SquarespaceAdditionalField
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
    }

    public sealed class SquarespaceOrder : SquarespaceEntityBase
    {
        [JsonPropertyName("number")] public int? Number { get; set; }
        [JsonPropertyName("channel")] public string? Channel { get; set; }
        [JsonPropertyName("externalOrderReference")] public string? ExternalOrderReference { get; set; }
        [JsonPropertyName("customerEmail")] public string? CustomerEmail { get; set; }
        [JsonPropertyName("billingAddress")] public SquarespaceAddress? BillingAddress { get; set; }
        [JsonPropertyName("shippingAddress")] public SquarespaceAddress? ShippingAddress { get; set; }
        [JsonPropertyName("lineItems")] public List<SquarespaceOrderLineItem>? LineItems { get; set; } = new();
        [JsonPropertyName("fulfillments")] public List<SquarespaceOrderFulfillment>? Fulfillments { get; set; } = new();
        [JsonPropertyName("subtotal")] public SquarespacePrice? Subtotal { get; set; }
        [JsonPropertyName("shippingTotal")] public SquarespacePrice? ShippingTotal { get; set; }
        [JsonPropertyName("taxTotal")] public SquarespacePrice? TaxTotal { get; set; }
        [JsonPropertyName("discountTotal")] public SquarespacePrice? DiscountTotal { get; set; }
        [JsonPropertyName("grandTotal")] public SquarespacePrice? GrandTotal { get; set; }
        [JsonPropertyName("testmode")] public bool? Testmode { get; set; }
        [JsonPropertyName("customerId")] public string? CustomerId { get; set; }
        [JsonPropertyName("discounts")] public List<SquarespaceOrderDiscount>? Discounts { get; set; } = new();
        [JsonPropertyName("formSubmission")] public SquarespaceFormSubmission? FormSubmission { get; set; }
    }

    public sealed class SquarespaceAddress
    {
        [JsonPropertyName("firstName")] public string? FirstName { get; set; }
        [JsonPropertyName("lastName")] public string? LastName { get; set; }
        [JsonPropertyName("address1")] public string? Address1 { get; set; }
        [JsonPropertyName("address2")] public string? Address2 { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("region")] public string? Region { get; set; }
        [JsonPropertyName("country")] public string? Country { get; set; }
        [JsonPropertyName("postalCode")] public string? PostalCode { get; set; }
        [JsonPropertyName("phone")] public string? Phone { get; set; }
    }

    public sealed class SquarespaceOrderLineItem
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("productId")] public string? ProductId { get; set; }
        [JsonPropertyName("variantId")] public string? VariantId { get; set; }
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("productName")] public string? ProductName { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("unitPricePaid")] public SquarespacePrice? UnitPricePaid { get; set; }
        [JsonPropertyName("lineTotal")] public SquarespacePrice? LineTotal { get; set; }
        [JsonPropertyName("variantOptions")] public List<SquarespaceVariantOptionValue>? VariantOptions { get; set; } = new();
        [JsonPropertyName("customizations")] public List<SquarespaceCustomization>? Customizations { get; set; } = new();
        [JsonPropertyName("imageUrl")] public string? ImageUrl { get; set; }
        [JsonPropertyName("weight")] public SquarespaceWeight? Weight { get; set; }
    }

    public sealed class SquarespaceVariantOptionValue
    {
        [JsonPropertyName("optionName")] public string? OptionName { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
    }

    public sealed class SquarespaceCustomization
    {
        [JsonPropertyName("label")] public string? Label { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
    }

    public sealed class SquarespaceOrderFulfillment : SquarespaceEntityBase
    {
        [JsonPropertyName("orderId")] public string? OrderId { get; set; }
        [JsonPropertyName("shipments")] public List<SquarespaceShipment>? Shipments { get; set; } = new();
    }

    public sealed class SquarespaceShipment
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("trackingNumber")] public string? TrackingNumber { get; set; }
        [JsonPropertyName("trackingUrl")] public string? TrackingUrl { get; set; }
        [JsonPropertyName("carrierName")] public string? CarrierName { get; set; }
        [JsonPropertyName("serviceName")] public string? ServiceName { get; set; }
        [JsonPropertyName("shipDate")] public DateTimeOffset? ShipDate { get; set; }
        [JsonPropertyName("lineItems")] public List<SquarespaceShipmentLineItem>? LineItems { get; set; } = new();
    }

    public sealed class SquarespaceShipmentLineItem
    {
        [JsonPropertyName("productId")] public string? ProductId { get; set; }
        [JsonPropertyName("variantId")] public string? VariantId { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
    }

    public sealed class SquarespaceOrderDiscount
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("applied")] public string? Applied { get; set; }
        [JsonPropertyName("value")] public SquarespacePrice? Value { get; set; }
    }

    public sealed class SquarespaceInventory
    {
        [JsonPropertyName("productId")] public string? ProductId { get; set; }
        [JsonPropertyName("variantId")] public string? VariantId { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("isUnlimited")] public bool? IsUnlimited { get; set; }
    }

    public sealed class SquarespaceProfile
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("firstName")] public string? FirstName { get; set; }
        [JsonPropertyName("lastName")] public string? LastName { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("bio")] public string? Bio { get; set; }
        [JsonPropertyName("websiteUrl")] public string? WebsiteUrl { get; set; }
        [JsonPropertyName("profileImageUrl")] public string? ProfileImageUrl { get; set; }
    }

    // ---------- Content Management ----------

    public sealed class SquarespacePage : SquarespaceEntityBase
    {
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("urlId")] public string? UrlId { get; set; }
        [JsonPropertyName("body")] public string? Body { get; set; }
        [JsonPropertyName("excerpt")] public string? Excerpt { get; set; }
        [JsonPropertyName("featuredImageUrl")] public string? FeaturedImageUrl { get; set; }
        [JsonPropertyName("thumbnailUrl")] public string? ThumbnailUrl { get; set; }
        [JsonPropertyName("promotedBlockId")] public string? PromotedBlockId { get; set; }
        [JsonPropertyName("pageType")] public string? PageType { get; set; }
        [JsonPropertyName("folderId")] public string? FolderId { get; set; }
        [JsonPropertyName("index")] public bool? Index { get; set; }
        [JsonPropertyName("homepage")] public bool? Homepage { get; set; }
        [JsonPropertyName("passphrase")] public string? Passphrase { get; set; }
        [JsonPropertyName("tags")] public List<string>? Tags { get; set; } = new();
        [JsonPropertyName("categories")] public List<string>? Categories { get; set; } = new();
        [JsonPropertyName("likeCount")] public int? LikeCount { get; set; }
        [JsonPropertyName("commentCount")] public int? CommentCount { get; set; }
        [JsonPropertyName("publicCommentCount")] public int? PublicCommentCount { get; set; }
        [JsonPropertyName("disqusId")] public string? DisqusId { get; set; }
        [JsonPropertyName("contactEmail")] public string? ContactEmail { get; set; }
        [JsonPropertyName("contactPhoneNumber")] public string? ContactPhoneNumber { get; set; }
        [JsonPropertyName("location")] public SquarespaceLocation? Location { get; set; }
    }

    public sealed class SquarespaceLocation
    {
        [JsonPropertyName("mapLat")] public decimal? MapLat { get; set; }
        [JsonPropertyName("mapLng")] public decimal? MapLng { get; set; }
        [JsonPropertyName("mapZoom")] public int? MapZoom { get; set; }
        [JsonPropertyName("markerLat")] public decimal? MarkerLat { get; set; }
        [JsonPropertyName("markerLng")] public decimal? MarkerLng { get; set; }
        [JsonPropertyName("addressLine1")] public string? AddressLine1 { get; set; }
        [JsonPropertyName("addressLine2")] public string? AddressLine2 { get; set; }
        [JsonPropertyName("addressCountry")] public string? AddressCountry { get; set; }
    }

    public sealed class SquarespaceBlog : SquarespaceEntityBase
    {
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("urlId")] public string? UrlId { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("websiteId")] public string? WebsiteId { get; set; }
        [JsonPropertyName("tags")] public List<string>? Tags { get; set; } = new();
        [JsonPropertyName("categories")] public List<string>? Categories { get; set; } = new();
    }

    public sealed class SquarespaceBlogPost : SquarespaceEntityBase
    {
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("urlId")] public string? UrlId { get; set; }
        [JsonPropertyName("body")] public string? Body { get; set; }
        [JsonPropertyName("excerpt")] public string? Excerpt { get; set; }
        [JsonPropertyName("featuredImageUrl")] public string? FeaturedImageUrl { get; set; }
        [JsonPropertyName("thumbnailUrl")] public string? ThumbnailUrl { get; set; }
        [JsonPropertyName("author")] public SquarespaceProfile? Author { get; set; }
        [JsonPropertyName("blogId")] public string? BlogId { get; set; }
        [JsonPropertyName("categories")] public List<string>? Categories { get; set; } = new();
        [JsonPropertyName("tags")] public List<string>? Tags { get; set; } = new();
        [JsonPropertyName("likeCount")] public int? LikeCount { get; set; }
        [JsonPropertyName("commentCount")] public int? CommentCount { get; set; }
        [JsonPropertyName("publicCommentCount")] public int? PublicCommentCount { get; set; }
        [JsonPropertyName("disqusId")] public string? DisqusId { get; set; }
        [JsonPropertyName("promotedBlockId")] public string? PromotedBlockId { get; set; }
        [JsonPropertyName("fullUrl")] public string? FullUrl { get; set; }
        [JsonPropertyName("sourceUrl")] public string? SourceUrl { get; set; }
    }

    public sealed class SquarespaceEvent : SquarespaceEntityBase
    {
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("urlId")] public string? UrlId { get; set; }
        [JsonPropertyName("body")] public string? Body { get; set; }
        [JsonPropertyName("excerpt")] public string? Excerpt { get; set; }
        [JsonPropertyName("featuredImageUrl")] public string? FeaturedImageUrl { get; set; }
        [JsonPropertyName("thumbnailUrl")] public string? ThumbnailUrl { get; set; }
        [JsonPropertyName("startDate")] public DateTimeOffset? StartDate { get; set; }
        [JsonPropertyName("endDate")] public DateTimeOffset? EndDate { get; set; }
        [JsonPropertyName("timezone")] public string? Timezone { get; set; }
        [JsonPropertyName("location")] public SquarespaceLocation? Location { get; set; }
        [JsonPropertyName("rsvp")] public SquarespaceEventRsvp? Rsvp { get; set; }
        [JsonPropertyName("tags")] public List<string>? Tags { get; set; } = new();
        [JsonPropertyName("categories")] public List<string>? Categories { get; set; } = new();
    }

    public sealed class SquarespaceEventRsvp
    {
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("capacity")] public int? Capacity { get; set; }
        [JsonPropertyName("guestCount")] public int? GuestCount { get; set; }
    }

    public sealed class SquarespaceGallery : SquarespaceEntityBase
    {
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("urlId")] public string? UrlId { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("websiteId")] public string? WebsiteId { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("aspectRatio")] public string? AspectRatio { get; set; }
    }

    public sealed class SquarespaceGalleryImage : SquarespaceEntityBase
    {
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("urlId")] public string? UrlId { get; set; }
        [JsonPropertyName("body")] public string? Body { get; set; }
        [JsonPropertyName("clickthroughUrl")] public string? ClickthroughUrl { get; set; }
        [JsonPropertyName("filename")] public string? Filename { get; set; }
        [JsonPropertyName("mediaFocalPoint")] public SquarespaceMediaFocalPoint? MediaFocalPoint { get; set; }
        [JsonPropertyName("displayIndex")] public int? DisplayIndex { get; set; }
        [JsonPropertyName("galleryId")] public string? GalleryId { get; set; }
    }

    public sealed class SquarespaceMediaFocalPoint
    {
        [JsonPropertyName("x")] public decimal? X { get; set; }
        [JsonPropertyName("y")] public decimal? Y { get; set; }
    }

    // ---------- Store Settings ----------

    public sealed class SquarespaceStore
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("websiteId")] public string? WebsiteId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("currency")] public string? Currency { get; set; }
        [JsonPropertyName("countryCode")] public string? CountryCode { get; set; }
        [JsonPropertyName("timezone")] public string? Timezone { get; set; }
        [JsonPropertyName("merchantCategoryCode")] public string? MerchantCategoryCode { get; set; }
        [JsonPropertyName("businessName")] public string? BusinessName { get; set; }
        [JsonPropertyName("businessAddress")] public SquarespaceAddress? BusinessAddress { get; set; }
        [JsonPropertyName("websiteTitle")] public string? WebsiteTitle { get; set; }
        [JsonPropertyName("websiteDescription")] public string? WebsiteDescription { get; set; }
        [JsonPropertyName("logoUrl")] public string? LogoUrl { get; set; }
        [JsonPropertyName("faviconUrl")] public string? FaviconUrl { get; set; }
        [JsonPropertyName("socialLinks")] public List<SquarespaceSocialLink>? SocialLinks { get; set; } = new();
        [JsonPropertyName("socialAccounts")] public List<SquarespaceSocialAccount>? SocialAccounts { get; set; } = new();
    }

    public sealed class SquarespaceSocialLink
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
    }

    public sealed class SquarespaceSocialAccount
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("screenname")] public string? Screenname { get; set; }
    }

    public sealed class SquarespaceShippingOption
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("countryCode")] public string? CountryCode { get; set; }
        [JsonPropertyName("stateCode")] public string? StateCode { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("postalCode")] public string? PostalCode { get; set; }
        [JsonPropertyName("carrierName")] public string? CarrierName { get; set; }
        [JsonPropertyName("serviceName")] public string? ServiceName { get; set; }
        [JsonPropertyName("rate")] public SquarespacePrice? Rate { get; set; }
        [JsonPropertyName("estimatedDeliveryDate")] public DateTimeOffset? EstimatedDeliveryDate { get; set; }
    }

    public sealed class SquarespaceTax
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("countryCode")] public string? CountryCode { get; set; }
        [JsonPropertyName("stateCode")] public string? StateCode { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("postalCode")] public string? PostalCode { get; set; }
        [JsonPropertyName("rate")] public decimal? Rate { get; set; }
        [JsonPropertyName("appliesToShipping")] public bool? AppliesToShipping { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    // ---------- Forms & Donations ----------

    public sealed class SquarespaceForm : SquarespaceEntityBase
    {
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("urlId")] public string? UrlId { get; set; }
        [JsonPropertyName("body")] public string? Body { get; set; }
        [JsonPropertyName("websiteId")] public string? WebsiteId { get; set; }
        [JsonPropertyName("folderId")] public string? FolderId { get; set; }
        [JsonPropertyName("index")] public bool? Index { get; set; }
        [JsonPropertyName("passphrase")] public string? Passphrase { get; set; }
        [JsonPropertyName("contactEmail")] public string? ContactEmail { get; set; }
        [JsonPropertyName("contactPhoneNumber")] public string? ContactPhoneNumber { get; set; }
        [JsonPropertyName("location")] public SquarespaceLocation? Location { get; set; }
    }

    public sealed class SquarespaceFormSubmission : SquarespaceEntityBase
    {
        [JsonPropertyName("formId")] public string? FormId { get; set; }
        [JsonPropertyName("firstName")] public string? FirstName { get; set; }
        [JsonPropertyName("lastName")] public string? LastName { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("website")] public string? Website { get; set; }
        [JsonPropertyName("subject")] public string? Subject { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("ipAddress")] public string? IpAddress { get; set; }
        [JsonPropertyName("fields")] public List<SquarespaceFormField>? Fields { get; set; } = new();
    }

    public sealed class SquarespaceFormField
    {
        [JsonPropertyName("label")] public string? Label { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
    }

    public sealed class SquarespaceDonation : SquarespaceEntityBase
    {
        [JsonPropertyName("amount")] public SquarespacePrice? Amount { get; set; }
        [JsonPropertyName("currency")] public string? Currency { get; set; }
        [JsonPropertyName("donorName")] public string? DonorName { get; set; }
        [JsonPropertyName("donorEmail")] public string? DonorEmail { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("anonymous")] public bool? Anonymous { get; set; }
        [JsonPropertyName("recurring")] public bool? Recurring { get; set; }
        [JsonPropertyName("frequency")] public string? Frequency { get; set; }
    }

    // ---------- Analytics ----------

    public sealed class SquarespaceWebsiteTraffic
    {
        [JsonPropertyName("date")] public DateTimeOffset? Date { get; set; }
        [JsonPropertyName("pageViews")] public int? PageViews { get; set; }
        [JsonPropertyName("uniqueVisitors")] public int? UniqueVisitors { get; set; }
        [JsonPropertyName("topPages")] public List<SquarespaceTopPage>? TopPages { get; set; } = new();
        [JsonPropertyName("topReferrers")] public List<SquarespaceTopReferrer>? TopReferrers { get; set; } = new();
    }

    public sealed class SquarespaceTopPage
    {
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("pageViews")] public int? PageViews { get; set; }
        [JsonPropertyName("uniqueVisitors")] public int? UniqueVisitors { get; set; }
    }

    public sealed class SquarespaceTopReferrer
    {
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("pageViews")] public int? PageViews { get; set; }
        [JsonPropertyName("uniqueVisitors")] public int? UniqueVisitors { get; set; }
    }

    public sealed class SquarespaceWebsiteOrder
    {
        [JsonPropertyName("date")] public DateTimeOffset? Date { get; set; }
        [JsonPropertyName("orders")] public int? Orders { get; set; }
        [JsonPropertyName("revenue")] public SquarespacePrice? Revenue { get; set; }
        [JsonPropertyName("averageOrderValue")] public SquarespacePrice? AverageOrderValue { get; set; }
        [JsonPropertyName("topProducts")] public List<SquarespaceTopProduct>? TopProducts { get; set; } = new();
    }

    public sealed class SquarespaceTopProduct
    {
        [JsonPropertyName("productId")] public string? ProductId { get; set; }
        [JsonPropertyName("productName")] public string? ProductName { get; set; }
        [JsonPropertyName("quantitySold")] public int? QuantitySold { get; set; }
        [JsonPropertyName("revenue")] public SquarespacePrice? Revenue { get; set; }
    }

    // ---------- Categories & Navigation ----------

    public sealed class SquarespaceCategory : SquarespaceEntityBase
    {
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("urlId")] public string? UrlId { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("parentId")] public string? ParentId { get; set; }
        [JsonPropertyName("ordinal")] public int? Ordinal { get; set; }
    }

    public sealed class SquarespaceNavigation
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("urlId")] public string? UrlId { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("children")] public List<SquarespaceNavigation>? Children { get; set; } = new();
        [JsonPropertyName("ordinal")] public int? Ordinal { get; set; }
        [JsonPropertyName("clickthroughUrl")] public string? ClickthroughUrl { get; set; }
        [JsonPropertyName("folderId")] public string? FolderId { get; set; }
        [JsonPropertyName("pageId")] public string? PageId { get; set; }
        [JsonPropertyName("externalUrl")] public string? ExternalUrl { get; set; }
    }
}