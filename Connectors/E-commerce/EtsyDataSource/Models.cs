// File: BeepDM/Connectors/Ecommerce/EtsyDataSource/Models/EtsyModels.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Ecommerce.EtsyDataSource.Models
{
    // Base
    public abstract class EtsyEntityBase
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("create_timestamp")] public long? CreateTimestamp { get; set; }
        [JsonPropertyName("created_timestamp")] public long? CreatedTimestamp { get; set; }
        [JsonPropertyName("update_timestamp")] public long? UpdateTimestamp { get; set; }

        [JsonIgnore] public IDataSource? DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : EtsyEntityBase { DataSource = ds; return (T)this; }
    }

    // ---------- Shop Management ----------

    public sealed class EtsyShop : EtsyEntityBase
    {
        [JsonPropertyName("shop_id")] public long? ShopId { get; set; }
        [JsonPropertyName("shop_name")] public string? ShopName { get; set; }
        [JsonPropertyName("user_id")] public long? UserId { get; set; }
        [JsonPropertyName("create_date")] public long? CreateDate { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("announcement")] public string? Announcement { get; set; }
        [JsonPropertyName("currency_code")] public string? CurrencyCode { get; set; }
        [JsonPropertyName("is_vacation")] public bool? IsVacation { get; set; }
        [JsonPropertyName("vacation_message")] public string? VacationMessage { get; set; }
        [JsonPropertyName("sale_message")] public string? SaleMessage { get; set; }
        [JsonPropertyName("digital_sale_message")] public string? DigitalSaleMessage { get; set; }
        [JsonPropertyName("update_date")] public long? UpdateDate { get; set; }
        [JsonPropertyName("listing_active_count")] public int? ListingActiveCount { get; set; }
        [JsonPropertyName("digital_listing_count")] public int? DigitalListingCount { get; set; }
        [JsonPropertyName("login_name")] public string? LoginName { get; set; }
        [JsonPropertyName("accepts_custom_requests")] public bool? AcceptsCustomRequests { get; set; }
        [JsonPropertyName("policy_welcome")] public string? PolicyWelcome { get; set; }
        [JsonPropertyName("policy_payment")] public string? PolicyPayment { get; set; }
        [JsonPropertyName("policy_shipping")] public string? PolicyShipping { get; set; }
        [JsonPropertyName("policy_refunds")] public string? PolicyRefunds { get; set; }
        [JsonPropertyName("policy_additional")] public string? PolicyAdditional { get; set; }
        [JsonPropertyName("policy_seller_info")] public string? PolicySellerInfo { get; set; }
        [JsonPropertyName("policy_updated_date")] public long? PolicyUpdatedDate { get; set; }
        [JsonPropertyName("policy_has_private_receipt_info")] public bool? PolicyHasPrivateReceiptInfo { get; set; }
        [JsonPropertyName("has_unstructured_policies")] public bool? HasUnstructuredPolicies { get; set; }
        [JsonPropertyName("policy_privacy")] public string? PolicyPrivacy { get; set; }
        [JsonPropertyName("vacation_autoreply")] public string? VacationAutoreply { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("image_url_760x100")] public string? ImageUrl760x100 { get; set; }
        [JsonPropertyName("num_favorers")] public int? NumFavorers { get; set; }
        [JsonPropertyName("languages")] public List<string>? Languages { get; set; } = new();
        [JsonPropertyName("icon_url_fullxfull")] public string? IconUrlFullxfull { get; set; }
        [JsonPropertyName("is_using_structured_policies")] public bool? IsUsingStructuredPolicies { get; set; }
        [JsonPropertyName("has_onboarded_structured_policies")] public bool? HasOnboardedStructuredPolicies { get; set; }
        [JsonPropertyName("include_dispute_form_link")] public bool? IncludeDisputeFormLink { get; set; }
        [JsonPropertyName("is_etsy_payments_onboarded")] public bool? IsEtsyPaymentsOnboarded { get; set; }
        [JsonPropertyName("is_calculated_eligible")] public bool? IsCalculatedEligible { get; set; }
        [JsonPropertyName("is_opted_in_to_buyer_promise")] public bool? IsOptedInToBuyerPromise { get; set; }
        [JsonPropertyName("is_shop_us_based")] public bool? IsShopUsBased { get; set; }
        [JsonPropertyName("transaction_sold_count")] public int? TransactionSoldCount { get; set; }
        [JsonPropertyName("shipping_from_country_iso")] public string? ShippingFromCountryIso { get; set; }
        [JsonPropertyName("shop_location_country_iso")] public string? ShopLocationCountryIso { get; set; }
    }

    public sealed class EtsyShopSection : EtsyEntityBase
    {
        [JsonPropertyName("shop_section_id")] public long? ShopSectionId { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("shop_id")] public long? ShopId { get; set; }
        [JsonPropertyName("rank")] public int? Rank { get; set; }
        [JsonPropertyName("user_id")] public long? UserId { get; set; }
        [JsonPropertyName("active_listing_count")] public int? ActiveListingCount { get; set; }
    }

    // ---------- Listings ----------

    public sealed class EtsyListing : EtsyEntityBase
    {
        [JsonPropertyName("listing_id")] public long? ListingId { get; set; }
        [JsonPropertyName("user_id")] public long? UserId { get; set; }
        [JsonPropertyName("shop_id")] public long? ShopId { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("state")] public string? State { get; set; }
        [JsonPropertyName("creation_timestamp")] public long? CreationTimestamp { get; set; }
        [JsonPropertyName("created_timestamp")] public long? CreatedTimestamp { get; set; }
        [JsonPropertyName("ending_timestamp")] public long? EndingTimestamp { get; set; }
        [JsonPropertyName("original_creation_timestamp")] public long? OriginalCreationTimestamp { get; set; }
        [JsonPropertyName("last_modified_timestamp")] public long? LastModifiedTimestamp { get; set; }
        [JsonPropertyName("updated_timestamp")] public long? UpdatedTimestamp { get; set; }
        [JsonPropertyName("state_timestamp")] public long? StateTimestamp { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("shop_section_id")] public long? ShopSectionId { get; set; }
        [JsonPropertyName("featured_rank")] public int? FeaturedRank { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("num_favorers")] public int? NumFavorers { get; set; }
        [JsonPropertyName("non_taxable")] public bool? NonTaxable { get; set; }
        [JsonPropertyName("is_customizable")] public bool? IsCustomizable { get; set; }
        [JsonPropertyName("is_personalizable")] public bool? IsPersonalizable { get; set; }
        [JsonPropertyName("personalization_is_required")] public bool? PersonalizationIsRequired { get; set; }
        [JsonPropertyName("personalization_char_count_max")] public int? PersonalizationCharCountMax { get; set; }
        [JsonPropertyName("personalization_instructions")] public string? PersonalizationInstructions { get; set; }
        [JsonPropertyName("listing_type")] public string? ListingType { get; set; }
        [JsonPropertyName("tags")] public List<string>? Tags { get; set; } = new();
        [JsonPropertyName("materials")] public List<string>? Materials { get; set; } = new();
        [JsonPropertyName("shop_section_id")] public long? ShopSectionId2 { get; set; }
        [JsonPropertyName("featured_rank")] public int? FeaturedRank2 { get; set; }
        [JsonPropertyName("state_timestamp")] public long? StateTimestamp2 { get; set; }
        [JsonPropertyName("url")] public string? Url2 { get; set; }
        [JsonPropertyName("views")] public int? Views { get; set; }
        [JsonPropertyName("num_favorers")] public int? NumFavorers2 { get; set; }
        [JsonPropertyName("shipping_template_id")] public long? ShippingTemplateId { get; set; }
        [JsonPropertyName("payment_template_id")] public long? PaymentTemplateId { get; set; }
        [JsonPropertyName("processing_min")] public int? ProcessingMin { get; set; }
        [JsonPropertyName("processing_max")] public int? ProcessingMax { get; set; }
        [JsonPropertyName("who_made")] public string? WhoMade { get; set; }
        [JsonPropertyName("when_made")] public string? WhenMade { get; set; }
        [JsonPropertyName("is_supply")] public bool? IsSupply { get; set; }
        [JsonPropertyName("item_weight")] public double? ItemWeight { get; set; }
        [JsonPropertyName("item_weight_unit")] public string? ItemWeightUnit { get; set; }
        [JsonPropertyName("item_length")] public double? ItemLength { get; set; }
        [JsonPropertyName("item_width")] public double? ItemWidth { get; set; }
        [JsonPropertyName("item_height")] public double? ItemHeight { get; set; }
        [JsonPropertyName("item_dimensions_unit")] public string? ItemDimensionsUnit { get; set; }
        [JsonPropertyName("is_private")] public bool? IsPrivate { get; set; }
        [JsonPropertyName("style")] public List<string>? Style { get; set; } = new();
        [JsonPropertyName("file_data")] public string? FileData { get; set; }
        [JsonPropertyName("has_variations")] public bool? HasVariations { get; set; }
        [JsonPropertyName("should_auto_renew")] public bool? ShouldAutoRenew { get; set; }
        [JsonPropertyName("language")] public string? Language { get; set; }
        [JsonPropertyName("price")] public EtsyPrice? Price { get; set; }
        [JsonPropertyName("taxonomy_id")] public long? TaxonomyId { get; set; }
        [JsonPropertyName("production_partners")] public List<EtsyProductionPartner>? ProductionPartners { get; set; } = new();
        [JsonPropertyName("skus")] public List<string>? Skus { get; set; } = new();
        [JsonPropertyName("translations")] public Dictionary<string, EtsyListingTranslation>? Translations { get; set; } = new();
        [JsonPropertyName("videos")] public List<EtsyListingVideo>? Videos { get; set; } = new();
        [JsonPropertyName("inventory")] public EtsyListingInventory? Inventory { get; set; }
        [JsonPropertyName("user")] public EtsyUser? User { get; set; }
        [JsonPropertyName("shop")] public EtsyShop? Shop { get; set; }
        [JsonPropertyName("images")] public List<EtsyListingImage>? Images { get; set; } = new();
        [JsonPropertyName("shipping_profile")] public EtsyShippingProfile? ShippingProfile { get; set; }
        [JsonPropertyName("return_policy")] public EtsyReturnPolicy? ReturnPolicy { get; set; }
        [JsonPropertyName("buyer_promise")] public EtsyBuyerPromise? BuyerPromise { get; set; }
    }

    public sealed class EtsyListingImage : EtsyEntityBase
    {
        [JsonPropertyName("listing_image_id")] public long? ListingImageId { get; set; }
        [JsonPropertyName("listing_id")] public long? ListingId { get; set; }
        [JsonPropertyName("url_75x75")] public string? Url75x75 { get; set; }
        [JsonPropertyName("url_170x135")] public string? Url170x135 { get; set; }
        [JsonPropertyName("url_570xN")] public string? Url570xN { get; set; }
        [JsonPropertyName("url_fullxfull")] public string? UrlFullxfull { get; set; }
        [JsonPropertyName("full_height")] public int? FullHeight { get; set; }
        [JsonPropertyName("full_width")] public int? FullWidth { get; set; }
        [JsonPropertyName("rank")] public int? Rank { get; set; }
        [JsonPropertyName("alt_text")] public string? AltText { get; set; }
        [JsonPropertyName("hex_code")] public string? HexCode { get; set; }
        [JsonPropertyName("red")] public int? Red { get; set; }
        [JsonPropertyName("green")] public int? Green { get; set; }
        [JsonPropertyName("blue")] public int? Blue { get; set; }
        [JsonPropertyName("hue")] public int? Hue { get; set; }
        [JsonPropertyName("saturation")] public int? Saturation { get; set; }
        [JsonPropertyName("brightness")] public int? Brightness { get; set; }
        [JsonPropertyName("is_black_and_white")] public bool? IsBlackAndWhite { get; set; }
        [JsonPropertyName("creation_tsz")] public double? CreationTsz { get; set; }
        [JsonPropertyName("listing_id")] public long? ListingId2 { get; set; }
    }

    public sealed class EtsyListingInventory : EtsyEntityBase
    {
        [JsonPropertyName("products")] public List<EtsyListingProduct>? Products { get; set; } = new();
        [JsonPropertyName("price_on_property")] public List<long>? PriceOnProperty { get; set; } = new();
        [JsonPropertyName("quantity_on_property")] public List<long>? QuantityOnProperty { get; set; } = new();
        [JsonPropertyName("sku_on_property")] public List<long>? SkuOnProperty { get; set; } = new();
    }

    public sealed class EtsyListingProduct : EtsyEntityBase
    {
        [JsonPropertyName("product_id")] public long? ProductId { get; set; }
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("is_deleted")] public bool? IsDeleted { get; set; }
        [JsonPropertyName("offerings")] public List<EtsyProductOffering>? Offerings { get; set; } = new();
        [JsonPropertyName("property_values")] public List<EtsyPropertyValue>? PropertyValues { get; set; } = new();
    }

    public sealed class EtsyProductOffering : EtsyEntityBase
    {
        [JsonPropertyName("offering_id")] public long? OfferingId { get; set; }
        [JsonPropertyName("price")] public EtsyPrice? Price { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("is_enabled")] public bool? IsEnabled { get; set; }
        [JsonPropertyName("is_deleted")] public bool? IsDeleted { get; set; }
    }

    // ---------- Orders & Transactions ----------

    public sealed class EtsyReceipt : EtsyEntityBase
    {
        [JsonPropertyName("receipt_id")] public long? ReceiptId { get; set; }
        [JsonPropertyName("receipt_type")] public int? ReceiptType { get; set; }
        [JsonPropertyName("order_id")] public long? OrderId { get; set; }
        [JsonPropertyName("seller_user_id")] public long? SellerUserId { get; set; }
        [JsonPropertyName("buyer_user_id")] public long? BuyerUserId { get; set; }
        [JsonPropertyName("creation_timestamp")] public long? CreationTimestamp { get; set; }
        [JsonPropertyName("created_timestamp")] public long? CreatedTimestamp { get; set; }
        [JsonPropertyName("can_refund")] public bool? CanRefund { get; set; }
        [JsonPropertyName("was_shipped")] public bool? WasShipped { get; set; }
        [JsonPropertyName("seller_email")] public string? SellerEmail { get; set; }
        [JsonPropertyName("is_gift")] public bool? IsGift { get; set; }
        [JsonPropertyName("needs_gift_wrap")] public bool? NeedsGiftWrap { get; set; }
        [JsonPropertyName("gift_wrap_price")] public EtsyPrice? GiftWrapPrice { get; set; }
        [JsonPropertyName("total_tax_cost")] public EtsyPrice? TotalTaxCost { get; set; }
        [JsonPropertyName("total_vat_cost")] public EtsyPrice? TotalVatCost { get; set; }
        [JsonPropertyName("discount_amt")] public EtsyPrice? DiscountAmt { get; set; }
        [JsonPropertyName("subtotal")] public EtsyPrice? Subtotal { get; set; }
        [JsonPropertyName("grandtotal")] public EtsyPrice? Grandtotal { get; set; }
        [JsonPropertyName("adjusted_grandtotal")] public EtsyPrice? AdjustedGrandtotal { get; set; }
        [JsonPropertyName("buyer_adjusted_grandtotal")] public EtsyPrice? BuyerAdjustedGrandtotal { get; set; }
        [JsonPropertyName("shipped_date")] public long? ShippedDate { get; set; }
        [JsonPropertyName("payment_method")] public string? PaymentMethod { get; set; }
        [JsonPropertyName("payment_email")] public string? PaymentEmail { get; set; }
        [JsonPropertyName("sale_date")] public long? SaleDate { get; set; }
        [JsonPropertyName("paid_timestamp")] public long? PaidTimestamp { get; set; }
        [JsonPropertyName("last_modified_timestamp")] public long? LastModifiedTimestamp { get; set; }
        [JsonPropertyName("message_from_seller")] public string? MessageFromSeller { get; set; }
        [JsonPropertyName("message_from_buyer")] public string? MessageFromBuyer { get; set; }
        [JsonPropertyName("was_paid")] public bool? WasPaid { get; set; }
        [JsonPropertyName("total_price")] public EtsyPrice? TotalPrice { get; set; }
        [JsonPropertyName("total_shipping_cost")] public EtsyPrice? TotalShippingCost { get; set; }
        [JsonPropertyName("currency_code")] public string? CurrencyCode { get; set; }
        [JsonPropertyName("message_from_payment_info")] public string? MessageFromPaymentInfo { get; set; }
        [JsonPropertyName("is_overpaid")] public bool? IsOverpaid { get; set; }
        [JsonPropertyName("buyer_coupon")] public EtsyPrice? BuyerCoupon { get; set; }
        [JsonPropertyName("seller_coupon")] public EtsyPrice? SellerCoupon { get; set; }
        [JsonPropertyName("buyer_email")] public string? BuyerEmail { get; set; }
        [JsonPropertyName("seller_user_id")] public long? SellerUserId2 { get; set; }
        [JsonPropertyName("buyer_user_id")] public long? BuyerUserId2 { get; set; }
        [JsonPropertyName("is_family_order")] public bool? IsFamilyOrder { get; set; }
        [JsonPropertyName("transactions")] public List<EtsyTransaction>? Transactions { get; set; } = new();
    }

    public sealed class EtsyTransaction : EtsyEntityBase
    {
        [JsonPropertyName("transaction_id")] public long? TransactionId { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("seller_user_id")] public long? SellerUserId { get; set; }
        [JsonPropertyName("buyer_user_id")] public long? BuyerUserId { get; set; }
        [JsonPropertyName("creation_timestamp")] public long? CreationTimestamp { get; set; }
        [JsonPropertyName("paid_timestamp")] public long? PaidTimestamp { get; set; }
        [JsonPropertyName("shipped_timestamp")] public long? ShippedTimestamp { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("listing_image_id")] public long? ListingImageId { get; set; }
        [JsonPropertyName("receipt_id")] public long? ReceiptId { get; set; }
        [JsonPropertyName("is_digital")] public bool? IsDigital { get; set; }
        [JsonPropertyName("file_data")] public string? FileData { get; set; }
        [JsonPropertyName("listing_id")] public long? ListingId { get; set; }
        [JsonPropertyName("transaction_type")] public string? TransactionType { get; set; }
        [JsonPropertyName("product_data")] public EtsyProductData? ProductData { get; set; }
        [JsonPropertyName("shipping_cost")] public EtsyPrice? ShippingCost { get; set; }
        [JsonPropertyName("variations")] public List<EtsyVariation>? Variations { get; set; } = new();
        [JsonPropertyName("product_id")] public long? ProductId { get; set; }
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("price")] public EtsyPrice? Price { get; set; }
        [JsonPropertyName("shipping_profile_id")] public long? ShippingProfileId { get; set; }
        [JsonPropertyName("min_processing_days")] public int? MinProcessingDays { get; set; }
        [JsonPropertyName("max_processing_days")] public int? MaxProcessingDays { get; set; }
        [JsonPropertyName("shipping_method")] public string? ShippingMethod { get; set; }
        [JsonPropertyName("shipping_upgrade")] public string? ShippingUpgrade { get; set; }
        [JsonPropertyName("expected_ship_date")] public long? ExpectedShipDate { get; set; }
        [JsonPropertyName("buyer_coupon")] public EtsyPrice? BuyerCoupon { get; set; }
        [JsonPropertyName("seller_coupon")] public EtsyPrice? SellerCoupon { get; set; }
    }

    // ---------- User Account ----------

    public sealed class EtsyUser : EtsyEntityBase
    {
        [JsonPropertyName("user_id")] public long? UserId { get; set; }
        [JsonPropertyName("login_name")] public string? LoginName { get; set; }
        [JsonPropertyName("primary_email")] public string? PrimaryEmail { get; set; }
        [JsonPropertyName("creation_timestamp")] public long? CreationTimestamp { get; set; }
        [JsonPropertyName("referred_by_user_id")] public long? ReferredByUserId { get; set; }
        [JsonPropertyName("feedback_info")] public EtsyFeedbackInfo? FeedbackInfo { get; set; }
        [JsonPropertyName("awaiting_feedback_count")] public int? AwaitingFeedbackCount { get; set; }
        [JsonPropertyName("use_new_inventory_endpoints")] public bool? UseNewInventoryEndpoints { get; set; }
    }

    public sealed class EtsyUserAddress : EtsyEntityBase
    {
        [JsonPropertyName("user_address_id")] public long? UserAddressId { get; set; }
        [JsonPropertyName("user_id")] public long? UserId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("first_line")] public string? FirstLine { get; set; }
        [JsonPropertyName("second_line")] public string? SecondLine { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("state")] public string? State { get; set; }
        [JsonPropertyName("zip")] public string? Zip { get; set; }
        [JsonPropertyName("country_id")] public long? CountryId { get; set; }
        [JsonPropertyName("country_name")] public string? CountryName { get; set; }
        [JsonPropertyName("country_iso")] public string? CountryIso { get; set; }
        [JsonPropertyName("is_default_shipping")] public bool? IsDefaultShipping { get; set; }
    }

    // ---------- Supporting Types ----------

    public sealed class EtsyPrice
    {
        [JsonPropertyName("amount")] public int? Amount { get; set; }
        [JsonPropertyName("divisor")] public int? Divisor { get; set; }
        [JsonPropertyName("currency_code")] public string? CurrencyCode { get; set; }
    }

    public sealed class EtsyPropertyValue
    {
        [JsonPropertyName("property_id")] public long? PropertyId { get; set; }
        [JsonPropertyName("property_name")] public string? PropertyName { get; set; }
        [JsonPropertyName("scale_id")] public long? ScaleId { get; set; }
        [JsonPropertyName("scale_name")] public string? ScaleName { get; set; }
        [JsonPropertyName("value_ids")] public List<long>? ValueIds { get; set; } = new();
        [JsonPropertyName("values")] public List<string>? Values { get; set; } = new();
    }

    public sealed class EtsyVariation
    {
        [JsonPropertyName("property_id")] public long? PropertyId { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
        [JsonPropertyName("formatted_value")] public string? FormattedValue { get; set; }
        [JsonPropertyName("scale_id")] public long? ScaleId { get; set; }
        [JsonPropertyName("scale_name")] public string? ScaleName { get; set; }
    }

    public sealed class EtsyProductData
    {
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("property_values")] public List<EtsyPropertyValue>? PropertyValues { get; set; } = new();
    }

    public sealed class EtsyFeedbackInfo
    {
        [JsonPropertyName("count")] public int? Count { get; set; }
        [JsonPropertyName("score")] public double? Score { get; set; }
    }

    public sealed class EtsyProductionPartner
    {
        [JsonPropertyName("production_partner_id")] public long? ProductionPartnerId { get; set; }
        [JsonPropertyName("partner_name")] public string? PartnerName { get; set; }
        [JsonPropertyName("location")] public string? Location { get; set; }
    }

    public sealed class EtsyListingTranslation
    {
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("tags")] public List<string>? Tags { get; set; } = new();
    }

    public sealed class EtsyListingVideo
    {
        [JsonPropertyName("video_id")] public long? VideoId { get; set; }
        [JsonPropertyName("height")] public int? Height { get; set; }
        [JsonPropertyName("width")] public int? Width { get; set; }
        [JsonPropertyName("thumbnail_url")] public string? ThumbnailUrl { get; set; }
        [JsonPropertyName("video_url")] public string? VideoUrl { get; set; }
        [JsonPropertyName("video_state")] public string? VideoState { get; set; }
    }

    public sealed class EtsyShippingProfile
    {
        [JsonPropertyName("shipping_profile_id")] public long? ShippingProfileId { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("user_id")] public long? UserId { get; set; }
        [JsonPropertyName("min_processing_time")] public int? MinProcessingTime { get; set; }
        [JsonPropertyName("max_processing_time")] public int? MaxProcessingTime { get; set; }
        [JsonPropertyName("processing_time_display")] public string? ProcessingTimeDisplay { get; set; }
        [JsonPropertyName("origin_country_iso")] public string? OriginCountryIso { get; set; }
        [JsonPropertyName("is_deleted")] public bool? IsDeleted { get; set; }
        [JsonPropertyName("shipping_profile_destinations")] public List<EtsyShippingProfileDestination>? ShippingProfileDestinations { get; set; } = new();
        [JsonPropertyName("shipping_profile_upgrades")] public List<EtsyShippingProfileUpgrade>? ShippingProfileUpgrades { get; set; } = new();
    }

    public sealed class EtsyShippingProfileDestination
    {
        [JsonPropertyName("shipping_profile_destination_id")] public long? ShippingProfileDestinationId { get; set; }
        [JsonPropertyName("shipping_profile_id")] public long? ShippingProfileId { get; set; }
        [JsonPropertyName("origin_country_iso")] public string? OriginCountryIso { get; set; }
        [JsonPropertyName("destination_country_iso")] public string? DestinationCountryIso { get; set; }
        [JsonPropertyName("destination_region")] public string? DestinationRegion { get; set; }
        [JsonPropertyName("primary_cost")] public EtsyPrice? PrimaryCost { get; set; }
        [JsonPropertyName("secondary_cost")] public EtsyPrice? SecondaryCost { get; set; }
    }

    public sealed class EtsyShippingProfileUpgrade
    {
        [JsonPropertyName("shipping_profile_id")] public long? ShippingProfileId { get; set; }
        [JsonPropertyName("upgrade_id")] public long? UpgradeId { get; set; }
        [JsonPropertyName("upgrade_name")] public string? UpgradeName { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("rank")] public int? Rank { get; set; }
        [JsonPropertyName("language")] public string? Language { get; set; }
        [JsonPropertyName("price")] public EtsyPrice? Price { get; set; }
        [JsonPropertyName("secondary_price")] public EtsyPrice? SecondaryPrice { get; set; }
        [JsonPropertyName("shipping_carrier_id")] public long? ShippingCarrierId { get; set; }
        [JsonPropertyName("mail_class")] public string? MailClass { get; set; }
        [JsonPropertyName("min_delivery_days")] public int? MinDeliveryDays { get; set; }
        [JsonPropertyName("max_delivery_days")] public int? MaxDeliveryDays { get; set; }
    }

    public sealed class EtsyReturnPolicy
    {
        [JsonPropertyName("return_policy_id")] public long? ReturnPolicyId { get; set; }
        [JsonPropertyName("shop_id")] public long? ShopId { get; set; }
        [JsonPropertyName("accepts_returns")] public bool? AcceptsReturns { get; set; }
        [JsonPropertyName("accepts_exchanges")] public bool? AcceptsExchanges { get; set; }
        [JsonPropertyName("return_deadline")] public int? ReturnDeadline { get; set; }
    }

    public sealed class EtsyBuyerPromise
    {
        [JsonPropertyName("buyer_promise_id")] public long? BuyerPromiseId { get; set; }
        [JsonPropertyName("shop_id")] public long? ShopId { get; set; }
        [JsonPropertyName("promise")] public string? Promise { get; set; }
    }
}