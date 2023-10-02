using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using SocialMediaAnalysis.Service;
using Swashbuckle.AspNetCore.Filters;
using Microsoft.Extensions.ML;
using SocialMediaAnalysis.Models;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

//this package is for adding swagger. Get more information on https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-7.0&tabs=visual-studio-code
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Description = "Standard Authorization header using the bearer scheme (\"bearer {token}\")",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
    });

    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration.GetSection("Identity:Token").Value)),
            ValidateIssuer = false,
            ValidateAudience = false,
        };
    });


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAnyOriginPolicy",
        builder => { builder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin(); });
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 268435456; // Set the maximum request body size for file uploads (in bytes)
    options.ValueLengthLimit = int.MaxValue; // Set the maximum length of individual form entries (in bytes)
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

//These changes are added to for ML.Net and sentiment analysis
builder.Services.AddPredictionEnginePool<ModelInput, ModelOutput>().FromFile(modelName: "SentimentAnalysisModel",
    filePath: "sentiment_model.zip", watchForChanges: true);

//configure httpClient
builder.Services.AddHttpClient("azure", client =>
{
    client.BaseAddress = new Uri("https://sna-analysis.cognitiveservices.azure.com/");
    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "12ab8c545dcc4e648e8fa4064d32e2a4");
});

//Add MongoDB connection settings
var connectionString = builder.Configuration["DatabaseSettings:MongoConnectionString"];
var databaseName = builder.Configuration["DatabaseSettings:DatabaseName"];

//register DB
builder.Services.AddScoped(c => new MongoClient(connectionString).GetDatabase(databaseName));


//register services
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<ICsvService, CsvService>();
builder.Services.AddScoped<ITweetService, TweetsService>();
builder.Services.AddScoped<IKeywordService, KeywordService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowAnyOriginPolicy");

app.MapControllers();

app.Run();