using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data;
using System.Linq;

namespace iTRAACv2.View
{
  public delegate void VendorSearchCallback(object vendorID, object vendorName);

  public partial class VendorSearch
  {
    public void Open(VendorSearchCallback callback)
    {
      if (popVendorSearch.IsOpen) return; //because we want to pop this open via both keyboard focus and click, it could be double fired
      popVendorSearch.IsOpen = true;

      //save keyboard focus so we can restore when closing
      _prePopupFocusedElement = Keyboard.FocusedElement;

      _cb = callback;

      if (lbxVendorList.Items.Count == 0) //only force focus to input box if we haven't been here already this session
        txtVendorName.Focus();
      else if (lbxVendorList.SelectedItem != null)
        ((ListBoxItem)lbxVendorList.ItemContainerGenerator.ContainerFromIndex(lbxVendorList.SelectedIndex)).Focus();
      else lbxVendorList.Focus();
    }
    private IInputElement _prePopupFocusedElement;
    private VendorSearchCallback _cb;
// ReSharper disable InconsistentNaming
    private readonly Proc Vendor_New = new iTRAACProc("Vendor_New");
// ReSharper restore InconsistentNaming

    private void BtnSelectClick(object sender, object e)
    {
      var row = lbxVendorList.SelectedItem as DataRowView;
      if (row == null) return; //i.e. if they hit select button w/o selecting a row in the grid
      CommonCloseLogic();
      _cb(row["RowGUID"], row["ShortDescription"]);
    }

    private void BtnCancelClick(object sender, RoutedEventArgs e)
    {
      CommonCloseLogic();
    }

    private void CommonCloseLogic()
    {
      popVendorSearch.IsOpen = false;
      Keyboard.Focus(_prePopupFocusedElement);
    }

    //datagrid approach:
    //private void lbxVendorList_DoubleClick(object sender, MouseButtonEventArgs e)
    //{
    //  if (e.OriginalSource is TextBlock) //only a selection if we double clicked on cell, (not header or scroll bars)
    //    btnSelect_Click(null, null);
    //}

    public VendorSearch()
    {
      DataContext = Vendor_New;
      InitializeComponent();
      border.MouseDown += (s, e) => e.Handled = true; //http://stackoverflow.com/questions/619798/why-does-a-wpf-popup-close-when-its-background-area-is-clicked
    }

    #region PlacementTarget - propagate from containing UserControl down to nested Popup control... amazingly complicated, must be an easier way???
    public UIElement PlacementTarget
    {
      get { return (GetValue(PlacementTargetProperty) as UIElement); }
      set { SetValue(PlacementTargetProperty, value); }
    }
    public static readonly DependencyProperty PlacementTargetProperty = DependencyProperty.Register(
      "PlacementTarget", typeof(UIElement), typeof(VendorSearch),
        new PropertyMetadata((obj, args) =>
        { ((VendorSearch) obj).popVendorSearch.PlacementTarget = args.NewValue as UIElement; })); 

    #endregion

    #region Type ahead search logic
    private readonly BackgroundWorkerEx<VendorSearchArgs> _vendorSearchTypeAhead = new BackgroundWorkerEx<VendorSearchArgs>();

    protected override void UserControlLoaded(object sender, RoutedEventArgs e)
    {
      txtVendorName.Focus();

      _vendorSearchTypeAhead.OnExecute += VendorSearchOnExecute;
      _vendorSearchTypeAhead.OnCompleted += VendorSearchOnCompleted;
    }

    private class VendorSearchArgs
    {
      public DataTable ResultTable;
// ReSharper disable FieldCanBeMadeReadOnly.Local - Text & Success can be set by reflection via Proc.ExecuteDataset()
// ReSharper disable UnassignedField.Local
#pragma warning disable 0649
      public string Text; //put a text propertyName on this object so that the generic ExecuteDataset(label) method can populate it with any error
// ReSharper restore UnassignedField.Local
// ReSharper restore FieldCanBeMadeReadOnly.Local
// ReSharper disable ConvertToConstant.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable UnassignedField.Local
      public bool Success;
#pragma warning restore 0649
// ReSharper restore UnassignedField.Local
// ReSharper restore FieldCanBeMadeReadOnly.Local
// ReSharper restore ConvertToConstant.Local
      public readonly string[] Values;

      public VendorSearchArgs(params string[] values)
      {
        Values = values;
      }
    }

    private void VendorSearchCriteriaChanged(object sender, object e)
    {
      //the comboboxes fire their change event before the form is initialized
      if (lblVendorSearchError == null) return;

      //implement mutually exclusive text boxes... since their TextChanged event handlers all fire on eachother, this nixes the feedback loop
      //kill any pending search whenever the text is changed
      _vendorSearchTypeAhead.Cancel();

      //blank out any previously posted search errors
      lblVendorSearchError.Text = "";

      //don't even bother searching if minimum input criteria hasn't been met
      if (
        //VendorName at least 3 chars
        (txtVendorName.Text.Length < 3)
      )
      {
        lbxVendorList.ItemsSource = null;
        return;
      }

      //(re)initiate search with new criteria
      Debug.Assert(cbxVendorNameSearchType != null, "cbxVendorNameSearchType != null");
      _vendorSearchTypeAhead.Initiate(new VendorSearchArgs(
        "VendorName", txtVendorName.Text,
        "VendorName_SearchType", ((ComboBoxItem) cbxVendorNameSearchType.SelectedItem).Content.ToString(),
        "VendorCity", txtVendorCity.Text,
        "VendorCity_SearchType", ((ComboBoxItem) cbxVendorCitySearchType.SelectedItem).Content.ToString()
      ));
    }

    //this happens off the UI thread
    static void VendorSearchOnExecute(VendorSearchArgs state)
    {
// ReSharper disable InconsistentNaming
      using (var Vendor_Search = new Proc("Vendor_Search"))
// ReSharper restore InconsistentNaming
        state.ResultTable = Vendor_Search.AssignValues(state.Values.ToArray<Object>()).ExecuteDataSet(state).Tables[0];
    }

    void VendorSearchOnCompleted(VendorSearchArgs state)
    {
      if (state.Success)
      {
        lbxVendorList.ItemsSource = state.ResultTable.DefaultView;
        //lbxVendorList.Columns[lbxVendorList.Columns.Count - 1].Visibility = Visibility.Hidden; //hide ShortDescription
        //lbxVendorList.Columns[lbxVendorList.Columns.Count - 2].Visibility = Visibility.Hidden; //hide VendorID

        if (lbxVendorList.Items.Count > 0)
        {
          Dispatcher.BeginInvoke((Action)(() =>
          {
            ((ListBoxItem) lbxVendorList.ItemContainerGenerator.ContainerFromIndex(0))
              .Focus();
            lbxVendorList.SelectedIndex = 0;
          }), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }
      }
      else
        lblVendorSearchError.Text = state.Text;
    }
    #endregion

    private void AddVendorClick(object sender, RoutedEventArgs e)
    {
      Vendor_New.ExecuteNonQuery();
      CommonCloseLogic();
      _cb(Vendor_New["@VendorGUID"], Vendor_New["@VendorName"]);
    }

    private void ClearVendorClick(object sender, RoutedEventArgs e)
    {
      Vendor_New.ClearParms();
      DataContext = null; DataContext = Vendor_New; //since Proc class is a POCO we have to manually refresh the UI bindings, NBD in this case.
    }

  }

}
