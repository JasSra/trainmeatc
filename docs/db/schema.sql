-- Core schema (initial cut; see comprehensive spec for details)
create table if not exists airport (
  icao text primary key,
  name text not null,
  lat real,
  lon real,
  atis_freq text,
  tower_freq text,
  ground_freq text,
  app_freq text
);

create table if not exists runway (
  id integer primary key autoincrement,
  airport_icao text not null,
  ident text not null,
  magnetic_heading integer,
  length_m integer,
  ils boolean,
  foreign key(airport_icao) references airport(icao)
);

create table if not exists scenario (
  id integer primary key autoincrement,
  name text,
  airport_icao text,
  kind text,
  difficulty text,
  seed integer,
  initial_state_json text,
  rubric_json text
);

create table if not exists session (
  id integer primary key autoincrement,
  user_id integer,
  scenario_id integer,
  started_utc text,
  ended_utc text,
  difficulty text,
  parameters_json text,
  score_total integer default 0,
  outcome text
);

create table if not exists turn (
  id integer primary key autoincrement,
  session_id integer,
  idx integer,
  user_audio_path text,
  user_transcript text,
  instructor_json text,
  atc_json text,
  tts_audio_path text,
  verdict text
);

create table if not exists metric (
  id integer primary key autoincrement,
  session_id integer,
  k text,
  v real,
  t_utc text
);

create table if not exists aircraft (
  id integer primary key autoincrement,
  type text not null,
  category text not null,
  manufacturer text not null,
  callsign_prefix text not null,
  cruise_speed integer,
  service_ceiling integer,
  wake_category text,
  engine_type text,
  seat_capacity integer
);

create table if not exists traffic_profile (
  id integer primary key autoincrement,
  aircraft_id integer not null,
  airport_icao text not null,
  callsign text not null,
  flight_type text,
  route text,
  frequency_weight real default 1.0,
  foreign key(aircraft_id) references aircraft(id),
  foreign key(airport_icao) references airport(icao)
);
