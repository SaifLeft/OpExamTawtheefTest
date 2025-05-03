using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TawtheefTest.DTOs
{
    public class ExamResultDetailDto
    {
        public int Id { get; set; }

        [Display(Name = "الاختبار")]
        public int ExamId { get; set; }

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
        public int TotalQuestions { get; set; }

        [Display(Name = "الإجابات الصحيحة")]
        public int CorrectAnswers { get; set; }

        [Display(Name = "الإجابات الخاطئة")]
        public int IncorrectAnswers { get; set; }

        [Display(Name = "تفاصيل الإجابات")]
        public List<AnswerDetailDto> Answers { get; set; }
    }

    public class AnswerDetailDto
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public string UserAnswer { get; set; }
        public string CorrectAnswer { get; set; }
        public bool IsCorrect { get; set; }
        public string Explanation { get; set; }
    }
}