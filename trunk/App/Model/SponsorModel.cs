using System;
using System.Data;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Windows.Data;

namespace iTRAACv2.Model
{

  public class SponsorModel : ModelBase
  {
    //public override string TitleBindingPath { get { return ("Fields.SponsorNameID"); } }
    public override BindingBase TitleBinding
    {
      get
      {
        if (_titleBinding == null)
        {
          _titleBinding = new MultiBinding { StringFormat = "{0} ({1})" };
          _titleBinding.Bindings.Add(new Binding("HouseMembers[0][LName]") { Mode = BindingMode.OneWay });
          _titleBinding.Bindings.Add(new Binding("HouseMembers[0][CCode]") { Mode = BindingMode.OneWay });
          //_TitleBinding = new Binding("Members[0].CCode") { Mode = BindingMode.OneWay };

          // ReSharper disable InconsistentNaming
          using (var Ranks_s = new Proc("Ranks_s")) Ranks = Ranks_s.ExecuteDataSet().Table0.DefaultView;
          // ReSharper restore InconsistentNaming
        }
        return (_titleBinding);
      }
    }

    private MultiBinding _titleBinding;

    public DataRowView UTAPFields { get; private set; }
    public DataView HouseMembers { get; private set; }
    public DataView TaxForms { get; private set; }
    public DataView SponsorRemarks { get; private set; }

    public bool HasSponsor { get { return (HouseMembers.Table.Rows.Cast<DataRow>().Any(r => r["SponsorGUID"].ToString() == GUID && r.Field<bool>("IsSponsor"))); } }
    public bool HasSpouse { get { return (HouseMembers.Cast<DataRowView>().Any(r => r.Field<bool>("IsSpouse"))); } }
    public bool ShowDeactiveMembers { get { return (HouseMembers.RowFilter == ""); } set { HouseMembers.RowFilter = value ? "" : "Active = true"; } }
    public bool HasDeactiveMembers
    {
      get
      {
        if (HouseMembers.Count == 0) return (false);

        //the members DataView.RowFilter is bound to the Show DeActive CheckBox, so if it's unchecked, Deactives won't be present in this view, therefore we have to look at the underlying table for deactive Clients matching this SponsorGUID
        return (HouseMembers.Table.Rows.Cast<DataRow>().Any(r => r["SponsorGUID"].ToString() == GUID && !(bool)r["Active"]));
      }
    }
    
    public bool HasUnreturnedClass2 { get { return ( TaxForms.Cast<DataRowView>().Any(r => r.Field<string>("Form #").Substring(2, 1) == "2" && r.Field<bool>("IsUnreturnedID"))); } }
    public int Class1TaxFormsCountUnreturned { get; private set; }
    public int Class1TaxFormsCountRemainingToBuy { get { return (SettingsModel.MaxClass1FormsCount - Class1TaxFormsCountUnreturned); } }
    public int TaxFormsCountReturnedNotFiled { get; private set; }

    static public DataView Ranks { get; private set;}

    static private DataTable ClientTable { get { return (DsCache.Tables["Client"]); } }

    /// <summary>
    /// DO NOT DATABIND - will not be Notified of PropertyChange
    /// </summary>
    //private bool IsSuspended { get { return (Fields["SuspensionExpiry"].ToString() == ""); } }

    //TODO:deleteme after testing:
    //static public void OpenNewSponsor()
    //{
    //  Sponsor NewSponsor = ModelBase.Lookup<Sponsor>(Guid.Empty.ToString()) as Sponsor;
    //  RoutedCommands.OpenSponsor.Execute(Guid.Empty.ToString(), null);
    //}

    public bool ShowDeletedRemarks
    {
      get
      {
        return (SponsorRemarks.RowFilter == "");
      }
      set
      {
        SponsorRemarks.RowFilter = RemarkModel.ShowDeletedFilter(value);
      }
    }


    public void SetSpouse(string newSpouseClientGUID, bool isDivorce = false)
    {
      //if we're doing a "Clear Spouse", check if we've just got a brand new raw row created for the spouse to be entered here on the client...
      //then we just drop it out of memory since it's not on the database yet anyway
      if (string.IsNullOrEmpty(newSpouseClientGUID) && DropInMemAddMember())
      {
        OnPropertyChanged("HasSpouse");
        OnPropertyChanged("DependentsSelectionList");
        return;
      }

// ReSharper disable InconsistentNaming
      using (var Sponsor_SetSpouse = new iTRAACProc("Sponsor_SetSpouse"))
// ReSharper restore InconsistentNaming
      {
        Sponsor_SetSpouse["@SponsorGUID"] = GUID;
        Sponsor_SetSpouse["@NewSpouseClientGUID"] = newSpouseClientGUID;
        Sponsor_SetSpouse["@IsDivorce"] = isDivorce;
        CacheTables(Sponsor_SetSpouse);
        HouseMembers = HouseMembers; //for some reason OnPropertyChanged("HouseMembers") didn't refresh the Members Grid, i don't have a good guess but this little hack worked immediately so i'm moving on
        OnPropertyChanged("HasSpouse");
        OnPropertyChanged("DependentsSelectionList");
      }
    }

