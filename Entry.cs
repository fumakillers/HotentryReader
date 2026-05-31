namespace HotentryReader
{
    public sealed class Entry
    {
        public string Title { get; init; } = string.Empty;
        public string Url { get; init; } = string.Empty;
        public string Domain { get; init; } = string.Empty;
        public int BookmarkCount { get; init; }
        public string ThumbnailUrl { get; init; } = string.Empty;
        public string Summary { get; init; } = string.Empty;

        public string BookmarkCountText => $"{BookmarkCount} users";
        public bool HasThumbnail => !string.IsNullOrWhiteSpace(ThumbnailUrl);
        public bool HasNoThumbnail => !HasThumbnail;
    }
}
