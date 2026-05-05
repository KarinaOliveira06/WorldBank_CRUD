using Microsoft.EntityFrameworkCore;
using WorldBank_CRUD.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(Options =>
 Options.UseSqlite(connectionString));

 builder.Services.AddControllers();

 var app = builder.Build();

 app.MapControllers();

 app.Run();