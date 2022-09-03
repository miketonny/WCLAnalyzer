var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors(opt => opt.AddPolicy("CorsPolicy", c =>
{
    c.AllowAnyOrigin()
       .AllowAnyHeader()
       .AllowAnyMethod();
}));

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

// Shows UseCors with CorsPolicyBuilder.
app.UseCors("CorsPolicy");

app.UseGraphQLGraphiQL();
app.UseGraphQLAltair();
app.UseHttpsRedirection();



app.UseAuthorization();

app.MapControllers(); 

app.Run();
