using System;
using System.Data;
using System.Linq;
using System.Windows.Data;
using System.Text.RegularExpressions;
using System.Text;
using System.IO;

namespace iTRAACv2
{
  public class TaxFormModel : ModelBase
  {
    private BindingBase _TitleBinding = new Binding("Fields[OrderNumber]") { Mode = BindingMode.OneWay };
    public override BindingBase TitleBinding { get { return (_TitleBinding); } }

    public static event Action<string> FormStatusChangeCallback;

    public bool IsClass2 { get { return ("2,5,".Contains(Fields["FormTypeId"].ToString() + ",")); } }

    public bool IsLocked
    {
      get { return (_IsLocked); }
      set
      {
        if (value == false) ShowUserMessage("Remember: Unlock to make *CORRECTIONS* only (i.e. typos),\rNot *CHANGES* that _differ_ from the actual VAT form hardcopy.");
        _IsLocked = value;
        OnPropertyChanged("IsLocked");
      }
    }
    private bool _IsLocked = true;

    //public bool IsReturnEnabled
    //{
    //  get
    //  {
    //    return (!IsFormStatusClosed || !IsLocked);
    //  }
    //}

    private bool IsFormFromMyOffice { get { return (SettingsModel.TaxOfficeId == Fields.Field<int>("TaxOfficeId")); } }
    public bool IsFormStatusClosed { get { return (Fields.Field<StatusFlagsForm>("StatusFlags").HasAnyFlags(StatusFlagsForm.Filed | StatusFlagsForm.Voided)); } }

    static TaxFormModel()
    {
      if (WPFHelpers.DesignMode) return;

      //piggyback the TransactionType code table load with the TaxForm to ExtendedFields by TransactionType master-detail tables
      string[] TableNames = CacheTables("TaxForm_init"); //among other things, this query will initialize an empty "TaxForm" DataTable in dsCache ... since this is a static constructor which fires before everything else, it is safe to assume TaxFormTable property will be non null in all other contexts

      TransactionTypes = new DataView(dsCache.Tables["TransactionType"]);
      TableNames.Where(name => name.Left(8) == "taxform_"). //nugget:
        Select(ExtTableName => { dsCache.AddRelation(ExtTableName, TaxFormTable.Columns["RowGUID"], dsCache.Tables[ExtTableName].Columns["TaxFormGUID"]); return(false); } ).Count(); //nugget:
      //nugget: wrapping a void function in an inline method which returns something makes LINQ Select happy
      //nugget: make sure to include something like .Count() at the end to short circuit LINQ's deferred execution behavior

      TaxFormViolationTypes = new DataView(dsCache.Tables["RemarkType"]);
      TaxFormViolationTypes.RowFilter = "CategoryId = 'FORM_VIOLATION'";

      TaxFormPackageServiceFees = new DataView(dsCache.Tables["TaxFormPackageServiceFee"]);
      TaxFormPackageServiceFees.Sort = "FormTypeId, FormCount";

      TaxFormOrderNumberView = new DataView(TaxFormTable); TaxFormOrderNumberView.Sort = "OrderNumber";
    }

    static public decimal LookupPackageServiceFee(FormType formType, int Qty)
    {
      return (Convert.ToDecimal(TaxFormPackageServiceFees.FindRows(new object[] {(int)formType, Qty})[0]["TotalServiceFee"]));
    }

    // Note: once we wrap a Fields[] member with a real property like this, then we should avoid all direct access to this member via Fields
    // because then we'd have to fire OnPropertyChanged("Fields") and that seems like a fair bit of unnecessary notification traffic
    // this is probably the one place where a new coder could cut themselves on an "exposed sharp edge"... keeping it for now... decide whether it's worth it after it soaks in
    public bool IsVoided
    {
      get { return (Fields.Field<StatusFlagsForm>("StatusFlags").HasFlag(StatusFlagsForm.Voided)); }
      set
      {
        Fields["StatusFlags"] = Fields.Field<StatusFlagsForm>("StatusFlags").SetFlags(StatusFlagsForm.Voided, value);
        OnPropertyChanged("IsVoided");
      }
    }

