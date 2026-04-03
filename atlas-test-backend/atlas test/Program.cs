using atlas_test.Application.Configuration;
using atlas_test.Application.Services;
using atlas_test.Common;
using atlas_test.Infrastructure.OpenAI;
using atlas_test.Infrastructure.VectorDb;
using atlas_test.Services;

var builder = WebApplication.CreateBuilder(args);

AddFrameworkServices(builder.Services);
AddOptions(builder.Services, builder.Configuration);
AddApplicationServices(builder.Services);

var app = builder.Build();

if (!app.Environment.IsProduction())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

static void AddFrameworkServices(IServiceCollection services)
{
	services.AddControllers();
	services.AddEndpointsApiExplorer();
	services.AddSwaggerGen();
	services.AddHttpClient();
	services.AddCors(options =>
	{
		options.AddPolicy("AllowFrontend", policy =>
		{
			policy
				.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://localhost:4173")
				.AllowAnyHeader()
				.AllowAnyMethod();
		});
	});
}

static void AddOptions(IServiceCollection services, IConfiguration configuration)
{
	services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));
	services.Configure<QdrantOptions>(configuration.GetSection(QdrantOptions.SectionName));
	services.Configure<DataOptions>(configuration.GetSection(DataOptions.SectionName));
	services.Configure<RetrievalOptions>(configuration.GetSection(RetrievalOptions.SectionName));
}

static void AddApplicationServices(IServiceCollection services)
{
	services.AddSingleton<ITextChunker, TextChunker>();
	services.AddSingleton<IPiiRedactionService, PiiRedactionService>();
	services.AddSingleton<IOpenAiClient, OpenAiClient>();
	services.AddSingleton<IVectorStore, QdrantVectorStore>();
	services.AddScoped<IIngestionService, IngestionService>();
	services.AddScoped<IRetrievalService, RetrievalService>();
	services.AddScoped<IChatService, ChatService>();
	services.AddScoped<IEvaluationService, EvaluationService>();
}

