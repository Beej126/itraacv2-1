using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Data;
using System.Collections.Specialized;

namespace WebDash
{
  public partial class Home : Page
  {
    public Home()
    {
      InitializeComponent();

      //SimpleTest();
      LoadBarChart();
    }

    private void SimpleTest()
    {
      var client = new DashWCFClient.DashWCFClient();
      client.SimpleTestCompleted += client_SimpleTestCompleted;
      client.SimpleTestAsync();
    }

    void client_SimpleTestCompleted(object sender, DashWCFClient.SimpleTestCompletedEventArgs e)
    {
      if (CheckAsyncResultError(e)) MessageBox.Show(e.Result);
    }

    private void LoadBarChart()
    {
      ((ISeriesHost)barChart).Axes.CollectionChanged += Axes_CollectionChanged;

      var client = new DashWCFClient.DashWCFClient();
      client.ProcCallCompleted += client_ProcCallCompleted;
      LayoutRoot.IsBusy = true;
      client.ProcCallAsync("Dash_ReturnedNotFiled", null);
    }

    private int _maxHorizontalAxis = 0;
    void client_ProcCallCompleted(object sender, DashWCFClient.ProcCallCompletedEventArgs e)
    {
      LayoutRoot.IsBusy = false;
      if (!CheckAsyncResultError(e)) return;

      _maxHorizontalAxis = e.Result.Where(r => (bool)r["OfficeActive"] == true).Max(r => (int)r["OfficeTotalFormCount"]);

      StackedBarSeries series = new StackedBarSeries();
      var FiscalYears = e.Result.OrderBy(g => g["FiscalYear"]).GroupBy(r => r["FiscalYear"]).Select(r => r.First()["FiscalYear"]);

      foreach (int year in FiscalYears)
      {
        series.SeriesDefinitions.Add(new SeriesDefinition()
        {
          Title = year.ToString(),
          ItemsSource = e.Result.Where(r => (int)r["FiscalYear"] == year).OrderBy(r => r["OfficeActive"]).ThenBy(r => r["OfficeTotalFormCount"]).ToArray<Dictionary<string, object>>(),
          DependentValuePath = "[FormCount]",
          IndependentValuePath = "[TaxOfficeName]"
          
        });
      }

      barChart.Series.Add(series);
    }

    private bool CheckAsyncResultError(System.ComponentModel.AsyncCompletedEventArgs e)
    {
      if (e.Error != null)
      {
        Exception ex = e.Error;
        while (ex.InnerException != null) ex = ex.InnerException;
        MessageBox.Show(ex.Message + "\r\r" + ex.StackTrace);
        return (false);
      }
      else return (true); //true = no error
    }

    //from here: http://blog.therohrers.org/post/Silverlight-ChartHelper-revisited.aspx
    void Axes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
      if (e.Action != NotifyCollectionChangedAction.Remove)
      {
        foreach (LinearAxis axis in e.NewItems.OfType<LinearAxis>())
        {
          axis.Maximum = _maxHorizontalAxis + 100;
        }
      }
    }

  }
}