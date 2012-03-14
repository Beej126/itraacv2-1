using System.Windows.Input;

namespace iTRAACv2.View
{
  class RoutedCommands
  {
    public static readonly RoutedCommand OpenTaxForm = new RoutedCommand();
    public static readonly RoutedCommand OpenSponsor = new RoutedCommand();
    public static readonly RoutedCommand NewSponsor = new RoutedCommand();
    public static readonly RoutedCommand CloseTab = new RoutedCommand();
  }

  /*
  public class DelegateCommand : ICommand
  {
    private Action _executeMethod;
    public DelegateCommand(Action executeMethod)
    {
      _executeMethod = executeMethod;
    }

    public bool CanExecute(object parameter)
    {
      return (true);
    }

    public event EventHandler CanExecuteChanged;

    public void Execute(object Parameter)
    {
      _executeMethod.Invoke();
    }
  }
  */
}
