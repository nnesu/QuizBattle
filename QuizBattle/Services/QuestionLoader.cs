using System.Diagnostics;
using QuizBattle.Models;

namespace QuizBattle.Services
{
    public class QuestionLoader
    {
        private readonly DatabaseService _dbService = new DatabaseService();

        public async Task<List<Question>> LoadQuestionsAsync(string deckName)
        {
            List<Question> questions = new List<Question>();

            try
            {
                await _dbService.InitAsync();
                string cleanDeckName = Path.GetFileNameWithoutExtension(deckName);

                var deck = await _dbService.GetDeckByNameAsync(cleanDeckName);
                if (deck == null)
                {
                    Debug.WriteLine($"[QuestionLoader] Deck '{cleanDeckName}' not found in database.");
                    return questions;
                }

                var dbQuestions = await _dbService.GetQuestionsForDeckAsync(deck.Id);

                foreach (var entity in dbQuestions)
                {
                    var q = new Question
                    {
                        Text = entity.Text,
                        TimesAsked = entity.TimesAsked,
                        TimesCorrect = entity.TimesCorrect,
                        TimesIncorrect = entity.TimesIncorrect,
                        CorrectProgress = entity.CorrectProgress
                    };

                    if (entity.Type.Equals("Identification", StringComparison.OrdinalIgnoreCase))
                    {
                        q.Type = QuestionType.Identification;
                        q.CorrectAnswers.Add(entity.AnswersRaw);
                    }
                    else if (entity.Type.Equals("MultipleChoice", StringComparison.OrdinalIgnoreCase))
                    {
                        q.Type = QuestionType.MultipleChoice;
                        q.CorrectAnswers.Add(entity.AnswersRaw);

                        string[] options = entity.OptionsRaw.Split('|');
                        foreach (string opt in options)
                        {
                            q.Options.Add(opt);
                        }
                    }

                    questions.Add(q);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading questions from database: {ex.Message}");
            }

            return questions;
        }
    }
}