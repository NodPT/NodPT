namespace NodPT.Data.DTOs
{
    public class OllamaMessage
    {
        public OllamaMessage()
        {
        }

        public string role { get; set; } = string.Empty;
        public string content { get; set; } = string.Empty;
    }
}
