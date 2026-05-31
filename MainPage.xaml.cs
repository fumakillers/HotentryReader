using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace HotentryReader
{
    public partial class MainPage : ContentPage
    {
        private const string RecentRssUrl = "https://b.hatena.ne.jp/entrylist.rss";
        private const string GeneralRssUrl = "https://b.hatena.ne.jp/hotentry.rss";
        private const string TechnologyRssUrl = "https://b.hatena.ne.jp/hotentry/it.rss";
        private const string SocialRssUrl = "https://b.hatena.ne.jp/hotentry/social.rss";
        private const string EconomicsRssUrl = "https://b.hatena.ne.jp/hotentry/economics.rss";
        private const string EntertainmentRssUrl = "https://b.hatena.ne.jp/hotentry/entertainment.rss";
        private const string GameRssUrl = "https://b.hatena.ne.jp/hotentry/game.rss";
        private const string LifeRssUrl = "https://b.hatena.ne.jp/hotentry/life.rss";
        private const string KnowledgeRssUrl = "https://b.hatena.ne.jp/hotentry/knowledge.rss";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(60);

        private static readonly HttpClient HttpClient = CreateHttpClient();
        private static readonly MemoryCache EntryCache = new(new MemoryCacheOptions());

        private readonly IReadOnlyList<HatenaCategory> availableCategories =
        [
            new("\u65b0\u7740", RecentRssUrl),
            new("\u7dcf\u5408", GeneralRssUrl),
            new("\u30c6\u30af\u30ce\u30ed\u30b8\u30fc", TechnologyRssUrl),
            new("\u4e16\u306e\u4e2d", SocialRssUrl),
            new("\u653f\u6cbb\u3068\u7d4c\u6e08", EconomicsRssUrl),
            new("\u30a8\u30f3\u30bf\u30e1", EntertainmentRssUrl),
            new("\u30a2\u30cb\u30e1\u3068\u30b2\u30fc\u30e0", GameRssUrl),
            new("\u66ae\u3089\u3057", LifeRssUrl),
            new("\u5b66\u3073", KnowledgeRssUrl),
        ];

#if ANDROID
        private readonly Dictionary<Android.Views.View, EventHandler> clickHandlers = [];
        private readonly Dictionary<Android.Views.View, EventHandler<Android.Views.View.LongClickEventArgs>> longPressHandlers = [];
        private readonly List<WeakReference<VisualElement>> entryItemViews = [];

        public static WeakReference<MainPage>? CurrentPageReference { get; private set; }
#endif

        private bool hasLoaded;
        private DateTime ignoreEntryTapUntil = DateTime.MinValue;
        private int selectedCategoryIndex = 2;
        private int loadRequestNumber;

        public ObservableCollection<CategoryTab> Categories { get; } = [];
        public ObservableCollection<CategoryPageState> CategoryPages { get; } = [];

        public int SelectedCategoryIndex
        {
            get => selectedCategoryIndex;
            set
            {
                if (selectedCategoryIndex == value)
                {
                    return;
                }

                selectedCategoryIndex = value;
                OnPropertyChanged();
            }
        }

        public MainPage()
        {
            InitializeComponent();

#if ANDROID
            CurrentPageReference = new WeakReference<MainPage>(this);
#endif

            for (int index = 0; index < availableCategories.Count; index++)
            {
                Categories.Add(new CategoryTab(availableCategories[index], index == selectedCategoryIndex));
                CategoryPages.Add(new CategoryPageState(availableCategories[index]));
            }

            BindingContext = this;
            MuteWordService.Instance.MuteWordsChanged += OnMuteWordsChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await MuteWordService.Instance.LoadAsync();

            if (hasLoaded)
            {
#if ANDROID
                ReattachVisibleEntryTouchHandlers();
#endif
                return;
            }

            hasLoaded = true;
            await LoadSelectedCategoryAsync();
        }

        private async void OnRefreshRequested(object? sender, EventArgs e)
        {
            if (sender is RefreshView refreshView &&
                refreshView.BindingContext is CategoryPageState categoryPage)
            {
                await LoadEntriesAsync(categoryPage, forceRefresh: true);
                categoryPage.IsRefreshing = false;
            }
        }

        private async void OnOptionsClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(OptionsPage));
        }

        private async void OnCategoryTapped(object? sender, TappedEventArgs e)
        {
            if (e.Parameter is not CategoryTab categoryTab)
            {
                return;
            }

            int categoryIndex = Categories.IndexOf(categoryTab);
            if (categoryIndex < 0)
            {
                return;
            }

            await SelectCategoryAsync(categoryIndex);
        }

        private async void OnCategoryCarouselPositionChanged(object? sender, PositionChangedEventArgs e)
        {
            if (e.CurrentPosition < 0 || e.CurrentPosition >= availableCategories.Count)
            {
                return;
            }

            await SelectCategoryAsync(e.CurrentPosition, updateCarouselPosition: false);
        }

        private void OnEntryItemLoaded(object? sender, EventArgs e)
        {
#if ANDROID
            if (sender is not VisualElement visualElement ||
                visualElement.BindingContext is not Entry entry ||
                visualElement.Handler?.PlatformView is not Android.Views.View platformView)
            {
                return;
            }

            RememberEntryItemView(visualElement);
            AttachEntryTouchHandlers(platformView, entry);
#endif
        }

