using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace iTRAACv2.Model
{
  class TaxOfficeModel : ModelBase // INotifyPropertyChanged
  {
    static public DataView AllOffices { get; private set; } //primarily used for the VAT Offices master info list
    static public DataView AllOfficesPlusAny { get; private set; } //primarily used for the ucTaxFormNumber office combobox
    static public TaxOfficeModel Current { get; private set; }
    static public DataTable TaxOfficeTable { get { return(DsCache.Tables["TaxOffice"]); }}
    static public DataTable UserTable { get { return (DsCache.Tables["User"]); } }

    public override string WhatIsModified { get { return (null); } }
    
    /// <summary>
    /// Returns a DataView representing the *Active* Users for the specified Office
    /// </summary>
    /// <param name="officeDataRowView">Must be a DataRowView originally obtained via the TaxOffice properties like AllOffices</param>
    /// <returns></returns>
    static public DataView OfficeUsers(object officeDataRowView) //primarily used for the sub-detail DataGrid full of Users under each Office row
    {
      var dv = ((DataRowView)officeDataRowView).CreateChildView("OfficeUsers");
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

    private static DataRow FindUserByGUID(Guid userGUID)
    {
      return(UserTable.Rows.Find(new object[] { SettingsModel.TaxOfficeId, userGUID }));
    }

    public void ReActivateUser(Guid userGUID)
    {
      FindUserByGUID(userGUID)["Active"] = true;
    }

    public bool AddNewUser(string firstName, string lastName, string email, string phone)
    {
      if (UserTable.DefaultView.Find(firstName + " " + lastName) > -1)
      {
        App.ShowUserMessage("UserName already exists.");
        return(false);
      }

      var newUser = UserTable.NewRow();
      newUser["TaxOfficeId"] = SettingsModel.TaxOfficeId;
      newUser["RowGUID"] = Guid.NewGuid();
      newUser["FirstName"] = firstName;
      newUser["LastName"] = lastName;
      newUser["DSNPhone"] = phone;
      newUser["Email"] = email;
      newUser["SigBlock"] = lastName + ", " + firstName;
      newUser["Active"] = true;
      UserTable.Rows.Add(newUser);

      return (true);
    }

    private TaxOfficeModel() { } //ensure "Current" is a singleton
    static TaxOfficeModel()
    {
      if (WPFHelpers.DesignMode) return;

      Current = new TaxOfficeModel();

      CacheTables("TaxOffice_Init");

      TaxOfficeTable.DefaultView.Sort = "TaxOfficeId";

      UserTable.PrimaryKey = new[] { UserTable.Columns["TaxOfficeId"], UserTable.Columns["RowGUID"] };
      UserTable.Columns.Add("UserNameId", typeof(string), "FirstName + ' ' + LastName");
      UserTable.DefaultView.Sort = "UserNameId";


      {// AllOffices
        AllOffices = new DataView(TaxOfficeTable) {RowFilter = "Office <> 'Any'", Sort = "Active desc, Office"};
      }

      {// AllOfficesPlusAny
        var any = TaxOfficeTable.NewRow();
        any["TaxOfficeId"] = 0; //we can't jam a DBNull in here since ADO.Net doesn't allow NULLs on Primary Keys
        any["OfficeCode"] = "__";
        any["Office"] = "Any";
        any["Active"] = true;
        TaxOfficeTable.Rows.InsertAt(any, 0);

        AllOfficesPlusAny = new DataView(TaxOfficeTable) {Sort = "Active desc, Office"};
      }

      // Current.Fields
      Current.Fields = TaxOfficeTable.DefaultView.FindRows(SettingsModel.TaxOfficeId)[0];

      {// Current.POC
        DsCache.Relations.Add("POC",
          new[] { UserTable.Columns["TaxOfficeId"], UserTable.Columns["RowGUID"] },
          new[] { TaxOfficeTable.Columns["TaxOfficeId"], TaxOfficeTable.Columns["POC_UserGUID"] }, false);
        Current.Fields.PropertyChanged += (s, e) => { if (e.PropertyName == "POC_UserGUID") Current.OnPropertyChanged("POC"); };

        TaxOfficeTable.Columns.Add("POC Name", typeof(string), "Parent(POC).UserNameId"); //nugget: syntax for pulling parent relationship columns over to the child DataTable
        TaxOfficeTable.Columns.Add("POC Phone", typeof(string), "Parent(POC).DSNPhone");
        TaxOfficeTable.Columns.Add("POC Email", typeof(string), "Parent(POC).Email");
      }

      {// Current.ActiveUsers
        DsCache.Relations.Add("OfficeUsers", TaxOfficeTable.Columns["TaxOfficeId"], UserTable.Columns["TaxOfficeId"], false);
        Current.ActiveUsers = Current.Fields.CreateChildView("OfficeUsers");
        Current.ActiveUsers.RowFilter = "Active = 1";
        Current.ActiveUsers.Sort = "UserNameId";
        UserTable.RowChanged += UserTableRowChanged;
      }

    }

    static void UserTableRowChanged(object sender, DataRowChangeEventArgs e)
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
        using (var taxOfficeU = new iTRAACProc("TaxOffice_u"))
        {
          if (taxOfficeU.AssignValues(Current.Fields).ExecuteNonQuery("Tax Office - ", true))
            Fields.AcceptChanges();
          else success = false;
        }
      }

      if (UserTable.GetChanges() != null)
      {
        using (var userU = new iTRAACProc("User_u"))
        {
          foreach (DataRowView r in from DataRowView r in UserTable.DefaultView where r.IsDirty() select r)
          {
            if (userU.AssignValues(r).ExecuteNonQuery(String.Format("Saving User: {0} - ", r["UserNameId"])))
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
