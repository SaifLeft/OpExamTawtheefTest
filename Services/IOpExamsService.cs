using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TawtheefTest.DTOs.ExamModels;

namespace TawtheefTest.Services
{
  public interface IOpExamsService
  {
    Task<List<ExamQuestionDTO>> GenerateQuestions(string topic, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<List<ExamQuestionDTO>> GenerateQuestionsFromText(string text, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<List<ExamQuestionDTO>> GenerateQuestionsFromLink(string link, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<List<ExamQuestionDTO>> GenerateQuestionsFromYoutube(string youtubeLink, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<string> UploadFile(Stream fileStream, string fileName);
    Task<List<ExamQuestionDTO>> GenerateQuestionsFromDocument(string documentId, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<List<ExamQuestionDTO>> GenerateQuestionsFromImage(string imageId, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<List<ExamQuestionDTO>> GenerateQuestionsFromAudio(string audioId, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<List<ExamQuestionDTO>> GenerateQuestionsFromVideo(string videoId, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<List<ExamQuestionDTO>> GenerateQuestionsWithOrderingOrMatching(string topic, string questionType, string language, string difficulty, int questionCount, int numberOfRows);
    Task<List<ExamQuestionDTO>> GenerateQuestionsWithMultiSelect(string topic, string questionType, string language, string difficulty, int questionCount, int optionsCount, int numberOfCorrectOptions);
    Task GenerateQuestionsAsync(int questionSetId);
  }
}