    // Note: once we wrap a Fields[] member with a real property like this, then we should avoid all direct access to this member via Fields
    // because then we'd have to fire OnPropertyChanged("Fields") and that seems like a fair bit of unnecessary notification traffic
    // this is probably the one place where a new coder could cut themselves on an exposed sharp edge... keep it for now... decide whether it's worth it after it soaks in
    public bool IsIncomplete
    {
      get { return (Fields.Field<bool>("Incomplete")); }
      set
      {
        Fields["Incomplete"] = value;
        OnPropertyChanged("IsIncomplete");
      }
    }

    public bool IsViolation { get { return ((Fields == null) ? false : TaxFormRemarks.Cast<DataRowView>().Any(r => r.Field<string>("CategoryId") == "FORM_VIOLATION" && r["DeleteReason"].ToString() == "")); } }

    private double? TotalCost { get { return (Fields["TotalCost"] == DBNull.Value ? (double?)null : Convert.ToDouble(Fields["TotalCost"])); } }

    private string Currency { get { string c = Fields["CurrencyUsed"].ToString(); return (c == "1" ? "USD" : (c == "2") ? "EUR" : "{undefined}"); } }

    public bool IsTotalCostMinWarning { get { return (TotalCost <= SettingsModel.TaxFormTotalCostMin); } }
    public bool IsTotalCostMinWarningConfirmed { get; set; }

    public bool IsTotalCostMaxViolation { get { return (!IsViolation && TotalCost * ((Currency == "USD") ? SettingsModel.RoughUSDToEuroRate : 1) >= 2500); } }
    public bool IsTotalCostMaxViolationConfirmed { get; set; }

    static public DataView TransactionTypes { get; private set; }
    static public DataView TaxFormViolationTypes { get; private set; }
    static public DataView TaxFormPackageServiceFees { get; private set; }

    public DataView TaxFormRemarks { get; private set; }

    static private DataTable TaxFormTable { get { return (dsCache.Tables["TaxForm"]); } }
    static private DataView TaxFormOrderNumberView { get; set; }
    static private DataTable TaxFormRemarksTable { get { return (dsCache.Tables["TaxForm_Remark"]); } }

    public void GiveBackToCustomer()
    {
      Fields["LocationCode"] = "CUST";
      Fields["StatusFlags"] = Fields.Field<int>("StatusFlags") & ~(int)StatusFlagsForm.Returned;
    }

    public bool IsExpired { get {
      //if today is greater than the expiration date + grace period (30 days) 
      return (DateTime.Now > Fields.Field<DateTime>("ExpirationDate").AddDays(Convert.ToInt32(SettingsModel.Global["FormReprintGraceDays"]))); } }

    /// <summary>
    /// Extended fields for transaction types like Weapons & Vehicles
    /// </summary>
    public DataRowView ExtendedFields { get; private set; }

    /// <summary>
    /// </summary>
    /// <param name="FormType">NF1 or NF2</param>
    /// <param name="OfficeCode">HD, RA, MA, etc</param>
    /// <param name="FiscalYear2Digit">09, 10, etc</param>
    /// <param name="CtrlNumber">5 or 6 digits, less than 5 will be left padded with zeros</param>
    /// <returns></returns>
    static public string FormatOrderNumber(bool AlwaysShow, string FormType, string OfficeCode, string FiscalYear2Digit, string CtrlNumber)
    {
      return ((CtrlNumber == "" && !AlwaysShow) ? "" : String.Format("{0}-{1}-{2}-{3}", FormType, OfficeCode, FiscalYear2Digit,
        //this field is little weird, it's 5 digits, "0" padded to the left, but it can also be 6 digits :)
        //basically the original design assumed 5 digits but Ramstein has been going over the 99,999 mark
        ("00000" + CtrlNumber).Right(Math.Max(5, CtrlNumber.Length))
      ));
    }

    public bool ShowDeletedRemarks
    {
      get
      {
        return (TaxFormRemarks.RowFilter == "");
      }
      set
      {
        TaxFormRemarks.RowFilter = RemarkModel.ShowDeletedFilter(value);
      }
    }

    private string UserMessagePrefix { get { return ("[Tax Form #: " + Fields["OrderNumber"].ToString() + "] "); } }

