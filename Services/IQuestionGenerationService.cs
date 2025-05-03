using System.Threading.Tasks;
using TawtheefTest.DTOs;

namespace TawtheefTest.Services
{
    public interface IQuestionGenerationService
    {
        // إنشاء مجموعة أسئلة جديدة
        Task<int> CreateQuestionSetAsync(CreateQuestionSetDto model);

        // الحصول على حالة مجموعة الأسئلة
        Task<QuestionSetDto> GetQuestionSetStatusAsync(int questionSetId);

        // الحصول على تفاصيل مجموعة الأسئلة مع الأسئلة
        Task<QuestionSetDto> GetQuestionSetDetailsAsync(int questionSetId);

        // إعادة محاولة توليد الأسئلة لمجموعة فاشلة
        Task<bool> RetryQuestionGenerationAsync(int questionSetId);

        // إضافة الأسئلة المولدة إلى اختبار
        Task<bool> AddQuestionsToExamAsync(int questionSetId, int examId);
    }
}
