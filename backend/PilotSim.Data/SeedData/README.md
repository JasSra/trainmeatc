# Seed Data for TrainMeATC

This directory contains JSON seed data files for the pilot training simulator.

## Files

- `airports.json` - Australian airport data with ICAO codes, frequencies, and runway information
- `aircraft.json` - Aircraft profiles for GA, Medium, and Heavy categories with realistic callsigns

## Usage

The seed data is automatically loaded by `DbInitializer.cs` when the database is first created.

## Aircraft Categories

- **GA (General Aviation)**: Cessna 172, 182, 210, Piper PA28, Diamond DA40
- **Medium**: DHC-8, ATR-72, Embraer E190, Boeing 737, Airbus A320
- **Heavy**: Boeing 777, 787, 747, Airbus A330, A380

## Australian Airports Included

- YMML - Melbourne Airport
- YSSY - Sydney Kingsford Smith Airport  
- YBBN - Brisbane Airport
- YPPH - Perth Airport
- YPAD - Adelaide Airport
- YBCG - Gold Coast Airport
- YSCB - Canberra Airport
- YBCS - Cairns Airport
- YPDN - Darwin Airport
- YMHB - Hobart Airport

All airports include realistic frequencies (Tower, Ground, Approach, ATIS) and runway configurations.