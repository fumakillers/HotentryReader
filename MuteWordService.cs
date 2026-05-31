using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HotentryReader
{
    public sealed class MuteWordService
    {
        private const string FileName = "MuteWords.json";
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
        };

        private readonly string filePath = Path.Combine(FileSystem.AppDataDirectory, FileName);
        private readonly ObservableCollection<MuteWord> muteWords = [];
        private readonly ReadOnlyObservableCollection<MuteWord> readonlyMuteWords;

        private MuteWordService()
        {
            readonlyMuteWords = new ReadOnlyObservableCollection<MuteWord>(muteWords);
        }

        public static MuteWordService Instance { get; } = new();

        public event EventHandler? MuteWordsChanged;

        public ReadOnlyObservableCollection<MuteWord> MuteWords => readonlyMuteWords;

        public async Task LoadAsync()
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return;
                }

                string json = await File.ReadAllTextAsync(filePath);
                List<MuteWord>? loadedMuteWords = JsonSerializer.Deserialize<List<MuteWord>>(json, JsonOptions);

                muteWords.Clear();
                foreach (MuteWord muteWord in loadedMuteWords ?? [])
                {
                    if (!string.IsNullOrWhiteSpace(muteWord.Word))
                    {
                        muteWords.Add(muteWord);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load mute words: {filePath}");
                Debug.WriteLine(ex);
            }
        }

        public async Task AddAsync(string word, bool isRegex)
        {
            string normalizedWord = word.Trim();
            if (string.IsNullOrWhiteSpace(normalizedWord))
            {
                return;
            }

            muteWords.Add(new MuteWord
            {
                Word = normalizedWord,
                IsRegex = isRegex,
            });

            await SaveAndNotifyAsync();
        }

        public async Task RemoveAsync(MuteWord muteWord)
        {
            muteWords.Remove(muteWord);
            await SaveAndNotifyAsync();
        }

        public bool ShouldMute(Entry entry)
        {
            string target = $"{entry.Title}\n{entry.Summary}";

            foreach (MuteWord muteWord in muteWords)
            {
                if (IsMatch(target, muteWord))
                {
                    return true;
                }
            }

            return false;
        }

        private async Task SaveAndNotifyAsync()
        {
            try
            {
                Directory.CreateDirectory(FileSystem.AppDataDirectory);
                string json = JsonSerializer.Serialize(muteWords.ToList(), JsonOptions);
                await File.WriteAllTextAsync(filePath, json);
                MuteWordsChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save mute words: {filePath}");
                Debug.WriteLine(ex);
            }
        }

        private static bool IsMatch(string target, MuteWord muteWord)
        {
            try
            {
                if (muteWord.IsRegex)
                {
                    return Regex.IsMatch(target, muteWord.Word, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(300));
                }

                return target.Contains(muteWord.Word, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is ArgumentException or RegexMatchTimeoutException)
            {
                Debug.WriteLine($"Invalid mute word regex: {muteWord.Word}");
                Debug.WriteLine(ex);
                return false;
            }
        }
    }
}
