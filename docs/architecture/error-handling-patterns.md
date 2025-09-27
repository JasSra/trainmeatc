# Error Handling Patterns

Controller Pattern:
```csharp
try {
  // service call
} catch (RateLimitException ex) {
  return TooManyRequests(ex.RetryAfter);
} catch (ValidationException ex) {
  return BadRequest(new { code = ex.Code, ex.Message });
} catch (UpstreamException ex) {
  _logger.LogWarning(ex, "Upstream failure");
  return StatusCode(502, new { code = "OPENAI_UPSTREAM" });
}
```

Do not expose stack traces to clients (except in Development environment).
