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
  public delegate void GoodsAndServicesSearchCallback(object ID, object Name);

  public partial class ucGoodsAndServicesSearch : ucBase
  {
    public ucGoodsAndServicesSearch()
    {
      InitializeComponent();
    }

    private void grdResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      btnSelect_Click(null, null);
    }

    private GoodsAndServicesSearchCallback cb = null;
    public void Open(GoodsAndServicesSearchCallback callback)
    {
      cb = callback;
      popup.IsOpen = true;
      txtSearch.Focus();
    }

    private void btnSelect_Click(object sender, RoutedEventArgs e)
    {
      popup.IsOpen = false;
      DataRowView row = grdResults.SelectedItem as DataRowView;
      if (row == null) return; //i.e. if they hit select button w/o selecting a row in the grid
      cb(row["GoodsServicesID"], row["GoodsServiceName"]);
    }

    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
      popup.IsOpen = false;
    }

    #region PlacementTarget - propagate from containing UserControl down to nested Popup control... amazingly complicated, must be an easier way???
    public UIElement PlacementTarget
    {
      get { return (GetValue(PlacementTargetProperty) as UIElement); }
      set { SetValue(PlacementTargetProperty, value); }
    }

    public static readonly DependencyProperty PlacementTargetProperty = DependencyProperty.Register(
      "PlacementTarget", typeof(UIElement), typeof(ucGoodsAndServicesSearch),
        new PropertyMetadata(propertyChangedCallback : (obj, args) =>
        { (obj as ucGoodsAndServicesSearch).popup.PlacementTarget = args.NewValue as UIElement; })); 
    #endregion

    #region Type ahead search logic
    private BackgroundWorkerEx<SearchArgs> SearchTypeAhead = new BackgroundWorkerEx<SearchArgs>(500); //half a second seems responsive enough but not too heavy on the server

    protected override void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      txtSearch.Focus();

      SearchTypeAhead.OnExecute += new BackgroundWorkerEx<SearchArgs>.BackgroundWorkerExCallback(SearchOnExecute);
      SearchTypeAhead.OnCompleted += new BackgroundWorkerEx<SearchArgs>.BackgroundWorkerExCallback(SearchOnCompleted);
    }

    private class SearchArgs
    {
      public System.Data.DataTable resultTable = null;
      public string Text = null; //put a text propertyName on this object so that the generic ExecuteDataset(label) method can populate it with any error
      public bool Success = false;
      public string[] values = null;

      public SearchArgs(params string[] Values)
      {
        values = Values;
      }
    }

    private void SearchCriteriaChanged(object sender, object e)
    {
      //the comboboxes fire their change event before the form is initialized
      if (lblSearchError == null) return;

      //implement mutually exclusive text boxes... since their TextChanged event handlers all fire on eachother, this nixes the feedback loop
      //kill any pending search whenever the text is changed
      SearchTypeAhead.Cancel();

      //blank out any previously posted search errors
      lblSearchError.Text = "";

      //don't even bother searching if minimum input criteria hasn't been met
      if (
        //search input at least 3 chars
        (txtSearch.Text.Length < 3)
      )
      {
        grdResults.ItemsSource = null;
        return;
      }

      //(re)initiate search with new criteria
      SearchTypeAhead.Initiate(new SearchArgs(
         "SearchType", cbxSearchType.SelectedValue.ToString(),
         "SearchName", txtSearch.Text
       ), true);
    }

    //this happens off the UI thread
    void SearchOnExecute(SearchArgs state)
    {
      using (Proc GoodsAndServices_Search = new Proc("GoodsAndServices_Search"))
      {
        GoodsAndServices_Search.AssignValues(state.values);
        state.resultTable = GoodsAndServices_Search.ExecuteDataTable(state);
      }
    }

    void SearchOnCompleted(SearchArgs state)
    {
      if (state.Success)
      {
        grdResults.ItemsSource = state.resultTable.DefaultView;
        grdResults.Columns.Where(c => c.SortMemberPath == "GoodsServicesID").Single().Visibility = Visibility.Hidden;

      }
      else
        lblSearchError.Text = state.Text;
    }
    #endregion

  }
}
