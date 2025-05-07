using System.Collections.Generic;
using System.Threading.Tasks;
using TawtheefTest.DTOs;
using TawtheefTest.ViewModels;

namespace TawtheefTest.Services
{
    public interface IQuestionSetLibraryService
    {
        /// <summary>
        /// الحصول على جميع مجموعات الأسئلة
        /// </summary>
        Task<List<QuestionSetDto>> GetAllQuestionSetsAsync();

        /// <summary>
        /// الحصول على تفاصيل مجموعة أسئلة معينة
        /// </summary>
        Task<QuestionSetDto> GetQuestionSetDetailsAsync(int id);

        /// <summary>
        /// إنشاء مجموعة أسئلة جديدة
        /// </summary>
        Task<int> CreateQuestionSetAsync(QuestionSetCreateViewModel model);

        /// <summary>
        /// إضافة مجموعة أسئلة إلى اختبار
        /// </summary>
        Task AddQuestionSetToExamAsync(int examId, int questionSetId, int displayOrder);

        /// <summary>
        /// نسخ مجموعة أسئلة
        /// </summary>
        Task<int> CloneQuestionSetAsync(int questionSetId);

        /// <summary>
        /// خلط خيارات الأسئلة في مجموعة أسئلة
        /// </summary>
        Task ShuffleQuestionOptionsAsync(int questionSetId, ShuffleType shuffleType);

        /// <summary>
        /// الحصول على مجموعات الأسئلة التي يمكن إعادة استخدامها
        /// </summary>
        Task<List<QuestionSetDto>> GetReusableQuestionSetsAsync();
    }
}