#if ANDROID
        public void RestoreEntryLongPressHandlers()
        {
            Dispatcher.Dispatch(ReattachVisibleEntryTouchHandlers);
        }

        private void AttachEntryTouchHandlers(Android.Views.View platformView, Entry entry)
        {
            AttachEntryTouchHandlersToView(platformView, entry);

            if (platformView is not Android.Views.ViewGroup viewGroup)
            {
                return;
            }

            for (int index = 0; index < viewGroup.ChildCount; index++)
            {
                Android.Views.View? child = viewGroup.GetChildAt(index);
                if (child is not null)
                {
                    AttachEntryTouchHandlers(child, entry);
                }
            }
        }

        private void AttachEntryTouchHandlersToView(Android.Views.View platformView, Entry entry)
        {
            if (clickHandlers.TryGetValue(platformView, out EventHandler? existingClickHandler))
            {
                platformView.Click -= existingClickHandler;
            }

            if (longPressHandlers.TryGetValue(platformView, out EventHandler<Android.Views.View.LongClickEventArgs>? existingLongPressHandler))
            {
                platformView.LongClick -= existingLongPressHandler;
            }

            EventHandler clickHandler = async (_, _) => await OpenEntryFromTouchAsync(entry);
            EventHandler<Android.Views.View.LongClickEventArgs> longPressHandler = async (_, args) =>
            {
                args.Handled = true;
                await ShowEntryLongPressMenuAsync(entry);
            };

            clickHandlers[platformView] = clickHandler;
            longPressHandlers[platformView] = longPressHandler;
            platformView.Clickable = true;
            platformView.LongClickable = true;
            platformView.Click += clickHandler;
            platformView.LongClick += longPressHandler;
        }

        private void RememberEntryItemView(VisualElement visualElement)
        {
            entryItemViews.RemoveAll(reference => !reference.TryGetTarget(out _));

            if (entryItemViews.Any(reference =>
                reference.TryGetTarget(out VisualElement? target) &&
                ReferenceEquals(target, visualElement)))
            {
                return;
            }

            entryItemViews.Add(new WeakReference<VisualElement>(visualElement));
        }

        private void ReattachVisibleEntryTouchHandlers()
        {
            entryItemViews.RemoveAll(reference => !reference.TryGetTarget(out _));

            foreach (WeakReference<VisualElement> reference in entryItemViews)
            {
                if (!reference.TryGetTarget(out VisualElement? visualElement) ||
                    visualElement.BindingContext is not Entry entry ||
                    visualElement.Handler?.PlatformView is not Android.Views.View platformView)
                {
                    continue;
                }

                AttachEntryTouchHandlers(platformView, entry);
            }
        }

