// File: Connectors/Ecommerce/Volusion/Models/VolusionModels.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Ecommerce.Volusion.Models
{
    // Base to optionally carry a reference back to the IDataSource (if useful)
    public abstract class VolusionEntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : VolusionEntityBase { DataSource = ds; return (T)this; }
    }

    // ---------- Shared/Value types ----------
    public sealed class VAddress
    {
        [JsonPropertyName("first_name")] public string FirstName { get; set; }
        [JsonPropertyName("last_name")] public string LastName { get; set; }
        [JsonPropertyName("company")] public string Company { get; set; }
        [JsonPropertyName("phone")] public string Phone { get; set; }
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("address1")] public string Address1 { get; set; }
        [JsonPropertyName("address2")] public string Address2 { get; set; }
        [JsonPropertyName("city")] public string City { get; set; }
        [JsonPropertyName("state")] public string State { get; set; }
        [JsonPropertyName("postal_code")] public string PostalCode { get; set; }
        [JsonPropertyName("country")] public string Country { get; set; }
    }

    public sealed class VImage
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("alt")] public string Alt { get; set; }
        [JsonPropertyName("is_primary")] public bool? IsPrimary { get; set; }
        [JsonPropertyName("sort_order")] public int? SortOrder { get; set; }
    }

    // ---------- Catalog ----------
    public sealed class VProduct : VolusionEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("product_code")] public string ProductCode { get; set; }
        [JsonPropertyName("sku")] public string Sku { get; set; }
        [JsonPropertyName("upc")] public string Upc { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("short_description")] public string ShortDescription { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
        [JsonPropertyName("sale_price")] public decimal? SalePrice { get; set; }
        [JsonPropertyName("cost")] public decimal? Cost { get; set; }
        [JsonPropertyName("currency")] public string Currency { get; set; }
        [JsonPropertyName("weight")] public decimal? Weight { get; set; }
        [JsonPropertyName("length")] public decimal? Length { get; set; }
        [JsonPropertyName("width")] public decimal? Width { get; set; }
        [JsonPropertyName("height")] public decimal? Height { get; set; }
        [JsonPropertyName("inventory_level")] public int? InventoryLevel { get; set; }
        [JsonPropertyName("inventory_tracking")] public string InventoryTracking { get; set; } // none, product, variant
        [JsonPropertyName("is_active")] public bool? IsActive { get; set; }
        [JsonPropertyName("is_visible")] public bool? IsVisible { get; set; }
        [JsonPropertyName("vendor_id")] public string VendorId { get; set; }
        [JsonPropertyName("category_ids")] public List<string> CategoryIds { get; set; } = new();
        [JsonPropertyName("images")] public List<VImage> Images { get; set; } = new();
        [JsonPropertyName("date_created")] public DateTimeOffset? DateCreated { get; set; }
        [JsonPropertyName("date_modified")] public DateTimeOffset? DateModified { get; set; }
    }

    public sealed class VCategory : VolusionEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("parent_category_id")] public string ParentCategoryId { get; set; }
        [JsonPropertyName("is_visible")] public bool? IsVisible { get; set; }
        [JsonPropertyName("sort_order")] public int? SortOrder { get; set; }
        [JsonPropertyName("date_created")] public DateTimeOffset? DateCreated { get; set; }
        [JsonPropertyName("date_modified")] public DateTimeOffset? DateModified { get; set; }
    }

    public sealed class VVendor : VolusionEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("phone")] public string Phone { get; set; }
        [JsonPropertyName("url")] public string Url { get; set; }
    }

    // ---------- Customers ----------
    public sealed class VCustomer : VolusionEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("first_name")] public string FirstName { get; set; }
        [JsonPropertyName("last_name")] public string LastName { get; set; }
        [JsonPropertyName("phone")] public string Phone { get; set; }
        [JsonPropertyName("company")] public string Company { get; set; }
        [JsonPropertyName("billing_address")] public VAddress BillingAddress { get; set; }
        [JsonPropertyName("shipping_address")] public VAddress ShippingAddress { get; set; }
        [JsonPropertyName("date_created")] public DateTimeOffset? DateCreated { get; set; }
        [JsonPropertyName("date_modified")] public DateTimeOffset? DateModified { get; set; }
        [JsonPropertyName("last_login")] public DateTimeOffset? LastLogin { get; set; }
        [JsonPropertyName("notes")] public string Notes { get; set; }
    }

    // ---------- Orders ----------
    public sealed class VOrderItem
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("order_id")] public string OrderId { get; set; }
        [JsonPropertyName("product_id")] public string ProductId { get; set; }
        [JsonPropertyName("sku")] public string Sku { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("unit_price")] public decimal? UnitPrice { get; set; }
        [JsonPropertyName("total")] public decimal? Total { get; set; }
        [JsonPropertyName("variant_options")] public Dictionary<string, string> VariantOptions { get; set; } = new();
    }

    public sealed class VShipmentItem
    {
        [JsonPropertyName("order_item_id")] public string OrderItemId { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
    }

    public sealed class VShipment : VolusionEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("order_id")] public string OrderId { get; set; }
        [JsonPropertyName("carrier")] public string Carrier { get; set; }
        [JsonPropertyName("service")] public string Service { get; set; }
        [JsonPropertyName("tracking_number")] public string TrackingNumber { get; set; }
        [JsonPropertyName("shipped_date")] public DateTimeOffset? ShippedDate { get; set; }
        [JsonPropertyName("items")] public List<VShipmentItem> Items { get; set; } = new();
    }

    public sealed class VOrder : VolusionEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("order_number")] public string OrderNumber { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("payment_status")] public string PaymentStatus { get; set; }
        [JsonPropertyName("fulfillment_status")] public string FulfillmentStatus { get; set; }
        [JsonPropertyName("customer_id")] public string CustomerId { get; set; }
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("billing_address")] public VAddress BillingAddress { get; set; }
        [JsonPropertyName("shipping_address")] public VAddress ShippingAddress { get; set; }
        [JsonPropertyName("shipping_method")] public string ShippingMethod { get; set; }
        [JsonPropertyName("subtotal")] public decimal? Subtotal { get; set; }
        [JsonPropertyName("discount_total")] public decimal? DiscountTotal { get; set; }
        [JsonPropertyName("shipping_total")] public decimal? ShippingTotal { get; set; }
        [JsonPropertyName("tax_total")] public decimal? TaxTotal { get; set; }
        [JsonPropertyName("total")] public decimal? Total { get; set; }
        [JsonPropertyName("currency")] public string Currency { get; set; }
        [JsonPropertyName("date_created")] public DateTimeOffset? DateCreated { get; set; }
        [JsonPropertyName("date_modified")] public DateTimeOffset? DateModified { get; set; }
        [JsonPropertyName("items")] public List<VOrderItem> Items { get; set; } = new();
        [JsonPropertyName("shipments")] public List<VShipment> Shipments { get; set; } = new();
        [JsonPropertyName("notes")] public string Notes { get; set; }
    }

    // ---------- Promotions ----------
    public sealed class VCoupon : VolusionEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("code")] public string Code { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("discount_type")] public string DiscountType { get; set; } // percent/fixed
        [JsonPropertyName("value")] public decimal? Value { get; set; }
        [JsonPropertyName("starts_at")] public DateTimeOffset? StartsAt { get; set; }
        [JsonPropertyName("ends_at")] public DateTimeOffset? EndsAt { get; set; }
        [JsonPropertyName("is_active")] public bool? IsActive { get; set; }
        [JsonPropertyName("usage_limit")] public int? UsageLimit { get; set; }
    }

    // ---------- Registry (entity name -> CLR type) ----------
    public static class VolusionEntityRegistry
    {
        public static readonly Dictionary<string, Type> Types = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Products"] = typeof(VProduct),
            ["Categories"] = typeof(VCategory),
            ["Customers"] = typeof(VCustomer),
            ["Orders"] = typeof(VOrder),
            ["OrderItems"] = typeof(VOrderItem),
            ["Shipments"] = typeof(VShipment),
            ["Vendors"] = typeof(VVendor),
            ["Coupons"] = typeof(VCoupon),
            ["ProductImages"] = typeof(VImage)
        };

        public static IReadOnlyList<string> Names => new List<string>(Types.Keys);
        public static Type Resolve(string entityName) =>
            entityName != null && Types.TryGetValue(entityName, out var t) ? t : null;
    }
}
