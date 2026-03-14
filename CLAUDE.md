# HTrack — Project Context for Claude

## What This Project Is
Multi-tenant employee attendance tracking system. Companies register their employees with RFID cards. An ESP32 module reads RFID cards and POSTs to this API, which toggles check-in/check-out. Managers receive real-time Telegram notifications and can generate Excel reports.

**Stack:** .NET 8 Web API · PostgreSQL 16 · Entity Framework Core 9 · Telegram.Bot · ClosedXML · Docker + Traefik · Razor Pages (Admin UI)

---

## Architecture

```
src/Htrack.Api/
├── Controllers/        # Thin — delegates to Services
├── Services/           # Business logic (interface + impl)
│   └── AttendanceCleanupService.cs  # BackgroundService: deletes records >6 months old, runs every 24h
├── Repositories/       # Data access (interface + impl)
├── Entities/           # EF Core entities + Fluent API configs
├── Dtos/               # Request/response models
├── Mappers/            # ToDto() / ToEntity() extension methods
├── Exceptions/         # Domain exceptions (CompanyNotFoundException, etc.)
├── TelegramBotServices/# Bot update handler, background service, notifier
├── Utilities/          # TimeHelper (UTC → Asia/Tashkent)
├── Data/               # HTrackDbContext, IHTrackDbContext
├── Abstractions/       # Shared interfaces
└── Pages/              # Razor Pages Admin UI
    ├── Login.cshtml(.cs)            # Public login — cookie auth
    └── Admin/
        ├── _Layout.cshtml           # Bootstrap 5 CDN, sidebar nav, active link detection
        ├── Companies/               # CRUD + CompanyFormHelper (shared ParseManagerIds)
        ├── Employees/               # CRUD with company-scoped filter
        ├── Attendances/             # Today's checked-in + checked-out view
        └── Reports/                 # Excel download (4 report types)
```

**Pattern:** Controller → Service → Repository → DbContext
**Admin UI pattern:** PageModel → Service → (same Services layer as API)

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
| `AdminCredentials:Username` | Admin UI login (default: `admin`) |
| `AdminCredentials:Password` | Admin UI password (default: `Admin123!`) |

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

### Pending State Machine (`BotUpdateHandler`)

Multi-step commands use `ConcurrentDictionary<long, string> pendingCommands` keyed by Telegram user ID.

| State key | Set by | Awaiting |
|-----------|--------|----------|
| `"updateEmployee"` | `/update_employee` | `RFID, Full Name` |
| `"newAttendance"` | `/new_attendance` | RFID UID |
| `"awaitingFromDate"` | `/custom_report` | `dd.MM.yyyy` from-date |
| `"customReport:{yyyy-MM-dd}"` | `awaitingFromDate` success | `dd.MM.yyyy` to-date |

**Escape & retry rules (implemented 2026-03-14):**
- **Command interception**: any keyboard button or `/command` received while in a pending state is detected via `MapButtonToCommand(text).StartsWith('/')` → state is cleared and the command executes normally.
- **Retry limit**: `ConcurrentDictionary<long, int> retryCounters` tracks consecutive format/validation errors per user. On the 3rd failure, `ClearUserPendingState()` is called and the user is told to restart the command. Counter resets on success or state transition.
- **`/cancel`**: explicit escape hatch, always clears state (idempotent).
- **`ClearUserPendingState(long userId)`**: private helper that removes from both `pendingCommands` and `retryCounters`. All state clears go through this — never call `.Remove` on the dicts directly.

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
- `Npgsql.EntityFrameworkCore.PostgreSQL` — DB provider
- `Telegram.Bot` — bot integration
- `Swashbuckle.AspNetCore` — Swagger
- Razor Pages + Cookie Auth — built-in ASP.NET Core, no extra packages

---

## Development Notes

- **API endpoints are public** — no JWT/API key. Admin UI is protected by cookie auth. Future work: add API-level auth.
- The ESP32 firmware (`Htrack.ino`) in the repo root calls `POST api/Attendances/create-attendance/{companyId}/{rfidUID}`. The RFID UID is space-delimited hex bytes encoded as `%20` in the URL.
- Docker Compose is in `htc_infra/docker-compose.yml`. Needs a `.env` file (not in repo) for secrets.
- Production URL: `https://htrack.ilmhub.uz`
- Admin UI: `https://htrack.ilmhub.uz/admin/companies` (redirects to `/Login` if unauthenticated)

---

## Admin UI (Razor Pages) — Conventions & Pitfalls

### Auth
- Cookie auth via `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)`.
- `AuthorizeFolder("/Admin")` in `AddRazorPages` options — ALL pages under `Pages/Admin/` require auth.
- Login page is at `Pages/Login.cshtml` (outside `/Admin/`, so public).
- Credentials read from `IConfiguration["AdminCredentials:Username/Password"]`.

### Page Model patterns
- Page model properties are `IReadOnlyList<T>` (not `IEnumerable<T>`) — always materialise with `.ToList()` in `OnGet`. Prevents double-enumeration in views (`foreach` + `Count` + `Any` = 3 DB hits if lazy).
- Write operations set `TempData["Success"]` on redirect and `Model.Error` on `Page()` re-render.
- Global `TempData["Success"]` alert is rendered in `_Layout.cshtml` — page-local errors go in `Model.Error`.
- All `<label>` elements must have a `for="inputId"` and matching `<input id="inputId">` for accessibility.

### Layout
- `_Layout.cshtml` uses `d-flex flex-column` on `<nav>` so `mt-auto` on the logout button actually works.
- Active nav link: `ViewContext.RouteData.Values["page"]?.ToString()` compared with `StartsWith("/Admin/Section")`.
- `ViewData["Title"]` is rendered as `{Title} — HTrack Admin` in the browser tab.

### Forms
- All forms inside `Pages/` use the Form Tag Helper (tag helpers activated in `_ViewImports.cshtml`), which auto-injects the anti-forgery token for `method="post"` forms.
- **Never put company/report selects outside the `<form>`** — the old pattern of copying values via `onclick` into separate hidden fields is brittle and was replaced with a single form + one hidden `reportType` input toggled by button `onclick`.

### Shared utilities
- `Pages/Admin/Companies/CompanyFormHelper.cs` — `ParseManagerIds(string?)` converts comma-separated string → `List<long>`. Used by both Create and Edit. Do not duplicate.
- `Utilities/TimeHelper.ToUzbekistanTime(DateTime)` — use for all display-side time conversion in views.
- Duration formatting: `$"{(int)a.Duration.TotalHours:D2}:{a.Duration.Minutes:D2}"` — **not** `TimeSpan.ToString("hh:mm")` (that truncates at 24h).

---

## Validate Changes

```bash
dotnet build src/Htrack.Api/Htrack.Api.csproj
dotnet ef migrations list --project src/Htrack.Api
```
