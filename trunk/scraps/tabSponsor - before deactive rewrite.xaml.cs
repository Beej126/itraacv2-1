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
using System.Threading;
using System.Windows.Documents;
using System.Text.RegularExpressions;

namespace iTRAACv2
{
  public partial class tabSponsor : tabBusinessBase
  {
    public Sponsor SponsorBO { get { return (bo as Sponsor); } }

    protected override bool OnClosing()
    {
      if (SponsorBO.Transactions.HasPending)
      {
        MessageBoxResult mb = MessageBox.Show("*DISCARD* all pending transactions?\r\n(un-printed forms, etc.)", 
          "Close", System.Windows.MessageBoxButton.YesNo);
        if (mb == MessageBoxResult.Yes) SponsorBO.Transactions.Clear();
        else return (false);
      }

      return (base.OnClosing());
    }

    /// <summary>
    /// </summary>
    /// <param name="tabControl">TabControl to place this new TabItem under</param>
    /// <param name="SponsorGUID"></param>
    static public void Open(TabControl tabControl, string SponsorGUID)
    {
      OpenTab<tabSponsor>(tabControl, BusinessBase.Lookup<Sponsor>(SponsorGUID));
    }

    public tabSponsor() 
    {
      InitializeComponent();

      using (Proc Ranks_s = new Proc("Ranks_s")) cbxRank.ItemsSource = Ranks_s.ExecuteDataSet().Table0.DefaultView;

      iTRAACHelpers.WPFDataGrid_Standard_Behavior(gridMembers);
      iTRAACHelpers.WPFDataGrid_Standard_Behavior(gridForms);
      iTRAACHelpers.WPFDataGrid_Standard_Behavior(gridRemarks);
      iTRAACHelpers.WPFDataGrid_Standard_Behavior(gridMatches);
      gridMatches.ItemsSource = PotentialMatches.DefaultView;
      InitializePotentialMatchesGridGrouping();
      gridMembers.Loaded += new RoutedEventHandler(gridMembers_Loaded);

      btnSuspend.Click += new RoutedEventHandler(btnSuspend_Click);
    }

