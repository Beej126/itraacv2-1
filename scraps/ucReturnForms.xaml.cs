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
  public partial class ucReturnForms : BrentAnderson.WPF.ucBase
  {
    static public OpenTaxFormDelgate OpenTaxForm = null;

    public ucReturnForms()
    {
      InitializeComponent();
      iTRAACHelpers.WPFDataGrid_Standard_Behavior(gridReturnForms);
    }

    private void btnReturnForm_Click(object sender, RoutedEventArgs e)
    {
      using (Proc TaxForm_Return = new Proc("TaxForm_Return"))
      {
        TaxForm_Return["@OrderNumber"] = txtOrderNumber.Text;
        TaxForm_Return["@UserGUID"] = User.Current.GUID;
        if (!TaxForm_Return.ExecuteNonQuery(App.ShowSystemMessage, "", false)) return;

        bool Success = (bool)TaxForm_Return["@Success"];

        //if we simply didn't get a hit on the supplied OrderNumber, then show that via generic message popup
        if (!Success && (TaxForm_Return["@CustomerName"].ToString() == ""))
          App.ShowSystemMessage("Tax Form #: " + txtOrderNumber.Text + " - " + TaxForm_Return["@Message"].ToString());

        // otherwise add a corresponding row to the returns grid as an aesthetically pleasing list of recent history
        else 
        {
          ReturnForms.Add(
            TaxForm_Return["@TaxFormGUID"].ToString(),
            TaxForm_Return["@OrderNumber"].ToString(),
            TaxForm_Return["@CustomerName"].ToString(),
            TaxForm_Return["@Message"].ToString(),
            Success
          );
          txtOrderNumber.Clear();
          txtOrderNumber.Focus();

          //if return committed successfully and autofile is checked, open the taxform edit screen to encourage the user to close out with a full file while we're they're at it
          if (Success && chkAutoFile.IsChecked.Value && OpenTaxForm != null) OpenTaxForm(TaxForm_Return["@TaxFormGUID"].ToString());
        }
      }
    }

  }
}
