using Microsoft.Extensions.AI;

namespace ChatBot.DeepSeek;

public class DeepSeekEmbeddingGenerator : StringEmbeddingGenerator
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly HttpClient _httpClient;

    public DeepSeekEmbeddingGenerator(string apiKey, string model = "deepseek-embedding")
    {
        _apiKey = apiKey;
        _model = model;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Embedding<float[]>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = _model,
            input = text
        };

        var response = await _httpClient.PostAsJsonAsync("https://api.deepseek.com/v1/embeddings", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<DeepSeekEmbeddingResponse>();
        return new Embedding<float[]>(result.Data[0].embedding);
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        throw new NotImplementedException();
    }
}
