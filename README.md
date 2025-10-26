# Prep API

[![API Deploy Status](https://img.shields.io/github/actions/workflow/status/ivanrdvc/prep-api/main_prep-api.yml?label=Deploy)](https://github.com/ivanrdvc/prep-api/actions/workflows/main_prep-api.yml)

A personal API for cooking and meal preparation management.

## Setup

```bash
dotnet user-jwts create --audience prep-api
```

```bash
docker run --name prep_db -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=prep_db -p 5432:5432 -d postgres
```

Set secrets in Bruno (Environments → local):
- `client_id` - Auth0 application client ID
- `client_secret` - Auth0 application client secret
- `audience` - API audience identifier
- `test_user_email` - Test user email address
- `test_user_password` - Test user password