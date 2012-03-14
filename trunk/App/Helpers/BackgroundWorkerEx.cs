using System;
using System.ComponentModel;
using System.Windows.Threading;

//interface IBGWorkerState
//{
//  bool Success;
//  DataTable resultTable;
//}

/// <summary>
/// This class bundles the requirements behind implementing functionality often referred to as "Type Ahead".
/// One must simply instantiate, assign the OnExecute and OnCompleted handlers and then call Initiate() when you want to fire some "background work".
/// The interesting spin on a standard Dispatch is that the work doesn't begin immediately, but rather after a specified timeout
/// this provides a natural period for user to finish entering input criteria before the heavy lifting is initiated
/// </summary>
/// <typeparam name="T">The class used to pass state around</typeparam>
// ReSharper disable CheckNamespace
public class BackgroundWorkerEx<T> //where T : IBGWorkerState
// ReSharper restore CheckNamespace
{

  public BackgroundWorkerEx(int inactivityTimeoutMillisecs = 500) //half a second seems responsive enough but not too heavy on the server
  {
    _backgroundWorker.WorkerSupportsCancellation = true;
    _backgroundWorker.DoWork += BackgroundWorkerDoWork;
    _backgroundWorker.RunWorkerCompleted += BackgroundWorkerRunWorkerCompleted;

    //delay the firing of search proc until the user hasn't changed his search criteria for a moment, save on unecessary DB server load
    _timer.Tick += TimerCallback;
    _timer.Interval = new TimeSpan(0, 0, 0, 0, inactivityTimeoutMillisecs);
  }

  public delegate void BackgroundWorkerExCallback(T state);
  public BackgroundWorkerExCallback OnExecute;
  public BackgroundWorkerExCallback OnCompleted;

  //initiate the type ahead timer... this goes in all your textbox event handlers
  //this is where we gather the UI values and make them available to be handed to the background thread that will shortly "do the work"
  public void Initiate(T state, bool waitCursor = false)
  {
    Cancel(); //cancel the previous
    _state = state;
    _waitCursor = waitCursor;
    _timer.Start();
  }

  public void Cancel()
  {
    _timer.Stop();
    _backgroundWorker.CancelAsync();
  }

  ////////////////////////////////////////////////////////////////////////////////////////////
  /// private 
  
  private readonly DispatcherTimer _timer = new DispatcherTimer();
  private readonly BackgroundWorker _backgroundWorker = new BackgroundWorker();
  private T _state;
  private bool _waitCursor;

  //this happens on the UI thread
  private void TimerCallback(object sender, EventArgs e)
  {
    if (_backgroundWorker.IsBusy)
    {
      _backgroundWorker.CancelAsync();
      return; //the timer will automatically bring us right back here
    }

    //the "AppStarting" wait cursor is perfect for this situation... it shows an hourglass next to the normal mouse pointer "Arrow"
    //this conveys both meanings well: that something is going on but that you can still interact with the UI
    //I'm pretty sure this cursor setting represents a WPF dependency (vs Windows Forms)... 
    //but I'm assuming if we cared for reuse elsewhere it'd be trivial to check for which Window drawing framework was in effect and set accordingly
    if (_waitCursor) System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.AppStarting;

    _timer.Stop(); //kill the timer when we do actually dive in to fulfill the most recent request

    //this immediately transfers control to backgroundWorker_DoWork() on a background thread
    _backgroundWorker.RunWorkerAsync(null);
  }

  //this happens _off_ the UI thread
  private void BackgroundWorkerDoWork(object sender, DoWorkEventArgs e)
  {
    //call back to our caller to execute the "work" that is initiated by the type ahead event
    //here we hand back the values originally provided so they can be used in the "work"
    //and also provides a slot for the caller to jam in the result...
    //which will be handed back upon completion of the background thread
    if (OnExecute != null) 
      OnExecute(_state);
  }

  //this happens on the UI thread
  private void BackgroundWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
  {
    System.Windows.Input.Mouse.OverrideCursor = null; // System.Windows.Input.Cursors.Arrow;

    if (e.Cancelled || _backgroundWorker.CancellationPending) return;

    if (OnCompleted != null) 
      OnCompleted(_state);
  }

}
