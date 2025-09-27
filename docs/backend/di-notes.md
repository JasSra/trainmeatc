# Dependency Injection Notes

Planned registrations (example):

```csharp
builder.Services.AddSingleton<ISttService, OpenAiSttService>();
builder.Services.AddScoped<IInstructorService, OpenAiInstructorService>();
builder.Services.AddScoped<IAtcService, OpenAiAtcService>();
builder.Services.AddSingleton<ITtsService, OpenAiTtsService>();
```

DbContext (once created):
```csharp
builder.Services.AddDbContext<SimDbContext>(o => o.UseSqlite(builder.Configuration.GetConnectionString("SimDb")));
```

SignalR:
```csharp
builder.Services.AddSignalR();
```

Configuration Keys:
- OPENAI_API_KEY (env var) -> injected via `IConfiguration`.

Resilience:
- Consider Polly for transient retries around OpenAI calls.
