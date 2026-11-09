using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using HarborKiosk.App.Services;

namespace HarborKiosk.App.ViewModels;

public sealed partial class WelcomeViewModel : ViewModelBase
{
    private readonly NavigationService _navigation;
    private readonly DepositSession _session;

    public WelcomeViewModel(NavigationService navigation, DepositSession session)
    {
        _navigation = navigation;
        _session = session;
        HeroImage = LoadHeroImage();
    }

    /// <summary>Optional photo dropped at Assets/hero.jpg next to the executable; a branded gradient is shown otherwise.</summary>
    public ImageSource? HeroImage { get; }

    public override bool IsIdleTimeoutEnabled => false;

    [RelayCommand]
    private void StartDeposit() => Start(KioskFlow.Deposit);

    [RelayCommand]
    private void ViewHistory() => Start(KioskFlow.History);

    private void Start(KioskFlow flow)
    {
        _session.Reset();
        _session.Flow = flow;
        _navigation.NavigateTo<AccountNumberViewModel>();
    }

    private static ImageSource? LoadHeroImage()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "hero.jpg");
        if (!File.Exists(path))
        {
            return null;
        }

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(path);
        image.EndInit();
        image.Freeze();
        return image;
    }
}
