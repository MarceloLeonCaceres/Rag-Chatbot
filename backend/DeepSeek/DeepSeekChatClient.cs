using Microsoft.Extensions.AI;

namespace ChatBot.DeepSeek;

public class DeepSeekChatClient : IChatClient
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly HttpClient _httpClient;

    public DeepSeekChatClient(string model, string apiKey)
    {
        _model = model;
        _apiKey = apiKey;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public async Task<ChatMessage> GetChatMessageAsync(IList<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        // Implement DeepSeek API call here
        var request = new
        {
            model = _model,
            messages = messages.Select(m => new { role = m.Role.ToString().ToLower(), content = m.Content }),
            stream = false
        };

        var response = await _httpClient.PostAsJsonAsync("https://api.deepseek.com/v1/chat/completions", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<DeepSeekChatResponse>();
        return new ChatMessage(ChatRole.Assistant, result.Choices[0].Message.Content);
    }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

}

