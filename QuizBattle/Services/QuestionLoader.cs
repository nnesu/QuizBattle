using QuizBattle.Models;

namespace QuizBattle.Services
{
    public class QuestionLoader
    {
        public async Task<List<Question>> LoadQuestionsAsync(string fileName)
        {
            List<Question> questions = new List<Question>();

            string filePath = Path.Combine(FileSystem.Current.AppDataDirectory, fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    $"'{fileName}' was not found in AppDataDirectory.");
            }

            using Stream stream = File.OpenRead(filePath);
            using StreamReader reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                string? line = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] parts = line.Split('|');

                Question question = new Question();

                switch (parts[0])
                {
                    case "Identification":
                        {
                            question.Type = QuestionType.Identification;
                            question.Text = parts[1];
                            question.CorrectAnswers.Add(parts[2]);
                            break;
                        }

                    case "MultipleChoice":
                        {
                            question.Type = QuestionType.MultipleChoice;
                            question.Text = parts[1];

                            int answerIndex = Array.IndexOf(parts, "ANS");

                            if (answerIndex == -1)
                            {
                                continue;
                            }

                            for (int index = 2; index < answerIndex; index++)
                            {
                                question.Options.Add(parts[index]);
                            }

                            for (int index = answerIndex + 1; index < parts.Length; index++)
                            {
                                question.CorrectAnswers.Add(parts[index]);
                            }

                            break;
                        }

                    default:
                        continue;
                }

                questions.Add(question);
            }

            return questions;
        }
    }
}