using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using Microsoft.Windows.Controls;
using System.Linq;

namespace iTRAACv2
{
  public partial class tabCustomer : tabBase
  {
    static public Delegates.OpenTaxFormTab OpenTaxFormTab = null;

    static public void Open(string ClientGUID)
    {
      Sponsor sponsor = Sponsor.Lookup(ClientGUID);
      NewTab(sponsor, typeof(tabCustomer));
    }

    public tabCustomer() 
    {
      InitializeComponent();

      WPFHelpers.WPFDataGrid_Standard_Behavior(gridDependents);
      WPFHelpers.WPFDataGrid_Standard_Behavior(gridForms);
      gridDependents.AutoGeneratingColumn += new System.EventHandler<DataGridAutoGeneratingColumnEventArgs>(iTRAACHelpers.CommonGridColumns);
      gridForms.Loaded += new RoutedEventHandler(gridForms_Loaded);
    }

    void gridForms_Loaded(object sender, RoutedEventArgs e)
    {
      WPFHelpers.GridSort(gridForms, "Purchased", System.ComponentModel.ListSortDirection.Descending); 
    }

    void TaxForm_Click(object sender, RoutedEventArgs e)
    {
      OpenTaxFormTab((e.OriginalSource as System.Windows.Documents.Hyperlink).CommandParameter.ToString());
    }

    private void gridForms_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      TaxFormDetails.DataContext = gridForms.SelectedItem;
    }

  }
}
