using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace iTRAACv2
{
  public partial class tabNewCustWiz : ucBase
  {
    public tabNewCustWiz()
    {
      InitializeComponent();

#if DEBUG
      btnTestFill.Visibility = System.Windows.Visibility.Visible;
#endif

      Fields = new DataFields();

      iTRAACHelpers.WPFDataGrid_Standard_Behavior(gridMatches);
      gridMatches.ItemsSource = PotentialMatches.DefaultView;
      InitializePotentialMatchesGridGrouping();

      using (Proc Ranks_s = new Proc("Ranks_s")) cbxRank.ItemsSource = Ranks_s.ExecuteDataTable().DefaultView;

      txtSponsorSSN1.Focus();
    }

    protected override void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      Fields.IsValidationEnabled = true; //nugget: avoid validating all fields & lighting up the initial screen with errors before the user has even had a chance to enter anything 
    }

    private DataFields Fields { get { return (DataContext as DataFields); } set { DataContext = value; } }

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

    private DataTable PotentialMatches = new DataTable();

    private void gridMatches_LoadingRow(object sender, DataGridRowEventArgs e)
    {
      e.Row.Header = (e.Row.GetIndex() + 1).ToString(); //show the record index in the rowheader of every row, just for aesthetics
    }

    private void ClearPreviousMatches(string MatchType)
    {
      if (PotentialMatches.Rows.Count == 0) return;

      //wipe out existing matches in a group, because each batch should be considered an entirely new list of hits
      using (DataView v = new DataView(PotentialMatches))
      {
        v.RowFilter = "MatchType = '" + MatchType + "'";
        v.ClearRows();
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
        //nugget: took me a minute to realize what was happening here... 
        //nugget: had to move the using(Proc) here into the background thread scope since it would dispose of objects when it fell out of scope in the outer context
        using (proc)
        {
          //this delegate body executes on a background thread
          DataTable results = proc.ExecuteDataTable();
          //and then we use our friend Mr. Dispatch to get us back to the UI thread for displaying the results
          Dispatcher.Invoke((Action)delegate() { MergeNewMatches(results); }, null);
        }
      }).Start();
    }

    private void txtSponsorSSN3_LostFocus(object sender, RoutedEventArgs e)
    {
      Proc Sponsor_New_SearchSSN = new Proc("Sponsor_New_SearchSSN");
      Sponsor_New_SearchSSN["@SSN1"] = Fields.SponsorSSN1;
      Sponsor_New_SearchSSN["@SSN2"] = Fields.SponsorSSN2;
      Sponsor_New_SearchSSN["@SSN3"] = Fields.SponsorSSN3;
      SearchProc("Sponsor SSN", Sponsor_New_SearchSSN);
    }

    private void txtSpouseSSN3_LostFocus(object sender, RoutedEventArgs e)
    {
      Proc Sponsor_New_SearchSSN = new Proc("Sponsor_New_SearchSSN");
      Sponsor_New_SearchSSN["@SSN1"] = Fields.SpouseSSN1;
      Sponsor_New_SearchSSN["@SSN2"] = Fields.SpouseSSN2;
      Sponsor_New_SearchSSN["@SSN3"] = Fields.SpouseSSN3;
      SearchProc("Spouse SSN", Sponsor_New_SearchSSN);
    }

    private void SponsorLastName_LostFocus(object sender, RoutedEventArgs e)
    {
      Proc Sponsor_New_SearchName = new Proc("Sponsor_New_SearchName");
      Sponsor_New_SearchName["@FirstName"] = Fields.SponsorFirstName;
      Sponsor_New_SearchName["@LastName"] = Fields.SponsorLastName;
      SearchProc("Sponsor Name", Sponsor_New_SearchName);
    }

    private void SpouseLastName_LostFocus(object sender, RoutedEventArgs e)
    {
      Proc Sponsor_New_SearchName = new Proc("Sponsor_New_SearchName");
      Sponsor_New_SearchName["@FirstName"] = Fields.SpouseFirstName;
      Sponsor_New_SearchName["@LastName"] = Fields.SpouseLastName;
      SearchProc("Spouse Name", Sponsor_New_SearchName);
    }


    private void SaveNewSponsor_Click(object sender, RoutedEventArgs e)
    {
      if (gridMatches.Items.Count > 0 &&
        MessageBox.Show("Have you verified that none of the existing\nentries to the right correspond to this customer?",
          "Create New Sponsor", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No) return;


      if (!Fields.ValidateAll())
      {
        MessageBox.Show("Please enter all required fields before submitting.",
          "Create New Sponsor", MessageBoxButton.OK, MessageBoxImage.Hand);
        return;
      }

      using (Proc Sponsor_New = new Proc("Sponsor_New"))
      {
        Sponsor_New["@SponsorSSN"] = Fields.SponsorSSN;
        Sponsor_New["@SponsorLastName"] = Fields.SponsorLastName;
        Sponsor_New["@SponsorFirstName"] = Fields.SponsorFirstName;
        Sponsor_New["@SponsorMI"] = Fields.SponsorMI;
        Sponsor_New["@SponsorSuffix"] = Fields.SponsorSuffix;
        Sponsor_New["@SponsorEmail"] = Fields.SponsorEmail;

        Sponsor_New["@SpouseSSN"] = Fields.SpouseSSN;
        Sponsor_New["@SpouseLastName"] = Fields.SpouseLastName;
        Sponsor_New["@SpouseFirstName"] = Fields.SpouseFirstName;
        Sponsor_New["@SpouseMI"] = Fields.SpouseMI;
        Sponsor_New["@SpouseSuffix"] = Fields.SpouseSuffix;
        Sponsor_New["@SpouseEmail"] = Fields.SpouseEmail;

        Sponsor_New["@DutyPhone"] = Fields.DutyPhone;
        Sponsor_New["@DutyLocation"] = Fields.DutyLocation;
        Sponsor_New["@RankCode"] = Fields.RankCode;
        Sponsor_New["@DEROS"] = Fields.DEROS;
        Sponsor_New["@PersonalPhoneCountry"] = Fields.PersonalPhoneCountry;
        Sponsor_New["@PersonalPhoneNumber"] = Fields.PersonalPhoneNumber;
        Sponsor_New["@OfficialMailLine1"] = Fields.OfficialMailLine1;
        Sponsor_New["@OfficialMailCMR"] = Fields.OfficialMailCMR;
        Sponsor_New["@OfficialMailBox"] = Fields.OfficialMailBox;
        Sponsor_New["@OfficialMailCity"] = Fields.OfficialMailCity;
        Sponsor_New["@OfficialMailState"] = Fields.OfficialMailState;
        Sponsor_New["@OfficialMailZip"] = Fields.OfficialMailZip;
        Sponsor_New["@HomeStreet"] = Fields.HostAddrStreet;
        Sponsor_New["@HomeNumber"] = Fields.HostAddrNumber;
        Sponsor_New["@HomeCity"] = Fields.HostAddrCity;
        Sponsor_New["@HomePostal"] = Fields.HostAddrPostal;

        if (Sponsor_New.ExecuteNonQuery(BusinessBase.ShowUserMessage, "New Sponsor: ", true))
        {
          Fields = new DataFields();
          RoutedCommands.OpenSponsor.Execute(Sponsor_New["@SponsorGUID"], null);
        }
      }
    }

    private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      //can't assign static event handlers in XAML so make a little hop here
      WPFHelpers.IntegerOnlyTextBox_PreviewTextInput(sender, e);
    }

    private void ClearAllFields_Click(object sender, RoutedEventArgs e)
    {
      Fields = new DataFields();
      PotentialMatches.Clear();
    }

    private void TestFill_Click(object sender, RoutedEventArgs e)
    {
      using(Proc Sponsor_New_ClearTest = new Proc("Sponsor_New_ClearTest")) Sponsor_New_ClearTest.ExecuteNonQuery();

      Fields.SponsorSSN1 = "347";
      Fields.SponsorSSN2 = "50";
      Fields.SponsorSSN3 = "7543";
      Fields.SponsorFirstName = "Jim";
      Fields.SponsorLastName = "Baker";
      Fields.SponsorEmail = "***test***";
  
      Fields.SpouseSSN1 = "121";
      Fields.SpouseSSN2 = "23";
      Fields.SpouseSSN3 = "3434";
      Fields.SpouseFirstName = "Tammy";
      Fields.SpouseLastName = "Baker";
      Fields.SpouseMI = "F";
      Fields.SpouseEmail = "***test***";

      Fields.DutyPhoneDSN1 = "370";
      Fields.DutyPhoneDSN2 = "6585";
      Fields.DutyLocation = "EUCOM J63";
  
      Fields.RankCode = "CIV";
      Fields.DEROS = "2011-08-31";
      Fields.PersonalPhoneCountry = "49";
      Fields.PersonalPhoneNumber = "62213388573";
      Fields.OfficialMailCMR = "419";
      Fields.OfficialMailBox = "1472";
      Fields.OfficialMailCity = "APO";
      Fields.OfficialMailState = "AE";
      Fields.OfficialMailZip = "09102";

      Fields.HostAddrStreet = "Max-Joseph-Str.";
      Fields.HostAddrNumber = "48";
      Fields.HostAddrCity = "Heidelberg";
      Fields.HostAddrPostal = "69126";
    }
  }


  /* 
   * Alright so this here is obviously Object Model layer stuff right in the GUI code behind
   * I didn't bother to throw it in another class simply because it's so specific to this one screen and it was more convenient to develop/debug here all together
   * if that situation changes, it'll be easy enough to toss it in another .CS file and make it properly reusable (very doubtful, but you never know :)
   */

  //nugget: if you're implementing INotifyPropertyChanged, you have to remember to declare your GUI {Binding}'s with Mode=TwoWay!!!
  //nugget: otherwise your Validation adornments won't show up because the GUI isn't listening to your PropertyChanged events
  public class DataFields : INotifyPropertyChanged, IDataErrorInfo
  {
    public string SponsorSSN { get { return(String.Format("{0}-{1}-{2}", SponsorSSN1, SponsorSSN2, SponsorSSN3)); } }
    [MinStringLength(3)]
    public string SponsorSSN1 { get { return (_SponsorSSN1); } set { StringSetter(ref _SponsorSSN1, value, "SponsorSSN1"); } } private string _SponsorSSN1 = null;
    [MinStringLength(2)]
    public string SponsorSSN2 { get { return (_SponsorSSN2); } set { StringSetter(ref _SponsorSSN2, value, "SponsorSSN2"); } } private string _SponsorSSN2 = null;
    [MinStringLength(4)]
    public string SponsorSSN3 { get { return (_SponsorSSN3); } set { StringSetter(ref _SponsorSSN3, value, "SponsorSSN3"); } } private string _SponsorSSN3 = null;
    [Required]
    public string SponsorLastName { get { return (_SponsorLastName); } set { StringSetter(ref _SponsorLastName, value, "SponsorLastName"); } } private string _SponsorLastName = null;
    [Required]
    public string SponsorFirstName { get { return (_SponsorFirstName); } set { StringSetter(ref _SponsorFirstName, value, "SponsorFirstName"); } } private string _SponsorFirstName = null;
    public string SponsorMI { get { return (_SponsorMI); } set { StringSetter(ref _SponsorMI, value, "SponsorMI"); } } private string _SponsorMI = null;
    public string SponsorSuffix { get { return (_SponsorSuffix); } set { StringSetter(ref _SponsorSuffix, value, "SponsorSuffix"); } } private string _SponsorSuffix = null;
    [Required]
    public string SponsorEmail { get { return (_SponsorEmail); } set { StringSetter(ref _SponsorEmail, value, "SponsorEmail"); } } private string _SponsorEmail = null;

    public bool NoSpouse { get; set; }
    public string SpouseSSN { get { return (String.Format("{0}-{1}-{2}", SpouseSSN1, SpouseSSN2, SpouseSSN3)); } }
    [MinStringLength(3)]
    public string SpouseSSN1 { get { return (_SpouseSSN1); } set { StringSetter(ref _SpouseSSN1, value, "SpouseSSN1"); } } private string _SpouseSSN1 = null;
    [MinStringLength(2)]
    public string SpouseSSN2 { get { return (_SpouseSSN2); } set { StringSetter(ref _SpouseSSN2, value, "SpouseSSN2"); } } private string _SpouseSSN2 = null;
    [MinStringLength(4)]
    public string SpouseSSN3 { get { return (_SpouseSSN3); } set { StringSetter(ref _SpouseSSN3, value, "SpouseSSN3"); } } private string _SpouseSSN3 = null;
    [Required]
    public string SpouseLastName { get { return (_SpouseLastName); } set { StringSetter(ref _SpouseLastName, value, "SpouseLastName"); } } private string _SpouseLastName = null;
    [Required]
    public string SpouseFirstName { get { return (_SpouseFirstName); } set { StringSetter(ref _SpouseFirstName, value, "SpouseFirstName"); } } private string _SpouseFirstName = null;
    public string SpouseMI { get { return (_SpouseMI); } set { StringSetter(ref _SpouseMI, value, "SpouseMI"); } } private string _SpouseMI = null;
    public string SpouseSuffix { get { return (_SpouseSuffix); } set { StringSetter(ref _SpouseSuffix, value, "SpouseSuffix"); } } private string _SpouseSuffix = null;
    public string SpouseEmail { get { return (_SpouseEmail); } set { StringSetter(ref _SpouseEmail, value, "SpouseEmail"); } } private string _SpouseEmail = null;

    public string DutyPhone { get { return (String.Format("{0}-{1}", DutyPhoneDSN1, DutyPhoneDSN2)); } }
    [MinStringLength(3)]
    public string DutyPhoneDSN1 { get { return (_DutyPhoneDSN1); } set { StringSetter(ref _DutyPhoneDSN1, value, "DutyPhoneDSN1"); } } private string _DutyPhoneDSN1 = null;
    [MinStringLength(4)]
    public string DutyPhoneDSN2 { get { return (_DutyPhoneDSN2); } set { StringSetter(ref _DutyPhoneDSN2, value, "DutyPhoneDSN2"); } } private string _DutyPhoneDSN2 = null;

    [Required]
    public string DutyLocation { get { return (_DutyLocation); } set { StringSetter(ref _DutyLocation, value, "DutyLocation"); } } private string _DutyLocation = null;
    [Required]
    public string RankCode { get { return (_RankCode); } set { StringSetter(ref _RankCode, value, "RankCode"); } } private string _RankCode = null;
    [Required]
    public string DEROS { get { return (_DEROS); } set { StringSetter(ref _DEROS, value, "DEROS"); } } private string _DEROS = null;

    [Required]
    public string PersonalPhoneCountry { get { return (_PersonalPhoneCountry); } set { StringSetter(ref _PersonalPhoneCountry, value, "PersonalPhoneCountry"); } } private string _PersonalPhoneCountry = null;
    [Required]
    public string PersonalPhoneNumber { get { return (_PersonalPhoneNumber); } set { StringSetter(ref _PersonalPhoneNumber, value, "PersonalPhoneNumber"); } } private string _PersonalPhoneNumber = null;

    public string OfficialMailLine1 { get { return (_OfficialMailLine1); } set { StringSetter(ref _OfficialMailLine1, value, "OfficialMailLine1"); } } private string _OfficialMailLine1 = null;
    [Required]
    public string OfficialMailCMR { get { return (_OfficialMailCMR); } set { StringSetter(ref _OfficialMailCMR, value, "OfficialMailCMR"); } } private string _OfficialMailCMR = null;
    [Required]
    public string OfficialMailBox { get { return (_OfficialMailBox); } set { StringSetter(ref _OfficialMailBox, value, "OfficialMailBox"); } } private string _OfficialMailBox = null;
    [Required]
    public string OfficialMailCity { get { return (_OfficialMailCity); } set { StringSetter(ref _OfficialMailCity, value, "OfficialMailCity"); } } private string _OfficialMailCity = "APO";
    [Required]
    public string OfficialMailState { get { return (_OfficialMailState); } set { StringSetter(ref _OfficialMailState, value, "OfficialMailState"); } } private string _OfficialMailState = "AE";
    [Required]
    public string OfficialMailZip { get { return (_OfficialMailZip); } set { StringSetter(ref _OfficialMailZip, value, "OfficialMailZip"); } } private string _OfficialMailZip = "09";

    [Required]
    public string HostAddrStreet { get { return (_HostAddrStreet); } set { StringSetter(ref _HostAddrStreet, value, "HostAddrStreet"); } } private string _HostAddrStreet = null;
    [Required]
    public string HostAddrNumber { get { return (_HostAddrNumber); } set { StringSetter(ref _HostAddrNumber, value, "HostAddrNumber"); } } private string _HostAddrNumber = null;
    [Required]
    public string HostAddrCity { get { return (_HostAddrCity); } set { StringSetter(ref _HostAddrCity, value, "HostAddrCity"); } } private string _HostAddrCity = null;
    [Required]
    public string HostAddrPostal { get { return (_HostAddrPostal); } set { StringSetter(ref _HostAddrPostal, value, "HostAddrPostal"); } } private string _HostAddrPostal = null;


    public bool IsValidationEnabled = false; //probably ought to default this to true in a generic base class

    private void StringSetter(ref string oldVal, string newVal, string PropertyName)
    {
      if (oldVal != newVal)
      {
        oldVal = newVal;
        ValidateProperty(PropertyName);
      }
    }

    private Dictionary<string, string> PropertyErrors = new Dictionary<string, string>();
    public void ValidateProperty(string PropertyName)
    {
      if (!IsValidationEnabled) return; //nugget: simplistic approach to avoid validating all fields & lighting up the initial screen with errors before the user has even had a chance to enter anything 

      PropertyDescriptor prop = TypeDescriptor.GetProperties(this)[PropertyName]; //an annoyance of GetType().GetProperty("") is that PropertyInfo.Attributes doesn't lend itself to the .OfType<>() extension method
      //you'd have to .Cast<>() which means a loop which would invalidate the reason to use GetProperty vs TypeDescriptor.GetProperties() in the first place

      var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
      var vc = new ValidationContext(this, null, null) { MemberName = PropertyName };
      var isValid = Validator.TryValidateProperty(prop.GetValue(this), vc, results);

      string errors = results.Select(x => x.ErrorMessage).Join("\n");
      if (errors.Length == 0) PropertyErrors.Remove(prop.Name);
      else PropertyErrors[prop.Name] = errors;

      if (PropertyChanged != null)
        PropertyChanged(this, new PropertyChangedEventArgs(PropertyName));
    }
    public event PropertyChangedEventHandler PropertyChanged;

    public bool ValidateAll()
    {
      //just fire the OnSetter validation for each field and the PropertyChanged event called therein will drive the GUI to come back and check whether a field needs error highlighting via IDataErrorInfo this[string columnName]
      foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(this))
      {
        if (GetType().GetMethod("set_" + prop.Name) == null) continue; //only check properties with setters
        ValidateProperty(prop.Name);
      }
      return (PropertyErrors.Count == 0);
    }

    public string Error { get { throw new NotImplementedException(); } }
    //putting all the validation complexity in the Setter Aspect renders the IDataErrorInfo interface completely trivial...
    public string this[string columnName] { get { return (PropertyErrors.GetValueOrDefault(columnName)); } }

  }



}
