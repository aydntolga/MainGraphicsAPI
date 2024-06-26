using MainGraphicsAPI.Controllers;
using MainGraphicsAPI.Jobs;
using MainGraphicsAPI.Models;
using MainGraphicsAPI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddCors();
builder.Services.AddDbContext<MyDbContext>(options =>
{
    options.UseSqlServer("Data Source=localhost;Initial Catalog=Northwind;Integrated Security=True");
});

builder.Services.AddControllers();
QuartzScheduler.Start();

builder.Services.AddScoped<ITableService, TableServices>(); 


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors(options => options
    .WithOrigins("http://localhost:3000")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    );
app.UseAuthorization();

app.MapControllers();

app.Run();
