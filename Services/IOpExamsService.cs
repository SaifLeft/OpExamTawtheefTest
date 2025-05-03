using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TawtheefTest.Models;

namespace TawtheefTest.Services
{
  public interface IOpExamsService
  {
    Task<List<Question>> GenerateQuestions(string topic, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<List<Question>> GenerateQuestionsFromText(string text, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<List<Question>> GenerateQuestionsFromLink(string link, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<List<Question>> GenerateQuestionsFromYoutube(string youtubeLink, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<string> UploadFile(Stream fileStream, string fileName);
    Task<List<Question>> GenerateQuestionsFromDocument(string documentId, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<List<Question>> GenerateQuestionsFromImage(string imageId, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<List<Question>> GenerateQuestionsFromAudio(string audioId, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<List<Question>> GenerateQuestionsFromVideo(string videoId, string questionType, string language, string difficulty, int questionCount, int optionsCount);
    Task<List<Question>> GenerateQuestionsWithOrderingOrMatching(string topic, string questionType, string language, string difficulty, int questionCount, int numberOfRows);
    Task<List<Question>> GenerateQuestionsWithMultiSelect(string topic, string questionType, string language, string difficulty, int questionCount, int optionsCount, int numberOfCorrectOptions);
  }
}
