namespace HotentryReader
{
    public sealed class MuteWord
    {
        public string Word { get; set; } = string.Empty;
        public bool IsRegex { get; set; }

        public string TypeText => IsRegex ? "\u6b63\u898f\u8868\u73fe" : "\u901a\u5e38\u30ef\u30fc\u30c9";
    }
}
