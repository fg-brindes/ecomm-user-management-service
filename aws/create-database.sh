#!/bin/bash
set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${GREEN}Creating usermanagement database in cart-quote-db...${NC}"

RDS_ENDPOINT="cart-quote-db.cu3kuphp0skx.us-east-1.rds.amazonaws.com"
DB_USER="postgres"
DB_NAME="usermanagement"

echo -e "${YELLOW}Enter PostgreSQL password:${NC}"
read -s DB_PASSWORD

export PGPASSWORD="$DB_PASSWORD"

echo -e "${YELLOW}Testing connection...${NC}"
if psql -h "$RDS_ENDPOINT" -U "$DB_USER" -d postgres -c "SELECT version();" > /dev/null 2>&1; then
    echo -e "${GREEN}Connection successful${NC}"
else
    echo -e "${RED}Failed to connect. Check password and network access.${NC}"
    exit 1
fi

DB_EXISTS=$(psql -h "$RDS_ENDPOINT" -U "$DB_USER" -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$DB_NAME'")

if [ "$DB_EXISTS" == "1" ]; then
    echo -e "${YELLOW}Database already exists${NC}"
else
    echo -e "${YELLOW}Creating database...${NC}"
    psql -h "$RDS_ENDPOINT" -U "$DB_USER" -d postgres -c "CREATE DATABASE $DB_NAME OWNER postgres;"
    echo -e "${GREEN}Database created${NC}"
fi

echo -e "${YELLOW}Updating Secrets Manager...${NC}"
CONNECTION_STRING="Host=$RDS_ENDPOINT;Port=5432;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD"
aws secretsmanager update-secret --secret-id user-management/db-connection-string --secret-string "$CONNECTION_STRING" --region us-east-1 > /dev/null
echo -e "${GREEN}Secret updated${NC}"

unset PGPASSWORD

echo ""
echo -e "${GREEN}=== Setup Complete ===${NC}"
echo "Database: $DB_NAME @ $RDS_ENDPOINT"
