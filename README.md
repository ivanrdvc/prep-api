# Prep API

[![API Deploy Status](https://img.shields.io/github/actions/workflow/status/ivanrdvc/prep-api/main_prep-api.yml?label=Deploy)](https://github.com/ivanrdvc/prep-api/actions/workflows/main_prep-api.yml)

A personal API for cooking and meal preparation management.

## Dev Setup

```
dotnet user-jwts create --audience prep-api
```

```
docker run --name prep_db -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=prep_db -p 5432:5432 -d postgres
```