#endif

        private static HttpClient CreateHttpClient()
        {
            HttpClient httpClient = new()
            {
                Timeout = TimeSpan.FromSeconds(20),
            };

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("HotentryReader/1.0");
            return httpClient;
        }

        private async Task SelectCategoryAsync(int categoryIndex, bool updateCarouselPosition = true)
        {
            if (categoryIndex < 0 || categoryIndex >= availableCategories.Count)
            {
                return;
            }

            SelectedCategoryIndex = categoryIndex;
            if (updateCarouselPosition && CategoryCarousel.Position != categoryIndex)
            {
                CategoryCarousel.Position = categoryIndex;
            }

            UpdateCategorySelection();
            await ScrollSelectedCategoryIntoViewAsync();
            await LoadSelectedCategoryAsync();
        }

        private void UpdateCategorySelection()
        {
            for (int index = 0; index < Categories.Count; index++)
            {
                Categories[index].IsSelected = index == selectedCategoryIndex;
            }
        }

        private async Task ScrollSelectedCategoryIntoViewAsync()
        {
            await Task.Yield();

            if (selectedCategoryIndex >= CategoryTabs.Children.Count)
            {
                return;
            }

            if (CategoryTabs.Children[selectedCategoryIndex] is Element selectedTab)
            {
                await CategoryTabsScrollView.ScrollToAsync(selectedTab, ScrollToPosition.MakeVisible, true);
            }
        }

        private Task LoadSelectedCategoryAsync(bool forceRefresh = false)
        {
            CategoryPageState categoryPage = CategoryPages[selectedCategoryIndex];
            return LoadEntriesAsync(categoryPage, forceRefresh);
        }

        private async Task LoadEntriesAsync(CategoryPageState categoryPage, bool forceRefresh)
        {
            int currentRequestNumber = ++loadRequestNumber;
            HatenaCategory category = categoryPage.Category;

            try
            {
                categoryPage.IsLoading = true;
                categoryPage.Entries.Clear();

                if (!forceRefresh &&
                    EntryCache.TryGetValue(category.Name, out List<Entry>? cachedEntries) &&
                    cachedEntries is not null)
                {
                    ApplyEntries(categoryPage, cachedEntries);
                    categoryPage.StatusMessage = GetEmptyStatusMessage(cachedEntries.Count, categoryPage.Entries.Count);
                    Debug.WriteLine($"Loaded RSS cache: category={category.Name}, entries={cachedEntries.Count}");
                    return;
                }

                categoryPage.StatusMessage = $"{category.Name}\u3092\u8aad\u307f\u8fbc\u307f\u4e2d...";

                string rssXml = await HttpClient.GetStringAsync(category.RssUrl);
                XDocument rssDocument = XDocument.Parse(rssXml);
                List<Entry> loadedEntries = ParseEntries(rssDocument);

                if (currentRequestNumber != loadRequestNumber)
                {
                    return;
                }

                Debug.WriteLine($"Loaded RSS: category={category.Name}, url={category.RssUrl}, entries={loadedEntries.Count}");

                EntryCache.Set(category.Name, loadedEntries, CacheDuration);
                ApplyEntries(categoryPage, loadedEntries);

                categoryPage.StatusMessage = GetEmptyStatusMessage(loadedEntries.Count, categoryPage.Entries.Count);
            }
            catch (Exception ex)
            {
                if (currentRequestNumber != loadRequestNumber)
                {
                    return;
                }

                categoryPage.Entries.Clear();
                Debug.WriteLine($"Failed to load RSS: category={category.Name}, url={category.RssUrl}");
                Debug.WriteLine(ex);
                categoryPage.StatusMessage = $"RSS\u3092\u8aad\u307f\u8fbc\u3081\u307e\u305b\u3093\u3067\u3057\u305f: {ex.Message}";
            }
            finally
            {
                if (currentRequestNumber == loadRequestNumber)
                {
                    categoryPage.IsLoading = false;
                }
            }
        }

        private static void ApplyEntries(CategoryPageState categoryPage, IEnumerable<Entry> entries)
        {
            categoryPage.AllEntries = entries.ToList();
            categoryPage.Entries.Clear();

            foreach (Entry entry in categoryPage.AllEntries.Where(entry => !MuteWordService.Instance.ShouldMute(entry)))
            {
                categoryPage.Entries.Add(entry);
            }
        }

        private static string GetEmptyStatusMessage(int sourceCount, int visibleCount)
        {
            if (visibleCount > 0)
            {
                return string.Empty;
            }

            return sourceCount == 0
                ? "\u8a18\u4e8b\u304c\u3042\u308a\u307e\u305b\u3093: RSS\u306eitem\u3092\u89e3\u6790\u3067\u304d\u307e\u305b\u3093\u3067\u3057\u305f"
                : "\u30df\u30e5\u30fc\u30c8\u30ef\u30fc\u30c9\u306b\u3088\u308a\u8868\u793a\u3067\u304d\u308b\u8a18\u4e8b\u304c\u3042\u308a\u307e\u305b\u3093";
        }

        private void OnMuteWordsChanged(object? sender, EventArgs e)
        {
            foreach (CategoryPageState categoryPage in CategoryPages)
            {
                if (categoryPage.AllEntries.Count > 0)
                {
                    ApplyEntries(categoryPage, categoryPage.AllEntries);
                    categoryPage.StatusMessage = GetEmptyStatusMessage(categoryPage.AllEntries.Count, categoryPage.Entries.Count);
                }
            }
        }

        private async Task ShowEntryMenuAsync(Entry entry)
        {
            string? action = await DisplayActionSheetAsync(entry.Title, "\u30ad\u30e3\u30f3\u30bb\u30eb", null, "\u53cd\u5fdc\u3092\u898b\u308b");

            if (action == "\u53cd\u5fdc\u3092\u898b\u308b")
            {
                await OpenUrlAsync(BuildBookmarkCommentUrl(entry.Url));
            }
        }

        private async Task ShowEntryLongPressMenuAsync(Entry entry)
        {
            ignoreEntryTapUntil = DateTime.UtcNow.AddSeconds(2);
            await ShowEntryMenuAsync(entry);
        }

        private async Task OpenEntryFromTouchAsync(Entry entry)
        {
            if (DateTime.UtcNow < ignoreEntryTapUntil)
            {
                return;
            }

            await OpenUrlAsync(entry.Url);
        }

        private static async Task OpenUrlAsync(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                Debug.WriteLine($"Invalid URL: {url}");
                return;
            }

            await Launcher.Default.OpenAsync(uri);
        }

        private static string BuildBookmarkCommentUrl(string entryUrl)
        {
            if (!Uri.TryCreate(entryUrl, UriKind.Absolute, out Uri? uri))
            {
                return entryUrl;
            }

            string hostAndPath = uri.Authority + uri.PathAndQuery;

            if (uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return $"https://b.hatena.ne.jp/entry/s/{hostAndPath}";
            }

            return $"https://b.hatena.ne.jp/entry/{hostAndPath}";
        }

        private static List<Entry> ParseEntries(XDocument rssDocument)
        {
            XNamespace hatenaNamespace = "http://www.hatena.ne.jp/info/xmlns#";
            XNamespace mediaNamespace = "http://search.yahoo.com/mrss/";

            return rssDocument
                .Descendants()
                .Where(element => element.Name.LocalName == "item")
                .Select(item =>
                {
                    string title = GetElementValue(item, "title");
                    string url = GetElementValue(item, "link");
                    string summary = CleanSummary(GetElementValue(item, "description"));
                    string thumbnailUrl = GetElementValue(item, hatenaNamespace + "imageurl");

                    if (string.IsNullOrWhiteSpace(thumbnailUrl))
                    {
                        thumbnailUrl = item.Element(mediaNamespace + "thumbnail")?.Attribute("url")?.Value ?? string.Empty;
                    }

                    if (string.IsNullOrWhiteSpace(thumbnailUrl))
                    {
                        thumbnailUrl = TryGetImageFromContent(GetElementValue(item, XName.Get("encoded", "http://purl.org/rss/1.0/modules/content/")));
                    }

                    int.TryParse(GetElementValue(item, hatenaNamespace + "bookmarkcount"), out int bookmarkCount);

                    return new Entry
                    {
                        Title = title,
                        Url = url,
                        Domain = GetDomain(url),
                        BookmarkCount = bookmarkCount,
                        ThumbnailUrl = thumbnailUrl,
                        Summary = summary,
                    };
                })
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Title) && !string.IsNullOrWhiteSpace(entry.Url))
                .ToList();
        }

        private static string GetElementValue(XElement element, string localName)
        {
            return element
                .Elements()
                .FirstOrDefault(child => child.Name.LocalName == localName)
                ?.Value
                .Trim() ?? string.Empty;
        }

        private static string GetElementValue(XElement element, XName name)
        {
            return element.Element(name)?.Value.Trim() ?? string.Empty;
        }

        private static string CleanSummary(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string withoutTags = Regex.Replace(value, "<.*?>", string.Empty);
            string decoded = WebUtility.HtmlDecode(withoutTags);
            return Regex.Replace(decoded, @"\s+", " ").Trim();
        }

        private static string TryGetImageFromContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            Match match = Regex.Match(WebUtility.HtmlDecode(content), "<img[^>]+src=[\"'](?<url>[^\"']+)[\"']", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups["url"].Value : string.Empty;
        }

        private static string GetDomain(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                return string.Empty;
            }

            return uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                ? uri.Host[4..]
                : uri.Host;
        }
    }

    public sealed record HatenaCategory(string Name, string RssUrl);

    public sealed class CategoryTab : INotifyPropertyChanged
    {
        private bool isSelected;

        public CategoryTab(HatenaCategory category, bool isSelected)
        {
            Name = category.Name;
            this.isSelected = isSelected;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name { get; }

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected == value)
                {
                    return;
                }

                isSelected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BackgroundColor));
                OnPropertyChanged(nameof(TextColor));
            }
        }

        public Color BackgroundColor => IsSelected ? Color.FromArgb("#0F5EA8") : Color.FromArgb("#1B232B");
        public Color TextColor => IsSelected ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#AAB5C0");

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed class CategoryPageState : INotifyPropertyChanged
    {
        private bool isLoading;
        private bool isRefreshing;
        private string statusMessage = "\u8aad\u307f\u8fbc\u307f\u4e2d...";

        public CategoryPageState(HatenaCategory category)
        {
            Category = category;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public HatenaCategory Category { get; }
        public ObservableCollection<Entry> Entries { get; } = [];
        public List<Entry> AllEntries { get; set; } = [];

        public bool IsLoading
        {
            get => isLoading;
            set
            {
                if (isLoading == value)
                {
                    return;
                }

                isLoading = value;
                OnPropertyChanged();
            }
        }

        public bool IsRefreshing
        {
            get => isRefreshing;
            set
            {
                if (isRefreshing == value)
                {
                    return;
                }

                isRefreshing = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => statusMessage;
            set
            {
                if (statusMessage == value)
                {
                    return;
                }

                statusMessage = value;
                OnPropertyChanged();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
