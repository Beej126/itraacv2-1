using System;
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

namespace iTRAACv2
{
  public partial class tabSponsor : tabBase
  {
    public SponsorModel sponsor { get { return (model as SponsorModel); } }

    /// <summary>
    /// </summary>
    /// <param name="tabControl">TabControl to place this new TabItem under</param>
    /// <param name="SponsorGUID"></param>
    static public void Open(TabControl tabControl, string SponsorGUID)
    {
      OpenTab<tabSponsor>(tabControl, ModelBase.Lookup<SponsorModel>(SponsorGUID));
    }

    public tabSponsor() 
    {
      InitializeComponent();

      iTRAACHelpers.WPFDataGrid_Standard_Behavior(gridForms);
      iTRAACHelpers.WPFDataGrid_Standard_Behavior(gridRemarks);
      iTRAACHelpers.WPFDataGrid_Standard_Behavior(gridMatches);

      btnSuspend.Click += btnSuspend_Click;
    }

    private System.Windows.Threading.DispatcherFrame DisableRemarkAlertReasonPopup_DispatcherFrame;
    void sponsor_ReasonConfirmation(object sender, ReasonConfirmationArgs e)
    {
      DisableRemarkAlertReasonPopup.Show();

      //nugget: idea from here: http://www.deanchalk.me.uk/post/WPF-Modal-Controls-Via-Dispatcher (Army firewall blocks this site)
      //nugget: this is really cool because it maintains the synchronous nature of this call stack, returning the result to the model layer after the psuedo modal popup, even though the popup is actually acting on it's own asynch event handler!!
      DisableRemarkAlertReasonPopup_DispatcherFrame = new System.Windows.Threading.DispatcherFrame(); //nugget:
      System.Windows.Threading.Dispatcher.PushFrame(DisableRemarkAlertReasonPopup_DispatcherFrame); //nugget: blocks gui message pump & createst nested pump, making this a blocking call 

      e.Accept = DisableRemarkAlertReasonPopup.IsOK;
      e.Reason = DisableRemarkAlertReasonPopup.ReasonText;
    }

    private void DisableRemarkAlertReasonPopup_Result(ReasonPopupResultEventArgs args)
    {
      DisableRemarkAlertReasonPopup_DispatcherFrame.Continue = false; //nugget: cancels the nested pump which allows the code after the PushFrame above to continue
    }

    protected override void OnClosed()
    {
      sponsor.PropertyChanged -= sponsor_PropertyChanged;
      btnSuspend.Click -= btnSuspend_Click;
    }

    private void gridRemarks_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

      TextBlock lblRemarks = remarksCol.GetCellContent(gridRemarks.CurrentItem) as TextBlock;  //this basically works but any kind of grid sorting must virtualize this info since it always returns null after that

      Hyperlink dummy;
      if (lblRemarks.TryFindChild<Hyperlink>(out dummy)) return; //if this column is already hyperlinked, bail out

      string original = lblRemarks.Text;
      MatchCollection matches = OrderNumberRegEx.Matches(original);
      if (matches.Count == 0) return;

      lblRemarks.Inlines.Clear();
      string removestr = "";
      foreach(Match match in matches)
      {
        Hyperlink link = new Hyperlink();
        link.Command = RoutedCommands.OpenTaxForm;
        link.CommandParameter = match.Groups[2].Value; //this is the OrderNumber... and then the magic of RoutedCommands makes it elegant to fire open the order tab
        link.Inlines.Add(match.Groups[2].Value);
        lblRemarks.Inlines.Add(match.Groups[1].Value);
        lblRemarks.Inlines.Add(link);
        removestr += match.Value;
      }
      lblRemarks.Inlines.Add(original.Replace(removestr, "")); //tack on any text following the last OrderNumber match

    }
    static private Regex OrderNumberRegEx = new Regex(SettingsModel.Global["OrderNumberRegEx"], RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    protected override void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      sponsor.ReasonConfirmation += new EventHandler<ReasonConfirmationArgs>(sponsor_ReasonConfirmation);

      InitializePotentialMatchesGridGrouping();

      WPFHelpers.GridSort(gridForms, "Purchased", System.ComponentModel.ListSortDirection.Descending);
      WPFHelpers.GridSort(gridRemarks, new string[] { "Alert", "LastUpdate" }, new ListSortDirection[] { ListSortDirection.Descending, ListSortDirection.Descending });

      sponsor.Transactions.CollectionChanged += (s, a) => { if (a.NewItems != null) TransactionsDataGrid.ScrollIntoView(a.NewItems[0]); }; //nugget: AutoScroll DataGrid
      rdoHideReturnedTaxForms.IsChecked = true;

      if (sponsor.TaxForms_CountReturnedNotFiled > 0) 
        ReturnedNotFiledLabel.Background = ReturnedNotFiledLabel.Background.BeginBrushColorAnimation(Colors.Red);

      if (sponsor.Class1TaxForms_CountUnreturned > SettingsModel.MaxClass1FormsCount)
        UnreturnedLabel.Background = UnreturnedLabel.Background.BeginBrushColorAnimation(Colors.Red);

      sponsor.PropertyChanged += sponsor_PropertyChanged;

      gridMembers.ItemContainerGenerator.ContainerFromIndex(0).FindChild<TextBox>(c => c.Name == "txtSSN1").Focus();
    }

