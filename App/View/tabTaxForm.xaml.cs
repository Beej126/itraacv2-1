using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Data;
using System.Windows.Controls;
using iTRAACv2.Model;

namespace iTRAACv2.View
{
  public partial class TabTaxForm
  {
    public TaxFormModel Taxform { get { return (Model as TaxFormModel); } }

    static public void Open(TabControl tabControl, string taxFormGUID)
    {
      OpenTab<TabTaxForm>(tabControl, ModelBase.Lookup<TaxFormModel>(taxFormGUID));
    }

    public TabTaxForm()
    {
      InitializeComponent();

      iTRAACHelpers.WpfDataGridStandardBehavior(gridRemarks);
    }

    protected override void UserControlLoaded(object sender, RoutedEventArgs e)
    {
      WPFHelpers.GridSort(gridRemarks, new[] { "Alert", "LastUpdate" }, new[] { ListSortDirection.Descending, ListSortDirection.Descending });
      UsedDate.Focus();
    }

    protected override void OnClosed(){} //nothing necessary here yet

    //private void BtnVendorGotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    //{
    //  if (string.IsNullOrWhiteSpace(Taxform.Fields["VendorGUID"].ToString()))
    //    btnVendor_Click();
    //}

    private void BtnVendorClick(object sender = null, RoutedEventArgs e = null)
    {
      popVendorSearch.Open(VendorSelected);
    }

    private void VendorSelected(object vendorGUID, object vendorName)
    {
      Taxform.Fields["VendorGUID"] = vendorGUID;
      Taxform.Fields["Vendor"] = vendorName;

      if (!String.IsNullOrWhiteSpace(vendorGUID.ToString()))
        KeyboardFocusContainer.FocusNext();
    }

    //private void BtnGoodServiceClick(object sender, RoutedEventArgs e)
    //{
    //  popGoodsAndServicesSearch.Open(new GoodsAndServicesSearchCallback(GoodServiceSelected));
    //}

    //private void GoodServiceSelected(object goodServiceGUID, object goodServiceDescription)
    //{
    //  Taxform.Fields["GoodServiceGUID"] = goodServiceGUID;
    //  Taxform.Fields["GoodsServiceName"] = goodServiceDescription;
    //}

    private void BtnReturnClick(object sender, RoutedEventArgs e)
    {
      var isClosing = (sender == btnReturnClose);
      if (!Taxform.ReturnForm(isClosing)) return;
      if (isClosing) BtnSaveCloseClick(null, null);
    }

    private void BtnFileClick(object sender, RoutedEventArgs e)
    {
      var isClosing = (sender == btnFileClose);
      if (!Taxform.FileForm(isClosing)) return; //taxform.FileForm() is essentially taxform.Save() with Return as well as File logic
      if (isClosing) Close();
    }

    private void BtnReloadClick(object sender, RoutedEventArgs e)
    {
      using(new IsEnabledWrapper(btnReload, true)) //nugget: i think this is pretty cool, saves a lot of syntax
      {
        if (!Model.IsModified) return;
        var mb = MessageBox.Show("Keep your unsaved changes?", "Reload", MessageBoxButton.YesNo);
        switch (mb)
        {
          case MessageBoxResult.Yes:
            return;
          case MessageBoxResult.No:
            Model.ReLoad();
            break;
        }
      }
    }

    private void BtnSaveClick(object sender, RoutedEventArgs e)
    {
      Taxform.Save();
    }

    private void BtnSaveCloseClick(object sender, RoutedEventArgs e)
    {
      if (Taxform.SaveUnload()) Close();
    }

    private int? _lastTransactionTypeId;
    private void CbxTransactionTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      var r = e.AddedItems[0] as DataRowView;
      Debug.Assert(r != null, "r != null");
      App.ShowUserMessage(r["ConfirmationText"].ToString());

      //if current selection is not active...
      if (!r.Field<bool>("Active"))
      {
        // and we've already set an initial default from loading the page,
        if (_lastTransactionTypeId != null)
        {
          //then deny this change by setting it back to the last one
          cbxTransactionType.SelectedValue = _lastTransactionTypeId.Value;
          return;
        }
      }
      _lastTransactionTypeId = r.Field<int>("TransactionTypeId");

      //extended fields (e.g weapon, vehicle)
      //TODO: 
      //var extendedFieldsCode = r["ExtendedFieldsCode"].ToString();
      //if (extendedFieldsCode != "")
      //{
      //  extendedFieldsCode = "TaxForm_" + extendedFieldsCode;
      //}
    }

    private void BtnLostVoidClick(object sender, RoutedEventArgs e)
    {
      var rp = (ReasonPopup)FindName("FormStatusReasonPopup");
      Debug.Assert(rp != null, "rp != null");
      var btn = sender as UcToggleButton;
      Debug.Assert(btn != null, "btn != null");

      //sender.Tag contains the RemarkTypeId for either Lost or Void
      //btn.IsChecked state has just been negated by clicking so we must take the opposite, since we may abort this change via cancelling the reason popup
      rp.ItemsSource = RemarkModel.SingleRemarkType(((Control)sender).Tag, btn.IsChecked); 

      rp.Show();
    }

