# Component Boundaries

- Web/UI: Razor components only orchestrate user input and display.
- Services: Wrap external calls (OpenAI) behind interfaces for testability.
- Data Layer: EF Core context + migration scripts, repository helpers.
- State Reducer: Pure functions to apply ATC next_state patches.
- Scoring Engine: Consumes InstructorVerdict + scenario rubric.
