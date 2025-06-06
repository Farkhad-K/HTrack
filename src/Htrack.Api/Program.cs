using HTrack.Api.Repositories;
using HTrack.Api.Services;
using HTrack.Api.TelegramBotServices;
using HTrack.Api.Abstractions.RepositoriesAbstractions;
using HTrack.Api.Abstractions.ServicesAbstractions;
using HTrack.Api.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using HTrack.Api.Abstractions;
using Hangfire;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<IHTrackDbContext, HTrackDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("HTrack")));
builder.Services.AddHostedService<MigrationsHostedService>();

builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(options =>
    {
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("HTrack"));
        // options.PrepareSchemaIfNecessary = true;
    })
);
builder.Services.AddHangfireServer();

// Repositories
builder.Services.AddScoped<ICompaniesRepository, CompaniesRepository>();
builder.Services.AddScoped<IEmployeesRepository, EmployeesRepository>();
builder.Services.AddScoped<IAttendancesRepository, AttendancesRepository>();

// Services
builder.Services.AddScoped<ICompaniesService, CompaniesService>();
builder.Services.AddScoped<IEmployeesService, EmployeesService>();
builder.Services.AddScoped<IAttendancesService, AttendancesService>();
builder.Services.AddScoped<IExcelReportService, ExcelReportService>();

builder.Services.AddScoped<ExcelExportService>(); // Test
builder.Services.AddScoped<ReportCleanupService>();

builder.Services.AddScoped<IAttendanceNotifier, TelegramAttendanceNotifier>();

// Telegram bot configuration
var botToken = builder.Configuration["TelegramBot:Token"]!;
builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
builder.Services.AddSingleton<IUpdateHandler, BotUpdateHandler>();
// builder.Services.AddSingleton(new TelegramBotClient(botToken));
builder.Services.AddHostedService<BotBackgroundService>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseHangfireDashboard();

RecurringJob.AddOrUpdate<ReportCleanupService>(
    "delete-old-reports",
    x => x.CleanupOldReports(),
    Cron.Hourly);

// RecurringJob.AddOrUpdate<ReportCleanupService>(
//     "delete-old-reports",
//     x => x.CleanupOldReports(),
//     "*/5 * * * *"    // every 5 minutes
// );

app.Run();
