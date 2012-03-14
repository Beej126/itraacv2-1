using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using System.ComponentModel;
using System.Windows.Documents;
using System.Text.RegularExpressions;
using iTRAACv2.Model;

namespace iTRAACv2.View
{
  public partial class TabSponsor
  {
    public SponsorModel Sponsor { get { return (Model as SponsorModel); } }

    /// <summary>
    /// </summary>
    /// <param name="tabControl">TabControl to place this new TabItem under</param>
    /// <param name="sponsorGUID"></param>
    static public void Open(TabControl tabControl, string sponsorGUID)
    {
      OpenTab<TabSponsor>(tabControl, ModelBase.Lookup<SponsorModel>(sponsorGUID));
    }

    public TabSponsor() 
    {
      InitializeComponent();

      iTRAACHelpers.WpfDataGridStandardBehavior(gridForms);
      iTRAACHelpers.WpfDataGridStandardBehavior(gridRemarks);
      iTRAACHelpers.WpfDataGridStandardBehavior(gridMatches);

      btnSuspend.Click += BtnSuspendClick;
    }

    private System.Windows.Threading.DispatcherFrame _disableRemarkAlertReasonPopupDispatcherFrame;
    void SponsorReasonConfirmation(object sender, ReasonConfirmationArgs e)
    {
      DisableRemarkAlertReasonPopup.Show();

      //nugget: idea from here: http://www.deanchalk.me.uk/post/WPF-Modal-Controls-Via-Dispatcher (Army firewall blocks this site)
      //nugget: this is really cool because it maintains the synchronous nature of this call stack, returning the result to the model layer after the psuedo modal popup, even though the popup is actually acting on it's own asynch event handler!!
      _disableRemarkAlertReasonPopupDispatcherFrame = new System.Windows.Threading.DispatcherFrame(); //nugget:
      System.Windows.Threading.Dispatcher.PushFrame(_disableRemarkAlertReasonPopupDispatcherFrame); //nugget: blocks gui message pump & createst nested pump, making this a blocking call 

      e.Accept = DisableRemarkAlertReasonPopup.IsOK;
      e.Reason = DisableRemarkAlertReasonPopup.ReasonText;
    }

    private void DisableRemarkAlertReasonPopupResult(ReasonPopupResultEventArgs args)
    {
      _disableRemarkAlertReasonPopupDispatcherFrame.Continue = false; //nugget: cancels the nested pump which allows the code after the PushFrame above to continue
    }

    protected override void OnClosed()
    {
      Sponsor.PropertyChanged -= SponsorPropertyChanged;
      btnSuspend.Click -= BtnSuspendClick;
    }

    private void GridRemarksSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      //nugget: hyperlink any Form# matches in the Remarks - nice! 
      //nugget: unfortunately any kind of code driven or user driven sorting blows away custom DataGridColumn.GetCellContent() modifications like this
      //nugget: see here: http://social.msdn.microsoft.com/forums/en-US/wpf/thread/63974f4f-d9ee-45af-8499-42f29cbc22ae?prof=required

      //so, just wound up doing this when the grid is clicked... still pretty practical and works great... much more convenient than opening this form through alternative means

      DataGridColumn remarksCol = gridRemarks.Columns.FirstOrDefault(c => c.SortMemberPath == "Remarks");
      if (remarksCol == null) return;

      //the regex looks like this: (.*?)([NE]F[12]-[A-Z]{2}-[0-9]{2}-[0-9]{5,6})
      //there are two capture groups (group[0] is always the full match):
      //group[1]: preliminary text
      //group[2]: OrderNumber
      //this way we can match multiple OrderNumbers in the text
      //each match gets the content between OrderNumbers
      //the very last bit tacks on any text that falls after the last OrderNumber
      //e.g. blah1 OrderNumber1 blah2 OrderNumber2 blah3
      //match[0]: blah1 OrderNumber1
      //match[1]: blah2 OrderNumber2

      var lblRemarks = remarksCol.GetCellContent(gridRemarks.CurrentItem) as TextBlock;  //this basically works but any kind of grid sorting must virtualize this info since it always returns null after that
      if (lblRemarks == null) return;

      Hyperlink dummy;
      if (lblRemarks.TryFindChild(out dummy)) return; //if this column is already hyperlinked, bail out

      var original = lblRemarks.Text;
      var matches = OrderNumberRegEx.Matches(original);
      if (matches.Count == 0) return;

