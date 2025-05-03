using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TawtheefTest.DTOs.ExamModels;

namespace TawtheefTest.Services
{
  public interface IOpExamsService
  {
    Task<List<QuestionDTO>> GenerateQuestions(string topic, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<List<QuestionDTO>> GenerateQuestionsFromText(string text, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<List<QuestionDTO>> GenerateQuestionsFromLink(string link, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<List<QuestionDTO>> GenerateQuestionsFromYoutube(string youtubeLink, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<string> UploadFile(Stream fileStream, string fileName);
    Task<List<QuestionDTO>> GenerateQuestionsFromDocument(string documentId, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<List<QuestionDTO>> GenerateQuestionsFromImage(string imageId, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<List<QuestionDTO>> GenerateQuestionsFromAudio(string audioId, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<List<QuestionDTO>> GenerateQuestionsFromVideo(string videoId, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<List<QuestionDTO>> GenerateQuestionsWithOrderingOrMatching(string topic, string questionType, string language, string difficulty, int questionCount, int numberOfRows);
    Task<List<QuestionDTO>> GenerateQuestionsWithMultiSelect(string topic, string questionType, string language, string difficulty, int questionCount, int optionsCount, int numberOfCorrectOptions);
    Task<bool> GenerateQuestionsAsync(int questionSetId);
  }
}
