using System.Text.Json.Serialization;

namespace TawtheefTest.DTOs.OpExamModels
{
    public class UploadFileResponse
    {
        [JsonPropertyName("url")]
        public string url { get; set; }
    }
}