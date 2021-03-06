﻿using System;
using System.Collections;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace iTRAACv2.View
{
  public partial class UcDetailsView
  {
    public delegate bool IsColVisibleCallback(DataColumn col);
    static public IsColVisibleCallback IsColVisible;

    public string EmptyMessage { get; set; }
    public Orientation Orientation
    { 
      get 
      { 
        return(pnlFields.Orientation); 
      } 
      set
      {
        pnlFields.Orientation = value;
        //this is a bit counterintuitive at first... 
        //the basic idea is that by disabling the scroll bars in the same dimension as the orientation, we force the controls to "flow" in the dimension of orientation
        //and a scrollbar will display in the alternate dimension... 
        //i.e. if we're orienting horizontally, we wind up with a vertical scroll bar when there's not enough horizontal space left to display everything
        scrollbars.HorizontalScrollBarVisibility = (value == Orientation.Horizontal) ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
        scrollbars.VerticalScrollBarVisibility = (value == Orientation.Vertical) ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
        //scrollbars.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        //scrollbars.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
      }

    }

    public Orientation LabelOrientation { get; set; }

    public UcDetailsView()
    {
      InitializeComponent();
      EmptyMessage = "ucDetailsView instance bound to null DataContext";
      LabelOrientation = Orientation.Vertical;
      Orientation = Orientation.Horizontal;
    }

    protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
    {
      base.OnRender(drawingContext);
      //if we're doing horizontal labels, then set all the labels to the same consistent max width thus making all the label+field pairs line up nice vertically
      if (LabelOrientation == Orientation.Horizontal)
      {
        var maxLableActualWidth = (from StackPanel lbl in _labels select lbl.ActualWidth).Concat(new double[] {0}).Max();
        foreach (StackPanel lbl in _labels) { lbl.Width = maxLableActualWidth; }
      }
    }

    public bool IsReadOnly
    {
      get { return (bool)GetValue(IsReadOnlyProperty); }
      set { SetValue(IsReadOnlyProperty, value); }
    }
    public static readonly DependencyProperty IsReadOnlyProperty =
      DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(UcDetailsView),
      new PropertyMetadata(OnIsReadOnlyChanged));

    //nugget: DependencyProperty -> PropertyChangedCallback example method signature (not available via auto code gen in VS2010)
    static private void OnIsReadOnlyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
    {
      foreach (TextBox txt in ((UcDetailsView) obj).pnlFields.FindChildren<TextBox>())
      {
        txt.Style = (bool)args.NewValue ? Application.Current.Resources["ReadOnlyTextBox"] as Style : null;
      }
    }

    //nugget: DependencyProperty - standard structure: normal .Net propertyName wrapped around statically declared DependencyProperty 'backing' field.  The backing field provides the point of indirection necessary for Binding etc... 
    //nugget: therefore the normal propertyName's Set() isn't really the VALUE setter we typically require and we need to provide a PropertyChangedCallback to the static DependencyProperty declaration where we can code up our normal setter logic
    public DataRowView ItemsSource
    {
      get { return (DataRowView)GetValue(ItemsSourceProperty); }
      set { SetValue(ItemsSourceProperty, value); }
    }
    public static readonly DependencyProperty ItemsSourceProperty = //warning: heavy one-liner attack ;)
      DependencyProperty.Register("ItemsSource", typeof(DataRowView), typeof(UcDetailsView),
        new PropertyMetadata((obj, args) => //nugget: passing lambda for PropertyChangedCallback requires either a cast or named param so that compiler finds proper PropertyMetadata() constructor overload (otherwise compiler errors on matching lambda to generic 'object' param)
                             ((UcDetailsView) obj).SetItemsSource(args.NewValue as DataRowView)));

    private readonly ArrayList _labels = new ArrayList();
    private void SetItemsSource(DataRowView fields)
    {
      pnlFields.Children.Clear();

      if (fields == null)
      {
        if (EmptyMessage != "") 
        {
          var tb = new TextBlock {Text = EmptyMessage};
          pnlFields.Children.Add(tb);
        }
        return;
      }

      //nugget: obtain the Path string for a bound control: http://stackoverflow.com/questions/2767557/wpf-get-propertyName-that-a-control-is-bound-to-in-code-behind
      //pull the Path from the ItemsSource Binding expression so that we can prefix our DataRowView field bindings appropriately
      var bexp = BindingOperations.GetBindingExpression(this, ItemsSourceProperty);
      var baseItemsSourcePath = (bexp == null) ? "" : bexp.ParentBinding.Path.Path + ".";

      foreach (DataColumn col in fields.Row.Table.Columns)
      {
        if (IsColVisible != null && !IsColVisible(col)) continue;

        var bundle = new StackPanel {Orientation = LabelOrientation, Margin = new Thickness(3, 4, 3, 3)};

        var lbl = new TextBlock
                    {
                      Margin = new Thickness(2, 0, 3, 2),
                      Text = col.ColumnName + ":",
                      HorizontalAlignment =
                        (LabelOrientation == Orientation.Vertical)
                          ? HorizontalAlignment.Left
                          : HorizontalAlignment.Right,
                      VerticalAlignment = VerticalAlignment.Center
                    };
        var lblContainer = new StackPanel(); //wrap something around the label to give it a box to right align into when we set the consisten max width (see OnRender)
        lblContainer.Children.Add(lbl);
        bundle.Children.Add(lblContainer);
        if (LabelOrientation == Orientation.Horizontal) _labels.Add(lblContainer);

        var txt = new TextBox {HorizontalAlignment = HorizontalAlignment.Left};

        if (col.MaxLength>0) txt.MaxLength = col.MaxLength;
        if (IsReadOnly) txt.Style = Application.Current.Resources["ReadOnlyTextBox"] as Style;

        //make a larger textbox for fields with MaxLength > 100 
        txt.Width = Math.Min(9 * ((col.MaxLength < 1) ? 10 /*if no col length defined, assume 10*/: col.MaxLength), 150);
        if (col.MaxLength > 100)
        {
          txt.Height = 36;
          txt.TextWrapping = TextWrapping.Wrap;
          txt.AcceptsReturn = true;
          txt.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        }

        txt.SetBinding(TextBox.TextProperty, new Binding(baseItemsSourcePath + col.ColumnName) 
        { 
          UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, //commit edits to the underlying datastore as each letter is typed (otherwise you're dependent on lose focus which doesn't fire on save button click)
          Mode = BindingMode.TwoWay //go ahead and bind TwoWay w/o checking current IsReadOnly setting so this handles IsReadOnly getting flipped off over the course of runtime
        });

        bundle.Children.Add(txt);

        pnlFields.Children.Add(bundle);
      }

    }

  }
}
