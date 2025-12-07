# Cloud Storage Connectors

## Overview

The Cloud Storage connectors category provides integration with cloud file storage and file sharing platforms, enabling file management, folder operations, sharing, and synchronization. All connectors inherit from `WebAPIDataSource` and use `CommandAttribute` to expose platform-specific functionality to the Beep framework.

## Architecture

- **Base Class**: All connectors inherit from `WebAPIDataSource`
- **Authentication**: Primarily OAuth 2.0 or API Keys
- **Models**: Strongly-typed POCO classes for files, folders, shares, metadata, etc.
- **CommandAttribute**: Public methods decorated with `CommandAttribute` for framework discovery

## Connectors

### Google Drive (`GoogleDriveDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://www.googleapis.com/drive/v3`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- File operations (upload, download, delete)
- Folder management
- File sharing and permissions
- File metadata operations
- Search and query

#### Configuration
```csharp
var props = new WebAPIConnectionProperties
{
    Url = "https://www.googleapis.com/drive/v3",
    AuthType = AuthTypeEnum.OAuth2,
    ClientId = "your_client_id",
    ClientSecret = "your_client_secret",
    TokenUrl = "https://oauth2.googleapis.com/token"
};
```

---

### Dropbox (`DropboxDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.dropboxapi.com/2`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- File operations
- Folder management
- Sharing operations
- File versioning
- Search functionality

---

### OneDrive (`OneDriveDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://graph.microsoft.com/v1.0/me/drive`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- File operations
- Folder management
- Sharing and permissions
- File metadata
- Search operations

---

### Box (`BoxDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.box.com/2.0`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- File operations
- Folder management
- Collaboration features
- Metadata operations
- Webhook management

---

### Amazon S3 (`AmazonS3DataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://{bucket}.s3.{region}.amazonaws.com`  
**Authentication**: AWS Access Key/Secret

#### CommandAttribute Methods
- Object operations (PUT, GET, DELETE)
- Bucket management
- Presigned URL generation
- Multipart uploads
- Lifecycle management

---

### Egnyte (`EgnyteDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://{domain}.egnyte.com/pubapi/v2`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- File operations
- Folder management
- Sharing operations
- Metadata management

---

### Citrix ShareFile (`CitrixShareFileDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://{subdomain}.sf-api.com/sf/v3`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- File operations
- Folder management
- Sharing and collaboration
- Workflow management

---

### pCloud (`pCloudDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://api.pcloud.com`  
**Authentication**: OAuth 2.0 or API Token

#### CommandAttribute Methods
- File operations
- Folder management
- Sharing operations
- Backup operations

---

### MediaFire (`MediaFireDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://www.mediafire.com/api`  
**Authentication**: API Key

#### CommandAttribute Methods
- File operations
- Folder management
- Sharing operations
- Upload management

---

### iCloud (`iCloudDataSource`)

**Base Class**: `WebAPIDataSource`  
**API Base URL**: `https://setup.icloud.com`  
**Authentication**: OAuth 2.0

#### CommandAttribute Methods
- File operations
- Folder management
- Sharing operations
- Sync operations

---

## Common Patterns

### CommandAttribute Structure

All cloud storage connectors use the `CommandAttribute` pattern:

```csharp
[CommandAttribute(
    Category = DatasourceCategory.Connector,
    DatasourceType = DataSourceType.CloudStorage,
    PointType = EnumPointType.Function,
    ObjectType = "EntityName",
    ClassName = "PlatformDataSource",
    Showin = ShowinType.Both,
    misc = "IEnumerable<EntityType>"
)]
public async Task<IEnumerable<EntityType>> GetFiles(AppFilter filter)
{
    // Implementation
}
```

### Entity Mapping

Cloud storage connectors typically support:
- **Files** - File upload, download, delete, metadata
- **Folders** - Folder creation, listing, navigation
- **Shares** - File/folder sharing and permissions
- **Metadata** - File properties, tags, custom fields
- **Versions** - File versioning and history
- **Search** - File and folder search operations

## Authentication Patterns

### OAuth 2.0 Platforms
- Google Drive, Dropbox, OneDrive, Box, Egnyte, Citrix ShareFile, pCloud, iCloud
- Requires client registration and user consent
- Supports refresh tokens for long-term access

### API Key Platforms
- MediaFire
- Uses API keys for authentication

### AWS Credentials
- Amazon S3
- Uses AWS Access Key ID and Secret Access Key

## Best Practices

1. **Rate Limiting**: Respect platform rate limits (Google Drive: 1,000 req/100sec, Dropbox: varies)
2. **Large Files**: Use chunked/multipart uploads for large files
3. **Error Handling**: Handle network failures, quota limits, and permission errors
4. **Caching**: Cache file metadata to reduce API calls
5. **Sync**: Implement efficient sync strategies for large directories
6. **Security**: Encrypt sensitive files and use secure sharing practices

## Configuration Requirements

### Google Drive
- Client ID, Client Secret
- OAuth 2.0 scopes
- Base URL: `https://www.googleapis.com/drive/v3`

### Dropbox
- App Key, App Secret
- OAuth 2.0 tokens
- Base URL: `https://api.dropboxapi.com/2`

### OneDrive
- Client ID, Client Secret
- Tenant ID (for business)
- Base URL: `https://graph.microsoft.com/v1.0`

### Amazon S3
- Access Key ID
- Secret Access Key
- Region
- Bucket name

## Status

All Cloud Storage connectors are **✅ Completed** and ready for use. See `progress.md` for detailed implementation status.