      lblRemarks.Inlines.Clear();
      var removestr = "";
      foreach(Match match in matches)
      {
        var link = new Hyperlink {Command = RoutedCommands.OpenTaxForm, CommandParameter = match.Groups[2].Value};
        //this is the OrderNumber... and then the magic of RoutedCommands makes it elegant to fire open the order tab
        link.Inlines.Add(match.Groups[2].Value);
        lblRemarks.Inlines.Add(match.Groups[1].Value);
        lblRemarks.Inlines.Add(link);
        removestr += match.Value;
      }
      lblRemarks.Inlines.Add(original.Replace(removestr, "")); //tack on any text following the last OrderNumber match
    }
    static private readonly Regex OrderNumberRegEx = new Regex(SettingsModel.Global["OrderNumberRegEx"], RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    protected override void UserControlLoaded(object sender, RoutedEventArgs e)
    {
      Sponsor.ReasonConfirmation += SponsorReasonConfirmation;

      InitializePotentialMatchesGridGrouping();

      WPFHelpers.GridSort(gridForms, "Purchased", ListSortDirection.Descending);
      WPFHelpers.GridSort(gridRemarks, new[] { "Alert", "LastUpdate" }, new[] { ListSortDirection.Descending, ListSortDirection.Descending });

      Sponsor.Transactions.CollectionChanged += (s, a) => { if (a.NewItems != null) TransactionsDataGrid.ScrollIntoView(a.NewItems[0]); }; //nugget: AutoScroll DataGrid
      rdoHideReturnedTaxForms.IsChecked = true;

      if (Sponsor.TaxFormsCountReturnedNotFiled > 0) 
        ReturnedNotFiledLabel.Background = ReturnedNotFiledLabel.Background.BeginBrushColorAnimation(Colors.Red);

      if (Sponsor.Class1TaxFormsCountUnreturned > SettingsModel.MaxClass1FormsCount)
        UnreturnedLabel.Background = UnreturnedLabel.Background.BeginBrushColorAnimation(Colors.Red);

      Sponsor.PropertyChanged += SponsorPropertyChanged;

      gridMembers.ItemContainerGenerator.ContainerFromIndex(0).FindChild<TextBox>(c => c.Name == "txtSSN1").Focus();
    }

    #region Potential Matches stuff

    void SponsorPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
      if (e.PropertyName == "PotentialClientMatches") PotentialMatchesUpdated();
    }

    void PotentialMatchesUpdated()
    {
      //nugget: total hack... DataGrid's don't AutoGenerate columns if there aren't any rows in the initial ItemsSource... that's pretty bogus if you ask me, DataTable's/DataView's can obviously have columns even if they don't have rows yet
      //so i've currently found no cleaner way than this ugly code which twiddles the ItemsSource enough to wake the DataGrid back up to AutoGeneate it's columns
      //... this spooks me out ... i guess every other DataGrid has always been fed some initial rows that hold this house of cards together, wild

      //nugget: there is something about the underlying approach where the bound DataGrid was tough to get to populate
      //        not just the AutogenerateColumns but even the rows wouldn't fill in unless i reassigned the ItemsSource
      //        my best guess is that it's because the PotentialClientMatches backing table gets updated on a background thread
      //        i thought it was a recommended approach to fill the bound ItemsSource however (background or not) and expect the UI to auto refresh

      gridMatches.ItemsSource = null;
      gridMatches.ItemsSource = Sponsor.PotentialClientMatches;

      //if we weren't already popped open, do the bounce
      if (Sponsor.PotentialClientMatches.Count > 0)
        MatchesColumn.GridSplitterOpeningBounce(true, 300);
    }

