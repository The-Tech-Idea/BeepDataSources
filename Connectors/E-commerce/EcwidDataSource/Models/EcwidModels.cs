// File: BeepDM/Connectors/Ecommerce/EcwidDataSource/Models/EcwidModels.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Ecommerce.EcwidDataSource.Models
{
    // Base
    public abstract class EcwidEntityBase
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("created")] public DateTime? Created { get; set; }
        [JsonPropertyName("updated")] public DateTime? Updated { get; set; }

        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : EcwidEntityBase { DataSource = ds; return (T)this; }
    }

    // ---------- Products ----------

    public sealed class EcwidProduct : EcwidEntityBase
    {
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
        [JsonPropertyName("costPrice")] public decimal? CostPrice { get; set; }
        [JsonPropertyName("wholesalePrices")] public List<EcwidWholesalePrice>? WholesalePrices { get; set; } = new();
        [JsonPropertyName("compareToPrice")] public decimal? CompareToPrice { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("unlimited")] public bool? Unlimited { get; set; }
        [JsonPropertyName("weight")] public decimal? Weight { get; set; }
        [JsonPropertyName("productClassId")] public long? ProductClassId { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("warningLimit")] public int? WarningLimit { get; set; }
        [JsonPropertyName("fixedShippingRateOnly")] public bool? FixedShippingRateOnly { get; set; }
        [JsonPropertyName("fixedShippingRate")] public decimal? FixedShippingRate { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("productType")] public string? ProductType { get; set; }
        [JsonPropertyName("seoTitle")] public string? SeoTitle { get; set; }
        [JsonPropertyName("seoDescription")] public string? SeoDescription { get; set; }
        [JsonPropertyName("showOnFrontpage")] public int? ShowOnFrontpage { get; set; }
        [JsonPropertyName("isSampleProduct")] public bool? IsSampleProduct { get; set; }
        [JsonPropertyName("googleItemCondition")] public string? GoogleItemCondition { get; set; }
        [JsonPropertyName("isShippingRequired")] public bool? IsShippingRequired { get; set; }
        [JsonPropertyName("hdThumbnailUrl")] public string? HdThumbnailUrl { get; set; }
        [JsonPropertyName("thumbnailUrl")] public string? ThumbnailUrl { get; set; }
        [JsonPropertyName("originalImageUrl")] public string? OriginalImageUrl { get; set; }
        [JsonPropertyName("imageUrl")] public string? ImageUrl { get; set; }
        [JsonPropertyName("galleryImages")] public List<EcwidProductImage>? GalleryImages { get; set; } = new();
        [JsonPropertyName("files")] public List<EcwidProductFile>? Files { get; set; } = new();
        [JsonPropertyName("attributes")] public List<EcwidProductAttribute>? Attributes { get; set; } = new();
        [JsonPropertyName("options")] public List<EcwidProductOption>? Options { get; set; } = new();
        [JsonPropertyName("taxes")] public List<EcwidProductTax>? Taxes { get; set; } = new();
        [JsonPropertyName("relatedProducts")] public EcwidRelatedProducts? RelatedProducts { get; set; }
        [JsonPropertyName("combinations")] public List<EcwidProductVariation>? Combinations { get; set; } = new();
        [JsonPropertyName("categories")] public List<long>? Categories { get; set; } = new();
        [JsonPropertyName("defaultCategoryId")] public long? DefaultCategoryId { get; set; }
        [JsonPropertyName("inStock")] public bool? InStock { get; set; }
        [JsonPropertyName("onSale")] public bool? OnSale { get; set; }
        [JsonPropertyName("brand")] public string? Brand { get; set; }
        [JsonPropertyName("upc")] public string? Upc { get; set; }
        [JsonPropertyName("ean")] public string? Ean { get; set; }
        [JsonPropertyName("jan")] public string? Jan { get; set; }
        [JsonPropertyName("isbn")] public string? Isbn { get; set; }
        [JsonPropertyName("mpn")] public string? Mpn { get; set; }
        [JsonPropertyName("dimensions")] public EcwidProductDimensions? Dimensions { get; set; }
        [JsonPropertyName("showOnFrontpage")] public int? ShowOnFrontpageDuplicate { get; set; } // Handle duplicate
    }

    public sealed class EcwidProductImage
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("thumbnail")] public string? Thumbnail { get; set; }
        [JsonPropertyName("originalImageUrl")] public string? OriginalImageUrl { get; set; }
        [JsonPropertyName("imageUrl")] public string? ImageUrl { get; set; }
        [JsonPropertyName("hdThumbnailUrl")] public string? HdThumbnailUrl { get; set; }
        [JsonPropertyName("thumbnailUrl")] public string? ThumbnailUrl { get; set; }
        [JsonPropertyName("alt")] public string? Alt { get; set; }
        [JsonPropertyName("width")] public int? Width { get; set; }
        [JsonPropertyName("height")] public int? Height { get; set; }
    }

    public sealed class EcwidProductFile
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("size")] public long? Size { get; set; }
        [JsonPropertyName("adminUrl")] public string? AdminUrl { get; set; }
        [JsonPropertyName("customerUrl")] public string? CustomerUrl { get; set; }
    }

    public sealed class EcwidProductAttribute
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
        [JsonPropertyName("show")] public string? Show { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
    }

    public sealed class EcwidProductOption
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("choices")] public List<EcwidProductOptionChoice>? Choices { get; set; } = new();
        [JsonPropertyName("defaultChoice")] public int? DefaultChoice { get; set; }
        [JsonPropertyName("required")] public bool? Required { get; set; }
    }

    public sealed class EcwidProductOptionChoice
    {
        [JsonPropertyName("text")] public string? Text { get; set; }
        [JsonPropertyName("priceModifier")] public decimal? PriceModifier { get; set; }
        [JsonPropertyName("priceModifierType")] public string? PriceModifierType { get; set; }
    }

    public sealed class EcwidProductTax
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("includeInPrice")] public bool? IncludeInPrice { get; set; }
        [JsonPropertyName("useShippingAddress")] public bool? UseShippingAddress { get; set; }
        [JsonPropertyName("taxShipping")] public bool? TaxShipping { get; set; }
        [JsonPropertyName("appliedByDefault")] public bool? AppliedByDefault { get; set; }
        [JsonPropertyName("rules")] public List<EcwidTaxRule>? Rules { get; set; } = new();
    }

    public sealed class EcwidTaxRule
    {
        [JsonPropertyName("zoneId")] public long? ZoneId { get; set; }
        [JsonPropertyName("tax")] public decimal? Tax { get; set; }
    }

    public sealed class EcwidRelatedProducts
    {
        [JsonPropertyName("productIds")] public List<long>? ProductIds { get; set; } = new();
    }

    public sealed class EcwidProductVariation
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("combinationNumber")] public int? CombinationNumber { get; set; }
        [JsonPropertyName("options")] public List<EcwidVariationOption>? Options { get; set; } = new();
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("unlimited")] public bool? Unlimited { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
        [JsonPropertyName("costPrice")] public decimal? CostPrice { get; set; }
        [JsonPropertyName("weight")] public decimal? Weight { get; set; }
        [JsonPropertyName("warningLimit")] public int? WarningLimit { get; set; }
        [JsonPropertyName("imageUrl")] public string? ImageUrl { get; set; }
        [JsonPropertyName("hdThumbnailUrl")] public string? HdThumbnailUrl { get; set; }
        [JsonPropertyName("thumbnailUrl")] public string? ThumbnailUrl { get; set; }
        [JsonPropertyName("smallThumbnailUrl")] public string? SmallThumbnailUrl { get; set; }
        [JsonPropertyName("originalImageUrl")] public string? OriginalImageUrl { get; set; }
        [JsonPropertyName("attributes")] public List<EcwidProductAttribute>? Attributes { get; set; } = new();
    }

    public sealed class EcwidVariationOption
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
    }

    public sealed class EcwidProductDimensions
    {
        [JsonPropertyName("length")] public decimal? Length { get; set; }
        [JsonPropertyName("width")] public decimal? Width { get; set; }
        [JsonPropertyName("height")] public decimal? Height { get; set; }
    }

    public sealed class EcwidWholesalePrice
    {
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
    }

    // ---------- Categories ----------

    public sealed class EcwidCategory : EcwidEntityBase
    {
        [JsonPropertyName("parentId")] public long? ParentId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("imageUrl")] public string? ImageUrl { get; set; }
        [JsonPropertyName("thumbnailUrl")] public string? ThumbnailUrl { get; set; }
        [JsonPropertyName("originalImageUrl")] public string? OriginalImageUrl { get; set; }
        [JsonPropertyName("hdThumbnailUrl")] public string? HdThumbnailUrl { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("orderBy")] public int? OrderBy { get; set; }
        [JsonPropertyName("productCount")] public int? ProductCount { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("seoTitle")] public string? SeoTitle { get; set; }
        [JsonPropertyName("seoDescription")] public string? SeoDescription { get; set; }
    }

    // ---------- Orders ----------

    public sealed class EcwidOrder : EcwidEntityBase
    {
        [JsonPropertyName("vendorOrderNumber")] public string? VendorOrderNumber { get; set; }
        [JsonPropertyName("subtotal")] public decimal? Subtotal { get; set; }
        [JsonPropertyName("total")] public decimal? Total { get; set; }
        [JsonPropertyName("usdTotal")] public decimal? UsdTotal { get; set; }
        [JsonPropertyName("paymentMethod")] public string? PaymentMethod { get; set; }
        [JsonPropertyName("paymentModule")] public string? PaymentModule { get; set; }
        [JsonPropertyName("paymentStatus")] public string? PaymentStatus { get; set; }
        [JsonPropertyName("fulfillmentStatus")] public string? FulfillmentStatus { get; set; }
        [JsonPropertyName("orderComments")] public string? OrderComments { get; set; }
        [JsonPropertyName("customerId")] public long? CustomerId { get; set; }
        [JsonPropertyName("customerEmail")] public string? CustomerEmail { get; set; }
        [JsonPropertyName("customerGroup")] public string? CustomerGroup { get; set; }
        [JsonPropertyName("customerTaxExempt")] public bool? CustomerTaxExempt { get; set; }
        [JsonPropertyName("customerTaxId")] public string? CustomerTaxId { get; set; }
        [JsonPropertyName("customerTaxIdValid")] public bool? CustomerTaxIdValid { get; set; }
        [JsonPropertyName("reversedTaxApplied")] public bool? ReversedTaxApplied { get; set; }
        [JsonPropertyName("acceptMarketing")] public bool? AcceptMarketing { get; set; }
        [JsonPropertyName("disableAllCustomerNotifications")] public bool? DisableAllCustomerNotifications { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("ipAddress")] public string? IpAddress { get; set; }
        [JsonPropertyName("refererUrl")] public string? RefererUrl { get; set; }
        [JsonPropertyName("globalReferer")] public string? GlobalReferer { get; set; }
        [JsonPropertyName("createDate")] public DateTime? CreateDate { get; set; }
        [JsonPropertyName("createTimestamp")] public long? CreateTimestamp { get; set; }
        [JsonPropertyName("updateDate")] public DateTime? UpdateDate { get; set; }
        [JsonPropertyName("updateTimestamp")] public long? UpdateTimestamp { get; set; }
        [JsonPropertyName("items")] public List<EcwidOrderItem>? Items { get; set; } = new();
        [JsonPropertyName("billingPerson")] public EcwidPerson? BillingPerson { get; set; }
        [JsonPropertyName("shippingPerson")] public EcwidPerson? ShippingPerson { get; set; }
        [JsonPropertyName("shippingOption")] public EcwidShippingOption? ShippingOption { get; set; }
        [JsonPropertyName("handlingFee")] public EcwidHandlingFee? HandlingFee { get; set; }
        [JsonPropertyName("additionalInfo")] public List<EcwidOrderAdditionalInfo>? AdditionalInfo { get; set; } = new();
        [JsonPropertyName("paymentParams")] public Dictionary<string, string>? PaymentParams { get; set; } = new();
        [JsonPropertyName("discountInfo")] public List<EcwidDiscountInfo>? DiscountInfo { get; set; } = new();
        [JsonPropertyName("couponDiscount")] public decimal? CouponDiscount { get; set; }
        [JsonPropertyName("volumeDiscount")] public decimal? VolumeDiscount { get; set; }
        [JsonPropertyName("membershipBasedDiscount")] public decimal? MembershipBasedDiscount { get; set; }
        [JsonPropertyName("totalAndMembershipBasedDiscount")] public decimal? TotalAndMembershipBasedDiscount { get; set; }
        [JsonPropertyName("taxesOnShipping")] public List<EcwidTaxOnShipping>? TaxesOnShipping { get; set; } = new();
        [JsonPropertyName("taxes")] public List<EcwidOrderTax>? Taxes { get; set; } = new();
        [JsonPropertyName("couponCode")] public string? CouponCode { get; set; }
        [JsonPropertyName("trackingNumber")] public string? TrackingNumber { get; set; }
        [JsonPropertyName("affiliateId")] public string? AffiliateId { get; set; }
        [JsonPropertyName("externalTransactionId")] public string? ExternalTransactionId { get; set; }
        [JsonPropertyName("externalOrderId")] public string? ExternalOrderId { get; set; }
        [JsonPropertyName("avsMessage")] public string? AvsMessage { get; set; }
        [JsonPropertyName("cvvMessage")] public string? CvvMessage { get; set; }
        [JsonPropertyName("creditCardStatus")] public EcwidCreditCardStatus? CreditCardStatus { get; set; }
        [JsonPropertyName("incomplete")] public bool? Incomplete { get; set; }
    }

    public sealed class EcwidOrderItem
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("productId")] public long? ProductId { get; set; }
        [JsonPropertyName("categoryId")] public long? CategoryId { get; set; }
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("shortDescription")] public string? ShortDescription { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
        [JsonPropertyName("costPrice")] public decimal? CostPrice { get; set; }
        [JsonPropertyName("weight")] public decimal? Weight { get; set; }
        [JsonPropertyName("isShippingRequired")] public bool? IsShippingRequired { get; set; }
        [JsonPropertyName("trackQuantity")] public bool? TrackQuantity { get; set; }
        [JsonPropertyName("fixedShippingRateOnly")] public bool? FixedShippingRateOnly { get; set; }
        [JsonPropertyName("digital")] public bool? Digital { get; set; }
        [JsonPropertyName("couponApplied")] public bool? CouponApplied { get; set; }
        [JsonPropertyName("selectedOptions")] public List<EcwidOrderItemOption>? SelectedOptions { get; set; } = new();
        [JsonPropertyName("taxes")] public List<EcwidOrderItemTax>? Taxes { get; set; } = new();
        [JsonPropertyName("dimensions")] public EcwidProductDimensions? Dimensions { get; set; }
        [JsonPropertyName("imageUrl")] public string? ImageUrl { get; set; }
        [JsonPropertyName("smallThumbnailUrl")] public string? SmallThumbnailUrl { get; set; }
        [JsonPropertyName("hdThumbnailUrl")] public string? HdThumbnailUrl { get; set; }
        [JsonPropertyName("thumbnailUrl")] public string? ThumbnailUrl { get; set; }
        [JsonPropertyName("files")] public List<EcwidOrderItemFile>? Files { get; set; } = new();
        [JsonPropertyName("quantityInStock")] public int? QuantityInStock { get; set; }
        [JsonPropertyName("nameTranslated")] public Dictionary<string, string>? NameTranslated { get; set; } = new();
        [JsonPropertyName("shortDescriptionTranslated")] public Dictionary<string, string>? ShortDescriptionTranslated { get; set; } = new();
    }

    public sealed class EcwidOrderItemOption
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
        [JsonPropertyName("valuesArray")] public List<string>? ValuesArray { get; set; } = new();
        [JsonPropertyName("files")] public List<EcwidOrderItemFile>? Files { get; set; } = new();
        [JsonPropertyName("selections")] public List<EcwidOrderItemSelection>? Selections { get; set; } = new();
    }

    public sealed class EcwidOrderItemFile
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("size")] public long? Size { get; set; }
        [JsonPropertyName("adminUrl")] public string? AdminUrl { get; set; }
        [JsonPropertyName("customerUrl")] public string? CustomerUrl { get; set; }
    }

    public sealed class EcwidOrderItemSelection
    {
        [JsonPropertyName("selectionTitle")] public string? SelectionTitle { get; set; }
        [JsonPropertyName("selectionModifier")] public decimal? SelectionModifier { get; set; }
        [JsonPropertyName("selectionModifierType")] public string? SelectionModifierType { get; set; }
    }

    public sealed class EcwidOrderItemTax
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("rate")] public decimal? Rate { get; set; }
        [JsonPropertyName("taxOnDiscountedSubtotal")] public decimal? TaxOnDiscountedSubtotal { get; set; }
        [JsonPropertyName("taxOnShipping")] public decimal? TaxOnShipping { get; set; }
        [JsonPropertyName("includeInPrice")] public bool? IncludeInPrice { get; set; }
        [JsonPropertyName("total")] public decimal? Total { get; set; }
    }

    public sealed class EcwidPerson
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("companyName")] public string? CompanyName { get; set; }
        [JsonPropertyName("street")] public string? Street { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("countryCode")] public string? CountryCode { get; set; }
        [JsonPropertyName("countryName")] public string? CountryName { get; set; }
        [JsonPropertyName("postalCode")] public string? PostalCode { get; set; }
        [JsonPropertyName("stateOrProvinceCode")] public string? StateOrProvinceCode { get; set; }
        [JsonPropertyName("stateOrProvinceName")] public string? StateOrProvinceName { get; set; }
        [JsonPropertyName("phone")] public string? Phone { get; set; }
    }

    public sealed class EcwidShippingOption
    {
        [JsonPropertyName("shippingCarrierName")] public string? ShippingCarrierName { get; set; }
        [JsonPropertyName("shippingServiceName")] public string? ShippingServiceName { get; set; }
        [JsonPropertyName("shippingRate")] public decimal? ShippingRate { get; set; }
        [JsonPropertyName("estimatedTransitTime")] public string? EstimatedTransitTime { get; set; }
        [JsonPropertyName("isPickup")] public bool? IsPickup { get; set; }
        [JsonPropertyName("pickupInstruction")] public string? PickupInstruction { get; set; }
    }

    public sealed class EcwidHandlingFee
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("value")] public decimal? Value { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
    }

    public sealed class EcwidOrderAdditionalInfo
    {
        [JsonPropertyName("orderId")] public long? OrderId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
    }

    public sealed class EcwidDiscountInfo
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("value")] public decimal? Value { get; set; }
        [JsonPropertyName("total")] public decimal? Total { get; set; }
    }

    public sealed class EcwidTaxOnShipping
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("rate")] public decimal? Rate { get; set; }
        [JsonPropertyName("total")] public decimal? Total { get; set; }
    }

    public sealed class EcwidOrderTax
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("rate")] public decimal? Rate { get; set; }
        [JsonPropertyName("includeInPrice")] public bool? IncludeInPrice { get; set; }
        [JsonPropertyName("taxOnDiscountedSubtotal")] public decimal? TaxOnDiscountedSubtotal { get; set; }
        [JsonPropertyName("taxOnShipping")] public decimal? TaxOnShipping { get; set; }
        [JsonPropertyName("total")] public decimal? Total { get; set; }
    }

    public sealed class EcwidCreditCardStatus
    {
        [JsonPropertyName("avsMessage")] public string? AvsMessage { get; set; }
        [JsonPropertyName("cvvMessage")] public string? CvvMessage { get; set; }
    }

    // ---------- Customers ----------

    public sealed class EcwidCustomer : EcwidEntityBase
    {
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("firstName")] public string? FirstName { get; set; }
        [JsonPropertyName("lastName")] public string? LastName { get; set; }
        [JsonPropertyName("registered")] public DateTime? Registered { get; set; }
        [JsonPropertyName("updated")] public DateTime? Updated { get; set; }
        [JsonPropertyName("customerGroupId")] public long? CustomerGroupId { get; set; }
        [JsonPropertyName("customerGroupName")] public string? CustomerGroupName { get; set; }
        [JsonPropertyName("billingPerson")] public EcwidPerson? BillingPerson { get; set; }
        [JsonPropertyName("shippingAddresses")] public List<EcwidCustomerAddress>? ShippingAddresses { get; set; } = new();
        [JsonPropertyName("contacts")] public List<EcwidCustomerContact>? Contacts { get; set; } = new();
        [JsonPropertyName("taxExempt")] public bool? TaxExempt { get; set; }
        [JsonPropertyName("taxId")] public string? TaxId { get; set; }
        [JsonPropertyName("acceptMarketing")] public bool? AcceptMarketing { get; set; }
        [JsonPropertyName("totalOrderCount")] public int? TotalOrderCount { get; set; }
        [JsonPropertyName("totalOrderValue")] public decimal? TotalOrderValue { get; set; }
    }

    public sealed class EcwidCustomerAddress
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("companyName")] public string? CompanyName { get; set; }
        [JsonPropertyName("street")] public string? Street { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("countryCode")] public string? CountryCode { get; set; }
        [JsonPropertyName("countryName")] public string? CountryName { get; set; }
        [JsonPropertyName("postalCode")] public string? PostalCode { get; set; }
        [JsonPropertyName("stateOrProvinceCode")] public string? StateOrProvinceCode { get; set; }
        [JsonPropertyName("stateOrProvinceName")] public string? StateOrProvinceName { get; set; }
        [JsonPropertyName("phone")] public string? Phone { get; set; }
        [JsonPropertyName("defaultAddress")] public bool? DefaultAddress { get; set; }
    }

    public sealed class EcwidCustomerContact
    {
        [JsonPropertyName("contact")] public string? Contact { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
    }

    // ---------- Store ----------

    public sealed class EcwidStore
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("storeUrl")] public string? StoreUrl { get; set; }
        [JsonPropertyName("storeName")] public string? StoreName { get; set; }
        [JsonPropertyName("companyName")] public string? CompanyName { get; set; }
        [JsonPropertyName("website")] public string? Website { get; set; }
        [JsonPropertyName("registered")] public DateTime? Registered { get; set; }
        [JsonPropertyName("account")] public EcwidAccount? Account { get; set; }
        [JsonPropertyName("settings")] public EcwidStoreSettings? Settings { get; set; }
        [JsonPropertyName("subscription")] public EcwidSubscription? Subscription { get; set; }
        [JsonPropertyName("billing")] public EcwidBilling? Billing { get; set; }
    }

    public sealed class EcwidAccount
    {
        [JsonPropertyName("accountName")] public string? AccountName { get; set; }
        [JsonPropertyName("accountNickName")] public string? AccountNickName { get; set; }
        [JsonPropertyName("availableFeatures")] public List<string>? AvailableFeatures { get; set; } = new();
    }

    public sealed class EcwidStoreSettings
    {
        [JsonPropertyName("closed")] public bool? Closed { get; set; }
        [JsonPropertyName("storeClosedMessage")] public string? StoreClosedMessage { get; set; }
        [JsonPropertyName("hideOutOfStockProductsInStorefront")] public bool? HideOutOfStockProductsInStorefront { get; set; }
        [JsonPropertyName("useVolumeDiscounts")] public bool? UseVolumeDiscounts { get; set; }
        [JsonPropertyName("useCustomerGroups")] public bool? UseCustomerGroups { get; set; }
        [JsonPropertyName("defaultProductSortOrder")] public string? DefaultProductSortOrder { get; set; }
        [JsonPropertyName("abandonedSales")] public EcwidAbandonedSalesSettings? AbandonedSales { get; set; }
    }

    public sealed class EcwidAbandonedSalesSettings
    {
        [JsonPropertyName("autoAbandonedSalesRecovery")] public bool? AutoAbandonedSalesRecovery { get; set; }
    }

    public sealed class EcwidSubscription
    {
        [JsonPropertyName("subscriptionName")] public string? SubscriptionName { get; set; }
        [JsonPropertyName("subscriptionId")] public long? SubscriptionId { get; set; }
        [JsonPropertyName("features")] public List<string>? Features { get; set; } = new();
    }

    public sealed class EcwidBilling
    {
        [JsonPropertyName("currency")] public string? Currency { get; set; }
        [JsonPropertyName("currencySymbol")] public string? CurrencySymbol { get; set; }
        [JsonPropertyName("currencyPrefix")] public string? CurrencyPrefix { get; set; }
        [JsonPropertyName("currencySuffix")] public string? CurrencySuffix { get; set; }
        [JsonPropertyName("currencyGroupSeparator")] public string? CurrencyGroupSeparator { get; set; }
        [JsonPropertyName("currencyDecimalSeparator")] public string? CurrencyDecimalSeparator { get; set; }
        [JsonPropertyName("currencyTruncateZeroFractional")] public bool? CurrencyTruncateZeroFractional { get; set; }
        [JsonPropertyName("currencyRate")] public decimal? CurrencyRate { get; set; }
    }

    public sealed class EcwidStoreStats
    {
        [JsonPropertyName("productCount")] public int? ProductCount { get; set; }
        [JsonPropertyName("categoryCount")] public int? CategoryCount { get; set; }
        [JsonPropertyName("orderCount")] public int? OrderCount { get; set; }
        [JsonPropertyName("customerCount")] public int? CustomerCount { get; set; }
        [JsonPropertyName("abandonedSalesCount")] public int? AbandonedSalesCount { get; set; }
        [JsonPropertyName("revenue")] public decimal? Revenue { get; set; }
        [JsonPropertyName("ordersValue")] public decimal? OrdersValue { get; set; }
        [JsonPropertyName("createTimestamp")] public long? CreateTimestamp { get; set; }
        [JsonPropertyName("updateTimestamp")] public long? UpdateTimestamp { get; set; }
    }

    // ---------- Types & Classes ----------

    public sealed class EcwidProductType
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("googleTaxonomy")] public string? GoogleTaxonomy { get; set; }
    }

    public sealed class EcwidProductClass
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("attributes")] public List<EcwidProductClassAttribute>? Attributes { get; set; } = new();
    }

    public sealed class EcwidProductClassAttribute
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("show")] public string? Show { get; set; }
    }

    // ---------- Marketing ----------

    public sealed class EcwidCoupon
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("discountType")] public string? DiscountType { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("discount")] public decimal? Discount { get; set; }
        [JsonPropertyName("launchDate")] public DateTime? LaunchDate { get; set; }
        [JsonPropertyName("expirationDate")] public DateTime? ExpirationDate { get; set; }
        [JsonPropertyName("totalLimit")] public int? TotalLimit { get; set; }
        [JsonPropertyName("usesLimit")] public string? UsesLimit { get; set; }
        [JsonPropertyName("applicationLimit")] public string? ApplicationLimit { get; set; }
        [JsonPropertyName("creationDate")] public DateTime? CreationDate { get; set; }
        [JsonPropertyName("updateDate")] public DateTime? UpdateDate { get; set; }
        [JsonPropertyName("orderCount")] public int? OrderCount { get; set; }
        [JsonPropertyName("catalogLimit")] public EcwidCouponCatalogLimit? CatalogLimit { get; set; }
    }

    public sealed class EcwidCouponCatalogLimit
    {
        [JsonPropertyName("products")] public List<long>? Products { get; set; } = new();
        [JsonPropertyName("categories")] public List<long>? Categories { get; set; } = new();
    }

    public sealed class EcwidFavorite
    {
        [JsonPropertyName("productId")] public long? ProductId { get; set; }
        [JsonPropertyName("added")] public DateTime? Added { get; set; }
        [JsonPropertyName("customerId")] public long? CustomerId { get; set; }
    }

    public sealed class EcwidAbandonedCart
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("customerId")] public long? CustomerId { get; set; }
        [JsonPropertyName("createDate")] public DateTime? CreateDate { get; set; }
        [JsonPropertyName("updateDate")] public DateTime? UpdateDate { get; set; }
        [JsonPropertyName("cartUrl")] public string? CartUrl { get; set; }
        [JsonPropertyName("ipAddress")] public string? IpAddress { get; set; }
        [JsonPropertyName("total")] public decimal? Total { get; set; }
        [JsonPropertyName("currency")] public string? Currency { get; set; }
        [JsonPropertyName("couponCode")] public string? CouponCode { get; set; }
        [JsonPropertyName("items")] public List<EcwidAbandonedCartItem>? Items { get; set; } = new();
    }

    public sealed class EcwidAbandonedCartItem
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("productId")] public long? ProductId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
        [JsonPropertyName("weight")] public decimal? Weight { get; set; }
        [JsonPropertyName("imageUrl")] public string? ImageUrl { get; set; }
        [JsonPropertyName("selectedOptions")] public List<EcwidOrderItemOption>? SelectedOptions { get; set; } = new();
    }

    public sealed class EcwidProductReview
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("productId")] public long? ProductId { get; set; }
        [JsonPropertyName("dateCreated")] public DateTime? DateCreated { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("reviewerName")] public string? ReviewerName { get; set; }
        [JsonPropertyName("reviewerEmail")] public string? ReviewerEmail { get; set; }
        [JsonPropertyName("review")] public string? Review { get; set; }
        [JsonPropertyName("rating")] public int? Rating { get; set; }
        [JsonPropertyName("verified")] public bool? Verified { get; set; }
    }

    // ---------- Settings ----------

    public sealed class EcwidShippingMethod
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("orderBy")] public int? OrderBy { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("carrierName")] public string? CarrierName { get; set; }
        [JsonPropertyName("ratesTable")] public List<EcwidShippingRate>? RatesTable { get; set; } = new();
    }

    public sealed class EcwidShippingRate
    {
        [JsonPropertyName("fromWeight")] public decimal? FromWeight { get; set; }
        [JsonPropertyName("toWeight")] public decimal? ToWeight { get; set; }
        [JsonPropertyName("rate")] public decimal? Rate { get; set; }
    }

    public sealed class EcwidTax
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("includeInPrice")] public bool? IncludeInPrice { get; set; }
        [JsonPropertyName("useShippingAddress")] public bool? UseShippingAddress { get; set; }
        [JsonPropertyName("taxShipping")] public bool? TaxShipping { get; set; }
        [JsonPropertyName("appliedByDefault")] public bool? AppliedByDefault { get; set; }
        [JsonPropertyName("rules")] public List<EcwidTaxRule>? Rules { get; set; } = new();
    }

    public sealed class EcwidPaymentMethod
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("checkoutType")] public string? CheckoutType { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("orderBy")] public int? OrderBy { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("instructionsForCustomer")] public string? InstructionsForCustomer { get; set; }
        [JsonPropertyName("configurePaymentIcon")] public bool? ConfigurePaymentIcon { get; set; }
        [JsonPropertyName("iconUrl")] public string? IconUrl { get; set; }
    }

    // ---------- Inventory ----------

    public sealed class EcwidProductInventory
    {
        [JsonPropertyName("productId")] public long? ProductId { get; set; }
        [JsonPropertyName("inStock")] public bool? InStock { get; set; }
        [JsonPropertyName("stock")] public int? Stock { get; set; }
        [JsonPropertyName("reserves")] public int? Reserves { get; set; }
        [JsonPropertyName("warningLimit")] public int? WarningLimit { get; set; }
        [JsonPropertyName("options")] public List<EcwidInventoryOption>? Options { get; set; } = new();
    }

    public sealed class EcwidInventoryOption
    {
        [JsonPropertyName("combinationNumber")] public int? CombinationNumber { get; set; }
        [JsonPropertyName("options")] public List<EcwidVariationOption>? Options { get; set; } = new();
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("trackQuantity")] public bool? TrackQuantity { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("unlimited")] public bool? Unlimited { get; set; }
        [JsonPropertyName("warningLimit")] public int? WarningLimit { get; set; }
        [JsonPropertyName("inStock")] public bool? InStock { get; set; }
    }
}