// File: BeepDM/Connectors/Ecommerce/BigCommerceDataSource/Models/BigCommerceModels.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Ecommerce.BigCommerceDataSource.Models
{
    // Base
    public abstract class BigCommerceEntityBase
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("date_created")] public DateTimeOffset? DateCreated { get; set; }
        [JsonPropertyName("date_modified")] public DateTimeOffset? DateModified { get; set; }

        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : BigCommerceEntityBase { DataSource = ds; return (T)this; }
    }

    // ---------- Core Commerce objects ----------

    public sealed class BigCommerceProduct : BigCommerceEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
        [JsonPropertyName("cost_price")] public decimal? CostPrice { get; set; }
        [JsonPropertyName("retail_price")] public decimal? RetailPrice { get; set; }
        [JsonPropertyName("sale_price")] public decimal? SalePrice { get; set; }
        [JsonPropertyName("calculated_price")] public decimal? CalculatedPrice { get; set; }
        [JsonPropertyName("weight")] public decimal? Weight { get; set; }
        [JsonPropertyName("width")] public decimal? Width { get; set; }
        [JsonPropertyName("height")] public decimal? Height { get; set; }
        [JsonPropertyName("depth")] public decimal? Depth { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("inventory_level")] public int? InventoryLevel { get; set; }
        [JsonPropertyName("inventory_warning_level")] public int? InventoryWarningLevel { get; set; }
        [JsonPropertyName("inventory_tracking")] public string? InventoryTracking { get; set; }
        [JsonPropertyName("fixed_cost_shipping_price")] public decimal? FixedCostShippingPrice { get; set; }
        [JsonPropertyName("is_free_shipping")] public bool? IsFreeShipping { get; set; }
        [JsonPropertyName("is_visible")] public bool? IsVisible { get; set; }
        [JsonPropertyName("is_featured")] public bool? IsFeatured { get; set; }
        [JsonPropertyName("related_products")] public List<int>? RelatedProducts { get; set; } = new();
        [JsonPropertyName("warranty")] public string? Warranty { get; set; }
        [JsonPropertyName("bin_picking_number")] public string? BinPickingNumber { get; set; }
        [JsonPropertyName("layout_file")] public string? LayoutFile { get; set; }
        [JsonPropertyName("upc")] public string? Upc { get; set; }
        [JsonPropertyName("mpn")] public string? Mpn { get; set; }
        [JsonPropertyName("gtin")] public string? Gtin { get; set; }
        [JsonPropertyName("search_keywords")] public string? SearchKeywords { get; set; }
        [JsonPropertyName("availability")] public string? Availability { get; set; }
        [JsonPropertyName("availability_description")] public string? AvailabilityDescription { get; set; }
        [JsonPropertyName("gift_wrapping_options_type")] public string? GiftWrappingOptionsType { get; set; }
        [JsonPropertyName("gift_wrapping_options_list")] public List<string>? GiftWrappingOptionsList { get; set; } = new();
        [JsonPropertyName("sort_order")] public int? SortOrder { get; set; }
        [JsonPropertyName("condition")] public string? Condition { get; set; }
        [JsonPropertyName("is_condition_shown")] public bool? IsConditionShown { get; set; }
        [JsonPropertyName("order_quantity_minimum")] public int? OrderQuantityMinimum { get; set; }
        [JsonPropertyName("order_quantity_maximum")] public int? OrderQuantityMaximum { get; set; }
        [JsonPropertyName("page_title")] public string? PageTitle { get; set; }
        [JsonPropertyName("meta_keywords")] public List<string>? MetaKeywords { get; set; } = new();
        [JsonPropertyName("meta_description")] public string? MetaDescription { get; set; }
        [JsonPropertyName("view_count")] public int? ViewCount { get; set; }
        [JsonPropertyName("preorder_release_date")] public DateTimeOffset? PreorderReleaseDate { get; set; }
        [JsonPropertyName("preorder_message")] public string? PreorderMessage { get; set; }
        [JsonPropertyName("is_preorder_only")] public bool? IsPreorderOnly { get; set; }
        [JsonPropertyName("is_price_hidden")] public bool? IsPriceHidden { get; set; }
        [JsonPropertyName("price_hidden_label")] public string? PriceHiddenLabel { get; set; }
        [JsonPropertyName("categories")] public List<int>? Categories { get; set; } = new();
        [JsonPropertyName("brand_id")] public int? BrandId { get; set; }
        [JsonPropertyName("tax_class_id")] public int? TaxClassId { get; set; }
        [JsonPropertyName("product_tax_code")] public string? ProductTaxCode { get; set; }
        [JsonPropertyName("images")] public List<BigCommerceProductImage>? Images { get; set; } = new();
        [JsonPropertyName("videos")] public List<BigCommerceProductVideo>? Videos { get; set; } = new();
        [JsonPropertyName("variants")] public List<BigCommerceProductVariant>? Variants { get; set; } = new();
        [JsonPropertyName("custom_fields")] public List<BigCommerceCustomField>? CustomFields { get; set; } = new();
        [JsonPropertyName("bulk_pricing_rules")] public List<BigCommerceBulkPricingRule>? BulkPricingRules { get; set; } = new();
        [JsonPropertyName("option_set_id")] public int? OptionSetId { get; set; }
        [JsonPropertyName("option_set_display")] public string? OptionSetDisplay { get; set; }
        [JsonPropertyName("reviews_rating_sum")] public int? ReviewsRatingSum { get; set; }
        [JsonPropertyName("reviews_count")] public int? ReviewsCount { get; set; }
        [JsonPropertyName("total_sold")] public int? TotalSold { get; set; }
    }

    public sealed class BigCommerceProductImage
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("product_id")] public int? ProductId { get; set; }
        [JsonPropertyName("is_thumbnail")] public bool? IsThumbnail { get; set; }
        [JsonPropertyName("sort_order")] public int? SortOrder { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("image_file")] public string? ImageFile { get; set; }
        [JsonPropertyName("url_zoom")] public string? UrlZoom { get; set; }
        [JsonPropertyName("url_standard")] public string? UrlStandard { get; set; }
        [JsonPropertyName("url_thumbnail")] public string? UrlThumbnail { get; set; }
        [JsonPropertyName("url_tiny")] public string? UrlTiny { get; set; }
        [JsonPropertyName("date_modified")] public DateTimeOffset? DateModified { get; set; }
    }

    public sealed class BigCommerceProductVideo
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("product_id")] public int? ProductId { get; set; }
        [JsonPropertyName("sort_order")] public int? SortOrder { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("length")] public string? Length { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("video_id")] public string? VideoId { get; set; }
    }

    public sealed class BigCommerceProductVariant
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("product_id")] public int? ProductId { get; set; }
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("sku_id")] public int? SkuId { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
        [JsonPropertyName("calculated_price")] public decimal? CalculatedPrice { get; set; }
        [JsonPropertyName("sale_price")] public decimal? SalePrice { get; set; }
        [JsonPropertyName("retail_price")] public decimal? RetailPrice { get; set; }
        [JsonPropertyName("weight")] public decimal? Weight { get; set; }
        [JsonPropertyName("width")] public decimal? Width { get; set; }
        [JsonPropertyName("height")] public decimal? Height { get; set; }
        [JsonPropertyName("depth")] public decimal? Depth { get; set; }
        [JsonPropertyName("is_free_shipping")] public bool? IsFreeShipping { get; set; }
        [JsonPropertyName("fixed_cost_shipping_price")] public decimal? FixedCostShippingPrice { get; set; }
        [JsonPropertyName("purchasing_disabled")] public bool? PurchasingDisabled { get; set; }
        [JsonPropertyName("purchasing_disabled_message")] public string? PurchasingDisabledMessage { get; set; }
        [JsonPropertyName("image_url")] public string? ImageUrl { get; set; }
        [JsonPropertyName("cost_price")] public decimal? CostPrice { get; set; }
        [JsonPropertyName("upc")] public string? Upc { get; set; }
        [JsonPropertyName("mpn")] public string? Mpn { get; set; }
        [JsonPropertyName("gtin")] public string? Gtin { get; set; }
        [JsonPropertyName("inventory_level")] public int? InventoryLevel { get; set; }
        [JsonPropertyName("inventory_warning_level")] public int? InventoryWarningLevel { get; set; }
        [JsonPropertyName("bin_picking_number")] public string? BinPickingNumber { get; set; }
        [JsonPropertyName("option_values")] public List<BigCommerceVariantOptionValue>? OptionValues { get; set; } = new();
    }

    public sealed class BigCommerceVariantOptionValue
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("label")] public string? Label { get; set; }
        [JsonPropertyName("option_id")] public int? OptionId { get; set; }
        [JsonPropertyName("option_display_name")] public string? OptionDisplayName { get; set; }
    }

    public sealed class BigCommerceCustomField
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
    }

    public sealed class BigCommerceBulkPricingRule
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("quantity_min")] public int? QuantityMin { get; set; }
        [JsonPropertyName("quantity_max")] public int? QuantityMax { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("amount")] public decimal? Amount { get; set; }
    }

    public sealed class BigCommerceCategory : BigCommerceEntityBase
    {
        [JsonPropertyName("parent_id")] public int? ParentId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("views")] public int? Views { get; set; }
        [JsonPropertyName("sort_order")] public int? SortOrder { get; set; }
        [JsonPropertyName("page_title")] public string? PageTitle { get; set; }
        [JsonPropertyName("meta_keywords")] public List<string>? MetaKeywords { get; set; } = new();
        [JsonPropertyName("meta_description")] public string? MetaDescription { get; set; }
        [JsonPropertyName("layout_file")] public string? LayoutFile { get; set; }
        [JsonPropertyName("image_url")] public string? ImageUrl { get; set; }
        [JsonPropertyName("is_visible")] public bool? IsVisible { get; set; }
        [JsonPropertyName("search_keywords")] public string? SearchKeywords { get; set; }
        [JsonPropertyName("default_product_sort")] public string? DefaultProductSort { get; set; }
        [JsonPropertyName("custom_url")] public BigCommerceCustomUrl? CustomUrl { get; set; }
    }

    public sealed class BigCommerceCustomUrl
    {
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("is_customized")] public bool? IsCustomized { get; set; }
    }

    public sealed class BigCommerceBrand : BigCommerceEntityBase
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("meta_keywords")] public List<string>? MetaKeywords { get; set; } = new();
        [JsonPropertyName("meta_description")] public string? MetaDescription { get; set; }
        [JsonPropertyName("image_url")] public string? ImageUrl { get; set; }
        [JsonPropertyName("search_keywords")] public string? SearchKeywords { get; set; }
        [JsonPropertyName("custom_url")] public BigCommerceCustomUrl? CustomUrl { get; set; }
    }

    public sealed class BigCommerceCustomer : BigCommerceEntityBase
    {
        [JsonPropertyName("company")] public string? Company { get; set; }
        [JsonPropertyName("first_name")] public string? FirstName { get; set; }
        [JsonPropertyName("last_name")] public string? LastName { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("phone")] public string? Phone { get; set; }
        [JsonPropertyName("registration_ip_address")] public string? RegistrationIpAddress { get; set; }
        [JsonPropertyName("customer_group_id")] public int? CustomerGroupId { get; set; }
        [JsonPropertyName("notes")] public string? Notes { get; set; }
        [JsonPropertyName("tax_exempt_category")] public string? TaxExemptCategory { get; set; }
        [JsonPropertyName("reset_pass_on_login")] public bool? ResetPassOnLogin { get; set; }
        [JsonPropertyName("accepts_product_review_abandoned_cart_emails")] public bool? AcceptsProductReviewAbandonedCartEmails { get; set; }
        [JsonPropertyName("store_credit_amounts")] public List<BigCommerceStoreCreditAmount>? StoreCreditAmounts { get; set; } = new();
        [JsonPropertyName("addresses")] public List<BigCommerceCustomerAddress>? Addresses { get; set; } = new();
        [JsonPropertyName("attributes")] public List<BigCommerceCustomerAttribute>? Attributes { get; set; } = new();
        [JsonPropertyName("authentication")] public BigCommerceCustomerAuthentication? Authentication { get; set; }
        [JsonPropertyName("form_fields")] public List<BigCommerceFormFieldValue>? FormFields { get; set; } = new();
    }

    public sealed class BigCommerceStoreCreditAmount
    {
        [JsonPropertyName("amount")] public decimal? Amount { get; set; }
    }

    public sealed class BigCommerceCustomerAddress
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("customer_id")] public int? CustomerId { get; set; }
        [JsonPropertyName("first_name")] public string? FirstName { get; set; }
        [JsonPropertyName("last_name")] public string? LastName { get; set; }
        [JsonPropertyName("company")] public string? Company { get; set; }
        [JsonPropertyName("address1")] public string? Address1 { get; set; }
        [JsonPropertyName("address2")] public string? Address2 { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("state_or_province")] public string? StateOrProvince { get; set; }
        [JsonPropertyName("postal_code")] public string? PostalCode { get; set; }
        [JsonPropertyName("country_code")] public string? CountryCode { get; set; }
        [JsonPropertyName("phone")] public string? Phone { get; set; }
        [JsonPropertyName("address_type")] public string? AddressType { get; set; }
    }

    public sealed class BigCommerceCustomerAttribute
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("customer_id")] public int? CustomerId { get; set; }
        [JsonPropertyName("attribute_id")] public int? AttributeId { get; set; }
        [JsonPropertyName("attribute_value")] public string? AttributeValue { get; set; }
        [JsonPropertyName("date_created")] public DateTimeOffset? DateCreated { get; set; }
        [JsonPropertyName("date_modified")] public DateTimeOffset? DateModified { get; set; }
    }

    public sealed class BigCommerceCustomerAuthentication
    {
        [JsonPropertyName("force_password_reset")] public bool? ForcePasswordReset { get; set; }
    }

    public sealed class BigCommerceFormFieldValue
    {
        [JsonPropertyName("customer_id")] public int? CustomerId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
    }

    public sealed class BigCommerceOrder
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("customer_id")] public int? CustomerId { get; set; }
        [JsonPropertyName("date_created")] public DateTimeOffset? DateCreated { get; set; }
        [JsonPropertyName("date_modified")] public DateTimeOffset? DateModified { get; set; }
        [JsonPropertyName("date_shipped")] public DateTimeOffset? DateShipped { get; set; }
        [JsonPropertyName("status_id")] public int? StatusId { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("subtotal_ex_tax")] public decimal? SubtotalExTax { get; set; }
        [JsonPropertyName("subtotal_inc_tax")] public decimal? SubtotalIncTax { get; set; }
        [JsonPropertyName("subtotal_tax")] public decimal? SubtotalTax { get; set; }
        [JsonPropertyName("base_shipping_cost")] public decimal? BaseShippingCost { get; set; }
        [JsonPropertyName("shipping_cost_ex_tax")] public decimal? ShippingCostExTax { get; set; }
        [JsonPropertyName("shipping_cost_inc_tax")] public decimal? ShippingCostIncTax { get; set; }
        [JsonPropertyName("shipping_cost_tax")] public decimal? ShippingCostTax { get; set; }
        [JsonPropertyName("shipping_cost_tax_class_id")] public int? ShippingCostTaxClassId { get; set; }
        [JsonPropertyName("base_handling_cost")] public decimal? BaseHandlingCost { get; set; }
        [JsonPropertyName("handling_cost_ex_tax")] public decimal? HandlingCostExTax { get; set; }
        [JsonPropertyName("handling_cost_inc_tax")] public decimal? HandlingCostIncTax { get; set; }
        [JsonPropertyName("handling_cost_tax")] public decimal? HandlingCostTax { get; set; }
        [JsonPropertyName("handling_cost_tax_class_id")] public int? HandlingCostTaxClassId { get; set; }
        [JsonPropertyName("base_wrapping_cost")] public decimal? BaseWrappingCost { get; set; }
        [JsonPropertyName("wrapping_cost_ex_tax")] public decimal? WrappingCostExTax { get; set; }
        [JsonPropertyName("wrapping_cost_inc_tax")] public decimal? WrappingCostIncTax { get; set; }
        [JsonPropertyName("wrapping_cost_tax")] public decimal? WrappingCostTax { get; set; }
        [JsonPropertyName("wrapping_cost_tax_class_id")] public int? WrappingCostTaxClassId { get; set; }
        [JsonPropertyName("total_ex_tax")] public decimal? TotalExTax { get; set; }
        [JsonPropertyName("total_inc_tax")] public decimal? TotalIncTax { get; set; }
        [JsonPropertyName("total_tax")] public decimal? TotalTax { get; set; }
        [JsonPropertyName("items_total")] public int? ItemsTotal { get; set; }
        [JsonPropertyName("items_shipped")] public int? ItemsShipped { get; set; }
        [JsonPropertyName("payment_method")] public string? PaymentMethod { get; set; }
        [JsonPropertyName("payment_provider_id")] public int? PaymentProviderId { get; set; }
        [JsonPropertyName("payment_status")] public string? PaymentStatus { get; set; }
        [JsonPropertyName("refunded_amount")] public decimal? RefundedAmount { get; set; }
        [JsonPropertyName("order_is_digital")] public bool? OrderIsDigital { get; set; }
        [JsonPropertyName("store_credit_amount")] public decimal? StoreCreditAmount { get; set; }
        [JsonPropertyName("gift_certificate_amount")] public decimal? GiftCertificateAmount { get; set; }
        [JsonPropertyName("ip_address")] public string? IpAddress { get; set; }
        [JsonPropertyName("geoip_country")] public string? GeoipCountry { get; set; }
        [JsonPropertyName("geoip_country_iso2")] public string? GeoipCountryIso2 { get; set; }
        [JsonPropertyName("currency_id")] public int? CurrencyId { get; set; }
        [JsonPropertyName("currency_code")] public string? CurrencyCode { get; set; }
        [JsonPropertyName("currency_exchange_rate")] public decimal? CurrencyExchangeRate { get; set; }
        [JsonPropertyName("default_currency_id")] public int? DefaultCurrencyId { get; set; }
        [JsonPropertyName("default_currency_code")] public string? DefaultCurrencyCode { get; set; }
        [JsonPropertyName("staff_notes")] public string? StaffNotes { get; set; }
        [JsonPropertyName("customer_message")] public string? CustomerMessage { get; set; }
        [JsonPropertyName("discount_amount")] public decimal? DiscountAmount { get; set; }
        [JsonPropertyName("coupon_discount")] public decimal? CouponDiscount { get; set; }
        [JsonPropertyName("shipping_address_count")] public int? ShippingAddressCount { get; set; }
        [JsonPropertyName("is_email_opt_in")] public bool? IsEmailOptIn { get; set; }
        [JsonPropertyName("credit_card_type")] public int? CreditCardType { get; set; }
        [JsonPropertyName("order_source")] public string? OrderSource { get; set; }
        [JsonPropertyName("channel_id")] public int? ChannelId { get; set; }
        [JsonPropertyName("external_source")] public string? ExternalSource { get; set; }
        [JsonPropertyName("products")] public List<BigCommerceOrderProduct>? Products { get; set; } = new();
        [JsonPropertyName("shipping_addresses")] public List<BigCommerceOrderShippingAddress>? ShippingAddresses { get; set; } = new();
        [JsonPropertyName("coupons")] public List<BigCommerceOrderCoupon>? Coupons { get; set; } = new();
        [JsonPropertyName("external_id")] public string? ExternalId { get; set; }
        [JsonPropertyName("external_merchant_id")] public string? ExternalMerchantId { get; set; }
    }

    public sealed class BigCommerceOrderProduct
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("order_id")] public int? OrderId { get; set; }
        [JsonPropertyName("product_id")] public int? ProductId { get; set; }
        [JsonPropertyName("variant_id")] public int? VariantId { get; set; }
        [JsonPropertyName("order_address_id")] public int? OrderAddressId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("name_customer")] public string? NameCustomer { get; set; }
        [JsonPropertyName("name_merchant")] public string? NameMerchant { get; set; }
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("upc")] public string? Upc { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("base_price")] public decimal? BasePrice { get; set; }
        [JsonPropertyName("price_ex_tax")] public decimal? PriceExTax { get; set; }
        [JsonPropertyName("price_inc_tax")] public decimal? PriceIncTax { get; set; }
        [JsonPropertyName("price_tax")] public decimal? PriceTax { get; set; }
        [JsonPropertyName("base_total")] public decimal? BaseTotal { get; set; }
        [JsonPropertyName("total_ex_tax")] public decimal? TotalExTax { get; set; }
        [JsonPropertyName("total_inc_tax")] public decimal? TotalIncTax { get; set; }
        [JsonPropertyName("total_tax")] public decimal? TotalTax { get; set; }
        [JsonPropertyName("weight")] public decimal? Weight { get; set; }
        [JsonPropertyName("width")] public decimal? Width { get; set; }
        [JsonPropertyName("height")] public decimal? Height { get; set; }
        [JsonPropertyName("depth")] public decimal? Depth { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("base_cost_price")] public decimal? BaseCostPrice { get; set; }
        [JsonPropertyName("cost_price_inc_tax")] public decimal? CostPriceIncTax { get; set; }
        [JsonPropertyName("cost_price_ex_tax")] public decimal? CostPriceExTax { get; set; }
        [JsonPropertyName("cost_price_tax")] public decimal? CostPriceTax { get; set; }
        [JsonPropertyName("is_refunded")] public bool? IsRefunded { get; set; }
        [JsonPropertyName("quantity_refunded")] public int? QuantityRefunded { get; set; }
        [JsonPropertyName("refund_amount")] public decimal? RefundAmount { get; set; }
        [JsonPropertyName("return_id")] public int? ReturnId { get; set; }
        [JsonPropertyName("wrapping_name")] public string? WrappingName { get; set; }
        [JsonPropertyName("base_wrapping_cost")] public decimal? BaseWrappingCost { get; set; }
        [JsonPropertyName("wrapping_cost_ex_tax")] public decimal? WrappingCostExTax { get; set; }
        [JsonPropertyName("wrapping_cost_inc_tax")] public decimal? WrappingCostIncTax { get; set; }
        [JsonPropertyName("wrapping_cost_tax")] public decimal? WrappingCostTax { get; set; }
        [JsonPropertyName("wrapping_message")] public string? WrappingMessage { get; set; }
        [JsonPropertyName("quantity_shipped")] public int? QuantityShipped { get; set; }
        [JsonPropertyName("event_name")] public string? EventName { get; set; }
        [JsonPropertyName("event_date")] public string? EventDate { get; set; }
        [JsonPropertyName("fixed_shipping_cost")] public decimal? FixedShippingCost { get; set; }
        [JsonPropertyName("ebay_item_id")] public string? EbayItemId { get; set; }
        [JsonPropertyName("ebay_transaction_id")] public string? EbayTransactionId { get; set; }
        [JsonPropertyName("option_set_id")] public int? OptionSetId { get; set; }
        [JsonPropertyName("parent_order_product_id")] public int? ParentOrderProductId { get; set; }
        [JsonPropertyName("is_bundled_product")] public bool? IsBundledProduct { get; set; }
        [JsonPropertyName("bin_picking_number")] public string? BinPickingNumber { get; set; }
        [JsonPropertyName("external_id")] public string? ExternalId { get; set; }
        [JsonPropertyName("fulfillment_source")] public string? FulfillmentSource { get; set; }
        [JsonPropertyName("brand")] public string? Brand { get; set; }
        [JsonPropertyName("applied_discounts")] public List<BigCommerceAppliedDiscount>? AppliedDiscounts { get; set; } = new();
        [JsonPropertyName("product_options")] public List<BigCommerceProductOption>? ProductOptions { get; set; } = new();
        [JsonPropertyName("configurable_fields")] public List<BigCommerceConfigurableField>? ConfigurableFields { get; set; } = new();
    }

    public sealed class BigCommerceAppliedDiscount
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("amount")] public decimal? Amount { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("target")] public string? Target { get; set; }
    }

    public sealed class BigCommerceProductOption
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("option_id")] public int? OptionId { get; set; }
        [JsonPropertyName("order_product_id")] public int? OrderProductId { get; set; }
        [JsonPropertyName("product_option_id")] public int? ProductOptionId { get; set; }
        [JsonPropertyName("display_name")] public string? DisplayName { get; set; }
        [JsonPropertyName("display_name_customer")] public string? DisplayNameCustomer { get; set; }
        [JsonPropertyName("display_name_merchant")] public string? DisplayNameMerchant { get; set; }
        [JsonPropertyName("display_value")] public string? DisplayValue { get; set; }
        [JsonPropertyName("display_value_customer")] public string? DisplayValueCustomer { get; set; }
        [JsonPropertyName("display_value_merchant")] public string? DisplayValueMerchant { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("display_style")] public string? DisplayStyle { get; set; }
    }

    public sealed class BigCommerceConfigurableField
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
    }

    public sealed class BigCommerceOrderShippingAddress
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("order_id")] public int? OrderId { get; set; }
        [JsonPropertyName("first_name")] public string? FirstName { get; set; }
        [JsonPropertyName("last_name")] public string? LastName { get; set; }
        [JsonPropertyName("company")] public string? Company { get; set; }
        [JsonPropertyName("street_1")] public string? Street1 { get; set; }
        [JsonPropertyName("street_2")] public string? Street2 { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("zip")] public string? Zip { get; set; }
        [JsonPropertyName("country")] public string? Country { get; set; }
        [JsonPropertyName("country_iso2")] public string? CountryIso2 { get; set; }
        [JsonPropertyName("state_or_province")] public string? StateOrProvince { get; set; }
        [JsonPropertyName("phone")] public string? Phone { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("items_total")] public int? ItemsTotal { get; set; }
        [JsonPropertyName("items_shipped")] public int? ItemsShipped { get; set; }
        [JsonPropertyName("shipping_method")] public string? ShippingMethod { get; set; }
        [JsonPropertyName("base_cost")] public decimal? BaseCost { get; set; }
        [JsonPropertyName("cost_ex_tax")] public decimal? CostExTax { get; set; }
        [JsonPropertyName("cost_inc_tax")] public decimal? CostIncTax { get; set; }
        [JsonPropertyName("cost_tax")] public decimal? CostTax { get; set; }
        [JsonPropertyName("cost_tax_class_id")] public int? CostTaxClassId { get; set; }
        [JsonPropertyName("base_handling_cost")] public decimal? BaseHandlingCost { get; set; }
        [JsonPropertyName("handling_cost_ex_tax")] public decimal? HandlingCostExTax { get; set; }
        [JsonPropertyName("handling_cost_inc_tax")] public decimal? HandlingCostIncTax { get; set; }
        [JsonPropertyName("handling_cost_tax")] public decimal? HandlingCostTax { get; set; }
        [JsonPropertyName("handling_cost_tax_class_id")] public int? HandlingCostTaxClassId { get; set; }
    }

    public sealed class BigCommerceOrderCoupon
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("coupon_id")] public int? CouponId { get; set; }
        [JsonPropertyName("order_id")] public int? OrderId { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("type")] public int? Type { get; set; }
        [JsonPropertyName("discount")] public decimal? Discount { get; set; }
    }

    // Additional model classes for other entities
    public sealed class BigCommerceCart
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("customer_id")] public int? CustomerId { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("currency")] public BigCommerceCurrencyRef? Currency { get; set; }
        [JsonPropertyName("tax_included")] public bool? TaxIncluded { get; set; }
        [JsonPropertyName("base_amount")] public decimal? BaseAmount { get; set; }
        [JsonPropertyName("discount_amount")] public decimal? DiscountAmount { get; set; }
        [JsonPropertyName("cart_amount")] public decimal? CartAmount { get; set; }
        [JsonPropertyName("coupons")] public List<BigCommerceCartCoupon>? Coupons { get; set; } = new();
        [JsonPropertyName("line_items")] public BigCommerceCartLineItems? LineItems { get; set; }
        [JsonPropertyName("created_time")] public DateTimeOffset? CreatedTime { get; set; }
        [JsonPropertyName("updated_time")] public DateTimeOffset? UpdatedTime { get; set; }
    }

    public sealed class BigCommerceCurrencyRef
    {
        [JsonPropertyName("code")] public string? Code { get; set; }
    }

    public sealed class BigCommerceCartCoupon
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("discount")] public decimal? Discount { get; set; }
    }

    public sealed class BigCommerceCartLineItems
    {
        [JsonPropertyName("physical_items")] public List<BigCommerceCartPhysicalItem>? PhysicalItems { get; set; } = new();
        [JsonPropertyName("digital_items")] public List<BigCommerceCartDigitalItem>? DigitalItems { get; set; } = new();
        [JsonPropertyName("gift_certificates")] public List<BigCommerceCartGiftCertificate>? GiftCertificates { get; set; } = new();
        [JsonPropertyName("custom_items")] public List<BigCommerceCartCustomItem>? CustomItems { get; set; } = new();
    }

    public sealed class BigCommerceCartPhysicalItem
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("parent_id")] public string? ParentId { get; set; }
        [JsonPropertyName("variant_id")] public int? VariantId { get; set; }
        [JsonPropertyName("product_id")] public int? ProductId { get; set; }
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("taxable")] public bool? Taxable { get; set; }
        [JsonPropertyName("image_url")] public string? ImageUrl { get; set; }
        [JsonPropertyName("discounts")] public List<BigCommerceCartItemDiscount>? Discounts { get; set; } = new();
        [JsonPropertyName("coupons")] public List<BigCommerceCartItemCoupon>? Coupons { get; set; } = new();
        [JsonPropertyName("discount_amount")] public decimal? DiscountAmount { get; set; }
        [JsonPropertyName("coupon_amount")] public decimal? CouponAmount { get; set; }
        [JsonPropertyName("list_price")] public decimal? ListPrice { get; set; }
        [JsonPropertyName("sale_price")] public decimal? SalePrice { get; set; }
        [JsonPropertyName("extended_list_price")] public decimal? ExtendedListPrice { get; set; }
        [JsonPropertyName("extended_sale_price")] public decimal? ExtendedSalePrice { get; set; }
        [JsonPropertyName("is_require_shipping")] public bool? IsRequireShipping { get; set; }
        [JsonPropertyName("is_mutable")] public bool? IsMutable { get; set; }
        [JsonPropertyName("added_by_promotion")] public bool? AddedByPromotion { get; set; }
    }

    public sealed class BigCommerceCartDigitalItem
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("parent_id")] public string? ParentId { get; set; }
        [JsonPropertyName("variant_id")] public int? VariantId { get; set; }
        [JsonPropertyName("product_id")] public int? ProductId { get; set; }
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("taxable")] public bool? Taxable { get; set; }
        [JsonPropertyName("image_url")] public string? ImageUrl { get; set; }
        [JsonPropertyName("discounts")] public List<BigCommerceCartItemDiscount>? Discounts { get; set; } = new();
        [JsonPropertyName("coupons")] public List<BigCommerceCartItemCoupon>? Coupons { get; set; } = new();
        [JsonPropertyName("discount_amount")] public decimal? DiscountAmount { get; set; }
        [JsonPropertyName("coupon_amount")] public decimal? CouponAmount { get; set; }
        [JsonPropertyName("list_price")] public decimal? ListPrice { get; set; }
        [JsonPropertyName("sale_price")] public decimal? SalePrice { get; set; }
        [JsonPropertyName("extended_list_price")] public decimal? ExtendedListPrice { get; set; }
        [JsonPropertyName("extended_sale_price")] public decimal? ExtendedSalePrice { get; set; }
        [JsonPropertyName("is_mutable")] public bool? IsMutable { get; set; }
        [JsonPropertyName("added_by_promotion")] public bool? AddedByPromotion { get; set; }
    }

    public sealed class BigCommerceCartGiftCertificate
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("theme")] public string? Theme { get; set; }
        [JsonPropertyName("amount")] public decimal? Amount { get; set; }
        [JsonPropertyName("is_taxable")] public bool? IsTaxable { get; set; }
        [JsonPropertyName("sender")] public BigCommerceCartGiftCertificateSender? Sender { get; set; }
        [JsonPropertyName("recipient")] public BigCommerceCartGiftCertificateRecipient? Recipient { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
    }

    public sealed class BigCommerceCartCustomItem
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("list_price")] public decimal? ListPrice { get; set; }
    }

    public sealed class BigCommerceCartItemDiscount
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("discounted_amount")] public decimal? DiscountedAmount { get; set; }
    }

    public sealed class BigCommerceCartItemCoupon
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("discounted_amount")] public decimal? DiscountedAmount { get; set; }
    }

    public sealed class BigCommerceCartGiftCertificateSender
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
    }

    public sealed class BigCommerceCartGiftCertificateRecipient
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
    }

    public sealed class BigCommerceCheckout
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("cart")] public BigCommerceCart? Cart { get; set; }
        [JsonPropertyName("billing_address")] public BigCommerceCheckoutBillingAddress? BillingAddress { get; set; }
        [JsonPropertyName("consignments")] public List<BigCommerceCheckoutConsignment>? Consignments { get; set; } = new();
        [JsonPropertyName("taxes")] public List<BigCommerceCheckoutTax>? Taxes { get; set; } = new();
        [JsonPropertyName("coupons")] public List<BigCommerceCheckoutCoupon>? Coupons { get; set; } = new();
        [JsonPropertyName("order_id")] public string? OrderId { get; set; }
        [JsonPropertyName("shipping_cost_total")] public decimal? ShippingCostTotal { get; set; }
        [JsonPropertyName("shipping_cost_before_discount")] public decimal? ShippingCostBeforeDiscount { get; set; }
        [JsonPropertyName("handling_cost_total")] public decimal? HandlingCostTotal { get; set; }
        [JsonPropertyName("tax_total")] public decimal? TaxTotal { get; set; }
        [JsonPropertyName("subtotal")] public decimal? Subtotal { get; set; }
        [JsonPropertyName("grand_total")] public decimal? GrandTotal { get; set; }
        [JsonPropertyName("created_time")] public DateTimeOffset? CreatedTime { get; set; }
        [JsonPropertyName("updated_time")] public DateTimeOffset? UpdatedTime { get; set; }
        [JsonPropertyName("customer_message")] public string? CustomerMessage { get; set; }
    }

    public sealed class BigCommerceCheckoutBillingAddress
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("first_name")] public string? FirstName { get; set; }
        [JsonPropertyName("last_name")] public string? LastName { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("company")] public string? Company { get; set; }
        [JsonPropertyName("address1")] public string? Address1 { get; set; }
        [JsonPropertyName("address2")] public string? Address2 { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("state_or_province")] public string? StateOrProvince { get; set; }
        [JsonPropertyName("postal_code")] public string? PostalCode { get; set; }
        [JsonPropertyName("country_code")] public string? CountryCode { get; set; }
        [JsonPropertyName("phone")] public string? Phone { get; set; }
    }

    public sealed class BigCommerceCheckoutConsignment
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("shipping_address")] public BigCommerceCheckoutShippingAddress? ShippingAddress { get; set; }
        [JsonPropertyName("available_shipping_options")] public List<BigCommerceShippingOption>? AvailableShippingOptions { get; set; } = new();
        [JsonPropertyName("selected_shipping_option")] public BigCommerceShippingOption? SelectedShippingOption { get; set; }
        [JsonPropertyName("coupon_discounts")] public List<BigCommerceCheckoutCouponDiscount>? CouponDiscounts { get; set; } = new();
        [JsonPropertyName("discounts")] public List<BigCommerceCheckoutDiscount>? Discounts { get; set; } = new();
        [JsonPropertyName("line_item_ids")] public List<string>? LineItemIds { get; set; } = new();
        [JsonPropertyName("shipping_cost")] public decimal? ShippingCost { get; set; }
        [JsonPropertyName("handling_cost")] public decimal? HandlingCost { get; set; }
        [JsonPropertyName("tax_total")] public decimal? TaxTotal { get; set; }
    }

    public sealed class BigCommerceCheckoutShippingAddress
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("first_name")] public string? FirstName { get; set; }
        [JsonPropertyName("last_name")] public string? LastName { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("company")] public string? Company { get; set; }
        [JsonPropertyName("address1")] public string? Address1 { get; set; }
        [JsonPropertyName("address2")] public string? Address2 { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("state_or_province")] public string? StateOrProvince { get; set; }
        [JsonPropertyName("postal_code")] public string? PostalCode { get; set; }
        [JsonPropertyName("country_code")] public string? CountryCode { get; set; }
        [JsonPropertyName("phone")] public string? Phone { get; set; }
    }

    public sealed class BigCommerceShippingOption
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("image_url")] public string? ImageUrl { get; set; }
        [JsonPropertyName("cost")] public decimal? Cost { get; set; }
        [JsonPropertyName("additional_cost")] public decimal? AdditionalCost { get; set; }
        [JsonPropertyName("estimated_transit_time")] public string? EstimatedTransitTime { get; set; }
    }

    public sealed class BigCommerceCheckoutCouponDiscount
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("amount")] public decimal? Amount { get; set; }
    }

    public sealed class BigCommerceCheckoutDiscount
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("discounted_amount")] public decimal? DiscountedAmount { get; set; }
    }

    public sealed class BigCommerceCheckoutTax
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("amount")] public decimal? Amount { get; set; }
    }

    public sealed class BigCommerceCheckoutCoupon
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("discounted_amount")] public decimal? DiscountedAmount { get; set; }
    }

    public sealed class BigCommerceWishlist
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("customer_id")] public int? CustomerId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("is_public")] public bool? IsPublic { get; set; }
        [JsonPropertyName("token")] public string? Token { get; set; }
        [JsonPropertyName("items")] public List<BigCommerceWishlistItem>? Items { get; set; } = new();
    }

    public sealed class BigCommerceWishlistItem
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("product_id")] public int? ProductId { get; set; }
        [JsonPropertyName("variant_id")] public int? VariantId { get; set; }
    }

    public sealed class BigCommercePage
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("channel_id")] public int? ChannelId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("is_visible")] public bool? IsVisible { get; set; }
        [JsonPropertyName("parent_id")] public int? ParentId { get; set; }
        [JsonPropertyName("sort_order")] public int? SortOrder { get; set; }
        [JsonPropertyName("is_homepage")] public bool? IsHomepage { get; set; }
        [JsonPropertyName("is_customers_only")] public bool? IsCustomersOnly { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("meta_title")] public string? MetaTitle { get; set; }
        [JsonPropertyName("meta_description")] public string? MetaDescription { get; set; }
        [JsonPropertyName("meta_keywords")] public List<string>? MetaKeywords { get; set; } = new();
        [JsonPropertyName("search_keywords")] public string? SearchKeywords { get; set; }
        [JsonPropertyName("has_mobile_version")] public bool? HasMobileVersion { get; set; }
    }

    public sealed class BigCommerceBlogPost
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("preview_url")] public string? PreviewUrl { get; set; }
        [JsonPropertyName("summary")] public string? Summary { get; set; }
        [JsonPropertyName("body")] public string? Body { get; set; }
        [JsonPropertyName("tags")] public List<int>? Tags { get; set; } = new();
        [JsonPropertyName("published_date")] public DateTimeOffset? PublishedDate { get; set; }
        [JsonPropertyName("is_published")] public bool? IsPublished { get; set; }
        [JsonPropertyName("author")] public string? Author { get; set; }
        [JsonPropertyName("meta_title")] public string? MetaTitle { get; set; }
        [JsonPropertyName("meta_description")] public string? MetaDescription { get; set; }
        [JsonPropertyName("meta_keywords")] public List<string>? MetaKeywords { get; set; } = new();
        [JsonPropertyName("thumbnail_path")] public string? ThumbnailPath { get; set; }
    }

    public sealed class BigCommerceCoupon
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("amount")] public decimal? Amount { get; set; }
        [JsonPropertyName("min_purchase")] public decimal? MinPurchase { get; set; }
        [JsonPropertyName("expires")] public DateTimeOffset? Expires { get; set; }
        [JsonPropertyName("max_uses")] public int? MaxUses { get; set; }
        [JsonPropertyName("max_uses_per_customer")] public int? MaxUsesPerCustomer { get; set; }
        [JsonPropertyName("restricted_to")] public List<BigCommerceCouponRestriction>? RestrictedTo { get; set; } = new();
        [JsonPropertyName("shipping_methods")] public List<string>? ShippingMethods { get; set; } = new();
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("applies_to")] public BigCommerceCouponAppliesTo? AppliesTo { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("is_deleted")] public bool? IsDeleted { get; set; }
    }

    public sealed class BigCommerceCouponRestriction
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("ids")] public List<int>? Ids { get; set; } = new();
    }

    public sealed class BigCommerceCouponAppliesTo
    {
        [JsonPropertyName("entity")] public string? Entity { get; set; }
        [JsonPropertyName("ids")] public List<int>? Ids { get; set; } = new();
    }

    public sealed class BigCommerceGiftCertificate
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("balance")] public decimal? Balance { get; set; }
        [JsonPropertyName("original_balance")] public decimal? OriginalBalance { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("to_name")] public string? ToName { get; set; }
        [JsonPropertyName("to_email")] public string? ToEmail { get; set; }
        [JsonPropertyName("from_name")] public string? FromName { get; set; }
        [JsonPropertyName("from_email")] public string? FromEmail { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("template")] public string? Template { get; set; }
        [JsonPropertyName("expiry_date")] public DateTimeOffset? ExpiryDate { get; set; }
    }

    public sealed class BigCommerceStore
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("domain")] public string? Domain { get; set; }
        [JsonPropertyName("secure_url")] public string? SecureUrl { get; set; }
        [JsonPropertyName("control_panel_base_url")] public string? ControlPanelBaseUrl { get; set; }
        [JsonPropertyName("logo")] public BigCommerceStoreLogo? Logo { get; set; }
        [JsonPropertyName("plan_name")] public string? PlanName { get; set; }
        [JsonPropertyName("plan_level")] public int? PlanLevel { get; set; }
        [JsonPropertyName("industry")] public string? Industry { get; set; }
        [JsonPropertyName("timezone")] public BigCommerceStoreTimezone? Timezone { get; set; }
        [JsonPropertyName("language")] public string? Language { get; set; }
        [JsonPropertyName("currency")] public string? Currency { get; set; }
        [JsonPropertyName("currency_symbol")] public string? CurrencySymbol { get; set; }
        [JsonPropertyName("decimal_separator")] public string? DecimalSeparator { get; set; }
        [JsonPropertyName("thousands_separator")] public string? ThousandsSeparator { get; set; }
        [JsonPropertyName("decimal_places")] public int? DecimalPlaces { get; set; }
        [JsonPropertyName("currency_symbol_location")] public string? CurrencySymbolLocation { get; set; }
        [JsonPropertyName("weight_units")] public string? WeightUnits { get; set; }
        [JsonPropertyName("dimension_units")] public string? DimensionUnits { get; set; }
        [JsonPropertyName("dimension_decimal_places")] public int? DimensionDecimalPlaces { get; set; }
        [JsonPropertyName("dimension_decimal_token")] public string? DimensionDecimalToken { get; set; }
        [JsonPropertyName("dimension_thousands_token")] public string? DimensionThousandsToken { get; set; }
        [JsonPropertyName("plan_dimension_decimal_places")] public int? PlanDimensionDecimalPlaces { get; set; }
        [JsonPropertyName("plan_dimension_decimal_token")] public string? PlanDimensionDecimalToken { get; set; }
        [JsonPropertyName("plan_dimension_thousands_token")] public string? PlanDimensionThousandsToken { get; set; }
        [JsonPropertyName("plan_weight_decimal_places")] public int? PlanWeightDecimalPlaces { get; set; }
        [JsonPropertyName("plan_weight_decimal_token")] public string? PlanWeightDecimalToken { get; set; }
        [JsonPropertyName("plan_weight_thousands_token")] public string? PlanWeightThousandsToken { get; set; }
        [JsonPropertyName("plan_price_decimal_places")] public int? PlanPriceDecimalPlaces { get; set; }
        [JsonPropertyName("plan_price_decimal_token")] public string? PlanPriceDecimalToken { get; set; }
        [JsonPropertyName("plan_price_thousands_token")] public string? PlanPriceThousandsToken { get; set; }
        [JsonPropertyName("features")] public Dictionary<string, object>? Features { get; set; } = new();
    }

    public sealed class BigCommerceStoreLogo
    {
        [JsonPropertyName("url")] public string? Url { get; set; }
    }

    public sealed class BigCommerceStoreTimezone
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("raw_offset")] public int? RawOffset { get; set; }
        [JsonPropertyName("dst_offset")] public int? DstOffset { get; set; }
        [JsonPropertyName("dst_correction")] public bool? DstCorrection { get; set; }
        [JsonPropertyName("date_format")] public BigCommerceDateFormat? DateFormat { get; set; }
    }

    public sealed class BigCommerceDateFormat
    {
        [JsonPropertyName("display")] public string? Display { get; set; }
        [JsonPropertyName("export")] public string? Export { get; set; }
        [JsonPropertyName("extended_display")] public string? ExtendedDisplay { get; set; }
    }

    public sealed class BigCommerceCurrency
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("is_default")] public bool? IsDefault { get; set; }
        [JsonPropertyName("last_updated")] public DateTimeOffset? LastUpdated { get; set; }
        [JsonPropertyName("is_active")] public bool? IsActive { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("symbol")] public string? Symbol { get; set; }
        [JsonPropertyName("symbol_placement")] public string? SymbolPlacement { get; set; }
        [JsonPropertyName("decimal_places")] public int? DecimalPlaces { get; set; }
        [JsonPropertyName("decimal_separator")] public string? DecimalSeparator { get; set; }
        [JsonPropertyName("thousands_separator")] public string? ThousandsSeparator { get; set; }
        [JsonPropertyName("exchange_rate")] public decimal? ExchangeRate { get; set; }
    }

    public sealed class BigCommerceTaxClass
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    public sealed class BigCommerceShippingZone
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("locations")] public List<BigCommerceShippingZoneLocation>? Locations { get; set; } = new();
        [JsonPropertyName("methods")] public List<BigCommerceShippingZoneMethod>? Methods { get; set; } = new();
    }

    public sealed class BigCommerceShippingZoneLocation
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("zone_id")] public int? ZoneId { get; set; }
        [JsonPropertyName("location_type")] public string? LocationType { get; set; }
        [JsonPropertyName("country_iso2")] public string? CountryIso2 { get; set; }
        [JsonPropertyName("state_iso2")] public string? StateIso2 { get; set; }
        [JsonPropertyName("zip")] public string? Zip { get; set; }
    }

    public sealed class BigCommerceShippingZoneMethod
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("settings")] public BigCommerceShippingMethodSettings? Settings { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("handling_fees")] public BigCommerceShippingMethodHandlingFees? HandlingFees { get; set; }
        [JsonPropertyName("is_fallback")] public bool? IsFallback { get; set; }
    }

    public sealed class BigCommerceShippingMethodSettings
    {
        [JsonPropertyName("rate")] public decimal? Rate { get; set; }
    }

    public sealed class BigCommerceShippingMethodHandlingFees
    {
        [JsonPropertyName("fixed")] public decimal? Fixed { get; set; }
        [JsonPropertyName("percentage")] public decimal? Percentage { get; set; }
    }

    public sealed class BigCommercePaymentMethod
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("client_id")] public string? ClientId { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("test_mode")] public bool? TestMode { get; set; }
        [JsonPropertyName("merchant_id")] public string? MerchantId { get; set; }
        [JsonPropertyName("supported_cards")] public List<string>? SupportedCards { get; set; } = new();
    }

    public sealed class BigCommerceInventoryItem
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("product_id")] public int? ProductId { get; set; }
        [JsonPropertyName("variant_id")] public int? VariantId { get; set; }
        [JsonPropertyName("inventory_level")] public int? InventoryLevel { get; set; }
        [JsonPropertyName("inventory_warning_level")] public int? InventoryWarningLevel { get; set; }
    }

    public sealed class BigCommerceCustomerGroup
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("is_default")] public bool? IsDefault { get; set; }
        [JsonPropertyName("category_access")] public BigCommerceCustomerGroupCategoryAccess? CategoryAccess { get; set; }
        [JsonPropertyName("discount_rules")] public List<BigCommerceCustomerGroupDiscountRule>? DiscountRules { get; set; } = new();
    }

    public sealed class BigCommerceCustomerGroupCategoryAccess
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("categories")] public List<int>? Categories { get; set; } = new();
    }

    public sealed class BigCommerceCustomerGroupDiscountRule
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("method")] public string? Method { get; set; }
        [JsonPropertyName("amount")] public decimal? Amount { get; set; }
        [JsonPropertyName("category_id")] public int? CategoryId { get; set; }
    }

    public sealed class BigCommercePriceList
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("date_created")] public DateTimeOffset? DateCreated { get; set; }
        [JsonPropertyName("date_modified")] public DateTimeOffset? DateModified { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("active")] public bool? Active { get; set; }
        [JsonPropertyName("currency")] public string? Currency { get; set; }
    }

    public sealed class BigCommerceProductReview
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("product_id")] public int? ProductId { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("text")] public string? Text { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("rating")] public int? Rating { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("date_created")] public DateTimeOffset? DateCreated { get; set; }
        [JsonPropertyName("date_modified")] public DateTimeOffset? DateModified { get; set; }
    }

    public sealed class BigCommerceStoreReview
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("text")] public string? Text { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("rating")] public int? Rating { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("date_created")] public DateTimeOffset? DateCreated { get; set; }
        [JsonPropertyName("date_modified")] public DateTimeOffset? DateModified { get; set; }
    }
}