# Prep API

A personal API for cooking and meal preparation management.

## Quick Setup
```
dotnet user-jwts create --audience prep-api
```
```
docker run --name prep-db -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=prep -p 5432:5432 -d postgres
```

http://localhost:5053/scalar/v1