    public void SetSponsor(string newSponsorClientGUID, bool fixExistingPackageLinks)
    {
// ReSharper disable InconsistentNaming
      using (var Sponsor_SetSponsor = new iTRAACProc("Sponsor_SetSponsor"))
// ReSharper restore InconsistentNaming
      {
        Sponsor_SetSponsor["@SponsorGUID"] = GUID;
        Sponsor_SetSponsor["@NewSponsorClientGUID"] = newSponsorClientGUID;
        Sponsor_SetSponsor["@FixExistingPackageLinks"] = fixExistingPackageLinks;
        CacheTables(Sponsor_SetSponsor);
        HouseMembers = HouseMembers; //for some reason OnPropertyChanged("HouseMembers") didn't refresh the Members Grid, i don't have a good guess but this little hack worked immediately so i'm moving on
      }
    }

    public void SetCCodesSame()
    {
// ReSharper disable InconsistentNaming
      using (var Sponsor_SetCCodesSame = new iTRAACProc("Sponsor_SetCCodesSame"))
// ReSharper restore InconsistentNaming
      {
        Sponsor_SetCCodesSame["@SponsorGUID"] = GUID;
        if (HouseMembers.Count > 0)
          Sponsor_SetCCodesSame["@NewCCode"] = HouseMembers[0]["CCode"];
        CacheTables(Sponsor_SetCCodesSame);
        HouseMembers = HouseMembers; //for some reason OnPropertyChanged("HouseMembers") didn't refresh the Members Grid, i don't have a good guess but this little hack worked immediately so i'm moving on
      }
    }

    /// <summary>
    /// remove the initial row used to enter the 'candidate' SSN etc which lead to choosing someone from the existing customer list and is now getting added (and therefore takes the place of this temporary row)
    /// </summary>
    /// <returns></returns>
    private bool DropInMemAddMember()
    {
      var simpleDrop = HouseMembers.Cast<DataRowView>().FirstOrDefault(r => r.Row.RowState == DataRowState.Added);
      if (simpleDrop == null) return (false);
      simpleDrop.DetachRow();
      return (true);
    }

    public void MoveClient(string moveClientGUID)
    {
      DropInMemAddMember();
      if (HouseMembers.Count == 0) SaveJustSponsorRecord(); //if brand new cust, save it so that there's something to connect the newly moved Client to below

// ReSharper disable InconsistentNaming
      using (var Sponsor_MoveClient = new iTRAACProc("Sponsor_MoveClient"))
// ReSharper restore InconsistentNaming
      {
        Sponsor_MoveClient["@SponsorGUID"] = GUID;
        Sponsor_MoveClient["@MoveClientGUID"] = moveClientGUID;
        CacheTables(Sponsor_MoveClient);
        HouseMembers = HouseMembers; //for some reason OnPropertyChanged("HouseMembers") didn't refresh the Members Grid, i don't have a good guess but this little hack worked immediately so i'm moving on
        OnPropertyChanged("HasSpouse");
        OnPropertyChanged("DependentsSelectionList");
      }

      if (!(bool)HouseMembers.Table.Rows.Find(moveClientGUID)["Active"])
      {
        ShowDeactiveMembers = true; //out of convenience, if the newly brought in member is flagged as deactive then disable deactive filter 
        OnPropertyChanged("ShowDeactiveMembers");
        OnPropertyChanged("HasDeactiveMembers");
      }
    }

    public void AddMember()
    {
      var newClientRow = ClientTable.NewRow();
      newClientRow["RowGUID"] = Guid.NewGuid();
      newClientRow["SponsorGUID"] = GUID == Guid.Empty.ToString() ? Fields["RowGUID"] : GUID;
      newClientRow["IsSponsor"] = HouseMembers.Count == 0;
      newClientRow["IsSpouse"] = false;
      newClientRow["Active"] = true;
      newClientRow["CCode"] = HouseMembers.Count > 0 ? HouseMembers[0]["CCode"] : "";

      newClientRow.SetAllNonDBNullColumnsToEmptyString();
      ClientTable.Rows.Add(newClientRow);
    }

    private string UserMessagePrefix { get { return (HouseMembers.Count == 0 ? "" : String.Format("[{0} ({1})] ", HouseMembers[0]["LName"], HouseMembers[0]["CCode"])); } }

    /// <summary>
    /// Provides a dynamically created list containing only *Dependents* (not Sponsor)
    /// To be used by Picklist style UI's
    /// </summary>
    public IEnumerable DependentsSelectionList
    {
      get
      {
        return (
          from DataRowView m in HouseMembers
          //form picklist from _active_ dependents (not including sponsor which is always printed for sure)
          where !m.Field<bool>("IsSponsor") && m.Field<bool>("Active")
          select new DependentLight
                   {
                    RowGUID = m["RowGUID"].ToString(),
                    FirstName = m["FName"].ToString(),
                    FullName = m["LName"] + ", " + m["FName"],
                    IsSpouse = m.Field<bool>("IsSpouse")
                  }
        );
      }
    }

