using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using iTRAACv2.Model;

namespace iTRAACv2.View
{
  public partial class UcTaxFormNumber
  {

    public delegate void EntryChangedEventHandler(object sender, object e);
    public event EntryChangedEventHandler EntryChanged = null;

    public UcTaxFormNumber()
    {
      InitializeComponent();
    }

    public new bool Focus()
    {
      return(txtOrderNumber_CtrlNumber.Focus());
    }

    void OnEntryChanged(bool localCallerFlag)
    {
      lblOrderNumber.Text = (Text == "") ? "" : String.Format("({0})", Text);
      if (!localCallerFlag && (EntryChanged != null)) EntryChanged(this, null);
    }

    protected override void UserControlLoaded(object sender, RoutedEventArgs e)
    {
      cbxOrderNumber_FormType.SelectedIndex = 0;

      cbxOrderNumber_FiscalYear.Items.Add(new KeyValuePair<string, string>("Any", "__"));
      //populate the fiscal year numbers for the form# input
      var currentFY = int.Parse(iTRAACHelpers.FiscalYear2Chars);
      for (int i = 5 /*2005 is beginning of iTRAAC recorded history*/; i <= currentFY; i++)
        cbxOrderNumber_FiscalYear.Items.Add(new KeyValuePair<string, string>(
          ("0" + i.ToString(CultureInfo.InvariantCulture)).Right(2), 
          ("0" + i.ToString(CultureInfo.InvariantCulture)).Right(2) )
        );
      
      cbxOrderNumber_FiscalYear.SelectedValue = iTRAACHelpers.FiscalYear2Chars; //default to current FY

      OnEntryChanged(true);

      cbxOrderNumber_FormType.SelectionChanged += (s, o) => OnEntryChanged(false);
      cbxOrderNumber_Office.SelectionChanged += (s, o) => OnEntryChanged(false);
      cbxOrderNumber_FiscalYear.SelectionChanged += (s, o) => OnEntryChanged(false);
      txtOrderNumber_CtrlNumber.TextChanged += (s, o) => OnEntryChanged(false);

      txtOrderNumber_CtrlNumber.PreviewTextInput += WPFHelpers.IntegerOnlyTextBoxPreviewTextInput;
    }

    public void Clear()
    {
      txtOrderNumber_CtrlNumber.Text = "";
      OnEntryChanged(true);
    }

    public bool AlwaysShow { get; set; }

    public string Text
    {
      get
      {
        return (TaxFormModel.FormatOrderNumber(AlwaysShow, 
          (cbxOrderNumber_FormType.SelectedValue != null) ? cbxOrderNumber_FormType.SelectedValue.ToString() : "",
          (cbxOrderNumber_Office.SelectedValue != null) ? cbxOrderNumber_Office.SelectedValue.ToString() : "",
          (cbxOrderNumber_FiscalYear.SelectedValue != null) ? cbxOrderNumber_FiscalYear.SelectedValue.ToString() : "",
          txtOrderNumber_CtrlNumber.Text)
        );
      }
    }
  }
}
