CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE TABLE IF NOT EXISTS auth_users (
  id uuid PRIMARY KEY,
  email text NOT NULL UNIQUE,
  display_name text NOT NULL,
  avatar_url text NULL,
  bio text NULL,
  credits integer NOT NULL DEFAULT 3,
  password_hash text NOT NULL,
  roles text[] NOT NULL DEFAULT ARRAY['user']::text[],
  refresh_token_hash text NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
);

ALTER TABLE auth_users ADD COLUMN IF NOT EXISTS avatar_url text NULL;
ALTER TABLE auth_users ADD COLUMN IF NOT EXISTS bio text NULL;
ALTER TABLE auth_users ADD COLUMN IF NOT EXISTS credits integer NOT NULL DEFAULT 3;

CREATE TABLE IF NOT EXISTS wallet_transactions (
  id uuid PRIMARY KEY,
  user_id uuid NOT NULL REFERENCES auth_users(id) ON DELETE CASCADE,
  amount integer NOT NULL,
  type text NOT NULL,
  description text NULL,
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_wallet_transactions_user_created_at
  ON wallet_transactions (user_id, created_at DESC);

CREATE TABLE IF NOT EXISTS king_rooms (
  id uuid PRIMARY KEY,
  name text NOT NULL,
  status text NOT NULL DEFAULT 'WaitingChallenger',
  king_user_id uuid NOT NULL REFERENCES auth_users(id) ON DELETE CASCADE,
  challenger_user_id uuid NULL REFERENCES auth_users(id) ON DELETE SET NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS king_room_queue_entries (
  id uuid PRIMARY KEY,
  room_id uuid NOT NULL REFERENCES king_rooms(id) ON DELETE CASCADE,
  user_id uuid NOT NULL REFERENCES auth_users(id) ON DELETE CASCADE,
  position integer NOT NULL,
  joined_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE (room_id, user_id),
  UNIQUE (room_id, position)
);

CREATE INDEX IF NOT EXISTS idx_king_rooms_created_at
  ON king_rooms (created_at DESC);

CREATE INDEX IF NOT EXISTS idx_king_room_queue_entries_room_position
  ON king_room_queue_entries (room_id, position ASC);

CREATE TABLE IF NOT EXISTS king_match_sessions (
  id uuid PRIMARY KEY,
  room_id uuid NOT NULL REFERENCES king_rooms(id) ON DELETE CASCADE,
  status text NOT NULL,
  king_user_id uuid NOT NULL REFERENCES auth_users(id) ON DELETE CASCADE,
  challenger_user_id uuid NOT NULL REFERENCES auth_users(id) ON DELETE CASCADE,
  created_by_user_id uuid NOT NULL REFERENCES auth_users(id) ON DELETE CASCADE,
  reported_by_user_id uuid NULL REFERENCES auth_users(id) ON DELETE SET NULL,
  winner_user_id uuid NULL REFERENCES auth_users(id) ON DELETE SET NULL,
  loser_user_id uuid NULL REFERENCES auth_users(id) ON DELETE SET NULL,
  game_rom text NULL,
  launch_source text NULL,
  client_instance_id text NULL,
  emulator_process_id integer NULL,
  result_source text NULL,
  last_idempotency_key text NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  started_at timestamptz NULL,
  ended_at timestamptz NULL
);

CREATE TABLE IF NOT EXISTS king_match_history (
  id uuid PRIMARY KEY,
  room_id uuid NOT NULL REFERENCES king_rooms(id) ON DELETE CASCADE,
  match_session_id uuid NOT NULL REFERENCES king_match_sessions(id) ON DELETE CASCADE,
  winner_user_id uuid NOT NULL REFERENCES auth_users(id) ON DELETE CASCADE,
  loser_user_id uuid NOT NULL REFERENCES auth_users(id) ON DELETE CASCADE,
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_king_match_sessions_room_status
  ON king_match_sessions (room_id, status);

CREATE INDEX IF NOT EXISTS idx_king_match_history_room_created_at
  ON king_match_history (room_id, created_at DESC);

CREATE INDEX IF NOT EXISTS idx_auth_users_email ON auth_users (email);