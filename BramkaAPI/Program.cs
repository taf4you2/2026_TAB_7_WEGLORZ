using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;
using BramkaAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<SyncBackgroundService>();

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