    private void FormStatusReasonPopupResult(ReasonPopupResultEventArgs args)
    {
      int iRemarkTypeId = Convert.ToInt16(args.SelectedValue); 
      UcToggleButton btn = ((Math.Abs(iRemarkTypeId) == 25) ? btnLost : btnVoid); //could be negative for "UN-xxx" action

      if (!args.OK) { btn.IsChecked = !btn.IsChecked; return; }

      RemarkModel.SaveNew(Taxform.GUID, iRemarkTypeId, args.Comments);

      if (iRemarkTypeId == 25) //LOST, popup the Lost Forms Memo
      {
        var pdf = new PDFFormFiller("Lost-Forms-Memo.pdf", Taxform.Fields["OrderNumber"].ToString());
        pdf.SetField("CustomerNum", Taxform.Fields["SponsorCCode"]);
        pdf.SetField("CustomerName", Taxform.Fields["SponsorName"]);
        pdf.SetField("OrderNum1", Taxform.Fields["OrderNumber"]);
        pdf.SetField("Date", DateTime.Today.ToShortDateString());
        pdf.SetField("VATAgentName", UserModel.Current.Name);
        pdf.Display();
      }
    }

    private void BtnGiveCustomerClick(object sender, RoutedEventArgs e)
    {
      Taxform.GiveBackToCustomer();
    }

    private void BtnViolationIsCheckedChanged(object sender, RoutedEventArgs e)
    {
      //the boolean binding of btnViolation.IsChecked is not a simple symmetrical arrangement for Un/Checked states... 
      //. Toggle btnViolation.IsChecked is *OneWay* bound to taxform.IsViolation so it'll check itself accordingly upon screen initializaion
      //. when the user drives unchecked to checked, we need to launch the violation popup, gather the remarks and submit those to the taxform model
      //taxform will then insert the records in the database which will eventually come back and drive taxform.IsViolation to true
      if (btnViolation.IsChecked && !Taxform.IsViolation) ViolationReasonPopup.Show();
      else if (!btnViolation.IsChecked && Taxform.IsViolation)
      {
        btnViolation.IsChecked = true;
        MessageBox.Show("To remove this Violation:\n\nPlease examine all Violation related remarks to the right,\n and remove the remarks which no longer apply.", 
          "Violation Removal", MessageBoxButton.OK, MessageBoxImage.Hand);
      }
    }

    private void ViolationReasonPopupResult(ReasonPopupResultEventArgs args)
    {
      if (args.OK) Taxform.SetViolation(Convert.ToInt16(args.SelectedValue), args.Comments);
      else btnViolation.IsChecked = false;
    }

    private void BtnRemoveRemarkClick(object sender, RoutedEventArgs e)
    {
      var rp = (ReasonPopup)((Button)sender).Tag;
      rp.State = gridRemarks.CurrentItem as DataRowView;
      rp.Show();
    }

    private void RemoveRemarkReasonPopupResult(ReasonPopupResultEventArgs args)
    {
      if (args.OK) Taxform.RemoveRemark(args.State as DataRowView, args.Comments);
    }