    private void InitializePotentialMatchesGridGrouping()
    {
      //nugget: show a grouping level for each block of potential matches that comes from a particular hit - DutyPhone, SSN, etc.
      //another way, maybe useful somewhere: BindingListCollectionView v = (BindingListCollectionView)CollectionViewSource.GetDefaultView(PotentialMatches);

      // good grouping related info pages:
      // http://stackoverflow.com/questions/2890371/wpf-bindinglistcollectionview-to-listcollectionview-for-datatable-as-itemssource
      // http://www.c-sharpcorner.com/UploadFile/dpatra/548/
      // http://bea.stollnitz.com/blog/?p=17&replytocom=348#respond
      // http://bea.stollnitz.com/blog/?p=387
      // http://www.beacosta.com/blog/?p=24
      // http://vbcity.com/blogs/xtab/archive/2011/01/12/wpf-listbox-custom-sort-within-groups.aspx

      if (gridMatches.Items.GroupDescriptions != null && gridMatches.Items.GroupDescriptions.Count == 0)
        gridMatches.Items.GroupDescriptions.Add(new PropertyGroupDescription("MatchType"));

      //*** wound up simply moving this sort to the underlying  sponsor.PotentialClientMatches DataView.Sort
      //sorting by increasing recordcount allows us to put the most specific and therefore hopefully the "best" matches up top
      //if (gridMatches.Items.SortDescriptions.Count == 0) //nugget: this gets whacked when you reassign the ItemSource!! :(
      //  gridMatches.Items.SortDescriptions.Add(new SortDescription("RecordCountID", ListSortDirection.Ascending));
    }

    #endregion

    private void BtnCartIsCheckedChanged(object sender, RoutedEventArgs e)
    {
      ShoppingCartColumn.GridSplitterOpeningBounce(btnCart.IsChecked, 275);
    }

    private void BtnNewNF1PackageClick(object sender, RoutedEventArgs e)
    {
      var anim = (Storyboard) Application.Current.FindResource("PulsingOpacity");
      if (anim != null) imgNewFormsCount.BeginStoryboard(anim);

      //fill combo with values up to how many new forms are allowed max... this much UI code is probably asking to be in a ViewModel
      cbxNewFormsCount.Items.Clear();
      for (int i = Sponsor.Class1TaxFormsCountRemainingToBuy; i > 0; i--) cbxNewFormsCount.Items.Add(i);
      cbxNewFormsCount.SelectedIndex = 0; //default to max remaining
      cbxNewFormsCount.IsDropDownOpen = (cbxNewFormsCount.Items.Count > 1); //be nice and auto-open the drop down if there is actually a choice to be made

      IsClass2Popup = false;
      popPrintNF1s.IsOpen = true;

      cbxNewFormsCount.Focus();
    }

    private void BtnReadyToPrintNoClick(object sender, RoutedEventArgs e)
    {
      popPrintNF1s.IsOpen = false;
    }

    public bool IsClass2Popup
    {
      get { return (bool)GetValue(IsClass2PopupProperty); }
      set { SetValue(IsClass2PopupProperty, value); }
    }
    public static readonly DependencyProperty IsClass2PopupProperty =
        DependencyProperty.Register("IsClass2Popup", typeof(bool), typeof(TabSponsor), new UIPropertyMetadata(false));

    private void BtnReadyToPrintClick(object sender, RoutedEventArgs e)
    {
      //this is sort of embarrasingly old school validation...
      //these days the "proper" ViewModel way would be to putt an "IsSelected" property on a model collection and then binding that to an ItemsControl which generates the radio button group
      //(this would have to be implemented as an on-the-fly linq query wrapped around TaxForm.Type.UserSelectableClass1 which tacked on "IsSelected" as a new property via linq projection syntax)
      //then put validation logic on that collection enforcing that one item has to have IsSelected==true
      if (rdoNF1.IsChecked == null || rdoEF1.IsChecked == null || (!rdoEF1.IsChecked.Value && !rdoNF1.IsChecked.Value))
      {
        App.ShowUserMessage("Form Type Selection Required");
        rdoNF1.Focus();
        return;
      }

      popPrintNF1s.IsOpen = false;

      //** UI is very much acting as a "controller" here... joining Sponsor and TaxForm... i think it works and pretty cleanly but is probably another ViewModel example
      //the main judgement for me is that wherever else this pattern would come up, there wouldn't be much logic left to be duplicate code, all logic is basically pulling values out of fields and passing them to reusable subroutines
      //the coordination of three entities is probably the one thing that asks to be encapsulated further

      //TODO: hopefully this whole SpinWait/background-thread pattern can be encapsulated in something directly reusable
      //the main trick to figure out is how to make a really clean & obvious separation between gathering the values from the UI
      //(which are assigned to stored proc parms)
      //and separate that from the stored proc execution
      //it seems like Proc could simply be handed back from the model to the UI as a typical "data transfer object" (DTO) pattern
      //e.g. public Proc sponsor.SetupNewPackage(...) {... return(proc); }

      //not necessary for now, the proc is responding very well: App.ShowWaitAnimation();

      //while we're still executing on the UI thread where UI property access is allowed,
      //pull all values from UI fields and assign them to a TaxFormPackage "DTO" (Data Transfer Object)
      //this facilitates doing the DB work on a background thread... see next block of code
      new TaxFormModel.TaxFormPackage(
        // ReSharper disable RedundantArgumentName
        parentTransactionList: Sponsor.Transactions,
        isPending: sender == btnReadyToPrint_Cart,
        sponsorGUID: Sponsor.GUID, 
        authorizedDependentClientGUID: (string)lbxPrintDependents.SelectedValue,
        formType: TaxFormModel.FormType.NF1,
        qty: Convert.ToInt32(cbxNewFormsCount.SelectedValue) );
        // ReSharper restore RedundantArgumentName

      btnCart.IsChecked = true; //for user convenience, show cart to remind clerk to get money from customer
    }

