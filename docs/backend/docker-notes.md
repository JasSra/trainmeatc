# Docker Notes

Publish Flow:
1. `dotnet publish -c Release -o publish`
2. Build image from Dockerfile (runtime image only) - multi-stage build to be added later.

Volumes:
- `/app/app_data` mounted for audio + sqlite db.

Future:
- Add health endpoint `/health` and use Docker HEALTHCHECK.
- Multi-stage (sdk -> runtime) to shrink image.
