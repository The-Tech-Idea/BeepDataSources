# Vector Database Quick Reference Guide

## Quick Start

### 1. Choose Your Vector Database

| Database | Best For | Deployment |
|----------|----------|------------|
| **Milvus** | Large-scale production, complex filtering | Self-hosted |
| **Qdrant** | Recommendation systems, snapshots | Self-hosted |
| **Pinecone** | Managed service, rapid deployment | Cloud |
| **SharpVector** | Prototyping, local development | In-Memory |
| **ChromaDB** | Document search, RAG applications | Self-hosted |

### 2. Installation

Add to your `.csproj`:
```xml
<PackageReference Include="TheTechIdea.Beep.DataManagementEngine" Version="2.0.35" />
<PackageReference Include="TheTechIdea.Beep.DataManagementModels" Version="2.0.100" />
```

### 3. Basic Usage Pattern

```csharp
// Create datasource
var dataSource = new MilvusDataSource(
    "my_milvus",
    logger,
    dmeEditor,
    DataSourceType.Milvus,
    errorObject
);

// Open connection
dataSource.Openconnection();

// Prepare filters
var filters = new List<AppFilter>
{
    new() { FieldName = "collection_name", FilterValue = "my_collection" },
    new() { FieldName = "vector", FilterValue = "[0.1, 0.2, ..., 0.768]" },
    new() { FieldName = "topK", FilterValue = "10" }
};

// Execute operation
var results = await dataSource.GetEntityAsync("vectors.search", filters);

// Close when done
dataSource.Closeconnection();
```

---

## Common Operations

### List Collections/Indexes

#### Milvus
```csharp
var collections = dataSource.GetEntity("collections.list", null);
```

#### Qdrant
```csharp
var collections = await dataSource.GetEntityAsync("collections.list", null);
```

#### Pinecone
```csharp
var indexes = dataSource.GetEntity("indexes.list", null);
```

#### ChromaDB
```csharp
var collections = dataSource.GetEntity("collections.list", null);
```

---

### Create Collection/Index

#### Milvus
```csharp
var filters = new List<AppFilter>
{
    new() { FieldName = "collection_name", FilterValue = "products" },
    new() { FieldName = "dimension", FilterValue = "768" },
    new() { FieldName = "metric_type", FilterValue = "L2" }
};
dataSource.GetEntity("collections.create", filters);
```

#### Qdrant
```csharp
var filters = new List<AppFilter>
{
    new() { FieldName = "collection_name", FilterValue = "articles" },
    new() { FieldName = "vector_size", FilterValue = "512" },
    new() { FieldName = "distance", FilterValue = "Cosine" }
};
dataSource.GetEntity("collections.create", filters);
```

#### ChromaDB
```csharp
var filters = new List<AppFilter>
{
    new() { FieldName = "name", FilterValue = "documents" },
    new() { FieldName = "metadata", FilterValue = "{\"description\": \"My docs\"}" }
};
dataSource.GetEntity("collections.create", filters);
```

---

### Insert Vectors

#### Milvus
```csharp
var filters = new List<AppFilter>
{
    new() { FieldName = "collection_name", FilterValue = "products" },
    new() { FieldName = "vectors", FilterValue = "[[0.1, 0.2, ...], [0.3, 0.4, ...]]" },
    new() { FieldName = "ids", FilterValue = "[1, 2]" }
};
dataSource.GetEntity("vectors.insert", filters);
```

#### Qdrant
```csharp
var filters = new List<AppFilter>
{
    new() { FieldName = "collection_name", FilterValue = "articles" },
    new() { FieldName = "points", FilterValue = "[{\"id\": 1, \"vector\": [0.1, ...]}]" }
};
dataSource.GetEntity("points.upsert", filters);
```

#### ChromaDB
```csharp
var filters = new List<AppFilter>
{
    new() { FieldName = "collection_id", FilterValue = "col_123" },
    new() { FieldName = "ids", FilterValue = "[\"doc1\", \"doc2\"]" },
    new() { FieldName = "embeddings", FilterValue = "[[0.1, ...], [0.2, ...]]" },
    new() { FieldName = "documents", FilterValue = "[\"text1\", \"text2\"]" }
};
dataSource.GetEntity("embeddings.add", filters);
```

---

### Search Vectors