    private void BtnNewNF2Click(object sender, RoutedEventArgs e)
    {
      //if spouse is the only choice, don't bother showing selection
      if (PanelDependentsPicklist.Visibility == Visibility.Visible)
      {
        IsClass2Popup = true;
        popPrintNF1s.IsOpen = true;
      }
      else
      {
        BtnNewNF2OKClick(null, null);
      }
    }

    private void BtnNewNF2OKClick(object sender, RoutedEventArgs e)
    {
      var authdep = lbxPrintDependents.SelectedItem as SponsorModel.DependentLight;

      TaxFormModel newForm = TaxFormModel.NewNF2(
        Sponsor.Transactions,
        Sponsor.GUID,
        string.Format("{0}, {1} ({2})", Sponsor.HouseMembers[0]["LName"], Sponsor.HouseMembers[0]["FName"], Sponsor.HouseMembers[0]["CCode"]),
        (string)lbxPrintDependents.SelectedValue,
        (authdep != null)?authdep.FirstName:null);

      RoutedCommands.OpenTaxForm.Execute(newForm.GUID, null);

      btnCart.IsChecked = true; //for user convenience, show cart to remind clerk to get money from customer
    }

    //set spouse as default authorized dependent because they are guaranteed privileges
    private void LbxPrintDependentsLoaded(object sender, RoutedEventArgs e)
    {
      for(var i=0; i < lbxPrintDependents.Items.Count; i++)
        if (((SponsorModel.DependentLight)lbxPrintDependents.Items[i]).IsSpouse)
        {
          lbxPrintDependents.SelectedIndex = i;
          break;
        }
    }

    private void EndBackgroundAnimation(object sender, MouseButtonEventArgs e)
    {
      ((Border) sender).Background.EndBrushColorAnimation();
    }

    private void BtnSaveClick(object sender, RoutedEventArgs e)
    {
      Sponsor.Save();
    }

    private void BtnSaveCloseClick(object sender, RoutedEventArgs e)
    {
      if (Sponsor.SaveUnload()) Close();
    }

