using AutoMapper;
using TawtheefTest.Data.Structure;
using TawtheefTest.DTOs;
using TawtheefTest.DTOs.ExamModels;
using TawtheefTest.ViewModels;
using System.Linq;
using System;
using TawtheefTest.Enums;

namespace TawtheefTest.Infrastructure
{
  public class MappingProfiles : Profile
  {
    public MappingProfiles()
    {
      // Map from Data Models to DTOs
      CreateMap<Job, JobDTO>();
      CreateMap<Exam, ExamDTO>();
      CreateMap<Candidate, CandidateDTO>()
          .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.Phone.ToString()))
          .ForMember(dest => dest.JobName, opt => opt.MapFrom(src => src.Job != null ? src.Job.Title : null))
          .ReverseMap();

      CreateMap<Question, ExamQuestionDTO>();
      CreateMap<Option, QuestionOptionDTO>();
      CreateMap<OtpVerification, OTPVerificationDto>();
      CreateMap<CandidateAnswer, CandidateAnswerDTO>()
          .ForMember(dest => dest.AnswerText, opt => opt.MapFrom(src => src.AnswerText ?? string.Empty));

      // تخطيط CandidateExam إلى CandidateExamViewModel
      CreateMap<Assignment, AssignmentViewModel>()
          .ForMember(dest => dest.CandidateName, opt => opt.MapFrom(src => src.Candidate.Name))
          .ForMember(dest => dest.ExamTitle, opt => opt.MapFrom(src => src.Exam.Name))
          .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.Exam.Job.Title))
          .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.Exam.Duration))
          .ForMember(dest => dest.TotalQuestions, opt => opt.MapFrom(src => src.TotalQuestions > 0
              ? src.TotalQuestions
              : src.Exam.ExamQuestionSetMappings.SelectMany(eqs => eqs.QuestionSet.Questions).Count()))
          .ForMember(dest => dest.CompletedQuestions, opt => opt.MapFrom(src => src.CompletedQuestions > 0
              ? src.CompletedQuestions
              : src.CandidateAnswers.Select(ca => ca.QuestionId).Distinct().Count()));

      // تخطيط Exam إلى ExamForCandidateViewModel
      CreateMap<Exam, ExamForCandidateViewModel>()
          .ForMember(dest => dest.JobName, opt => opt.MapFrom(src => src.Job.Title))
          .ForMember(dest => dest.TotalQuestions, opt => opt.MapFrom(src => src.ExamQuestionSetMappings.SelectMany(eqs => eqs.QuestionSet.Questions).Count()))
          .ForMember(dest => dest.QuestionCountForEachCandidate, opt => opt.MapFrom(src => src.TotalQuestionsPerCandidate));

      // تخطيط CandidateExam إلى CandidateExamResultViewModel
      CreateMap<Assignment, AssignmentResultViewModel>()
          .ForMember(dest => dest.CandidateName, opt => opt.MapFrom(src => src.Candidate.Name))
          .ForMember(dest => dest.ExamTitle, opt => opt.MapFrom(src => src.Exam.Name))
          .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.Exam.Job.Title))
          .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.Exam.Duration))
          .ForMember(dest => dest.TotalQuestions, opt => opt.MapFrom(src => src.Exam.TotalQuestionsPerCandidate))
          .ForMember(dest => dest.CompletedQuestions, opt => opt.MapFrom(src => src.CompletedQuestions > 0
              ? src.CompletedQuestions
              : src.CandidateAnswers.Select(ca => ca.QuestionId).Distinct().Count()));

      // تخطيط CandidateAnswer إلى CandidateAnswerViewModel
      CreateMap<CandidateAnswer, CandidateAnswerViewModel>()
          .ForMember(dest => dest.QuestionText, opt => opt.MapFrom(src => src.Question.QuestionText))
          .ForMember(dest => dest.QuestionType, opt => opt.MapFrom(src => src.Question.QuestionType))
          .ForMember(dest => dest.CorrectAnswerText, opt => opt.MapFrom(src => src.Question.Answer))
          .ForMember(dest => dest.AnswerText, opt => opt.MapFrom(src => src.AnswerText ?? string.Empty))
          .ForMember(dest => dest.SubmittedAt, opt => opt.MapFrom(src => src.UpdatedAt ?? src.CreatedAt));

      // تخطيط Question إلى CandidateQuestionViewModel
      CreateMap<Question, CandidateQuestionViewModel>()
          .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.Options))
          .ForMember(dest => dest.OrderingItems, opt => opt.MapFrom(src => src.OrderingItems.Where(ss => ss.CorrectOrder == 0)))
          .ForMember(dest => dest.MatchingPairs, opt => opt.MapFrom(src => src.MatchingPairs))
          .ForMember(dest => dest.InstructionText, opt => opt.MapFrom(src => src.InstructionText));

      CreateMap<Option, QuestionOptionViewModel>();
      CreateMap<OrderingItem, OrderingItemViewModel>();
      CreateMap<MatchingPair, MatchingPairViewModel>()
          .ForMember(dest => dest.LeftSide, opt => opt.MapFrom(src => src.LeftItem))
          .ForMember(dest => dest.RightSide, opt => opt.MapFrom(src => src.RightItem));

      // للتعامل مع نماذج توليد الأسئلة
      CreateMap<QuestionSet, QuestionSetDto>()
          .ForMember(dest => dest.StatusDescription, opt => opt.MapFrom(src => GetStatusDescription(src.Status)))
          .ForMember(dest => dest.Questions, opt => opt.Ignore())
          .ForMember(dest => dest.UsageCount, opt => opt.Ignore())
          .ForMember(dest => dest.UsedInExams, opt => opt.Ignore())
          .ReverseMap();

      // تعيين Question إلى QuestionDto بدلاً من QuestionViewModel
      CreateMap<Question, QuestionDto>();
      CreateMap<QuestionViewModel, QuestionDto>();

      CreateMap<Question, QuestionViewModel>();
      CreateMap<Option, QuestionOptionViewModel>();

      // تخطيط Notification إلى NotificationViewModel
      CreateMap<Notification, NotificationViewModel>()
          .ForMember(dest => dest.TimeSince, opt => opt.MapFrom(src => GetTimeSince(src.CreatedAt)));

      // Map from DTOs to Data Models
      CreateMap<JobDTO, Job>();
      CreateMap<ExamDTO, Exam>();
      CreateMap<CandidateDTO, Candidate>();
      CreateMap<ExamQuestionDTO, Question>();
      CreateMap<QuestionOptionDTO, Option>();
      CreateMap<CandidateAnswerDTO, CandidateAnswer>()
          .ForMember(dest => dest.AnswerText, opt => opt.MapFrom(src => src.AnswerText));
      CreateMap<CreateExamDTO, Exam>();
      CreateMap<EditExamDTO, Exam>();
      CreateMap<CreateJobDTO, Job>();
      CreateMap<EditJobDTO, Job>();
      CreateMap<OTPVerificationDto, OtpVerification>()
        .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => (long)src.PhoneNumber))
        .ForMember(dest => dest.OtpCode, opt => opt.MapFrom(src => src.OTPCode))
        .ForMember(dest => dest.IsVerified, opt => opt.MapFrom(src => src.IsVerified ? 1L : 0L))
        .ForMember(dest => dest.ExpiresAt, opt => opt.MapFrom(src => src.ExpiresAt.ToString("yyyy-MM-dd HH:mm:ss")))
        .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));

      // Map from Data Models to ViewModels
      CreateMap<Candidate, CandidateViewModel>()
          .ForMember(dest => dest.JobTitle, opt => opt.MapFrom(src => src.Job != null ? src.Job.Title : null));

      CreateMap<QuestionSet, QuestionSetDetailsViewModel>()
          .ForMember(dest => dest.StatusDescription, opt => opt.MapFrom(src => GetStatusDescription(src.Status)))
          .ForMember(dest => dest.ExamId, opt => opt.MapFrom(src => src.ExamQuestionSetMappings.FirstOrDefault().ExamId));
      CreateMap<QuestionSet, QuestionSetStatusViewModel>()
          .ForMember(dest => dest.StatusDescription, opt => opt.MapFrom(src => GetStatusDescription(src.Status)))
          .ForMember(dest => dest.QuestionsGenerated, opt => opt.MapFrom(src => src.Questions.Count));

      // Map from DTOs to ViewModels
      CreateMap<ExamDTO, ExamViewModel>();
      CreateMap<JobDTO, JobViewModel>();
      CreateMap<CandidateDTO, CandidateViewModel>();
      CreateMap<ExamQuestionDTO, QuestionViewModel>();
      CreateMap<QuestionOptionDTO, OptionViewModel>();
      CreateMap<CandidateAnswerDTO, CandidateAnswerViewModel>()
          .ForMember(dest => dest.QuestionText, opt => opt.Ignore())
          .ForMember(dest => dest.QuestionType, opt => opt.Ignore())
          .ForMember(dest => dest.CorrectAnswerText, opt => opt.Ignore())
          .ForMember(dest => dest.AnswerText, opt => opt.MapFrom(src => src.AnswerText));
      CreateMap<OTPVerificationDto, OTPVerificationViewModel>();

      // إضافة تخطيط JobDTO إلى EditJobViewModel
      CreateMap<JobDTO, EditJobViewModel>();
      // إضافة تخطيط EditJobViewModel إلى JobDTO
      CreateMap<EditJobViewModel, JobDTO>();

      CreateMap<QuestionSetDto, QuestionSetDetailsViewModel>();
      CreateMap<QuestionSetDto, QuestionSetStatusViewModel>();

      // تعيين QuestionDto إلى QuestionViewModel
      CreateMap<QuestionDto, QuestionViewModel>();

      // Map from ViewModels to DTOs
      CreateMap<ExamViewModel, ExamDTO>();
      CreateMap<JobViewModel, JobDTO>();
      CreateMap<CandidateViewModel, CandidateDTO>();
      CreateMap<QuestionViewModel, ExamQuestionDTO>();
      CreateMap<OptionViewModel, QuestionOptionDTO>();
      CreateMap<CandidateAnswerViewModel, CandidateAnswerDTO>()
          .ForMember(dest => dest.AnswerText, opt => opt.MapFrom(src => src.AnswerText));
      CreateMap<CreateExamViewModel, CreateExamDTO>();
      CreateMap<OTPVerificationViewModel, OTPVerificationDto>();
      CreateMap<OTPRequestViewModel, OTPRequestDto>();

      CreateMap<CreateQuestionSetViewModel, CreateQuestionSetDto>();
      CreateMap<QuestionSetCreateViewModel, CreateQuestionSetDto>()
        .ForMember(dest => dest.ContentSourceType, opt => opt.MapFrom(src => src.ContentSourceType));

      // Mapping لصفحة تعليمات الامتحان
      CreateMap<ExamInstructionsDto, ExamInstructionsViewModel>();
      CreateMap<ExamInstructionsViewModel, ExamInstructionsDto>();
    }

    private string GetStatusDescription(string statuss)
    {
      QuestionSetStatus status = Enum.Parse<QuestionSetStatus>(statuss, true);
      return status switch
      {
        Enums.QuestionSetStatus.Pending => "في الانتظار",
        Enums.QuestionSetStatus.Processing => "قيد المعالجة",
        Enums.QuestionSetStatus.Completed => "مكتمل",
        Enums.QuestionSetStatus.Failed => "فشل",
        _ => "غير معروف"
      };
    }

    private string GetTimeSince(DateTime dateTime)
    {
      var span = DateTime.Now - dateTime;

      if (span.TotalDays > 7)
      {
        return $"{dateTime.ToString("yyyy-MM-dd")}";
      }
      else if (span.TotalDays > 1)
      {
        return $"منذ {(int)span.TotalDays} يوم";
      }
      else if (span.TotalHours > 1)
      {
        return $"منذ {(int)span.TotalHours} ساعة";
      }
      else if (span.TotalMinutes > 1)
      {
        return $"منذ {(int)span.TotalMinutes} دقيقة";
      }
      else
      {
        return "منذ لحظات";
      }
    }
  }
}
