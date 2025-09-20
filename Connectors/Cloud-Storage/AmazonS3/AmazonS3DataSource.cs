using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Report;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.WebAPI;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace TheTechIdea.Beep.Connectors.AmazonS3
{
    /// <summary>
    /// Amazon S3 data source implementation using WebAPIDataSource as base class
    /// Supports S3 buckets, objects, versions, and multipart uploads
    /// </summary>
    [AddinAttribute(Category = DatasourceCategory.Connector, DatasourceType = DataSourceType.AmazonS3)]
    public class AmazonS3DataSource : WebAPIDataSource
    {
        // Entity endpoints mapping for Amazon S3 API
        private static readonly Dictionary<string, string> EntityEndpoints = new(StringComparer.OrdinalIgnoreCase)
        {
            // Bucket operations
            ["buckets"] = "https://s3.{region}.amazonaws.com/",
            ["bucket"] = "https://s3.{region}.amazonaws.com/{bucket_name}",
            ["bucket_versioning"] = "https://s3.{region}.amazonaws.com/{bucket_name}?versioning",
            ["bucket_encryption"] = "https://s3.{region}.amazonaws.com/{bucket_name}?encryption",
            ["bucket_policy"] = "https://s3.{region}.amazonaws.com/{bucket_name}?policy",
            ["bucket_cors"] = "https://s3.{region}.amazonaws.com/{bucket_name}?cors",
            ["bucket_lifecycle"] = "https://s3.{region}.amazonaws.com/{bucket_name}?lifecycle",
            ["bucket_tags"] = "https://s3.{region}.amazonaws.com/{bucket_name}?tagging",

            // Object operations
            ["objects"] = "https://s3.{region}.amazonaws.com/{bucket_name}",
            ["object"] = "https://s3.{region}.amazonaws.com/{bucket_name}/{object_key}",
            ["object_versions"] = "https://s3.{region}.amazonaws.com/{bucket_name}/{object_key}?versions",
            ["object_acl"] = "https://s3.{region}.amazonaws.com/{bucket_name}/{object_key}?acl",
            ["object_tags"] = "https://s3.{region}.amazonaws.com/{bucket_name}/{object_key}?tagging",
            ["object_metadata"] = "https://s3.{region}.amazonaws.com/{bucket_name}/{object_key}?metadata",

            // Multipart upload operations
            ["multipart_uploads"] = "https://s3.{region}.amazonaws.com/{bucket_name}?uploads",
            ["multipart_upload"] = "https://s3.{region}.amazonaws.com/{bucket_name}/{object_key}?uploadId={upload_id}",
            ["multipart_parts"] = "https://s3.{region}.amazonaws.com/{bucket_name}/{object_key}?uploadId={upload_id}&partNumber={part_number}",

            // Batch operations
            ["batch_delete"] = "https://s3.{region}.amazonaws.com/{bucket_name}?delete"
        };

        // Required filters for each entity
        private static readonly Dictionary<string, string[]> RequiredFilters = new(StringComparer.OrdinalIgnoreCase)
        {
            // Bucket operations
            ["buckets"] = Array.Empty<string>(),
            ["bucket"] = new[] { "bucket_name" },
            ["bucket_versioning"] = new[] { "bucket_name" },
            ["bucket_encryption"] = new[] { "bucket_name" },
            ["bucket_policy"] = new[] { "bucket_name" },
            ["bucket_cors"] = new[] { "bucket_name" },
            ["bucket_lifecycle"] = new[] { "bucket_name" },
            ["bucket_tags"] = new[] { "bucket_name" },

            // Object operations
            ["objects"] = new[] { "bucket_name" },
            ["object"] = new[] { "bucket_name", "object_key" },
            ["object_versions"] = new[] { "bucket_name", "object_key" },
            ["object_acl"] = new[] { "bucket_name", "object_key" },
            ["object_tags"] = new[] { "bucket_name", "object_key" },
            ["object_metadata"] = new[] { "bucket_name", "object_key" },

            // Multipart upload operations
            ["multipart_uploads"] = new[] { "bucket_name" },
            ["multipart_upload"] = new[] { "bucket_name", "object_key", "upload_id" },
            ["multipart_parts"] = new[] { "bucket_name", "object_key", "upload_id", "part_number" },

            // Batch operations
            ["batch_delete"] = new[] { "bucket_name" }
        };

        public AmazonS3DataSource(string datasourcename, IDMLogger logger, IDMEEditor dmeEditor, DataSourceType databasetype, IErrorsInfo errorObject) : base(datasourcename, logger, dmeEditor, databasetype, errorObject)
        {
            // Ensure WebAPI connection props exist
            if (Dataconnection?.ConnectionProp is not WebAPIConnectionProperties)
            {
                if (Dataconnection != null)
                    Dataconnection.ConnectionProp = new WebAPIConnectionProperties();
            }

            // Register entities
            EntitiesNames = EntityEndpoints.Keys.ToList();
            Entities = EntitiesNames
                .Select(n => new EntityStructure { EntityName = n, DatasourceEntityName = n })
                .ToList();
        }

        // Return the fixed list
        public new IEnumerable<string> GetEntitesList() => EntitiesNames;

        // -------------------- Overrides (same signatures) --------------------

        // Sync
        public override IEnumerable<object> GetEntity(string EntityName, List<AppFilter> filter)
        {
            var data = GetEntityAsync(EntityName, filter).ConfigureAwait(false).GetAwaiter().GetResult();
            return data ?? Array.Empty<object>();
        }

        // Async
        public override async Task<IEnumerable<object>> GetEntityAsync(string EntityName, List<AppFilter> Filter)
        {
            if (!EntityEndpoints.TryGetValue(EntityName, out var endpoint))
                throw new InvalidOperationException($"Unknown Amazon S3 entity '{EntityName}'.");

            var q = FiltersToQuery(Filter);
            RequireFilters(EntityName, q, RequiredFilters.GetValueOrDefault(EntityName, Array.Empty<string>()));

            var resolvedEndpoint = ResolveEndpoint(endpoint, q);

            using var resp = await GetAsync(resolvedEndpoint, q).ConfigureAwait(false);
            if (resp is null || !resp.IsSuccessStatusCode) return Array.Empty<object>();

            return ExtractArray(resp, EntityName);
        }

        // ---------------------------- helpers ----------------------------

        private static Dictionary<string, string> FiltersToQuery(List<AppFilter> filters)
        {
            var q = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (filters == null) return q;
            foreach (var f in filters)
            {
                if (f == null || string.IsNullOrWhiteSpace(f.FieldName)) continue;
                q[f.FieldName.Trim()] = f.FilterValue?.ToString() ?? string.Empty;
            }
            return q;
        }

        private static void RequireFilters(string entity, Dictionary<string, string> q, string[] required)
        {
            if (required == null || required.Length == 0) return;
            var missing = required.Where(r => !q.ContainsKey(r) || string.IsNullOrWhiteSpace(q[r])).ToList();
            if (missing.Count > 0)
                throw new ArgumentException($"Amazon S3 entity '{entity}' requires parameter(s): {string.Join(", ", missing)}.");
        }

        private static string ResolveEndpoint(string template, Dictionary<string, string> q)
        {
            var result = template;

            // Handle region
            if (result.Contains("{region}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("region", out var region) || string.IsNullOrWhiteSpace(region))
                    region = "us-east-1"; // Default region
                result = result.Replace("{region}", Uri.EscapeDataString(region));
            }

            // Handle bucket_name
            if (result.Contains("{bucket_name}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("bucket_name", out var bucketName) || string.IsNullOrWhiteSpace(bucketName))
                    throw new ArgumentException("Missing required 'bucket_name' filter for this endpoint.");
                result = result.Replace("{bucket_name}", Uri.EscapeDataString(bucketName));
            }

            // Handle object_key
            if (result.Contains("{object_key}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("object_key", out var objectKey) || string.IsNullOrWhiteSpace(objectKey))
                    throw new ArgumentException("Missing required 'object_key' filter for this endpoint.");
                result = result.Replace("{object_key}", Uri.EscapeDataString(objectKey));
            }

            // Handle upload_id
            if (result.Contains("{upload_id}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("upload_id", out var uploadId) || string.IsNullOrWhiteSpace(uploadId))
                    throw new ArgumentException("Missing required 'upload_id' filter for this endpoint.");
                result = result.Replace("{upload_id}", Uri.EscapeDataString(uploadId));
            }

            // Handle part_number
            if (result.Contains("{part_number}", StringComparison.Ordinal))
            {
                if (!q.TryGetValue("part_number", out var partNumber) || string.IsNullOrWhiteSpace(partNumber))
                    throw new ArgumentException("Missing required 'part_number' filter for this endpoint.");
                result = result.Replace("{part_number}", Uri.EscapeDataString(partNumber));
            }

            return result;
        }

        private IEnumerable<object> ExtractArray(HttpResponseMessage resp, string entityName)
        {
            try
            {
                var content = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                if (string.IsNullOrWhiteSpace(content)) return Array.Empty<object>();

                return entityName.ToLowerInvariant() switch
                {
                    "buckets" => ExtractBuckets(content),
                    "objects" => ExtractObjects(content),
                    "object_versions" => ExtractObjectVersions(content),
                    _ => ExtractGeneric(content, entityName)
                };
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error extracting array for {entityName}: {ex.Message}");
                return Array.Empty<object>();
            }
        }

        private IEnumerable<object> ExtractBuckets(string content)
        {
            try
            {
                // Parse S3 ListBuckets response
                var doc = JsonDocument.Parse(content);
                var buckets = new List<S3Bucket>();

                if (doc.RootElement.TryGetProperty("Buckets", out var bucketsArray))
                {
                    foreach (var bucket in bucketsArray.EnumerateArray())
                    {
                        var s3Bucket = new S3Bucket
                        {
                            BucketName = bucket.GetProperty("Name").GetString(),
                            CreationDate = bucket.GetProperty("CreationDate").GetDateTime()
                        };
                        buckets.Add(s3Bucket);
                    }
                }

                return buckets;
            }
            catch
            {
                return Array.Empty<object>();
            }
        }

        private IEnumerable<object> ExtractObjects(string content)
        {
            try
            {
                // Parse S3 ListObjects response
                var doc = JsonDocument.Parse(content);
                var objects = new List<S3Object>();

                if (doc.RootElement.TryGetProperty("Contents", out var contentsArray))
                {
                    foreach (var obj in contentsArray.EnumerateArray())
                    {
                        var s3Object = new S3Object
                        {
                            Key = obj.GetProperty("Key").GetString(),
                            LastModified = obj.GetProperty("LastModified").GetDateTime(),
                            ETag = obj.GetProperty("ETag").GetString(),
                            Size = obj.GetProperty("Size").GetInt64(),
                            StorageClass = obj.GetProperty("StorageClass").GetString()
                        };
                        objects.Add(s3Object);
                    }
                }

                return objects;
            }
            catch
            {
                return Array.Empty<object>();
            }
        }

        private IEnumerable<object> ExtractObjectVersions(string content)
        {
            try
            {
                // Parse S3 ListObjectVersions response
                var doc = JsonDocument.Parse(content);
                var versions = new List<S3ObjectVersion>();

                if (doc.RootElement.TryGetProperty("Versions", out var versionsArray))
                {
                    foreach (var version in versionsArray.EnumerateArray())
                    {
                        var s3Version = new S3ObjectVersion
                        {
                            Key = version.GetProperty("Key").GetString(),
                            VersionId = version.GetProperty("VersionId").GetString(),
                            LastModified = version.GetProperty("LastModified").GetDateTime(),
                            ETag = version.GetProperty("ETag").GetString(),
                            Size = version.GetProperty("Size").GetInt64(),
                            StorageClass = version.GetProperty("StorageClass").GetString(),
                            IsLatest = version.GetProperty("IsLatest").GetBoolean()
                        };
                        versions.Add(s3Version);
                    }
                }

                return versions;
            }
            catch
            {
                return Array.Empty<object>();
            }
        }

        private IEnumerable<object> ExtractGeneric(string content, string entityName)
        {
            try
            {
                var doc = JsonDocument.Parse(content);
                return new object[] { doc.RootElement };
            }
            catch
            {
                return Array.Empty<object>();
            }
        }
    }
}
