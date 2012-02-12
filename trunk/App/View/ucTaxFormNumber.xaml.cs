using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace iTRAACv2
{
  public partial class ucTaxFormNumber : ucBase
  {

    public delegate void EntryChangedEventHandler(object sender, object e);
    public event EntryChangedEventHandler EntryChanged = null;

    public ucTaxFormNumber()
    {
      InitializeComponent();
    }

    public new bool Focus()
    {
      return(txtOrderNumber_CtrlNumber.Focus());
    }

    private void _EntryChanged(bool LocalCallerFlag)
    {
      lblOrderNumber.Text = (Text == "") ? "" : String.Format("({0})", Text);
      if (!LocalCallerFlag && (EntryChanged != null)) EntryChanged(this, null);
    }

    protected override void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      cbxOrderNumber_FormType.SelectedIndex = 0;

      cbxOrderNumber_FiscalYear.Items.Add(new KeyValuePair<string, string>("Any", "__"));
      //populate the fiscal year numbers for the form# input
      int currentFY = int.Parse(iTRAACHelpers.FiscalYear2Chars);
      for (int i = 5 /*2005 is beginning of iTRAAC recorded history*/; i <= currentFY; i++)
        cbxOrderNumber_FiscalYear.Items.Add(new KeyValuePair<string, string>( ("0" + i.ToString()).Right(2), ("0" + i.ToString()).Right(2) ));
      
      cbxOrderNumber_FiscalYear.SelectedValue = iTRAACHelpers.FiscalYear2Chars; //default to current FY

      _EntryChanged(true);

      cbxOrderNumber_FormType.SelectionChanged += (s, o) => _EntryChanged(false);
      cbxOrderNumber_Office.SelectionChanged += (s, o) => _EntryChanged(false);
      cbxOrderNumber_FiscalYear.SelectionChanged += (s, o) => _EntryChanged(false);
      txtOrderNumber_CtrlNumber.TextChanged += (s, o) => _EntryChanged(false);

      txtOrderNumber_CtrlNumber.PreviewTextInput += new TextCompositionEventHandler(WPFHelpers.IntegerOnlyTextBox_PreviewTextInput);
    }

    public void Clear()
    {
      txtOrderNumber_CtrlNumber.Text = "";
      _EntryChanged(true);
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
