using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Text.RegularExpressions;
using System.Windows.Documents;

namespace iTRAACv2
{

  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();

      iTRAACHelpers.WPFDataGrid_Standard_Behavior(gridUserMessages);

      //little extra code to have the popup follow its PlacementTarget when Window is moved/resized
      Window w = Window.GetWindow(popUserMessage.PlacementTarget);
      if (null != w) w.LocationChanged += delegate(object sender, EventArgs args)
      {
        popUserMessage.VerticalOffset += 1; popUserMessage.VerticalOffset -=1; 
      };

      Loaded += new RoutedEventHandler(MainWindow_Loaded);

    }

    void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
      //(tabsMain.Items[0] as TabItem).Focus();
    }

    #region Tab Stuff
    //this is bound in the MainWindow.xaml - <Window.CommandBindings>
    private void OpenSponsor_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      tabSponsor.Open(tabsMain, e.Parameter.ToString());
    }

    private void OpenTaxForm_Executed(object sender, ExecutedRoutedEventArgs e)
    {
      tabTaxForm.Open(tabsMain, e.Parameter.ToString());
    }

    private void CloseTab(object sender, object args)
    {
      (((sender as FrameworkElement).Tag as TabItem).Content as tabBase).Close();
    }

    private void TabItemHeader_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
      if (e.ChangedButton == System.Windows.Input.MouseButton.Middle) CloseTab(sender, null);
    }
    #endregion

    #region Admin Override Stuff
    private void lblLogin_MouseDoubleClick(object sender, MouseButtonEventArgs e)
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

    private void txtAdminPassword_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape)
      {
        txtAdminPassword.Password = "";
        popAdminOverride.IsOpen = false;
        return;
      }

      if (e.Key == Key.Enter)
      {
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
    }

    private void lblAdmin_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      UserModel.Current.Access.AdminOverrideCancel();
    }
    #endregion

    #region UserMessages Stuff
    public void ShowUserMessage(string Text)
    {
      lblUserMessage.Text = "";

      if (string.IsNullOrWhiteSpace(Text)) return; //this way clients can have generic logic fire w/o checking to see whether they're actually sending anything in each particular case
      //e.g. TransactionType dropdown on TaxForm UI, only a few types actually have a warning message associated with them

      UserMessagesModel.Add(Text);

      if (UserMessage_FadeawayStoryboard == null)
      {
        UserMessage_FadeawayStoryboard = (FindResource("Fadeaway_Animation") as Storyboard).Clone(); //so we can reuse the fadeaway template in multiple places
        Storyboard.SetTarget(UserMessage_FadeawayStoryboard, popUserMessage);
      }

      //textual carriage returns must be turned into <LineBreak /> objects in XAML land
      string[] lines = Text.Split('\n'); 
      for (int i = 0; i < lines.Length; i++)
      {
        if (i>0) lblUserMessage.Inlines.Add(new LineBreak());
        lblUserMessage.Inlines.Add(lines[i]);
      }

      UserMessage_FadeawayStoryboard.Begin();
    }
    private Storyboard UserMessage_FadeawayStoryboard;

    private void UserMessage_MouseEnter(object sender, MouseEventArgs e)
    {
      //on MouseEnter, bring opacity back up to 100% and pause fadeaway
      UserMessage_CurrentFadePosition = UserMessage_FadeawayStoryboard.GetCurrentTime();
      UserMessage_FadeawayStoryboard.Seek(new TimeSpan(0, 0, 0));
      UserMessage_FadeawayStoryboard.Pause();
    }
    private TimeSpan UserMessage_CurrentFadePosition;

    private void UserMessage_MouseLeave(object sender, MouseEventArgs e)
    {
      //on MouseLeave, return opacity to where it was before we interrupted and continue fadeaway
      UserMessage_FadeawayStoryboard.Seek(UserMessage_CurrentFadePosition);
      UserMessage_FadeawayStoryboard.Resume();
    }

    //not digging this at all... manually manipulating the Grid.RowDefinition.Height... 
    //would've been nice if the row would automatically close when Row.Content.Visibility = Collapsed,
    //but it would just sit there with blank Content at the same position
    //apparently it's the <GridSplitter>'s fault: http://stackoverflow.com/questions/1601171/wpf-grid-auto-sized-column-not-collapsing-when-content-visibility-set-to-visibi
    //i don't want to use an Expander either because i want the user to specify how much they open this little "peek window" and the Splitter feels right for this
    //it's actually justified now that i've incorporated an Easing Animation to the whole affair
    private void btnUserMessages_IsCheckedChanged(object sender, RoutedEventArgs e)
    {
      WPFHelpers.GridSplitterOpeningBounce(UserMessageRow, btnUserMessages.IsChecked, 50);
    }
    #endregion

    private void btnFontSizeReset_Click(object sender, RoutedEventArgs e)
    {
      sliderZoom.Value = 1.0;
    }

    private void btnReturns_IsCheckedChanged(object sender, RoutedEventArgs e)
    {
      WPFHelpers.GridSplitterOpeningBounce(ReturnsColumn, btnReturns.IsChecked, 300,
        //annoying but true... stuff has to be visible before .Focus() will work
        //and the Returns popout dialog is not visible until the animation makes it so
        //so we have to wait until the animation has .Completed to set focus by passing this delegate
        (bool Opening) => { if (Opening) ReturnForms.txtSequenceNumber.Focus(); } 
      );

      //save focus so when we return/file & close Form, we can automatically put focus back in the returns search box
      //trying to streamline rapid fire 100% keyboard driven return/files as much as possible
      if (btnReturns.IsChecked) App.FocusStack_Push(ReturnForms.txtSequenceNumber); 
    }

    private void popUserMessage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      btnUserMessages.IsChecked = true;
    }

  }

}
