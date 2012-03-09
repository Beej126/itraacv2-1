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
  public delegate void VendorSearchCallback(object VendorID, object VendorName);

  public partial class VendorSearch : ucBase
  {
    public object VendorGUID { get { return (_VendorGUID); } }
    public object VendorName { get { return (_VendorName); } }

    private object _VendorGUID = DBNull.Value;
    private object _VendorName = DBNull.Value;

    public void Open(VendorSearchCallback callback)
    {
      if (popVendorSearch.IsOpen) return; //because we want to pop this open via both keyboard focus and click, it could be double fired
      popVendorSearch.IsOpen = true;

      //save keyboard focus so we can restore when closing
      PrePopupFocusedElement = Keyboard.FocusedElement;

      cb = callback;

      if (lbxVendorList.Items.Count == 0) //only force focus to input box if we haven't been here already this session
        txtVendorName.Focus();
      else if (lbxVendorList.SelectedItem != null)
        ((ListBoxItem)lbxVendorList.ItemContainerGenerator.ContainerFromIndex(lbxVendorList.SelectedIndex)).Focus();
      else lbxVendorList.Focus();
    }
    private IInputElement PrePopupFocusedElement;
    private VendorSearchCallback cb = null;
    private Proc Vendor_New = new iTRAACProc("Vendor_New");

    private void btnSelect_Click(object sender, object e)
    {
      DataRowView row = lbxVendorList.SelectedItem as DataRowView;
      if (row == null) return; //i.e. if they hit select button w/o selecting a row in the grid
      CommonCloseLogic();
      cb(row["RowGUID"], row["ShortDescription"]);
    }

    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
      CommonCloseLogic();
    }

    private void CommonCloseLogic()
    {
      popVendorSearch.IsOpen = false;
      Keyboard.Focus(PrePopupFocusedElement);
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
        new PropertyMetadata(propertyChangedCallback : (obj, args) =>
        { (obj as VendorSearch).popVendorSearch.PlacementTarget = args.NewValue as UIElement; })); 

    #endregion

    #region Type ahead search logic
    private BackgroundWorkerEx<VendorSearchArgs> VendorSearchTypeAhead = new BackgroundWorkerEx<VendorSearchArgs>();

    protected override void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      txtVendorName.Focus();

      VendorSearchTypeAhead.OnExecute += new BackgroundWorkerEx<VendorSearchArgs>.BackgroundWorkerExCallback(VendorSearchOnExecute);
      VendorSearchTypeAhead.OnCompleted += new BackgroundWorkerEx<VendorSearchArgs>.BackgroundWorkerExCallback(VendorSearchOnCompleted);
    }

    private class VendorSearchArgs
    {
      public System.Data.DataTable resultTable = null;
      public string Text = null; //put a text propertyName on this object so that the generic ExecuteDataset(label) method can populate it with any error
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
        lbxVendorList.ItemsSource = null;
        return;
      }

      //(re)initiate search with new criteria
     VendorSearchTypeAhead.Initiate(new VendorSearchArgs(
        "VendorName", txtVendorName.Text,
        "VendorName_SearchType", (cbxVendorNameSearchType.SelectedItem as ComboBoxItem).Content.ToString(),
        "VendorCity", txtVendorCity.Text,
        "VendorCity_SearchType", (cbxVendorCitySearchType.SelectedItem as ComboBoxItem).Content.ToString()
      ));
    }

    //this happens off the UI thread
    void VendorSearchOnExecute(VendorSearchArgs state)
    {
      using (Proc Vendor_Search = new Proc("Vendor_Search"))
        state.resultTable = Vendor_Search.AssignValues(state.values).ExecuteDataSet(state).Tables[0];
    }

    void VendorSearchOnCompleted(VendorSearchArgs state)
    {
      if (state.Success)
      {
        lbxVendorList.ItemsSource = state.resultTable.DefaultView;
        //lbxVendorList.Columns[lbxVendorList.Columns.Count - 1].Visibility = Visibility.Hidden; //hide ShortDescription
        //lbxVendorList.Columns[lbxVendorList.Columns.Count - 2].Visibility = Visibility.Hidden; //hide VendorID

        if (lbxVendorList.Items.Count > 0)
        {
          Dispatcher.BeginInvoke((Action)delegate() 
          {
           ((ListBoxItem)lbxVendorList.ItemContainerGenerator.ContainerFromIndex(0)).Focus();
           lbxVendorList.SelectedIndex = 0;
          }, System.Windows.Threading.DispatcherPriority.ContextIdle);
        }
      }
      else
        lblVendorSearchError.Text = state.Text;
    }
    #endregion

    private void AddVendor_Click(object sender, RoutedEventArgs e)
    {
      Vendor_New.ExecuteNonQuery();
      CommonCloseLogic();
      cb(Vendor_New["@VendorGUID"], Vendor_New["@VendorName"]);
    }

    private void ClearVendor_Click(object sender, RoutedEventArgs e)
    {
      Vendor_New.ClearParms();
      DataContext = null; DataContext = Vendor_New; //since Proc class is a POCO we have to manually refresh the UI bindings, NBD in this case.
    }

  }

}
