using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace TawtheefTest.DTOs.OpExamModels
{
    public class OpExamsResponse
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("Questions")]
        public List<OpExamsQuestion> Questions { get; set; }
    }

    public class OpExamsQuestion
    {
        [JsonPropertyName("index")]
        public long index { get; set; }

        [JsonPropertyName("question")]
        public string question { get; set; }

        [JsonPropertyName("Options")]
        public List<string> Options { get; set; }

        [JsonPropertyName("answerIndex")]
        public long answerIndex { get; set; }

        [JsonPropertyName("Answer")]
        public string Answer { get; set; }

        [JsonPropertyName("id")]
        public string id { get; set; }

        // Properties for ordering questions
        [JsonPropertyName("instructionText")]
        public string instructionText { get; set; }

        [JsonPropertyName("correctlyOrdered")]
        public List<string> correctlyOrdered { get; set; }

        [JsonPropertyName("shuffledOrder")]
        public List<string> shuffledOrder { get; set; }

        // Properties for matching questions
        [JsonPropertyName("correctPairs")]
        public List<MatchingPair> correctPairs { get; set; }

        [JsonPropertyName("shuffledPairs")]
        public List<MatchingPair> shuffledPairs { get; set; }

        // Properties for multiSelect questions
        [JsonPropertyName("answerIndexes")]
        public List<int> answerIndexes { get; set; }
    }

    public class MatchingPair
    {
        [JsonPropertyName("left")]
        public string left { get; set; }

        [JsonPropertyName("right")]
        public string right { get; set; }
    }
}