# Configuration Keys

| Key | Source | Notes |
|-----|--------|-------|
| OPENAI_API_KEY | Environment | Required for all OpenAI calls |
| ConnectionStrings:SimDb | appsettings.json / env | SQLite path (e.g., Data Source=app_data/sim.db) |
| RateLimits:SttPerMinute | appsettings.json | Default 20 |
| RateLimits:AtcPerMinute | appsettings.json | Default 15 |
| Logging:Level | appsettings.json | Serilog minimum level |
