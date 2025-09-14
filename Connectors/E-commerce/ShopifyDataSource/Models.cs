// File: BeepDM/Connectors/Ecommerce/Shopify/Models/ShopifyModels.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Ecommerce.Shopify.Models
{
    // Base
    public abstract class ShopifyEntityBase
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTimeOffset? UpdatedAt { get; set; }

        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : ShopifyEntityBase { DataSource = ds; return (T)this; }
    }

    // ---------- Core Shopify objects ----------
    public sealed class Product : ShopifyEntityBase
    {
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("body_html")] public string BodyHtml { get; set; }
        [JsonPropertyName("vendor")] public string Vendor { get; set; }
        [JsonPropertyName("product_type")] public string ProductType { get; set; }
        [JsonPropertyName("handle")] public string Handle { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; }
        [JsonPropertyName("tags")] public string Tags { get; set; }
        [JsonPropertyName("variants")] public List<Variant> Variants { get; set; } = new();
    }

    public sealed class Variant
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("product_id")] public long ProductId { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("sku")] public string Sku { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
        [JsonPropertyName("position")] public int? Position { get; set; }
        [JsonPropertyName("inventory_item_id")] public long? InventoryItemId { get; set; }
        [JsonPropertyName("inventory_quantity")] public int? InventoryQuantity { get; set; } // legacy but still present
        [JsonPropertyName("weight")] public decimal? Weight { get; set; }
        [JsonPropertyName("weight_unit")] public string WeightUnit { get; set; }
        [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTimeOffset? UpdatedAt { get; set; }

        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public Variant Attach(IDataSource ds) { DataSource = ds; return this; }
    }

    public sealed class Order : ShopifyEntityBase
    {
        [JsonPropertyName("name")] public string Name { get; set; } // e.g. "#1001"
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("currency")] public string Currency { get; set; }
        [JsonPropertyName("financial_status")] public string FinancialStatus { get; set; }
        [JsonPropertyName("fulfillment_status")] public string FulfillmentStatus { get; set; }
        [JsonPropertyName("total_price")] public decimal? TotalPrice { get; set; }
        [JsonPropertyName("subtotal_price")] public decimal? SubtotalPrice { get; set; }
        [JsonPropertyName("customer")] public Customer Customer { get; set; }
        [JsonPropertyName("line_items")] public List<OrderLineItem> LineItems { get; set; } = new();
    }

    public sealed class OrderLineItem
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("product_id")] public long? ProductId { get; set; }
        [JsonPropertyName("variant_id")] public long? VariantId { get; set; }
        [JsonPropertyName("sku")] public string Sku { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("quantity")] public int Quantity { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
    }

    public sealed class Customer : ShopifyEntityBase
    {
        [JsonPropertyName("first_name")] public string FirstName { get; set; }
        [JsonPropertyName("last_name")] public string LastName { get; set; }
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("phone")] public string Phone { get; set; }
        [JsonPropertyName("state")] public string State { get; set; }
        [JsonPropertyName("total_spent")] public decimal? TotalSpent { get; set; }
        [JsonPropertyName("orders_count")] public int? OrdersCount { get; set; }
        [JsonPropertyName("verified_email")] public bool? VerifiedEmail { get; set; }
        [JsonPropertyName("tags")] public string Tags { get; set; }
    }

    public sealed class InventoryItem
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("sku")] public string Sku { get; set; }
        [JsonPropertyName("cost")] public decimal? Cost { get; set; }
        [JsonPropertyName("tracked")] public bool? Tracked { get; set; }
        [JsonPropertyName("requires_shipping")] public bool? RequiresShipping { get; set; }
        [JsonPropertyName("country_code_of_origin")] public string CountryCodeOfOrigin { get; set; }
        [JsonPropertyName("harmonized_system_code")] public string HarmonizedSystemCode { get; set; }

        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public InventoryItem Attach(IDataSource ds) { DataSource = ds; return this; }
    }

    public sealed class InventoryLevel
    {
        [JsonPropertyName("inventory_item_id")] public long InventoryItemId { get; set; }
        [JsonPropertyName("location_id")] public long LocationId { get; set; }
        [JsonPropertyName("available")] public int? Available { get; set; }
        [JsonPropertyName("updated_at")] public DateTimeOffset? UpdatedAt { get; set; }

        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public InventoryLevel Attach(IDataSource ds) { DataSource = ds; return this; }
    }

    public sealed class Location
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("address1")] public string Address1 { get; set; }
        [JsonPropertyName("address2")] public string Address2 { get; set; }
        [JsonPropertyName("city")] public string City { get; set; }
        [JsonPropertyName("zip")] public string Zip { get; set; }
        [JsonPropertyName("province")] public string Province { get; set; }
        [JsonPropertyName("country")] public string Country { get; set; }
        [JsonPropertyName("phone")] public string Phone { get; set; }
        [JsonPropertyName("active")] public bool? Active { get; set; }
        [JsonPropertyName("legacy")] public bool? Legacy { get; set; }
        [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTimeOffset? UpdatedAt { get; set; }

        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public Location Attach(IDataSource ds) { DataSource = ds; return this; }
    }

    public sealed class CustomCollection : ShopifyEntityBase
    {
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("handle")] public string Handle { get; set; }
        [JsonPropertyName("published")] public bool? Published { get; set; }
    }

    public sealed class SmartCollection : ShopifyEntityBase
    {
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("handle")] public string Handle { get; set; }
        [JsonPropertyName("rules")] public List<CollectionRule> Rules { get; set; } = new();
    }

    public sealed class CollectionRule
    {
        [JsonPropertyName("column")] public string Column { get; set; }
        [JsonPropertyName("relation")] public string Relation { get; set; }
        [JsonPropertyName("condition")] public string Condition { get; set; }
    }

    // -------- Registry & helpers --------
    public static class ShopifyEntityRegistry
    {
        // Entity name -> CLR type
        public static readonly Dictionary<string, Type> Types = new()
        {
            ["Products"] = typeof(Product),
            ["Variants"] = typeof(Variant),
            ["Orders"] = typeof(Order),
            ["Customers"] = typeof(Customer),
            ["InventoryItems"] = typeof(InventoryItem),
            ["InventoryLevels"] = typeof(InventoryLevel),
            ["Locations"] = typeof(Location),
            ["CustomCollections"] = typeof(CustomCollection),
            ["SmartCollections"] = typeof(SmartCollection),
        };

        public static IReadOnlyList<string> Names => Types.Keys.ToList();

        // Reflection helper (for EntityStructure build)
        public static IEnumerable<(string Name, Type Type)> GetPublicProps(Type t)
            => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p =>
                {
                    var j = p.GetCustomAttribute<JsonPropertyNameAttribute>();
                    return (j?.Name ?? p.Name, p.PropertyType);
                });
    }
}
