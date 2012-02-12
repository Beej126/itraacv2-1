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
using System.Threading;
using System.Data;

namespace iTRAACv2
{
  public partial class DailyActivity : ucBase
  {
    public DailyActivity()
    {
      InitializeComponent();

      iTRAACHelpers.WPFDataGrid_Standard_Behavior(gridDailyActivity);
    }

    protected override void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      dateRangeActivity.Minimum = DateTime.Today.AddMonths(-3);
      dateRangeActivity.Maximum = DateTime.Today.AddDays(1).AddSeconds(-1);
      cbxActivityDate.SelectedIndex = 0;
    }

    private bool _skip_cbxActivityDate_SelectionChanged = false;
    private void cbxActivityDate_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      string selected = (string)((ComboBoxItem)cbxActivityDate.SelectedItem).Tag; //pulling tag off SelectedItem vs SelectedValue was necessary because at this point in the change event lifecycle, SelectedValue still reflects the *previously* selected value.
      if (selected == "CUSTOM") return;

      _skip_cbxActivityDate_SelectionChanged = true;
      dateActivityEnd.SelectedDate = dateRangeActivity.Maximum;
      switch (selected)
      {
        case "TODAY": dateActivityStart.SelectedDate = DateTime.Today; break;
        case "WEEK": dateActivityStart.SelectedDate = DateTime.Today.MondayOfWeek(); break;
        case "2WEEK": dateActivityStart.SelectedDate = DateTime.Today.AddDays(-7).MondayOfWeek(); break;
      }
      _skip_cbxActivityDate_SelectionChanged = false;
    }

    private Thread _QueryThread = null;

    class DateRange
    {
      public DateRange(DailyActivity me, string activityType, DateTime start, DateTime end)
      {
        Me = me;
        ActivityType = activityType;
        Start = start;
        End = end;
      }
      public DailyActivity Me { get; private set; }
      public string ActivityType { get; private set; }
      public DateTime Start { get; private set; }
      public DateTime End { get; private set; }
    }

    private void ActivityStartEnd_DateChanged(object sender, SelectionChangedEventArgs e)
    {
      if (WPFHelpers.DesignMode) return; //there was an annoying exception that would close down all of VS2010

      if (dateActivityStart == null || dateActivityStart.SelectedDate == null || dateActivityEnd == null || dateActivityEnd.SelectedDate == null) return;

      long minticks = Math.Min(dateActivityStart.SelectedDate.Value.Ticks, dateActivityEnd.SelectedDate.Value.Ticks);
      if (minticks < dateRangeActivity.Minimum.Ticks) dateRangeActivity.Minimum = new DateTime(minticks); //expand the slider's minimum allowed if we're trying to go further back in time with the date boxes

      if (_QueryThread != null) _QueryThread.Abort();
      _QueryThread = new Thread(delegate(object state)
      {
        Thread.Sleep(1500); //slight delay to smooth out reacting to the slider while it's still being drug around

        DateRange parms = state as DateRange;
        int daysspanned = (parms.End - parms.Start).Days;
        if (daysspanned > 30) if (MessageBoxResult.Cancel == MessageBox.Show(
          String.Format("That's a {0} day span.\rIt will make the database really smoke, are you sure?", daysspanned),
            "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning)) return; //nugget: aint that nice that MessageBox goes switches to the UI thread for us

        using (Proc DailyActivity_s = new iTRAACProc("DailyActivity_s"))
        {
          DailyActivity_s["@ActivityType"] = parms.ActivityType;
          DailyActivity_s["@StartDate"] = parms.Start;
          DailyActivity_s["@EndDate"] = parms.End;
          DataTable t = DailyActivity_s.ExecuteDataSet().Table0;
          parms.Me.Dispatcher.Invoke((Action)delegate() {
            DataView dv = (parms.Me.gridDailyActivity.ItemsSource as DataView);
            if (dv != null) dv.Table.Dispose();
            parms.Me.gridDailyActivity.ItemsSource = t.DefaultView;
            WPFHelpers.GridSort(parms.Me.gridDailyActivity, "Purchased", System.ComponentModel.ListSortDirection.Descending);
          });
        }
      });
      _QueryThread.Start(new DateRange(this, (string)cbxActivityType.SelectedValue, dateActivityStart.SelectedDate.Value, dateActivityEnd.SelectedDate.Value));

      if (_skip_cbxActivityDate_SelectionChanged) return;
      cbxActivityDate.SelectedValue = "CUSTOM";
    }

  }

}
