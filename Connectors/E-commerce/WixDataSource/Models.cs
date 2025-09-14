// File: Connectors/Wix/Models/WixModels.cs
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Wix.Models
{
    // -------------------------------------------------------
    // Base
    // -------------------------------------------------------
    public abstract class WixEntityBase
    {
        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : WixEntityBase { DataSource = ds; return (T)this; }
    }

    // =======================================================
    // PRODUCTS
    // =======================================================
    public sealed class WixProduct : WixEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("slug")] public string Slug { get; set; }
        [JsonPropertyName("productType")] public string ProductType { get; set; } // e.g., "physical","digital"
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("sku")] public string Sku { get; set; }
        [JsonPropertyName("visible")] public bool? Visible { get; set; }

        [JsonPropertyName("priceData")] public WixPriceData PriceData { get; set; }
        [JsonPropertyName("inventory")] public WixInventory Inventory { get; set; }

        [JsonPropertyName("mainMedia")] public WixMediaRef MainMedia { get; set; }
        [JsonPropertyName("mediaItems")] public List<WixMediaItem> MediaItems { get; set; } = new();

        [JsonPropertyName("collectionIds")] public List<string> CollectionIds { get; set; } = new();
        [JsonPropertyName("variants")] public List<WixVariant> Variants { get; set; } = new();
        [JsonPropertyName("customFields")] public Dictionary<string, object> CustomFields { get; set; } = new();
    }

    public sealed class WixPriceData
    {
        [JsonPropertyName("price")] public decimal? Price { get; set; }
        [JsonPropertyName("discountedPrice")] public decimal? DiscountedPrice { get; set; }
        [JsonPropertyName("currency")] public string Currency { get; set; }
        [JsonPropertyName("formatted")] public string Formatted { get; set; }
    }

    public sealed class WixInventory
    {
        [JsonPropertyName("trackQuantity")] public bool? TrackQuantity { get; set; }
        [JsonPropertyName("inStock")] public bool? InStock { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("availableForPreOrder")] public bool? AvailableForPreOrder { get; set; }
    }

    public sealed class WixVariant
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("sku")] public string Sku { get; set; }
        [JsonPropertyName("priceData")] public WixPriceData PriceData { get; set; }
        [JsonPropertyName("inStock")] public bool? InStock { get; set; }
        [JsonPropertyName("choices")] public Dictionary<string, string> Choices { get; set; } = new(); // e.g. Color=Red, Size=M
    }

    public sealed class WixMediaRef
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("url")] public string Url { get; set; }
    }

    public sealed class WixMediaItem
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("url")] public string Url { get; set; }
        [JsonPropertyName("width")] public int? Width { get; set; }
        [JsonPropertyName("height")] public int? Height { get; set; }
        [JsonPropertyName("altText")] public string AltText { get; set; }
    }

    // =======================================================
    // ORDERS
    // =======================================================
    public sealed class WixOrder : WixEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("number")] public string Number { get; set; }
        [JsonPropertyName("currency")] public string Currency { get; set; }

        [JsonPropertyName("createdDate")] public DateTimeOffset? CreatedDate { get; set; }
        [JsonPropertyName("updatedDate")] public DateTimeOffset? UpdatedDate { get; set; }

        [JsonPropertyName("paymentStatus")] public string PaymentStatus { get; set; }      // e.g., "PAID","UNPAID","PARTIALLY_REFUNDED"
        [JsonPropertyName("fulfillmentStatus")] public string FulfillmentStatus { get; set; }  // e.g., "FULFILLED","NOT_FULFILLED"

        [JsonPropertyName("buyerInfo")] public WixBuyerInfo BuyerInfo { get; set; }
        [JsonPropertyName("billingInfo")] public WixBillingInfo BillingInfo { get; set; }
        [JsonPropertyName("shippingInfo")] public WixShippingInfo ShippingInfo { get; set; }

        [JsonPropertyName("totals")] public WixOrderTotals Totals { get; set; }
        [JsonPropertyName("lineItems")] public List<WixOrderLineItem> LineItems { get; set; } = new();

        [JsonPropertyName("notes")] public string Notes { get; set; }
        [JsonPropertyName("customFields")] public Dictionary<string, object> CustomFields { get; set; } = new();
    }

    public sealed class WixBuyerInfo
    {
        [JsonPropertyName("contactId")] public string ContactId { get; set; }
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("firstName")] public string FirstName { get; set; }
        [JsonPropertyName("lastName")] public string LastName { get; set; }
        [JsonPropertyName("phone")] public string Phone { get; set; }
    }

    public sealed class WixBillingInfo
    {
        [JsonPropertyName("address")] public WixAddress Address { get; set; }
        [JsonPropertyName("vatId")] public string VatId { get; set; }
        [JsonPropertyName("companyName")] public string CompanyName { get; set; }
    }

    public sealed class WixShippingInfo
    {
        [JsonPropertyName("shippingRegion")] public string ShippingRegion { get; set; }
        [JsonPropertyName("address")] public WixAddress Address { get; set; }
        [JsonPropertyName("deliveryOption")] public string DeliveryOption { get; set; }
        [JsonPropertyName("shippingProvider")] public string ShippingProvider { get; set; }
        [JsonPropertyName("trackingNumber")] public string TrackingNumber { get; set; }
        [JsonPropertyName("trackingLink")] public string TrackingLink { get; set; }
    }

    public sealed class WixOrderLineItem
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("productId")] public string ProductId { get; set; }
        [JsonPropertyName("variantId")] public string VariantId { get; set; }
        [JsonPropertyName("sku")] public string Sku { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("priceData")] public WixPriceData PriceData { get; set; }
        [JsonPropertyName("discounts")] public List<WixDiscount> Discounts { get; set; } = new();
        [JsonPropertyName("customFields")] public Dictionary<string, object> CustomFields { get; set; } = new();
    }

    public sealed class WixOrderTotals
    {
        [JsonPropertyName("subtotal")] public decimal? Subtotal { get; set; }
        [JsonPropertyName("tax")] public decimal? Tax { get; set; }
        [JsonPropertyName("shipping")] public decimal? Shipping { get; set; }
        [JsonPropertyName("discount")] public decimal? Discount { get; set; }
        [JsonPropertyName("total")] public decimal? Total { get; set; }
    }

    public sealed class WixDiscount
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("amount")] public decimal? Amount { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; } // e.g., "COUPON","MANUAL"
    }

    // =======================================================
    // COLLECTIONS
    // =======================================================
    public sealed class WixCollection : WixEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("slug")] public string Slug { get; set; }
        [JsonPropertyName("visible")] public bool? Visible { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("parentId")] public string ParentId { get; set; }
    }

    // =======================================================
    // CONTACTS (Wix CRM)
    // =======================================================
    public sealed class WixContact : WixEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("firstName")] public string FirstName { get; set; }
        [JsonPropertyName("lastName")] public string LastName { get; set; }
        [JsonPropertyName("emails")] public List<WixEmail> Emails { get; set; } = new();
        [JsonPropertyName("phones")] public List<WixPhone> Phones { get; set; } = new();
        [JsonPropertyName("addresses")] public List<WixAddress> Addresses { get; set; } = new();
        [JsonPropertyName("company")] public string Company { get; set; }
        [JsonPropertyName("picture")] public string Picture { get; set; }
        [JsonPropertyName("tags")] public List<string> Tags { get; set; } = new();
        [JsonPropertyName("source")] public string Source { get; set; }
        [JsonPropertyName("birthday")] public DateTimeOffset? Birthday { get; set; }
    }

    public sealed class WixEmail
    {
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("primary")] public bool? Primary { get; set; }
        [JsonPropertyName("tag")] public string Tag { get; set; } // e.g., "WORK","HOME"
    }

    public sealed class WixPhone
    {
        [JsonPropertyName("phone")] public string Phone { get; set; }
        [JsonPropertyName("primary")] public bool? Primary { get; set; }
        [JsonPropertyName("tag")] public string Tag { get; set; }
    }

    public sealed class WixAddress
    {
        [JsonPropertyName("addressLine1")] public string AddressLine1 { get; set; }
        [JsonPropertyName("addressLine2")] public string AddressLine2 { get; set; }
        [JsonPropertyName("city")] public string City { get; set; }
        [JsonPropertyName("region")] public string Region { get; set; }
        [JsonPropertyName("country")] public string Country { get; set; }
        [JsonPropertyName("postalCode")] public string PostalCode { get; set; }
    }

    // =======================================================
    // COUPONS
    // =======================================================
    public sealed class WixCoupon : WixEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("code")] public string Code { get; set; }
        [JsonPropertyName("discountType")] public string DiscountType { get; set; } // "AMOUNT","PERCENT"
        [JsonPropertyName("amount")] public decimal? Amount { get; set; }
        [JsonPropertyName("percentOff")] public decimal? PercentOff { get; set; }
        [JsonPropertyName("startTime")] public DateTimeOffset? StartTime { get; set; }
        [JsonPropertyName("endTime")] public DateTimeOffset? EndTime { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; } // "ACTIVE","EXPIRED"
        [JsonPropertyName("appliesTo")] public string AppliesTo { get; set; } // e.g., "ALL","SPECIFIC_PRODUCTS"
    }

    // =======================================================
    // INVENTORY ITEMS
    // =======================================================
    public sealed class WixInventoryItem : WixEntityBase
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("productId")] public string ProductId { get; set; }
        [JsonPropertyName("variantId")] public string VariantId { get; set; }
        [JsonPropertyName("sku")] public string Sku { get; set; }
        [JsonPropertyName("inStock")] public bool? InStock { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
    }

    // =======================================================
    // REGISTRY (optional mapping entity name -> CLR type)
    // =======================================================
    public static class WixEntityRegistry
    {
        public static readonly Dictionary<string, Type> Types = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Products"] = typeof(WixProduct),
            ["Orders"] = typeof(WixOrder),
            ["Collections"] = typeof(WixCollection),
            ["Contacts"] = typeof(WixContact),
            ["Coupons"] = typeof(WixCoupon),
            ["InventoryItems"] = typeof(WixInventoryItem)
        };

        public static IReadOnlyList<string> Names => new List<string>(Types.Keys);
    }
}
