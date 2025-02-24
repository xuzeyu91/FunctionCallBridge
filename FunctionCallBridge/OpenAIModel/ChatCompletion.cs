namespace FunctionCallBridge.OpenAIModel
{
    public class ChatCompletion
    {
        public string Id { get; set; }
        public string Object { get; set; }
        public long Created { get; set; }
        public string Model { get; set; }
        public string SystemFingerprint { get; set; }
        public List<Choice> Choices { get; set; }
        public Usage Usage { get; set; }
    }

    public class Choice
    {
        public int index { get; set; }
        public Message message { get; set; }
        public string finish_reason { get; set; }
    }

    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }

        public List<ToolCall> tool_calls { get; set; }
    }

    public class ToolCall
    {
        public string id { get; set; } = $"call_{Guid.NewGuid()}";
        public string type { get; set; } = "function";
        public Function function { get; set; }
    }

    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }

    public class FunctionDetails
    {
        public string id { get; set; }
        public string Type { get; set; }
        public Function function { get; set; }
    }

    public class Function
    {
        public string name { get; set; }
        public object arguments { get; set; }
    }
}
