using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TawtheefTest.Data.Structure;
using TawtheefTest.DTOs;

public interface ICandidateQuestionService
{
  Task<List<QuestionDto>> GetCandidateQuestion(int candidateExamId, int? questionIndex);
}

public class CandidateQuestionService : ICandidateQuestionService
{
  private readonly ApplicationDbContext _context;
  private readonly IMapper _mapper;

  public CandidateQuestionService(ApplicationDbContext context, IMapper mapper)
  {
    _context = context;
    _mapper = mapper;
  }

  public async Task<List<QuestionDto>> GetCandidateQuestion(int candidateExamId, int? questionIndex)
  {
    var candidateExam = await _context.CandidateExams
    .Include(ce => ce.Exam)
    .ThenInclude(e => e.ExamQuestionSets)
    .ThenInclude(eqs => eqs.QuestionSet)
    .ThenInclude(qs => qs.Questions)
    .FirstOrDefaultAsync(ce => ce.Id == candidateExamId);

    if (candidateExam == null)
    {
      return Enumerable.Empty<QuestionDto>().ToList();
    }

    var questions = candidateExam.Exam.ExamQuestionSets.SelectMany(eqs => eqs.QuestionSet.Questions).ToList();
    if (questionIndex == null)
    {
      var mappedQuestions = _mapper.Map<List<QuestionDto>>(questions);
      return mappedQuestions;
    }
    else
    {
      var question = questions[questionIndex.Value];
      var mappedQuestion = _mapper.Map<QuestionDto>(question);
      return new List<QuestionDto> { mappedQuestion };
    }

  }
}
