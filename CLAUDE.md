# HTrack — Project Context for Claude

## What This Project Is
Multi-tenant employee attendance tracking system. Companies register their employees with RFID cards. An ESP32 module reads RFID cards and POSTs to this API, which toggles check-in/check-out. Managers receive real-time Telegram notifications and can generate Excel reports.

**Stack:** .NET 8 Web API · PostgreSQL 16 · Entity Framework Core 9 · Hangfire · Telegram.Bot · ClosedXML · Docker + Traefik

---

## Architecture

```
src/Htrack.Api/
├── Controllers/        # Thin — delegates to Services
├── Services/           # Business logic (interface + impl)
├── Repositories/       # Data access (interface + impl)
├── Entities/           # EF Core entities + Fluent API configs
├── Dtos/               # Request/response models
├── Mappers/            # ToDto() / ToEntity() extension methods
├── Exceptions/         # Domain exceptions (CompanyNotFoundException, etc.)
├── TelegramBotServices/# Bot update handler, background service, notifier
├── Utilities/          # TimeHelper (UTC → Asia/Tashkent)
├── Data/               # HTrackDbContext, IHTrackDbContext
└── Abstractions/       # Shared interfaces
```

**Pattern:** Controller → Service → Repository → DbContext

---

## Domain Model

| Entity | Key Fields |
|--------|-----------|
| `Company` | `Id (Guid)`, `Name`, `TgChatID (long)`, `ManagerTgUserIDs (List<long> JSON)` |
| `Employee` | `Id (Guid)`, `Name`, `RFIDCardUID (unique)`, `CompanyId` |
| `Attendance` | `Id (Guid)`, `CheckIn`, `CheckOut?`, `Duration (interval)`, `EmployeeId` |

**Multi-tenancy:** Company-level isolation. All queries are scoped by `companyId`. No JWT/API auth — Telegram manager user IDs serve as access control.

**Check-in/out toggle logic** (`AttendancesService.HandleAttendanceAsync`):
- Last record has `CheckOut` (or no records) → create new check-in
- Last record has no `CheckOut` → set `CheckOut`, calculate `Duration`

---

## API Endpoints Summary

```
POST   api/Attendances/create-attendance/{companyId}/{rfidCardUID}   ← ESP32 calls this
GET    api/companies/
POST   api/companies/create-company
GET    api/employees/get-all-employees/{companyId}
POST   api/employees/create-employee
GET    api/reports/generate
GET    api/reports/download
```

---

## Key Configuration

| Key | Purpose |
|-----|---------|
| `ConnectionStrings:HTrack` | PostgreSQL connection string |
| `MigrateDatabase` | Run EF migrations at startup |
| `SeedData` | Seed test companies/employees with Bogus |
| `TelegramBot:Token` | Bot token (keep out of source control) |

**Timezone:** All display times converted to `Asia/Tashkent` via `TimeHelper`. DB stores UTC (`timestamp with time zone`).

---

## Database & Migrations

- Provider: `Npgsql.EntityFrameworkCore.PostgreSQL` v9.0.4
- EF Core v9.0.3
- Migrations live in `Entities/Migrations/`
- Add migration: `dotnet ef migrations add <Name> --project src/Htrack.Api`
- Migrations auto-run on startup when `MigrateDatabase: true`

---

## Telegram Bot

- Language: **Uzbek** (UI strings, command responses, report headers)
- Bot runs as a `BackgroundService` (polling mode)
- `BotUpdateHandler` is split into **partial classes** — one file per command
- Sends group notifications via `TelegramAttendanceNotifier` on each check-in/out
- Manager commands restricted by checking `Company.ManagerTgUserIDs`

---

## Excel Reports (ClosedXML)

Three report types, all stored in `/app/Reports/`:
1. Last 30 days
2. Last 15 days
3. Month-to-date (1st → today)

Old reports cleaned up hourly via Hangfire `ReportCleanupService`.

---

## NuGet Packages (check before adding)

- `Bogus` — seeding
- `ClosedXML` — Excel
- `Hangfire` + `Hangfire.PostgreSql` — background jobs
- `Npgsql.EntityFrameworkCore.PostgreSQL` — DB provider
- `Telegram.Bot` — bot integration
- `Swashbuckle.AspNetCore` — Swagger

---

## Development Notes

- **No authentication layer yet** — all endpoints are public. Future work.
- The ESP32 firmware (`Htrack.ino`) in the repo root calls `POST api/Attendances/create-attendance/{companyId}/{rfidUID}`. The RFID UID is space-delimited hex bytes encoded as `%20` in the URL.
- Docker Compose is in `htc_infra/docker-compose.yml`. Needs a `.env` file (not in repo) for secrets.
- Production URL: `https://htrack.ilmhub.uz`

---

## Validate Changes

```bash
dotnet build src/Htrack.Api/Htrack.Api.csproj
dotnet ef migrations list --project src/Htrack.Api
```
