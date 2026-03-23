# Project Map

- `src/Htrack.Api/`: single .NET 8 ASP.NET Core application.
- `src/Htrack.Api/Program.cs`: app bootstrap, Swagger, service registration, middleware pipeline.
- `src/Htrack.Api/Controllers/`: API endpoints for companies, employees, attendances, and reports.
- `src/Htrack.Api/Pages/`: Razor Pages UI, including admin pages and login.
- `src/Htrack.Api/Pages/Admin/Reports/`: admin-side Excel report download UI.
- `src/Htrack.Api/Services/`: business services, including Excel report generation and background jobs.
- `src/Htrack.Api/TelegramBotServices/`: Telegram bot update handling, commands, and conversation state.
- `src/Htrack.Api/Data/`: EF Core context, migrations host service, and seed/bootstrap logic.
- `src/Htrack.Api/Entities/`: domain entities such as `Company`, `Employee`, and `Attendance`.
- `src/Htrack.Api/Repositories/`: data access for employees, attendances, and related queries.
- `src/Htrack.Api/Extensions/`: DI and app setup extensions, including database, admin UI, and Telegram bot wiring.
- `tests/Htrack.Api.Tests/`: xUnit safety-net tests for calendar rules, duplicate scan logic, and Excel report output.
- `src/Htrack.Api/appsettings.json`: application configuration.
- `src/Htrack.Api/Properties/launchSettings.json`: local development launch profiles.
- `htc_infra/`: Docker Compose and infrastructure assets for app, Traefik, and Postgres.
- `Dockerfile`: container build for the ASP.NET Core app.
- `.env.example`: environment variable template.
- `Htrack.ino`: Arduino/ESP32 RFID client posting attendance data into the API.

# Feature Map

- Reporting:
  - Company-wide Excel reports are generated in `src/Htrack.Api/Services/ExcelReportService.cs`.
  - Per-employee Excel reports are generated in `src/Htrack.Api/Services/ExcelReportService.Employee.cs`.
  - Report anomaly/manual marker helpers live in `src/Htrack.Api/Services/ExcelReportService.Anomalies.cs`.
  - Telegram report flows live in `src/Htrack.Api/TelegramBotServices/Handlers/`.
- Telegram bot:
  - Main message routing is in `src/Htrack.Api/TelegramBotServices/Handlers/BotUpdateHandler.Message.cs`.
  - Button/command handlers without extra arguments are in `src/Htrack.Api/TelegramBotServices/Handlers/BotUpdateHandler.CommandsWithoutArgument.cs`.
  - Multi-step employee report flows are in `src/Htrack.Api/TelegramBotServices/Handlers/BotUpdateHandler.EmployeeReports.cs`.
- Admin UI:
  - Report downloads are handled by `src/Htrack.Api/Pages/Admin/Reports/Index.cshtml.cs`.
- Reliability/testing:
  - Business calendar rules live in `src/Htrack.Api/Utilities/AttendanceBusinessRules.cs`.
  - Attendance source persistence is defined on `src/Htrack.Api/Entities/Attendance.cs` and migrated in `src/Htrack.Api/Data/Migrations/`.
