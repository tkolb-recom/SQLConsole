using System.ComponentModel;

namespace Recom.SQLConsole.Services;

public class NavigationService : INavigationService
{
    private readonly Dictionary<Type, Type> _viewMapping = new();

    private readonly Dictionary<object, Window> _openWindows = new();

    public void Register<TViewModel, TWindow>()
        where TViewModel : class, INotifyPropertyChanged
        where TWindow : Window
    {
        _viewMapping[typeof(TViewModel)] = typeof(TWindow);
    }

    public void Navigate<TViewModel, TOwner>(TViewModel viewModel, TOwner? owner = null)
        where TViewModel : class, INotifyPropertyChanged
        where TOwner : class, INotifyPropertyChanged
    {
        Window window = this.CreateWindow(viewModel);
        if (owner != null && _openWindows.TryGetValue(owner, out Window? ownerWindow))
        {
            window.Owner = ownerWindow;
        }

        window.Closed += (sender, _) => this.HandleWindowClosed((Window)sender!);
        window.Show();
    }

    public bool? ShowDialog<TViewModel, TOwner>(TViewModel viewModel, TOwner? owner = null)
        where TViewModel : class, INotifyPropertyChanged
        where TOwner : class, INotifyPropertyChanged
    {
        Window window = this.CreateWindow(viewModel);
        if (owner != null && _openWindows.TryGetValue(owner, out Window? ownerWindow))
        {
            window.Owner = ownerWindow;
        }

        window.Closed += (sender, _) => this.HandleWindowClosed((Window)sender!);
        return window.ShowDialog();
    }

    private Window CreateWindow(object viewModel)
    {
        Type viewModelType = viewModel.GetType();
        if (!_viewMapping.TryGetValue(viewModelType, out Type? windowType))
        {
            throw new InvalidOperationException($"No window registered for view model {viewModelType.FullName}");
        }

        if (_openWindows.TryGetValue(viewModel, out Window? opened))
        {
            opened.Activate();

            return opened;
        }

        var window = (Window)Activator.CreateInstance(windowType)!;
        window.DataContext = viewModel;

        _openWindows[viewModel] = window;
        return window;
    }

    private void HandleWindowClosed(Window window)
    {
        if (_openWindows.Remove(window.DataContext))
        {
            window.Owner = null;
        }
    }

    public void Close<TViewModel>(TViewModel viewModel, bool? result = null)
        where TViewModel : class, INotifyPropertyChanged
    {
        if (_openWindows.TryGetValue(viewModel, out Window? window))
        {
            if (result.HasValue)
            {
                window.DialogResult = result.Value;
            }

            window.Close();

            _openWindows.Remove(viewModel);
        }
    }
}