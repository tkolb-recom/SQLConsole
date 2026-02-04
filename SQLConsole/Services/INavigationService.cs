using System.ComponentModel;

namespace Recom.SQLConsole.Services;

public interface INavigationService
{
    /// <summary>
    /// Registers a view model type with a window type.
    /// </summary>
    /// <typeparam name="TViewModel">The view model type to register.</typeparam>
    /// <typeparam name="TWindow">The window type associated with the view model.</typeparam>
    void Register<TViewModel, TWindow>()
        where TViewModel : class, INotifyPropertyChanged
        where TWindow : Window;

    /// <summary>
    /// Navigates to the given view model.
    /// </summary>
    /// <param name="viewModel">The view model to navigate to.</param>
    /// <param name="owner">The view model of the owner window for the dialog.</param>
    void Navigate<TViewModel, TOwner>(TViewModel viewModel, TOwner? owner = null)
        where TViewModel : class, INotifyPropertyChanged
        where TOwner : class, INotifyPropertyChanged;

    /// <summary>
    /// Shows a modal dialog for the given view model.
    /// </summary>
    /// <param name="viewModel">The view model to show in a modal dialog.</param>
    /// <param name="owner">The view model of the owner window for the dialog.</param>
    /// <returns>The result of the dialog, or null if the dialog was closed without a result.</returns>
    bool? ShowDialog<TViewModel, TOwner>(TViewModel viewModel, TOwner? owner = null)
        where TViewModel : class, INotifyPropertyChanged
        where TOwner : class, INotifyPropertyChanged;

    /// <summary>
    /// Closes the window associated with the given view model.
    /// </summary>
    /// <param name="viewModel">The view model to close windows for.</param>
    /// <param name="result">The result to pass to the closed windows.</param>
    void Close<TViewModel>(TViewModel viewModel, bool? result = null)
        where TViewModel : class, INotifyPropertyChanged;
}