    public void SetViolation(int RemarkTypeId, string Remarks)
    {
      RemarkModel.SaveNew(GUID, RemarkTypeId, Remarks);
      OnPropertyChanged("IsViolation");
      OnFormStatusChange();
    }

    public void RemoveRemark(DataRowView RemarkRow, string Reason)
    {
      RemarkModel.Remove(RemarkRow, Reason);
      OnPropertyChanged("IsViolation");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>The SponsorGUID for the new replacement form</returns>
    public void VoidAndNew(out string SponsorGUID, out string NewTaxFormGUID, out decimal ServiceFee)
    {
      using (Proc TaxForm_VoidAndNew = new iTRAACProc("TaxForm_VoidAndNew"))
      {
        TaxForm_VoidAndNew["@VoidTaxFormGUID"] = GUID;
        SponsorGUID = TaxForm_VoidAndNew["@SponsorGUID"].ToString();
        NewTaxFormGUID = TaxForm_VoidAndNew["@NewTaxFormGUID"].ToString();
        ServiceFee = Convert.ToDecimal(TaxForm_VoidAndNew["@NewTaxFormGUID"]);
      }
    }

    public bool ReturnForm(bool IsClosing = false, bool DisplaySuccess = true)
    {
      Save();

      using (Proc TaxForm_ReturnFile = new iTRAACProc("TaxForm_ReturnFile"))
      {
        TaxForm_ReturnFile["@TaxFormGUID"] = GUID;
        TaxForm_ReturnFile["@File"] = false;
        if (!TaxForm_ReturnFile.ExecuteDataSet(UserMessagePrefix + "Return - ", true)) return (false);

        //if we're firing Returned and this object is remaining visible (i.e. user didn't select the Return-and-Close button) then pull back the updated values reflecting the Returned status change
        if (!IsClosing)
        {
          CacheTables(TaxForm_ReturnFile);
          OnPropertyChanged("Fields"); //nugget: amazingly inconsistent IMHO, DataTable.Merge() does *not* fire DataRowView.PropertyChanged events!?! (tsk tsk Microsoft!!) 
        }
      }

      OnFormStatusChange();
      return (true);
    }

    private bool Validate(bool IsFiling = false)
    {
      //Validate will be called for the initial NF2 printing scenario where all fields except Description and DateUsed must filled
      // -OR- the final filing scenario where all fields must be populated 

      //needed to structure this as individual validation method calls otherwise execution short circuits on the first false
      //and it's naturally preferable to show all the little red validation boxes at once rather than making the user continue hitting submit to discover each new error
      bool IsValid = true;
      if (IsFiling) Validate_Generic(ref IsValid, "UsedDate");
      Validate_Generic(ref IsValid, "TransTypeID", "'?' != '2'");
      Validate_Generic(ref IsValid, "Vendor");
      Validate_Generic(ref IsValid, "CurrencyUsed");
      Validate_Generic(ref IsValid, "TotalCost");
      //description can't be mandatory, too much for clerk to think about... we'll just get garbage: if (IsFiling) Validate_Generic(ref IsValid, "Description");

      //note: we have a chronic amount of rediculously small and over 2500 NF1's ... trying to plug that hole...

      //make sure user meant to enter small total cost
      if (IsValid && IsTotalCostMinWarning && !IsTotalCostMinWarningConfirmed)
        Validate_Generic(ref IsValid, "TotalCost", "false", "Are you sure Total Cost is that small?");

      //make sure user meant to enter a >2500 Euro total cost... and create appropriate violation records if so...
      if (IsValid && !IsClass2 && IsTotalCostMaxViolation && !IsTotalCostMaxViolationConfirmed)
        Validate_Generic(ref IsValid, "TotalCost", "false", "Total Cost greater than 2499 Euros.\nIs this really a violation?");

      return (IsValid);
    }

    public bool FileForm(bool IsClosing = false)
    {
      //the moment we're attempting to file a form means it's physically at this office
      Fields["LocationCode"] = SettingsModel.TaxOfficeCode;

      bool isvalid = Validate(IsFiling: true);
      IsIncomplete = !isvalid; //right at the time of testing validity to completely "File" is where we flip the incomplete flag on
      //while tempting, don't move this "IsIncomplete" line into Validate() because that would create a cyclic dependency between the two values in LoadFields()
      if (!isvalid)
      {
        ShowUserMessage("Please correct highlighted fields");
        return (false);
      }

      //we're leaving the form IsReadOnly=false for the convenience of immediate typo type edits while it's still open, it will be safely locked (IsReadOnly=true) the next time it's opened

      if (SaveMe(IsFiling: true, UpdateSponsor: false)) //don't update sponsor because we're going to do that next here below
      {
        if (isvalid && IsTotalCostMaxViolation) 
          SetViolation(10, String.Format("Total Cost: {0} {1}", TotalCost, Currency)); //this also calls OnFormStatusChange() to update Sponsor UI
        else 
          OnFormStatusChange();
      }
      else return (false);

      if (!IsClosing)
        OnPropertyChanged("Fields");

      return (true);
    }

    public TaxFormPackage ParentPackage = null;
    static public TaxFormModel NewNF2(TransactionList ParentTransactionList, string SponsorGUID, string SponsorName,
      string AuthorizedDependentClientGUID, string AuthorizedDependentName)
    {
      TaxFormPackage pkg = new TaxFormPackage(
        ParentTransactionList: ParentTransactionList,
        IsPending: true, 
        SponsorGUID: SponsorGUID,
        AuthorizedDependentClientGUID: AuthorizedDependentClientGUID, 
        FormType: FormType.NF2, Qty: 1);

      TaxFormModel frm = Lookup<TaxFormModel>(Guid.Empty.ToString()); //empty guid triggers new form logic in sproc
      frm.ParentPackage = pkg;
      frm.Fields["SponsorGUID"] = SponsorGUID;
      frm.Fields["SponsorName"] = SponsorName;
      frm.Fields["AuthorizedDependent"] = AuthorizedDependentName;

      return (frm);
    }

    #region DataAccess

    protected override string AltLookup(string AltId)
    {
      DataRowView[] forms = TaxFormOrderNumberView.FindRows(AltId);
      if (forms.Length > 0) return (forms[0]["RowGUID"].ToString());
      return (null);
    }

    protected override void LoadSubClass()
    {
      Fields = EntityLookup(GUID, "TaxForm", "TaxForm_s");
      GUID = Fields["RowGUID"].ToString(); //for the case where TaxForm_s can lookup on OrderNumber as well as GUID
      //currently comes back with the following tables loaded: TaxForm, (optional) TaxForm_Weapon or (optional) TaxForm_Vehicle

      Fields.PropertyChanged += FieldChange;
      SetExtendedFields();

      _IsLocked = IsFormStatusClosed || !IsFormFromMyOffice;

      //create the logical lookup field for "Location" via TaxForm.LocationCode to TaxOffice.TaxOfficeCode
      dsCache.AddRelation("Location", TaxOfficeModel.TaxOfficeTable.Columns["OfficeCode"], TaxFormTable.Columns["LocationCode"], false);
      if (!TaxFormTable.Columns.Contains("Location"))
        TaxFormTable.Columns.Add("Location", typeof(string), "Parent(Location).Office");

      //create the TaxForm_Remark relationship 
      dsCache.AddRelation("TaxForm_Remark", TaxFormTable.Columns["RowGUID"], TaxFormRemarksTable.Columns["FKRowGUID"], false);
      TaxFormRemarks = Fields.CreateChildView("TaxForm_Remark");
      ShowDeletedRemarks = false;
      RemarkModel.CommonRemarkTableSettings(TaxFormRemarks);
      TaxFormRemarks.Table.RowChanged += TaxFormRemarks_RowChanged;

      if (IsIncomplete) Validate(); //flip on all the red boxes so the user is prompted to resolve right away
    }

    void TaxFormRemarks_RowChanged(object sender, DataRowChangeEventArgs e)
    {
      if (e.Row["FKRowGUID"].ToString() == GUID) IsModified = true;
    }

    private void FieldChange(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      switch (e.PropertyName)
      {
        case "TransTypeID": SetExtendedFields(); break;

        case "TotalCost":
          OnPropertyChanged("IsTotalCostMinWarning");
          OnPropertyChanged("IsTotalCostMaxViolation");
          break;

      }
    }

    private void OnFormStatusChange()
    {
      if (FormStatusChangeCallback != null) FormStatusChangeCallback(Fields["SponsorGUID"].ToString());
    }

    public override string WhatIsModified
    {
      get
      {
        return (
          (Fields.IsDirty() ? "\n   * Taxform edits" : null) +
          (TaxFormRemarks.IsDirty(RespectRowFilter: false) ? "\n   * Remarks edits" : null)
        );
      }
    }

    //public override bool IsModified
    //{
    //  get
    //  {
    //    return (base.IsModified || ExtendedFields.IsDirty() || TaxFormRemarks.IsDirty());
    //  }
    //}

    protected override void UnLoadSubClass()
    {
      if (ParentPackage != null) ParentPackage.Rollback(); //only happens under NF2 scenario

      if (TaxFormRemarks != null) TaxFormRemarks.Table.RowChanged -= TaxFormRemarks_RowChanged;
      TaxFormRemarks.DetachRowsAndDispose();

      //blow away child row first to avoid the 'missing foreign to primary key' exception from the dataset police
      if (ExtendedFields != null) ExtendedFields.PropertyChanged -= ExtendedFields_PropertyChanged;
      ExtendedFields.DetachRow();

      if (Fields != null) Fields.PropertyChanged -= FieldChange;
      Fields.DetachRow();
    }

    protected override bool SaveSubClass()
    {
      return (SaveMe()); //so that we can support having a default parm (not something we want to force all subclasses to implement from the base class declaration)
    }

    protected bool SaveMe(bool IsFiling = false, bool UpdateSponsor = true)
    {
      // generally, inserts and deletes will be edge cases handled elsewhere, modified fields on existing rows are the primary save scenario for child lists hanging off main entities like TaxForm (e.g. TaxFormRemarks)

      //pulling back on this for now... seems theoretically possible to save a pending NF2 with un-printed status just like NF1's, it's not "issued" until it's printed
      //if (IsClass2 && !Validate())
      //{
      //  ShowUserMessage("It is invalid to save an NF2/EF2 that's not fully completed.");
      //  return (false);
      //}

      if (Fields.IsDirty()) //the "Fields" logic does actually need to be here if called from FileForm() rather than base.Save();
      {
        using (iTRAACProc TaxForm_u = new iTRAACProc("TaxForm_u"))
        {
          TaxForm_u.AssignValues(Fields);
          TaxForm_u["@IsFiling"] = IsFiling;
          if (!TaxForm_u.ExecuteDataSet(UserMessagePrefix, false)) return (false);
          Fields.AcceptChanges();
          if (IsFiling) CacheTables(TaxForm_u);
        }

        if (UpdateSponsor) OnFormStatusChange();
      }

      if (ExtendedFields.IsDirty())
      {
        using (Proc TaxForm_TransactionTypeExt_u = new Proc("TaxForm_TransactionTypeExt_u"))
        {
          TaxForm_TransactionTypeExt_u.AssignValues(ExtendedFields);
          TaxForm_TransactionTypeExt_u["@TaxFormGUID"] = GUID;
          if (!TaxForm_TransactionTypeExt_u.ExecuteNonQuery(UserMessagePrefix, false)) return (false);
        }
        ExtendedFields.AcceptChanges();
      }

      RemarkModel.SaveRemarks("FKRowGUID", GUID, UserMessagePrefix, TaxFormRemarks);

      return (true);
    }

    private void SetExtendedFields()
    {
      if (ExtendedFields != null)
      {
        ExtendedFields.PropertyChanged -= ExtendedFields_PropertyChanged;
        ExtendedFields.DetachRow ();
      }

      string extable = GetCurrentTransactionTypeExtendedTableName();

      if (dsCache.Relations.Contains(extable)) //if this is a transaction type with extended fields required...
      {
        DataView ExtFields = Fields.CreateChildView(extable);

        if (ExtFields.Count == 0) //if no row already exists, create a default row for this tax form and transaction type to provide the empty slot for fields to be filled out
        {
          DataTable extTable = dsCache.Tables[extable];

          //it'd be cool to break this out into a generic AddBlankRow() function that populates a GUID PK if present and cruises the column types to create blank default values
          DataRow extrow = extTable.NewRow();
          DataColumnCollection cols = extTable.Columns;
          foreach (DataColumn col in cols) if (!col.ColumnName.Contains("GUID")) extrow[col.ColumnName] = ""; //fill out all non GUID fields generically to dodge not null errors
          extrow["RowGUID"] = Guid.NewGuid();
          extrow["TaxFormGUID"] = GUID;
          extTable.Rows.Add(extrow);
          extrow.AcceptChanges();
        }

        ExtendedFields = ExtFields[0];
        ExtendedFields.PropertyChanged += ExtendedFields_PropertyChanged;
      }

      OnPropertyChanged("ExtendedFields");  //let the UI know to update the extended fields display when we change TransactionType

    }

    void ExtendedFields_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      IsModified = true;
    }

