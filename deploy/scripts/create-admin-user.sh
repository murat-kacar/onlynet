#!/bin/bash

# Create initial admin user for TabFlow Platform

dotnet ef migrations add InitialAdminUser --project /opt/onlynet/src/infra/postgres/TabFlow.Migrations.csproj --context PlatformDbContext --output-dir migrations/platform 2>&1
