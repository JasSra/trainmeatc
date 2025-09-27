# Frontend Segment (Blazor Server UI)

Razor pages/components for:
- Scenario selection
- Live simulation (mic capture, transcript, ATC panel, retry bar, score strip)
- Debrief (timeline, audio playback, metrics)

Leverages SignalR through shared hub. Audio capture via JS interop (WebAudio API).