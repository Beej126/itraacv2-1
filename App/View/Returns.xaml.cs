using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using iTRAACv2.Model;

namespace iTRAACv2.View
{
  public partial class Returns
  {
    public Returns()
    {
      InitializeComponent();

      iTRAACHelpers.WpfDataGridStandardBehavior(gridReturns);
    }

    private void BtnSearchClick(object sender, RoutedEventArgs e)
    {
      txtSequenceNumber.SelectAll();
      gridReturns.ItemsSource = null; //for visual consistency, blank out the existing list before we go off and search
// ReSharper disable InconsistentNaming
      using (var TaxForm_Returns_Search = new Proc("TaxForm_Returns_Search"))
// ReSharper restore InconsistentNaming
      {
        TaxForm_Returns_Search["@SequenceNumber"] = txtSequenceNumber.Text;
        gridReturns.ItemsSource = TaxForm_Returns_Search.ExecuteDataSet().Table0.DefaultView;
        FilterChanged();
      }

      //automatically open the only one found if it is only one
      if (gridReturns.Items.Count == 1) OpenForm(gridReturns.Items[0] as DataRowView);
      else if (gridReturns.Items.Count > 1) gridReturns.GetCell(0, 0).Focus();
    }

    private void GridReturnsKeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Return)
      {
        e.Handled = true;
        OpenForm(gridReturns.SelectedItem as DataRowView);
      }
    }

    private void OpenForm(DataRowView row)
    {
      RoutedCommands.OpenTaxForm.Execute(row["TaxFormGUID"], null);
    }

    public bool FilterReturned
    {
      get { return (bool)GetValue(FilterReturnedProperty); }
      set { SetValue(FilterReturnedProperty, value); }
    }
    public static readonly DependencyProperty FilterReturnedProperty =
      DependencyProperty.Register("FilterReturned", typeof(bool), typeof(Returns),
        new UIPropertyMetadata(true, (obj, args) => ((Returns) obj).FilterChanged()));

    public bool FilterFiled
    {
      get { return (bool)GetValue(FilterFiledProperty); }
      set { SetValue(FilterFiledProperty, value); }
    }
    public static readonly DependencyProperty FilterFiledProperty =
      DependencyProperty.Register("FilterFiled", typeof(bool), typeof(Returns),
        new UIPropertyMetadata(false, (obj, args) => ((Returns) obj).FilterChanged()));

    public bool FilterLocalOffice
    {
      get { return (bool)GetValue(FilterLocalOfficeProperty); }
      set { SetValue(FilterLocalOfficeProperty, value); }
    }
    public static readonly DependencyProperty FilterLocalOfficeProperty =
      DependencyProperty.Register("FilterLocalOffice", typeof(bool), typeof(Returns),
        new UIPropertyMetadata(true, (obj, args) => ((Returns) obj).FilterChanged()));

    
    protected void FilterChanged()
    {
      var dv = (gridReturns.ItemsSource as DataView);
      if (dv == null) return;
      dv.RowFilter = SqlClientHelpers.BuildRowFilter(
        FilterReturned ? "Status = 'UnReturned'" : null,
        FilterFiled ? "Status <> 'Filed'" : null, 
        FilterLocalOffice ? "TaxOfficeId = " + SettingsModel.TaxOfficeId.ToString(CultureInfo.InvariantCulture) : null
      );
    }

  }
}
