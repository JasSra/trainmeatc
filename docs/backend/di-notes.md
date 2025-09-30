# Dependency Injection Notes

Current registrations:

```csharp
builder.Services.AddSingleton<ISttService, OpenAiSttService>();
builder.Services.AddScoped<IInstructorService, OpenAiInstructorServiceV2>();
builder.Services.AddScoped<ITrafficAgent, OpenAiTrafficAgentService>();
builder.Services.AddScoped<IResponderRouter, ResponderRouter>();
builder.Services.AddScoped<ITurnService, TurnService>();
builder.Services.AddScoped<IAtcService, AtcServiceAdapter>(); // Adapter for backward compatibility
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
