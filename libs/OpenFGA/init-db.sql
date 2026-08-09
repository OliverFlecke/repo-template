-- Runs against the shared postgres instance on first init (see
-- docker-entrypoint-initdb.d) to give OpenFGA its own database within the
-- same postgres server used by the API.
CREATE DATABASE openfga;
