using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Input;

namespace iTRAACv2.View
{
  public partial class CustomerSearch
  {
    public CustomerSearch()
    {
      InitializeComponent();

      txtSSN1.PreviewTextInput += WPFHelpers.IntegerOnlyTextBoxPreviewTextInput;
      txtSSN2.PreviewTextInput += WPFHelpers.IntegerOnlyTextBoxPreviewTextInput;
      txtSSN3.PreviewTextInput += WPFHelpers.IntegerOnlyTextBoxPreviewTextInput;

      txtDoDId.PreviewTextInput += WPFHelpers.IntegerOnlyTextBoxPreviewTextInput;

      iTRAACHelpers.WpfDataGridStandardBehavior(gridCustomerSearch);

      WPFHelpers.FocusOnVisible(txtCCode);
    }

    public delegate void OpenSponsorDelegate(string clientGUID);

    private readonly BackgroundWorkerEx<CustomerSearchArgs> _customerSearchTypeAhead = new BackgroundWorkerEx<CustomerSearchArgs>(); 

    protected override void UserControlLoaded(object sender, RoutedEventArgs e)
    {
      _customerSearchTypeAhead.OnExecute += CustomerSearchOnExecute;
      _customerSearchTypeAhead.OnCompleted += CustomerSearchOnCompleted;
    }

    private class CustomerSearchArgs
    {
      public DataTable ResultTable;
      // ReSharper disable UnassignedReadonlyField
#pragma warning disable 0649
      public readonly string Text; //put a text propertyName on this object so that the generic ExecuteDataset(label) method can populate it with any error
// ReSharper restore UnassignedReadonlyField
// ReSharper disable ConvertToConstant.Local - set via reflection
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable UnassignedField.Local
      public bool Success;
#pragma warning restore 0649
      // ReSharper restore UnassignedField.Local
// ReSharper restore FieldCanBeMadeReadOnly.Local
// ReSharper restore ConvertToConstant.Local
      public readonly object[] Values;

      public CustomerSearchArgs(params object[] values)
      {
        Values = values;
      }
    }

    private bool _customerSearchCriteriaChangeInProgress;
    private void CustomerSearchCriteriaChanged(object sender, object e)
    {
      if (lblCustomerSearchError == null) return; //comboboxes fire SelectionChanged before form has fully initialized itself

      //implement mutually exclusive text boxes... since their TextChanged event handlers all fire on eachother, this nixes the feedback loop
      if (_customerSearchCriteriaChangeInProgress) return;
      _customerSearchCriteriaChangeInProgress = true;

      try
      {
        //kill any pending search whenever the text is changed
        _customerSearchTypeAhead.Cancel();

        //blank out any previously posted search errors
        lblCustomerSearchError.Text = "";

        //implement mutually exclusive text boxes... blank out whichever ones aren't currently receiving input
        if (sender != txtLastName && sender != txtFirstName)
        { 
          txtLastName.Text = "";
          txtFirstName.Text = "";
        }
        if (sender != txtCCode) txtCCode.Text = "";

        if (
            (sender != txtSSN1) && (sender != txtSSN2) && (sender != txtSSN3)
           )
        {
          txtSSN1.Text = "";
          txtSSN2.Text = "";
          txtSSN3.Text = "";
        }

        if (sender != txtDoDId)
        {
          txtDoDId.Clear();
        }

        if (sender != txtOrderNumber) 
        {
          txtOrderNumber.Clear();
        }

        if (sender != cbxTransactionType) cbxTransactionType.SelectedIndex = -1;

        //don't even bother searching if minimum input criteria hasn't been met
        if (
          //Last Name - at least 3 chars
          (txtLastName.Text.Length < 3) &&
          //CCode - all 5 chars, since you can search by lastname or last 4 SSN via other fields
          (txtCCode.Text.Length < 5) &&
          //SSN - at least 3 out of last 4 digits
          (txtSSN3.Text.Length < 3) &&
          //DoD Id - all 10 chars
          (txtDoDId.Text.Length < 10) &&
          //Form# - at least 1 chars in addition to the rest - e.g. NF1-HD-09-____1
          (txtOrderNumber.Text.Length < 1) &&
          //if no transaction type selected
          (cbxTransactionType.SelectedIndex == -1)
        )
        {
          gridCustomerSearch.ItemsSource = null;
          return;
        }

        //(re)initiate search with new criteria
        _customerSearchTypeAhead.Initiate(new CustomerSearchArgs(
          "LastName", txtLastName.Text,
          "FirstName", txtFirstName.Text,
          "CCode", txtCCode.Text,
          "SSN", SmartFormat("%", "-", txtSSN1.Text, txtSSN2.Text, txtSSN3.Text),
          "DoDId", txtDoDId.Text,
          "OrderNumber", txtOrderNumber.Text,
          "TransactionTypeID", cbxTransactionType.SelectedValue
        ));
      }
      finally
      {
        _customerSearchCriteriaChangeInProgress = false;
      }
    }

    private string SmartFormat(string blankFiller, string delimiter, params object[] args)
    {
      var s = args.Aggregate("", (current, arg) => current + ((arg == null || arg.ToString() == "") ? ((current.Right(blankFiller.Length) == blankFiller) ? "" : blankFiller) : arg + delimiter));
      return (s.Left(s.Length - delimiter.Length));
    }

    //this executes _off_ the UI thread
    static void CustomerSearchOnExecute(CustomerSearchArgs state)
    {
// ReSharper disable InconsistentNaming
      using (var Customer_Search = new Proc("Customer_Search"))
// ReSharper restore InconsistentNaming
        state.ResultTable = Customer_Search.AssignValues(state.Values).ExecuteDataSet(state).Tables[0];
    }

    void CustomerSearchOnCompleted(CustomerSearchArgs state)
    {
      if (state.Success)
      {
        gridCustomerSearch.ItemsSource = state.ResultTable.DefaultView;

        //if (multiple) client search results correspond to the same sponsor, then automatically open that household tab
        var sponsorGUIDs = state.ResultTable.AsEnumerable().GroupBy(r => r["SponsorGUID"]).Select(g => g.Key.ToString());
// ReSharper disable PossibleMultipleEnumeration
        if (sponsorGUIDs.Count() == 1) RoutedCommands.OpenSponsor.Execute(sponsorGUIDs.First(), null); //for user convenience, automatically fire up the sponsor page if we only got one hit
// ReSharper restore PossibleMultipleEnumeration
      }
      else
        lblCustomerSearchError.Text = state.Text;
    }

    private void TxtSSNTextChanged(object sender, TextChangedEventArgs e)
    {
      WPFHelpers.AutoTabTextBoxTextChanged(sender, e);
      CustomerSearchCriteriaChanged(sender, e);
    }

    private void RegisterNewSponsorClick(object sender, RoutedEventArgs e)
    {
      RoutedCommands.OpenSponsor.Execute(Guid.Empty.ToString(), null);
    }

    private void TxtDoDIdTextChanged(object sender, TextChangedEventArgs e)
    {

    }

  }

}
