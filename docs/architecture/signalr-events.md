# SignalR Events

| Event | Payload | Description |
|-------|---------|-------------|
| partialTranscript | { text, isFinal? } | Streaming STT updates |
| instructorVerdict | InstructorVerdict JSON | Verdict after transcript processed |
| atcTransmission | { transmission, expected_readback[] } | ATC reply |
| ttsReady | { url } | Audio file ready for playback |
| scoreTick | { scoreTotal, delta } | Score update |
