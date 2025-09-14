
# 🎬 Movie Search API (.NET 8 + Elasticsearch + Kafka)

This project demonstrates how to build and test a **.NET 8 Web API** backed by **Elasticsearch** for full-text search, autocomplete, and faceted filtering.  
It also shows how to integrate **Apache Kafka** to make the system event-driven.

---

## 🚀 Getting Started

### 1. Requirements
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or VS Code
- [Elasticsearch 8.x ZIP](https://www.elastic.co/downloads/elasticsearch) (Windows)
- (Optional) [Kibana 8.x ZIP](https://www.elastic.co/downloads/kibana)
- [Elasticvue](https://elasticvue.com/) (browser extension or desktop app)
- [Apache Kafka 3.9+](https://kafka.apache.org/downloads) (KRaft mode, Windows batch scripts available in `bin/windows`)
- Java 11 or 17 (LTS) for Kafka

### 2. Running Elasticsearch (without Docker)
Extract the downloaded ZIP, then in PowerShell run:

```powershell
cd "C:\path\to\elasticsearch-8.x"
.bin\elasticsearch.bat -E discovery.type=single-node -E xpack.security.enabled=false
```

Check http://localhost:9200 to verify it’s running.

(Optional) Start Kibana:
```powershell
cd "C:\path\to\kibana-8.x"
.bin\kibana.bat
```
Open http://localhost:5601

### 3. Running Kafka (KRaft mode)
1. Install Java 17 (recommended).  
2. Extract Kafka to `C:\kafka\3.9.0` (avoid spaces in path).  
3. Generate cluster id:
   ```powershell
   & ".\bin\windows\kafka-storage.bat" random-uuid
   ```
4. Format logs (replace with your UUID):
   ```powershell
   & ".\bin\windows\kafka-storage.bat" format -t <uuid> -c ".\config\kraft\server.properties"
   ```
5. Start the broker:
   ```powershell
   & ".\bin\windows\kafka-server-start.bat" ".\config\kraft\server.properties"
   ```
6. Verify with:
   ```powershell
   & ".\bin\windows\kafka-topics.bat" --list --bootstrap-server localhost:9092
   ```

### 4. Run the API
```powershell
dotnet run --project src/Movies.Api/Movies.Api.csproj
```
Swagger will open at https://localhost:5001/swagger

### 5. Run the Worker
```powershell
dotnet run --project src/Movies.Worker/Movies.Worker.csproj
```
This worker consumes Kafka events (`movies.upserted`) and indexes movies into Elasticsearch.

---

## 📖 API Endpoints

### Health
- **GET /api/health** → Cluster health info

### Movies
- **POST /api/movies/seed** → Seed 8 sample movies
- **POST /api/movies** → Create or replace a movie (upsert) → publishes an event to Kafka
- **PATCH /api/movies/{id}** → Partial update (PATCH via Elasticsearch)
- **GET /api/movies/{id}** → Get movie by id
- **DELETE /api/movies/{id}** → Delete movie by id

### Search
- **GET /api/movies/search** → Full-text search with filters, sorting, pagination, and aggregations
- **GET /api/movies/suggest** → Autocomplete suggestions

Example search:
```
/api/movies/search?q=incepshon&genre=action&yearFrom=2000&sort=year:desc&page=1&pageSize=5
```

---

## 🔍 Testing

### With Swagger
- Use the interactive UI to call endpoints, seed data, search, and update movies.

### With Postman
Import the provided `MovieSearch.postman_collection.json` into Postman.
- Set `baseUrl` to your API (e.g., https://localhost:5001)
- Try `Health`, `Seed`, `Search`, `Suggest`, and `Update` endpoints.

### With Elasticvue
1. Connect to `http://localhost:9200`
2. Explore the `movies` index → mappings and documents
3. Run queries in **Dev Tools**

Example query (fuzzy search):
```json
GET movies/_search
{
  "query": {
    "match": { "title": "incepshon" }
  }
}
```

Example suggester:
```json
GET movies/_search
{
  "suggest": {
    "title_suggest": {
      "prefix": "la",
      "completion": { "field": "title_suggest" }
    }
  }
}
```

### With Kafka (manual test)
- Produce a message:
  ```powershell
  & ".\bin\windows\kafka-console-producer.bat" --topic movies.upserted --bootstrap-server localhost:9092
  ```
  Paste JSON like:
  ```json
  { "type": "MovieUpserted", "payload": { "id": "999", "title": "CLI Test", "year": 2025, "genre": "test" } }
  ```
- Watch the Worker log process it.

---

## ⚠️ Debugging

- 500 errors → check API/worker logs; ensure ES and Kafka are running.
- Kafka errors → check Java version (use 11 or 17), path length (avoid spaces), and bootstrap server `localhost:9092`.
- Bulk seed errors → ensure NDJSON body ends with a newline (`\n`).
- Suggest errors → verify `title_suggest` field exists as `completion` in mapping.

---

## ✅ Features Demonstrated

- Full-text search with fuzzy matching
- Autocomplete suggestions
- Faceted filtering and aggregations
- Partial updates (PATCH)
- Event-driven integration with Kafka
- Swagger + Postman + Elasticvue for testing

## Commands for the Kafka

# One-time format
.\bin\windows\kafka-storage.bat format -t (New-Guid).Guid -c .\config\kraft\server.properties

# Start broker
.\bin\windows\kafka-server-start.bat .\config\kraft\server.properties

# Verify it’s running
bin/kafka-topics.sh --list --bootstrap-server localhost:9092

# Create the orders topic
# Windows
.\bin\windows\kafka-topics.bat --create --topic orders --bootstrap-server localhost:9092 --partitions 1 --replication-factor 1

# (Optional) Watch messages directly
.\bin\windows\kafka-console-consumer.bat --bootstrap-server localhost:9092 --topic orders --from-beginning

---

Enjoy exploring search + event-driven architecture with .NET + Elasticsearch + Kafka!
