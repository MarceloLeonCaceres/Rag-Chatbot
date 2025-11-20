using ChatBot.DeepSeek;
using ChatBot.Services;
using Microsoft.Extensions.AI;
using OpenAI;
using Pinecone;
using System.ClientModel;

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
        // Register DeepSeek Embedding generator directly
        //builder.Services.AddSingleton<StringEmbeddingGenerator>(s => new DeepSeekEmbeddingGenerator(
        //    apiKey: deepSeekKey,
        //    model: "deepseek-embedding" // Use correct DeepSeek Embedding model
        //));
        builder.Services.AddSingleton<OpenAIClient>(sp =>
        {
            return new OpenAIClient(
                new ApiKeyCredential(deepSeekKey),
                new OpenAIClientOptions
                {
                    Endpoint = new Uri("https://api.deepseek.com") // <--- The Magic: Points standard client to DeepSeek
                });
        });

        builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            var openAiClient = sp.GetRequiredService<OpenAIClient>();

            // Use the official "AsEmbeddingGenerator" adapter
            return openAiClient.GetEmbeddingClient("deepseek-embedding")
                .AsIEmbeddingGenerator();
        });

        builder.Services.AddSingleton<IndexClient>(s => new PineconeClient(pineconeKey).Index("landmark-chunks"));

        builder.Services.AddSingleton<DocumentChunkStore>(s => new DocumentChunkStore());

        builder.Services.AddSingleton<VectorSearchService>();

        builder.Services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Information));

        builder.Services.AddSingleton<ILoggerFactory>(sp =>
            LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information)));

        builder.Services.AddSingleton<IChatClient>(sp =>
        {
            var openAiClient = sp.GetRequiredService<OpenAIClient>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            // Get the low-level client for "deepseek-chat"
            var chatClient = openAiClient.GetChatClient("deepseek-chat");

            // Build the pipeline using the official builder
            return new ChatClientBuilder(chatClient.AsIChatClient()) // Adapts OpenAI to Microsoft.Extensions.AI
                .UseFunctionInvocation(loggerFactory)                 // <--- Handles Tool Calls automatically!
                .UseLogging(loggerFactory)
                .Build();
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
