// File: Connectors/Ecommerce/WooCommerce/Models/WooCommerceModels.cs
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Ecommerce.WooCommerce.Models
{
    public static class WooEntityRegistry
    {
        // Logical entity name -> CLR type
        public static readonly Dictionary<string, Type> Types = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Products"] = typeof(WooProduct),
            ["ProductVariations"] = typeof(WooProductVariation),

            ["Orders"] = typeof(WooOrder),
            ["Customers"] = typeof(WooCustomer),

            ["Coupons"] = typeof(WooCoupon),
            ["Categories"] = typeof(WooCategory),
            ["Reviews"] = typeof(WooReview),

            ["Taxes"] = typeof(WooTax),
            ["TaxClasses"] = typeof(WooTaxClass),

            ["ShippingZones"] = typeof(WooShippingZone),
            ["ShippingMethods"] = typeof(WooShippingMethod),

            ["Attributes"] = typeof(WooAttribute),
        };

        public static IReadOnlyList<string> Names => Types.Keys.ToList();

        public static Type Resolve(string entityName)
            => entityName != null && Types.TryGetValue(entityName, out var t) ? t : null;
    }
    // =======================================================
    // Base
    // =======================================================
    public abstract class WooEntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : WooEntityBase { DataSource = ds; return (T)this; }
    }

    // =======================================================
    // Products
    // =======================================================
    public sealed class WooProduct : WooEntityBase
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("slug")] public string Slug { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }          // simple, variable, grouped, external
        [JsonPropertyName("status")] public string Status { get; set; }        // draft, pending, private, publish
        [JsonPropertyName("permalink")] public string Permalink { get; set; }
        [JsonPropertyName("sku")] public string Sku { get; set; }
        [JsonPropertyName("price")] public string Price { get; set; }
        [JsonPropertyName("regular_price")] public string RegularPrice { get; set; }
        [JsonPropertyName("sale_price")] public string SalePrice { get; set; }
        [JsonPropertyName("on_sale")] public bool? OnSale { get; set; }
        [JsonPropertyName("purchasable")] public bool? Purchasable { get; set; }
        [JsonPropertyName("total_sales")] public int? TotalSales { get; set; }
        [JsonPropertyName("date_created")] public DateTimeOffset? DateCreated { get; set; }
        [JsonPropertyName("date_modified")] public DateTimeOffset? DateModified { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("short_description")] public string ShortDescription { get; set; }
        [JsonPropertyName("virtual")] public bool? Virtual { get; set; }
        [JsonPropertyName("downloadable")] public bool? Downloadable { get; set; }
        [JsonPropertyName("stock_status")] public string StockStatus { get; set; }   // instock, outofstock, onbackorder
        [JsonPropertyName("manage_stock")] public bool? ManageStock { get; set; }
        [JsonPropertyName("stock_quantity")] public int? StockQuantity { get; set; }
        [JsonPropertyName("weight")] public string Weight { get; set; }
        [JsonPropertyName("dimensions")] public WooDimensions Dimensions { get; set; }

        [JsonPropertyName("categories")] public List<WooTermRef> Categories { get; set; } = new();
        [JsonPropertyName("images")] public List<WooImage> Images { get; set; } = new();
        [JsonPropertyName("attributes")] public List<WooProductAttribute> Attributes { get; set; } = new();
        [JsonPropertyName("default_attributes")] public List<WooProductDefaultAttribute> DefaultAttributes { get; set; } = new();
        [JsonPropertyName("variations")] public List<long> Variations { get; set; } = new(); // IDs (for variable products)
    }

    public sealed class WooDimensions
    {
        [JsonPropertyName("length")] public string Length { get; set; }
        [JsonPropertyName("width")] public string Width { get; set; }
        [JsonPropertyName("height")] public string Height { get; set; }
    }

    public sealed class WooTermRef
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("slug")] public string Slug { get; set; }
    }

    public sealed class WooImage
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("date_created")] public DateTimeOffset? DateCreated { get; set; }
        [JsonPropertyName("src")] public string Src { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("alt")] public string Alt { get; set; }
    }

    public sealed class WooProductAttribute
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("position")] public int? Position { get; set; }
        [JsonPropertyName("visible")] public bool? Visible { get; set; }
        [JsonPropertyName("variation")] public bool? Variation { get; set; }
        [JsonPropertyName("options")] public List<string> Options { get; set; } = new();
    }

    public sealed class WooProductDefaultAttribute
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("option")] public string Option { get; set; }
    }

    // =======================================================
    // Product Variations
    // =======================================================
    public sealed class WooProductVariation : WooEntityBase
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("product_id")] public long? ProductId { get; set; }
        [JsonPropertyName("sku")] public string Sku { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("price")] public string Price { get; set; }
        [JsonPropertyName("regular_price")] public string RegularPrice { get; set; }
        [JsonPropertyName("sale_price")] public string SalePrice { get; set; }
        [JsonPropertyName("on_sale")] public bool? OnSale { get; set; }
        [JsonPropertyName("stock_status")] public string StockStatus { get; set; }
        [JsonPropertyName("manage_stock")] public bool? ManageStock { get; set; }
        [JsonPropertyName("stock_quantity")] public int? StockQuantity { get; set; }
        [JsonPropertyName("image")] public WooImage Image { get; set; }
        [JsonPropertyName("attributes")] public List<WooProductAttributeTerm> Attributes { get; set; } = new();
    }

    public sealed class WooProductAttributeTerm
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("option")] public string Option { get; set; }
    }

    // =======================================================
    // Orders
    // =======================================================
    public sealed class WooOrder : WooEntityBase
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("number")] public string Number { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("currency")] public string Currency { get; set; }
        [JsonPropertyName("date_created")] public DateTimeOffset? DateCreated { get; set; }
        [JsonPropertyName("date_modified")] public DateTimeOffset? DateModified { get; set; }
        [JsonPropertyName("customer_id")] public long? CustomerId { get; set; }
        [JsonPropertyName("billing")] public WooBilling Billing { get; set; }
        [JsonPropertyName("shipping")] public WooShipping Shipping { get; set; }

        [JsonPropertyName("payment_method")] public string PaymentMethod { get; set; }
        [JsonPropertyName("payment_method_title")] public string PaymentMethodTitle { get; set; }
        [JsonPropertyName("transaction_id")] public string TransactionId { get; set; }

        [JsonPropertyName("total")] public string Total { get; set; }
        [JsonPropertyName("total_tax")] public string TotalTax { get; set; }
        [JsonPropertyName("shipping_total")] public string ShippingTotal { get; set; }
        [JsonPropertyName("discount_total")] public string DiscountTotal { get; set; }

        [JsonPropertyName("customer_note")] public string CustomerNote { get; set; }

        [JsonPropertyName("line_items")] public List<WooOrderLineItem> LineItems { get; set; } = new();
        [JsonPropertyName("shipping_lines")] public List<WooShippingLine> ShippingLines { get; set; } = new();
        [JsonPropertyName("fee_lines")] public List<WooFeeLine> FeeLines { get; set; } = new();
        [JsonPropertyName("coupon_lines")] public List<WooCouponLine> CouponLines { get; set; } = new();
        [JsonPropertyName("tax_lines")] public List<WooTaxLine> TaxLines { get; set; } = new();
    }

    public sealed class WooBilling
    {
        [JsonPropertyName("first_name")] public string FirstName { get; set; }
        [JsonPropertyName("last_name")] public string LastName { get; set; }
        [JsonPropertyName("company")] public string Company { get; set; }
        [JsonPropertyName("address_1")] public string Address1 { get; set; }
        [JsonPropertyName("address_2")] public string Address2 { get; set; }
        [JsonPropertyName("city")] public string City { get; set; }
        [JsonPropertyName("state")] public string State { get; set; }
        [JsonPropertyName("postcode")] public string Postcode { get; set; }
        [JsonPropertyName("country")] public string Country { get; set; }
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("phone")] public string Phone { get; set; }
    }

    public sealed class WooShipping
    {
        [JsonPropertyName("first_name")] public string FirstName { get; set; }
        [JsonPropertyName("last_name")] public string LastName { get; set; }
        [JsonPropertyName("company")] public string Company { get; set; }
        [JsonPropertyName("address_1")] public string Address1 { get; set; }
        [JsonPropertyName("address_2")] public string Address2 { get; set; }
        [JsonPropertyName("city")] public string City { get; set; }
        [JsonPropertyName("state")] public string State { get; set; }
        [JsonPropertyName("postcode")] public string Postcode { get; set; }
        [JsonPropertyName("country")] public string Country { get; set; }
    }

    public sealed class WooOrderLineItem
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("product_id")] public long? ProductId { get; set; }
        [JsonPropertyName("variation_id")] public long? VariationId { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("subtotal")] public string Subtotal { get; set; }
        [JsonPropertyName("total")] public string Total { get; set; }
        [JsonPropertyName("tax_class")] public string TaxClass { get; set; }
        [JsonPropertyName("subtotal_tax")] public string SubtotalTax { get; set; }
        [JsonPropertyName("total_tax")] public string TotalTax { get; set; }
        [JsonPropertyName("sku")] public string Sku { get; set; }
        [JsonPropertyName("price")] public string Price { get; set; }
        [JsonPropertyName("meta_data")] public List<WooMetaData> MetaData { get; set; } = new();
    }

    public sealed class WooShippingLine
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("method_title")] public string MethodTitle { get; set; }
        [JsonPropertyName("method_id")] public string MethodId { get; set; }
        [JsonPropertyName("total")] public string Total { get; set; }
        [JsonPropertyName("total_tax")] public string TotalTax { get; set; }
        [JsonPropertyName("meta_data")] public List<WooMetaData> MetaData { get; set; } = new();
    }

    public sealed class WooFeeLine
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("tax_class")] public string TaxClass { get; set; }
        [JsonPropertyName("tax_status")] public string TaxStatus { get; set; }
        [JsonPropertyName("total")] public string Total { get; set; }
        [JsonPropertyName("total_tax")] public string TotalTax { get; set; }
        [JsonPropertyName("meta_data")] public List<WooMetaData> MetaData { get; set; } = new();
    }

    public sealed class WooCouponLine
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("code")] public string Code { get; set; }
        [JsonPropertyName("discount")] public string Discount { get; set; }
        [JsonPropertyName("discount_tax")] public string DiscountTax { get; set; }
        [JsonPropertyName("meta_data")] public List<WooMetaData> MetaData { get; set; } = new();
    }

    public sealed class WooTaxLine
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("rate_code")] public string RateCode { get; set; }
        [JsonPropertyName("rate_id")] public long? RateId { get; set; }
        [JsonPropertyName("label")] public string Label { get; set; }
        [JsonPropertyName("compound")] public bool? Compound { get; set; }
        [JsonPropertyName("tax_total")] public string TaxTotal { get; set; }
        [JsonPropertyName("shipping_tax_total")] public string ShippingTaxTotal { get; set; }
        [JsonPropertyName("meta_data")] public List<WooMetaData> MetaData { get; set; } = new();
    }

    public sealed class WooMetaData
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("key")] public string Key { get; set; }
        [JsonPropertyName("value")] public object Value { get; set; }
    }

    // =======================================================
    // Customers
    // =======================================================
    public sealed class WooCustomer : WooEntityBase
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("first_name")] public string FirstName { get; set; }
        [JsonPropertyName("last_name")] public string LastName { get; set; }
        [JsonPropertyName("username")] public string Username { get; set; }
        [JsonPropertyName("role")] public string Role { get; set; }
        [JsonPropertyName("date_created")] public DateTimeOffset? DateCreated { get; set; }
        [JsonPropertyName("date_modified")] public DateTimeOffset? DateModified { get; set; }
        [JsonPropertyName("is_paying_customer")] public bool? IsPayingCustomer { get; set; }
        [JsonPropertyName("avatar_url")] public string AvatarUrl { get; set; }
        [JsonPropertyName("billing")] public WooBilling Billing { get; set; }
        [JsonPropertyName("shipping")] public WooShipping Shipping { get; set; }
        [JsonPropertyName("meta_data")] public List<WooMetaData> MetaData { get; set; } = new();
    }

    // =======================================================
    // Coupons
    // =======================================================
    public sealed class WooCoupon : WooEntityBase
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("code")] public string Code { get; set; }
        [JsonPropertyName("amount")] public string Amount { get; set; }
        [JsonPropertyName("discount_type")] public string DiscountType { get; set; } // fixed_cart, percent, fixed_product
        [JsonPropertyName("date_created")] public DateTimeOffset? DateCreated { get; set; }
        [JsonPropertyName("date_modified")] public DateTimeOffset? DateModified { get; set; }
        [JsonPropertyName("date_expires")] public DateTimeOffset? DateExpires { get; set; }
        [JsonPropertyName("individual_use")] public bool? IndividualUse { get; set; }
        [JsonPropertyName("usage_limit")] public int? UsageLimit { get; set; }
        [JsonPropertyName("usage_count")] public int? UsageCount { get; set; }
        [JsonPropertyName("free_shipping")] public bool? FreeShipping { get; set; }
        [JsonPropertyName("product_ids")] public List<long> ProductIds { get; set; } = new();
        [JsonPropertyName("excluded_product_ids")] public List<long> ExcludedProductIds { get; set; } = new();
        [JsonPropertyName("minimum_amount")] public string MinimumAmount { get; set; }
        [JsonPropertyName("maximum_amount")] public string MaximumAmount { get; set; }
    }

    // =======================================================
    // Categories
    // =======================================================
    public sealed class WooCategory : WooEntityBase
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("slug")] public string Slug { get; set; }
        [JsonPropertyName("parent")] public long? Parent { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("display")] public string Display { get; set; }
        [JsonPropertyName("image")] public WooImage Image { get; set; }
        [JsonPropertyName("menu_order")] public int? MenuOrder { get; set; }
        [JsonPropertyName("count")] public int? Count { get; set; }
    }

    // =======================================================
    // Reviews
    // =======================================================
    public sealed class WooReview : WooEntityBase
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("date_created")] public DateTimeOffset? DateCreated { get; set; }
        [JsonPropertyName("product_id")] public long? ProductId { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }   // approved, hold, spam
        [JsonPropertyName("reviewer")] public string Reviewer { get; set; }
        [JsonPropertyName("reviewer_email")] public string ReviewerEmail { get; set; }
        [JsonPropertyName("review")] public string Review { get; set; }
        [JsonPropertyName("rating")] public int? Rating { get; set; }
        [JsonPropertyName("verified")] public bool? Verified { get; set; }
    }

    // =======================================================
    // Taxes & Tax Classes
    // =======================================================
    public sealed class WooTax : WooEntityBase
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("country")] public string Country { get; set; }
        [JsonPropertyName("state")] public string State { get; set; }
        [JsonPropertyName("postcode")] public string Postcode { get; set; }
        [JsonPropertyName("city")] public string City { get; set; }
        [JsonPropertyName("rate")] public string Rate { get; set; } // percentage as string
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("priority")] public int? Priority { get; set; }
        [JsonPropertyName("compound")] public bool? Compound { get; set; }
        [JsonPropertyName("shipping")] public bool? Shipping { get; set; }
        [JsonPropertyName("order")] public int? Order { get; set; }
        [JsonPropertyName("class")] public string Class { get; set; } // e.g., "standard", "reduced-rate"
    }

    public sealed class WooTaxClass : WooEntityBase
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("slug")] public string Slug { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
    }

    // =======================================================
    // Shipping Zones & Methods
    // =======================================================
    public sealed class WooShippingZone : WooEntityBase
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("order")] public int? Order { get; set; }
        [JsonPropertyName("locations")] public List<WooShippingZoneLocation> Locations { get; set; } = new();
    }

    public sealed class WooShippingZoneLocation
    {
        [JsonPropertyName("code")] public string Code { get; set; } // e.g., country/state/postcode code
        [JsonPropertyName("type")] public string Type { get; set; } // "country","state","postcode","continent"
    }

    // Global shipping methods list (or zone methods via zones/{id}/methods)
    public sealed class WooShippingMethod : WooEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }     // method id (e.g., "flat_rate")
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
        [JsonPropertyName("method_id")] public string MethodId { get; set; }   // for zone methods
        [JsonPropertyName("instance_id")] public long? InstanceId { get; set; }  // for zone methods
        [JsonPropertyName("settings")] public Dictionary<string, object> Settings { get; set; } = new();
    }

    // =======================================================
    // Product Attributes (taxonomy)
    // =======================================================
    public sealed class WooAttribute : WooEntityBase
    {
        [JsonPropertyName("id")] public long? Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("slug")] public string Slug { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }       // select, text
        [JsonPropertyName("order_by")] public string OrderBy { get; set; }    // menu_order, name, name_num, id
        [JsonPropertyName("has_archives")] public bool? HasArchives { get; set; }
    }
}
