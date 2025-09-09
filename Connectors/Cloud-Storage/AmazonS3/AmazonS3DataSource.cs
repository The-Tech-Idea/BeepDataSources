using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

using Microsoft.Extensions.Logging;

namespace BeepDM.Connectors.CloudStorage.AmazonS3
{
    public class AmazonS3Config
    {
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string Region { get; set; }
        public string SessionToken { get; set; }
        public bool UseSessionToken { get; set; }
    }

    public class AmazonS3DataSource : IDataSource
    {
        private readonly ILogger<AmazonS3DataSource> _logger;
        private IAmazonS3 _s3Client;
        private AmazonS3Config _config;
        private bool _isConnected;

        public string DataSourceName => "AmazonS3";
        public string DataSourceType => "CloudStorage";
        public string Version => "1.0.0";
        public string Description => "Amazon S3 Cloud Storage Data Source";

        public AmazonS3DataSource(ILogger<AmazonS3DataSource> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> ConnectAsync(Dictionary<string, object> parameters)
        {
            try
            {
                _config = new AmazonS3Config();

                // Check authentication method
                if (parameters.ContainsKey("AccessKey") && parameters.ContainsKey("SecretKey"))
                {
                    _config.AccessKey = parameters["AccessKey"].ToString();
                    _config.SecretKey = parameters["SecretKey"].ToString();
                    _config.Region = parameters.ContainsKey("Region") ? parameters["Region"].ToString() : "us-east-1";
                    _config.UseSessionToken = parameters.ContainsKey("SessionToken");
                    if (_config.UseSessionToken)
                    {
                        _config.SessionToken = parameters["SessionToken"].ToString();
                    }
                }
                else
                {
                    throw new ArgumentException("AccessKey and SecretKey are required");
                }

                // Initialize the S3 client
                await InitializeS3ClientAsync();

                // Test connection by listing buckets
                var listBucketsRequest = new ListBucketsRequest();
                var response = await _s3Client.ListBucketsAsync(listBucketsRequest);

                if (response != null)
                {
                    _isConnected = true;
                    _logger.LogInformation("Successfully connected to Amazon S3 API");
                    return true;
                }

                _logger.LogError("Failed to connect to Amazon S3 API");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to Amazon S3 API");
                return false;
            }
        }

        private async Task InitializeS3ClientAsync()
        {
            var config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_config.Region)
            };

            if (_config.UseSessionToken)
            {
                _s3Client = new AmazonS3Client(_config.AccessKey, _config.SecretKey, _config.SessionToken, config);
            }
            else
            {
                _s3Client = new AmazonS3Client(_config.AccessKey, _config.SecretKey, config);
            }
        }

        public async Task<bool> DisconnectAsync()
        {
            try
            {
                if (_s3Client != null)
                {
                    _s3Client.Dispose();
                    _s3Client = null;
                }
                _isConnected = false;
                _logger.LogInformation("Disconnected from Amazon S3 API");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from Amazon S3 API");
                return false;
            }
        }

