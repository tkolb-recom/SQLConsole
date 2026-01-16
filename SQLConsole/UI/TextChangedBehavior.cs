using System.Windows;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using Microsoft.Xaml.Behaviors;

namespace Recom.SQLConsole.UI;

public class TextChangedBehavior : Behavior<TextEditor>
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(TextChangedBehavior));

    public ICommand? Command
    {
        get => (ICommand)this.GetValue(CommandProperty);
        set => this.SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(TextChangedBehavior));

    public object? CommandParameter
    {
        get => this.GetValue(CommandParameterProperty);
        set => this.SetValue(CommandParameterProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (this.AssociatedObject != null)
        {
            this.AssociatedObject.TextChanged += this.OnTextChanged;
        }
    }

    protected override void OnDetaching()
    {
        if (this.AssociatedObject != null)
        {
            this.AssociatedObject.TextChanged -= this.OnTextChanged;
        }

        base.OnDetaching();
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        ICommand? cmd = this.Command;
        object? parameter = this.CommandParameter ?? this.AssociatedObject?.Text;
        if (cmd != null && cmd.CanExecute(parameter))
        {
            cmd.Execute(parameter);
        }
    }
}