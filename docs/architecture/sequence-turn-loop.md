# Turn Loop Sequence (Simplified)

1. User audio chunk -> /api/stt
2. STT partial -> SignalR `partialTranscript`
3. Final STT -> Instructor request
4. Instructor verdict -> SignalR `instructorVerdict`
5. If blocked: UI shows Retry
6. If pass: ATC request
7. ATC reply -> SignalR `atcTransmission`
8. TTS synth -> file saved
9. SignalR `ttsReady` -> client plays
10. Score update -> `scoreTick`