    private string GetCurrentTransactionTypeExtendedTableName()
    {
      string TransTypeId = Fields["TransTypeID"].ToString();
      if (TransTypeId == "") return (null);
      DataRowView[] drv = TransactionTypes.Table.DefaultView.FindRows(TransTypeId);
      string ext = drv[0]["ExtendedFieldsCode"].ToString().ToLower();
      return ((ext != "") ? "taxform_" + ext : null);
    }

    //private DataRelation[] TransactionTypeRelations { get { return (dsCache.Relations.Cast<DataRelation>().Where(rel => rel.ParentTable.TableName == "taxform" && rel.RelationName.Left(8) == "taxform_").ToArray<DataRelation>()); } }

    #endregion

    //todo: there's got to be a more appropriate name for this... but "FormType" is already taken by NF1, NF2, EF1, EF2, etc.
    [Flags]
    public enum PackageComponent { OrderForm = 1 << 0, Abw = 1 << 1 } //the stored proc depends on these ints

    [Flags]
    public enum StatusFlagsForm { Issued = 1 << 0, Returned = 1 << 1, Filed = 1 << 2, Reissued = 1 << 3, Amended = 1 << 4, Voided = 1 << 5}

    public void Print(PackageComponent PrintComponent)
    {
      if (!SaveMe() || (IsClass2 && !Validate())) return;
      TaxFormModel.Print(PrintComponent, GUID);
      OnPropertyChanged("Fields"); //to twiddle the InitPrtAbw field that is presumably bound to an "Un-Printed" type visual alert
    }


