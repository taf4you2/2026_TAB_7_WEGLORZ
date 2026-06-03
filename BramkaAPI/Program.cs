using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;
using BramkaAPI.Services;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
// builder.Services.AddHostedService<SyncBackgroundService>(); // Wyłączone w środowisku współdzielonej bazy danych

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var dbPassword = builder.Configuration["DbPassword"];

var npgsqlBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
{
    Password = dbPassword
};

builder.Services.AddDbContext<SkiResortDbContext>(options =>
    options.UseNpgsql(npgsqlBuilder.ConnectionString));

var app = builder.Build();

app.MapControllers();

app.Run();