    private void GridRemarksBeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
      string msg;
      e.Cancel = RemarkModel.DenyEdit(e.Row.Item as DataRowView, e.Column.SortMemberPath, out msg);
      e.Row.ToolTip = msg;
    }

    private void PrintPoChecked(object sender, RoutedEventArgs e)
    {
    }

    private void PrintAbwChecked(object sender, RoutedEventArgs e)
    {

    }

    private void BtnOpenPrintClick(object sender, RoutedEventArgs e)
    {
      //TODO: this implementation is too heavy on logic in the view layer
      //TODO: the pure approach would be moving this to a model method with a bunch of view layer callbacks for all the prompts

      //if expired, 

      //if either PO or Abw are not printed yet...
        //then if expired, message that it's too late to do anything other than voiding this form
        //if not expired, just display the print options popup

      //otherwise, if both PO & Abw are printed then...
        //if expired, ask if this is for administrative purposes?
      MessageBoxResult adminusage = MessageBox.Show("Both PO and Abw are already printed.\rIs this for administrative purposes?\r\rNo = Void this form and ",
        "Administrative Purposes?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

      if (adminusage == MessageBoxResult.Cancel) return;
      if (adminusage == MessageBoxResult.No) MessageBox.Show(""); //TODO:

      //if expired, then admin only reprints
      if (Taxform.IsExpired)
      {
        //make the "expired" message visible... including how this should not be used for giving the customer a "free reprint"
        //disable the $2 reprint radio button
        //check the "administrative reprint" radio
        //make the popup visible for selecting either form for printing and hitting ok or cancel
        //the _checked() events on either PO or Abw should do nothing fancy in this case
        popPrint.IsOpen = true;
        return;
      }
      //otherwise, not expired, so "$2 customer reprint" radio is enabled as well as "administrative reprint" 
      //then when PO or Abw is checked, if "$2 customer reprint" is selected, then 

      var expiredReprint = MessageBox.Show("Form is Expired:\rYes = Void & Create new replacement?\rNo = Administrative reprint",
                                                         "Expired Form - Customer wants Replacement?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
      switch (expiredReprint)
      {
        case MessageBoxResult.Cancel:
          return;
        case MessageBoxResult.Yes:
          {
            if (MessageBox.Show("Has the customer returned full package - All 3 copies of both PO and Abw?", "Existing paperwork accounted for?",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
              MessageBox.Show("Can't proceed with voiding this form until it's been fully returned.", "Abort", MessageBoxButton.OK, MessageBoxImage.Stop);
              return;
            }

            string sponsorGUID;
            string newTaxFormGUID;
            decimal serviceFee;
            Taxform.VoidAndNew(out sponsorGUID, out newTaxFormGUID, out serviceFee);

            // if Sponsor is loaded, add this to the current shop list
            var sponsor = ModelBase.Lookup<SponsorModel>(sponsorGUID, false);
            if (sponsor != null)
            {
// ReSharper disable UnusedVariable - TODO
              var reprint = new TransactionList.TransactionItem(sponsor.Transactions, "Reprint/Replacement", serviceFee);
// ReSharper restore UnusedVariable
            }
            return;
          }
      }

      //        MessageBox.Show("Expired form can't be (re)Printed,\ronly Voided and brand new Form sold.", "Expired Form", MessageBoxButton.OK, MessageBoxImage.Stop);

      /*else
        {
          ((CheckBox)sender).IsChecked = false;
          popPrint.IsOpen = false;
        }*/

      popPrint.IsOpen = true;
    }

    private void BtnConfirmPrintClick(object sender, RoutedEventArgs e)
    {
      Debug.Assert(chkPrintPO.IsChecked != null, "chkPrintPO.IsChecked != null");
      Debug.Assert(chkPrintAbw.IsChecked != null, "chkPrintAbw.IsChecked != null");

      Taxform.Print(
        (chkPrintPO.IsChecked.Value ? TaxFormModel.PackageComponent.OrderForm : 0) |
        (chkPrintAbw.IsChecked.Value ? TaxFormModel.PackageComponent.Abw : 0));
    }

    private void BtnCancelPrintClick(object sender, RoutedEventArgs e)
    {
      popPrint.IsOpen = false;
    }

  }

  #region Converters
  //the moment this logic is needed to be reused anywhere else, it's a good candidate for a ViewModel class that bundles both the User and TaxForm model inputs required to form the result
  public class LockToolTipConverter : WPFValueConverters.MarkupExtensionConverter, IMultiValueConverter
  {
// ReSharper disable EmptyConstructor
    public LockToolTipConverter() {} //to avoid an XAML annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (WPFHelpers.DesignMode || values[0] == DependencyProperty.UnsetValue) return ("Unlock");

      return (
        (bool)values[0] ? /*IsLocked? */
        /*readonly*/ (bool)values[1] /*HasUnlockForm*/ ? "Unlock" : "Requires [HasUnlockForm] Access"
        /*not readonly*/: /*filed?*/ ((TaxFormModel.StatusFlagsForm)values[2]).HasFlag(TaxFormModel.StatusFlagsForm.Filed) ?
            "Unlocked" : "This Tax Form is not yet filed and is therefore editable");
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class LostLocationCodeToBool : WPFValueConverters.MarkupExtensionConverter, IValueConverter
  {
// ReSharper disable EmptyConstructor
    public LostLocationCodeToBool() { } //to avoid an XAML annoying warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return (value.ToString() == "LOST");
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      return ((bool)value ? "LOST" : SettingsModel.TaxOfficeCode);
    }
  }

  public class IsGiveBackToCustomerEnabled : WPFValueConverters.MarkupExtensionConverter, IMultiValueConverter
  {
// ReSharper disable EmptyConstructor
    public IsGiveBackToCustomerEnabled() { } //to avoid an XAML annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (WPFHelpers.DesignMode || values[0] == DependencyProperty.UnsetValue) return (false);
      var form = values[0] as TaxFormModel;
      Debug.Assert(form != null, "values[0] != null");
      return (
        form.Fields.Field<string>("LocationCode") != "CUST"
        || UserModel.Current.Access.IsAdmin);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class IsReturnFormEnabled : WPFValueConverters.MarkupExtensionConverter, IMultiValueConverter
  {
// ReSharper disable EmptyConstructor
    public IsReturnFormEnabled() { } //to avoid an XAML annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (WPFHelpers.DesignMode || values[0] == DependencyProperty.UnsetValue) return (false);
      var form = values[0] as TaxFormModel;
      Debug.Assert(form != null, "form != null");
      return (
        "LOST,CUST".Contains(form.Fields.Field<string>("LocationCode"))
        || UserModel.Current.Access.IsAdmin);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
  #endregion


}
