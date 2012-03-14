using System.Windows;
using System.Windows.Controls;

namespace iTRAACv2.View
{
  public class UcShowHideCheckBox : CheckBox
  {

    public FrameworkElement IsUnCheckedVisibleControl
    {
      get { return (FrameworkElement)GetValue(IsUnCheckedVisibleControlProperty); }
      set { SetValue(IsUnCheckedVisibleControlProperty, value); }
    }
    public static readonly DependencyProperty IsUnCheckedVisibleControlProperty =
        DependencyProperty.Register("IsUnCheckedVisibleControl", typeof(FrameworkElement), typeof(UcShowHideCheckBox), new UIPropertyMetadata(null));

    public FrameworkElement IsCheckedVisibleControl
    {
      get { return (FrameworkElement)GetValue(IsCheckedVisibleControlProperty); }
      set { SetValue(IsCheckedVisibleControlProperty, value); }
    }
    public static readonly DependencyProperty IsCheckedVisibleControlProperty =
        DependencyProperty.Register("IsCheckedVisibleControl", typeof(FrameworkElement), typeof(UcShowHideCheckBox), new UIPropertyMetadata(null));


    /// <summary>
    /// If using for Enable/Disable functionality, only specify either IsUnCheckedVisibleControl or IsCheckedVisibleControl not both
    /// </summary>
    public bool Disable
    {
      get { return (bool)GetValue(DisableProperty); }
      set { SetValue(DisableProperty, value); }
    }
    public static readonly DependencyProperty DisableProperty =
        DependencyProperty.Register("Disable", typeof(bool), typeof(UcShowHideCheckBox), new UIPropertyMetadata(false));



    protected override void OnChecked(RoutedEventArgs e)
    {
      base.OnChecked(e);
      if (Disable)
      {
        if (IsUnCheckedVisibleControl != null) IsUnCheckedVisibleControl.IsEnabled = false;
        else if (IsCheckedVisibleControl != null) IsCheckedVisibleControl.IsEnabled = false;
      }
      else
      {
        if (IsCheckedVisibleControl != null) IsCheckedVisibleControl.Visibility = Visibility.Visible;
        if (IsUnCheckedVisibleControl != null) IsUnCheckedVisibleControl.Visibility = Visibility.Collapsed;
      }
    }

    protected override void OnUnchecked(RoutedEventArgs e)
    {
      base.OnUnchecked(e);
      if (Disable)
      {
        if (IsUnCheckedVisibleControl != null) IsUnCheckedVisibleControl.IsEnabled = true;
        else if (IsCheckedVisibleControl != null) IsCheckedVisibleControl.IsEnabled = true;
      }
      else
      {
        if (IsCheckedVisibleControl != null) IsCheckedVisibleControl.Visibility = Visibility.Collapsed;
        if (IsUnCheckedVisibleControl != null) IsUnCheckedVisibleControl.Visibility = Visibility.Visible;
      }
    }
  }
}
