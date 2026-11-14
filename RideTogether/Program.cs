using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RideTogether.Context;
using RideTogether.Converters;
using RideTogether.Interfaces;
using RideTogether.Interfaces.Converters;
using RideTogether.Interfaces.Services;
using RideTogether.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "MyAllowSpecificOrigins",
        policy =>
        {
            policy.WithOrigins("https://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

IConfiguration configuration = builder.Configuration;
// Add services to the container.

builder.Services.AddControllers();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException(
                           "ConnectionStrings:DefaultConnection is missing.");

builder.Services.AddDbContext<RideTogetherDbContext>(
    options => options.UseSqlite(connectionString));

builder.Services.AddScoped<IRideService, RideService>();
builder.Services.AddScoped<IRideBookingService, RideBookingService>();
builder.Services.AddScoped<IRideConverter, RideConverter>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserConverter, UserConverter>();
builder.Services.AddScoped(typeof(IPasswordHasher<>), typeof(PasswordHasher<>)); 
// Added AI-Search
builder.Services.AddHttpClient<RideSearchParser>(client =>
{
    client.BaseAddress = new Uri("http://localhost:11434/");
    client.Timeout = TimeSpan.FromSeconds(60);
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// До builder.Build()
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "RideTogether.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("MyAllowSpecificOrigins");
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();