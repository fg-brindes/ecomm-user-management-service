-- User Management Service Database Setup
-- This script creates the usermanagement database in the cart-quote-db RDS instance

-- Create the database (run this as postgres user)
CREATE DATABASE usermanagement
    WITH
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.UTF-8'
    LC_CTYPE = 'en_US.UTF-8'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;

-- Connect to the new database
\c usermanagement

-- Grant privileges
GRANT ALL PRIVILEGES ON DATABASE usermanagement TO postgres;

-- The EF Core migrations will create all tables automatically when the service starts
