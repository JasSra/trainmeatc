# Seed Data Notes

Airports (placeholders - fetch authoritative frequencies before production):
- YBBN Brisbane
- YSSY Sydney
- YMML Melbourne
- YPAD Adelaide

Seeding Process:
1. On first run, detect empty airport table.
2. Insert rows with placeholder frequencies (NULL) and comment referencing AIP requirement.
3. Provide admin utility to update with actual frequencies (DO NOT bake into source without citation).