    static private Regex OrderNumberRegEx = new Regex(Setting.Global["OrderNumberRegEx"], RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    void gridRemarks_HyperLinkOrderNumbers()
    {
      //nugget: hyperlink any Form# matches in the Remarks - nice! unforunately any column sorting applied after this blows away the hyperlinks so i'll have to come back later and figure out a workaround
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
      foreach (object r in gridRemarks.Items)
      {
        //this did work but then when i started using WPFHelpders.GridSort logic it would always return null: 
        TextBlock lblRemarks = remarksCol.GetCellContent(r) as TextBlock;
        string original = lblRemarks.Text;
        MatchCollection matches = OrderNumberRegEx.Matches(original);
        if (matches.Count == 0) continue;

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
    }

    protected override void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      base.UserControl_Loaded(sender, e);

      //nugget: see here: http://social.msdn.microsoft.com/forums/en-US/wpf/thread/63974f4f-d9ee-45af-8499-42f29cbc22ae?prof=required
      //nugget: unfortunately any kind of code driven or user driven sorting blows away custom DataGridColumn.GetCellContent() modifications like my initial hyperlinking approach... i'll have to come back to this
      //gridRemarks_HyperLinkOrderNumbers(); 

      WPFHelpers.GridSort(gridForms, "Purchased", System.ComponentModel.ListSortDirection.Descending);
      WPFHelpers.GridSort(gridRemarks, new string[] { "Alert", "LastUpdate" }, new ListSortDirection[] { ListSortDirection.Descending, ListSortDirection.Descending });

      SponsorBO.Transactions.CollectionChanged += (s, a) => { if (a.NewItems != null) TransactionsDataGrid.ScrollIntoView(a.NewItems[0]); }; //nugget: AutoScroll DataGrid
      rdoHideReturnedTaxForms.IsChecked = true;

      if (SponsorBO.TaxForms_CountReturnedNotFiled > 0) 
        ReturnedNotFiledLabel.Background = ReturnedNotFiledLabel.Background.BeginBrushColorAnimation(Colors.Red);

      //btnSuspend_Checked(null, null); //just to jiggle the 
    }

    void gridMembers_Loaded(object sender, RoutedEventArgs e)
    {
      if (!(bool)SponsorBO.Members[0]["IsReadOnly"]) SelectLastMemberRow();
    }

    private void SelectLastMemberRow()
    {
      Dispatcher.Invoke((Action)delegate() //nugget: critical for FindChild() to have a fully initialized visual tree to look at... it was unbelievably annoying to figure this one out!!!
      {
        int RowNumber = gridMembers.Items.Count - 1;
        //default focus to SSN field on a brand new Sponsor
        gridMembers.SelectedIndex = RowNumber;
        gridMembers.CurrentCell = gridMembers.SelectedCells[2];
        gridMembers.BeginEdit();
        gridMembers.GetCell(RowNumber, gridMembers.Columns.IndexOf(gridMembers.CurrentCell.Column)).FindChild<TextBox>().Focus();
      }, System.Windows.Threading.DispatcherPriority.Render); //nugget: through trial and error i found that Dispatcher.Invoke is required (versus async BeginInvoke) and DispatcherPriority.Render is the highest priority we can get away with, DispatcherPriority.DataBind (8) and higher would always fail
    }

    #region Potential Matches stuff

    private DataTable PotentialMatches = new DataTable();

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

      gridMatches.Items.GroupDescriptions.Add(new PropertyGroupDescription("MatchType"));

      //returning the record count for every row is an annoying waste of bandwidth but I haven't been able to find a more optimal way yet and there shouldn't be that many rows matching on SSN anyway
      //sorting by increasing recordcount allows us to put the most specific and therefore hopefully the "best" matches up top
      gridMatches.Items.SortDescriptions.Add(new SortDescription("RecordCountID", ListSortDirection.Ascending));
    }

    private void ClearPreviousMatches(string MatchType)
    {
      if (PotentialMatches.Rows.Count == 0) return;

      //clear out any existing match rows of the same match type, because each batch should be considered an entirely new list of hits
      using (DataView v = new DataView(PotentialMatches))
      {
        v.RowFilter = "MatchType = '" + MatchType + "'";
        v.DetachRowsAndDispose();
      }
    }

    private void MergeNewMatches(DataTable NewMatches)
    {
      PotentialMatches.Merge(NewMatches);
      splitterMatches.Visibility = groupMatches.Visibility = (PotentialMatches.Rows.Count > 0) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
      if (PotentialMatches.Rows.Count > 0 && MatchesColumn.ActualWidth == 0.0) //just for aesthetics, don't muck with the matches window width if it's already been opened for displaying previous matches
        WPFHelpers.GridSplitterOpeningBounce(MatchesColumn, 300, true);
    }

    private void SearchProc(string MatchType, Proc proc)
    {
      ClearPreviousMatches(MatchType);
      proc["@MatchType"] = MatchType;
      BackgroundProc(proc);
    }

    private void BackgroundProc(Proc proc)
    {
      //throw the search proc call on a background thread so the user doesn't experience any delay in entering the rest of the fields
      new Thread(delegate()
      {
        //this delegate body executes on a background thread...

        //nugget: took me a minute to realize what was happening here... 
        //nugget: had to move the using(Proc) here into the background thread scope since it would dispose when it fell out of scope in the outer context
        //        and then the proc parms would be in a bad state (i.e. null) when the background thread started executing and trying to send those parm values up to the server
        using (proc)
        {
          DataTable results = proc.ExecuteDataSet().Table0;
          //and then we use our friend Mr. Dispatch to get us back to the UI thread for displaying the results
          Dispatcher.Invoke((Action)delegate() { MergeNewMatches(results); }, null);
        }
      }).Start();
    }


