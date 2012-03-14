using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

// copied (& fixed :) from here: http://stackoverflow.com/questions/833943/watermark-textbox-in-wpf/5433907#5433907
namespace iTRAACv2.View
{
  public class UcTextBoxWatermarked : TextBox
  {
    public string Watermark
    {
      get { return (string)GetValue(WaterMarkProperty); }
      set { SetValue(WaterMarkProperty, value); }
    }
    public static readonly DependencyProperty WaterMarkProperty =
      DependencyProperty.Register("Watermark", typeof(string), typeof(UcTextBoxWatermarked), new PropertyMetadata(OnWatermarkChanged));

    private bool _isWatermarked;

    //using a hidden alternative binding to the original source, to receive bound data change notifications that should drive replacing the watermark with the new data
// ReSharper disable UnusedMember.Local
    private string SaveText 
// ReSharper restore UnusedMember.Local
    {
      get { return (string)GetValue(SaveTextProperty); }
      set { SetValue(SaveTextProperty, value); }
    }
    public static readonly DependencyProperty SaveTextProperty =
      DependencyProperty.Register("SaveTextProperty", typeof(string), typeof(UcTextBoxWatermarked),
                                  new UIPropertyMetadata(null, (d, e) => { if (!String.IsNullOrEmpty(e.NewValue as String)) ((UcTextBoxWatermarked)d).HideWatermark(); }));

    public UcTextBoxWatermarked()
    {
      Loaded += (s, ea) => ShowWatermark();
    }

    protected override void OnGotFocus(RoutedEventArgs e)
    {
      base.OnGotFocus(e);
      HideWatermark();
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
      base.OnLostFocus(e);
      ShowWatermark();
    }

    private static void OnWatermarkChanged(DependencyObject sender, DependencyPropertyChangedEventArgs ea)
    {
      var tbw = sender as UcTextBoxWatermarked;
      if (tbw == null || !tbw.IsLoaded) return; //needed to check IsLoaded so that we didn't dive into the ShowWatermark() routine before initial Bindings had been made
      tbw.ShowWatermark();
    }

    private void ShowWatermark()
    {
      if (String.IsNullOrEmpty(Text) && !String.IsNullOrEmpty(Watermark) && IsEnabled)
      {
        _isWatermarked = true;
      
        //save the existing binding so it can be restored
        //second fix: save it in a dependency property so that we get a changed notification if this control's bound value changes (crucial!)
        var txt = BindingOperations.GetBinding(this, TextProperty);
        if (txt != null) SetBinding(SaveTextProperty, txt);
      
        //blank out the existing binding so we can throw in our Watermark
        BindingOperations.ClearBinding(this, TextProperty);

        //set the signature watermark gray
        Foreground = new SolidColorBrush(Colors.Gray);

        //display our watermark text
        Text = Watermark;
      }
    }

    private void HideWatermark()
    {
      if (_isWatermarked)
      {
        _isWatermarked = false;
        ClearValue(ForegroundProperty);

        Binding txt = BindingOperations.GetBinding(this, SaveTextProperty);
        if (txt != null) SetBinding(TextProperty, txt);
        else Text = "";
        BindingOperations.ClearBinding(this, SaveTextProperty);
      }
    }

  }
}

