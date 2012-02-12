using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Linq;

namespace iTRAACv2
{
  public partial class CustomerSearch : ucBase
  {
    public CustomerSearch()
    {
      InitializeComponent();

      txtSSN1.PreviewTextInput += WPFHelpers.IntegerOnlyTextBox_PreviewTextInput;
      txtSSN2.PreviewTextInput += WPFHelpers.IntegerOnlyTextBox_PreviewTextInput;
      txtSSN3.PreviewTextInput += WPFHelpers.IntegerOnlyTextBox_PreviewTextInput;

      txtDoDId.PreviewTextInput += WPFHelpers.IntegerOnlyTextBox_PreviewTextInput;

      iTRAACHelpers.WPFDataGrid_Standard_Behavior(gridCustomerSearch);
    }

    public delegate void OpenSponsorDelegate(string ClientGUID);

    private BackgroundWorkerEx<CustomerSearchArgs> CustomerSearchTypeAhead = new BackgroundWorkerEx<CustomerSearchArgs>(); 

    protected override void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      txtCCode.Focus();
      CustomerSearchTypeAhead.OnExecute += new BackgroundWorkerEx<CustomerSearchArgs>.BackgroundWorkerExCallback(CustomerSearchOnExecute);
      CustomerSearchTypeAhead.OnCompleted += new BackgroundWorkerEx<CustomerSearchArgs>.BackgroundWorkerExCallback(CustomerSearchOnCompleted);
    }

    private class CustomerSearchArgs
    {
      public DataTable resultTable = null;
      public string Text = null; //put a text propertyName on this object so that the generic ExecuteDataset(label) method can populate it with any error
      public bool Success = false;
      public object[] values = null;

      public CustomerSearchArgs(params object[] Values)
      {
        values = Values;
      }
    }

    private bool CustomerSearchCriteriaChangeInProgress = false;
    private void CustomerSearchCriteriaChanged(object sender, object e)
    {
      if (lblCustomerSearchError == null) return; //comboboxes fire SelectionChanged before form has fully initialized itself

      //implement mutually exclusive text boxes... since their TextChanged event handlers all fire on eachother, this nixes the feedback loop
      if (CustomerSearchCriteriaChangeInProgress) return;
      CustomerSearchCriteriaChangeInProgress = true;

      try
      {
        //kill any pending search whenever the text is changed
        CustomerSearchTypeAhead.Cancel();

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
        CustomerSearchTypeAhead.Initiate(new CustomerSearchArgs(
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
        CustomerSearchCriteriaChangeInProgress = false;
      }
    }

    private string SmartFormat(string BlankFiller, string Delimiter, params object[] args)
    {
      string s = "";
      foreach(object arg in args)
      {
        s += ((arg == null || arg.ToString() == "") ? ((s.Right(BlankFiller.Length) == BlankFiller) ? "" : BlankFiller) : arg + Delimiter);
      }
      return (s.Left(s.Length - Delimiter.Length));
    }

    //this executes _off_ the UI thread
    void CustomerSearchOnExecute(CustomerSearchArgs state)
    {
      using (Proc Customer_Search = new Proc("Customer_Search"))
        state.resultTable = Customer_Search.AssignValues(state.values).ExecuteDataSet(state).Tables[0];
    }

    void CustomerSearchOnCompleted(CustomerSearchArgs state)
    {
      if (state.Success)
      {
        gridCustomerSearch.ItemsSource = state.resultTable.DefaultView;

        //if (multiple) client search results correspond to the same sponsor, then automatically open that household tab
        var SponsorGUIDs = state.resultTable.AsEnumerable().GroupBy(r => r["SponsorGUID"]).Select(g => g.Key.ToString());
        if (SponsorGUIDs.Count() == 1) RoutedCommands.OpenSponsor.Execute(SponsorGUIDs.First(), null); //for user convenience, automatically fire up the sponsor page if we only got one hit
      }
      else
        lblCustomerSearchError.Text = state.Text;
    }

    private void txtSSN_TextChanged(object sender, TextChangedEventArgs e)
    {
      WPFHelpers.AutoTabTextBox_TextChanged(sender, e);
      CustomerSearchCriteriaChanged(sender, e);
    }

    private void RegisterNewSponsor_Click(object sender, RoutedEventArgs e)
    {
      RoutedCommands.OpenSponsor.Execute(Guid.Empty.ToString(), null);
    }

    private void txtDoDId_TextChanged(object sender, TextChangedEventArgs e)
    {

    }

  }

}
