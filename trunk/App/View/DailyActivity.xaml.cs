using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace iTRAACv2.View
{
  public partial class DailyActivity
  {
    public DailyActivity()
    {
      InitializeComponent();

      iTRAACHelpers.WpfDataGridStandardBehavior(gridDailyActivity);
    }

    protected override void UserControlLoaded(object sender, RoutedEventArgs e)
    {
      dateRangeActivity.Minimum = DateTime.Today.AddMonths(-3);
      dateRangeActivity.Maximum = DateTime.Today.AddDays(1).AddSeconds(-1);
      cbxActivityDate.SelectedIndex = 0;
    }

    private bool _skipCbxActivityDateSelectionChanged;
    private void CbxActivityDateSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      var selected = (string)((ComboBoxItem)cbxActivityDate.SelectedItem).Tag; //pulling tag off SelectedItem vs SelectedValue was necessary because at this point in the change event lifecycle, SelectedValue still reflects the *previously* selected value.
      if (selected == "CUSTOM") return;

      _skipCbxActivityDateSelectionChanged = true;
      dateActivityEnd.SelectedDate = dateRangeActivity.Maximum;
      switch (selected)
      {
        case "TODAY": dateActivityStart.SelectedDate = DateTime.Today; break;
        case "WEEK": dateActivityStart.SelectedDate = DateTime.Today.MondayOfWeek(); break;
        case "2WEEK": dateActivityStart.SelectedDate = DateTime.Today.AddDays(-7).MondayOfWeek(); break;
      }
      _skipCbxActivityDateSelectionChanged = false;
    }

    private Thread _queryThread;

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

    private void ActivityStartEndDateChanged(object sender, SelectionChangedEventArgs e)
    {
      if (WPFHelpers.DesignMode) return; //there was an annoying exception that would close down all of VS2010

      if (dateActivityStart == null || dateActivityStart.SelectedDate == null || dateActivityEnd == null || dateActivityEnd.SelectedDate == null) return;

      long minticks = Math.Min(dateActivityStart.SelectedDate.Value.Ticks, dateActivityEnd.SelectedDate.Value.Ticks);
      if (minticks < dateRangeActivity.Minimum.Ticks) dateRangeActivity.Minimum = new DateTime(minticks); //expand the slider's minimum allowed if we're trying to go further back in time with the date boxes

      if (_queryThread != null) _queryThread.Abort();
      _queryThread = new Thread(delegate(object state)
      {
        Thread.Sleep(1500); //slight delay to smooth out reacting to the slider while it's still being drug around

        var parms = state as DateRange;
        Debug.Assert(parms != null, "parms != null");
        var daysspanned = (parms.End - parms.Start).Days;
        if (daysspanned > 30) if (MessageBoxResult.Cancel == MessageBox.Show(
          String.Format("That's a {0} day span.\rIt will make the database really smoke, are you sure?", daysspanned),
            "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning)) return; //nugget: aint that nice that MessageBox goes switches to the UI thread for us

// ReSharper disable InconsistentNaming
        using (Proc DailyActivity_s = new iTRAACProc("DailyActivity_s"))
// ReSharper restore InconsistentNaming
        {
          DailyActivity_s["@ActivityType"] = parms.ActivityType;
          DailyActivity_s["@StartDate"] = parms.Start;
          DailyActivity_s["@EndDate"] = parms.End;
          var t = DailyActivity_s.ExecuteDataSet().Table0;
          parms.Me.Dispatcher.Invoke((Action)delegate
          {
            var dv = (parms.Me.gridDailyActivity.ItemsSource as DataView);
            if (dv != null) dv.Table.Dispose();
            parms.Me.gridDailyActivity.ItemsSource = t.DefaultView;
            WPFHelpers.GridSort(parms.Me.gridDailyActivity, "Purchased", System.ComponentModel.ListSortDirection.Descending);
          });
        }
      });
      _queryThread.Start(new DateRange(this, (string)cbxActivityType.SelectedValue, dateActivityStart.SelectedDate.Value, dateActivityEnd.SelectedDate.Value));

      if (_skipCbxActivityDateSelectionChanged) return;
      cbxActivityDate.SelectedValue = "CUSTOM";
    }

  }

}