#### Milvus
```csharp
var filters = new List<AppFilter>
{
    new() { FieldName = "collection_name", FilterValue = "products" },
    new() { FieldName = "vector", FilterValue = "[0.1, 0.2, 0.3, ..., 0.768]" },
    new() { FieldName = "topK", FilterValue = "10" },
    new() { FieldName = "filter", FilterValue = "price > 100 AND category == 'electronics'" }
};
var results = await dataSource.GetEntityAsync("vectors.search", filters);
```

#### Qdrant
```csharp
var filters = new List<AppFilter>
{
    new() { FieldName = "collection_name", FilterValue = "articles" },
    new() { FieldName = "vector", FilterValue = "[0.1, 0.2, ..., 0.512]" },
    new() { FieldName = "limit", FilterValue = "5" }
};
var results = await dataSource.GetEntityAsync("points.search", filters);
```

#### ChromaDB
```csharp
var filters = new List<AppFilter>
{
    new() { FieldName = "collection_id", FilterValue = "docs_123" },
    new() { FieldName = "query_embeddings", FilterValue = "[0.1, ..., 0.1536]" },
    new() { FieldName = "n_results", FilterValue = "10" }
};
var results = dataSource.GetEntity("embeddings.query", filters);
```

---

### Get Recommendations (Qdrant Only)

```csharp
var filters = new List<AppFilter>
{
    new() { FieldName = "collection_name", FilterValue = "articles" },
    new() { FieldName = "positive", FilterValue = "point_id_1, point_id_2" },
    new() { FieldName = "negative", FilterValue = "point_id_3" },  // Optional
    new() { FieldName = "limit", FilterValue = "5" }
};
var similar = await dataSource.GetEntityAsync("points.recommend", filters);
```

---

### Delete Vectors

#### Milvus
```csharp
var filters = new List<AppFilter>
{
    new() { FieldName = "collection_name", FilterValue = "products" },
    new() { FieldName = "ids", FilterValue = "[1, 2, 3]" }
};
dataSource.GetEntity("vectors.delete", filters);
```

#### Qdrant
```csharp
var filters = new List<AppFilter>
{
    new() { FieldName = "collection_name", FilterValue = "articles" },
    new() { FieldName = "ids", FilterValue = "[\"id1\", \"id2\"]" }
};
dataSource.GetEntity("points.delete", filters);
```

---

## Filter Reference

### Common Filter Parameters

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `collection_name` | string | Collection/index name | "my_collection" |
| `collection_id` | string | Collection ID | "col_123" |
| `vector` | float[] | Search vector | "[0.1, 0.2, 0.3]" |
| `vectors` | float[][] | Multiple vectors | "[[0.1, 0.2], [0.3, 0.4]]" |
| `ids` | string[] | Vector IDs | "[1, 2, 3]" or "[\"id1\", \"id2\"]" |
| `topK` / `limit` | int | Max results | "10" |
| `n_results` | int | Number of results (ChromaDB) | "10" |
| `filter` / `expr` | string | Metadata filter | "price > 100" |

### Vector Format Options

1. **JSON Array** (Recommended):
   ```csharp
   FilterValue = "[0.1, 0.2, 0.3, 0.4, 0.5]"
   ```

2. **CSV Format**:
   ```csharp
   FilterValue = "0.1, 0.2, 0.3, 0.4, 0.5"
   ```

3. **Multi-vector JSON**:
   ```csharp
   FilterValue = "[[0.1, 0.2], [0.3, 0.4], [0.5, 0.6]]"
   ```

---

## Connection Strings

### Milvus
```csharp
{
    "ConnectionName": "MilvusLocal",
    "Host": "localhost",
    "Port": 9091,
    "DatabaseType": "Milvus",
    "Ssl": false
}
```

### Qdrant
```csharp
{
    "ConnectionName": "QdrantLocal",
    "Host": "localhost",
    "Port": 6333,
    "DatabaseType": "Qdrant",
    "Ssl": false
}
```

### Qdrant Cloud
```csharp
{
    "ConnectionName": "QdrantCloud",
    "Host": "xyz-example.aws.cloud.qdrant.io",
    "ApiKey": "your-api-key",
    "DatabaseType": "Qdrant",
    "Ssl": true
}
```

### Pinecone
```csharp
{
    "ConnectionName": "PineconeCloud",
    "Host": "api.pinecone.io",
    "ApiKey": "your-api-key",
    "DatabaseType": "PineCone",
    "Ssl": true
}
```

