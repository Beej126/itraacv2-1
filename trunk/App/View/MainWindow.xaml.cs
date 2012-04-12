using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Documents;
using iTRAACv2.Model;

namespace iTRAACv2.View
{
  public partial class MainWindow
  {
    public MainWindow()
    {
      InitializeComponent();

      iTRAACHelpers.WpfDataGridStandardBehavior(gridUserMessages);

      //little extra code to have the popup follow its PlacementTarget when Window is moved/resized
      var w = GetWindow(popUserMessage.PlacementTarget);
      if (null != w) w.LocationChanged += (s,e) =>
      {
        popUserMessage.VerticalOffset += 1; popUserMessage.VerticalOffset -=1; 
      };

      Dispatcher.BeginInvoke((Action)(() => Keyboard.Focus(HomeTab)));
    }

    #region Tab Stuff
    //this is bound in the MainWindow.xaml - <Window.CommandBindings>
    private void OpenSponsorExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      TabSponsor.Open(tabsMain, e.Parameter.ToString());
    }

    private void OpenTaxFormExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      TabTaxForm.Open(tabsMain, e.Parameter.ToString());
    }

    private void CloseTabExecuted(object sender, ExecutedRoutedEventArgs e)
    {
      CloseTab2(tabsMain.SelectedContent as TabBase);
    }


    private void CloseTab(object sender, object args)
    {
      CloseTab2(((TabItem) ((FrameworkElement) sender).Tag).Content as TabBase);
    }

    private void CloseTab2(TabBase thetab)
    {
      if (thetab == null) return;

      thetab.Close();

      if (thetab is TabTaxForm && btnReturns.IsChecked)
        ReturnForms.txtSequenceNumber.Focus();
    }

    private void TabItemHeaderPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
      if (e.ChangedButton == MouseButton.Middle) CloseTab(sender, null);
    }
    #endregion

    #region Admin Override Stuff
    private void LblLoginMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (UserModel.Current.Access.IsAdmin)
      {
        UserModel.Current.Access.AdminOverrideCancel();
      }
      else
      {
        popAdminOverride.IsOpen = true;
        txtAdminPassword.Focus();
      }
    }

    private void TxtAdminPasswordKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        txtAdminPassword.Password = "";
        popAdminOverride.IsOpen = false;
        return;
      }

      if (e.Key != Key.Enter) return;
      if (UserModel.Current.Access.AdminOverride(txtAdminPassword.Password, 5))
      {
        txtAdminPassword.Password = "";
        popAdminOverride.IsOpen = false;
      }
      else
      {
        MessageBox.Show("Invalid Password", "Admin Override");
        txtAdminPassword.Focus();
      }
    }

    private void LblAdminMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      UserModel.Current.Access.AdminOverrideCancel();
    }
    #endregion

    #region UserMessages Stuff
    public void ShowUserMessage(string text)
    {
      lblUserMessage.Text = "";

      if (string.IsNullOrWhiteSpace(text)) return; //this way clients can have generic logic fire w/o checking to see whether they're actually sending anything in each particular case
      //e.g. TransactionType dropdown on TaxForm UI, only a few types actually have a warning message associated with them

      UserMessagesModel.Add(text);

      if (_userMessageFadeawayStoryboard == null)
      {
        _userMessageFadeawayStoryboard = ((Storyboard) FindResource("Fadeaway_Animation")).Clone(); //so we can reuse the fadeaway template in multiple places
        Storyboard.SetTarget(_userMessageFadeawayStoryboard, popUserMessage);
      }

      //textual carriage returns must be turned into <LineBreak /> objects in XAML land
      var lines = text.Split('\n'); 
      for (int i = 0; i < lines.Length; i++)
      {
        if (i>0) lblUserMessage.Inlines.Add(new LineBreak());
        lblUserMessage.Inlines.Add(lines[i]);
      }

      _userMessageFadeawayStoryboard.Begin();
    }
    private Storyboard _userMessageFadeawayStoryboard;

    private void UserMessageMouseEnter(object sender, MouseEventArgs e)
    {
      //on MouseEnter, bring opacity back up to 100% and pause fadeaway
      _userMessageCurrentFadePosition = _userMessageFadeawayStoryboard.GetCurrentTime();
      _userMessageFadeawayStoryboard.Seek(new TimeSpan(0, 0, 0));
      _userMessageFadeawayStoryboard.Pause();
    }
    private TimeSpan _userMessageCurrentFadePosition;

    private void UserMessageMouseLeave(object sender, MouseEventArgs e)
    {
      //on MouseLeave, return opacity to where it was before we interrupted and continue fadeaway
      _userMessageFadeawayStoryboard.Seek(_userMessageCurrentFadePosition);
      _userMessageFadeawayStoryboard.Resume();
    }

    //not digging this at all... manually manipulating the Grid.RowDefinition.Height... 
    //would've been nice if the row would automatically close when Row.Content.Visibility = Collapsed,
    //but it would just sit there with blank Content at the same position
    //apparently it's the <GridSplitter>'s fault: http://stackoverflow.com/questions/1601171/wpf-grid-auto-sized-column-not-collapsing-when-content-visibility-set-to-visibi
    //i don't want to use an Expander either because i want the user to specify how much they open this little "peek window" and the Splitter feels right for this
    //it's actually justified now that i've incorporated an Easing Animation to the whole affair
    private void BtnUserMessagesIsCheckedChanged(object sender, RoutedEventArgs e)
    {
      UserMessageRow.GridSplitterOpeningBounce(btnUserMessages.IsChecked, 50);
    }
    #endregion

    private void BtnFontSizeResetClick(object sender, RoutedEventArgs e)
    {
      sliderZoom.Value = 1.0;
    }

    private void BtnReturnsIsCheckedChanged(object sender, RoutedEventArgs e)
    {
      ReturnsColumn.GridSplitterOpeningBounce(btnReturns.IsChecked, 300);
    }

    private void PopUserMessageMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      btnUserMessages.IsChecked = true;
    }


  }

}