    private void txtSSN3_LostFocus(object sender, RoutedEventArgs e)
    {
      DataRowView currentrow = gridMembers.CurrentItem as DataRowView;
      if (!currentrow.Row.IsFieldsModified("SSN1", "SSN2", "SSN3")) return; //could just check 

      Proc Sponsor_New_SearchSSN = new Proc("Sponsor_New_SearchSSN");
      Sponsor_New_SearchSSN["@SSN1"] = currentrow["SSN1"];
      Sponsor_New_SearchSSN["@SSN2"] = currentrow["SSN2"];
      Sponsor_New_SearchSSN["@SSN3"] = currentrow["SSN3"];
      SearchProc("Matched on " + MemberRowType(currentrow) + "SSN", Sponsor_New_SearchSSN);
    }

    private string MemberRowType(DataRowView row)
    {
      return (
        (Convert.ToBoolean(row["IsSponsor"]) ? "Sponsor " : "") +
        (Convert.ToBoolean(row["IsSpouse"]) ? "Spouse " : "")
      );
    }

    private void txtLastName_LostFocus(object sender, RoutedEventArgs e)
    {
      DataRowView currentrow = gridMembers.CurrentItem as DataRowView;
      if (!currentrow.Row.IsFieldsModified("FName", "LName")) return;

      Proc Sponsor_New_SearchName = new Proc("Sponsor_New_SearchName");
      Sponsor_New_SearchName["@FirstName"] = currentrow["FName"];
      Sponsor_New_SearchName["@LastName"] = currentrow["LName"];
      SearchProc("Matched on " + MemberRowType(currentrow) + "Name", Sponsor_New_SearchName);
    }

    #endregion

    private void btnCart_IsCheckedChanged(object sender, RoutedEventArgs e)
    {
      WPFHelpers.GridSplitterOpeningBounce(ShoppingCartColumn, 275, btnCart.IsChecked);
    }

    private void btnNewNF1Package_Click(object sender, RoutedEventArgs e)
    {
      imgNewFormsCount.BeginStoryboard((Storyboard)Application.Current.FindResource("PulsingOpacity"));

      //TODO: grey out the "sell new forms" button if count is already maxed out, with tooltip that says why its grey (always)
      //remember: the NF1 10 count now includes *incomplete* count
      //TODO: incomplete-override button... fires reason code popup... need a generic reason code popup
      //TODO: add "x overrides in y days" to server side "data cop" background scans... parameterize the rules as much as possible... parameter for who to notify (program manager)

      //fill combo with values up to how many new forms are allowed max... this much UI code is probably asking to be in a ViewModel
      cbxNewFormsCount.Items.Clear();
      for (int i = SponsorBO.Class1TaxForms_CountRemainingToBuy; i > 0; i--) cbxNewFormsCount.Items.Add(i);
      cbxNewFormsCount.SelectedIndex = 0; //default to max remaining
      cbxNewFormsCount.IsDropDownOpen = (cbxNewFormsCount.Items.Count > 1); //be nice and auto-open the drop down if there is actually a choice to be made

      popPrintNF1s.IsOpen = true;

      cbxNewFormsCount.Focus();
    }

    private void btnReadyToPrint_No_Click(object sender, RoutedEventArgs e)
    {
      popPrintNF1s.IsOpen = false;
    }

