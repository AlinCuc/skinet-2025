﻿using API.Middleware;
using Core.Entities;
using Core.Interfaces;
using Infrstructure.Data;
using Infrstructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddDbContext<StoreContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
});

builder.Services.AddCors();

builder.Services.AddSingleton<IConnectionMultiplexer>(config =>
{
    var connString = builder.Configuration.GetConnectionString("Redis") ?? throw new Exception("Cannot get redis connection string");
    var configuration = ConfigurationOptions.Parse(connString, true);
    return ConnectionMultiplexer.Connect(configuration);
});
builder.Services.AddSingleton<ICartService, CartService>();
builder.Services.AddIdentityApiEndpoints<AppUser>()
                .AddEntityFrameworkStores<StoreContext>()
                .AddDefaultTokenProviders();

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
});


// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowCredentials()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();


app.UseCors("CorsPolicy");
app.UseAuthentication();  // <- important dacă ai rute protejate
app.UseAuthorization();

app.MapControllers();
app.MapGroup("api").MapIdentityApi<AppUser>();

try
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<StoreContext>();
    await context.Database.MigrateAsync();
    await StoreContextSeed.SeedAsync(context);
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
    throw;
}


app.Run();
