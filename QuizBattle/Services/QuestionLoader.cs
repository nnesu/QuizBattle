using System.Diagnostics;
using QuizBattle.Models;

namespace QuizBattle.Services
{
    public class QuestionLoader
    {
        public async Task<List<Question>> LoadQuestionsAsync(string fileName)
        {
            List<Question> questions = new List<Question>();

            // Clean filename extension
            string cleanName = fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
                ? fileName
                : $"{fileName}.txt";

            // Check Decks directory first
            string decksDir = Path.Combine(FileSystem.Current.AppDataDirectory, "Decks");
            string filePath = Path.Combine(decksDir, cleanName);

            // Fallback to AppDataDirectory root
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(FileSystem.Current.AppDataDirectory, cleanName);
            }

            if (!File.Exists(filePath))
            {
                Debug.WriteLine($"[QuestionLoader] File not found: {filePath}");
                return questions;
            }

            try
            {
                using Stream stream = File.OpenRead(filePath);
                using StreamReader reader = new StreamReader(stream);

                while (!reader.EndOfStream)
                {
                    string? line = await reader.ReadLineAsync();

                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] parts = line.Split('|');
                    Question question = new Question();

                    switch (parts[0].Trim())
                    {
                        case "Identification":
                            {
                                if (parts.Length < 3) continue;
                                question.Type = QuestionType.Identification;
                                question.Text = parts[1].Trim();
                                question.CorrectAnswers.Add(parts[2].Trim());
                                break;
                            }

                        case "MultipleChoice":
                            {
                                question.Type = QuestionType.MultipleChoice;
                                question.Text = parts[1].Trim();

                                int answerIndex = Array.IndexOf(parts, "ANS");

                                if (answerIndex == -1) continue;

                                for (int index = 2; index < answerIndex; index++)
                                {
                                    question.Options.Add(parts[index].Trim());
                                }

                                for (int index = answerIndex + 1; index < parts.Length; index++)
                                {
                                    question.CorrectAnswers.Add(parts[index].Trim());
                                }

                                break;
                            }

                        default:
                            continue;
                    }

                    questions.Add(question);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error parsing question file: {ex.Message}");
            }

            return questions;
        }
    }
}