    public class DependentLight
    {
      public string RowGUID { get; set; }
      public string FirstName { get; set; }
      public string FullName { get; set; }
      public bool IsSpouse { get; set; }
    }

    private bool _taxFormsUnReturnedOnlyFilter;
    public bool TaxFormsUnReturnedOnlyFilter
    {
      get
      {
        return (_taxFormsUnReturnedOnlyFilter);
      }
      set
      {
        _taxFormsUnReturnedOnlyFilter = value;
        SetTaxFormsRowFilter();
      }
    }

    private bool _taxFormsUnPrintedOnlyFilter;
    public bool TaxFormsUnPrintedOnlyFilter
    {
      get
      {
        return (_taxFormsUnPrintedOnlyFilter);
      }
      set
      {
        _taxFormsUnPrintedOnlyFilter = value;
        SetTaxFormsRowFilter();
      }
    }

    private bool _taxFormsReturnedNotFiledOnlyFilter;
    public bool TaxFormsReturnedNotFiledOnlyFilter
    {
      get
      {
        return (_taxFormsReturnedNotFiledOnlyFilter);
      }
      set
      {
        _taxFormsReturnedNotFiledOnlyFilter = value;
        SetTaxFormsRowFilter();
      }
    }

    private void SetTaxFormsRowFilter()
    {
      //the UI binds a radio button group to these filter properties
      //when you switch from one radio to the other, it fires the setters for each one in succession
      //so for a moment, the old and newly selected items are both true, the following logic skips this temporary state and only pays attention to the last setter fired
      if ((_taxFormsUnReturnedOnlyFilter?1:0) + (_taxFormsUnPrintedOnlyFilter?1:0)  + (_taxFormsReturnedNotFiledOnlyFilter?1:0) > 1) return; //this enforces the mutual exclusivity of these filters
      TaxForms.RowFilter =
        (_taxFormsUnReturnedOnlyFilter ? "IsUnreturnedID = 1 or IsIncompleteId = 1" : "") + //unreturned logically includes incomplete now as well since those should be given back to customer the moment they're back in the office (e.g. trying to buy more forms)
        (_taxFormsUnPrintedOnlyFilter ? "IsPrintedId = 0 and Status <> 'Voided'" : "") +
        //this is the funky way to do bitwise logic in the rowfilter syntax which doesn't support it directly... this checks that only the returned (2^1=2) bit is set out of both returned & filed (2^2=4) 
        //(_TaxForms_ReturnedNotFiledOnlyFilter ? "Convert((StatusFlagsID - StatusFlagsID %2)/2,'System.Int32') % 2 = 1 and Convert((StatusFlagsID - StatusFlagsID %4)/4,'System.Int32') % 2 = 0" : ""); 
        (_taxFormsReturnedNotFiledOnlyFilter ? "Status = 'Returned'" : "");
    }

    static public void TaxFormStatusChanged(string sponsorGUID)
    {
      ModelBase model;
      if (ModelCache.TryGetValue(sponsorGUID, out model))
      {
        ((SponsorModel) model).RefreshTaxFormsList();
      }
    }


    private void RefreshTaxFormsList(bool refreshData = true)
    {
      TaxForms.DetachRowsAndDispose();

      if (refreshData)
      {
        // hack alert... TableCache currently assumes it's looking for *single* model style records via their RowGUID
        // yet we're passing the Sponsor RowGUID to a proc that loads a *LIST* of forms
        // this 'works' because (a) we know we want to refresh the data so we specifically desire a cache *miss* in this case which is guaranteed because we'll never find a SponsorGUID in the TaxForms table
        // and (b) we need that SponsorGUID to target its list of forms
        EntityLookup(GUID, "Sponsor_TaxForm", "Sponsor_TaxForms"); //refresh the list of forms for this sponsor so we see the new ones just created
      }

      TaxForms = Fields.CreateChildView("Sponsor_TaxForm");
      SetTaxFormsRowFilter();

      Class1TaxFormsCountUnreturned = (TaxForms.Cast<DataRowView>().Where(r => r.Field<int>("FormTypeID") == 1 && (r.Field<bool>("IsUnreturnedID") || r.Field<bool>("IsIncompleteId")))).Count();
      TaxFormsCountReturnedNotFiled = (TaxForms.Cast<DataRowView>().Where(r => r.Field<string>("Status") == "Returned")).Count();

      OnPropertyChanged("Fields"); //added this to update Suspension info due to Form Violation
      OnPropertyChanged("SponsorRemarks"); //ditto on the Remarks driven by Form Violation 
      OnPropertyChanged("TaxForms");
      OnPropertyChanged("HasUnreturnedClass2");
      OnPropertyChanged("Class1TaxForms_CountUnreturned");
      OnPropertyChanged("Class1TaxForms_CountRemainingToBuy");
      OnPropertyChanged("Class1TaxForms_RemainingToBuyToolTipText");
      OnPropertyChanged("TaxForms_CountReturnedNotFiled");
    }

