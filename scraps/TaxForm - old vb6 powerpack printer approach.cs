using System;
using System.Data;
using System.Linq;
using Microsoft.VisualBasic.PowerPacks.Printing.Compatibility.VB6;
using System.Windows.Data;
using System.Text.RegularExpressions;
using System.Text;

namespace iTRAACv2
{

  public class TaxForm : BusinessBase
  {
    private BindingBase _TitleBinding = new Binding("Fields[OrderNumber]") { Mode = BindingMode.OneWay };
    public override BindingBase TitleBinding { get { return (_TitleBinding); } }

    public static event Action<string> FormStatusChangeCallback;

    public bool IsClass2 { get { return ("2,5,".Contains(Fields["FormTypeId"].ToString() + ",")); } }

    public bool IsReadOnly
    {
      get { return (_IsReadOnly); }
      set
      {
        if (value == false) ShowUserMessage("Remember: Unlock to make *CORRECTIONS* only (i.e. typos),\rNot *CHANGES* that _differ_ from the actual VAT form hardcopy.");
        _IsReadOnly = value;
        OnPropertyChanged("IsReadOnly");
      }
    }
    private bool _IsReadOnly = true;

    static TaxForm()
    {
      if (WPFHelpers.DesignMode) return;

      //piggyback the TransactionType code table load with the TaxForm to ExtendedFields by TransactionType master-detail tables
      string TableNames = "";
      TableCache(Guid.Empty.ToString(), ref TableNames, "TaxForm_init"); //among other things, this query will initialize an empty "TaxForm" DataTable in dsCache ... since this is a static constructor which fires before everything else, it is safe to assume TaxFormTable property will be non null in all other contexts

      TransactionTypes = new DataView(dsCache.Tables["TransactionType"]);
      foreach (string ExtTableName in TableNames.Split(','))
      {
        if (ExtTableName.Left(8) != "taxform_") continue;
        dsCache.Relations.Add(ExtTableName, TaxFormTable.Columns["RowGUID"], dsCache.Tables[ExtTableName].Columns["TaxFormGUID"]);
      }

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
      get { return (iTRAACHelpers.IsBitOn(Fields["StatusFlags"], StatusFlagsForm.Voided)); }
      set
      {
        Fields["StatusFlags"] = iTRAACHelpers.BitFlipper(Fields["StatusFlags"], StatusFlagsForm.Voided, value);
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

    public bool IsTotalCostMinWarning { get { return (TotalCost <= Setting.TaxFormTotalCostMin); } }
    public bool IsTotalCostMinWarningConfirmed { get; set; }

    public bool IsTotalCostMaxViolation { get { return (!IsViolation && TotalCost * ((Currency == "USD") ? Setting.RoughUSDToEuroRate : 1) >= 2500); } }
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
      Fields["StatusFlags"] = iTRAACHelpers.BitFlipper((int)Fields["StatusFlags"], StatusFlagsForm.Returned, false);
    }

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
        TaxFormRemarks.RowFilter = Remark.ShowDeletedFilter(value);
      }
    }

    private string UserMessagePrefix { get { return ("[Tax Form #: " + Fields["OrderNumber"].ToString() + "] "); } }

    public void SetViolation(int RemarkTypeId, string Remarks)
    {
      Remark.SaveNew(GUID, RemarkTypeId, Remarks);
      OnPropertyChanged("IsViolation");
      OnFormStatusChange();
    }

    public void RemoveRemark(DataRowView RemarkRow, string Reason)
    {
      Remark.Remove(RemarkRow, Reason);
      OnPropertyChanged("IsViolation");
    }

