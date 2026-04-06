# Phase 07 — Messaging & vector stores

## Objective

Document `Messaging/` datasource cores (Kafka, SQS, Service Bus, Pub/Sub) and `VectorDatabase/` integrations at a high level.

## BeepDM `.cursor` sources

- `BeepDM/.cursor/idatasource/SKILL.md`
- Streaming-specific skills if extended in BeepDM (optional cross-link)

## Repo targets

- `Messaging/Messaging.sln` and `Messaging/*DataSourceCore` / `MassTransitDataSource` projects
- `VectorDatabase/` (`TheTechIdea.Beep.*Datasource` projects)

## Target HTML

| File | Content |
|------|---------|
| `Help/impl-messaging-vector.html` | Semantics: topics, queues vs tables; vector collections; **Contrast: Connectors REST** + flagship anchor; forward link to phases 08–09 — **shipped** |
| `Help/providers/msg-kafka.html` | **shipped** |
| `Help/providers/msg-rabbitmq.html` | **shipped** |
| `Help/providers/msg-nats.html` | **shipped** |
| `Help/providers/msg-masstransit.html` | **shipped** |
| `Help/providers/msg-redis-streams.html` | **shipped** (vs KV `providers/redis.html`) |
| `Help/providers/msg-google-pubsub.html` | **shipped** |
| `Help/providers/vector-qdrant.html` | **shipped** (`QdrantDatasourceGeneric`) |
| `Help/providers/vector-milvus.html` | **shipped** |
| `Help/providers/vector-chromadb.html` | **shipped** |
| … | Pinecone / SharpVector etc. when add-in attributes are active |

## TODO checklist

- [x] Clarify **IDataSource** mapping for messaging (entities vs topics) — see overview
- [x] Vector DB: link to embedding/index concepts — see overview

## Verification

- [x] Solution name `Messaging/Messaging.sln` and project folder names match help
- [x] `DataSourceType` / category nuances called out (Kafka `QUEUE`, Redis Streams vs KV `Redis`)

## Dependency

Phase 02 complete.
