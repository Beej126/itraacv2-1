using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using iTRAACv2.Model;

namespace iTRAACv2.View
{
  /// <summary>
  /// think of the static methods on tabBase as an extension of MainWindow's main TabControl
  //    and then tabBase subclass instances are the individual tab sheets (aka TabItem's)
  /// </summary>
  public class TabBase : UcBase
  // abstract class would be more "OO proper" but then Visual Studio (2010) won't show our subclass UI's in design mode because it thinks it can't instantiate :(
  // nugget: the answer in this post about UserControls with a base class was helpful: http://social.msdn.microsoft.com/forums/en-US/wpf/thread/d73d0e08-d541-49e8-846a-4b75dba9f3d3/
  {
    protected TabItem TabItem;
    protected ModelBase Model; 
    protected TabControl ParentTabControl { get { return (TabItem.Parent as TabControl); } }
    protected TabBase() { }
    protected virtual void OnClosed() { }

    static protected void OpenTab<TAbType>(TabControl tabControl, ModelBase model)
      where TAbType : TabBase, new() 
    {
      //see if we already have a tab open for this model, if not, create a new one
      var tabItem = tabControl.Items.Cast<TabItem>().SingleOrDefault(tab => tab.Content is TabBase && (tab.Content as TabBase).Model == model);
      if (tabItem == null)
      {

        //the VS2010 designer canvas doesn't visually render any of the content when the root element is <TabItem> (and that's no fun! :)
        //  so i'm avoiding that issue by wrapping the TabItem element around the rest of the content here on the fly
        tabItem = new TabItem();

        TabBase tabContent = new TAbType(); 
        tabItem.Content = tabContent;
        tabContent.TabItem = tabItem; //back-link our tabBase subclass instance to the TabItem it rests on for easy reference later

        //see the template definition... this customizes what's displayed in the TabItem's tab header area (the 'title' of the model, a close button, etc)
        tabItem.HeaderTemplate = Application.Current.MainWindow.Resources["BOTabItemHeaderTemplate"] as DataTemplate;

        // nugget: assign the model as the default background 'datasource' that everything in the TabItem context has access to
        tabItem.Header = //nugget: had to specifically bind the tabItem.Header for DataContext to be passed to the HeaderTemplate, otherwise DataContext at that scope was null by default (apparently a strange quirk that assigning a Template to the header wipes out the normal DataContext pointer)
          tabItem.DataContext = //set the datacontext to the model so that all the XAML has a convenient default binding 
            tabContent.Model = model; //and lastly, provide the same model as a convenient inherited propertyName so all the code behind has easy access to data fields as well

        //try to pull the tab background color if defined... this allows us to set a different color per model tab subclass for easy visual reference (all Customers are blue, TaxForms are green, etc)
        var bg = tabContent.Resources["TabBackground"] as Brush;
        if (bg != null) tabItem.Background = bg;

        //lastly, add the new tab to the main tab control
        //position it to the right of the currently selected tab so that TaxForms are intuitively grouped to right of their corresponding Customer tab
        tabControl.Items.Insert(Math.Max(2 /*static tabs, current home & cus search*/, tabControl.SelectedIndex+1), tabItem);

        tabItem.Loaded += TabItemLoaded;
      }

      tabControl.SelectedItem = tabItem;
    }

    static void TabItemLoaded(object sender, RoutedEventArgs e)
    {
      var tabItem = sender as TabItem;
      Debug.Assert(tabItem != null, "tabItem != null");

      ContentPresenter contentPresenter = null;
      for (var i = 0; i < 4; i++)
      {
        if (tabItem.TryFindChild(out contentPresenter)) break; //nugget: get reference to control in DataTemplate by name - from here: http://social.msdn.microsoft.com/Forums/en/wpf/thread/c121b7e5-cd65-415d-8bef-53a293006c37
        WPFHelpers.DoEvents();
      }
      Debug.Assert(contentPresenter != null, "contentPresenter != null");
      var txtTabHeader = tabItem.HeaderTemplate.FindName("txtTabHeader", contentPresenter) as TextBlock;
      Debug.Assert(txtTabHeader != null, "txtTabHeader != null");
      
      txtTabHeader.SetBinding(TextBlock.TextProperty, ((ModelBase) tabItem.DataContext).TitleBinding);
    }

    public void Close()
    {
      //if (tabItem.IsSelected && ParentTabControl.SelectedIndex < ParentTabControl.Items.Count - 1)
      //{
      //  ParentTabControl.SelectedIndex -= 1;
      //}

      var whatIsModified = Model.WhatIsModified;

      if (!string.IsNullOrEmpty(whatIsModified))
      {
        var mb = MessageBox.Show(
          "Save changes?" + whatIsModified, 
          "Closing " + GetType().Name.Replace("tab","") + " tab...",
          MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        switch (mb)
        {
          case MessageBoxResult.Yes:
            Model.Save();
            break;
          case MessageBoxResult.Cancel:
            return;
        }
      }

      ParentTabControl.Items.Remove(TabItem);

      OnClosed();

      Model.UnLoad();
    }

  }
}