    public bool ReturnForm(bool IsClosing = false, bool DisplaySuccess = true)
    {
      using (Proc TaxForm_ReturnFile = new iTRAACProc("TaxForm_ReturnFile"))
      {
        TaxForm_ReturnFile["@TaxFormGUID"] = GUID;
        TaxForm_ReturnFile["@File"] = 0;
        if (!TaxForm_ReturnFile.ExecuteDataSet(UserMessagePrefix + "Return - ", true)) return (false);

        //if we're firing Returned and this object is remaining visible (i.e. user didn't select the Return-and-Close button) then pull back the updated values reflecting the Returned status change
        if (!IsClosing)
        {
          TaxFormTable.Merge(TaxForm_ReturnFile.Table0, false);
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
      if (IsFiling) Validate_Generic(ref IsValid, "Description");

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
      bool isvalid = Validate(IsFiling: true);
      IsIncomplete = !isvalid; //right at the time of testing validity to completely "File" is where we flip the incomplete flag on
      //while tempting, don't move this "IsIncomplete" line into Validate() because that would create a cyclic dependency between the two values in LoadFields()
      if (!isvalid)
      {
        ShowUserMessage("Please correct highlighted fields");
        return (false);
      }

      //we're leaving the form IsReadOnly=false for the convenience of immediate typo type edits while it's still open, it will be safely locked (IsReadOnly=true) the next time it's opened

      if (!SaveMe(IsFiling: true)) return (false);

      if (IsTotalCostMaxViolation) SetViolation(10, String.Format("Total Cost: {0} {1}", TotalCost, Currency)); //this also calls OnFormStatusChange() to update Sponsor UI
      else OnFormStatusChange();

      if (!IsClosing)
        OnPropertyChanged("Fields");

      return (true);
    }

    public TaxFormPackage ParentPackage = null;
    static public TaxForm NewNF2(TransactionList ParentTransactionList, string SponsorGUID, string SponsorName,
      string AuthorizedDependentClientGUID, string AuthorizedDependentName)
    {
      TaxFormPackage pkg = new TaxFormPackage(
        ParentTransactionList: ParentTransactionList,
        IsPending: true, 
        SponsorGUID: SponsorGUID,
        AuthorizedDependentClientGUID: AuthorizedDependentClientGUID, 
        FormType: FormType.NF2, Qty: 1);

      TaxForm frm = Lookup<TaxForm>(Guid.Empty.ToString()); //empty guid triggers new form logic in sproc
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
      string TableNames = "TaxForm";
      Fields = TableCache(GUID, ref TableNames, "TaxForm_s");
      GUID = Fields["RowGUID"].ToString(); //for the case where TaxForm_s can lookup on OrderNumber as well as GUID
      //currently comes back with the following tables loaded: TaxForm, (optional) TaxForm_Weapon or (optional) TaxForm_Vehicle

      Fields.PropertyChanged += FieldChange;
      SetExtendedFields();

      //default state to readonly when form has been filed or voided
      _IsReadOnly = iTRAACHelpers.IsBitOn(Fields["StatusFlags"], StatusFlagsForm.Filed) ||
                    iTRAACHelpers.IsBitOn(Fields["StatusFlags"], StatusFlagsForm.Voided);

      //create the logical lookup field for "Location" via TaxForm.LocationCode to TaxOffice.TaxOfficeCode
      dsCache.AddRelation("Location", TaxOffice.TaxOfficeTable.Columns["OfficeCode"], TaxFormTable.Columns["LocationCode"], false);
      if (!TaxFormTable.Columns.Contains("Location"))
        TaxFormTable.Columns.Add("Location", typeof(string), "Parent(Location).Office");

      //create the TaxForm_Remark relationship 
      dsCache.AddRelation("TaxForm_Remark", TaxFormTable.Columns["RowGUID"], TaxFormRemarksTable.Columns["FKRowGUID"], false);
      TaxFormRemarks = Fields.CreateChildView("TaxForm_Remark");
      ShowDeletedRemarks = false;
      Remark.CommonRemarkTableSettings(TaxFormRemarks);
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
      return (SaveMe());
    }

    protected bool SaveMe(bool IsFiling = false)
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
          if (!TaxForm_u.ExecuteDataSet(UserMessagePrefix, true)) return (false);
          Fields.AcceptChanges();
          if (IsFiling) TaxFormTable.Merge(TaxForm_u.Table0, false);
        }
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

      Remark.SaveRemarks("FKRowGUID", GUID, UserMessagePrefix, TaxFormRemarks);

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

    //todo: there's got to be a more appropriate name for this... "FormType" is already taken by NF1, NF2, EF1, EF2, etc.
    [Flags]
    public enum PackageComponent { Both=0, OrderForm=1, Abw=2 } //the stored proc depends on these ints

    static private Printer InitDotMatrix(string PrinterName)
    {
      Printer printer = (new PrinterCollection()).Cast<Printer>().FirstOrDefault(p => p.DeviceName == PrinterName);
      if (printer == null) { ShowUserMessage(String.Format("Invalid printer mapping for '{0}'", PrinterName)); return(null); }

      //nugget: sending raw ESCape codes to dot matrix printer (Epson's "ESC/P" standard)
      //nugget: ESC/P reference manual: http://files.support.epson.com/pdf/general/escp2ref.pdf
      //ESC @   = reset
      //ESC x n = quality: 0 = Draft, 1 = NLQ
      //ESC k n = font: 0 = Roman, 1 = Sans serif font
      //ESC M   = 12 cpi
      //RawPrinterHelper.SendStringToPrinter(PrinterName, Properties.Settings.Default.PrinterEscapeCodes.Replace("~", "\x1b")+"does this do anything\r\ntesting line2"); //nugget:

      // 12cpi = 10 point font = 120 twips per char horizontally
      // and it seems the printer subsystem is hard coded to 1/6" per line or 240 twips per line = 72 lines max for our 12" fanfold paper page
      // fortunately the original VB code looked like it was doing 1/6" line spacing as well
      // the original VB code used the 120 horizontally but used 200 twips to advance to the next line... i wonder if they just guessed and it basically worked out or if it was actually necessary for other screwy system reason

      //printer.PaperSize = //PrinterObjectConstants.vbPRPSFanfoldStdGerman; //12" inch long German fanfold would truncate at 80 chars
      //these are in "twips"... twips = inches x 1440: http://stackoverflow.com/questions/604203/twips-pixels-and-points-oh-my/604276#604276
      //interesting, "twip" = "Twentieth of an Imperial Point" (fancy! i'll have a royale with cheese! ;)
      //8.5" = 12240 twips, 9" = 12960, 11" = 15840, 12" = 17280
      printer.Height = 17280; 
      printer.Width = 12960; 

      printer.FontName = "Courier New"; //"Sans Serif 12cpi";
      printer.FontSize = 12; //fontsize is in points = 120 / cpi, 10 pt = 12 cpi
      printer.ScaleMode = 1; //4 = characters, 1 = twips
      printer.PrintAction = System.Drawing.Printing.PrintAction.PrintToPreview;

      //"<1B>3<1E121B>M<1B>x<001B>k<01>"
      //printer.Write("\x1b3\x1e\x12\x1bM\x1bx\x00\x1bk\x01");

      return (printer);
    }

    static private void SetCurrentXY(Printer p, int col, float row)
    {
      p.ScaleMode = 4;
      p.CurrentX = 0;
      if (col > 1)
        p.Write(new string(' ', Convert.ToInt16(col) - 1));
      //-FormField definition is 0-based 
      //return (float.Parse(col.ToString()) * ((ScaleMode == 1 /*twips*/) ? 120F : 1F /*chars*/) );

      p.ScaleMode = 1;
      p.CurrentY = Math.Min(float.Parse(row.ToString()), 64F) * 200F;
    }
  
    static private void PrintFields(Printer printer, DataRowCollection rows)
    {
      if (rows.Count == 0) return;

      //@ = reset
      //P = 10 cpi
      //1 = 7/72 line spacing (~8/72 = 1/9 < 1/8 default)
      //printer.Print("~@~M~A7~C83~E".Replace("~", "\x1b"));

      foreach (DataRow r in rows)
      {
        //http://support.microsoft.com/kb/76388
        //pitch = chars per inch (cpi)
        //points = 120 / pitch
        //these two magic numbers came from the iTRAAC v1 VB6 source code, they were not commented
        //this page provides a little clarity: http://msdn.microsoft.com/en-us/library/microsoft.visualbasic.powerpacks.printing.compatibility.vb6.printer.scalemode.aspx
        //a character seems to be hard coded to be 120 twips... so 90 chars = 10800 twips which should not be getting truncated for page width = 12240 twips, but it is!! why!?!?
        //we actually get truncated at column 86 when page width = 12240, so somehow it's more like 1 char = 142.3 twips

        SetCurrentXY(printer, (int)r["col"], float.Parse(r["row"].ToString()));

        string data = r["Data"].ToString();
        int maxlength = Convert.ToInt32(r["MaxLength"]);
        int maxrows   = Convert.ToInt32(r["MaxRows"]);

        if (maxrows == 1 || data.Length <= maxlength)
        {
          printer.Write(data.Left(maxlength));
        }
        else
        {
          //wordwrap... taken from here: http://en.wikipedia.org/wiki/Word_wrap
          int currentrow = 1;
          string linetext = "";

          Regex regx = new Regex(@"\w+(\W|$)", RegexOptions.Compiled | RegexOptions.Singleline);
          MatchCollection words = regx.Matches(data);
          foreach (Match word in words)
          {
            //if the next word is more than the space remaining ...
            if (word.Value.TrimEnd().Length > maxlength - linetext.Length)
            {
              //... then print the line of text we've built up to this point...
              printer.Write(linetext + ((currentrow == maxrows) ? "..." : ""));
              if (currentrow == maxrows) break;

              // ... and start a fresh empty line 
              linetext = "";
              SetCurrentXY(printer, (int)r["col"], (float)++currentrow);
            }

            linetext += word;

            // if this is the last word then print the line
            if (!word.NextMatch().Success) printer.Write(linetext);
          }
        }
      }

      printer.EndDoc();
    }

    public void Print(PackageComponent PrintComponent)
    {
      if (!SaveMe() || (IsClass2 && !Validate())) return;
      TaxForm.Print(PrintComponent, GUID);
    }


    static public void PrintTest(PackageComponent PrintComponent)
    {
      Print(PrintComponent, Guid.Empty.ToString());
    }

    static private void PrintStringAtPos(string str, int col, int row)
    {
      PrintBytesAtPos(Encoding.ASCII.GetBytes(str), col, row);
    }

    static private void PrintBytesAtPos(byte[] bytes, int col, int row)
    {
      Buffer.BlockCopy(bytes, 0, pagebytes, pageinit.Length + row * pagewidth + col, bytes.Length);
    }

    static int pagewidth = 97; //0 to 96 (printed forms support 96 chars horizontally, but we need one more char to represent the end of line feed (LF))
    static int pageheight = 76; //0 to 83

    //ESC @ = reset
    //ESC 2 = 1/6" line spacing = 0.166666666666666
    //ESC 2 = n/216 line spacing
    //shooting for the original line density by trial and error i couldn't find a perfect match with the n/216 unit
    //30(x1E)/216 = 0.139 was a smidge to much and 29/216 (0.134) was a smidge to small (farther off than 30 was)
    //and unfortunately there's nothing more granular to choose from, "ESC +" (n/360) doesn't work for our little 9-pin model (i tried it)
    static string pageinit = "~@~2\x1E".Replace('~', '\x1b');

    static byte[] pagebytes = new byte[pagewidth * pageheight + pageinit.Length + 1 /*for form feed*/];
    static private void Print(PackageComponent PrintComponent, string guid)
    {
      //nugget: sending raw ESCape codes to dot matrix printer (Epson's "ESC/P" standard)
      //nugget: ESC/P reference manual: http://files.support.epson.com/pdf/general/escp2ref.pdf
      //ESC @   = reset
      //ESC x n = quality: 0 = Draft, 1 = NLQ
      //ESC k n = font: 0 = Roman, 1 = Sans serif font
      //ESC M   = 12 cpi

      //reinitialize the page every time by filling the array with spaces plus a line feed at the end of every line...
      Buffer.BlockCopy(Encoding.ASCII.GetBytes(pageinit), 0, pagebytes, 0, pageinit.Length);
      // "flattening a 2D array": http://www.dotnetperls.com/flatten-array
      // logicalrow = row * pagewidth
      byte[] rowbytes = Encoding.ASCII.GetBytes(new string(' ', pagewidth));
      rowbytes[pagewidth - 1] = 0x0A; // linefeed (LF)
      pagebytes[pagebytes.Length - 1] = 0x0C; // form feed (FF)

      //apparently simple for-loop is the recommended best practice for this drudgery
      //http://stackoverflow.com/questions/1897555/what-is-the-equivalent-of-memset-in-c
      //http://stackoverflow.com/questions/6150097/initialize-a-byte-array-to-a-certain-value-other-than-the-default-null
      for (int row = 0; row < pageheight; row++)
      {
        PrintBytesAtPos(rowbytes, 0, row);
      }

      PrintStringAtPos(String.Format("{0}{0}{0}{0}{0}{0}{0}{0}{0}123456", "1234567890"), 0, 0);
      PrintStringAtPos(String.Format("2        1{0}2{0}3{0}4{0}5{0}6{0}7{0}8{0}9", "         "), 0, 1);
      for (int row = 2; row < pageheight; row++)
      {
        //we need to leave FormField data definition 0-based to be consistent with v1...
        //but want to view in 1-based on the hardcopy
        PrintStringAtPos((row + 1).ToString(), 0, row);
      }

      RawPrinterHelper.SendManagedBytesToPrinter(Setting.Local["POPrinter"], pagebytes); 

      return;

      using (Proc TaxForm_print = new Proc("TaxForm_print"))
      {
        TaxForm_print["@TaxFormGUID"] = guid; //for testprint, pass Guid.Empty i.e. '00000000-0000-0000-0000-000000000000'
        TaxForm_print["@PrintComponent"] = (int)PrintComponent;

        TaxForm_print.ExecuteDataSet();

        Printer POPrinter = InitDotMatrix(Setting.Local["POPrinter"]); //coding defensively, reset printer settings with every print... so agents can readily power cycle the printer with no ramifications, etc.
        Printer AbwPrinter = ((Setting.Local["POPrinter"] == Setting.Local["AbwPrinter"])) ?  POPrinter : InitDotMatrix(Setting.Local["AbwPrinter"]); //don't re-init the same printer 

        if (guid == Guid.Empty.ToString()) //test mode...
        {
          SetCurrentXY(POPrinter, 0, 0);
          POPrinter.Write(String.Format("{0}{0}{0}{0}{0}{0}{0}{0}{0}123456", "1234567890"));

          SetCurrentXY(POPrinter, 0, 1);
          POPrinter.Write(String.Format("2        1{0}2{0}3{0}4{0}5{0}6{0}7{0}8{0}9", "         "));

          for (int i = 2; i <= 63; i++)
          {
            //we need to leave FormField data definition 0-based to be consistent with v1...
            SetCurrentXY(POPrinter, 0, i);
            //but want to view in 1-based on the hardcopy
            POPrinter.Write((i + 1).ToString());
          }
        }

        PrintFields(POPrinter, TaxForm_print.dataSet.Tables[0].Rows);
        PrintFields(AbwPrinter, TaxForm_print.dataSet.Tables[1].Rows);
      }

    }

    public enum FormType { NF1=1, NF2=2, EF1=4, EF2=5 };

    public class TaxFormPackage : TransactionList.TransactionItem
    {
      //inputs
      //public bool Pending (TransactionItem baseclass)
      //public string Description (TransactionItem baseclass)
      public string SponsorGUID { get; private set; }
      public string AuthorizedDependentClientGUID { get; private set; }
      public TaxForm.FormType FormType { get; private set; }
      public int Qty { get; private set; }

      //outputs
      //public double Price (TransactionItem baseclass)
      //public string GUID (TransactionItem baseclass)
      public string PackageCode { get; private set; }

      public TaxFormPackage(TransactionList ParentTransactionList, bool IsPending, string SponsorGUID, 
        string AuthorizedDependentClientGUID, TaxForm.FormType FormType, int Qty) : base(ParentTransactionList)
      {
        this.IsPending = IsPending;
        this.Description = Extensions.Pluralize("{count} New {0} Form{s}", Qty, Enum.GetName(typeof(TaxForm.FormType), FormType));
        this.SponsorGUID = SponsorGUID;
        this.AuthorizedDependentClientGUID = AuthorizedDependentClientGUID;
        this.FormType = FormType;
        this.Qty = Qty;
        this.Price = LookupPackageServiceFee(FormType, Qty);

        AfterConstructor();
      }

      public void Execute()
      {
        Assert.Check(GUID == null, String.Format("Invalid: Attempting to re-create a Package that's already been initialzed in the DB. [PacakgeCode: {0}]", PackageCode));

        using (iTRAACProc TaxFormPackage_New = new iTRAACProc("TaxFormPackage_New"))
        {
          TaxFormPackage_New["@FormTypeID"] = Convert.ToInt32(FormType);
          TaxFormPackage_New["@FormCount"] = Qty;
          TaxFormPackage_New["@SponsorGUID"] = SponsorGUID;
          TaxFormPackage_New["@ClientGUID"] = AuthorizedDependentClientGUID;
          TaxFormPackage_New["@Pending"] = IsPending;

          TaxFormPackage_New.ExecuteNonQuery();

          this.Price = (decimal)TaxFormPackage_New["@ServiceFee"];
          this.GUID = TaxFormPackage_New["@TaxFormPackageGUID"].ToString();
          this.PackageCode = TaxFormPackage_New["@PackageCode"].ToString();
        }

        TaxForm.FormStatusChangeCallback(SponsorGUID);
      }

    }

  }
}