using System;
using System.ComponentModel;
using System.Windows.Threading;
using System.Collections.Specialized;


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
public class BackgroundWorkerEx<T> //where T : IBGWorkerState
{

  public BackgroundWorkerEx(int InactivityTimeoutMillisecs = 500) //half a second seems responsive enough but not too heavy on the server
  {
    backgroundWorker.WorkerSupportsCancellation = true;
    backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
    backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);

    //delay the firing of search proc until the user hasn't changed his search criteria for a moment, save on unecessary DB server load
    timer.Tick += new EventHandler(timerCallback);
    timer.Interval = new TimeSpan(0, 0, 0, 0, InactivityTimeoutMillisecs);
  }

  public delegate void BackgroundWorkerExCallback(T State);
  public BackgroundWorkerExCallback OnExecute = null;
  public BackgroundWorkerExCallback OnCompleted = null;

  //initiate the type ahead timer... this goes in all your textbox event handlers
  //this is where we gather the UI values and make them available to be handed to the background thread that will shortly "do the work"
  public void Initiate(T State, bool WaitCursor = false)
  {
    Cancel(); //cancel the previous
    state = State;
    waitCursor = WaitCursor;
    timer.Start();
  }

  public void Cancel()
  {
    timer.Stop();
    backgroundWorker.CancelAsync();
  }

  ////////////////////////////////////////////////////////////////////////////////////////////
  /// private 
  
  private DispatcherTimer timer = new DispatcherTimer();
  private BackgroundWorker backgroundWorker = new BackgroundWorker();
  private T state;
  private bool waitCursor = false;

  //this happens on the UI thread
  private void timerCallback(object sender, EventArgs e)
  {
    if (backgroundWorker.IsBusy)
    {
      backgroundWorker.CancelAsync();
      return; //the timer will automatically bring us right back here
    }

    //the "AppStarting" wait cursor is perfect for this situation... it shows an hourglass next to the normal mouse pointer "Arrow"
    //this conveys both meanings well: that something is going on but that you can still interact with the UI
    //I'm pretty sure this cursor setting represents a WPF dependency (vs Windows Forms)... 
    //but I'm assuming if we cared for reuse elsewhere it'd be trivial to check for which Window drawing framework was in effect and set accordingly
    if (waitCursor) System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.AppStarting;

    timer.Stop(); //kill the timer when we do actually dive in to fulfill the most recent request

    //this immediately transfers control to backgroundWorker_DoWork() on a background thread
    backgroundWorker.RunWorkerAsync(null);
  }

  //this happens _off_ the UI thread
  private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
  {
    //call back to our caller to execute the "work" that is initiated by the type ahead event
    //here we hand back the values originally provided so they can be used in the "work"
    //and also provides a slot for the caller to jam in the result...
    //which will be handed back upon completion of the background thread
    if (OnExecute != null) 
      OnExecute(state);
  }

  //this happens on the UI thread
  private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
  {
    System.Windows.Input.Mouse.OverrideCursor = null; // System.Windows.Input.Cursors.Arrow;

    if (e.Cancelled || backgroundWorker.CancellationPending) return;

    if (OnCompleted != null) 
      OnCompleted(state);
  }

}