    static public void ResetPrinterSettings(PackageComponent PrintComponent)
    {
      //this routine sends a binary payload that resets the Epson FX-890 printer back to desirable defaults (listed below)

      //nugget: embedding a binary file in the assembly for runtime retrieval
      //http://www.dotnetscraps.com/dotnetscraps/post/Insert-any-binary-file-in-C-assembly-and-extract-it-at-runtime.aspx
      //the other main thing to do is set the file's properties to "Embedded Resource"

      //reset_epson.bin is generated via "Epson Remote Configuration Manager" 
      //download here: http://www.epson.com/cgi-bin/Store/support/supDetail.jsp?BV_UseBVCookie=yes&oid=23730&infoType=Downloads
      //(make sure to run it from a CMD.EXE with %TEMP% set to an accessible 8.3 path, long path names screw up the setup.exe since it's from the Win9x days, but it does still work on Win7!)
      //it's a Windows app that bundles all the special settings and fires them at the printer
      //i could find no other way to select "Normal Draft" out of USD, HSD, Draft
      //this binary file also sets the top-of-form position to the smallest value (0.167)
      //and defaults the pitch to 12cpi

      using(Stream reset_epson = AssemblyHelper.GetEmbeddedResource("reset_epson.bin"))
      {
        RawPrinterHelper.SendStreamToPrinter(
          SettingsModel.Local[PrintComponent == PackageComponent.OrderForm ? "POPrinter" : "AbwPrinter"],
          reset_epson);
      }
    }

