using backend.Repositories;
using backend.Services;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson;
using MongoDB.Driver;

BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddPolicy("frontend", policy =>
    policy.WithOrigins(builder.Configuration["FrontendOrigin"] ?? "http://localhost:5173")
          .AllowAnyHeader().AllowAnyMethod()));
var connection = builder.Configuration["MongoDb:ConnectionString"] ?? "mongodb://localhost:27017";
var databaseName = builder.Configuration["MongoDb:DatabaseName"] ?? "group_assignment_manager";
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(connection));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(databaseName));
builder.Services.AddSingleton<UserRepository>();
builder.Services.AddSingleton<ProjectRepository>();
builder.Services.AddSingleton<TaskRepository>();
builder.Services.AddScoped<TaskService>();
var app = builder.Build();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.UseCors("frontend");
app.MapControllers();
app.Run();
