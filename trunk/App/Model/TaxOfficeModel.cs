using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.ComponentModel;

namespace iTRAACv2
{
  class TaxOfficeModel : ModelBase // INotifyPropertyChanged
  {
    static public DataView AllOffices { get; private set; } //primarily used for the VAT Offices master info list
    static public DataView AllOfficesPlusAny { get; private set; } //primarily used for the ucTaxFormNumber office combobox
    static public TaxOfficeModel Current { get; private set; }
    static public DataTable TaxOfficeTable { get { return(dsCache.Tables["TaxOffice"]); }}
    static public DataTable UserTable { get { return (dsCache.Tables["User"]); } }

    public override string WhatIsModified { get { return (null); } }
    
    /// <summary>
    /// Returns a DataView representing the *Active* Users for the specified Office
    /// </summary>
    /// <param name="OfficeDataRowView">Must be a DataRowView originally obtained via the TaxOffice properties like AllOffices</param>
    /// <returns></returns>
    static public DataView OfficeUsers(object OfficeDataRowView) //primarily used for the sub-detail DataGrid full of Users under each Office row
    {
      DataView dv = ((DataRowView)OfficeDataRowView).CreateChildView("OfficeUsers");
      dv.RowFilter = "Active = 1";
      dv.Sort = "UserNameId";
      return (dv);
    }

    public DataView ActiveUsers { get; private set; } //primarily used for the POC combobox

    public IEnumerable<object> AllUsersExceptLocal { get { //primarily used for the "Add Existing User" listbox
        return (from DataRow u in UserTable.AsEnumerable() //select the users which are not already active in this office
                where !(from DataRow l in UserTable.AsEnumerable()
                        where (int)l["TaxOfficeId"] == SettingsModel.TaxOfficeId && (bool)l["Active"]
                        select (Guid)l["RowGUID"]).Contains((Guid)u["RowGUID"]) 
                select new { UserName = u["UserNameId"], UserGUID = u["RowGUID"] }).Distinct().OrderBy(o => o.UserName); } }

    private DataRow FindUserByGUID(Guid UserGUID)
    {
      return(UserTable.Rows.Find(new object[] { SettingsModel.TaxOfficeId, UserGUID }));
    }

    public void ReActivateUser(Guid UserGUID)
    {
      FindUserByGUID(UserGUID)["Active"] = true;
    }

    public bool AddNewUser(string FirstName, string LastName, string Email, string Phone)
    {
      if (UserTable.DefaultView.Find(FirstName + " " + LastName) > -1)
      {
        App.ShowUserMessage("UserName already exists.");
        return(false);
      }

      DataRow NewUser = UserTable.NewRow();
      NewUser["TaxOfficeId"] = SettingsModel.TaxOfficeId;
      NewUser["RowGUID"] = Guid.NewGuid();
      NewUser["FirstName"] = FirstName;
      NewUser["LastName"] = LastName;
      NewUser["DSNPhone"] = Phone;
      NewUser["Email"] = Email;
      NewUser["SigBlock"] = LastName + ", " + FirstName;
      NewUser["Active"] = true;
      UserTable.Rows.Add(NewUser);

      return (true);
    }