    #region Potential Matches stuff

    void sponsor_PropertyChanged(object sender, PropertyChangedEventArgs e)
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
      gridMatches.ItemsSource = sponsor.PotentialClientMatches;

      //if we weren't already popped open, do the bounce
      if (sponsor.PotentialClientMatches.Count > 0)
        WPFHelpers.GridSplitterOpeningBounce(MatchesColumn, true, 300);
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

      if (gridMatches.Items.GroupDescriptions.Count == 0)
        gridMatches.Items.GroupDescriptions.Add(new PropertyGroupDescription("MatchType"));

      //*** wound up simply moving this sort to the underlying  sponsor.PotentialClientMatches DataView.Sort
      //sorting by increasing recordcount allows us to put the most specific and therefore hopefully the "best" matches up top
      //if (gridMatches.Items.SortDescriptions.Count == 0) //nugget: this gets whacked when you reassign the ItemSource!! :(
      //  gridMatches.Items.SortDescriptions.Add(new SortDescription("RecordCountID", ListSortDirection.Ascending));
    }

    #endregion

    private void btnCart_IsCheckedChanged(object sender, RoutedEventArgs e)
    {
      WPFHelpers.GridSplitterOpeningBounce(ShoppingCartColumn, btnCart.IsChecked, 275);
    }

    private void btnNewNF1Package_Click(object sender, RoutedEventArgs e)
    {
      imgNewFormsCount.BeginStoryboard((Storyboard)Application.Current.FindResource("PulsingOpacity"));

      //fill combo with values up to how many new forms are allowed max... this much UI code is probably asking to be in a ViewModel
      cbxNewFormsCount.Items.Clear();
      for (int i = sponsor.Class1TaxForms_CountRemainingToBuy; i > 0; i--) cbxNewFormsCount.Items.Add(i);
      cbxNewFormsCount.SelectedIndex = 0; //default to max remaining
      cbxNewFormsCount.IsDropDownOpen = (cbxNewFormsCount.Items.Count > 1); //be nice and auto-open the drop down if there is actually a choice to be made

      IsClass2Popup = false;
      popPrintNF1s.IsOpen = true;

      cbxNewFormsCount.Focus();
    }

    private void btnReadyToPrint_No_Click(object sender, RoutedEventArgs e)
    {
      popPrintNF1s.IsOpen = false;
    }

    public bool IsClass2Popup
    {
      get { return (bool)GetValue(IsClass2PopupProperty); }
      set { SetValue(IsClass2PopupProperty, value); }
    }
    public static readonly DependencyProperty IsClass2PopupProperty =
        DependencyProperty.Register("IsClass2Popup", typeof(bool), typeof(tabSponsor), new UIPropertyMetadata(false));

    private void btnReadyToPrint_Click(object sender, RoutedEventArgs e)
    {
      //sanity check: if there are already printed forms in the shopping cart, everything else must be printed during this session... too complicated to manage otherwise
      if (sponsor.Transactions.Any(t => !t.IsPending))
      {
        MessageBox.Show("Since forms have already been printed.\rAll subsequent forms must also be printed.\rPlease select 'Print Immediately' only.");
        return;
      }

      //this is sort of embarrasingly old school validation...
      //these days the "proper" ViewModel way would be to putt an "IsSelected" property on a model collection and then binding that to an ItemsControl which generates the radio button group
      //(this would have to be implemented as an on-the-fly linq query wrapped around TaxForm.Type.UserSelectableClass1 which tacked on "IsSelected" as a new property via linq projection syntax)
      //then put validation logic on that collection enforcing that one item has to have IsSelected==true
      if (!rdoEF1.IsChecked.Value && !rdoNF1.IsChecked.Value)
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
      TaxFormModel.TaxFormPackage pkg = new TaxFormModel.TaxFormPackage(
        ParentTransactionList: sponsor.Transactions,
        IsPending: sender == btnReadyToPrint_Cart,
        SponsorGUID: sponsor.GUID, 
        AuthorizedDependentClientGUID: (string)lbxPrintDependents.SelectedValue,
        FormType: TaxFormModel.FormType.NF1,
        Qty: Convert.ToInt32(cbxNewFormsCount.SelectedValue) );

      btnCart.IsChecked = true; //for user convenience, show cart to remind clerk to get money from customer
    }

    private void btnNewNF2_Click(object sender, RoutedEventArgs e)
    {
      //if spouse is the only choice, don't bother showing selection
      if (PanelDependentsPicklist.Visibility == System.Windows.Visibility.Visible)
      {
        IsClass2Popup = true;
        popPrintNF1s.IsOpen = true;
      }
      else
      {
        btnNewNF2_OK_Click(null, null);
      }
    }

    private void btnNewNF2_OK_Click(object sender, RoutedEventArgs e)
    {
      SponsorModel.DependentLight authdep = lbxPrintDependents.SelectedItem as SponsorModel.DependentLight;

      TaxFormModel newForm = TaxFormModel.NewNF2(
        sponsor.Transactions,
        sponsor.GUID,
        string.Format("{0}, {1} ({2})", sponsor.HouseMembers[0]["LName"], sponsor.HouseMembers[0]["FName"], sponsor.HouseMembers[0]["CCode"]),
        (string)lbxPrintDependents.SelectedValue,
        (authdep != null)?authdep.FirstName:null);

      RoutedCommands.OpenTaxForm.Execute(newForm.GUID, null);

      btnCart.IsChecked = true; //for user convenience, show cart to remind clerk to get money from customer
    }

    //set spouse as default authorized dependent because they are guaranteed privileges
    private void lbxPrintDependents_Loaded(object sender, RoutedEventArgs e)
    {
      for(int i=0; i < lbxPrintDependents.Items.Count; i++)
        if (((SponsorModel.DependentLight)lbxPrintDependents.Items[i]).IsSpouse)
        {
          lbxPrintDependents.SelectedIndex = i;
          break;
        }
    }

    private void EndBackgroundAnimation(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      (sender as Border).Background.EndBrushColorAnimation();
    }

    private void btnSave_Click(object sender, RoutedEventArgs e)
    {
      sponsor.Save();
    }

    private void btnSaveClose_Click(object sender, RoutedEventArgs e)
    {
      if (sponsor.SaveUnload()) Close();
    }

    private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      //can't assign *static* event handlers in XAML so make that little hop here
      WPFHelpers.IntegerOnlyTextBox_PreviewTextInput(sender, e);
    }

    private void SetSpouse_Click(object sender, RoutedEventArgs e)
    {
      string NewSpouseClientGUID = Convert.ToString((sender as Control).Tag);
      if (NewSpouseClientGUID == "" || !sponsor.HouseMembers.Cast<DataRowView>().Any(r => r.Field<bool>("IsSpouse"))
        || MessageBox.Show("Spouse already defined.  Are you sure you want to override?", "Set Spouse", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        sponsor.SetSpouse(NewSpouseClientGUID);
    }

    private void Divorce_Click(object sender, RoutedEventArgs e)
    {
      if (MessageBox.Show("Has Sponsor provided FINALIZED DIVORCE paperwork?\n\n" +
        "Note: Legal \"Separation\" is NOT sufficient.",
        "Divorce:", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

      //bool dome = (sponsor.HasSpouse) ? MessageBox.Show("Will Spouse now become a new Sponsor?\n\n" +
      //   "Note: Tax Relief privileges must be confirmed *before* hitting Yes.\nOfficial Orders are the best way.",
      //   "Spouse Self Sponsor:", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes : false;

      sponsor.SetSpouse("", IsDivorce: true);
      chkShowDeactiveMembers.IsChecked = true;
    }

    private void AddMember_Click(object sender, RoutedEventArgs e)
    {
      sponsor.AddMember();
      WPFHelpers.GridSplitterOpeningBounce(MatchesColumn, true, 300); //open the existing customers sidebar so the following message makes sense
      App.ShowUserMessage("Fill in SSN & Name first.\n" +
        "Use the \"Existing Customers\" list on the right to move existing members into this household.\n"+
        "Then use the \"Set Spouse\" button in the Members grid where applicable.");
    }

    private void MoveClient_Click(object sender, RoutedEventArgs e)
    {
      DataRowView MoveClientRow = ((Control)sender).Tag as DataRowView;
      if (!MoveClientRow.Field<bool>("Is Sponsor") ||
        MessageBox.Show("Merging Sponsors into one household is a significant event.\n\n"+
        "Are you sure??", "Merge * Sponsor *", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
      {
        sponsor.MoveClient(MoveClientRow["ClientGUID"].ToString());
      }
    }

    private void SetSponsor_Click(object sender, RoutedEventArgs e)
    {
      MessageBoxResult result = MessageBoxResult.Yes;

      if (sponsor.HasSponsor)
      {
        result = MessageBox.Show("YES = Leave existing forms assigned to old sponsor (*RECOMMENDED*)\n\n" +
          "NO = Reassign ALL EXISTING FORMS to new Sponsor (BE CAREFUL)\n     " +
          "Only choose if the current sponsor was actually an ERROR.\n     " +
          "Represents a CHANGE of FINANCIAL RESPONSIBILITY.",
          "Changing the Sponsor of this household - Leave existing forms as is?",
          MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (result == MessageBoxResult.Cancel) return;
      }

      sponsor.SetSponsor((sender as Control).Tag.ToString(), FixExistingPackageLinks: result == MessageBoxResult.No);
    }

    void btnSuspend_Click(object sender, RoutedEventArgs e)
    {
      SuspensionReasonPopup.Show(); //we require a comment for either suspending or un-suspending
    }

    private void SuspensionReasonPopup_Result(ReasonPopupResultEventArgs args)
    {
      if (args.OK) sponsor.Suspend(btnSuspend.IsChecked.Value ? SponsorModel.SuspendDuration.Forever : SponsorModel.SuspendDuration.Remove, args.Comments);
      else btnSuspend.IsChecked = !btnSuspend.IsChecked; //otherwise this leaves the button where it was before the user clicked it.
    }

    void btnSuspend_Checked(object sender, RoutedEventArgs e)
    {
      if (DiaryColumn.Width.Value < 350) DiaryColumn.Width = new GridLength(350); //nudge things out to where the suspension duration buttons all line up in a nice looking horizontal row
      btnSuspend.Content = "Suspended"; //for some reason changing Content property doesn't work via the same Style Trigger on this button where several other properties are successfully changed when IsChecked=true
    }

    private void btnSuspendDuration_Click(object sender, RoutedEventArgs e)
    {
      sponsor.Suspend((string)((Button)sender).Tag);
    }

    private void btnRemoveRemark_Click(object sender, RoutedEventArgs e)
    {
      RemoveRemarkReasonPopup.State = gridRemarks.CurrentItem as DataRowView;
      RemoveRemarkReasonPopup.Show();
    }

    private void RemoveRemarkReasonPopup_Result(ReasonPopupResultEventArgs args)
    {
      if (args.OK) sponsor.RemoveRemark(args.State as DataRowView, args.Comments);
    }


    private void CCodeSame_Click(object sender, RoutedEventArgs e)
    {
      sponsor.SetCCodesSame();
    }

    private void ClientActive_Click(object sender, RoutedEventArgs e)
    {
      CheckBox chkClientActive = sender as CheckBox;
      string Message;
      bool ReasonRequired;

      //yeah, kind of a crazy two-phase commit going on here to maintain separation of concerns between UI and model 
      //seems pretty well contained i guess... got any better ideas?
      if (!sponsor.CheckClientActiveRules(chkClientActive.Tag as DataRowView,
              chkClientActive.IsChecked.Value, out Message, out ReasonRequired))
      {
        if (ReasonRequired)
        {
          ReasonPopup reason = new ReasonPopup();
          reason.Title = Message;
          reason.State = chkClientActive;
          reason.PlacementTarget = chkClientActive;
          reason.Result += ActiveChange_Reason_Result;
          reason.Show();
        }
        else App.ShowUserMessage(Message);

        chkClientActive.IsChecked = !chkClientActive.IsChecked.Value;
      }
    }

    void ActiveChange_Reason_Result(ReasonPopupResultEventArgs args)
    {
      CheckBox chkClientActive = args.State as CheckBox;
      string Message;
      bool ReasonRequired;

      if (args.OK)
      {
        if (sponsor.CheckClientActiveRules(chkClientActive.Tag as DataRowView, 
              !chkClientActive.IsChecked.Value, //gotta flip the IsChecked bool because we nixed the flip prior to coming here (in case the user wound up aborting the reason input prior to coming here)
              out Message, out ReasonRequired, args.Comments))
          chkClientActive.IsChecked = !chkClientActive.IsChecked.Value; 
      }
    }

    private void gridRemarks_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
      string msg;
      e.Cancel = RemarkModel.DenyEdit(e.Row.Item as DataRowView, e.Column.SortMemberPath, out msg);
      e.Row.ToolTip = msg;
    }

    private void IsUTAPActive_Checked(object sender, RoutedEventArgs e)
    {
      tabsSubsystems.SelectedItem = tabUTAP;
    }

    private void DiaryAddPopup_Result(ReasonPopupResultEventArgs args)
    {
      if (args.OK)
        RemarkModel.AddNew(sponsor, sponsor.SponsorRemarks, 0, args.Comments, null, args.IsAlert);
    }

  }

  public class Class1SellButtonToolTipConverter : WPFValueConverters.MarkupExtensionConverter, IMultiValueConverter
  {
    public Class1SellButtonToolTipConverter() { } //to avoid an XAML annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection bug.

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (WPFHelpers.DesignMode) return (null);

      if (!(bool)values[0]) return("Deactive Sponsor, no new forms can be sold.");
      else if ((int)values[1] < 1) return(String.Format("The maximum {0} NF1 forms have already been issued", SettingsModel.MaxClass1FormsCount));
      else return("Sell New < 2500€ VAT Forms (NF1)");
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }


  public class Class2SellButtonToolTipConverter : WPFValueConverters.MarkupExtensionConverter, IMultiValueConverter
  {
    public Class2SellButtonToolTipConverter() { } //to avoid an XAML annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection bug.

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (WPFHelpers.DesignMode) return (null);

      if (!(bool)values[0]) return ("Deactive Sponsor, no new forms can be sold.");
      else if ((bool)values[1]) return (String.Format("There is already an NF2 outstanding.", SettingsModel.MaxClass1FormsCount));
      else return ("Sell New > 2500€ VAT Form (NF2)");
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }


  public class ShowDependentsSelectionList : WPFValueConverters.MarkupExtensionConverter, IValueConverter
  {
    public ShowDependentsSelectionList() { } //to avoid an annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection bug.

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      IEnumerable<SponsorModel.DependentLight> DependentsList = (value as IEnumerable<SponsorModel.DependentLight>);
      if (DependentsList == null) return(DependencyProperty.UnsetValue);
      List<SponsorModel.DependentLight> list = DependentsList.ToList();

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
    public ShowSetSponsor() { } //to avoid an annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection bug.

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      bool IsAdmin = System.Convert.ToBoolean(values[0]);

      DataRowView row = values[1] as DataRowView;
      string SponsorGUID = row["SponsorGUID"].ToString();

      bool IsSponsor = System.Convert.ToBoolean(values[2]);

      return ( !IsSponsor && (IsAdmin || 
        !row.Row.Table.Rows.Cast<DataRow>().Any(r => r["SponsorGUID"].ToString() == SponsorGUID && r.Field<bool>("IsSponsor"))) ) 
        ? Visibility.Visible : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }


}
