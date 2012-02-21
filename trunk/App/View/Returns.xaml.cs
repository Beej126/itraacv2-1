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
using System.Data;

namespace iTRAACv2
{
  public partial class Returns : UserControl
  {
    public Returns()
    {
      InitializeComponent();

      iTRAACHelpers.WPFDataGrid_Standard_Behavior(gridReturns);
    }

    private void btnSearch_Click(object sender, RoutedEventArgs e)
    {
      txtSequenceNumber.SelectAll();
      gridReturns.ItemsSource = null; //for visual consistency, blank out the existing list before we go off and search
      using(Proc TaxForm_search = new Proc("TaxForm_search"))
      {
        TaxForm_search["@SequenceNumber"] = txtSequenceNumber.Text;
        gridReturns.ItemsSource = TaxForm_search.ExecuteDataSet().Table0.DefaultView;
        FilterChanged();
      }

      //automatically open the only one found if it is only one
      if (gridReturns.Items.Count == 1)
        RoutedCommands.OpenTaxForm.Execute((gridReturns.Items[0] as DataRowView)["TaxFormGUID"], null);
    }

    public bool FilterReturned
    {
      get { return (bool)GetValue(FilterReturnedProperty); }
      set { SetValue(FilterReturnedProperty, value); }
    }
    public static readonly DependencyProperty FilterReturnedProperty =
      DependencyProperty.Register("FilterReturned", typeof(bool), typeof(Returns),
        new UIPropertyMetadata(true, (obj, args) => { (obj as Returns).FilterChanged(); }));

    public bool FilterFiled
    {
      get { return (bool)GetValue(FilterFiledProperty); }
      set { SetValue(FilterFiledProperty, value); }
    }
    public static readonly DependencyProperty FilterFiledProperty =
      DependencyProperty.Register("FilterFiled", typeof(bool), typeof(Returns),
        new UIPropertyMetadata(false, (obj, args) => { (obj as Returns).FilterChanged(); }));

    public bool FilterLocalOffice
    {
      get { return (bool)GetValue(FilterLocalOfficeProperty); }
      set { SetValue(FilterLocalOfficeProperty, value); }
    }
    public static readonly DependencyProperty FilterLocalOfficeProperty =
      DependencyProperty.Register("FilterLocalOffice", typeof(bool), typeof(Returns),
        new UIPropertyMetadata(true, (obj, args) => { (obj as Returns).FilterChanged(); }));

    
    protected void FilterChanged()
    {
      DataView dv = (gridReturns.ItemsSource as DataView);
      if (dv == null) return;
      dv.RowFilter = SqlClientHelpers.BuildRowFilter(
        FilterReturned ? "Status not in ('Returned', 'Filed')" : null,
        FilterFiled ? "Status <> 'Filed'" : null, 
        FilterLocalOffice ? "TaxOfficeId = " + SettingsModel.TaxOfficeId.ToString() : null
      );
    }
  }
}
