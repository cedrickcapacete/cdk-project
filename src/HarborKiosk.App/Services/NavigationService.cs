using System;
using CommunityToolkit.Mvvm.ComponentModel;
using HarborKiosk.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace HarborKiosk.App.Services;

public sealed partial class NavigationService : ObservableObject
{
    private readonly IServiceProvider _services;
    private readonly DepositSession _session;

    [ObservableProperty]
    private ViewModelBase? _currentViewModel;

    public NavigationService(IServiceProvider services, DepositSession session)
    {
        _services = services;
        _session = session;
    }

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        var next = _services.GetRequiredService<TViewModel>();
        CurrentViewModel?.OnNavigatedFrom();
        CurrentViewModel = next;
        next.OnNavigatedTo();
    }

    public void GoHome()
    {
        _session.Reset();
        NavigateTo<WelcomeViewModel>();
    }
}
