// File: BeepDM/Connectors/Ecommerce/OpenCartDataSource/Models/OpenCartModels.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Ecommerce.OpenCartDataSource.Models
{
    // Base
    public abstract class OpenCartEntityBase
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("date_added")] public DateTime? DateAdded { get; set; }
        [JsonPropertyName("date_modified")] public DateTime? DateModified { get; set; }

        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : OpenCartEntityBase { DataSource = ds; return (T)this; }
    }

    // ---------- Categories ----------

    public sealed class OpenCartCategory : OpenCartEntityBase
    {
        [JsonPropertyName("category_id")] public int? CategoryId { get; set; }
        [JsonPropertyName("image")] public string? Image { get; set; }
        [JsonPropertyName("parent_id")] public int? ParentId { get; set; }
        [JsonPropertyName("top")] public bool? Top { get; set; }
        [JsonPropertyName("column")] public int? Column { get; set; }
        [JsonPropertyName("sort_order")] public int? SortOrder { get; set; }
        [JsonPropertyName("status")] public bool? Status { get; set; }
        [JsonPropertyName("date_added")] public DateTime? DateAdded { get; set; }
        [JsonPropertyName("date_modified")] public DateTime? DateModified { get; set; }
        [JsonPropertyName("language_id")] public int? LanguageId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("meta_title")] public string? MetaTitle { get; set; }
        [JsonPropertyName("meta_description")] public string? MetaDescription { get; set; }
        [JsonPropertyName("meta_keyword")] public string? MetaKeyword { get; set; }
        [JsonPropertyName("store_ids")] public List<int>? StoreIds { get; set; } = new();
        [JsonPropertyName("keyword")] public string? Keyword { get; set; }
        [JsonPropertyName("layout")] public string? Layout { get; set; }
    }

    // ---------- Products ----------

    public sealed class OpenCartProduct : OpenCartEntityBase
    {
        [JsonPropertyName("product_id")] public int? ProductId { get; set; }
        [JsonPropertyName("model")] public string? Model { get; set; }
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("upc")] public string? Upc { get; set; }
        [JsonPropertyName("ean")] public string? Ean { get; set; }
        [JsonPropertyName("jan")] public string? Jan { get; set; }
        [JsonPropertyName("isbn")] public string? Isbn { get; set; }
        [JsonPropertyName("mpn")] public string? Mpn { get; set; }
        [JsonPropertyName("location")] public string? Location { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("stock_status_id")] public int? StockStatusId { get; set; }
        [JsonPropertyName("image")] public string? Image { get; set; }
        [JsonPropertyName("manufacturer_id")] public int? ManufacturerId { get; set; }
        [JsonPropertyName("shipping")] public bool? Shipping { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
        [JsonPropertyName("points")] public int? Points { get; set; }
        [JsonPropertyName("tax_class_id")] public int? TaxClassId { get; set; }
        [JsonPropertyName("date_available")] public DateTime? DateAvailable { get; set; }
        [JsonPropertyName("weight")] public decimal? Weight { get; set; }
        [JsonPropertyName("weight_class_id")] public int? WeightClassId { get; set; }
        [JsonPropertyName("length")] public decimal? Length { get; set; }
        [JsonPropertyName("width")] public decimal? Width { get; set; }
        [JsonPropertyName("height")] public decimal? Height { get; set; }
        [JsonPropertyName("length_class_id")] public int? LengthClassId { get; set; }
        [JsonPropertyName("subtract")] public bool? Subtract { get; set; }
        [JsonPropertyName("minimum")] public int? Minimum { get; set; }
        [JsonPropertyName("sort_order")] public int? SortOrder { get; set; }
        [JsonPropertyName("status")] public bool? Status { get; set; }
        [JsonPropertyName("viewed")] public int? Viewed { get; set; }
        [JsonPropertyName("date_added")] public DateTime? DateAdded { get; set; }
        [JsonPropertyName("date_modified")] public DateTime? DateModified { get; set; }
        [JsonPropertyName("language_id")] public int? LanguageId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("tag")] public string? Tag { get; set; }
        [JsonPropertyName("meta_title")] public string? MetaTitle { get; set; }
        [JsonPropertyName("meta_description")] public string? MetaDescription { get; set; }
        [JsonPropertyName("meta_keyword")] public string? MetaKeyword { get; set; }
        [JsonPropertyName("store_ids")] public List<int>? StoreIds { get; set; } = new();
        [JsonPropertyName("keyword")] public string? Keyword { get; set; }
        [JsonPropertyName("layout")] public string? Layout { get; set; }
        [JsonPropertyName("categories")] public List<int>? Categories { get; set; } = new();
        [JsonPropertyName("filters")] public List<int>? Filters { get; set; } = new();
        [JsonPropertyName("downloads")] public List<int>? Downloads { get; set; } = new();
        [JsonPropertyName("related")] public List<int>? Related { get; set; } = new();
        [JsonPropertyName("images")] public List<OpenCartProductImage>? Images { get; set; } = new();
        [JsonPropertyName("options")] public List<OpenCartProductOption>? Options { get; set; } = new();
        [JsonPropertyName("specials")] public List<OpenCartProductSpecial>? Specials { get; set; } = new();
        [JsonPropertyName("discounts")] public List<OpenCartProductDiscount>? Discounts { get; set; } = new();
        [JsonPropertyName("attributes")] public List<OpenCartProductAttribute>? Attributes { get; set; } = new();
        [JsonPropertyName("recurrings")] public List<OpenCartProductRecurring>? Recurrings { get; set; } = new();
    }

    public sealed class OpenCartProductImage
    {
        [JsonPropertyName("product_image_id")] public int? ProductImageId { get; set; }
        [JsonPropertyName("product_id")] public int? ProductId { get; set; }
        [JsonPropertyName("image")] public string? Image { get; set; }
        [JsonPropertyName("sort_order")] public int? SortOrder { get; set; }
    }

    public sealed class OpenCartProductOption
    {
        [JsonPropertyName("product_option_id")] public int? ProductOptionId { get; set; }
        [JsonPropertyName("product_id")] public int? ProductId { get; set; }
        [JsonPropertyName("option_id")] public int? OptionId { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
        [JsonPropertyName("required")] public bool? Required { get; set; }
        [JsonPropertyName("option")] public OpenCartOption? Option { get; set; }
        [JsonPropertyName("product_option_values")] public List<OpenCartProductOptionValue>? ProductOptionValues { get; set; } = new();
    }

    public sealed class OpenCartProductOptionValue
    {
        [JsonPropertyName("product_option_value_id")] public int? ProductOptionValueId { get; set; }
        [JsonPropertyName("product_option_id")] public int? ProductOptionId { get; set; }
        [JsonPropertyName("product_id")] public int? ProductId { get; set; }
        [JsonPropertyName("option_id")] public int? OptionId { get; set; }
        [JsonPropertyName("option_value_id")] public int? OptionValueId { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("subtract")] public bool? Subtract { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
        [JsonPropertyName("price_prefix")] public string? PricePrefix { get; set; }
        [JsonPropertyName("points")] public int? Points { get; set; }
        [JsonPropertyName("points_prefix")] public string? PointsPrefix { get; set; }
        [JsonPropertyName("weight")] public decimal? Weight { get; set; }
        [JsonPropertyName("weight_prefix")] public string? WeightPrefix { get; set; }
        [JsonPropertyName("option_value")] public OpenCartOptionValue? OptionValue { get; set; }
    }

    public sealed class OpenCartProductSpecial
    {
        [JsonPropertyName("product_special_id")] public int? ProductSpecialId { get; set; }
        [JsonPropertyName("product_id")] public int? ProductId { get; set; }
        [JsonPropertyName("customer_group_id")] public int? CustomerGroupId { get; set; }
        [JsonPropertyName("priority")] public int? Priority { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
        [JsonPropertyName("date_start")] public DateTime? DateStart { get; set; }
        [JsonPropertyName("date_end")] public DateTime? DateEnd { get; set; }
    }

    public sealed class OpenCartProductDiscount
    {
        [JsonPropertyName("product_discount_id")] public int? ProductDiscountId { get; set; }
        [JsonPropertyName("product_id")] public int? ProductId { get; set; }
        [JsonPropertyName("customer_group_id")] public int? CustomerGroupId { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("priority")] public int? Priority { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
        [JsonPropertyName("date_start")] public DateTime? DateStart { get; set; }
        [JsonPropertyName("date_end")] public DateTime? DateEnd { get; set; }
    }

    public sealed class OpenCartProductAttribute
    {
        [JsonPropertyName("product_id")] public int? ProductId { get; set; }
        [JsonPropertyName("attribute_id")] public int? AttributeId { get; set; }
        [JsonPropertyName("language_id")] public int? LanguageId { get; set; }
        [JsonPropertyName("text")] public string? Text { get; set; }
        [JsonPropertyName("attribute")] public OpenCartAttribute? Attribute { get; set; }
    }

    public sealed class OpenCartProductRecurring
    {
        [JsonPropertyName("product_id")] public int? ProductId { get; set; }
        [JsonPropertyName("recurring_id")] public int? RecurringId { get; set; }
        [JsonPropertyName("customer_group_id")] public int? CustomerGroupId { get; set; }
    }

    // ---------- Orders ----------

    public sealed class OpenCartOrder : OpenCartEntityBase
    {
        [JsonPropertyName("order_id")] public int? OrderId { get; set; }
        [JsonPropertyName("invoice_no")] public int? InvoiceNo { get; set; }
        [JsonPropertyName("invoice_prefix")] public string? InvoicePrefix { get; set; }
        [JsonPropertyName("store_id")] public int? StoreId { get; set; }
        [JsonPropertyName("store_name")] public string? StoreName { get; set; }
        [JsonPropertyName("store_url")] public string? StoreUrl { get; set; }
        [JsonPropertyName("customer_id")] public int? CustomerId { get; set; }
        [JsonPropertyName("customer_group_id")] public int? CustomerGroupId { get; set; }
        [JsonPropertyName("firstname")] public string? Firstname { get; set; }
        [JsonPropertyName("lastname")] public string? Lastname { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("telephone")] public string? Telephone { get; set; }
        [JsonPropertyName("fax")] public string? Fax { get; set; }
        [JsonPropertyName("custom_field")] public string? CustomField { get; set; }
        [JsonPropertyName("payment_firstname")] public string? PaymentFirstname { get; set; }
        [JsonPropertyName("payment_lastname")] public string? PaymentLastname { get; set; }
        [JsonPropertyName("payment_company")] public string? PaymentCompany { get; set; }
        [JsonPropertyName("payment_address_1")] public string? PaymentAddress1 { get; set; }
        [JsonPropertyName("payment_address_2")] public string? PaymentAddress2 { get; set; }
        [JsonPropertyName("payment_city")] public string? PaymentCity { get; set; }
        [JsonPropertyName("payment_postcode")] public string? PaymentPostcode { get; set; }
        [JsonPropertyName("payment_country")] public string? PaymentCountry { get; set; }
        [JsonPropertyName("payment_country_id")] public int? PaymentCountryId { get; set; }
        [JsonPropertyName("payment_zone")] public string? PaymentZone { get; set; }
        [JsonPropertyName("payment_zone_id")] public int? PaymentZoneId { get; set; }
        [JsonPropertyName("payment_address_format")] public string? PaymentAddressFormat { get; set; }
        [JsonPropertyName("payment_custom_field")] public string? PaymentCustomField { get; set; }
        [JsonPropertyName("payment_method")] public string? PaymentMethod { get; set; }
        [JsonPropertyName("payment_code")] public string? PaymentCode { get; set; }
        [JsonPropertyName("shipping_firstname")] public string? ShippingFirstname { get; set; }
        [JsonPropertyName("shipping_lastname")] public string? ShippingLastname { get; set; }
        [JsonPropertyName("shipping_company")] public string? ShippingCompany { get; set; }
        [JsonPropertyName("shipping_address_1")] public string? ShippingAddress1 { get; set; }
        [JsonPropertyName("shipping_address_2")] public string? ShippingAddress2 { get; set; }
        [JsonPropertyName("shipping_city")] public string? ShippingCity { get; set; }
        [JsonPropertyName("shipping_postcode")] public string? ShippingPostcode { get; set; }
        [JsonPropertyName("shipping_country")] public string? ShippingCountry { get; set; }
        [JsonPropertyName("shipping_country_id")] public int? ShippingCountryId { get; set; }
        [JsonPropertyName("shipping_zone")] public string? ShippingZone { get; set; }
        [JsonPropertyName("shipping_zone_id")] public int? ShippingZoneId { get; set; }
        [JsonPropertyName("shipping_address_format")] public string? ShippingAddressFormat { get; set; }
        [JsonPropertyName("shipping_custom_field")] public string? ShippingCustomField { get; set; }
        [JsonPropertyName("shipping_method")] public string? ShippingMethod { get; set; }
        [JsonPropertyName("shipping_code")] public string? ShippingCode { get; set; }
        [JsonPropertyName("comment")] public string? Comment { get; set; }
        [JsonPropertyName("total")] public decimal? Total { get; set; }
        [JsonPropertyName("order_status_id")] public int? OrderStatusId { get; set; }
        [JsonPropertyName("affiliate_id")] public int? AffiliateId { get; set; }
        [JsonPropertyName("commission")] public decimal? Commission { get; set; }
        [JsonPropertyName("marketing_id")] public int? MarketingId { get; set; }
        [JsonPropertyName("tracking")] public string? Tracking { get; set; }
        [JsonPropertyName("language_id")] public int? LanguageId { get; set; }
        [JsonPropertyName("currency_id")] public int? CurrencyId { get; set; }
        [JsonPropertyName("currency_code")] public string? CurrencyCode { get; set; }
        [JsonPropertyName("currency_value")] public decimal? CurrencyValue { get; set; }
        [JsonPropertyName("ip")] public string? Ip { get; set; }
        [JsonPropertyName("forwarded_ip")] public string? ForwardedIp { get; set; }
        [JsonPropertyName("user_agent")] public string? UserAgent { get; set; }
        [JsonPropertyName("accept_language")] public string? AcceptLanguage { get; set; }
        [JsonPropertyName("date_added")] public DateTime? DateAdded { get; set; }
        [JsonPropertyName("date_modified")] public DateTime? DateModified { get; set; }
        [JsonPropertyName("products")] public List<OpenCartOrderProduct>? Products { get; set; } = new();
        [JsonPropertyName("totals")] public List<OpenCartOrderTotal>? Totals { get; set; } = new();
        [JsonPropertyName("histories")] public List<OpenCartOrderHistory>? Histories { get; set; } = new();
    }

    public sealed class OpenCartOrderProduct
    {
        [JsonPropertyName("order_product_id")] public int? OrderProductId { get; set; }
        [JsonPropertyName("order_id")] public int? OrderId { get; set; }
        [JsonPropertyName("product_id")] public int? ProductId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("model")] public string? Model { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
        [JsonPropertyName("total")] public decimal? Total { get; set; }
        [JsonPropertyName("tax")] public decimal? Tax { get; set; }
        [JsonPropertyName("reward")] public int? Reward { get; set; }
        [JsonPropertyName("option")] public List<OpenCartOrderOption>? Option { get; set; } = new();
    }

    public sealed class OpenCartOrderOption
    {
        [JsonPropertyName("order_option_id")] public int? OrderOptionId { get; set; }
        [JsonPropertyName("order_id")] public int? OrderId { get; set; }
        [JsonPropertyName("order_product_id")] public int? OrderProductId { get; set; }
        [JsonPropertyName("product_option_id")] public int? ProductOptionId { get; set; }
        [JsonPropertyName("product_option_value_id")] public int? ProductOptionValueId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
    }

    public sealed class OpenCartOrderTotal
    {
        [JsonPropertyName("order_total_id")] public int? OrderTotalId { get; set; }
        [JsonPropertyName("order_id")] public int? OrderId { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("value")] public decimal? Value { get; set; }
        [JsonPropertyName("sort_order")] public int? SortOrder { get; set; }
    }

    public sealed class OpenCartOrderHistory
    {
        [JsonPropertyName("order_history_id")] public int? OrderHistoryId { get; set; }
        [JsonPropertyName("order_id")] public int? OrderId { get; set; }
        [JsonPropertyName("order_status_id")] public int? OrderStatusId { get; set; }
        [JsonPropertyName("notify")] public bool? Notify { get; set; }
        [JsonPropertyName("comment")] public string? Comment { get; set; }
        [JsonPropertyName("date_added")] public DateTime? DateAdded { get; set; }
    }

    // ---------- Customers ----------

    public sealed class OpenCartCustomer : OpenCartEntityBase
    {
        [JsonPropertyName("customer_id")] public int? CustomerId { get; set; }
        [JsonPropertyName("customer_group_id")] public int? CustomerGroupId { get; set; }
        [JsonPropertyName("store_id")] public int? StoreId { get; set; }
        [JsonPropertyName("language_id")] public int? LanguageId { get; set; }
        [JsonPropertyName("firstname")] public string? Firstname { get; set; }
        [JsonPropertyName("lastname")] public string? Lastname { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("telephone")] public string? Telephone { get; set; }
        [JsonPropertyName("fax")] public string? Fax { get; set; }
        [JsonPropertyName("custom_field")] public string? CustomField { get; set; }
        [JsonPropertyName("newsletter")] public bool? Newsletter { get; set; }
        [JsonPropertyName("ip")] public string? Ip { get; set; }
        [JsonPropertyName("status")] public bool? Status { get; set; }
        [JsonPropertyName("approved")] public bool? Approved { get; set; }
        [JsonPropertyName("safe")] public bool? Safe { get; set; }
        [JsonPropertyName("token")] public string? Token { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("date_added")] public DateTime? DateAdded { get; set; }
        [JsonPropertyName("addresses")] public List<OpenCartCustomerAddress>? Addresses { get; set; } = new();
    }

    public sealed class OpenCartCustomerAddress
    {
        [JsonPropertyName("address_id")] public int? AddressId { get; set; }
        [JsonPropertyName("customer_id")] public int? CustomerId { get; set; }
        [JsonPropertyName("firstname")] public string? Firstname { get; set; }
        [JsonPropertyName("lastname")] public string? Lastname { get; set; }
        [JsonPropertyName("company")] public string? Company { get; set; }
        [JsonPropertyName("address_1")] public string? Address1 { get; set; }
        [JsonPropertyName("address_2")] public string? Address2 { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("postcode")] public string? Postcode { get; set; }
        [JsonPropertyName("country_id")] public int? CountryId { get; set; }
        [JsonPropertyName("zone_id")] public int? ZoneId { get; set; }
        [JsonPropertyName("custom_field")] public string? CustomField { get; set; }
        [JsonPropertyName("default")] public bool? Default { get; set; }
    }

    public sealed class OpenCartCustomerGroup
    {
        [JsonPropertyName("customer_group_id")] public int? CustomerGroupId { get; set; }
        [JsonPropertyName("approval")] public int? Approval { get; set; }
        [JsonPropertyName("sort_order")] public int? SortOrder { get; set; }
        [JsonPropertyName("language_id")] public int? LanguageId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
    }

    // ---------- Manufacturers ----------

    public sealed class OpenCartManufacturer : OpenCartEntityBase
    {
        [JsonPropertyName("manufacturer_id")] public int? ManufacturerId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("image")] public string? Image { get; set; }
        [JsonPropertyName("sort_order")] public int? SortOrder { get; set; }
        [JsonPropertyName("store_ids")] public List<int>? StoreIds { get; set; } = new();
        [JsonPropertyName("keyword")] public string? Keyword { get; set; }
    }

    // ---------- Attributes & Options ----------

    public sealed class OpenCartAttribute : OpenCartEntityBase
    {
        [JsonPropertyName("attribute_id")] public int? AttributeId { get; set; }
        [JsonPropertyName("attribute_group_id")] public int? AttributeGroupId { get; set; }
        [JsonPropertyName("sort_order")] public int? SortOrder { get; set; }
        [JsonPropertyName("language_id")] public int? LanguageId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("attribute_group")] public OpenCartAttributeGroup? AttributeGroup { get; set; }
    }

    public sealed class OpenCartAttributeGroup
    {
        [JsonPropertyName("attribute_group_id")] public int? AttributeGroupId { get; set; }
        [JsonPropertyName("sort_order")] public int? SortOrder { get; set; }
        [JsonPropertyName("language_id")] public int? LanguageId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    public sealed class OpenCartOption : OpenCartEntityBase
    {
        [JsonPropertyName("option_id")] public int? OptionId { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("sort_order")] public int? SortOrder { get; set; }
        [JsonPropertyName("language_id")] public int? LanguageId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("option_values")] public List<OpenCartOptionValue>? OptionValues { get; set; } = new();
    }

    public sealed class OpenCartOptionValue
    {
        [JsonPropertyName("option_value_id")] public int? OptionValueId { get; set; }
        [JsonPropertyName("option_id")] public int? OptionId { get; set; }
        [JsonPropertyName("image")] public string? Image { get; set; }
        [JsonPropertyName("sort_order")] public int? SortOrder { get; set; }
        [JsonPropertyName("language_id")] public int? LanguageId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    // ---------- Coupons & Vouchers ----------

    public sealed class OpenCartCoupon
    {
        [JsonPropertyName("coupon_id")] public int? CouponId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("discount")] public decimal? Discount { get; set; }
        [JsonPropertyName("logged")] public bool? Logged { get; set; }
        [JsonPropertyName("shipping")] public bool? Shipping { get; set; }
        [JsonPropertyName("total")] public decimal? Total { get; set; }
        [JsonPropertyName("date_start")] public DateTime? DateStart { get; set; }
        [JsonPropertyName("date_end")] public DateTime? DateEnd { get; set; }
        [JsonPropertyName("uses_total")] public int? UsesTotal { get; set; }
        [JsonPropertyName("uses_customer")] public int? UsesCustomer { get; set; }
        [JsonPropertyName("status")] public bool? Status { get; set; }
        [JsonPropertyName("date_added")] public DateTime? DateAdded { get; set; }
        [JsonPropertyName("coupon_products")] public List<int>? CouponProducts { get; set; } = new();
        [JsonPropertyName("coupon_categories")] public List<int>? CouponCategories { get; set; } = new();
        [JsonPropertyName("coupon_histories")] public List<OpenCartCouponHistory>? CouponHistories { get; set; } = new();
    }

    public sealed class OpenCartCouponHistory
    {
        [JsonPropertyName("coupon_history_id")] public int? CouponHistoryId { get; set; }
        [JsonPropertyName("coupon_id")] public int? CouponId { get; set; }
        [JsonPropertyName("order_id")] public int? OrderId { get; set; }
        [JsonPropertyName("customer_id")] public int? CustomerId { get; set; }
        [JsonPropertyName("amount")] public decimal? Amount { get; set; }
        [JsonPropertyName("date_added")] public DateTime? DateAdded { get; set; }
    }

    public sealed class OpenCartVoucher
    {
        [JsonPropertyName("voucher_id")] public int? VoucherId { get; set; }
        [JsonPropertyName("order_id")] public int? OrderId { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("from_name")] public string? FromName { get; set; }
        [JsonPropertyName("from_email")] public string? FromEmail { get; set; }
        [JsonPropertyName("to_name")] public string? ToName { get; set; }
        [JsonPropertyName("to_email")] public string? ToEmail { get; set; }
        [JsonPropertyName("voucher_theme_id")] public int? VoucherThemeId { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("amount")] public decimal? Amount { get; set; }
        [JsonPropertyName("status")] public bool? Status { get; set; }
        [JsonPropertyName("date_added")] public DateTime? DateAdded { get; set; }
    }

    // ---------- Reviews ----------

    public sealed class OpenCartReview
    {
        [JsonPropertyName("review_id")] public int? ReviewId { get; set; }
        [JsonPropertyName("product_id")] public int? ProductId { get; set; }
        [JsonPropertyName("customer_id")] public int? CustomerId { get; set; }
        [JsonPropertyName("author")] public string? Author { get; set; }
        [JsonPropertyName("text")] public string? Text { get; set; }
        [JsonPropertyName("rating")] public int? Rating { get; set; }
        [JsonPropertyName("status")] public bool? Status { get; set; }
        [JsonPropertyName("date_added")] public DateTime? DateAdded { get; set; }
        [JsonPropertyName("date_modified")] public DateTime? DateModified { get; set; }
    }

    // ---------- Returns ----------

    public sealed class OpenCartReturn : OpenCartEntityBase
    {
        [JsonPropertyName("return_id")] public int? ReturnId { get; set; }
        [JsonPropertyName("order_id")] public int? OrderId { get; set; }
        [JsonPropertyName("product_id")] public int? ProductId { get; set; }
        [JsonPropertyName("customer_id")] public int? CustomerId { get; set; }
        [JsonPropertyName("firstname")] public string? Firstname { get; set; }
        [JsonPropertyName("lastname")] public string? Lastname { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("telephone")] public string? Telephone { get; set; }
        [JsonPropertyName("product")] public string? Product { get; set; }
        [JsonPropertyName("model")] public string? Model { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("opened")] public bool? Opened { get; set; }
        [JsonPropertyName("return_reason_id")] public int? ReturnReasonId { get; set; }
        [JsonPropertyName("return_action_id")] public int? ReturnActionId { get; set; }
        [JsonPropertyName("return_status_id")] public int? ReturnStatusId { get; set; }
        [JsonPropertyName("comment")] public string? Comment { get; set; }
        [JsonPropertyName("date_ordered")] public DateTime? DateOrdered { get; set; }
        [JsonPropertyName("date_added")] public DateTime? DateAdded { get; set; }
        [JsonPropertyName("date_modified")] public DateTime? DateModified { get; set; }
        [JsonPropertyName("return_histories")] public List<OpenCartReturnHistory>? ReturnHistories { get; set; } = new();
    }

    public sealed class OpenCartReturnHistory
    {
        [JsonPropertyName("return_history_id")] public int? ReturnHistoryId { get; set; }
        [JsonPropertyName("return_id")] public int? ReturnId { get; set; }
        [JsonPropertyName("return_status_id")] public int? ReturnStatusId { get; set; }
        [JsonPropertyName("notify")] public bool? Notify { get; set; }
        [JsonPropertyName("comment")] public string? Comment { get; set; }
        [JsonPropertyName("date_added")] public DateTime? DateAdded { get; set; }
    }

    // ---------- Affiliates ----------

    public sealed class OpenCartAffiliate : OpenCartEntityBase
    {
        [JsonPropertyName("affiliate_id")] public int? AffiliateId { get; set; }
        [JsonPropertyName("firstname")] public string? Firstname { get; set; }
        [JsonPropertyName("lastname")] public string? Lastname { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("telephone")] public string? Telephone { get; set; }
        [JsonPropertyName("fax")] public string? Fax { get; set; }
        [JsonPropertyName("company")] public string? Company { get; set; }
        [JsonPropertyName("website")] public string? Website { get; set; }
        [JsonPropertyName("address_1")] public string? Address1 { get; set; }
        [JsonPropertyName("address_2")] public string? Address2 { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("postcode")] public string? Postcode { get; set; }
        [JsonPropertyName("country_id")] public int? CountryId { get; set; }
        [JsonPropertyName("zone_id")] public int? ZoneId { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("commission")] public decimal? Commission { get; set; }
        [JsonPropertyName("tax")] public string? Tax { get; set; }
        [JsonPropertyName("payment")] public string? Payment { get; set; }
        [JsonPropertyName("cheque")] public string? Cheque { get; set; }
        [JsonPropertyName("paypal")] public string? Paypal { get; set; }
        [JsonPropertyName("bank_name")] public string? BankName { get; set; }
        [JsonPropertyName("bank_branch_number")] public string? BankBranchNumber { get; set; }
        [JsonPropertyName("bank_swift_code")] public string? BankSwiftCode { get; set; }
        [JsonPropertyName("bank_account_name")] public string? BankAccountName { get; set; }
        [JsonPropertyName("bank_account_number")] public string? BankAccountNumber { get; set; }
        [JsonPropertyName("custom_field")] public string? CustomField { get; set; }
        [JsonPropertyName("comment")] public string? Comment { get; set; }
        [JsonPropertyName("status")] public bool? Status { get; set; }
        [JsonPropertyName("date_added")] public DateTime? DateAdded { get; set; }
    }

    // ---------- Supporting Types ----------

    public sealed class OpenCartZone
    {
        [JsonPropertyName("zone_id")] public int? ZoneId { get; set; }
        [JsonPropertyName("country_id")] public int? CountryId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("status")] public bool? Status { get; set; }
    }

    public sealed class OpenCartGeoZone
    {
        [JsonPropertyName("geo_zone_id")] public int? GeoZoneId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("date_added")] public DateTime? DateAdded { get; set; }
        [JsonPropertyName("date_modified")] public DateTime? DateModified { get; set; }
        [JsonPropertyName("geo_zone_zones")] public List<OpenCartGeoZoneZone>? GeoZoneZones { get; set; } = new();
    }

    public sealed class OpenCartGeoZoneZone
    {
        [JsonPropertyName("geo_zone_zone_id")] public int? GeoZoneZoneId { get; set; }
        [JsonPropertyName("geo_zone_id")] public int? GeoZoneId { get; set; }
        [JsonPropertyName("country_id")] public int? CountryId { get; set; }
        [JsonPropertyName("zone_id")] public int? ZoneId { get; set; }
        [JsonPropertyName("date_added")] public DateTime? DateAdded { get; set; }
        [JsonPropertyName("date_modified")] public DateTime? DateModified { get; set; }
    }

    public sealed class OpenCartLanguage
    {
        [JsonPropertyName("language_id")] public int? LanguageId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("locale")] public string? Locale { get; set; }
        [JsonPropertyName("image")] public string? Image { get; set; }
        [JsonPropertyName("directory")] public string? Directory { get; set; }
        [JsonPropertyName("filename")] public string? Filename { get; set; }
        [JsonPropertyName("sort_order")] public int? SortOrder { get; set; }
        [JsonPropertyName("status")] public bool? Status { get; set; }
    }

    public sealed class OpenCartCurrency
    {
        [JsonPropertyName("currency_id")] public int? CurrencyId { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("symbol_left")] public string? SymbolLeft { get; set; }
        [JsonPropertyName("symbol_right")] public string? SymbolRight { get; set; }
        [JsonPropertyName("decimal_place")] public int? DecimalPlace { get; set; }
        [JsonPropertyName("value")] public decimal? Value { get; set; }
        [JsonPropertyName("status")] public bool? Status { get; set; }
        [JsonPropertyName("date_modified")] public DateTime? DateModified { get; set; }
    }

    public sealed class OpenCartStockStatus
    {
        [JsonPropertyName("stock_status_id")] public int? StockStatusId { get; set; }
        [JsonPropertyName("language_id")] public int? LanguageId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    public sealed class OpenCartOrderStatus
    {
        [JsonPropertyName("order_status_id")] public int? OrderStatusId { get; set; }
        [JsonPropertyName("language_id")] public int? LanguageId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    public sealed class OpenCartTaxClass
    {
        [JsonPropertyName("tax_class_id")] public int? TaxClassId { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("date_added")] public DateTime? DateAdded { get; set; }
        [JsonPropertyName("date_modified")] public DateTime? DateModified { get; set; }
    }

    public sealed class OpenCartTaxRate
    {
        [JsonPropertyName("tax_rate_id")] public int? TaxRateId { get; set; }
        [JsonPropertyName("geo_zone_id")] public int? GeoZoneId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("rate")] public decimal? Rate { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("date_added")] public DateTime? DateAdded { get; set; }
        [JsonPropertyName("date_modified")] public DateTime? DateModified { get; set; }
    }

    public sealed class OpenCartWeightClass
    {
        [JsonPropertyName("weight_class_id")] public int? WeightClassId { get; set; }
        [JsonPropertyName("value")] public decimal? Value { get; set; }
        [JsonPropertyName("language_id")] public int? LanguageId { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("unit")] public string? Unit { get; set; }
    }

    public sealed class OpenCartLengthClass
    {
        [JsonPropertyName("length_class_id")] public int? LengthClassId { get; set; }
        [JsonPropertyName("value")] public decimal? Value { get; set; }
        [JsonPropertyName("language_id")] public int? LanguageId { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("unit")] public string? Unit { get; set; }
    }

    public sealed class OpenCartInformation
    {
        [JsonPropertyName("information_id")] public int? InformationId { get; set; }
        [JsonPropertyName("bottom")] public int? Bottom { get; set; }
        [JsonPropertyName("sort_order")] public int? SortOrder { get; set; }
        [JsonPropertyName("status")] public bool? Status { get; set; }
        [JsonPropertyName("language_id")] public int? LanguageId { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("meta_title")] public string? MetaTitle { get; set; }
        [JsonPropertyName("meta_description")] public string? MetaDescription { get; set; }
        [JsonPropertyName("meta_keyword")] public string? MetaKeyword { get; set; }
        [JsonPropertyName("store_ids")] public List<int>? StoreIds { get; set; } = new();
        [JsonPropertyName("keyword")] public string? Keyword { get; set; }
        [JsonPropertyName("layout")] public string? Layout { get; set; }
    }
}