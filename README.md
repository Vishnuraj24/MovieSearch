
# 🎬 Movie Search API (.NET 8 + Elasticsearch)

This project demonstrates how to build and test a **.NET 8 Web API** backed by **Elasticsearch** for full-text search, autocomplete, and faceted filtering.

---

## 🚀 Getting Started

### 1. Requirements
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or VS Code
- [Elasticsearch 8.x ZIP](https://www.elastic.co/downloads/elasticsearch) (Windows)
- (Optional) [Kibana 8.x ZIP](https://www.elastic.co/downloads/kibana)
- [Elasticvue](https://elasticvue.com/) (browser extension or desktop app)

### 2. Running Elasticsearch (without Docker)
Extract the downloaded ZIP, then in PowerShell run:

```powershell
cd "C:\path\to\elasticsearch-8.x"
 .\bin\elasticsearch.bat -E discovery.type=single-node -E xpack.security.enabled=false
```

Check http://localhost:9200 to verify it’s running.

(Optional) Start Kibana:
```powershell
cd "C:\path\to\kibana-8.x"
.in\kibana.bat
```
Open http://localhost:5601

### 3. Run the API
```powershell
dotnet run --project src/Movies.Api/Movies.Api.csproj
```
Swagger will open at https://localhost:5001/swagger

---

## 📖 API Endpoints

### Health
- **GET /api/health** → Cluster health info

### Movies
- **POST /api/movies/seed** → Seed 8 sample movies
- **POST /api/movies** → Create or replace a movie (upsert)
- **PATCH /api/movies/{id}** → Partial update (e.g., update description or cast)
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

---

## ⚠️ Debugging

- 500 errors → check console logs, often caused by ES not running or index missing
- Bulk seed errors → ensure NDJSON body ends with a newline (`\n`)
- Suggest errors → verify `title_suggest` field exists as `completion` in mapping

---

## ✅ Features Demonstrated

- Full-text search with fuzzy matching
- Autocomplete suggestions
- Faceted filtering and aggregations
- Partial updates (PATCH)
- Swagger + Postman + Elasticvue for testing

---

Enjoy exploring search with .NET + Elasticsearch!