    protected override void LoadSubClass()
    {
      if (Transactions == null)
      {
        Transactions = new TransactionList(this, "Transactions");
      }

      //assumption we're taking with caching with Sponsor entities in particular:
      //  the corresponding sproc returns the whole household of sponsor + dependent clients records
      //  and also the "household" (sponsor) address/phone record
      //  then this lookup method returns a cached model of the specific type
      //  and the GUI can then pull what it needs off that object via basic properties

      //the specified GUID passed in is for the Sponsor's *Sponsor*table* RowGUID
      Fields = EntityLookup(GUID, "Sponsor", "Sponsor_s");

      var sponsorTable = DsCache.Tables["Sponsor"];
      sponsorTable.Columns["DutyPhoneDSN1"].AllowDBNull = false; //sucks to have to fix these but they're 'generated' from a real field so they lose their NOT NULL metadata
      sponsorTable.Columns["DutyPhoneDSN2"].AllowDBNull = false;

      ClientTable.ColumnChanged += ClientTableColumnChanged;
      ClientTable.RowChanged += ClientTableRowChanged;

      UTAPFields = RowLookup("Sponsor_UTAP", GUID);

      //create the logical lookup field for SuspensionTaxOffice via Sponsor.SuspensionTaxOfficeId to Office.TaxOfficeId
      DsCache.AddRelation("SuspensionTaxOffice", DsCache.Tables["TaxOffice"].Columns["TaxOfficeId"], sponsorTable.Columns["SuspensionTaxOfficeId"]);
      if (!sponsorTable.Columns.Contains("SuspensionTaxOffice"))
        sponsorTable.Columns.Add("SuspensionTaxOffice", typeof(string), "Parent(SuspensionTaxOffice).Office");

      //map the parent-child relationships hanging off Sponsor ... 
      DataColumn sponsorTableRowGUID = sponsorTable.Columns["RowGUID"];

      //SponsorTable tweaks:
      if (!sponsorTable.Columns.Contains("CanSellForms"))
      {
        sponsorTable.Columns.Add("CanSellForms", typeof(bool), "Active AND ISNULL(SuspensionExpiry, #1/1/01#) = #1/1/01#");
        sponsorTable.Columns["Active"].ReadOnly = true; //block access to this field from the UI, elsewhere in this class we temporarily open it up to make validated changes (see CheckClientActiveRules method)
      }
      
      //Dependents:
      DsCache.AddRelation("Sponsor_Client", sponsorTableRowGUID, ClientTable.Columns["SponsorGUID"]);
      HouseMembers = Fields.CreateChildView("Sponsor_Client");
      HouseMembers.Sort = "IsSponsor desc, IsSpouse desc, LName, FName";

      //if brand new sponsor, add a default row to fill out for convenience
      if (HouseMembers.Count == 0) AddMember();

      //set ShowDeactive to true, if there's not an Active Sponsor 
      ShowDeactiveMembers = !HouseMembers[0].Field<bool>("Active") || !HouseMembers[0].Field<bool>("IsSponsor");
      if (!ClientTable.Columns.Contains("IsSponsorOrSpouse"))
      {
        ClientTable.Columns.Add("IsSponsorOrSpouse", typeof(bool), "IsSponsor OR IsSpouse");
        ClientTable.Columns["Active"].ReadOnly = true; //block access to this field from the UI, elsewhere in this class we temporarily open it up to make validated changes (see CheckClientActiveRules method)
      }

      //TaxForms:
      DsCache.AddRelation("Sponsor_TaxForm", sponsorTableRowGUID, DsCache.Tables["Sponsor_TaxForm"].Columns["SponsorGUID"]);
      RefreshTaxFormsList(false);

      //Remarks:
      var remarkTable = DsCache.Tables["Sponsor_Remark"];
      DsCache.AddRelation("Sponsor_Remark", sponsorTableRowGUID, remarkTable.Columns["SponsorGUID"]);
      SponsorRemarks = Fields.CreateChildView("Sponsor_Remark");
      ShowDeletedRemarks = false;
      RemarkModel.CommonRemarkTableSettings(SponsorRemarks);
      SponsorRemarks.Table.RowChanged += SponsorRemarksRowChanged;
      SponsorRemarks.Table.ColumnChanging += SponsorRemarksColumnChanging;

      _potentialClientMatchesBgWorker.OnExecute = PotentialMatchesSearchExecute;
      _potentialClientMatchesBgWorker.OnCompleted = PotentialMatchesSearchCompleted;
    }