### ChromaDB
```csharp
{
    "ConnectionName": "ChromaLocal",
    "Host": "localhost",
    "Port": 8000,
    "DatabaseType": "ChromaDB",
    "Ssl": false
}
```

---

## Error Handling

### Basic Pattern
```csharp
try
{
    dataSource.Openconnection();
    var results = dataSource.GetEntity("vectors.search", filters);
}
catch (InvalidOperationException ex)
{
    // Missing required filter
    Console.WriteLine($"Filter error: {ex.Message}");
}
catch (HttpRequestException ex)
{
    // Network/API error
    Console.WriteLine($"Connection error: {ex.Message}");
}
catch (JsonException ex)
{
    // Response parsing error
    Console.WriteLine($"Parse error: {ex.Message}");
}
finally
{
    dataSource.Closeconnection();
}
```

### Check Connection Status
```csharp
if (dataSource.ConnectionStatus != ConnectionState.Open)
{
    Console.WriteLine("Connection failed!");
    return;
}
```

---

## Performance Tips

### 1. Reuse Connections
```csharp
// DON'T: Open/close for each operation
foreach (var query in queries)
{
    dataSource.Openconnection();
    var results = dataSource.GetEntity("vectors.search", query);
    dataSource.Closeconnection();
}

// DO: Open once, close once
dataSource.Openconnection();
foreach (var query in queries)
{
    var results = dataSource.GetEntity("vectors.search", query);
}
dataSource.Closeconnection();
```

### 2. Use Async Methods
```csharp
// Better for I/O-bound operations
var results = await dataSource.GetEntityAsync("vectors.search", filters);
```

### 3. Batch Operations
```csharp
// Insert multiple vectors at once
var filters = new List<AppFilter>
{
    new() { FieldName = "vectors", FilterValue = "[[0.1, ...], [0.2, ...], ...]" }
};
```

### 4. Limit Results
```csharp
// Don't retrieve more than needed
new() { FieldName = "topK", FilterValue = "10" }  // Not "1000"
```

---

## Troubleshooting

### "Operation requires filter 'X'"
**Cause**: Missing required parameter  
**Solution**: Check RequiredFilters dictionary for operation
```csharp
// Milvus vectors.search requires:
new() { FieldName = "collection_name", FilterValue = "..." }
new() { FieldName = "vector", FilterValue = "..." }
```

### "Connection refused" / "Host not found"
**Cause**: Service not running or wrong host/port  
**Solution**: Verify service is running
```bash
# Milvus
curl http://localhost:9091/v1/vector/collections

# Qdrant
curl http://localhost:6333/collections

# ChromaDB
curl http://localhost:8000/api/v1/heartbeat
```

### "Invalid vector format"
**Cause**: Vector string not parseable  
**Solution**: Use JSON array format
```csharp
// Correct
FilterValue = "[0.1, 0.2, 0.3]"

// Incorrect
FilterValue = "0.1 0.2 0.3"
```

### "Collection not found"
**Cause**: Collection doesn't exist  
**Solution**: Create collection first
```csharp
dataSource.GetEntity("collections.create", createFilters);
```

---

## Cheat Sheet

### Milvus Operations
```
collections.list, collections.describe, collections.create, collections.drop
collections.load, collections.release, collections.stats
vectors.insert, vectors.search, vectors.query, vectors.delete
indexes.create, indexes.describe, indexes.drop
```

### Qdrant Operations
```
collections.list, collections.get, collections.create, collections.delete, collections.update
points.upsert, points.search, points.scroll, points.get, points.delete, points.recommend
snapshots.create, snapshots.list
```

### Pinecone Operations
```
indexes.list, indexes.describe, indexes.create, indexes.delete
vectors.upsert, vectors.query, vectors.delete, vectors.fetch
index.stats
```

### ChromaDB Operations
```
collections.list, collections.get, collections.create, collections.delete, collections.update
embeddings.add, embeddings.query, embeddings.get, embeddings.update, embeddings.delete
system.heartbeat, system.version
```

---

## Additional Resources

### Documentation
- Milvus: https://milvus.io/docs
- Qdrant: https://qdrant.tech/documentation
- Pinecone: https://docs.pinecone.io
- ChromaDB: https://docs.trychroma.com

### Support
- GitHub Issues: [Your repo URL]
- Email: [Support email]
- Discord: [Community link]

---

*Last Updated*: January 2025  
*Version*: 1.0.0
