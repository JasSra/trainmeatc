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