    //nugget: nifty event based approach to UI updating bound Model.field, model firing confirmation model logic which drives a UI modal popup, yet still keeping model *decoupled* from UI
    //nugget: the UI subscribes to BO's "confirmation" event which facilitates the decoupling
    //nugget: ideas from here: http://blog.tonysneed.com/2011/01/28/tackling-the-problem-of-modal-dialogs-in-mvvm/
    public event EventHandler<ReasonConfirmationArgs> ReasonConfirmation;
    void SponsorRemarksColumnChanging(object sender, DataColumnChangeEventArgs e)
    {
      if (!IsMyRow(e.Row)) return;

      if (e.Column.ColumnName == "Alert" && (bool)e.ProposedValue == false)
      {
        if (ReasonConfirmation != null)
        {
          var args = new ReasonConfirmationArgs();
          ReasonConfirmation(null, args);
          if (args.Accept)
          {
            e.Row.SetReadonlyField("AlertResolved", args.Reason);
          }
          else e.ProposedValue = e.Row[e.Column]; //cancel by putting the current value into proposed (throwing an exception gets messy to handle at the UI)
        }
      }
    }

    //one slightly annoying wrinkle of this object/DataTable hybrid approach is that one DataTable naturally represents multiple modelects (one model for each row)
    //yet the Row/Col-change events are only available for the entire table
    //each model must therefore register with the table to be notified of changes
    //unfortunately, that means all BO's are notified when any given row is updated
    //so a bit of extra messaging going on... hopefully that doesn't add up to much since there's typicalyl only going to be a handful of active BO's of any type at any given time...
    private bool IsMyRow(DataRow row)
    {
      //but we do have to account for ignoring the incoming notifications intended for other BO's by some logical check
      return (row.RowState != DataRowState.Detached && row["SponsorGUID"].ToString() == GUID);
    }

    void SponsorRemarksRowChanged(object sender, DataRowChangeEventArgs e)
    {
      if (IsMyRow(e.Row)) IsModified = true;
    }

    protected override void UnLoadSubClass()
    {
      if (SponsorRemarks != null) SponsorRemarks.Table.RowChanged -= SponsorRemarksRowChanged;

      if (ClientTable != null)
      {
        ClientTable.ColumnChanged -= ClientTableColumnChanged;
        ClientTable.RowChanged -= ClientTableRowChanged;
      }

      if (_potentialClientMatchesBgWorker != null)
      {
        _potentialClientMatchesBgWorker.OnExecute = null;
        _potentialClientMatchesBgWorker.OnCompleted = null;
      }

      UTAPFields.DetachRow();
      TaxForms.DetachRowsAndDispose();
      HouseMembers.DetachRowsAndDispose();
      SponsorRemarks.DetachRowsAndDispose();
    }


    #region PotentialMatches stuff

    public class PotentialClientMatchesState
    {
      public DataRow ClientRow;
      public PotentialMatchFieldType FieldType = PotentialMatchFieldType.SSN;
    }

    public DataView PotentialClientMatches { get ; private set; } //{if (_PotentialClientMatches == null) PotentialMatchesSearchExecute(new PotentialClientMatchesState()); return(_PotentialClientMatches); } }
    private readonly BackgroundWorkerEx<PotentialClientMatchesState> _potentialClientMatchesBgWorker = new BackgroundWorkerEx<PotentialClientMatchesState>();

    public enum PotentialMatchFieldType { SSN, Name }

    public void PotentialMatchesSearchCompleted(PotentialClientMatchesState state)
    {
      //nugget: there is something about this approach where the bound DataGrid was tough to get to populate
      //        my best guess is that it's because the PotentialClientMatches backing table gets updated on a background thread
      //        i thought it was a recommended approach to fill the bound ItemsSource however (background or not) and expect the UI to auto refresh
      OnPropertyChanged("PotentialClientMatches");
    }

