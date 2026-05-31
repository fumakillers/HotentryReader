using Android.App;
using Android.Content.PM;
using Android.OS;

namespace HotentryReader
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnResume()
        {
            base.OnResume();

            if (MainPage.CurrentPageReference?.TryGetTarget(out MainPage? mainPage) == true)
            {
                mainPage.RestoreEntryLongPressHandlers();
            }
        }
    }
}