    static public void PrintTest(PackageComponent PrintComponent)
    {
      Print(PrintComponent, Guid.Empty.ToString());

      /*this is a good place to jump in and do some test logic...
      CharacterPrinter POPrinter = new CharacterPrinter(
        Setting.Global["FormPrinterInitCodes"],
        Convert.ToInt16(Setting.Global["FormPrinterWidth"]),
        Convert.ToInt16(Setting.Global["FormPrinterHeight"]));

      //for (int i = 128; i < 178; i++) test1 += new string((char)i, 1);
      //POPrinter.PrintStringAtPos(test2, 10, 11);

      POPrinter.PrintStringAtPos("A: Ää E: Ëë I: Ïï O: Öö U: Üü Y: Ÿÿ SS: ß Euro: €", 10, 10);

      POPrinter.SendToPrinter(Setting.Local["POPrinter"]);
      */
    }

    static private bool PrintOK()
    {
      if (String.IsNullOrEmpty(SettingsModel.Local["POPrinter"]) || String.IsNullOrEmpty(SettingsModel.Local["AbwPrinter"]))
      {
        ShowUserMessage("Dot Matrix printers have not been assigned yet.");
        return (false);
      }
      return (true);
    }

    static private bool Print(PackageComponent PrintComponents, string guid)
    {
      if (!PrintOK()) return(false);

      //nugget: sending raw ESCape codes to dot matrix printer (Epson's "ESC/P" standard)
      //nugget: ESC/P reference manual: http://files.support.epson.com/pdf/general/escp2ref.pdf
      //ESC @   = reset
      //ESC 3 n = n/216" line spacing
      //ESC M   = 12 cpi
      //ESC x n = quality: 0 = Draft, 1 = NLQ
      //ESC k n = font: 0 = Roman, 1 = Sans serif font
      //ESC p 0 = turn off proportional
      //ESC 2   = 1/6" line spacing = 0.166666666666666

      using (Proc TaxForm_print = new Proc("TaxForm_print"))
      {
        TaxForm_print["@TaxFormGUID"] = guid; //for testprint, pass Guid.Empty i.e. '00000000-0000-0000-0000-000000000000'
        TaxForm_print["@PrintComponent"] = (int)PrintComponents;

        TaxForm_print.ExecuteDataSet();

        RawCharacterPage POPrinter = new RawCharacterPage(
          SettingsModel.Global["FormPrinterInitCodes"],
          Convert.ToInt16(SettingsModel.Global["FormPrinterWidth"]),
          Convert.ToInt16(SettingsModel.Global["FormPrinterHeight"]));

        if (guid == Guid.Empty.ToString()) POPrinter.PrintTestRulers();
        RawCharacterPage AbwPrinter = POPrinter.Clone();

        if (PrintFields(POPrinter, TaxForm_print.dataSet.Tables[0].Rows))
          POPrinter.SendToPrinter(SettingsModel.Local["POPrinter"]);

        if (PrintFields(AbwPrinter, TaxForm_print.dataSet.Tables[1].Rows))
          POPrinter.SendToPrinter(SettingsModel.Local["AbwPrinter"]);

        CacheTables(TaxForm_print, "TaxForm"); //sync the updated PrintDate columns in the TableCache
      }
      return (true);
    }