    public void PotentialMatchesSearchExecute(PotentialClientMatchesState state)
    {
// ReSharper disable InconsistentNaming
      Proc Sponsor_New_Search = null;
// ReSharper restore InconsistentNaming
      switch (state.FieldType)
      {
        case PotentialMatchFieldType.SSN:
          Sponsor_New_Search = new Proc("Sponsor_New_SearchSSN");
          Sponsor_New_Search["@SSN1"] = state.ClientRow["SSN1"];
          Sponsor_New_Search["@SSN2"] = state.ClientRow["SSN2"];
          Sponsor_New_Search["@SSN3"] = state.ClientRow["SSN3"];
          break;
        case PotentialMatchFieldType.Name: Sponsor_New_Search = new Proc("Sponsor_New_SearchName");
          Sponsor_New_Search["@FirstName"] = state.ClientRow["FName"];
          Sponsor_New_Search["@LastName"] = state.ClientRow["LName"];
          break;
      }
      Debug.Assert(Sponsor_New_Search != null, "Sponsor_New_Search != null");

      using (Sponsor_New_Search)
      {
        Sponsor_New_Search["@MatchType"] = string.Format("Matched on {0} {1}",
          state.ClientRow.Field<bool>("IsSponsor") ? "Sponsor" :
            state.ClientRow.Field<bool>("IsSpouse") ? "Spouse" :
              "Dependent",
          state.FieldType);

        Sponsor_New_Search.ExecuteDataSet();

        // if this is the first time through, just take sproc result as our backdrop table
        if (PotentialClientMatches == null)
        {
          Sponsor_New_Search.Table0.Columns.Add("RecordCountId", typeof(int)); //backdrop table's RecordCount has to be a real value since a computed Count() would always be the same for all rows in this table
          foreach (DataRow r in Sponsor_New_Search.Table0.Rows) r["RecordCountId"] = Sponsor_New_Search.Table0.Rows.Count; //fyi, can't use expression based count column because that would reflect the total rows 
          PotentialClientMatches = new DataView(Sponsor_New_Search.Table0) {Sort = "RecordCountId, LName, FName"};
          //sort by ascending RecordCount so that the more specific matches are at the top
        }
        else //otherwise, just keep merging new results into the backdrop table
        {
          // clear out any existing match rows of the same match type, because each NewMatches batch of the same type should be considered an entirely new list of hits specific to the most recent inputs
          using (var v = new DataView(PotentialClientMatches.Table))
          {
            v.RowFilter = String.Format("MatchType = '{0}'", Sponsor_New_Search["@MatchType"]);
            v.DetachRowsAndDispose(true);
          }

          Sponsor_New_Search.Table0.Columns.Add("RecordCountId", typeof(int), "Count(LName)"); //bring the new rows in with their own count
          PotentialClientMatches.Table.Merge(Sponsor_New_Search.Table0);
        }
      }

    }

    #endregion

    #region Suspend stuff

    public enum SuspendDuration { Remove, OneMonth, OneYear, Forever };
    public void Suspend(string duration)
    {
      Suspend((SuspendDuration)Enum.Parse(typeof(SuspendDuration), duration));
    }

    public void Suspend(SuspendDuration duration, string remarks = null)
    {
      Assert.Check(duration == SuspendDuration.Remove || !String.IsNullOrEmpty(remarks), "Remarks must be provided when customer is suspended.");

      object suspensionExpiry;
      switch (duration)
      {
        case SuspendDuration.Remove: suspensionExpiry = DBNull.Value; break;
        case SuspendDuration.OneMonth: suspensionExpiry = DateTime.Today.AddMonths(1); break;
        case SuspendDuration.OneYear: suspensionExpiry = DateTime.Today.AddYears(1); break;
        case SuspendDuration.Forever: suspensionExpiry = DateTime.MaxValue; break;
        default: throw (new Exception("fix me"));
      }

// ReSharper disable InconsistentNaming
      using (var Sponsor_Suspend = new iTRAACProc("Sponsor_Suspend"))
// ReSharper restore InconsistentNaming
      {
        Sponsor_Suspend["@SponsorGUID"] = GUID;
        Sponsor_Suspend["@SuspensionExpiry"] = suspensionExpiry;
        Sponsor_Suspend["@Remarks"] = remarks;
        CacheTables(Sponsor_Suspend);
        OnPropertyChanged("Fields"); //nugget: this really bums me out that the DataSet.Merge() called by TableCache doesn't fire PropertyChanged events... i don't get how that makes sense!?!
        OnPropertyChanged("SponsorRemarks");
      }

    }

    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <param name="clientRow"></param>
    /// <param name="proposedActive"></param>
    /// <param name="message"></param>
    /// <param name="reasonRequired"></param>
    /// <param name="reason"></param>
    /// <returns>true = save was allowed, false = further action required</returns>
    public bool CheckClientActiveRules(DataRowView clientRow, bool proposedActive, out string message, out bool reasonRequired, string reason = null)
    {
      //nugget: this DataTable.ColumnChanging => Exception based approach doesn't really work out... 
      //nugget: good to document and remember since it's conceptually appealing
      //nugget: unfortunately the UI checkbox still flips itself even if the underlying field doesn't
      //nugget: DataTable.ColumnChanging: if (error) { e.Row.CancelEdit(); e.Row.SetColumnError(e.Column, err); throw (new Exception(err)); }

      message = null;
      reasonRequired = false;

      //sponsor rules...
      if (clientRow.Field<bool>("IsSponsor"))
      {
        //make sure not PCS'ed, which should be resolved via PCS specific logic elsewhere
        if (IsPCS) 
        {
          message = "Resolving PCS status will clear de-active.";
          return (false);
        }
        //otherwise get Reason for hitting Active ...
        if (reason == null) //... and no reason has been provided yet, ...
        {
          message = !proposedActive ? "Why deactivating this Household?\n(if Suspension or PCS, use those buttons)" :
                                                                                                                      "Why allowing this Household to be active again?";
          reasonRequired = true; // indicate reason required
          return (false);
        }

// ReSharper disable InconsistentNaming
        using (var Sponsor_Active_u = new iTRAACProc("Sponsor_Active_u"))
// ReSharper restore InconsistentNaming
        {
          Sponsor_Active_u["@SponsorGUID"] = GUID;
          Sponsor_Active_u["@RemarkTypeId"] = 23 * (!proposedActive ? 1 : -1); //! necessary because it's a "deactivated" oriented status 
          Sponsor_Active_u["@Reason"] = reason;
          CacheTables(Sponsor_Active_u);
        }
        if (!proposedActive) ShowDeactiveMembers = true;
        OnPropertyChanged("ShowDeactiveMembers");
        HouseMembers = HouseMembers; //for some reason OnPropertyChanged("HouseMembers") didn't refresh the Members Grid, i don't have a good guess but this little hack worked immediately so i'm moving on
        OnPropertyChanged("Fields");

        return (true); //no further processing desired after special sponsor level sproc
      }

      //spousal rules...
      //if attempting to deactivate spouse, let them know the rules...
      if (clientRow.Field<bool>("IsSpouse") && !proposedActive)
      {
        message = "Only valid way to deactivate Spouse is by *official* divorce (\"Legal Separation\" is not sufficient).\r\n"+
                  "Use the hidden Divorce button to the left of the Spouse.";
        return (false);
      }

        //if this is just a fresh new row the user doesn't want to fill out, just whack it straight away
      if (clientRow.Row.RowState == DataRowState.Added)
      {
        clientRow.DetachRow();
        //can't track an IsModified "undo" anymore now that i've had to move to a flat property that's set by everything that can be dirty:
        //OnPropertyChanged("IsModified");
        return (true);
      }

      //finally!  set the flag... 
      clientRow.Row.SetReadonlyField("Active", proposedActive);
      OnPropertyChanged("DependentsSelectionList");
      IsModified = true;
      return (true); //return success
    }

