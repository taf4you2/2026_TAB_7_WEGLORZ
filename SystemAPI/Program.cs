using Microsoft.EntityFrameworkCore;
using SystemStacjiNarciarskiejDLL;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var dbPassword = builder.Configuration["DbPassword"];

var npgsqlBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
{
    Password = dbPassword
};

builder.Services.AddDbContext<SkiResortDbContext>(options =>
    options.UseNpgsql(npgsqlBuilder.ConnectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
app.MapControllers();
app.Run();
