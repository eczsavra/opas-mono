CREATE TABLE token_store (
  id serial PRIMARY KEY,
  name text NOT NULL, -- "ITS-Access"
  token text NOT NULL,
  expires_at_utc timestamptz NULL,
  created_at timestamptz DEFAULT now()
);