        public async Task<DataTable> GetEntityAsync(string entityName, Dictionary<string, object> parameters = null)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Amazon S3 API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "buckets":
                        return await GetBucketsAsync(parameters);
                    case "objects":
                        return await GetObjectsAsync(parameters);
                    case "versions":
                        return await GetVersionsAsync(parameters);
                    case "multipartuploads":
                        return await GetMultipartUploadsAsync(parameters);
                    case "presignedurls":
                        return await GetPresignedUrlsAsync(parameters);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting entity {entityName} from Amazon S3");
                throw;
            }
        }

        private async Task<DataTable> GetBucketsAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("buckets");

            try
            {
                var listBucketsRequest = new ListBucketsRequest();
                var response = await _s3Client.ListBucketsAsync(listBucketsRequest);

                // Create columns
                dataTable.Columns.Add("bucket_name", typeof(string));
                dataTable.Columns.Add("creation_date", typeof(DateTime));

                // Add rows
                foreach (var bucket in response.Buckets)
                {
                    var row = dataTable.NewRow();
                    row["bucket_name"] = bucket.BucketName;
                    row["creation_date"] = bucket.CreationDate;

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting buckets from Amazon S3");
                throw;
            }
        }

        private async Task<DataTable> GetObjectsAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("objects");

            try
            {
                if (!parameters.ContainsKey("bucketName"))
                {
                    throw new ArgumentException("bucketName is required for objects");
                }

                var bucketName = parameters["bucketName"].ToString();
                var prefix = parameters.ContainsKey("prefix") ? parameters["prefix"].ToString() : "";
                var delimiter = parameters.ContainsKey("delimiter") ? parameters["delimiter"].ToString() : "";

                var listObjectsRequest = new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    Prefix = prefix,
                    Delimiter = delimiter
                };

                var response = await _s3Client.ListObjectsV2Async(listObjectsRequest);

                // Create columns
                dataTable.Columns.Add("key", typeof(string));
                dataTable.Columns.Add("bucket_name", typeof(string));
                dataTable.Columns.Add("size", typeof(long));
                dataTable.Columns.Add("last_modified", typeof(DateTime));
                dataTable.Columns.Add("etag", typeof(string));
                dataTable.Columns.Add("storage_class", typeof(string));
                dataTable.Columns.Add("is_delete_marker", typeof(bool));

                // Add object rows
                foreach (var obj in response.S3Objects)
                {
                    var row = dataTable.NewRow();
                    row["key"] = obj.Key;
                    row["bucket_name"] = obj.BucketName;
                    row["size"] = obj.Size;
                    row["last_modified"] = obj.LastModified;
                    row["etag"] = obj.ETag;
                    row["storage_class"] = obj.StorageClass;
                    row["is_delete_marker"] = false;

                    dataTable.Rows.Add(row);
                }

                // Add common prefixes (folders)
                foreach (var prefix in response.CommonPrefixes)
                {
                    var row = dataTable.NewRow();
                    row["key"] = prefix;
                    row["bucket_name"] = bucketName;
                    row["size"] = 0L;
                    row["last_modified"] = DateTime.MinValue;
                    row["etag"] = "";
                    row["storage_class"] = "";
                    row["is_delete_marker"] = false;

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting objects from Amazon S3");
                throw;
            }
        }

        private async Task<DataTable> GetVersionsAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("versions");

            try
            {
                if (!parameters.ContainsKey("bucketName"))
                {
                    throw new ArgumentException("bucketName is required for versions");
                }

                var bucketName = parameters["bucketName"].ToString();
                var prefix = parameters.ContainsKey("prefix") ? parameters["prefix"].ToString() : "";

                var listVersionsRequest = new ListVersionsRequest
                {
                    BucketName = bucketName,
                    Prefix = prefix
                };

                var response = await _s3Client.ListVersionsAsync(listVersionsRequest);

                // Create columns
                dataTable.Columns.Add("key", typeof(string));
                dataTable.Columns.Add("version_id", typeof(string));
                dataTable.Columns.Add("bucket_name", typeof(string));
                dataTable.Columns.Add("size", typeof(long));
                dataTable.Columns.Add("last_modified", typeof(DateTime));
                dataTable.Columns.Add("etag", typeof(string));
                dataTable.Columns.Add("storage_class", typeof(string));
                dataTable.Columns.Add("is_delete_marker", typeof(bool));
                dataTable.Columns.Add("is_latest", typeof(bool));

                // Add rows
                foreach (var version in response.Versions)
                {
                    var row = dataTable.NewRow();
                    row["key"] = version.Key;
                    row["version_id"] = version.VersionId;
                    row["bucket_name"] = version.BucketName;
                    row["size"] = version.Size;
                    row["last_modified"] = version.LastModified;
                    row["etag"] = version.ETag;
                    row["storage_class"] = version.StorageClass;
                    row["is_delete_marker"] = version.IsDeleteMarker;
                    row["is_latest"] = version.IsLatest;

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting versions from Amazon S3");
                throw;
            }
        }

        private async Task<DataTable> GetMultipartUploadsAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("multipartuploads");

            try
            {
                if (!parameters.ContainsKey("bucketName"))
                {
                    throw new ArgumentException("bucketName is required for multipart uploads");
                }

                var bucketName = parameters["bucketName"].ToString();
                var prefix = parameters.ContainsKey("prefix") ? parameters["prefix"].ToString() : "";

                var listMultipartUploadsRequest = new ListMultipartUploadsRequest
                {
                    BucketName = bucketName,
                    Prefix = prefix
                };

                var response = await _s3Client.ListMultipartUploadsAsync(listMultipartUploadsRequest);

                // Create columns
                dataTable.Columns.Add("key", typeof(string));
                dataTable.Columns.Add("upload_id", typeof(string));
                dataTable.Columns.Add("bucket_name", typeof(string));
                dataTable.Columns.Add("initiated", typeof(DateTime));
                dataTable.Columns.Add("storage_class", typeof(string));
                dataTable.Columns.Add("owner_id", typeof(string));
                dataTable.Columns.Add("owner_display_name", typeof(string));

                // Add rows
                foreach (var upload in response.MultipartUploads)
                {
                    var row = dataTable.NewRow();
                    row["key"] = upload.Key;
                    row["upload_id"] = upload.UploadId;
                    row["bucket_name"] = bucketName;
                    row["initiated"] = upload.Initiated;
                    row["storage_class"] = upload.StorageClass;
                    row["owner_id"] = upload.Owner?.Id ?? "";
                    row["owner_display_name"] = upload.Owner?.DisplayName ?? "";

                    dataTable.Rows.Add(row);
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting multipart uploads from Amazon S3");
                throw;
            }
        }

        private async Task<DataTable> GetPresignedUrlsAsync(Dictionary<string, object> parameters = null)
        {
            var dataTable = new DataTable("presignedurls");

            try
            {
                if (!parameters.ContainsKey("bucketName") || !parameters.ContainsKey("key"))
                {
                    throw new ArgumentException("bucketName and key are required for presigned URLs");
                }

                var bucketName = parameters["bucketName"].ToString();
                var key = parameters["key"].ToString();
                var expiresInMinutes = parameters.ContainsKey("expiresInMinutes") ?
                    Convert.ToInt32(parameters["expiresInMinutes"]) : 60;
                var operation = parameters.ContainsKey("operation") ?
                    parameters["operation"].ToString() : "GET";

                var request = new GetPreSignedUrlRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes)
                };

                switch (operation.ToUpper())
                {
                    case "GET":
                        request.Verb = HttpVerb.GET;
                        break;
                    case "PUT":
                        request.Verb = HttpVerb.PUT;
                        break;
                    case "DELETE":
                        request.Verb = HttpVerb.DELETE;
                        break;
                    default:
                        request.Verb = HttpVerb.GET;
                        break;
                }

                var url = _s3Client.GetPreSignedURL(request);

                // Create columns
                dataTable.Columns.Add("bucket_name", typeof(string));
                dataTable.Columns.Add("key", typeof(string));
                dataTable.Columns.Add("url", typeof(string));
                dataTable.Columns.Add("operation", typeof(string));
                dataTable.Columns.Add("expires_in_minutes", typeof(int));
                dataTable.Columns.Add("expires_at", typeof(DateTime));

                // Add row
                var row = dataTable.NewRow();
                row["bucket_name"] = bucketName;
                row["key"] = key;
                row["url"] = url;
                row["operation"] = operation;
                row["expires_in_minutes"] = expiresInMinutes;
                row["expires_at"] = DateTime.UtcNow.AddMinutes(expiresInMinutes);

                dataTable.Rows.Add(row);

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating presigned URL from Amazon S3");
                throw;
            }
        }

        public async Task<DataTable> GetEntitiesAsync()
        {
            var dataTable = new DataTable("entities");

            // Create columns
            dataTable.Columns.Add("entity_name", typeof(string));
            dataTable.Columns.Add("entity_type", typeof(string));
            dataTable.Columns.Add("description", typeof(string));
            dataTable.Columns.Add("supports_create", typeof(bool));
            dataTable.Columns.Add("supports_read", typeof(bool));
            dataTable.Columns.Add("supports_update", typeof(bool));
            dataTable.Columns.Add("supports_delete", typeof(bool));

            // Add entity definitions
            var entities = new[]
            {
                new { Name = "buckets", Type = "Bucket", Description = "S3 bucket containers", Create = true, Read = true, Update = false, Delete = true },
                new { Name = "objects", Type = "Object", Description = "Files and objects stored in S3", Create = true, Read = true, Update = true, Delete = true },
                new { Name = "versions", Type = "Version", Description = "Object version history", Create = false, Read = true, Update = false, Delete = true },
                new { Name = "multipartuploads", Type = "MultipartUpload", Description = "Multipart upload sessions", Create = true, Read = true, Update = false, Delete = true },
                new { Name = "presignedurls", Type = "PresignedUrl", Description = "Temporary access URLs", Create = false, Read = true, Update = false, Delete = false }
            };

            foreach (var entity in entities)
            {
                var row = dataTable.NewRow();
                row["entity_name"] = entity.Name;
                row["entity_type"] = entity.Type;
                row["description"] = entity.Description;
                row["supports_create"] = entity.Create;
                row["supports_read"] = entity.Read;
                row["supports_update"] = entity.Update;
                row["supports_delete"] = entity.Delete;

                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        public async Task<DataTable> CreateEntityAsync(string entityName, Dictionary<string, object> data)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Amazon S3 API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "buckets":
                        return await CreateBucketAsync(data);
                    case "objects":
                        return await CreateObjectAsync(data);
                    case "multipartuploads":
                        return await CreateMultipartUploadAsync(data);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' creation is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating entity {entityName} in Amazon S3");
                throw;
            }
        }

        private async Task<DataTable> CreateBucketAsync(Dictionary<string, object> data)
        {
            var dataTable = new DataTable("buckets");

            try
            {
                if (!data.ContainsKey("bucketName"))
                {
                    throw new ArgumentException("bucketName is required for bucket creation");
                }

                var bucketName = data["bucketName"].ToString();
                var putBucketRequest = new PutBucketRequest
                {
                    BucketName = bucketName,
                    UseClientRegion = true
                };

                var response = await _s3Client.PutBucketAsync(putBucketRequest);

                // Create columns
                dataTable.Columns.Add("bucket_name", typeof(string));
                dataTable.Columns.Add("creation_date", typeof(DateTime));
                dataTable.Columns.Add("request_id", typeof(string));

                // Add created bucket
                var row = dataTable.NewRow();
                row["bucket_name"] = bucketName;
                row["creation_date"] = DateTime.UtcNow;
                row["request_id"] = response.ResponseMetadata.RequestId;

                dataTable.Rows.Add(row);

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bucket in Amazon S3");
                throw;
            }
        }

        private async Task<DataTable> CreateObjectAsync(Dictionary<string, object> data)
        {
            // Implementation for object upload would go here
            // This is a placeholder for the full implementation
            throw new NotImplementedException("Object upload not yet implemented");
        }

        private async Task<DataTable> CreateMultipartUploadAsync(Dictionary<string, object> data)
        {
            var dataTable = new DataTable("multipartuploads");

            try
            {
                if (!data.ContainsKey("bucketName") || !data.ContainsKey("key"))
                {
                    throw new ArgumentException("bucketName and key are required for multipart upload creation");
                }

                var bucketName = data["bucketName"].ToString();
                var key = data["key"].ToString();

                var initiateMultipartUploadRequest = new InitiateMultipartUploadRequest
                {
                    BucketName = bucketName,
                    Key = key
                };

                var response = await _s3Client.InitiateMultipartUploadAsync(initiateMultipartUploadRequest);

                // Create columns
                dataTable.Columns.Add("bucket_name", typeof(string));
                dataTable.Columns.Add("key", typeof(string));
                dataTable.Columns.Add("upload_id", typeof(string));
                dataTable.Columns.Add("request_id", typeof(string));

                // Add created multipart upload
                var row = dataTable.NewRow();
                row["bucket_name"] = bucketName;
                row["key"] = key;
                row["upload_id"] = response.UploadId;
                row["request_id"] = response.ResponseMetadata.RequestId;

                dataTable.Rows.Add(row);

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating multipart upload in Amazon S3");
                throw;
            }
        }

        public async Task<DataTable> UpdateEntityAsync(string entityName, Dictionary<string, object> data)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Amazon S3 API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "objects":
                        return await UpdateObjectAsync(data);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' update is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating entity {entityName} in Amazon S3");
                throw;
            }
        }

        private async Task<DataTable> UpdateObjectAsync(Dictionary<string, object> data)
        {
            // Implementation for object update would go here
            // This is a placeholder for the full implementation
            throw new NotImplementedException("Object update not yet implemented");
        }

        public async Task<bool> DeleteEntityAsync(string entityName, Dictionary<string, object> parameters)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Not connected to Amazon S3 API");
            }

            try
            {
                switch (entityName.ToLower())
                {
                    case "buckets":
                        return await DeleteBucketAsync(parameters);
                    case "objects":
                        return await DeleteObjectAsync(parameters);
                    case "versions":
                        return await DeleteVersionAsync(parameters);
                    case "multipartuploads":
                        return await DeleteMultipartUploadAsync(parameters);
                    default:
                        throw new ArgumentException($"Entity '{entityName}' deletion is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting entity {entityName} from Amazon S3");
                throw;
            }
        }

        private async Task<bool> DeleteBucketAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("bucketName"))
                {
                    throw new ArgumentException("bucketName is required for bucket deletion");
                }

                var bucketName = parameters["bucketName"].ToString();
                var deleteBucketRequest = new DeleteBucketRequest
                {
                    BucketName = bucketName
                };

                await _s3Client.DeleteBucketAsync(deleteBucketRequest);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting bucket from Amazon S3");
                throw;
            }
        }

        private async Task<bool> DeleteObjectAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("bucketName") || !parameters.ContainsKey("key"))
                {
                    throw new ArgumentException("bucketName and key are required for object deletion");
                }

                var bucketName = parameters["bucketName"].ToString();
                var key = parameters["key"].ToString();

                var deleteObjectRequest = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = key
                };

                await _s3Client.DeleteObjectAsync(deleteObjectRequest);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting object from Amazon S3");
                throw;
            }
        }

        private async Task<bool> DeleteVersionAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("bucketName") || !parameters.ContainsKey("key") || !parameters.ContainsKey("versionId"))
                {
                    throw new ArgumentException("bucketName, key, and versionId are required for version deletion");
                }

                var bucketName = parameters["bucketName"].ToString();
                var key = parameters["key"].ToString();
                var versionId = parameters["versionId"].ToString();

                var deleteVersionRequest = new DeleteVersionRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    VersionId = versionId
                };

                await _s3Client.DeleteVersionAsync(deleteVersionRequest);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting version from Amazon S3");
                throw;
            }
        }

        private async Task<bool> DeleteMultipartUploadAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("bucketName") || !parameters.ContainsKey("key") || !parameters.ContainsKey("uploadId"))
                {
                    throw new ArgumentException("bucketName, key, and uploadId are required for multipart upload deletion");
                }

                var bucketName = parameters["bucketName"].ToString();
                var key = parameters["key"].ToString();
                var uploadId = parameters["uploadId"].ToString();

                var abortMultipartUploadRequest = new AbortMultipartUploadRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    UploadId = uploadId
                };

                await _s3Client.AbortMultipartUploadAsync(abortMultipartUploadRequest);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting multipart upload from Amazon S3");
                throw;
            }
        }

        public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters = null)
        {
            // Implementation for custom query execution would go here
            // This is a placeholder for the full implementation
            throw new NotImplementedException("Custom query execution not yet implemented");
        }

        public async Task<DataTable> GetEntityMetadataAsync(string entityName)
        {
            var dataTable = new DataTable("metadata");

            // Create columns
            dataTable.Columns.Add("field_name", typeof(string));
            dataTable.Columns.Add("field_type", typeof(string));
            dataTable.Columns.Add("is_nullable", typeof(bool));
            dataTable.Columns.Add("description", typeof(string));

            try
            {
                switch (entityName.ToLower())
                {
                    case "buckets":
                        var bucketFields = new[]
                        {
                            new { Name = "bucket_name", Type = "string", Nullable = false, Description = "Unique name of the S3 bucket" },
                            new { Name = "creation_date", Type = "datetime", Nullable = true, Description = "Date and time when the bucket was created" }
                        };

                        foreach (var field in bucketFields)
                        {
                            var row = dataTable.NewRow();
                            row["field_name"] = field.Name;
                            row["field_type"] = field.Type;
                            row["is_nullable"] = field.Nullable;
                            row["description"] = field.Description;
                            dataTable.Rows.Add(row);
                        }
                        break;

                    case "objects":
                        var objectFields = new[]
                        {
                            new { Name = "key", Type = "string", Nullable = false, Description = "Object key (path) within the bucket" },
                            new { Name = "bucket_name", Type = "string", Nullable = false, Description = "Name of the bucket containing the object" },
                            new { Name = "size", Type = "long", Nullable = true, Description = "Size of the object in bytes" },
                            new { Name = "last_modified", Type = "datetime", Nullable = true, Description = "Date and time of last modification" },
                            new { Name = "etag", Type = "string", Nullable = true, Description = "Entity tag for the object" },
                            new { Name = "storage_class", Type = "string", Nullable = true, Description = "Storage class of the object" },
                            new { Name = "is_delete_marker", Type = "bool", Nullable = true, Description = "Whether this is a delete marker" }
                        };

                        foreach (var field in objectFields)
                        {
                            var row = dataTable.NewRow();
                            row["field_name"] = field.Name;
                            row["field_type"] = field.Type;
                            row["is_nullable"] = field.Nullable;
                            row["description"] = field.Description;
                            dataTable.Rows.Add(row);
                        }
                        break;

                    case "versions":
                        var versionFields = new[]
                        {
                            new { Name = "key", Type = "string", Nullable = false, Description = "Object key" },
                            new { Name = "version_id", Type = "string", Nullable = false, Description = "Version identifier" },
                            new { Name = "bucket_name", Type = "string", Nullable = false, Description = "Bucket name" },
                            new { Name = "size", Type = "long", Nullable = true, Description = "Size of the version" },
                            new { Name = "last_modified", Type = "datetime", Nullable = true, Description = "Last modification date" },
                            new { Name = "etag", Type = "string", Nullable = true, Description = "Entity tag" },
                            new { Name = "storage_class", Type = "string", Nullable = true, Description = "Storage class" },
                            new { Name = "is_delete_marker", Type = "bool", Nullable = true, Description = "Whether this is a delete marker" },
                            new { Name = "is_latest", Type = "bool", Nullable = true, Description = "Whether this is the latest version" }
                        };

                        foreach (var field in versionFields)
                        {
                            var row = dataTable.NewRow();
                            row["field_name"] = field.Name;
                            row["field_type"] = field.Type;
                            row["is_nullable"] = field.Nullable;
                            row["description"] = field.Description;
                            dataTable.Rows.Add(row);
                        }
                        break;

                    case "multipartuploads":
                        var multipartUploadFields = new[]
                        {
                            new { Name = "key", Type = "string", Nullable = false, Description = "Object key" },
                            new { Name = "upload_id", Type = "string", Nullable = false, Description = "Multipart upload ID" },
                            new { Name = "bucket_name", Type = "string", Nullable = false, Description = "Bucket name" },
                            new { Name = "initiated", Type = "datetime", Nullable = true, Description = "Date and time when upload was initiated" },
                            new { Name = "storage_class", Type = "string", Nullable = true, Description = "Storage class" },
                            new { Name = "owner_id", Type = "string", Nullable = true, Description = "Owner ID" },
                            new { Name = "owner_display_name", Type = "string", Nullable = true, Description = "Owner display name" }
                        };

                        foreach (var field in multipartUploadFields)
                        {
                            var row = dataTable.NewRow();
                            row["field_name"] = field.Name;
                            row["field_type"] = field.Type;
                            row["is_nullable"] = field.Nullable;
                            row["description"] = field.Description;
                            dataTable.Rows.Add(row);
                        }
                        break;

                    case "presignedurls":
                        var presignedUrlFields = new[]
                        {
                            new { Name = "bucket_name", Type = "string", Nullable = false, Description = "Bucket name" },
                            new { Name = "key", Type = "string", Nullable = false, Description = "Object key" },
                            new { Name = "url", Type = "string", Nullable = false, Description = "Presigned URL" },
                            new { Name = "operation", Type = "string", Nullable = false, Description = "HTTP operation (GET, PUT, DELETE)" },
                            new { Name = "expires_in_minutes", Type = "int", Nullable = false, Description = "URL expiration time in minutes" },
                            new { Name = "expires_at", Type = "datetime", Nullable = false, Description = "URL expiration date and time" }
                        };

                        foreach (var field in presignedUrlFields)
                        {
                            var row = dataTable.NewRow();
                            row["field_name"] = field.Name;
                            row["field_type"] = field.Type;
                            row["is_nullable"] = field.Nullable;
                            row["description"] = field.Description;
                            dataTable.Rows.Add(row);
                        }
                        break;

                    default:
                        throw new ArgumentException($"Entity '{entityName}' is not supported");
                }

                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting metadata for entity {entityName}");
                throw;
            }
        }
    }
}
