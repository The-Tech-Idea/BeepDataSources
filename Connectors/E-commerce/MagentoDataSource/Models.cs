// File: BeepDM/Connectors/Ecommerce/Magento/Models/MagentoModels.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Connectors.Ecommerce.Magento.Models
{
    // Base
    public abstract class MagentoEntityBase
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTimeOffset? UpdatedAt { get; set; }

        [JsonIgnore] public IDataSource DataSource { get; private set; }
        public T Attach<T>(IDataSource ds) where T : MagentoEntityBase { DataSource = ds; return (T)this; }
    }

    // ---------- Core Magento objects ----------

    public sealed class Product : MagentoEntityBase
    {
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
        [JsonPropertyName("status")] public int? Status { get; set; }
        [JsonPropertyName("visibility")] public int? Visibility { get; set; }
        [JsonPropertyName("type_id")] public string? TypeId { get; set; }
        [JsonPropertyName("weight")] public decimal? Weight { get; set; }
        [JsonPropertyName("custom_attributes")] public List<CustomAttribute>? CustomAttributes { get; set; } = new();
        [JsonPropertyName("extension_attributes")] public ProductExtensionAttributes? ExtensionAttributes { get; set; }
    }

    public sealed class CustomAttribute
    {
        [JsonPropertyName("attribute_code")] public string? AttributeCode { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
    }

    public sealed class ProductExtensionAttributes
    {
        [JsonPropertyName("stock_item")] public StockItem? StockItem { get; set; }
        [JsonPropertyName("category_links")] public List<CategoryLink>? CategoryLinks { get; set; } = new();
    }

    public sealed class StockItem
    {
        [JsonPropertyName("item_id")] public int? ItemId { get; set; }
        [JsonPropertyName("product_id")] public int? ProductId { get; set; }
        [JsonPropertyName("stock_id")] public int? StockId { get; set; }
        [JsonPropertyName("qty")] public decimal? Qty { get; set; }
        [JsonPropertyName("is_in_stock")] public bool? IsInStock { get; set; }
        [JsonPropertyName("is_qty_decimal")] public bool? IsQtyDecimal { get; set; }
        [JsonPropertyName("show_default_notification_message")] public bool? ShowDefaultNotificationMessage { get; set; }
        [JsonPropertyName("use_config_min_qty")] public bool? UseConfigMinQty { get; set; }
        [JsonPropertyName("min_qty")] public decimal? MinQty { get; set; }
        [JsonPropertyName("use_config_min_sale_qty")] public bool? UseConfigMinSaleQty { get; set; }
        [JsonPropertyName("min_sale_qty")] public decimal? MinSaleQty { get; set; }
        [JsonPropertyName("use_config_max_sale_qty")] public bool? UseConfigMaxSaleQty { get; set; }
        [JsonPropertyName("max_sale_qty")] public decimal? MaxSaleQty { get; set; }
        [JsonPropertyName("use_config_backorders")] public bool? UseConfigBackorders { get; set; }
        [JsonPropertyName("backorders")] public int? Backorders { get; set; }
        [JsonPropertyName("use_config_notify_stock_qty")] public bool? UseConfigNotifyStockQty { get; set; }
        [JsonPropertyName("notify_stock_qty")] public decimal? NotifyStockQty { get; set; }
        [JsonPropertyName("use_config_qty_increments")] public bool? UseConfigQtyIncrements { get; set; }
        [JsonPropertyName("qty_increments")] public decimal? QtyIncrements { get; set; }
        [JsonPropertyName("use_config_enable_qty_inc")] public bool? UseConfigEnableQtyInc { get; set; }
        [JsonPropertyName("enable_qty_increments")] public bool? EnableQtyIncrements { get; set; }
        [JsonPropertyName("use_config_manage_stock")] public bool? UseConfigManageStock { get; set; }
        [JsonPropertyName("manage_stock")] public bool? ManageStock { get; set; }
        [JsonPropertyName("low_stock_date")] public DateTimeOffset? LowStockDate { get; set; }
        [JsonPropertyName("is_decimal_divided")] public bool? IsDecimalDivided { get; set; }
        [JsonPropertyName("stock_status_changed_auto")] public int? StockStatusChangedAuto { get; set; }
    }

    public sealed class Category : MagentoEntityBase
    {
        [JsonPropertyName("parent_id")] public int? ParentId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("is_active")] public bool? IsActive { get; set; }
        [JsonPropertyName("position")] public int? Position { get; set; }
        [JsonPropertyName("level")] public int? Level { get; set; }
        [JsonPropertyName("children")] public string? Children { get; set; }
        [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTimeOffset? UpdatedAt { get; set; }
        [JsonPropertyName("path")] public string? Path { get; set; }
        [JsonPropertyName("available_sort_by")] public List<string>? AvailableSortBy { get; set; } = new();
        [JsonPropertyName("include_in_menu")] public bool? IncludeInMenu { get; set; }
        [JsonPropertyName("extension_attributes")] public CategoryExtensionAttributes? ExtensionAttributes { get; set; }
        [JsonPropertyName("custom_attributes")] public List<CustomAttribute>? CustomAttributes { get; set; } = new();
    }

    public sealed class CategoryLink
    {
        [JsonPropertyName("position")] public int? Position { get; set; }
        [JsonPropertyName("category_id")] public string? CategoryId { get; set; }
    }

    public sealed class CategoryExtensionAttributes
    {
        // Add extension attributes as needed
    }

    public sealed class Order : MagentoEntityBase
    {
        [JsonPropertyName("increment_id")] public string? IncrementId { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("state")] public string? State { get; set; }
        [JsonPropertyName("customer_id")] public int? CustomerId { get; set; }
        [JsonPropertyName("customer_email")] public string? CustomerEmail { get; set; }
        [JsonPropertyName("customer_is_guest")] public int? CustomerIsGuest { get; set; }
        [JsonPropertyName("customer_note")] public string? CustomerNote { get; set; }
        [JsonPropertyName("customer_note_notify")] public int? CustomerNoteNotify { get; set; }
        [JsonPropertyName("customer_group_id")] public int? CustomerGroupId { get; set; }
        [JsonPropertyName("store_id")] public int? StoreId { get; set; }
        [JsonPropertyName("store_name")] public string? StoreName { get; set; }
        [JsonPropertyName("grand_total")] public decimal? GrandTotal { get; set; }
        [JsonPropertyName("base_grand_total")] public decimal? BaseGrandTotal { get; set; }
        [JsonPropertyName("subtotal")] public decimal? Subtotal { get; set; }
        [JsonPropertyName("base_subtotal")] public decimal? BaseSubtotal { get; set; }
        [JsonPropertyName("tax_amount")] public decimal? TaxAmount { get; set; }
        [JsonPropertyName("base_tax_amount")] public decimal? BaseTaxAmount { get; set; }
        [JsonPropertyName("shipping_amount")] public decimal? ShippingAmount { get; set; }
        [JsonPropertyName("base_shipping_amount")] public decimal? BaseShippingAmount { get; set; }
        [JsonPropertyName("discount_amount")] public decimal? DiscountAmount { get; set; }
        [JsonPropertyName("base_discount_amount")] public decimal? BaseDiscountAmount { get; set; }
        [JsonPropertyName("shipping_tax_amount")] public decimal? ShippingTaxAmount { get; set; }
        [JsonPropertyName("base_shipping_tax_amount")] public decimal? BaseShippingTaxAmount { get; set; }
        [JsonPropertyName("shipping_discount_tax_compensation_amount")] public decimal? ShippingDiscountTaxCompensationAmount { get; set; }
        [JsonPropertyName("base_shipping_discount_tax_compensation_amnt")] public decimal? BaseShippingDiscountTaxCompensationAmnt { get; set; }
        [JsonPropertyName("shipping_incl_tax")] public decimal? ShippingInclTax { get; set; }
        [JsonPropertyName("base_shipping_incl_tax")] public decimal? BaseShippingInclTax { get; set; }
        [JsonPropertyName("items")] public List<OrderItem>? Items { get; set; } = new();
        [JsonPropertyName("billing_address")] public OrderAddress? BillingAddress { get; set; }
        [JsonPropertyName("payment_authorization_amount")] public decimal? PaymentAuthorizationAmount { get; set; }
        [JsonPropertyName("extension_attributes")] public OrderExtensionAttributes? ExtensionAttributes { get; set; }
    }

    public sealed class OrderItem
    {
        [JsonPropertyName("item_id")] public int? ItemId { get; set; }
        [JsonPropertyName("order_id")] public int? OrderId { get; set; }
        [JsonPropertyName("parent_item_id")] public int? ParentItemId { get; set; }
        [JsonPropertyName("quote_item_id")] public int? QuoteItemId { get; set; }
        [JsonPropertyName("store_id")] public int? StoreId { get; set; }
        [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTimeOffset? UpdatedAt { get; set; }
        [JsonPropertyName("product_id")] public int? ProductId { get; set; }
        [JsonPropertyName("product_type")] public string? ProductType { get; set; }
        [JsonPropertyName("product_options")] public string? ProductOptions { get; set; }
        [JsonPropertyName("weight")] public decimal? Weight { get; set; }
        [JsonPropertyName("is_virtual")] public int? IsVirtual { get; set; }
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("applied_rule_ids")] public string? AppliedRuleIds { get; set; }
        [JsonPropertyName("additional_data")] public string? AdditionalData { get; set; }
        [JsonPropertyName("is_qty_decimal")] public int? IsQtyDecimal { get; set; }
        [JsonPropertyName("no_discount")] public int? NoDiscount { get; set; }
        [JsonPropertyName("qty_backordered")] public decimal? QtyBackordered { get; set; }
        [JsonPropertyName("qty_canceled")] public decimal? QtyCanceled { get; set; }
        [JsonPropertyName("qty_invoiced")] public decimal? QtyInvoiced { get; set; }
        [JsonPropertyName("qty_ordered")] public decimal? QtyOrdered { get; set; }
        [JsonPropertyName("qty_refunded")] public decimal? QtyRefunded { get; set; }
        [JsonPropertyName("qty_shipped")] public decimal? QtyShipped { get; set; }
        [JsonPropertyName("base_cost")] public decimal? BaseCost { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
        [JsonPropertyName("base_price")] public decimal? BasePrice { get; set; }
        [JsonPropertyName("original_price")] public decimal? OriginalPrice { get; set; }
        [JsonPropertyName("base_original_price")] public decimal? BaseOriginalPrice { get; set; }
        [JsonPropertyName("tax_percent")] public decimal? TaxPercent { get; set; }
        [JsonPropertyName("tax_amount")] public decimal? TaxAmount { get; set; }
        [JsonPropertyName("base_tax_amount")] public decimal? BaseTaxAmount { get; set; }
        [JsonPropertyName("tax_invoiced")] public decimal? TaxInvoiced { get; set; }
        [JsonPropertyName("base_tax_invoiced")] public decimal? BaseTaxInvoiced { get; set; }
        [JsonPropertyName("discount_percent")] public decimal? DiscountPercent { get; set; }
        [JsonPropertyName("discount_amount")] public decimal? DiscountAmount { get; set; }
        [JsonPropertyName("base_discount_amount")] public decimal? BaseDiscountAmount { get; set; }
        [JsonPropertyName("discount_invoiced")] public decimal? DiscountInvoiced { get; set; }
        [JsonPropertyName("base_discount_invoiced")] public decimal? BaseDiscountInvoiced { get; set; }
        [JsonPropertyName("amount_refunded")] public decimal? AmountRefunded { get; set; }
        [JsonPropertyName("base_amount_refunded")] public decimal? BaseAmountRefunded { get; set; }
        [JsonPropertyName("row_total")] public decimal? RowTotal { get; set; }
        [JsonPropertyName("base_row_total")] public decimal? BaseRowTotal { get; set; }
        [JsonPropertyName("row_invoiced")] public decimal? RowInvoiced { get; set; }
        [JsonPropertyName("base_row_invoiced")] public decimal? BaseRowInvoiced { get; set; }
        [JsonPropertyName("row_weight")] public decimal? RowWeight { get; set; }
    }

    public sealed class OrderAddress
    {
        [JsonPropertyName("address_type")] public string? AddressType { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("company")] public string? Company { get; set; }
        [JsonPropertyName("country_id")] public string? CountryId { get; set; }
        [JsonPropertyName("customer_address_id")] public int? CustomerAddressId { get; set; }
        [JsonPropertyName("customer_id")] public int? CustomerId { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("entity_id")] public int? EntityId { get; set; }
        [JsonPropertyName("fax")] public string? Fax { get; set; }
        [JsonPropertyName("firstname")] public string? Firstname { get; set; }
        [JsonPropertyName("lastname")] public string? Lastname { get; set; }
        [JsonPropertyName("middlename")] public string? Middlename { get; set; }
        [JsonPropertyName("parent_id")] public int? ParentId { get; set; }
        [JsonPropertyName("postcode")] public string? Postcode { get; set; }
        [JsonPropertyName("prefix")] public string? Prefix { get; set; }
        [JsonPropertyName("region")] public string? Region { get; set; }
        [JsonPropertyName("region_code")] public string? RegionCode { get; set; }
        [JsonPropertyName("region_id")] public int? RegionId { get; set; }
        [JsonPropertyName("street")] public List<string>? Street { get; set; } = new();
        [JsonPropertyName("suffix")] public string? Suffix { get; set; }
        [JsonPropertyName("telephone")] public string? Telephone { get; set; }
        [JsonPropertyName("vat_id")] public string? VatId { get; set; }
        [JsonPropertyName("vat_is_valid")] public int? VatIsValid { get; set; }
        [JsonPropertyName("vat_request_date")] public string? VatRequestDate { get; set; }
        [JsonPropertyName("vat_request_id")] public string? VatRequestId { get; set; }
        [JsonPropertyName("vat_request_success")] public int? VatRequestSuccess { get; set; }
    }

    public sealed class OrderExtensionAttributes
    {
        [JsonPropertyName("shipping_assignments")] public List<ShippingAssignment>? ShippingAssignments { get; set; } = new();
        [JsonPropertyName("payment_additional_info")] public List<PaymentAdditionalInfo>? PaymentAdditionalInfo { get; set; } = new();
        [JsonPropertyName("applied_taxes")] public List<OrderTaxDetails>? AppliedTaxes { get; set; } = new();
        [JsonPropertyName("item_applied_taxes")] public List<OrderItemAppliedTax>? ItemAppliedTaxes { get; set; } = new();
    }

    public sealed class ShippingAssignment
    {
        [JsonPropertyName("shipping")] public Shipping? Shipping { get; set; }
        [JsonPropertyName("items")] public List<OrderItem>? Items { get; set; } = new();
        [JsonPropertyName("stock_id")] public int? StockId { get; set; }
        [JsonPropertyName("stock_name")] public string? StockName { get; set; }
    }

    public sealed class Shipping
    {
        [JsonPropertyName("address")] public OrderAddress? Address { get; set; }
        [JsonPropertyName("method")] public string? Method { get; set; }
        [JsonPropertyName("total")] public ShippingTotal? Total { get; set; }
    }

    public sealed class ShippingTotal
    {
        [JsonPropertyName("base_shipping_amount")] public decimal? BaseShippingAmount { get; set; }
        [JsonPropertyName("base_shipping_discount_amount")] public decimal? BaseShippingDiscountAmount { get; set; }
        [JsonPropertyName("base_shipping_discount_tax_amount")] public decimal? BaseShippingDiscountTaxAmount { get; set; }
        [JsonPropertyName("base_shipping_incl_tax")] public decimal? BaseShippingInclTax { get; set; }
        [JsonPropertyName("base_shipping_invoiced")] public decimal? BaseShippingInvoiced { get; set; }
        [JsonPropertyName("base_shipping_tax_amount")] public decimal? BaseShippingTaxAmount { get; set; }
        [JsonPropertyName("shipping_amount")] public decimal? ShippingAmount { get; set; }
        [JsonPropertyName("shipping_discount_amount")] public decimal? ShippingDiscountAmount { get; set; }
        [JsonPropertyName("shipping_discount_tax_amount")] public decimal? ShippingDiscountTaxAmount { get; set; }
        [JsonPropertyName("shipping_incl_tax")] public decimal? ShippingInclTax { get; set; }
        [JsonPropertyName("shipping_invoiced")] public decimal? ShippingInvoiced { get; set; }
        [JsonPropertyName("shipping_tax_amount")] public decimal? ShippingTaxAmount { get; set; }
    }

    public sealed class PaymentAdditionalInfo
    {
        [JsonPropertyName("key")] public string? Key { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
    }

    public sealed class OrderTaxDetails
    {
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("percent")] public decimal? Percent { get; set; }
        [JsonPropertyName("amount")] public decimal? Amount { get; set; }
        [JsonPropertyName("base_amount")] public decimal? BaseAmount { get; set; }
        [JsonPropertyName("position")] public int? Position { get; set; }
        [JsonPropertyName("base_real_amount")] public decimal? BaseRealAmount { get; set; }
    }

    public sealed class OrderItemAppliedTax
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("item_id")] public int? ItemId { get; set; }
        [JsonPropertyName("associated_item_id")] public int? AssociatedItemId { get; set; }
        [JsonPropertyName("applied_taxes")] public List<OrderTaxDetails>? AppliedTaxes { get; set; } = new();
    }

    public sealed class Customer : MagentoEntityBase
    {
        [JsonPropertyName("group_id")] public int? GroupId { get; set; }
        [JsonPropertyName("default_billing")] public string? DefaultBilling { get; set; }
        [JsonPropertyName("default_shipping")] public string? DefaultShipping { get; set; }
        [JsonPropertyName("confirmation")] public string? Confirmation { get; set; }
        [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTimeOffset? UpdatedAt { get; set; }
        [JsonPropertyName("created_in")] public string? CreatedIn { get; set; }
        [JsonPropertyName("dob")] public DateTimeOffset? Dob { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("firstname")] public string? Firstname { get; set; }
        [JsonPropertyName("lastname")] public string? Lastname { get; set; }
        [JsonPropertyName("middlename")] public string? Middlename { get; set; }
        [JsonPropertyName("prefix")] public string? Prefix { get; set; }
        [JsonPropertyName("suffix")] public string? Suffix { get; set; }
        [JsonPropertyName("gender")] public int? Gender { get; set; }
        [JsonPropertyName("store_id")] public int? StoreId { get; set; }
        [JsonPropertyName("taxvat")] public string? Taxvat { get; set; }
        [JsonPropertyName("website_id")] public int? WebsiteId { get; set; }
        [JsonPropertyName("addresses")] public List<CustomerAddress>? Addresses { get; set; } = new();
        [JsonPropertyName("disable_auto_group_change")] public int? DisableAutoGroupChange { get; set; }
        [JsonPropertyName("extension_attributes")] public CustomerExtensionAttributes? ExtensionAttributes { get; set; }
        [JsonPropertyName("custom_attributes")] public List<CustomAttribute>? CustomAttributes { get; set; } = new();
    }

    public sealed class CustomerAddress
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("customer_id")] public int? CustomerId { get; set; }
        [JsonPropertyName("region")] public AddressRegion? Region { get; set; }
        [JsonPropertyName("region_id")] public int? RegionId { get; set; }
        [JsonPropertyName("country_id")] public string? CountryId { get; set; }
        [JsonPropertyName("street")] public List<string>? Street { get; set; } = new();
        [JsonPropertyName("telephone")] public string? Telephone { get; set; }
        [JsonPropertyName("postcode")] public string? Postcode { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("firstname")] public string? Firstname { get; set; }
        [JsonPropertyName("lastname")] public string? Lastname { get; set; }
        [JsonPropertyName("default_shipping")] public bool? DefaultShipping { get; set; }
        [JsonPropertyName("default_billing")] public bool? DefaultBilling { get; set; }
    }

    public sealed class AddressRegion
    {
        [JsonPropertyName("region_code")] public string? RegionCode { get; set; }
        [JsonPropertyName("region")] public string? Region { get; set; }
        [JsonPropertyName("region_id")] public int? RegionId { get; set; }
        [JsonPropertyName("extension_attributes")] public Dictionary<string, object>? ExtensionAttributes { get; set; } = new();
    }

    public sealed class CustomerExtensionAttributes
    {
        [JsonPropertyName("company_attributes")] public CompanyAttributes? CompanyAttributes { get; set; }
        [JsonPropertyName("is_subscribed")] public bool? IsSubscribed { get; set; }
    }

    public sealed class CompanyAttributes
    {
        [JsonPropertyName("customer_id")] public int? CustomerId { get; set; }
        [JsonPropertyName("company_id")] public int? CompanyId { get; set; }
        [JsonPropertyName("status")] public int? Status { get; set; }
        [JsonPropertyName("job_title")] public string? JobTitle { get; set; }
        [JsonPropertyName("telephone")] public string? Telephone { get; set; }
    }

    public sealed class InventorySourceItem
    {
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("source_code")] public string? SourceCode { get; set; }
        [JsonPropertyName("quantity")] public decimal? Quantity { get; set; }
        [JsonPropertyName("status")] public int? Status { get; set; }
        [JsonPropertyName("extension_attributes")] public InventoryExtensionAttributes? ExtensionAttributes { get; set; }
    }

    public sealed class InventoryExtensionAttributes
    {
        // Add extension attributes as needed
    }

    public sealed class Cart
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTimeOffset? UpdatedAt { get; set; }
        [JsonPropertyName("is_active")] public bool? IsActive { get; set; }
        [JsonPropertyName("is_virtual")] public bool? IsVirtual { get; set; }
        [JsonPropertyName("items")] public List<CartItem>? Items { get; set; } = new();
        [JsonPropertyName("items_count")] public int? ItemsCount { get; set; }
        [JsonPropertyName("items_qty")] public decimal? ItemsQty { get; set; }
        [JsonPropertyName("customer")] public CartCustomer? Customer { get; set; }
        [JsonPropertyName("billing_address")] public CartAddress? BillingAddress { get; set; }
        [JsonPropertyName("orig_order_id")] public int? OrigOrderId { get; set; }
        [JsonPropertyName("currency")] public CartCurrency? Currency { get; set; }
        [JsonPropertyName("customer_is_guest")] public bool? CustomerIsGuest { get; set; }
        [JsonPropertyName("customer_note")] public string? CustomerNote { get; set; }
        [JsonPropertyName("customer_note_notify")] public int? CustomerNoteNotify { get; set; }
        [JsonPropertyName("customer_tax_class_id")] public int? CustomerTaxClassId { get; set; }
        [JsonPropertyName("store_id")] public int? StoreId { get; set; }
        [JsonPropertyName("extension_attributes")] public CartExtensionAttributes? ExtensionAttributes { get; set; }
    }

    public sealed class CartItem
    {
        [JsonPropertyName("item_id")] public int? ItemId { get; set; }
        [JsonPropertyName("sku")] public string? Sku { get; set; }
        [JsonPropertyName("qty")] public decimal? Qty { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
        [JsonPropertyName("product_type")] public string? ProductType { get; set; }
        [JsonPropertyName("quote_id")] public string? QuoteId { get; set; }
        [JsonPropertyName("extension_attributes")] public CartItemExtensionAttributes? ExtensionAttributes { get; set; }
    }

    public sealed class CartItemExtensionAttributes
    {
        // Add extension attributes as needed
    }

    public sealed class CartCustomer
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("group_id")] public int? GroupId { get; set; }
        [JsonPropertyName("default_billing")] public string? DefaultBilling { get; set; }
        [JsonPropertyName("default_shipping")] public string? DefaultShipping { get; set; }
        [JsonPropertyName("confirmation")] public string? Confirmation { get; set; }
        [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTimeOffset? UpdatedAt { get; set; }
        [JsonPropertyName("created_in")] public string? CreatedIn { get; set; }
        [JsonPropertyName("dob")] public DateTimeOffset? Dob { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("firstname")] public string? Firstname { get; set; }
        [JsonPropertyName("lastname")] public string? Lastname { get; set; }
        [JsonPropertyName("middlename")] public string? Middlename { get; set; }
        [JsonPropertyName("prefix")] public string? Prefix { get; set; }
        [JsonPropertyName("suffix")] public string? Suffix { get; set; }
        [JsonPropertyName("gender")] public int? Gender { get; set; }
        [JsonPropertyName("store_id")] public int? StoreId { get; set; }
        [JsonPropertyName("taxvat")] public string? Taxvat { get; set; }
        [JsonPropertyName("website_id")] public int? WebsiteId { get; set; }
        [JsonPropertyName("addresses")] public List<CustomerAddress>? Addresses { get; set; } = new();
        [JsonPropertyName("disable_auto_group_change")] public int? DisableAutoGroupChange { get; set; }
        [JsonPropertyName("extension_attributes")] public CustomerExtensionAttributes? ExtensionAttributes { get; set; }
        [JsonPropertyName("custom_attributes")] public List<CustomAttribute>? CustomAttributes { get; set; } = new();
    }

    public sealed class CartAddress
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("region")] public string? Region { get; set; }
        [JsonPropertyName("region_id")] public int? RegionId { get; set; }
        [JsonPropertyName("region_code")] public string? RegionCode { get; set; }
        [JsonPropertyName("country_id")] public string? CountryId { get; set; }
        [JsonPropertyName("street")] public List<string>? Street { get; set; } = new();
        [JsonPropertyName("telephone")] public string? Telephone { get; set; }
        [JsonPropertyName("postcode")] public string? Postcode { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("firstname")] public string? Firstname { get; set; }
        [JsonPropertyName("lastname")] public string? Lastname { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("same_as_billing")] public int? SameAsBilling { get; set; }
        [JsonPropertyName("save_in_address_book")] public int? SaveInAddressBook { get; set; }
        [JsonPropertyName("extension_attributes")] public Dictionary<string, object>? ExtensionAttributes { get; set; } = new();
        [JsonPropertyName("vat_id")] public string? VatId { get; set; }
    }

    public sealed class CartCurrency
    {
        [JsonPropertyName("global_currency_code")] public string? GlobalCurrencyCode { get; set; }
        [JsonPropertyName("base_currency_code")] public string? BaseCurrencyCode { get; set; }
        [JsonPropertyName("store_currency_code")] public string? StoreCurrencyCode { get; set; }
        [JsonPropertyName("quote_currency_code")] public string? QuoteCurrencyCode { get; set; }
        [JsonPropertyName("store_to_base_rate")] public decimal? StoreToBaseRate { get; set; }
        [JsonPropertyName("store_to_quote_rate")] public decimal? StoreToQuoteRate { get; set; }
        [JsonPropertyName("base_to_global_rate")] public decimal? BaseToGlobalRate { get; set; }
        [JsonPropertyName("base_to_quote_rate")] public decimal? BaseToQuoteRate { get; set; }
    }

    public sealed class CartExtensionAttributes
    {
        [JsonPropertyName("shipping_assignments")] public List<ShippingAssignment>? ShippingAssignments { get; set; } = new();
        [JsonPropertyName("negotiable_quote")] public NegotiableQuote? NegotiableQuote { get; set; }
    }

    public sealed class NegotiableQuote
    {
        [JsonPropertyName("quote_id")] public int? QuoteId { get; set; }
        [JsonPropertyName("is_regular_quote")] public bool? IsRegularQuote { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("negotiated_price_type")] public int? NegotiatedPriceType { get; set; }
        [JsonPropertyName("negotiated_price_value")] public decimal? NegotiatedPriceValue { get; set; }
    }

    public sealed class Review
    {
        [JsonPropertyName("review_id")] public int? ReviewId { get; set; }
        [JsonPropertyName("created_at")] public DateTimeOffset? CreatedAt { get; set; }
        [JsonPropertyName("entity_id")] public int? EntityId { get; set; }
        [JsonPropertyName("entity_pk_value")] public int? EntityPkValue { get; set; }
        [JsonPropertyName("status_id")] public int? StatusId { get; set; }
        [JsonPropertyName("detail_id")] public int? DetailId { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("detail")] public string? Detail { get; set; }
        [JsonPropertyName("nickname")] public string? Nickname { get; set; }
        [JsonPropertyName("customer_id")] public int? CustomerId { get; set; }
        [JsonPropertyName("entity_code")] public string? EntityCode { get; set; }
        [JsonPropertyName("rating_votes")] public List<RatingVote>? RatingVotes { get; set; } = new();
        [JsonPropertyName("review_entity")] public string? ReviewEntity { get; set; }
        [JsonPropertyName("review_status")] public int? ReviewStatus { get; set; }
        [JsonPropertyName("review_type")] public int? ReviewType { get; set; }
    }

    public sealed class RatingVote
    {
        [JsonPropertyName("vote_id")] public int? VoteId { get; set; }
        [JsonPropertyName("review_id")] public int? ReviewId { get; set; }
        [JsonPropertyName("rating_id")] public int? RatingId { get; set; }
        [JsonPropertyName("percent")] public int? Percent { get; set; }
        [JsonPropertyName("value")] public int? Value { get; set; }
        [JsonPropertyName("rating_code")] public string? RatingCode { get; set; }
    }

    public sealed class StoreConfig
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("website_id")] public int? WebsiteId { get; set; }
        [JsonPropertyName("store_group_id")] public int? StoreGroupId { get; set; }
        [JsonPropertyName("is_active")] public int? IsActive { get; set; }
        [JsonPropertyName("extension_attributes")] public StoreConfigExtensionAttributes? ExtensionAttributes { get; set; }
    }

    public sealed class StoreConfigExtensionAttributes
    {
        // Add extension attributes as needed
    }

    public sealed class ProductAttribute
    {
        [JsonPropertyName("attribute_id")] public int? AttributeId { get; set; }
        [JsonPropertyName("attribute_code")] public string? AttributeCode { get; set; }
        [JsonPropertyName("frontend_input")] public string? FrontendInput { get; set; }
        [JsonPropertyName("entity_type_id")] public string? EntityTypeId { get; set; }
        [JsonPropertyName("is_required")] public bool? IsRequired { get; set; }
        [JsonPropertyName("is_user_defined")] public bool? IsUserDefined { get; set; }
        [JsonPropertyName("default_frontend_label")] public string? DefaultFrontendLabel { get; set; }
        [JsonPropertyName("frontend_labels")] public List<AttributeLabel>? FrontendLabels { get; set; } = new();
        [JsonPropertyName("note")] public string? Note { get; set; }
        [JsonPropertyName("backend_type")] public string? BackendType { get; set; }
        [JsonPropertyName("backend_model")] public string? BackendModel { get; set; }
        [JsonPropertyName("source_model")] public string? SourceModel { get; set; }
        [JsonPropertyName("default_value")] public string? DefaultValue { get; set; }
        [JsonPropertyName("is_unique")] public string? IsUnique { get; set; }
        [JsonPropertyName("frontend_class")] public string? FrontendClass { get; set; }
        [JsonPropertyName("validation_rules")] public List<string>? ValidationRules { get; set; } = new();
        [JsonPropertyName("custom_attributes")] public List<CustomAttribute>? CustomAttributes { get; set; } = new();
        [JsonPropertyName("options")] public List<AttributeOption>? Options { get; set; } = new();
    }

    public sealed class AttributeLabel
    {
        [JsonPropertyName("store_id")] public int? StoreId { get; set; }
        [JsonPropertyName("label")] public string? Label { get; set; }
    }

    public sealed class AttributeOption
    {
        [JsonPropertyName("label")] public string? Label { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
        [JsonPropertyName("sort_order")] public int? SortOrder { get; set; }
        [JsonPropertyName("is_default")] public bool? IsDefault { get; set; }
        [JsonPropertyName("store_labels")] public List<AttributeLabel>? StoreLabels { get; set; } = new();
    }

    public sealed class TaxRule
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("priority")] public int? Priority { get; set; }
        [JsonPropertyName("position")] public int? Position { get; set; }
        [JsonPropertyName("customer_tax_class_ids")] public List<int>? CustomerTaxClassIds { get; set; } = new();
        [JsonPropertyName("product_tax_class_ids")] public List<int>? ProductTaxClassIds { get; set; } = new();
        [JsonPropertyName("tax_rate_ids")] public List<int>? TaxRateIds { get; set; } = new();
        [JsonPropertyName("calculate_subtotal")] public bool? CalculateSubtotal { get; set; }
    }
}