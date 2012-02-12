using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace iTRAACv2
{
  public class ReasonPopupResultEventArgs
  {
    public ReasonPopupResultEventArgs(bool ok, string selectedValue, bool isAlert, string comments, object state)
    {
      OK = ok;
      SelectedValue = selectedValue;
      IsAlert = isAlert;
      Comments = comments;
      State = state;
    }

    public bool OK { get; private set; }
    public string SelectedValue { get; private set; }
    public bool IsAlert { get; private set; }
    public string Comments { get; private set; }
    public object State { get; private set; }
  }

  public partial class ReasonPopup : ucBase
  {
    public ReasonPopup()
    {
      InitializeComponent();
      popupThis.Placement = PlacementMode.Relative; //nugget: PlacementMode.Relative = top-left of PlacementTarget: http://msdn.microsoft.com/en-us/library/ms750577%28v=vs.85%29.aspx
    }

    public delegate void ReasonPopupResultEventHandler(ReasonPopupResultEventArgs args);
    public event ReasonPopupResultEventHandler Result;

    public IEnumerable ItemsSource
    {
      get { return (IEnumerable)GetValue(ItemsSourceProperty); }
      set { SetValue(ItemsSourceProperty, value); }
    }
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(ReasonPopup), 
        new UIPropertyMetadata(null, (o,e) => ((ReasonPopup)o).cbxReason.ItemsSource = e.NewValue as IEnumerable) );

    public string DisplayMemberPath
    {
      get { return (string)GetValue(DisplayMemberPathProperty); }
      set { SetValue(DisplayMemberPathProperty, value); }
    }
    public static readonly DependencyProperty DisplayMemberPathProperty =
        DependencyProperty.Register("DisplayMemberPath", typeof(string), typeof(ReasonPopup),
        new UIPropertyMetadata(null, (o, e) => ((ReasonPopup)o).cbxReason.DisplayMemberPath = e.NewValue as string));

    public string SelectedValuePath
    {
      get { return (string)GetValue(SelectedValuePathProperty); }
      set { SetValue(SelectedValuePathProperty, value); }
    }
    public static readonly DependencyProperty SelectedValuePathProperty =
        DependencyProperty.Register("SelectedValuePath", typeof(string), typeof(ReasonPopup),
        new UIPropertyMetadata(null, (o, e) => ((ReasonPopup)o).cbxReason.SelectedValuePath = e.NewValue as string));

    public UIElement PlacementTarget
    {
      get { return (UIElement)GetValue(PlacementTargetProperty); }
      set { SetValue(PlacementTargetProperty, value); }
    }
    public static readonly DependencyProperty PlacementTargetProperty =
        DependencyProperty.Register("PlacementTarget", typeof(UIElement), typeof(ReasonPopup),
        new UIPropertyMetadata(null, (o, e) => ((ReasonPopup)o).popupThis.PlacementTarget = e.NewValue as UIElement));

    public PlacementMode Placement
    {
      get { return (PlacementMode)GetValue(PlacementProperty); }
      set { SetValue(PlacementProperty, value); }
    }
    public static readonly DependencyProperty PlacementProperty =
        DependencyProperty.Register("Placement", typeof(PlacementMode), typeof(ReasonPopup),
        new UIPropertyMetadata(PlacementMode.Relative, (o, e) => ((ReasonPopup)o).popupThis.Placement = (PlacementMode)e.NewValue));

    public bool ShowAlert
    {
      get { return (bool)GetValue(ShowAlertProperty); }
      set { SetValue(ShowAlertProperty, value); }
    }
    public static readonly DependencyProperty ShowAlertProperty =
        DependencyProperty.Register("ShowAlert", typeof(bool), typeof(ReasonPopup), 
        new UIPropertyMetadata(false, (o,e) => ((ReasonPopup)o).chkIsAlert.Visibility = ((bool)e.NewValue) ? Visibility.Visible : Visibility.Collapsed));
    

    public object State = null;
    public string Title { get { return (Convert.ToString(lblTitle.Content)); } set { lblTitle.Content = value; } }
    public bool IsOK;
    public string ReasonText { get { return (txtComments.Text); } }

    public void Show()
    {
      //always start fresh, so that we can leave values in place when popup is closed so they can be pulled from client
      IsOK = false;
      cbxReason.SelectedValue = -1;
      txtComments.Text = "";

      if (cbxReason.Items.Count == 1) cbxReason.SelectedIndex = 0;
      popupThis.IsOpen = true;
      txtComments.Focus();
    }

    private void btnOKCancel_Click(object sender, RoutedEventArgs e)
    {
      if (sender == btnOK)
      {
        txtComments.Focus(); //unfortunately this sillyness is necessary to remove the watermark text out of the txtComments.Text property to get at the real text if there is any
        if ((cbxReason.ItemsSource != null && cbxReason.SelectedIndex == -1) || txtComments.Text == "") return; //if OK was clicked, but nothing is filled out, that corresponds to a logical cancel as well
        else IsOK = true;
      }
      
      popupThis.IsOpen = false;
    }

    private void popupThis_Closed(object sender, EventArgs e)
    {
      Result(new ReasonPopupResultEventArgs(
        IsOK, Convert.ToString(cbxReason.SelectedValue), chkIsAlert.IsChecked.Value, txtComments.Text, State));
    }

    /*
    nugget: this explains some focus weirdness i was observing,
    nugget: and the PreviewLostKeyboardFocus suggestion did the trick:
    nugget: http://stackoverflow.com/questions/1784977/wpf-popup-focus-in-data-grid 
    
    nugget: and this "PopupRoot" stuff was enlightening as well:
    nugget: http://social.msdn.microsoft.com/forums/en-US/wpf/thread/3e7cc288-f146-4b9d-9157-41efb1623c71/
    */

    private void txtComments_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
      if (popupThis.IsOpen) e.Handled = true;  
    }

  }
}
