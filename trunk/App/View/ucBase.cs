using System;
using System.Windows.Controls;

namespace iTRAACv2
{
  public /*abstract*/ class ucBase : UserControl
  {
    public ucBase()
    {
      Loaded += new System.Windows.RoutedEventHandler(ucBase_Loaded);
    }

    //base class logic to avoid double fires of the loaded event... e.g. TabItem refires loaded for all its children every time it's reselected
    private bool _loaded = false;
    void ucBase_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
      if (_loaded) return; _loaded = true; //this sucks but is actually recommended by an MSDN MVP: http://social.msdn.microsoft.com/Forums/en/wpf/thread/39ce4ebd-75a6-46d5-b303-2e0f89c6eb8d

      if (WPFHelpers.DesignMode) return; //don't fire UserControl_Loaded events at designtime since the DB connection string won't be defined and corresponding exceptions prevent designer surface from displaying UI

      UserControl_Loaded(sender, e);
    }

    //this method defines a body because making it abstract would prevent the corresponding subclassed controls from rendering in the VS2010 designer :(
    //but since there is no actual implementation, sublclasses are expected _NOT_ to call base.UserControl_Loaded() in their implementations
    protected virtual void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e) { }

  }
}