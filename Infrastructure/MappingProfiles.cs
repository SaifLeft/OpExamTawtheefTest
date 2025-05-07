using AutoMapper;
using TawtheefTest.Data.Structure;
using TawtheefTest.DTOs;
using TawtheefTest.DTOs.ExamModels;
using TawtheefTest.ViewModels;
using System.Linq;
using System;

namespace TawtheefTest.Infrastructure
{
  public class MappingProfiles : Profile
  {
    public MappingProfiles()
    {
      // Map from Data Models to DTOs
      CreateMap<Job, JobDTO>();
      CreateMap<Exam, ExamDTO>();
      CreateMap<Candidate, CandidateDTO>();
      CreateMap<Question, QuestionDTO>();
      CreateMap<QuestionOption, QuestionOptionDTO>();
      CreateMap<OTPVerification, OTPVerificationDto>();
      CreateMap<CandidateAnswer, CandidateAnswerDTO>()
          .ForMember(dest => dest.AnswerText, opt => opt.MapFrom(src => src.AnswerText ?? string.Empty));
      CreateMap<CandidateExam, CandidateExamViewModel>()
          .ForMember(dest => dest.CandidateName, opt => opt.MapFrom(src => src.Candidate.Name))
          .ForMember(dest => dest.ExamName, opt => opt.MapFrom(src => src.Exam.Name))
          .ForMember(dest => dest.JobName, opt => opt.MapFrom(src => src.Exam.Job.Title))
          .ForMember(dest => dest.QuestionType, opt => opt.MapFrom(src => src.Exam.Questions.FirstOrDefault().QuestionType))
          .ForMember(dest => dest.IsCompleted, opt => opt.MapFrom(src => src.Status == "Completed"));
      CreateMap<Exam, ExamForCandidateViewModel>()
          .ForMember(dest => dest.JobName, opt => opt.MapFrom(src => src.Job.Title))
          .ForMember(dest => dest.QuestionType, opt => opt.MapFrom(src => src.Questions.FirstOrDefault().QuestionType));
      CreateMap<CandidateExam, CandidateExamResultViewModel>();
      CreateMap<CandidateAnswer, CandidateAnswerViewModel>()
          .ForMember(dest => dest.QuestionText, opt => opt.MapFrom(src => src.Question.QuestionText))
          .ForMember(dest => dest.QuestionType, opt => opt.MapFrom(src => src.Question.QuestionType))
          .ForMember(dest => dest.CorrectAnswer, opt => opt.MapFrom(src => src.Question.Answer))
          .ForMember(dest => dest.Answer, opt => opt.MapFrom(src => src.AnswerText ?? string.Empty));
      CreateMap<QuestionOption, CandidateQuestionOptionViewModel>();

      // للتعامل مع نماذج توليد الأسئلة
      CreateMap<QuestionSet, QuestionSetDto>()
          .ForMember(dest => dest.StatusDescription, opt => opt.MapFrom(src => GetQuestionSetStatusDescription(src.Status)))
          .ForMember(dest => dest.Questions, opt => opt.Ignore())
          .ForMember(dest => dest.UsageCount, opt => opt.Ignore())
          .ForMember(dest => dest.UsedInExams, opt => opt.Ignore());

      CreateMap<ContentSource, ContentSourceViewModel>();
      CreateMap<UploadedFile, UploadedFileViewModel>();

      // تعيين Question إلى QuestionDto بدلاً من QuestionViewModel
      CreateMap<Question, QuestionDto>();
      CreateMap<QuestionViewModel, QuestionDto>();

      CreateMap<Question, QuestionViewModel>();
      CreateMap<QuestionOption, QuestionOptionViewModel>();
      CreateMap<MatchingPair, MatchingPairViewModel>();
      CreateMap<OrderingItem, OrderingItemViewModel>();

      // Map from DTOs to Data Models
      CreateMap<JobDTO, Job>();
      CreateMap<ExamDTO, Exam>();
      CreateMap<CandidateDTO, Candidate>();
      CreateMap<QuestionDTO, Question>();
      CreateMap<QuestionOptionDTO, QuestionOption>();
      CreateMap<CandidateAnswerDTO, CandidateAnswer>()
          .ForMember(dest => dest.AnswerText, opt => opt.MapFrom(src => src.AnswerText));
      CreateMap<CreateExamDTO, Exam>();
      CreateMap<EditExamDTO, Exam>();
      CreateMap<CreateJobDTO, Job>();
      CreateMap<EditJobDTO, Job>();
      CreateMap<OTPVerificationDto, OTPVerification>();

      // Map from Data Models to ViewModels
      CreateMap<Candidate, CandidateViewModel>();
      CreateMap<QuestionSet, QuestionSetDetailsViewModel>()
          .ForMember(dest => dest.StatusDescription, opt => opt.MapFrom(src => GetStatusDescription(src.Status)))
          .ForMember(dest => dest.ExamId, opt => opt.MapFrom(src => src.ExamQuestionSets.FirstOrDefault().ExamId));
      CreateMap<QuestionSet, QuestionSetStatusViewModel>()
          .ForMember(dest => dest.StatusDescription, opt => opt.MapFrom(src => GetStatusDescription(src.Status)))
          .ForMember(dest => dest.QuestionsGenerated, opt => opt.MapFrom(src => src.Questions.Count));

      // Map from DTOs to ViewModels
      CreateMap<ExamDTO, ExamViewModel>();
      CreateMap<JobDTO, JobViewModel>();
      CreateMap<CandidateDTO, CandidateViewModel>();
      CreateMap<QuestionDTO, QuestionViewModel>();
      CreateMap<QuestionOptionDTO, OptionViewModel>();
      CreateMap<CandidateAnswerDTO, CandidateAnswerViewModel>()
          .ForMember(dest => dest.QuestionText, opt => opt.Ignore())
          .ForMember(dest => dest.QuestionType, opt => opt.Ignore())
          .ForMember(dest => dest.CorrectAnswer, opt => opt.Ignore())
          .ForMember(dest => dest.Options, opt => opt.Ignore())
          .ForMember(dest => dest.Answer, opt => opt.MapFrom(src => src.AnswerText));
      CreateMap<OTPVerificationDto, OTPVerificationViewModel>();

      CreateMap<QuestionSetDto, QuestionSetDetailsViewModel>();
      CreateMap<QuestionSetDto, QuestionSetStatusViewModel>();

      // تعيين QuestionDto إلى QuestionViewModel
      CreateMap<QuestionDto, QuestionViewModel>();

      // Map from ViewModels to DTOs
      CreateMap<ExamViewModel, ExamDTO>();
      CreateMap<JobViewModel, JobDTO>();
      CreateMap<CandidateViewModel, CandidateDTO>();
      CreateMap<QuestionViewModel, QuestionDTO>();
      CreateMap<OptionViewModel, QuestionOptionDTO>();
      CreateMap<CandidateAnswerViewModel, CandidateAnswerDTO>()
          .ForMember(dest => dest.AnswerText, opt => opt.MapFrom(src => src.Answer));
      CreateMap<CreateExamViewModel, CreateExamDTO>();
      CreateMap<OTPVerificationViewModel, OTPVerificationDto>();
      CreateMap<OTPRequestViewModel, OTPRequestDto>();

      CreateMap<CreateQuestionSetViewModel, CreateQuestionSetDto>();
    }

    private string GetStatusDescription(Enum.QuestionSetStatus status)
    {
      return status switch
      {
        Enum.QuestionSetStatus.Pending => "في الانتظار",
        Enum.QuestionSetStatus.Processing => "قيد المعالجة",
        Enum.QuestionSetStatus.Completed => "مكتمل",
        Enum.QuestionSetStatus.Failed => "فشل",
        _ => "غير معروف"
      };
    }

    private string GetQuestionSetStatusDescription(Enum.QuestionSetStatus status)
    {
      return status switch
      {
        Enum.QuestionSetStatus.Pending => "في الانتظار",
        Enum.QuestionSetStatus.Processing => "قيد المعالجة",
        Enum.QuestionSetStatus.Completed => "مكتمل",
        Enum.QuestionSetStatus.Failed => "فشل",
        _ => "غير معروف"
      };
    }
  }
}
