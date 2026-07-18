using SQLite;
using QuizBattle.Models;

namespace QuizBattle.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection? _db;

        public async Task InitAsync()
        {
            if (_db != null) return;

            // initialize sqlite
            SQLitePCL.Batteries_V2.Init();
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "QuizBattle.db3");
            _db = new SQLiteAsyncConnection(dbPath);

            await _db.CreateTableAsync<DeckEntity>();
            await _db.CreateTableAsync<QuestionEntity>();
            await _db.CreateTableAsync<DeckMasteryEntity>();

            await MigrateLegacyTextFilesAsync();
        }

        // deck operations
        public async Task<List<DeckEntity>> GetDecksAsync()
        {
            await InitAsync();
            var decks = await _db!.Table<DeckEntity>().ToListAsync();
            return decks.OrderBy(d => d.Name).ToList();
        }

        public async Task<DeckEntity?> GetDeckByNameAsync(string name)
        {
            await InitAsync();
            if (string.IsNullOrWhiteSpace(name)) return null;
            string cleanName = name.Trim();
            var allDecks = await _db!.Table<DeckEntity>().ToListAsync();
            return allDecks.FirstOrDefault(d => d.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<DeckEntity> CreateDeckAsync(string name)
        {
            await InitAsync();
            string cleanName = name.Trim();
            var existing = await GetDeckByNameAsync(cleanName);
            if (existing != null) return existing;

            var newDeck = new DeckEntity { Name = cleanName };
            await _db!.InsertAsync(newDeck);
            return newDeck;
        }

        public async Task RenameDeckAsync(int deckId, string newName)
        {
            await InitAsync();
            var deck = await _db!.Table<DeckEntity>().FirstOrDefaultAsync(d => d.Id == deckId);
            if (deck != null)
            {
                deck.Name = newName.Trim();
                await _db.UpdateAsync(deck);
            }
        }

        public async Task DeleteDeckAsync(int deckId)
        {
            await InitAsync();
            await _db!.ExecuteAsync("DELETE FROM Questions WHERE DeckId = ?", deckId);
            await _db!.DeleteAsync<DeckEntity>(deckId);
        }

        // question operations
        public async Task<List<QuestionEntity>> GetQuestionsForDeckAsync(int deckId)
        {
            await InitAsync();
            return await _db!.Table<QuestionEntity>().Where(q => q.DeckId == deckId).ToListAsync();
        }

        public async Task SaveQuestionAsync(QuestionEntity question)
        {
            await InitAsync();
            if (question.Id != 0)
                await _db!.UpdateAsync(question);
            else
                await _db!.InsertAsync(question);
        }

        public async Task DeleteQuestionAsync(int questionId)
        {
            await InitAsync();
            await _db!.DeleteAsync<QuestionEntity>(questionId);
        }

        // import and export text formats
        public async Task ImportDeckFromTextAsync(string deckName, string rawText, bool clearExisting = false)
        {
            await InitAsync();
            var deck = await CreateDeckAsync(deckName);

            if (clearExisting)
            {
                await _db!.ExecuteAsync("DELETE FROM Questions WHERE DeckId = ?", deck.Id);
            }

            string[] lines = rawText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] parts = line.Split('|');
                if (parts.Length < 3) continue;

                string type = parts[0].Trim();

                if (type.Equals("Identification", StringComparison.OrdinalIgnoreCase))
                {
                    var q = new QuestionEntity
                    {
                        DeckId = deck.Id,
                        Type = "Identification",
                        Text = parts[1].Trim(),
                        AnswersRaw = parts[2].Trim()
                    };
                    await SaveQuestionAsync(q);
                }
                else if (type.Equals("MultipleChoice", StringComparison.OrdinalIgnoreCase) && parts.Length >= 8)
                {
                    var q = new QuestionEntity
                    {
                        DeckId = deck.Id,
                        Type = "MultipleChoice",
                        Text = parts[1].Trim(),
                        OptionsRaw = $"{parts[2].Trim()}|{parts[3].Trim()}|{parts[4].Trim()}|{parts[5].Trim()}",
                        AnswersRaw = parts[7].Trim()
                    };
                    await SaveQuestionAsync(q);
                }
            }
        }

        public async Task<string> ExportDeckToTextAsync(int deckId)
        {
            await InitAsync();
            var questions = await GetQuestionsForDeckAsync(deckId);

            List<string> lines = new List<string>();

            foreach (var q in questions)
            {
                if (q.Type.Equals("Identification", StringComparison.OrdinalIgnoreCase))
                {
                    lines.Add($"Identification|{q.Text}|{q.AnswersRaw}");
                }
                else if (q.Type.Equals("MultipleChoice", StringComparison.OrdinalIgnoreCase))
                {
                    string[] opts = q.OptionsRaw.Split('|');
                    string opt1 = opts.Length > 0 ? opts[0] : "";
                    string opt2 = opts.Length > 1 ? opts[1] : "";
                    string opt3 = opts.Length > 2 ? opts[2] : "";
                    string opt4 = opts.Length > 3 ? opts[3] : "";

                    lines.Add($"MultipleChoice|{q.Text}|{opt1}|{opt2}|{opt3}|{opt4}|ANS|{q.AnswersRaw}");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        // legacy text file migration
        private async Task MigrateLegacyTextFilesAsync()
        {
            try
            {
                string decksFolder = Path.Combine(FileSystem.AppDataDirectory, "Decks");
                if (!Directory.Exists(decksFolder)) return;

                var txtFiles = Directory.GetFiles(decksFolder, "*.txt");
                foreach (var file in txtFiles)
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    var existingDeck = await GetDeckByNameAsync(name);

                    if (existingDeck == null)
                    {
                        string text = await File.ReadAllTextAsync(file);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            await ImportDeckFromTextAsync(name, text);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Migration error: {ex.Message}");
            }
        }

        // Deck Mastery Operations
        public async Task<DeckMasteryEntity> GetDeckMasteryAsync(int deckId)
        {
            await InitAsync();
            return await _db!.Table<DeckMasteryEntity>().FirstOrDefaultAsync(m => m.DeckId == deckId);
        }

        public async Task SaveDeckMasteryAsync(int deckId, int newScore)
        {
            await InitAsync();
            var mastery = await GetDeckMasteryAsync(deckId);

            if (mastery == null)
            {
                mastery = new DeckMasteryEntity
                {
                    DeckId = deckId,
                    HighScore = newScore,
                    LastPlayed = DateTime.Now
                };
                await _db!.InsertAsync(mastery);
            }
            else if (newScore > mastery.HighScore)
            {
                mastery.HighScore = newScore;
                mastery.LastPlayed = DateTime.Now;
                await _db!.UpdateAsync(mastery);
            }
        }
    }
}