    private void NumericOnlyPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      //can't assign *static* event handlers in XAML so make that little hop here
      WPFHelpers.IntegerOnlyTextBoxPreviewTextInput(sender, e);
    }

    private void SetSpouseClick(object sender, RoutedEventArgs e)
    {
      var newSpouseClientGUID = Convert.ToString(((Control) sender).Tag);
      if (newSpouseClientGUID == "" || !Sponsor.HouseMembers.Cast<DataRowView>().Any(r => r.Field<bool>("IsSpouse"))
        || MessageBox.Show("Spouse already defined.  Are you sure you want to override?", "Set Spouse", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        Sponsor.SetSpouse(newSpouseClientGUID);
    }

    private void DivorceClick(object sender, RoutedEventArgs e)
    {
      if (MessageBox.Show("Has Sponsor provided FINALIZED DIVORCE paperwork?\n\n" +
        "Note: Legal \"Separation\" is NOT sufficient.",
        "Divorce:", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

      //bool dome = (sponsor.HasSpouse) ? MessageBox.Show("Will Spouse now become a new Sponsor?\n\n" +
      //   "Note: Tax Relief privileges must be confirmed *before* hitting Yes.\nOfficial Orders are the best way.",
      //   "Spouse Self Sponsor:", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes : false;

      Sponsor.SetSpouse("", isDivorce: true);
      chkShowDeactiveMembers.IsChecked = true;
    }

    private void AddMemberClick(object sender, RoutedEventArgs e)
    {
      Sponsor.AddMember();
      MatchesColumn.GridSplitterOpeningBounce(true, 300); //open the existing customers sidebar so the following message makes sense
      App.ShowUserMessage("Fill in SSN & Name first.\n" +
        "Use the \"Existing Customers\" list on the right to move existing members into this household.\n"+
        "Then use the \"Set Spouse\" button in the Members grid where applicable.");
    }

    private void MoveClientClick(object sender, RoutedEventArgs e)
    {
      var moveClientRow = ((Control)sender).Tag as DataRowView;
      if (moveClientRow == null) return;
      if (!moveClientRow.Field<bool>("Is Sponsor") ||
        MessageBox.Show("Merging Sponsors into one household is a significant event.\n\n"+
        "Are you sure??", "Merge * Sponsor *", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
      {
        Sponsor.MoveClient(moveClientRow["ClientGUID"].ToString());
      }
    }

    private void SetSponsorClick(object sender, RoutedEventArgs e)
    {
      var result = MessageBoxResult.Yes;

      if (Sponsor.HasSponsor)
      {
        result = MessageBox.Show("YES = Leave existing forms assigned to old sponsor (*RECOMMENDED*)\n\n" +
          "NO = Reassign ALL EXISTING FORMS to new Sponsor (BE CAREFUL)\n     " +
          "Only choose if the current sponsor was actually an ERROR.\n     " +
          "Represents a CHANGE of FINANCIAL RESPONSIBILITY.",
          "Changing the Sponsor of this household - Leave existing forms as is?",
          MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (result == MessageBoxResult.Cancel) return;
      }

// ReSharper disable RedundantArgumentName
      Sponsor.SetSponsor(((Control) sender).Tag.ToString(), fixExistingPackageLinks: result == MessageBoxResult.No);
// ReSharper restore RedundantArgumentName
    }

    void BtnSuspendClick(object sender, RoutedEventArgs e)
    {
      SuspensionReasonPopup.Show(); //we require a comment for either suspending or un-suspending
    }

    private void SuspensionReasonPopupResult(ReasonPopupResultEventArgs args)
    {
      if (args.OK) Sponsor.Suspend(btnSuspend.IsChecked != null && btnSuspend.IsChecked.Value ? SponsorModel.SuspendDuration.Forever : SponsorModel.SuspendDuration.Remove, args.Comments);
      else btnSuspend.IsChecked = !btnSuspend.IsChecked; //otherwise this leaves the button where it was before the user clicked it.
    }

    void BtnSuspendChecked(object sender, RoutedEventArgs e)
    {
      if (DiaryColumn.Width.Value < 350) DiaryColumn.Width = new GridLength(350); //nudge things out to where the suspension duration buttons all line up in a nice looking horizontal row
      btnSuspend.Content = "Suspended"; //for some reason changing Content property doesn't work via the same Style Trigger on this button where several other properties are successfully changed when IsChecked=true
    }

    private void BtnSuspendDurationClick(object sender, RoutedEventArgs e)
    {
      Sponsor.Suspend((string)((Button)sender).Tag);
    }

    private void BtnRemoveRemarkClick(object sender, RoutedEventArgs e)
    {
      RemoveRemarkReasonPopup.State = gridRemarks.CurrentItem as DataRowView;
      RemoveRemarkReasonPopup.Show();
    }

    private void RemoveRemarkReasonPopupResult(ReasonPopupResultEventArgs args)
    {
      if (args.OK) Sponsor.RemoveRemark(args.State as DataRowView, args.Comments);
    }


    private void CCodeSameClick(object sender, RoutedEventArgs e)
    {
      Sponsor.SetCCodesSame();
    }

    private void ClientActiveClick(object sender, RoutedEventArgs e)
    {
      var chkClientActive = sender as CheckBox;
      Debug.Assert(chkClientActive != null && chkClientActive.IsChecked != null, "chkClientActive != null");

      string message;
      bool reasonRequired;


      //yeah, kind of a crazy two-phase commit going on here to maintain separation of concerns between UI and model 
      //seems pretty well contained i guess... got any better ideas?
      if (Sponsor.CheckClientActiveRules(chkClientActive.Tag as DataRowView,
                                         chkClientActive.IsChecked.Value, out message,
                                         out reasonRequired))
        return;

      if (reasonRequired)
      {
        var reason = new ReasonPopup {Title = message, State = chkClientActive, PlacementTarget = chkClientActive};
        reason.Result += ActiveChangeReasonResult;
        reason.Show();
      }
      else App.ShowUserMessage(message);

      chkClientActive.IsChecked = !chkClientActive.IsChecked.Value;
    }

    void ActiveChangeReasonResult(ReasonPopupResultEventArgs args)
    {
      var chkClientActive = args.State as CheckBox;
      Debug.Assert(chkClientActive != null && chkClientActive.IsChecked != null, "chkClientActive != null");

      if (!args.OK) return;
      string message;
      bool reasonRequired;
      if (Sponsor.CheckClientActiveRules(chkClientActive.Tag as DataRowView, 
                                         !chkClientActive.IsChecked.Value, //gotta flip the IsChecked bool because we nixed the flip prior to coming here (in case the user wound up aborting the reason input prior to coming here)
                                         out message, out reasonRequired, args.Comments))
        chkClientActive.IsChecked = !chkClientActive.IsChecked.Value;
    }

    private void GridRemarksBeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
      string msg;
      e.Cancel = RemarkModel.DenyEdit(e.Row.Item as DataRowView, e.Column.SortMemberPath, out msg);
      e.Row.ToolTip = msg;
    }

    private void IsUTAPActiveChecked(object sender, RoutedEventArgs e)
    {
      tabsSubsystems.SelectedItem = tabUTAP;
    }

    private void DiaryAddPopupResult(ReasonPopupResultEventArgs args)
    {
      if (args.OK)
        RemarkModel.AddNew(Sponsor, Sponsor.SponsorRemarks, 0, args.Comments, null, args.IsAlert);
    }

  }

  public class Class1SellButtonToolTipConverter : WPFValueConverters.MarkupExtensionConverter, IMultiValueConverter
  {
// ReSharper disable EmptyConstructor
    public Class1SellButtonToolTipConverter() { } //to avoid an XAML annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (WPFHelpers.DesignMode) return (null);

      if (!(bool)values[0]) return("Deactive Sponsor, no new forms can be sold.");
      return (int)values[1] < 1 ? (String.Format("The maximum {0} NF1 forms have already been issued", SettingsModel.MaxClass1FormsCount)) : ("Sell New < 2500€ VAT Forms (NF1)");
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }


  public class Class2SellButtonToolTipConverter : WPFValueConverters.MarkupExtensionConverter, IMultiValueConverter
  {
// ReSharper disable EmptyConstructor
    public Class2SellButtonToolTipConverter() { } //to avoid an XAML annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (WPFHelpers.DesignMode) return (null);

      if (!(bool)values[0]) return ("Deactive Sponsor, no new forms can be sold.");
      if ((bool)values[1]) return ("There is already an NF2 outstanding.");
      return ("Sell New > 2500€ VAT Form (NF2)");
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }


  public class ShowDependentsSelectionList : WPFValueConverters.MarkupExtensionConverter, IValueConverter
  {
// ReSharper disable EmptyConstructor
    public ShowDependentsSelectionList() { } //to avoid an annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      var dependentsList = (value as IEnumerable<SponsorModel.DependentLight>);
      if (dependentsList == null) return(DependencyProperty.UnsetValue);
      var list = dependentsList.ToList();

      return ( (list.Count == 1 && !list[0].IsSpouse) || list.Count > 1 ? 
        Visibility.Visible : Visibility.Collapsed);
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class ShowSetSponsor : WPFValueConverters.MarkupExtensionConverter, IMultiValueConverter
  {
// ReSharper disable EmptyConstructor
    public ShowSetSponsor() { } //to avoid an annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      var isAdmin = System.Convert.ToBoolean(values[0]);

      var row = values[1] as DataRowView;
      Debug.Assert(row != null, "row != null");
      var sponsorGUID = row["SponsorGUID"].ToString();

      var isSponsor = System.Convert.ToBoolean(values[2]);

      return ( !isSponsor && (isAdmin || 
        !row.Row.Table.Rows.Cast<DataRow>().Any(r => r["SponsorGUID"].ToString() == sponsorGUID && r.Field<bool>("IsSponsor"))) ) 
        ? Visibility.Visible : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }


}
