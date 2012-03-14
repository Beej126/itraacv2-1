using System;
using System.Configuration;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using iTRAACv2.Model;
using iTRAACv2.View;

namespace iTRAACv2
{
  public partial class App
  {
    static public void ShowUserMessage(string text)
    {
      if (String.IsNullOrWhiteSpace(text)) return;
      if (!Current.Dispatcher.CheckAccess()) { Current.Dispatcher.Invoke((Action)(() => ShowUserMessage(text))); return; }
      var mainWindow = Current.MainWindow as MainWindow;
      if (mainWindow != null) mainWindow.ShowUserMessage(text);
    }

    static public void ShowWaitAnimation()
    {
      var mainWindow = Current.MainWindow as MainWindow;
      if (mainWindow != null) mainWindow.popWaitAnimation.Show();
    }

    static public void StopWaitAnimation()
    {
      if (Current != null && !Current.Dispatcher.CheckAccess()) { Current.Dispatcher.Invoke((Action)StopWaitAnimation); return; }
      if (Current != null && Current.MainWindow != null && (Current.MainWindow as MainWindow) != null && (Current.MainWindow as MainWindow).popWaitAnimation != null) 
        (Current.MainWindow as MainWindow).popWaitAnimation.Hide();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
      //the App.DispatcherUnhandledException is the preferrable catcher because you can "Handle = true" it and prevent the app from crashing
      DispatcherUnhandledException += CurrentDispatcherUnhandledException;

      //AppDomain.UnhandledException is only good for last ditch capturing of the problematic state info... if an Exception bubbles up this far, the app is going down no way to prevent
      AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

      //using (frmSplash splash = new frmSplash())
      {
        if (!SecurityHelpers.IsUserInGroup(SecurityHelpers.CurrentWindowsLoginNameSansDomain, "iTRAAC_Users"))
        {
          MessageBox.Show("You must be a member of the local 'iTRAAC_Users' group to run this application.", "Not authorized", MessageBoxButton.OK, MessageBoxImage.Stop);
          Shutdown();
          return;
        }
        
        base.OnStartup(e);

        //nugget: Provide the datalayer with a callback to spin the wait cursor so we don't have to litter that code all over the points where the UI waits on the datalayer
        //since we implement the waitcursor logic with 'using' boxing syntax we're actually providing an object factory as the callback method
        Proc.NewWaitObject = WaitCursorWrapper.WaitCursorWrapperFactory;

        try
        {
          Proc.ConnectionString = ConfigurationManager.ConnectionStrings["iTRAACv2ConnectionString"].ConnectionString;
        }
        catch (ConfigurationErrorsException ex)
        {
          if (ex.Message.Contains("The RSA key container could not be opened"))
          {
            MessageBox.Show(
              "The RSA encryption key has not been installed on this machine yet.\r\n" +
              "See RSAKey_Manager.cmd for more info.\r\n" +
              "Make sure to apply *read* ACL for \"iTRAAC_Users\" group to most recent files in folder:\r\n" +
              @"    ""C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys""");
            Shutdown();
            return;
          }
          throw;
        }

        Proc.ConnectionString += iTRAACv2.Properties.Settings.Default.iTRAACv2ConnectionString;
        Proc.MessageCallback = ShowUserMessage;

        SqlClientHelpers.SqlErrorObjectRemoveRegex = "(^dbo.|^tbl|^vw_|es$|s$)";

        new MainWindow().Show();

        var copyMenu = FindResource("WPFDataGrid_CopyMenu") as ContextMenu; //nugget: create a popup menu to be reused on grids which provides typical right-mouse, Copy functionality
        if (copyMenu != null)
        {
          var menuItem = copyMenu.Items[0] as MenuItem;
          if (menuItem != null)
            menuItem.Click += WPFHelpers.WPFDataGridCopyCellClick;
        }

        ModelBase.SetUserMessageCallback(ShowUserMessage); //give the model layer a generic callback to be able to fire user messages

        //join TaxForm and Sponsor at the hip w/o burdening them with a static compile time dependency
        TaxFormModel.FormStatusChangeCallback += SponsorModel.TaxFormStatusChanged;

        WPFValueConverters.BoolExpressionToBool.VariableReplacement = delegate(ref string expression) //nugget: dynamic variable expansion in ValueConverter
        {
          expression = expression.Replace("%myofficeid%", SettingsModel.TaxOfficeId.ToString(CultureInfo.InvariantCulture));
          expression = expression.Replace("%myofficecode%", SettingsModel.TaxOfficeCode.ToString(CultureInfo.InvariantCulture));
        };

        UcDetailsView.IsColVisible = iTRAACHelpers.DataColumnVisible;

        //nugget: set consisten date display format for <DatePicker>'s etc
        System.Threading.Thread.CurrentThread.CurrentCulture = (CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
        System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat.ShortDatePattern = "dd MMM yyyy"; 
      }

    }

    void CurrentDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
      e.Handled = true;
      DefaultExceptionHandler(e.Exception);
    }

    void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      DefaultExceptionHandler(e.ExceptionObject as Exception);
    }


    private void DefaultExceptionHandler(Exception ex)
    {
      StopWaitAnimation();
      while (ex.InnerException != null) ex = ex.InnerException; //drill down to root exception since this is 99.9% the most useful information

      MessageBox.Show("[" + ex.GetType() + "]\r\n" +
        ex.Message + ((ex is System.Data.SqlClient.SqlException) ? "" : "\r\n" + ex.StackTrace), "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
      //only totally bomb out of the app if we popped an exception before the mainwindow was visible... otherwise we should be able to sit where we are after an error most of the time
      if (MainWindow == null || !MainWindow.IsVisible) Shutdown(1); 
      //(MainWindow as MainWindow).ShowUserMessage("Unexpected Error" + ((ex != null) ? ": " + ex.Message : ""));
    }

  }
}