    public bool IsPCS
    {
      get
      {
        return (SponsorRemarks.Cast<DataRowView>().Any(r => r["RemarkTypeId"].ToString() == "22" && r.Field<bool>("Alert") && r["DeleteReason"].ToString() == ""));
      }
      set
      {
// ReSharper disable InconsistentNaming
        using (var Sponsor_Active_u = new iTRAACProc("Sponsor_Active_u"))
// ReSharper restore InconsistentNaming
        {
          Sponsor_Active_u["@SponsorGUID"] = GUID;
          Sponsor_Active_u["@RemarkTypeId"] = 22 * (value ? 1 : -1);
          CacheTables(Sponsor_Active_u);
        }

        if (value) ShowDeactiveMembers = true;
        OnPropertyChanged("ShowDeactiveMembers");
        HouseMembers = HouseMembers; //for some reason OnPropertyChanged("HouseMembers") didn't refresh the Members Grid, i don't have a good guess but this little hack worked immediately so i'm moving on
        OnPropertyChanged("Fields"); //for Fields["Active"] references
      }
    }

    public void RemoveRemark(DataRowView remarkRow, string reason)
    {
      RemarkModel.Remove(remarkRow, reason);
      //OnPropertyChanged("IsPCS");
    }

    void ClientTableColumnChanged(object sender, DataColumnChangeEventArgs e)
    {
      if (!IsMyRow(e.Row)) return;

      //dynamically tweak the CCode field with first letter of last name + last 4 SSN... so that the CCode is auto-updated with SSN/LastName corrections 
      if ( (e.Column.ColumnName == "SSN3" || e.Column.ColumnName == "LName") && e.Row.Field<bool>("IsSponsor") )
      {
        e.Row["CCode"] = e.Row["LName"].ToString().Left(1).ToUpper() + e.Row["SSN3"];
      }

      PotentialMatchFieldType matchFieldType;
      if ( Enum.TryParse(e.Column.ColumnName.Left(3), out matchFieldType) ||
           Enum.TryParse(e.Column.ColumnName.Right(4), out matchFieldType) )
      {
        _potentialClientMatchesBgWorker.Initiate(new PotentialClientMatchesState { ClientRow = e.Row, FieldType = matchFieldType });
      }

      //e.Row.EndEdit(); //nugget:if you're binding to a datagrid, apparently there is an implicit BeginEdit() fired for the row, which means each field edit is ignored until you force a current row pointer change or something dramatic like that
    }

    void ClientTableRowChanged(object sender, DataRowChangeEventArgs e)
    {
      if (!IsMyRow(e.Row)) return;

      if (e.Row.RowState == DataRowState.Added && e.Row["SSN1"].ToString() == "") return; //added doesn't mean anything until it's been filled out and we'll pop here again when that happens
      //this way the user can still cancel an empty added row and never get dirty

      e.Row.ClearErrors();
      IsModified = true;
    }

