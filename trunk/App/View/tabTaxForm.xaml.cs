using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Data; //needed for IMultiValueConverter
using System.Data;
using System.Windows.Controls;
using System.IO;
using iTextSharp.text.pdf;

using System.Threading;

namespace iTRAACv2
{
  public partial class tabTaxForm : tabBase
  {
    public TaxFormModel taxform { get { return (model as TaxFormModel); } }

    static public void Open(System.Windows.Controls.TabControl tabControl, string TaxFormGUID)
    {
      OpenTab<tabTaxForm>(tabControl, ModelBase.Lookup<TaxFormModel>(TaxFormGUID));
    }

    public tabTaxForm()
    {
      InitializeComponent();

      iTRAACHelpers.WPFDataGrid_Standard_Behavior(gridRemarks);
    }

    protected override void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      WPFHelpers.GridSort(gridRemarks, new string[] { "Alert", "LastUpdate" }, new ListSortDirection[] { ListSortDirection.Descending, ListSortDirection.Descending });
      UsedDate.Focus();
    }

    protected override void OnClosed(){} //nothing necessary here yet

    private void btnVendor_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
      if (string.IsNullOrWhiteSpace(taxform.Fields["VendorGUID"].ToString()))
        btnVendor_Click();
    }

    private void btnVendor_Click(object sender = null, RoutedEventArgs e = null)
    {
      popVendorSearch.Open(new VendorSearchCallback(VendorSelected));
    }

    private void VendorSelected(object VendorGUID, object VendorName)
    {
      taxform.Fields["VendorGUID"] = VendorGUID;
      taxform.Fields["Vendor"] = VendorName;

      if (!String.IsNullOrWhiteSpace(VendorGUID.ToString()))
        KeyboardFocusContainer.FocusNext();
    }

    private void btnGoodService_Click(object sender, RoutedEventArgs e)
    {
      //popGoodsAndServicesSearch.Open(new GoodsAndServicesSearchCallback(GoodServiceSelected));
    }

    private void GoodServiceSelected(object GoodServiceGUID, object GoodServiceDescription)
    {
      taxform.Fields["GoodServiceGUID"] = GoodServiceGUID;
      taxform.Fields["GoodsServiceName"] = GoodServiceDescription;
    }

    private void btnReturn_Click(object sender, RoutedEventArgs e)
    {
      bool IsClosing = (sender == btnReturnClose);
      if (!taxform.ReturnForm(IsClosing)) return;
      if (IsClosing) btnSaveClose_Click(null, null);
    }

    private void btnFile_Click(object sender, RoutedEventArgs e)
    {
      bool IsClosing = (sender == btnFileClose);
      if (!taxform.FileForm(IsClosing)) return; //taxform.FileForm() is essentially taxform.Save() with Return as well as File logic
      if (IsClosing) Close();
    }

    private void btnReload_Click(object sender, RoutedEventArgs e)
    {
      using(new IsEnabledWrapper(btnReload, true)) //nugget: i think this is pretty cool, saves a lot of syntax
      {
        if (model.IsModified)
        {
          MessageBoxResult mb = MessageBox.Show("Keep your unsaved changes?", "Reload", System.Windows.MessageBoxButton.YesNo);
          if (mb == MessageBoxResult.Yes) return;
          else if (mb == MessageBoxResult.No) model.ReLoad();
        }
      }
    }

    private void btnSave_Click(object sender, RoutedEventArgs e)
    {
      taxform.Save();
    }

    private void btnSaveClose_Click(object sender, RoutedEventArgs e)
    {
      if (taxform.SaveUnload()) Close();
    }

    private int? lastTransactionTypeId;
    private void cbxTransactionType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
      DataRowView r = e.AddedItems[0] as DataRowView;
      App.ShowUserMessage(r["ConfirmationText"].ToString());

      //if current selection is not active...
      if (!r.Field<bool>("Active"))
      {
        // and we've already set an initial default from loading the page,
        if (lastTransactionTypeId != null)
        {
          //then deny this change by setting it back to the last one
          cbxTransactionType.SelectedValue = lastTransactionTypeId.Value;
          return;
        }
      }
      lastTransactionTypeId = r.Field<int>("TransactionTypeId");

      //extended fields (e.g weapon, vehicle)
      string ExtendedFieldsCode = r["ExtendedFieldsCode"].ToString();
      if (ExtendedFieldsCode != "")
      {
        ExtendedFieldsCode = "TaxForm_" + ExtendedFieldsCode;
      }
    }

    private void btnLostVoid_Click(object sender, RoutedEventArgs e)
    {
      ReasonPopup rp = (ReasonPopup)FindName("FormStatusReasonPopup");
      ucToggleButton btn = sender as ucToggleButton;

      //sender.Tag contains the RemarkTypeId for either Lost or Void
      //btn.IsChecked state has just been negated by clicking so we must take the opposite, since we may abort this change via cancelling the reason popup
      rp.ItemsSource = RemarkModel.SingleRemarkType(((Control)sender).Tag, btn.IsChecked); 

      rp.Show();
    }

    private void FormStatusReasonPopup_Result(ReasonPopupResultEventArgs args)
    {
      int iRemarkTypeId = Convert.ToInt16(args.SelectedValue); 
      ucToggleButton btn = ((Math.Abs(iRemarkTypeId) == 25) ? btnLost : btnVoid); //could be negative for "UN-xxx" action

      if (!args.OK) { btn.IsChecked = !btn.IsChecked; return; }

      RemarkModel.SaveNew(taxform.GUID, iRemarkTypeId, args.Comments);

      if (iRemarkTypeId == 25) //LOST, popup the Lost Forms Memo
      {
        PDFFormFiller pdf = new PDFFormFiller("Lost-Forms-Memo.pdf", taxform.Fields["OrderNumber"].ToString());
        pdf.SetField("CustomerNum", taxform.Fields["SponsorCCode"]);
        pdf.SetField("CustomerName", taxform.Fields["SponsorName"]);
        pdf.SetField("OrderNum1", taxform.Fields["OrderNumber"]);
        pdf.SetField("Date", DateTime.Today.ToShortDateString());
        pdf.SetField("VATAgentName", UserModel.Current.Name);
        pdf.Display();
      }
    }

    private void btnGiveCustomer_Click(object sender, RoutedEventArgs e)
    {
      taxform.GiveBackToCustomer();
    }

    private void btnViolation_IsCheckedChanged(object sender, RoutedEventArgs e)
    {
      //the boolean binding of btnViolation.IsChecked is not a simple symmetrical arrangement for Un/Checked states... 
      //. Toggle btnViolation.IsChecked is *OneWay* bound to taxform.IsViolation so it'll check itself accordingly upon screen initializaion
      //. when the user drives unchecked to checked, we need to launch the violation popup, gather the remarks and submit those to the taxform model
      //taxform will then insert the records in the database which will eventually come back and drive taxform.IsViolation to true
      if (btnViolation.IsChecked && !taxform.IsViolation) ViolationReasonPopup.Show();
      else if (!btnViolation.IsChecked && taxform.IsViolation)
      {
        btnViolation.IsChecked = true;
        MessageBox.Show("To remove this Violation:\n\nPlease examine all Violation related remarks to the right,\n and remove the remarks which no longer apply.", 
          "Violation Removal", MessageBoxButton.OK, MessageBoxImage.Hand);
      }
    }

    private void ViolationReasonPopup_Result(ReasonPopupResultEventArgs args)
    {
      if (args.OK) taxform.SetViolation(Convert.ToInt16(args.SelectedValue), args.Comments);
      else btnViolation.IsChecked = false;
    }

    private void btnRemoveRemark_Click(object sender, RoutedEventArgs e)
    {
      ReasonPopup rp = (ReasonPopup)((System.Windows.Controls.Button)sender).Tag;
      rp.State = gridRemarks.CurrentItem as DataRowView;
      rp.Show();
    }

    private void RemoveRemarkReasonPopup_Result(ReasonPopupResultEventArgs args)
    {
      if (args.OK) taxform.RemoveRemark(args.State as DataRowView, args.Comments);
    }

    private void gridRemarks_BeginningEdit(object sender, System.Windows.Controls.DataGridBeginningEditEventArgs e)
    {
      string msg;
      e.Cancel = RemarkModel.DenyEdit(e.Row.Item as DataRowView, e.Column.SortMemberPath, out msg);
      e.Row.ToolTip = msg;
    }

    private void Print_PO_Checked(object sender, RoutedEventArgs e)
    {
    }

    private void Print_Abw_Checked(object sender, RoutedEventArgs e)
    {

    }

    private void btnOpenPrint_Click(object sender, RoutedEventArgs e)
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
      else if (adminusage == MessageBoxResult.No)
      {
        MessageBox.Show("");
      }

      //if expired, then admin only reprints
      if (taxform.IsExpired)
      {
        //make the "expired" message visible... including how this should not be used for giving the customer a "free reprint"
        //disable the $2 reprint radio button
        //check the "administrative reprint" radio
        //make the popup visible for selecting either form for printing and hitting ok or cancel
        //the _checked() events on either PO or Abw should do nothing fancy in this case
        popPrint.IsOpen = true;
        return;
      }
      else
      {
        //otherwise, not expired, so "$2 customer reprint" radio is enabled as well as "administrative reprint" 
        //then when PO or Abw is checked, if "$2 customer reprint" is selected, then 

        MessageBoxResult expired_reprint = MessageBox.Show("Form is Expired:\rYes = Void & Create new replacement?\rNo = Administrative reprint",
          "Expired Form - Customer wants Replacement?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (expired_reprint == MessageBoxResult.Cancel) return;
        else if (expired_reprint == MessageBoxResult.Yes)
        {
          if (MessageBox.Show("Has the customer returned full package - All 3 copies of both PO and Abw?", "Existing paperwork accounted for?",
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
          {
            MessageBox.Show("Can't proceed with voiding this form until it's been fully returned.", "Abort", MessageBoxButton.OK, MessageBoxImage.Stop);
            return;
          }

          string SponsorGUID;
          string NewTaxFormGUID;
          decimal ServiceFee;
          taxform.VoidAndNew(out SponsorGUID, out NewTaxFormGUID, out ServiceFee);

          // if Sponsor is loaded, add this to the current shop list
          SponsorModel sponsor = SponsorModel.Lookup<SponsorModel>(SponsorGUID, false);
          if (sponsor != null)
          {
            TransactionList.TransactionItem reprint = new TransactionList.TransactionItem(sponsor.Transactions, "Reprint/Replacement", ServiceFee);
          }
          return;
        }

        //        MessageBox.Show("Expired form can't be (re)Printed,\ronly Voided and brand new Form sold.", "Expired Form", MessageBoxButton.OK, MessageBoxImage.Stop);

        /*else
        {
          ((CheckBox)sender).IsChecked = false;
          popPrint.IsOpen = false;
        }*/

      }

      popPrint.IsOpen = true;
    }

    private void btnConfirmPrint_Click(object sender, RoutedEventArgs e)
    {
      taxform.Print(
        (chkPrintPO.IsChecked.Value ? TaxFormModel.PackageComponent.OrderForm : 0) |
        (chkPrintAbw.IsChecked.Value ? TaxFormModel.PackageComponent.Abw : 0));
    }

    private void btnCancelPrint_Click(object sender, RoutedEventArgs e)
    {
      popPrint.IsOpen = false;
    }

  }

  #region Converters
  //the moment this logic is needed to be reused anywhere else, it's a good candidate for a ViewModel class that bundles both the User and TaxForm model inputs required to form the result
  public class LockToolTipConverter : WPFValueConverters.MarkupExtensionConverter, IMultiValueConverter
  {
    public LockToolTipConverter() {} //to avoid an XAML annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection bug.

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
    public LostLocationCodeToBool() { } //to avoid an XAML annoying warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection bug.

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
    public IsGiveBackToCustomerEnabled() { } //to avoid an XAML annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection bug.

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (WPFHelpers.DesignMode || values[0] == DependencyProperty.UnsetValue) return (false);
      TaxFormModel form = values[0] as TaxFormModel;
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
    public IsReturnFormEnabled() { } //to avoid an XAML annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection bug.

    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (WPFHelpers.DesignMode || values[0] == DependencyProperty.UnsetValue) return (false);
      TaxFormModel form = values[0] as TaxFormModel;
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