    private void btnReadyToPrint_Click(object sender, RoutedEventArgs e)
    {
      //this is sort of embarrasingly old school validation...
      //these days the "proper" ViewModel way would be to putt an "IsSelected" property on a business collection and then binding that to an ItemsControl which generates the radio button group
      //(this would have to be implemented as an on-the-fly linq query wrapped around TaxForm.Type.UserSelectableClass1 which tacked on "IsSelected" as a new property via linq projection syntax)
      //then put validation logic on that collection enforcing that one item has to have IsSelected==true
      //but i'm just lazy enough to not go do all of that right now... i'm fighting MVVM... it's been good to finally have generated some solid exmples to see for myself where it could actually provide some design-elegance type value
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
      //it seems like Proc could simply be handed back from the BizObj to the UI as a typical "data transfer object" (DTO) pattern
      //e.g. public Proc SponsorBO.SetupNewPackage(...) {... return(proc); }

      App.ShowWaitAnimation();

      //while we're still executing on the UI thread where UI property access is allowed,
      //pull all values from UI fields and assign them to a TaxFormPackage "DTO" (Data Transfer Object)
      //this facilitates doing the DB work on a background thread... see next block of code
      TaxForm.TaxFormPackage pkg = new TaxForm.TaxFormPackage(
          Pending: (sender == btnReadyToPrint_Cart),
          SponsorGUID: SponsorBO.GUID,
          SoldToClientGUID: (string)lbxDependents.SelectedValue,
          FormType: TaxForm.Type.List[rdoNF1.IsChecked.Value ? TaxForm.FormType.NF1.ToString() : TaxForm.FormType.EF1.ToString()],
          Qty: Convert.ToInt32(cbxNewFormsCount.SelectedValue)
      );

      //nugget: most concise pattern i've found to pop off a background thread for some "heavy lifting", and update the UI after it completes
      //nugget: implemented as an anonymous ThreadStart delegate which executes the work and then Dispatcher.Invokes another anonymous method
      //nugget: the anonymous methods allow us to keep the code all here together in one place for easier understanding rather than spreading it out into multiple concreate method signatures
      //nugget: winds up with a pretty light dusting of thread support code over what still looks like pretty traditional code structure... which is nice for readability

      //it works by launching a delegate method context which:
      // - performs the work on a new thread 
      // - and then once the work has finished, calls back to the UI thread for visual updates via Dispatch 

      //reason for going to the trouble of the background thread is that inserting these new records can take a bit of work at the DB level...
      //so i wanted to throw up a visually distracting "throbber" in the meantime (http://en.wikipedia.org/wiki/Throbber) ...
      //and if we didn't background thread the DB work, it would block the UI from animating the throbber
      new System.Threading.Thread(delegate()
      {
        try
        {
          pkg.Execute(); //hit the DB to create all the new Package/Forms records

          //then do the UI oriented updates back on the proper UI thread via our friend Mr. Dispatch
          Dispatcher.Invoke((Action)delegate()
          {
            App.StopWaitAnimation(); //the global default exception handler will call this as well... so i'm avoiding a Finally block just to keep the code a little shorter
            SponsorBO.LoadNewTaxFormPackage(pkg); //nugget: ObservableCollections are locked to the UI thread - good to know
            btnCart.IsChecked = true; //for user convenience, show cart to remind clerk to get money from customer
            rdoShowUnPrintedTaxFormsOnly.IsChecked = true; //for user convenience, filter the list of tax forms down to these recently created new ones

          }, null);
        }
        catch (Exception ex) //this exception handler must be included as part of this pattern wherever else it's implemented
        {
          Dispatcher.Invoke((Action)delegate(){ throw ex; }, null); //toss any exceptions over to the main UI thread, per MSDN direction: http://msdn.microsoft.com/en-us/library/system.windows.application.dispatcherunhandledexception.aspx
        }
      }).Start();

    }

    private void btnDiaryAdd_OK_Click(object sender, RoutedEventArgs e)
    {
      //Proc 
      //txtDiaryAdd.Text
    }

    private void ReturnedNotFiledLabel_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      ReturnedNotFiledLabel.Background.EndBrushColorAnimation();
    }

    private void btnSave_Click(object sender, RoutedEventArgs e)
    {
      SponsorBO.Save();
    }

    private void btnSaveClose_Click(object sender, RoutedEventArgs e)
    {
      if (SponsorBO.SaveUnload()) Close();
    }

