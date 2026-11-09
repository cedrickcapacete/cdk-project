using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HarborKiosk.App.Services;

namespace HarborKiosk.App.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    public virtual bool IsIdleTimeoutEnabled => true;

    public virtual TimeSpan? IdleTimeoutOverride => null;

    public virtual void OnNavigatedTo()
    {
    }

    public virtual void OnNavigatedFrom()
    {
    }
}

public interface IKeypadInput
{
    void AppendDigit(char digit);

    void DeleteDigit();
}

/// <summary>Screens that show the step header (logo, back, exit, progress bar).</summary>
public abstract partial class StepViewModelBase : ViewModelBase
{
    protected StepViewModelBase(NavigationService navigation)
    {
        Navigation = navigation;
    }

    public abstract string FlowLabel { get; }

    public abstract string StepText { get; }

    public abstract double Progress { get; }

    public virtual bool CanGoBack => false;

    public virtual bool CanExit => true;

    protected NavigationService Navigation { get; }

    protected virtual void OnBack()
    {
    }

    [RelayCommand]
    private void GoBack() => OnBack();

    [RelayCommand]
    private void Exit() => Navigation.GoHome();
}

public abstract partial class KeypadStepViewModelBase : StepViewModelBase, IKeypadInput
{
    protected KeypadStepViewModelBase(NavigationService navigation)
        : base(navigation)
    {
    }

    public abstract void AppendDigit(char digit);

    public abstract void DeleteDigit();

    [RelayCommand]
    private void Digit(string? value)
    {
        if (value is { Length: 1 } && char.IsDigit(value[0]))
        {
            AppendDigit(value[0]);
        }
    }

    [RelayCommand]
    private void Backspace() => DeleteDigit();
}
