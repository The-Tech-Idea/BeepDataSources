using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WooCommerceDataSourceCore
{
    // Supporting classes for complex types

    public class Download
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("file")]
        public string File { get; set; }
    }

    public class Dimensions
    {
        [JsonProperty("length")]
        public string Length { get; set; }

        [JsonProperty("width")]
        public string Width { get; set; }

        [JsonProperty("height")]
        public string Height { get; set; }
    }

    public class Category
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }
    }

    public class Tag
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }
    }

    public class Image
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("date_created")]
        public DateTime DateCreated { get; set; }

        [JsonProperty("date_created_gmt")]
        public DateTime DateCreatedGmt { get; set; }

        [JsonProperty("date_modified")]
        public DateTime DateModified { get; set; }

        [JsonProperty("date_modified_gmt")]
        public DateTime DateModifiedGmt { get; set; }

        [JsonProperty("src")]
        public string Src { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("alt")]
        public string Alt { get; set; }
    }

    public class MetaData
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
    public class LineItem
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("variation_id")]
        public int VariationId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("tax_class")]
        public int TaxClass { get; set; }

        [JsonProperty("subtotal")]
        public string Subtotal { get; set; }

        [JsonProperty("subtotal_tax")]
        public string SubtotalTax { get; set; }

        [JsonProperty("total")]
        public string Total { get; set; }

        [JsonProperty("total_tax")]
        public string TotalTax { get; set; }

        [JsonProperty("taxes")]
        public List<Tax> Taxes { get; set; } = new List<Tax>();

        [JsonProperty("meta_data")]
        public List<MetaData> MetaData { get; set; } = new List<MetaData>();

        [JsonProperty("sku")]
        public string Sku { get; set; }

        [JsonProperty("price")]
        public string Price { get; set; }

        [JsonProperty("refund_total")]
        public decimal RefundTotal { get; set; }
    }

    public class Tax
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("total")]
        public string Total { get; set; }

        [JsonProperty("subtotal")]
        public string Subtotal { get; set; }

        [JsonProperty("refund_total")]
        public decimal RefundTotal { get; set; }
    }
    public class BillingAddress
    {
        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonProperty("company")]
        public string Company { get; set; }

        [JsonProperty("address_1")]
        public string Address1 { get; set; }

        [JsonProperty("address_2")]
        public string Address2 { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("postcode")]
        public string Postcode { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }
    }

    public class ShippingAddress
    {
        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonProperty("company")]
        public string Company { get; set; }

        [JsonProperty("address_1")]
        public string Address1 { get; set; }

        [JsonProperty("address_2")]
        public string Address2 { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("postcode")]
        public string Postcode { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }
    }

    public class TaxLine
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("rate_code")]
        public string RateCode { get; set; }

        [JsonProperty("rate_id")]
        public string RateId { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("compound")]
        public bool Compound { get; set; }

        [JsonProperty("tax_total")]
        public string TaxTotal { get; set; }

        [JsonProperty("shipping_tax_total")]
        public string ShippingTaxTotal { get; set; }

        [JsonProperty("meta_data")]
        public List<MetaData> MetaData { get; set; }
    }

    public class ShippingLine
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("method_title")]
        public string MethodTitle { get; set; }

        [JsonProperty("method_id")]
        public string MethodId { get; set; }

        [JsonProperty("total")]
        public string Total { get; set; }

        [JsonProperty("total_tax")]
        public string TotalTax { get; set; }

        [JsonProperty("taxes")]
        public List<Tax> Taxes { get; set; }

        [JsonProperty("meta_data")]
        public List<MetaData> MetaData { get; set; }
    }

    public class FeeLine
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("tax_class")]
        public string TaxClass { get; set; }

        [JsonProperty("tax_status")]
        public string TaxStatus { get; set; }

        [JsonProperty("total")]
        public string Total { get; set; }

        [JsonProperty("total_tax")]
        public string TotalTax { get; set; }

        [JsonProperty("taxes")]
        public List<Tax> Taxes { get; set; }

        [JsonProperty("meta_data")]
        public List<MetaData> MetaData { get; set; }
    }

    public class CouponLine
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("discount")]
        public string Discount { get; set; }

        [JsonProperty("discount_tax")]
        public string DiscountTax { get; set; }

        [JsonProperty("meta_data")]
        public List<MetaData> MetaData { get; set; }
    }



}
