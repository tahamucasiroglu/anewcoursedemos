#!/bin/sh
# Taşınabilir test PostgreSQL'inin yerleri (plan 0001, R-1). Repo DIŞINDA durur; repoya girmez.
# Docker kurulunca (B-1) entegrasyon testleri Testcontainers'a geçer ve bu klasör kalkar.
PG_VERSION="17.12-1"
PG_URL="https://get.enterprisedb.com/postgresql/postgresql-${PG_VERSION}-windows-x64-binaries.zip"
DEV_HOME="${LOCALAPPDATA:-$HOME/.local/share}/anewconcept-dev"
PG_HOME="$DEV_HOME/pgsql"
PG_BIN="$PG_HOME/bin"
