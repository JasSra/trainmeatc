# Component Inventory (Planned)

- Pages:
  - `Pages/Index.razor` (Home / Disclaimer accept)
  - `Pages/ScenarioSelect.razor`
  - `Pages/LiveSim.razor`
  - `Pages/Debrief.razor`
- Components:
  - `Components/MicCapture.razor`
  - `Components/TranscriptPane.razor`
  - `Components/ATCPanel.razor`
  - `Components/ScoreStrip.razor`
  - `Components/RetryBar.razor`
  - `Components/AudioPlayer.razor`
- JS Interop:
  - `wwwroot/js/audioCapture.js` (start/stop, chunk encode, level meter)

State Flow Notes:
- LiveSim orchestrates turn loop via injected services + SignalR callbacks.
- Retry gating: Instructor verdict with block reason prevents ATC request until cleared.
