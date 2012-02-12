using System;
using System.Data;
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
using System.Windows.Shapes;

namespace iTRAACv2
{
  /// <summary>
  /// </summary>
  public partial class frmVendorSearch : Window
  {
    private frmVendorSearch()
    {
      InitializeComponent();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="vendor"></param>
    /// <returns>True = vendor was selected, False = dialog cancelled</returns>
    static public bool ShowModal(Control parentCtrl, out VendorSelection vendor)
    {
      vendor = null;

      frmVendorSearch frm = new frmVendorSearch();
      frm.Owner = Window.GetWindow(parentCtrl);
      //frm.Top = parentCtrl.hor
      frm.ShowDialog();
      
      if (!frm.DialogResult.Value) return ( false );

      DataRowView row = frm.grdVendorList.SelectedItem as DataRowView;
      vendor = new VendorSelection() {
        VendorName = row["ShortDescription"],
        VendorID = row["VendorID"]
      };
      return (true);
    }

    private BackgroundWorkerEx<VendorSearchArgs> VendorSearchTypeAhead = new BackgroundWorkerEx<VendorSearchArgs>(500); //half a second seems responsive enough but not too heavy on the server

    private void VendorPicklist_Loaded(object sender, RoutedEventArgs e)
    {
      txtVendorName.Focus();

      VendorSearchTypeAhead.OnExecute += new BackgroundWorkerEx<VendorSearchArgs>.BackgroundWorkerExCallback(VendorSearchOnExecute);
      VendorSearchTypeAhead.OnCompleted += new BackgroundWorkerEx<VendorSearchArgs>.BackgroundWorkerExCallback(VendorSearchOnCompleted);
    }

    private class VendorSearchArgs
    {
      public System.Data.DataTable resultTable = null;
      public string Text = null; //put a text property on this object so that the generic ExecuteDataset(label) method can populate it with any error
      public bool Success = false;
      public string[] values = null;

      public VendorSearchArgs(params string[] Values)
      {
        values = Values;
      }
    }

    private void VendorSearchCriteriaChanged(object sender, object e)
    {
      //the comboboxes fire their change event before the form is initialized
      if (lblVendorSearchError == null) return;

      //implement mutually exclusive text boxes... since their TextChanged event handlers all fire on eachother, this nixes the feedback loop
      //kill any pending search whenever the text is changed
      VendorSearchTypeAhead.Cancel();

      //blank out any previously posted search errors
      lblVendorSearchError.Text = "";

      //don't even bother searching if minimum input criteria hasn't been met
      if (
        //VendorName at least 3 chars
        (txtVendorName.Text.Length < 3)
      )
      {
        grdVendorList.ItemsSource = null;
        return;
      }

      //(re)initiate search with new criteria
     VendorSearchTypeAhead.Initiate(new VendorSearchArgs(
        "VendorName", txtVendorName.Text,
        "VendorName_SearchType", (cbxVendorNameSearchType.SelectedItem as ComboBoxItem).Content.ToString(),
        "VendorCity", txtVendorCity.Text,
        "VendorCity_SearchType", (cbxVendorCitySearchType.SelectedItem as ComboBoxItem).Content.ToString()
      ), true);
    }

    //this happens off the UI thread
    void VendorSearchOnExecute(VendorSearchArgs state)
    {
      using (Proc Vendor_Search = new Proc("Vendor_Search"))
      {
        Vendor_Search.AssignValues(state.values);
        state.resultTable = Vendor_Search.ExecuteDataTable(state);
      }
    }

    void VendorSearchOnCompleted(VendorSearchArgs state)
    {
      if (state.Success)
      {
        grdVendorList.ItemsSource = state.resultTable.DefaultView;
        //grdVendorList.Columns[grdVendorList.Columns.Count - 1].Visibility = Visibility.Hidden; //hide ShortDescription
        //grdVendorList.Columns[grdVendorList.Columns.Count - 2].Visibility = Visibility.Hidden; //hide VendorID
      }
      else
        lblVendorSearchError.Text = state.Text;
    }

    private void btnSelect_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = true;
      Close();
    }

  }

  public class VendorSelection
  {
    public object VendorName = DBNull.Value;
    public object VendorID = DBNull.Value;
  }

}
