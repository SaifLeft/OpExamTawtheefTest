using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TawtheefTest.DTOs
{
    public class ExamResultDetailDto
    {
        public long Id { get; set; }

        [Display(Name = "الاختبار")]
        public long ExamId { get; set; }

        [Display(Name = "اسم الاختبار")]
        public string ExamName { get; set; }

        [Display(Name = "اسم المتقدم")]
        public string ApplicantName { get; set; }

        [Display(Name = "البريد الإلكتروني")]
        public string ApplicantEmail { get; set; }

        [Display(Name = "وقت البدء")]
        public DateTime? StartTime { get; set; }

        [Display(Name = "وقت الانتهاء")]
        public DateTime? EndTime { get; set; }

        [Display(Name = "النتيجة")]
        public double Score { get; set; }

        [Display(Name = "إجمالي الأسئلة")]
        public long TotalQuestions { get; set; }

        [Display(Name = "الإجابات الصحيحة")]
        public long CorrectAnswers { get; set; }

        [Display(Name = "الإجابات الخاطئة")]
        public long IncorrectAnswers { get; set; }

        [Display(Name = "تفاصيل الإجابات")]
        public List<AnswerDetailDto> Answers { get; set; }
    }

    public class AnswerDetailDto
    {
        public long Id { get; set; }
        public long QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string UserAnswer { get; set; }
        public string CorrectAnswer { get; set; }
        public long IsCorrect { get; set; }
        public string Explanation { get; set; }
    }
}