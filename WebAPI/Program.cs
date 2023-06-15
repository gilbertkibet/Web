using Core.Configurations;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.Data;
using Infrastructure.ServicesImplementations;
using Microsoft.EntityFrameworkCore;
using System.Configuration;
using Hangfire;
using Infrastructure.ServicesImplementations.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//dbcontext
builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
    {
        options.UseSqlServer
        (
            builder.Configuration.GetConnectionString("DefaultConnection"),
            b=>b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
         );
    }
    );
builder.Services.Configure<CacheConfiguration>(builder.Configuration.GetSection("CacheConfiguration"));

//For In-Memory Caching
builder.Services.AddMemoryCache();
builder.Services.AddTransient<MemoryCacheService>();
builder.Services.AddTransient<RedisCacheService>();
builder.Services.AddTransient<Func<CacheTech, ICacheService>>(serviceProvider => key =>
{
    switch (key)
    {
        case CacheTech.Memory:
            return serviceProvider.GetService<MemoryCacheService>();
        case CacheTech.Redis:
            return serviceProvider.GetService<RedisCacheService>();
        default:
            return serviceProvider.GetService<MemoryCacheService>();
    }
});
#region Repositories
builder.Services.AddTransient(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddTransient<ICustomerRepository, CustomerRepository>();
#endregion
builder.Services.AddHangfire(x => x.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHangfireDashboard("/jobs");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
