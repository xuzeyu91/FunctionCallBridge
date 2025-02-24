using FunctionCallBridge.OpenAIModel;
using Newtonsoft.Json;

namespace FunctionCallBridge
{
    public class OpenAIRequest
    {
        public string model { get; set; }
        public List<Message> messages { get; set; }

        public double temperature { get; set; }
        public double top_p { get; set; }
        public bool stream { get; set; }
        public int max_tokens { get; set; }
        public double presence_penalty { get; set; }
        public double frequency_penalty { get; set; }

        public ResponseFormat? response_format { get; set; }
        public List<Tool>? tools { get; set; }
    }

    public class ResponseFormat
    {
        public string type { get; set; }
    }

    public class Message
    {
        public string role { get; set; }
        public string? content { get; set; }

        public List<ToolCall>? tool_calls { get; set; }
    }

    public class Tool
    {
        public string type { get; set; }
        public Function function { get; set; }
    }

    public class Function
    {
        public string name { get; set; }
        public string description { get; set; }
        public Parameters parameters { get; set; }
    }

    public class Parameters
    {
        public string type { get; set; }
        public Properties? properties { get; set; }
        public List<string> required { get; set; }
    }

    public class Properties
    {
        public Location? location { get; set; }
        public Unit? unit { get; set; }
    }

    public class Location
    {
        public string type { get; set; }
        public string description { get; set; }
    }

    public class Unit
    {
        public string type { get; set; }
        public List<string> Enum { get; set; }
    }

}
