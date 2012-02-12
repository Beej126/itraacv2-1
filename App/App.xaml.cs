using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System;
using System.Text.RegularExpressions;

namespace iTRAACv2
{
  public partial class App : Application
  {
    static public void ShowUserMessage(string Text)
    {
      if (String.IsNullOrWhiteSpace(Text)) return;
      if (!App.Current.Dispatcher.CheckAccess()) { App.Current.Dispatcher.Invoke((Action)delegate() { ShowUserMessage(Text); }); return; }
      (App.Current.MainWindow as MainWindow).ShowUserMessage(Text);
    }

    static public void ShowWaitAnimation()
    {
      (App.Current.MainWindow as MainWindow).popWaitAnimation.Show();
    }

    static public void StopWaitAnimation()
    {
      if (App.Current != null && !App.Current.Dispatcher.CheckAccess()) { App.Current.Dispatcher.Invoke((Action)delegate() { StopWaitAnimation(); }); return; }
      if (App.Current != null && App.Current.MainWindow != null && (App.Current.MainWindow as MainWindow).popWaitAnimation != null) 
        (App.Current.MainWindow as MainWindow).popWaitAnimation.Hide();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
      //the App.DispatcherUnhandledException is the preferrable catcher because you can "Handle = true" it and prevent the app from crashing
      DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(Current_DispatcherUnhandledException);

      //AppDomain.UnhandledException is only good for last ditch capturing of the problematic state info... if an Exception bubbles up this far, the app is going down no way to prevent
      System.AppDomain.CurrentDomain.UnhandledException += new System.UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

      //using (frmSplash splash = new frmSplash())
      {
        if (!SecurityHelpers.IsUserInGroup(SecurityHelpers.CurrentWindowsLoginName_SansDomain, "iTRAAC_Users"))
        {
          MessageBox.Show("You must be a member of the local 'iTRAAC_Users' group to run this application.", "Not authorized", MessageBoxButton.OK, MessageBoxImage.Stop);
          Shutdown();
          return;
        }
        
        base.OnStartup(e);

        //nugget: Provide the datalayer with a callback to spin the wait cursor so we don't have to litter that code all over the points where the UI waits on the datalayer
        //since we implement the waitcursor logic with 'using' boxing syntax we're actually providing an object factory as the callback method
        Proc.NewWaitObject = new Proc.WaitObjectConstructor(WaitCursorWrapper.WaitCursorWrapperFactory);

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
          else throw (ex);
        }

        Proc.ConnectionString += iTRAACv2.Properties.Settings.Default.iTRAACv2ConnectionString;
        Proc.MessageCallback = ShowUserMessage;

        SqlClientHelpers.SQLError_ObjectRemoveRegex = "(^dbo.|^tbl|^vw_|es$|s$)";

        new iTRAACv2.MainWindow().Show();

        ContextMenu CopyMenu = FindResource("WPFDataGrid_CopyMenu") as ContextMenu; //nugget: create a popup menu to be reused on grids which provides typical right-mouse, Copy functionality
        (CopyMenu.Items[0] as MenuItem).Click += new RoutedEventHandler(WPFHelpers.WPFDataGrid_CopyCell_Click);

        ModelBase.SetUserMessageCallback(ShowUserMessage); //give the model layer a generic callback to be able to fire user messages

        //join TaxForm and Sponsor at the hip w/o burdening them with a static compile time dependency
        TaxFormModel.FormStatusChangeCallback += SponsorModel.TaxFormStatusChanged;

        WPFValueConverters.BoolExpressionToBool.VariableReplacement = delegate(ref string Expression) //nugget: dynamic variable expansion in ValueConverter
        {
          Expression = Expression.Replace("%myoffice%", SettingsModel.TaxOfficeId.ToString());
        };

        ucDetailsView.IsColVisible = iTRAACHelpers.DataColumnVisible;

        //nugget: set consisten date display format for <DatePicker>'s etc
        System.Threading.Thread.CurrentThread.CurrentCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
        System.Threading.Thread.CurrentThread.CurrentCulture.DateTimeFormat.ShortDatePattern = "dd MMM yyyy"; 
      }

    }

    void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
      e.Handled = true;
      DefaultExceptionHandler(e.Exception);
    }

    void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
      DefaultExceptionHandler(e.ExceptionObject as Exception);
    }


    private void DefaultExceptionHandler(Exception ex)
    {
      StopWaitAnimation();
      while (ex.InnerException != null) ex = ex.InnerException; //drill down to root exception since this is 99.9% the most useful information

      MessageBox.Show("[" + ex.GetType().ToString() + "]\r\n" +
        ex.Message + ((ex is System.Data.SqlClient.SqlException) ? "" : "\r\n" + ex.StackTrace), "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
      //only totally bomb out of the app if we popped an exception before the mainwindow was visible... otherwise we should be able to sit where we are after an error most of the time
      if (MainWindow == null || !MainWindow.IsVisible) Shutdown(1); 
      //(MainWindow as MainWindow).ShowUserMessage("Unexpected Error" + ((ex != null) ? ": " + ex.Message : ""));
    }

  }
}