    static private bool PrintFields(RawCharacterPage printer, DataRowCollection rows)
    {
      if (rows.Count == 0) return (false);

      foreach (DataRow r in rows)
      {
        printer.PrintStringAtPos(r["Data"].ToString(), (int)r["col"], (int)r["row"], (int)r["MaxLength"], (int)r["MaxRows"]);
      }
      return (true);
    }

    public enum FormType { NF1=1, NF2=2, EF1=4, EF2=5 };

    public class TaxFormPackage : TransactionList.TransactionItem
    {
      //inputs
      //public bool Pending (TransactionItem baseclass)
      //public string Description (TransactionItem baseclass)
      public string SponsorGUID { get; private set; }
      public string AuthorizedDependentClientGUID { get; private set; }
      public TaxFormModel.FormType FormType { get; private set; }
      public int Qty { get; private set; }

      //outputs
      //public double Price (TransactionItem baseclass)
      //public string GUID (TransactionItem baseclass)
      public string PackageCode { get; private set; }

      public TaxFormPackage(TransactionList ParentTransactionList, bool IsPending, string SponsorGUID, 
        string AuthorizedDependentClientGUID, TaxFormModel.FormType FormType, int Qty) : base(ParentTransactionList)
      {
        //sanity check: if there are already printed forms in the shopping cart, everything else must be printed during this session... too complicated to manage otherwise
        if (IsPending && ParentTransactionList.Any(t => !t.IsPending))
        {
          ShowUserMessage("Since forms have already been printed.\rAll subsequent forms must also be printed.\rPlease select 'Print Immediately' only.");
          return;
        }

        //combine all NF1's requested during this customer session into one bundle
        TaxFormPackage existingNF1Package = ParentTransactionList.OfType<TaxFormPackage>().Where(w => w.FormType == TaxFormModel.FormType.NF1 /*&& w.IsPending*/).FirstOrDefault(); //realized, filtering on IsPending is counterproductive. An NF1 package will only be in this list if generated during this customer "session" and therefore the total qty of forms is what should factor into the qty discount calc
        if (existingNF1Package != null)
        {
          Qty += existingNF1Package.Qty; //add the existing quantity to this new package...
          this.PackageCode = existingNF1Package.PackageCode; //to facilitate adding these new forms to a previously printed package
          IsPending = existingNF1Package.IsPending; //pending status must be same as whatever was already initiated
        }

        PendingAction = "Print";

        this.IsPending = IsPending;
        this.Description = String.Format("New {0} Package {1}", Enum.GetName(typeof(TaxFormModel.FormType), FormType), FormType == TaxFormModel.FormType.NF1 ? "(" + Qty.ToString() + ")" : ""); //Extensions.Pluralize("{count} New {0} Form{s}", Qty, Enum.GetName(typeof(TaxForm.FormType), FormType));
        this.SponsorGUID = SponsorGUID;
        this.AuthorizedDependentClientGUID = AuthorizedDependentClientGUID;
        this.FormType = FormType;
        this.Qty = Qty;
        this.Price = LookupPackageServiceFee(FormType, Qty);


        if (!IsPending)
        {
          new System.Threading.Thread(delegate(object existingPackage) //pop off a background thread to create the records
          {
            if (Execute())
            {
              //hit the DB to create all the new Package/Forms records
              if (existingPackage != null)
                ParentTransactionList.Remove(existingPackage as TaxFormPackage); //delete the old package
              AfterConstructor();
            }
          }).Start(existingNF1Package);
        }

      }

