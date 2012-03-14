using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace iTRAACv2.Model
{
  public class TaxFormModel : ModelBase
  {
    private readonly BindingBase _titleBinding = new Binding("Fields[OrderNumber]") { Mode = BindingMode.OneWay };
    public override BindingBase TitleBinding { get { return (_titleBinding); } }

    public static event Action<string> FormStatusChangeCallback;

    public bool IsClass2 { get { return ("2,5,".Contains(Fields["FormTypeId"] + ",")); } }

    public bool IsLocked
    {
      get { return (_isLocked); }
      set
      {
        if (value == false) ShowUserMessage("Remember: Unlock to make *CORRECTIONS* only (i.e. typos),\rNot *CHANGES* that _differ_ from the actual VAT form hardcopy.");
        _isLocked = value;
        OnPropertyChanged("IsLocked");
      }
    }
    private bool _isLocked = true;

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
      var tableNames = CacheTables("TaxForm_init"); //among other things, this query will initialize an empty "TaxForm" DataTable in dsCache ... since this is a static constructor which fires before everything else, it is safe to assume TaxFormTable property will be non null in all other contexts

      TransactionTypes = new DataView(DsCache.Tables["TransactionType"]);
      tableNames.Where(name => name.Left(8) == "taxform_"). //nugget:
// ReSharper disable ReturnValueOfPureMethodIsNotUsed
        Select(extTableName => { DsCache.AddRelation(extTableName, TaxFormTable.Columns["RowGUID"], DsCache.Tables[extTableName].Columns["TaxFormGUID"]); return(false); } ).Count(); //nugget:
// ReSharper restore ReturnValueOfPureMethodIsNotUsed
      //nugget: wrapping a void function in an inline method which returns something makes LINQ Select happy
      //nugget: make sure to include something like .Count() at the end to short circuit LINQ's deferred execution behavior

      TaxFormViolationTypes = new DataView(DsCache.Tables["RemarkType"]) {RowFilter = "CategoryId = 'FORM_VIOLATION'"};

      TaxFormPackageServiceFees = new DataView(DsCache.Tables["TaxFormPackageServiceFee"])
                                    {Sort = "FormTypeId, FormCount"};

      TaxFormOrderNumberView = new DataView(TaxFormTable) {Sort = "OrderNumber"};
    }

    static public decimal LookupPackageServiceFee(FormType formType, int qty)
    {
      return (Convert.ToDecimal(TaxFormPackageServiceFees.FindRows(new object[] {(int)formType, qty})[0]["TotalServiceFee"]));
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

    public bool IsViolation { get { return (Fields != null && TaxFormRemarks.Cast<DataRowView>().Any(r => r.Field<string>("CategoryId") == "FORM_VIOLATION" && r["DeleteReason"].ToString() == "")); } }

    private double? TotalCost { get { return (Fields["TotalCost"] == DBNull.Value ? (double?)null : Convert.ToDouble(Fields["TotalCost"])); } }

    private string Currency { get { var c = Fields["CurrencyUsed"].ToString(); return (c == "1" ? "USD" : (c == "2") ? "EUR" : "{undefined}"); } }

    public bool IsTotalCostMinWarning { get { return (TotalCost <= SettingsModel.TaxFormTotalCostMin); } }
    public bool IsTotalCostMinWarningConfirmed { get; set; }

    public bool IsTotalCostMaxViolation { get { return (!IsViolation && TotalCost * ((Currency == "USD") ? SettingsModel.RoughUSDToEuroRate : 1) >= 2500); } }
    public bool IsTotalCostMaxViolationConfirmed { get; set; }

    static public DataView TransactionTypes { get; private set; }
    static public DataView TaxFormViolationTypes { get; private set; }
    static public DataView TaxFormPackageServiceFees { get; private set; }

    public DataView TaxFormRemarks { get; private set; }

    static private DataTable TaxFormTable { get { return (DsCache.Tables["TaxForm"]); } }
    static private DataView TaxFormOrderNumberView { get; set; }
    static private DataTable TaxFormRemarksTable { get { return (DsCache.Tables["TaxForm_Remark"]); } }

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
    /// <param name="alwaysShow"> </param>
    /// <param name="formType">NF1 or NF2</param>
    /// <param name="officeCode">HD, RA, MA, etc</param>
    /// <param name="fiscalYear2Digit">09, 10, etc</param>
    /// <param name="ctrlNumber">5 or 6 digits, less than 5 will be left padded with zeros</param>
    /// <returns></returns>
    static public string FormatOrderNumber(bool alwaysShow, string formType, string officeCode, string fiscalYear2Digit, string ctrlNumber)
    {
      return ((ctrlNumber == "" && !alwaysShow) ? "" : String.Format("{0}-{1}-{2}-{3}", formType, officeCode, fiscalYear2Digit,
        //this field is little weird, it's 5 digits, "0" padded to the left, but it can also be 6 digits :)
        //basically the original design assumed 5 digits but Ramstein has been going over the 99,999 mark
        ("00000" + ctrlNumber).Right(Math.Max(5, ctrlNumber.Length))
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

    private string UserMessagePrefix { get { return ("[Tax Form #: " + Fields["OrderNumber"] + "] "); } }

    public void SetViolation(int remarkTypeId, string remarks)
    {
      RemarkModel.SaveNew(GUID, remarkTypeId, remarks);
      OnPropertyChanged("IsViolation");
      OnFormStatusChange();
    }

    public void RemoveRemark(DataRowView remarkRow, string reason)
    {
      RemarkModel.Remove(remarkRow, reason);
      OnPropertyChanged("IsViolation");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>The SponsorGUID for the new replacement form</returns>
    public void VoidAndNew(out string sponsorGUID, out string newTaxFormGUID, out decimal serviceFee)
    {
      using (Proc taxFormVoidAndNew = new iTRAACProc("TaxForm_VoidAndNew"))
      {
        taxFormVoidAndNew["@VoidTaxFormGUID"] = GUID;
        sponsorGUID = taxFormVoidAndNew["@SponsorGUID"].ToString();
        newTaxFormGUID = taxFormVoidAndNew["@NewTaxFormGUID"].ToString();
        serviceFee = Convert.ToDecimal(taxFormVoidAndNew["@NewTaxFormGUID"]);
      }
    }

    public bool ReturnForm(bool isClosing = false, bool displaySuccess = true)
    {
      Save();

// ReSharper disable InconsistentNaming
      using (Proc TaxForm_ReturnFile = new iTRAACProc("TaxForm_ReturnFile"))
// ReSharper restore InconsistentNaming
      {
        TaxForm_ReturnFile["@TaxFormGUID"] = GUID;
        TaxForm_ReturnFile["@File"] = false;
        if (!TaxForm_ReturnFile.ExecuteDataSet(UserMessagePrefix + "Return - ", true)) return (false);

        //if we're firing Returned and this object is remaining visible (i.e. user didn't select the Return-and-Close button) then pull back the updated values reflecting the Returned status change
        if (!isClosing)
        {
          CacheTables(TaxForm_ReturnFile);
          OnPropertyChanged("Fields"); //nugget: amazingly inconsistent IMHO, DataTable.Merge() does *not* fire DataRowView.PropertyChanged events!?! (tsk tsk Microsoft!!) 
        }
      }

      OnFormStatusChange();
      return (true);
    }

    private bool Validate(bool isFiling = false)
    {
      //Validate will be called for the initial NF2 printing scenario where all fields except Description and DateUsed must filled
      // -OR- the final filing scenario where all fields must be populated 

      //needed to structure this as individual validation method calls otherwise execution short circuits on the first false
      //and it's naturally preferable to show all the little red validation boxes at once rather than making the user continue hitting submit to discover each new error
      var isValid = true;
      if (isFiling) ValidateGeneric(ref isValid, "UsedDate");
      ValidateGeneric(ref isValid, "TransTypeID", "'?' != '2'");
      ValidateGeneric(ref isValid, "Vendor");
      ValidateGeneric(ref isValid, "CurrencyUsed");
      ValidateGeneric(ref isValid, "TotalCost");
      //description can't be mandatory, too much for clerk to think about... we'll just get garbage: if (IsFiling) Validate_Generic(ref IsValid, "Description");

      //note: we have a chronic amount of rediculously small and over 2500 NF1's ... trying to plug that hole...

      //make sure user meant to enter small total cost
      if (isValid && IsTotalCostMinWarning && !IsTotalCostMinWarningConfirmed)
        ValidateGeneric(ref isValid, "TotalCost", "false", "Are you sure Total Cost is that small?");

      //make sure user meant to enter a >2500 Euro total cost... and create appropriate violation records if so...
      if (isValid && !IsClass2 && IsTotalCostMaxViolation && !IsTotalCostMaxViolationConfirmed)
        ValidateGeneric(ref isValid, "TotalCost", "false", "Total Cost greater than 2499 Euros.\nIs this really a violation?");

      return (isValid);
    }

    public bool FileForm(bool isClosing = false)
    {
      //the moment we're attempting to file a form means it's physically at this office
      Fields["LocationCode"] = SettingsModel.TaxOfficeCode;

      var isvalid = Validate(isFiling: true);
      IsIncomplete = !isvalid; //right at the time of testing validity to completely "File" is where we flip the incomplete flag on
      //while tempting, don't move this "IsIncomplete" line into Validate() because that would create a cyclic dependency between the two values in LoadFields()
      if (!isvalid)
      {
        ShowUserMessage("Please correct highlighted fields");
        return (false);
      }

      //we're leaving the form IsReadOnly=false for the convenience of immediate typo type edits while it's still open, it will be safely locked (IsReadOnly=true) the next time it's opened

      if (SaveMe(isFiling: true, updateSponsor: false)) //don't update sponsor because we're going to do that next here below
      {
        if (IsTotalCostMaxViolation) 
          SetViolation(10, String.Format("Total Cost: {0} {1}", TotalCost, Currency)); //this also calls OnFormStatusChange() to update Sponsor UI
        else 
          OnFormStatusChange();
      }
      else return (false);

      if (!isClosing)
        OnPropertyChanged("Fields");

      return (true);
    }

    public TaxFormPackage ParentPackage;
    static public TaxFormModel NewNF2(TransactionList parentTransactionList, string sponsorGUID, string sponsorName,
      string authorizedDependentClientGUID, string authorizedDependentName)
    {
      // ReSharper disable RedundantArgumentName
      var pkg = new TaxFormPackage(
        parentTransactionList: parentTransactionList,
        isPending: true, 
        sponsorGUID: sponsorGUID,
        authorizedDependentClientGUID: authorizedDependentClientGUID, 
        formType: FormType.NF2, qty: 1);
      // ReSharper restore RedundantArgumentName

      var frm = Lookup<TaxFormModel>(Guid.Empty.ToString()); //empty guid triggers new form logic in sproc
      frm.ParentPackage = pkg;
      frm.Fields["SponsorGUID"] = sponsorGUID;
      frm.Fields["SponsorName"] = sponsorName;
      frm.Fields["AuthorizedDependent"] = authorizedDependentName;

      return (frm);
    }

    #region DataAccess

    protected override string AltLookup(string altId)
    {
      var forms = TaxFormOrderNumberView.FindRows(altId);
      return forms.Length > 0 ? (forms[0]["RowGUID"].ToString()) : (null);
    }

    protected override void LoadSubClass()
    {
      Fields = EntityLookup(GUID, "TaxForm", "TaxForm_s");
      GUID = Fields["RowGUID"].ToString(); //for the case where TaxForm_s can lookup on OrderNumber as well as GUID
      //currently comes back with the following tables loaded: TaxForm, (optional) TaxForm_Weapon or (optional) TaxForm_Vehicle

      Fields.PropertyChanged += FieldChange;
      SetExtendedFields();

      _isLocked = IsFormStatusClosed || !IsFormFromMyOffice;

      //create the logical lookup field for "Location" via TaxForm.LocationCode to TaxOffice.TaxOfficeCode
      DsCache.AddRelation("Location", TaxOfficeModel.TaxOfficeTable.Columns["OfficeCode"], TaxFormTable.Columns["LocationCode"], false);
      if (!TaxFormTable.Columns.Contains("Location"))
        TaxFormTable.Columns.Add("Location", typeof(string), "Parent(Location).Office");

      //create the TaxForm_Remark relationship 
      DsCache.AddRelation("TaxForm_Remark", TaxFormTable.Columns["RowGUID"], TaxFormRemarksTable.Columns["FKRowGUID"], false);
      TaxFormRemarks = Fields.CreateChildView("TaxForm_Remark");
      ShowDeletedRemarks = false;
      RemarkModel.CommonRemarkTableSettings(TaxFormRemarks);
      TaxFormRemarks.Table.RowChanged += TaxFormRemarksRowChanged;

      if (IsIncomplete) Validate(); //flip on all the red boxes so the user is prompted to resolve right away
    }

    void TaxFormRemarksRowChanged(object sender, DataRowChangeEventArgs e)
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
          (TaxFormRemarks.IsDirty(respectRowFilter: false) ? "\n   * Remarks edits" : null)
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

      if (TaxFormRemarks != null) TaxFormRemarks.Table.RowChanged -= TaxFormRemarksRowChanged;
      TaxFormRemarks.DetachRowsAndDispose();

      //blow away child row first to avoid the 'missing foreign to primary key' exception from the dataset police
      if (ExtendedFields != null) ExtendedFields.PropertyChanged -= ExtendedFieldsPropertyChanged;
      ExtendedFields.DetachRow();

      if (Fields != null) Fields.PropertyChanged -= FieldChange;
      Fields.DetachRow();
    }

    protected override bool SaveSubClass()
    {
      return (SaveMe()); //so that we can support having a default parm (not something we want to force all subclasses to implement from the base class declaration)
    }

    protected bool SaveMe(bool isFiling = false, bool updateSponsor = true)
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
// ReSharper disable InconsistentNaming
        using (var TaxForm_u = new iTRAACProc("TaxForm_u"))
// ReSharper restore InconsistentNaming
        {
          TaxForm_u.AssignValues(Fields);
          TaxForm_u["@IsFiling"] = isFiling;
          if (!TaxForm_u.ExecuteDataSet(UserMessagePrefix)) return (false);
          Fields.AcceptChanges();
          if (isFiling) CacheTables(TaxForm_u);
        }

        if (updateSponsor) OnFormStatusChange();
      }

      if (ExtendedFields.IsDirty())
      {
// ReSharper disable InconsistentNaming
        using (var TaxForm_TransactionTypeExt_u = new Proc("TaxForm_TransactionTypeExt_u"))
// ReSharper restore InconsistentNaming
        {
          TaxForm_TransactionTypeExt_u.AssignValues(ExtendedFields);
          TaxForm_TransactionTypeExt_u["@TaxFormGUID"] = GUID;
          if (!TaxForm_TransactionTypeExt_u.ExecuteNonQuery(UserMessagePrefix)) return (false);
        }
        ExtendedFields.AcceptChanges();
      }

      RemarkModel.SaveRemarks("FKRowGUID", GUID, UserMessagePrefix, TaxFormRemarks);

      return (true);
    }

    private void SetExtendedFields()
    {
      if (ExtendedFields == null) return;
      ExtendedFields.PropertyChanged -= ExtendedFieldsPropertyChanged;
      ExtendedFields.DetachRow();
      var extable = GetCurrentTransactionTypeExtendedTableName();

      if (DsCache.Relations.Contains(extable)) //if this is a transaction type with extended fields required...
      {
        var extFields = Fields.CreateChildView(extable);

        if (extFields.Count == 0)
          //if no row already exists, create a default row for this tax form and transaction type to provide the empty slot for fields to be filled out
        {
          DataTable extTable = DsCache.Tables[extable];

          //it'd be cool to break this out into a generic AddBlankRow() function that populates a GUID PK if present and cruises the column types to create blank default values
          var extrow = extTable.NewRow();
          var cols = extTable.Columns;
          foreach (var col in cols.Cast<DataColumn>().Where(col => !col.ColumnName.Contains("GUID")))
            extrow[col.ColumnName] = ""; //fill out all non GUID fields generically to dodge not null errors
          extrow["RowGUID"] = Guid.NewGuid();
          extrow["TaxFormGUID"] = GUID;
          extTable.Rows.Add(extrow);
          extrow.AcceptChanges();
        }

        ExtendedFields = extFields[0];
        ExtendedFields.PropertyChanged += ExtendedFieldsPropertyChanged;
      }

      OnPropertyChanged("ExtendedFields");
        //let the UI know to update the extended fields display when we change TransactionType
    }

    void ExtendedFieldsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      IsModified = true;
    }

    private string GetCurrentTransactionTypeExtendedTableName()
    {
      var transTypeId = Fields["TransTypeID"].ToString();
      if (transTypeId == "") return (null);
      var drv = TransactionTypes.Table.DefaultView.FindRows(transTypeId);
      var ext = drv[0]["ExtendedFieldsCode"].ToString().ToLower();
      return ((ext != "") ? "taxform_" + ext : null);
    }

    //private DataRelation[] TransactionTypeRelations { get { return (dsCache.Relations.Cast<DataRelation>().Where(rel => rel.ParentTable.TableName == "taxform" && rel.RelationName.Left(8) == "taxform_").ToArray<DataRelation>()); } }

    #endregion

    //todo: there's got to be a more appropriate name for this... but "FormType" is already taken by NF1, NF2, EF1, EF2, etc.
    [Flags]
    public enum PackageComponent { OrderForm = 1 << 0, Abw = 1 << 1 } //the stored proc depends on these ints

    [Flags]
    public enum StatusFlagsForm { Issued = 1 << 0, Returned = 1 << 1, Filed = 1 << 2, Reissued = 1 << 3, Amended = 1 << 4, Voided = 1 << 5}

    public void Print(PackageComponent printComponent)
    {
      if (!SaveMe() || (IsClass2 && !Validate())) return;
      Print(printComponent, GUID);
      OnPropertyChanged("Fields"); //to twiddle the InitPrtAbw field that is presumably bound to an "Un-Printed" type visual alert
    }


    static public void ResetPrinterSettings(PackageComponent printComponent)
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

      using(var resetEpson = AssemblyHelper.GetEmbeddedResource("reset_epson.bin"))
      {
        RawPrinterHelper.SendStreamToPrinter(
          SettingsModel.Local[printComponent == PackageComponent.OrderForm ? "POPrinter" : "AbwPrinter"],
          resetEpson);
      }
    }

    static public void PrintTest(PackageComponent printComponent)
    {
      Print(printComponent, Guid.Empty.ToString());

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

    static private bool IsPrintOK()
    {
      if (String.IsNullOrEmpty(SettingsModel.Local["POPrinter"]) || String.IsNullOrEmpty(SettingsModel.Local["AbwPrinter"]))
      {
        ShowUserMessage("Dot Matrix printers have not been assigned yet.");
        return (false);
      }
      return (true);
    }

    static private bool Print(PackageComponent printComponents, string guid)
    {
      if (!IsPrintOK()) return(false);

      //nugget: sending raw ESCape codes to dot matrix printer (Epson's "ESC/P" standard)
      //nugget: ESC/P reference manual: http://files.support.epson.com/pdf/general/escp2ref.pdf
      //ESC @   = reset
      //ESC 3 n = n/216" line spacing
      //ESC M   = 12 cpi
      //ESC x n = quality: 0 = Draft, 1 = NLQ
      //ESC k n = font: 0 = Roman, 1 = Sans serif font
      //ESC p 0 = turn off proportional
      //ESC 2   = 1/6" line spacing = 0.166666666666666

// ReSharper disable InconsistentNaming
      using (var TaxForm_print = new Proc("TaxForm_print"))
// ReSharper restore InconsistentNaming
      {
        TaxForm_print["@TaxFormGUID"] = guid; //for testprint, pass Guid.Empty i.e. '00000000-0000-0000-0000-000000000000'
        TaxForm_print["@PrintComponent"] = (int)printComponents;

        TaxForm_print.ExecuteDataSet();

        var poPrinter = new RawCharacterPage(
          SettingsModel.Global["FormPrinterInitCodes"],
          Convert.ToInt16(SettingsModel.Global["FormPrinterWidth"]),
          Convert.ToInt16(SettingsModel.Global["FormPrinterHeight"]));

        var abwPrinter = poPrinter.Clone();

        if (PrintFields(poPrinter, TaxForm_print.DataSet.Tables[0].Rows))
        {
          if (guid == Guid.Empty.ToString()) poPrinter.PrintTestRulers();
          poPrinter.SendToPrinter(SettingsModel.Local["POPrinter"]);
        }

        if (PrintFields(abwPrinter, TaxForm_print.DataSet.Tables[1].Rows))
        {
          if (guid == Guid.Empty.ToString()) abwPrinter.PrintTestRulers();
          abwPrinter.SendToPrinter(SettingsModel.Local["AbwPrinter"]);
        }

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
      public FormType FormType { get; private set; }
      public int Qty { get; private set; }

      //outputs
      //public double Price (TransactionItem baseclass)
      //public string GUID (TransactionItem baseclass)
      public string PackageCode { get; private set; }

      public TaxFormPackage(TransactionList parentTransactionList, bool isPending, string sponsorGUID, 
        string authorizedDependentClientGUID, FormType formType, int qty) : base(parentTransactionList)
      {
        //sanity check: if there are already printed forms in the shopping cart, everything else must be printed during this session... too complicated to manage otherwise
        if (isPending && parentTransactionList.Any(t => !t.IsPending))
        {
          ShowUserMessage("Since forms have already been printed.\rAll subsequent forms must also be printed.\rPlease select 'Print Immediately' only.");
          return;
        }

        //combine all NF1's requested during this customer session into one bundle
        var existingNF1Package = parentTransactionList.OfType<TaxFormPackage>().FirstOrDefault(w => w.FormType == FormType.NF1); //realized, filtering on IsPending is counterproductive. An NF1 package will only be in this list if generated during this customer "session" and therefore the total qty of forms is what should factor into the qty discount calc
        if (existingNF1Package != null)
        {
          qty += existingNF1Package.Qty; //add the existing quantity to this new package...
          PackageCode = existingNF1Package.PackageCode; //to facilitate adding these new forms to a previously printed package
          isPending = existingNF1Package.IsPending; //pending status must be same as whatever was already initiated
        }

        PendingAction = "Print";

        IsPending = isPending;
        Description = String.Format("New {0} Package {1}", Enum.GetName(typeof(FormType), formType), formType == FormType.NF1 ? "(" + qty.ToString(CultureInfo.InvariantCulture) + ")" : ""); //Extensions.Pluralize("{count} New {0} Form{s}", Qty, Enum.GetName(typeof(TaxForm.FormType), FormType));
        SponsorGUID = sponsorGUID;
        AuthorizedDependentClientGUID = authorizedDependentClientGUID;
        FormType = formType;
        Qty = qty;
        Price = LookupPackageServiceFee(formType, qty);

        if (!isPending)
        {
          new System.Threading.Thread(delegate(object existingPackage) //pop off a background thread to create the records
          {
            if (Execute())
            {
              //hit the DB to create all the new Package/Forms records
              if (existingPackage != null)
                parentTransactionList.Remove(existingPackage as TaxFormPackage); //delete the old package
              AfterConstructor();
            }
          }).Start(existingNF1Package);
        }

      }

      public override bool Execute()
      {
        if (!IsPrintOK()) return (false);

        string[] taxFormGUIDs;
        using (var taxFormPackageNew = new iTRAACProc("TaxFormPackage_New"))
        {
          taxFormPackageNew["@PackageCode"] = PackageCode; //for merging multiple requests for the same form type during this same customer session
          taxFormPackageNew["@FormTypeID"] = Convert.ToInt32(FormType);
          taxFormPackageNew["@FormCount"] = Qty;
          taxFormPackageNew["@SponsorGUID"] = SponsorGUID;
          taxFormPackageNew["@ClientGUID"] = AuthorizedDependentClientGUID;
          taxFormPackageNew["@Pending"] = IsPending;

          taxFormPackageNew.ExecuteNonQuery();

          Price = (decimal)taxFormPackageNew["@ServiceFee"];
          GUID = taxFormPackageNew["@TaxFormPackageGUID"].ToString();
          PackageCode = taxFormPackageNew["@PackageCode"].ToString();
          taxFormGUIDs = taxFormPackageNew["@TaxFormGUIDs"].ToString().Split(',');

          IsPending = false;
          NotifyPropertyChanged("IsPending");
        }

        if (taxFormGUIDs.Any(taxFormGUID => !Print(PackageComponent.OrderForm | PackageComponent.Abw, taxFormGUID)))
        {
          return(false);
        }

        FormStatusChangeCallback(SponsorGUID);

        return (true);
      }

    }

  }
}