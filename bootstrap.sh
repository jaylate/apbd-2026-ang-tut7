#!/bin/bash
# Start the DB
docker compose up -d

# Populate the DB with tables and records
cat 01_create_and_seed_clinic.sql | docker compose exec -T db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P NotForPr0duction! -No
