# Rate Limiting Strategy

Endpoints:
- /api/stt : token bucket 20 req/min/IP
- /api/atc : token bucket 15 req/min/IP
- /api/instructor : implicitly tied to /api/stt (could share bucket)

Implementation Options:
- ASP.NET middleware + MemoryCache sliding counters
- Or `AspNetCoreRateLimit` NuGet

Responses on limit exceed: 429 with `Retry-After` header.