    private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      //can't assign *static* event handlers in XAML so make that little hop here
      WPFHelpers.IntegerOnlyTextBox_PreviewTextInput(sender, e);
    }

    private void gridMembers_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
      //business rule: disable edits to the first/last name fields when we're not creating a brand new client record...
      //  names are printed on forms and therefore we have to be more careful about changing that data from an audit/paper-trail standpoint
      //  there are more explicit change buttons which walk the user through the more elaborate process for doing that
      if ( Convert.ToBoolean((e.Row.Item as DataRowView)["IsReadonly"])
        && (e.Column.SortMemberPath == "FName" || e.Column.SortMemberPath == "LName")) e.Cancel = true;
    }

    private void AddMember_Click(object sender, RoutedEventArgs e)
    {
      SponsorBO.AddMember();
      SelectLastMemberRow();
    }

    private void btnDiaryAdd_Click(object sender, RoutedEventArgs e)
    {

    }

    private void SetSpouse_Click(object sender, RoutedEventArgs e)
    {
      foreach (DataRowView member in SponsorBO.Members) if ((bool)member["IsSpouse"] == true) member["IsSpouse"] = false;
      ((DataRowView)gridMembers.CurrentItem)["IsSpouse"] = true;
    }

    void btnSuspend_Click(object sender, RoutedEventArgs e)
    {
      SuspensionReasonPopup.Show(); //we require a comment for either suspending or un-suspending
    }

    private void SuspensionReasonPopup_Result(bool OK, string RemarkTypeId, string Remarks)
    {
      if (OK) SponsorBO.Suspend(btnSuspend.IsChecked.Value ? Sponsor.SuspendDuration.Forever : Sponsor.SuspendDuration.Remove, Remarks);
      else btnSuspend.IsChecked = !btnSuspend.IsChecked; //otherwise this leaves the button where it was before the user clicked it.
    }

    void btnSuspend_Checked(object sender, RoutedEventArgs e)
    {
      if (DiaryColumn.Width.Value < 350) DiaryColumn.Width = new GridLength(350); //nudge things out to where the suspension duration buttons all line up in a nice looking horizontal row
      btnSuspend.Content = "Suspended"; //for some reason changing Content property doesn't work via the same Style Trigger on this button where several other properties are successfully changed when IsChecked=true
    }

    private void btnSuspendDuration_Click(object sender, RoutedEventArgs e)
    {
      SponsorBO.Suspend((string)((Button)sender).Tag);
    }

    private void btnRemoveRemark_Click(object sender, RoutedEventArgs e)
    {
      SponsorBO.RemoveRemark(gridRemarks.CurrentItem as DataRowView);
    }

  }

  public class SuspensionIsEnabled: WPFValueConverters.MarkupExtensionConverter, IValueConverter
  {
    public SuspensionIsEnabled() { } //to avoid an XAML annoying warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection bug.

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (WPFHelpers.DesignMode) return (true);
      Sponsor SponsorBO = value as Sponsor;
      return (SponsorBO.Fields["SuspensionExpiry"].ToString() == "" || (int)SponsorBO.Fields["SuspensionTaxOfficeId"] == Setting.TaxOfficeId);
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class Class1SellButtonToolTipConverter : WPFValueConverters.MarkupExtensionConverter, IMultiValueConverter
  {
    public Class1SellButtonToolTipConverter() { } //to avoid an XAML annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection bug.

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (WPFHelpers.DesignMode) return (null);

      if (!(bool)values[0]) return("Deactive Sponsor, no new forms can be sold.");
      else if ((int)values[1] < 1) return(String.Format("The maximum {0} NF1/EF1 forms have already been issued", Setting.MaxClass1FormsCount));
      else return("Sell New < 2500€ VAT Forms (NF1/EF1)");
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
      else if ((bool)values[1]) return (String.Format("There is already an NF2/EF2 outstanding.", Setting.MaxClass1FormsCount));
      else return ("Sell New > 2500€ VAT Form (NF2/EF2)");
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }


}