    protected override bool SaveSubClass()
    {
      //validate everything first so we see all the red boxes at once... 
      var isValid = true;

      ValidateGeneric(ref isValid, "Rank");
      ValidateGeneric(ref isValid, "DEROS");
      ValidateGeneric(ref isValid, "DutyLocation");

      ValidateGeneric(ref isValid, "DutyPhoneDSN1", "'?'.Length == 3", "Enter 3 Digits");
      ValidateGeneric(ref isValid, "DutyPhoneDSN2", "'?'.Length == 4", "Enter 4 Digits");

      ValidateGeneric(ref isValid, "OfficialMailCMR");
      ValidateGeneric(ref isValid, "OfficialMailBox");
      ValidateGeneric(ref isValid, "OfficialMailCity");
      ValidateGeneric(ref isValid, "OfficialMailState");
      ValidateGeneric(ref isValid, "OfficialMailZip", "'?'.Length == 5", "Enter 5 Digits");

      ValidateGeneric(ref isValid, "HomePhoneCountry");
      ValidateGeneric(ref isValid, "HomePhone");

      var validateUTAP = Fields.Field<bool>("IsUTAPActive");
      ValidateGeneric(Fields, ref isValid, "HomeStreet,IsUTAPActive", validateUTAP);
      ValidateGeneric(Fields, ref isValid, "HomeStreetNumber,IsUTAPActive", validateUTAP);
      ValidateGeneric(Fields, ref isValid, "HomeCity,IsUTAPActive", validateUTAP);
      ValidateGeneric(Fields, ref isValid, "HomePostal,IsUTAPActive", validateUTAP);

      foreach(DataRowView member in HouseMembers)
      {
        ValidateGeneric(member, ref isValid, "SSN1", true, "'?'.Length == 3", "Enter 3 Digits");
        ValidateGeneric(member, ref isValid, "SSN2", true, "'?'.Length == 2", "Enter 2 Digits");
        ValidateGeneric(member, ref isValid, "SSN3", true, "'?'.Length == 4", "Enter 4 Digits");
        if (ValidateGeneric(member, ref isValid, "FName"))
          ValidateGeneric(member, ref isValid, "FName", true, "'?' != '?'.toUpperCase()", "Use proper uppper/lower casing for all names.\nForms will automatically print in all upper case.");
        if (ValidateGeneric(member, ref isValid, "LName"))
          ValidateGeneric(member, ref isValid, "LName", true, "'?' != '?'.toUpperCase()", "Use proper uppper/lower casing for all names.\nForms will automatically print in all upper case.");
      }

      if (!isValid)
      {
        ShowUserMessage("Please correct all highlighted fields before saving.");
        return (false);
      }

      //then save attempt to save everything if we made it through all the validation...
      if (Fields.IsDirty()) if (!SaveJustSponsorRecord()) return (false);

      foreach (var member in HouseMembers.Cast<DataRowView>().Where(member => member.IsDirty()))
// ReSharper disable InconsistentNaming
        using (Proc Client_u = new iTRAACProc("Client_u"))
// ReSharper restore InconsistentNaming
        {
          Client_u.AssignValues(member);
          if (!Client_u.ExecuteDataSet(UserMessagePrefix)) return (false);
          member.AcceptChanges();
          CacheTables(Client_u);
        }

      RemarkModel.SaveRemarks("SponsorGUID", GUID, UserMessagePrefix, SponsorRemarks);

      return (true);
    }

    private bool SaveJustSponsorRecord()
    {
// ReSharper disable InconsistentNaming
      using (var Sponsor_u = new iTRAACProc("Sponsor_u"))
// ReSharper restore InconsistentNaming
      {
        Fields.Row.SetAllNonDBNullColumnsToEmptyString("-");
        try
        {
          Sponsor_u.AssignValues(Fields);
          if (!Sponsor_u.ExecuteDataSet(UserMessagePrefix)) return (false); //base class clears "Fields" dirty flags for us
          CacheTables(Sponsor_u);
        }
        finally
        {
          Fields.Row.RemoveEmptyPlaceholder("-");
        }
      }
      return (true);
    }

    public override string WhatIsModified
    {
      get
      {
        return(
          (Fields.IsDirty() ? "\n   * Household edits" : null) +
          (HouseMembers.IsDirty(respectRowFilter: false) ? "\n   * Members edits" : null) +
          (SponsorRemarks.IsDirty(respectRowFilter: false) ? "\n   * Remarks edits" : null)
        );
      }
    }

    //public override bool IsModified
    //{
    //  get
    //  {
    //    return (base.IsModified || 
    //      (Transactions != null && Transactions.HasPending || 
    //      HouseMembers.IsDirty() || 
    //      SponsorRemarks.IsDirty() ));
    //  }
    //}

    /// <summary>
    /// Example Usage: sponsor.Transactions.Add(...);
    /// </summary>
    public TransactionList Transactions { get; private set; }

  }
}