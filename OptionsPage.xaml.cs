namespace HotentryReader
{
    public partial class OptionsPage : ContentPage
    {
        public MuteWordService MuteWordService { get; } = MuteWordService.Instance;

        public OptionsPage()
        {
            InitializeComponent();
            BindingContext = MuteWordService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await MuteWordService.LoadAsync();
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private async void OnAddMuteWordClicked(object? sender, EventArgs e)
        {
            await MuteWordService.AddAsync(MuteWordEntry.Text ?? string.Empty, RegexCheckBox.IsChecked);
            MuteWordEntry.Text = string.Empty;
            RegexCheckBox.IsChecked = false;
        }

        private async void OnRemoveMuteWordClicked(object? sender, EventArgs e)
        {
            if (sender is Button button &&
                button.CommandParameter is MuteWord muteWord)
            {
                await MuteWordService.RemoveAsync(muteWord);
            }
        }
    }
}
