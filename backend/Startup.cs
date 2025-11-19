using ChatBot.Services;
using Microsoft.Extensions.AI;
using Pinecone;

namespace ChatBot;

static class Startup
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        //var deepSeekKey = builder.RequireEnv("OPENAI_API_KEY");
        var deepSeekKey = builder.RequireEnv("sk-89c9860f10ae4e40b8e1fa7b3eb004cd");
        //var pineconeKey = builder.RequireEnv("PINECONE_API_KEY");
        var pineconeKey = builder.RequireEnv("pcsk_tiT86_9o2EYETFgUs6tQPo7ApVsQizxkkrQZjFBDQsJMNw93mxvU3g7rY4eZ8L7hiXtSo");

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("FrontendCors", policy =>
                policy
                    .WithOrigins("http://localhost:3000")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
            );
        });

        //builder.Services.AddSingleton<StringEmbeddingGenerator>(s => new OpenAI.Embeddings.EmbeddingClient(
        builder.Services.AddSingleton<StringEmbeddingGenerator>(s => new DeepSeekEmbeddingGenerator(
                //model: "text-embedding-3-small",
                model: "deepseek-embedding",
                apiKey: deepSeekKey
            ).AsIEmbeddingGenerator());

        builder.Services.AddSingleton<IndexClient>(s => new PineconeClient(pineconeKey).Index("landmark-chunks"));

        builder.Services.AddSingleton<DocumentChunkStore>(s => new DocumentChunkStore());

        builder.Services.AddSingleton<VectorSearchService>();

        builder.Services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Information));

        builder.Services.AddSingleton<ILoggerFactory>(sp =>
            LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information)));

        builder.Services.AddSingleton<IChatClient>(sp =>
         {
             var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
             //var client = new OpenAI.Chat.ChatClient(
             var client = new DeepSeekChatClient(
                  "deepseek-chat",
                  deepSeekKey).AsIChatClient();

             return new ChatClientBuilder(client)
                 .UseLogging(loggerFactory)
                 .UseFunctionInvocation(loggerFactory, c =>
                 {
                     c.IncludeDetailedErrors = true;
                 })
                 .Build(sp);
         });

        builder.Services.AddTransient<ChatOptions>(sp => new ChatOptions
        {
            Tools = FunctionRegistry.GetTools(sp).ToList(),
        });

        builder.Services.AddSingleton<WikipediaClient>();
        builder.Services.AddSingleton<IndexBuilder>();
        builder.Services.AddSingleton<RagQuestionService>();
        builder.Services.AddSingleton<ArticleSplitter>();
        builder.Services.AddSingleton<PromptService>();
    }
}