      public override bool Execute()
      {
        if (!PrintOK()) return (false);

        string[] TaxFormGUIDs;
        using (iTRAACProc TaxFormPackage_New = new iTRAACProc("TaxFormPackage_New"))
        {
          TaxFormPackage_New["@PackageCode"] = PackageCode; //for merging multiple requests for the same form type during this same customer session
          TaxFormPackage_New["@FormTypeID"] = Convert.ToInt32(FormType);
          TaxFormPackage_New["@FormCount"] = Qty;
          TaxFormPackage_New["@SponsorGUID"] = SponsorGUID;
          TaxFormPackage_New["@ClientGUID"] = AuthorizedDependentClientGUID;
          TaxFormPackage_New["@Pending"] = IsPending;

          TaxFormPackage_New.ExecuteNonQuery();

          this.Price = (decimal)TaxFormPackage_New["@ServiceFee"];
          this.GUID = TaxFormPackage_New["@TaxFormPackageGUID"].ToString();
          this.PackageCode = TaxFormPackage_New["@PackageCode"].ToString();
          TaxFormGUIDs = TaxFormPackage_New["@TaxFormGUIDs"].ToString().Split(',');

          this.IsPending = false;
          NotifyPropertyChanged("IsPending");
        }

        foreach (string TaxFormGUID in TaxFormGUIDs)
        {
          if (!Print(PackageComponent.OrderForm | PackageComponent.Abw, TaxFormGUID))
            return(false);
        }

        TaxFormModel.FormStatusChangeCallback(SponsorGUID);

        return (true);
      }

    }

  }
}