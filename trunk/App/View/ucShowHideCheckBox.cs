using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace iTRAACv2
{
  public class ucShowHideCheckBox : CheckBox
  {

    public FrameworkElement IsUnCheckedVisibleControl
    {
      get { return (FrameworkElement)GetValue(IsUnCheckedVisibleControlProperty); }
      set { SetValue(IsUnCheckedVisibleControlProperty, value); }
    }
    public static readonly DependencyProperty IsUnCheckedVisibleControlProperty =
        DependencyProperty.Register("IsUnCheckedVisibleControl", typeof(FrameworkElement), typeof(ucShowHideCheckBox), new UIPropertyMetadata(null));

    public FrameworkElement IsCheckedVisibleControl
    {
      get { return (FrameworkElement)GetValue(IsCheckedVisibleControlProperty); }
      set { SetValue(IsCheckedVisibleControlProperty, value); }
    }
    public static readonly DependencyProperty IsCheckedVisibleControlProperty =
        DependencyProperty.Register("IsCheckedVisibleControl", typeof(FrameworkElement), typeof(ucShowHideCheckBox), new UIPropertyMetadata(null));


    /// <summary>
    /// If using for Enable/Disable functionality, only specify either IsUnCheckedVisibleControl or IsCheckedVisibleControl not both
    /// </summary>
    public bool Disable
    {
      get { return (bool)GetValue(DisableProperty); }
      set { SetValue(DisableProperty, value); }
    }
    public static readonly DependencyProperty DisableProperty =
        DependencyProperty.Register("Disable", typeof(bool), typeof(ucShowHideCheckBox), new UIPropertyMetadata(false));



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