    private TaxOfficeModel() { } //ensure "Current" is a singleton
    static TaxOfficeModel()
    {
      if (WPFHelpers.DesignMode) return;

      Current = new TaxOfficeModel();

      CacheTables("TaxOffice_Init");

      TaxOfficeTable.DefaultView.Sort = "TaxOfficeId";

      UserTable.PrimaryKey = new DataColumn[] { UserTable.Columns["TaxOfficeId"], UserTable.Columns["RowGUID"] };
      UserTable.Columns.Add("UserNameId", typeof(string), "FirstName + ' ' + LastName");
      UserTable.DefaultView.Sort = "UserNameId";


      {// AllOffices
        AllOffices = new DataView(TaxOfficeTable);
        AllOffices.RowFilter = "Office <> 'Any'";
        AllOffices.Sort = "Active desc, Office";
      }

      {// AllOfficesPlusAny
        DataRow Any = TaxOfficeTable.NewRow();
        Any["TaxOfficeId"] = 0; //we can't jam a DBNull in here since ADO.Net doesn't allow NULLs on Primary Keys
        Any["OfficeCode"] = "__";
        Any["Office"] = "Any";
        Any["Active"] = true;
        TaxOfficeTable.Rows.InsertAt(Any, 0);

        AllOfficesPlusAny = new DataView(TaxOfficeTable);
        AllOfficesPlusAny.Sort = "Active desc, Office";
      }

      // Current.Fields
      Current.Fields = TaxOfficeTable.DefaultView.FindRows(SettingsModel.TaxOfficeId)[0];

      {// Current.POC
        dsCache.Relations.Add("POC",
          new DataColumn[] { UserTable.Columns["TaxOfficeId"], UserTable.Columns["RowGUID"] },
          new DataColumn[] { TaxOfficeTable.Columns["TaxOfficeId"], TaxOfficeTable.Columns["POC_UserGUID"] }, false);
        Current.Fields.PropertyChanged += (s, e) => { if (e.PropertyName == "POC_UserGUID") Current.OnPropertyChanged("POC"); };

        TaxOfficeTable.Columns.Add("POC Name", typeof(string), "Parent(POC).UserNameId"); //nugget: syntax for pulling parent relationship columns over to the child DataTable
        TaxOfficeTable.Columns.Add("POC Phone", typeof(string), "Parent(POC).DSNPhone");
        TaxOfficeTable.Columns.Add("POC Email", typeof(string), "Parent(POC).Email");
      }

      {// Current.ActiveUsers
        dsCache.Relations.Add("OfficeUsers", TaxOfficeTable.Columns["TaxOfficeId"], UserTable.Columns["TaxOfficeId"], false);
        Current.ActiveUsers = Current.Fields.CreateChildView("OfficeUsers");
        Current.ActiveUsers.RowFilter = "Active = 1";
        Current.ActiveUsers.Sort = "UserNameId";
        UserTable.RowChanged += new DataRowChangeEventHandler(UserTable_RowChanged);
      }

    }

    static void UserTable_RowChanged(object sender, DataRowChangeEventArgs e)
    {
      //if the the Active flag has changed for a UserTable Row of the current office, then notify the observer's of the views which depend on the active flag
      if ((int)e.Row["TaxOfficeId"] == SettingsModel.TaxOfficeId && (bool)e.Row["Active", DataRowVersion.Original] != (bool)e.Row["Active"])
      {
        Current.IsModified = true;
        Current.OnPropertyChanged("ActiveUsers");
        Current.OnPropertyChanged("AllUsersExceptLocal");
      }
    }

    ~TaxOfficeModel()
    {
      AllOffices.Dispose();
      AllOfficesPlusAny.Dispose();
      ActiveUsers.Dispose();
    }


    protected override void LoadSubClass()
    {
      throw new NotImplementedException();
    }

    protected override bool SaveSubClass()
    {
      bool success = true;

      if (Fields.IsDirty())
      {
        using (iTRAACProc TaxOffice_u = new iTRAACProc("TaxOffice_u"))
        {
          if (TaxOffice_u.AssignValues(Current.Fields).ExecuteNonQuery("Tax Office - ", true))
            Fields.AcceptChanges();
          else success = false;
        }
      }

      if (UserTable.GetChanges() != null)
      {
        using (iTRAACProc User_u = new iTRAACProc("User_u"))
        {
          foreach (DataRowView r in UserTable.DefaultView)
          {
            if (!r.IsDirty()) continue;
            if (User_u.AssignValues(r).ExecuteNonQuery(String.Format("Saving User: {0} - ", r["UserNameId"]), false))
              r.AcceptChanges();
            else success = false;
          }
        }
      }

      return (success);
    }

    protected override void UnLoadSubClass()
    {
      throw new NotImplementedException();
    }

    public override System.Windows.Data.BindingBase TitleBinding
    {
      get { throw new NotImplementedException(); }
    }
  }
}
