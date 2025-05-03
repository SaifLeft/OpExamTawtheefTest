using System.Threading.Tasks;
using TawtheefTest.DTOs;
using TawtheefTest.Enum;
using TawtheefTest.ViewModels;

namespace TawtheefTest.Services
{
  public interface IOpExamQuestionGenerationService
  {
    Task<string> UploadFileAsync(byte[] fileData, string fileName);
    Task<QuestionSetDto> GenerateQuestionsFromTopicAsync(int questionSetId, string topic, string questionType,
        int numberOfQuestions, string difficulty, string language = "Arabic");
    Task<QuestionSetDto> GenerateQuestionsFromTextAsync(int questionSetId, string text, string questionType,
        int numberOfQuestions, string difficulty, string language = "Arabic");
    Task<QuestionSetDto> GenerateQuestionsFromLinkAsync(int questionSetId, string link, string questionType,
        int numberOfQuestions, string difficulty, string language = "Arabic");
    Task<QuestionSetDto> GenerateQuestionsFromYoutubeAsync(int questionSetId, string youtubeLink, string questionType,
        int numberOfQuestions, string difficulty, string language = "Arabic");
    Task<QuestionSetDto> GenerateQuestionsFromDocumentAsync(int questionSetId, string documentUrl, string questionType,
        int numberOfQuestions, string difficulty, string language = "Arabic");
    Task<QuestionSetDto> GenerateQuestionsFromImageAsync(int questionSetId, string imageUrl, string questionType,
        int numberOfQuestions, string difficulty, string language = "Arabic");
    Task<QuestionSetDto> GenerateQuestionsFromAudioAsync(int questionSetId, string audioUrl, string questionType,
        int numberOfQuestions, string difficulty, string language = "Arabic");
    Task<QuestionSetDto> GenerateQuestionsFromVideoAsync(int questionSetId, string videoUrl, string questionType,
        int numberOfQuestions, string difficulty, string language = "Arabic");
    Task<QuestionSetDto> GetQuestionSetDetailsAsync(int questionSetId);
    Task<QuestionSetDto> GetQuestionSetStatusAsync(int questionSetId);
    Task<bool> RetryQuestionGenerationAsync(int questionSetId);
    Task<bool> AddQuestionsToExamAsync(int questionSetId, int examId);
  }
}
