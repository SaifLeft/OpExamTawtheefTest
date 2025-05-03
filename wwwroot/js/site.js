// Add your custom script in this file

// Save answer using AJAX
function saveAnswer(candidateExamId, questionId, answer) {
    $.ajax({
        url: '/CandidateExams/SaveAnswer',
        type: 'POST',
        data: {
            candidateExamId: candidateExamId,
            questionId: questionId,
            answer: answer
        },
        success: function (result) {
            // Show saved indicator
            $('#savedIndicator-' + questionId).show().fadeOut(2000);
        },
        error: function (error) {
            console.log(error);
        }
    });
}

// Initialize on document ready
$(document).ready(function () {
    // Add event listeners for exam choices
    $('.exam-choice').change(function () {
        var questionId = $(this).data('question-id');
        var candidateExamId = $(this).data('candidate-exam-id');
        var answer = $(this).val();
        
        saveAnswer(candidateExamId, questionId, answer);
    });

    // Add event listeners for text answers
    $('.exam-text-answer').blur(function () {
        var questionId = $(this).data('question-id');
        var candidateExamId = $(this).data('candidate-exam-id');
        var answer = $(this).val();
        
        if (answer.trim() !== '') {
            saveAnswer(candidateExamId, questionId, answer);
        }
    });

    // Auto-dismiss alerts after 5 seconds
    setTimeout(function () {
        $('.alert').alert('close');
    }, 5000);
});
