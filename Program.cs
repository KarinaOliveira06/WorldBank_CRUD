using microsoft.EntityFrameworkCore;
using WorldBank_Crud.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(Options =>
 options.UseSqlite(connectionString));

 builder.Services.AddControllers();

 var app = builder.Build();

 app.MapControllers();

 app.Run();