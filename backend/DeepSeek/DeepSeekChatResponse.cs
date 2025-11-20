namespace ChatBot.DeepSeek;

public class DeepSeekChatResponse
{
    public List<DeepSeekChoice> Choices { get; set; } = new();
}