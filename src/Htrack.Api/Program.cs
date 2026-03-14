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

var builder = WebApplication.CreateBuilder(args);

// Bind to the port Render injects at runtime (falls back to 8080 locally)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddDbContext<IHTrackDbContext, HTrackDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("HTrack")));
builder.Services.AddHostedService<MigrationsHostedService>();

// Repositories
builder.Services.AddScoped<ICompaniesRepository, CompaniesRepository>();
builder.Services.AddScoped<IEmployeesRepository, EmployeesRepository>();
builder.Services.AddScoped<IAttendancesRepository, AttendancesRepository>();

// Services
builder.Services.AddScoped<ICompaniesService, CompaniesService>();
builder.Services.AddScoped<IEmployeesService, EmployeesService>();
builder.Services.AddScoped<IAttendancesService, AttendancesService>();
builder.Services.AddScoped<IExcelReportService, ExcelReportService>();

builder.Services.AddScoped<IAttendanceNotifier, TelegramAttendanceNotifier>();

// Telegram bot configuration
var botToken = builder.Configuration["TelegramBot:Token"]!;
builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
builder.Services.AddSingleton<IUpdateHandler, BotUpdateHandler>();
builder.Services.AddHostedService<BotBackgroundService>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapGet("/health", () => Results.Ok("OK"));

app.MapControllers();

app.Run();
