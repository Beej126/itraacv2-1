using System;
using System.Collections;
using System.Linq;
using System.Data;

namespace iTRAACv2.Model
{
  class RemarkModel
  {
    static RemarkModel()
    {
// ReSharper disable InconsistentNaming
      using (var RemarkTypes_s = new Proc("RemarkTypes_s"))
// ReSharper restore InconsistentNaming
      {
        ModelBase.CacheTables(RemarkTypes_s);
      }
    }

    static public IEnumerable SingleRemarkType(object remarkTypeId, bool positiveState)
    {
      var row = ModelBase.DsCache.Tables["RemarkType"].DefaultView.FindRows(remarkTypeId);
      return(row.Select(r => new { 
        Title = (positiveState ? "" : "Un-") + r["Title"].ToString(),
        RemarkTypeId = (positiveState ? 1 : -1) * Convert.ToInt16(r["RemarkTypeId"]) }));
    } 

    static public void CommonRemarkTableSettings(DataView v)
    {
      v.Sort = "Alert desc, LastUpdate desc"; //show Alerts first no matter what, then sort by Descending LastUpdated
      DataTable t = v.Table;

      var createdBy = t.TableName + "_CreatedBy";
      var updatedBy = t.TableName + "_UpdatedBy";
      const string columnExpression = "Parent({0}).FirstName + ' ' + Parent({0}).LastName + ' (' + Parent({0}).TaxOfficeName + ')'";

      t.DataSet.AddRelation(createdBy, TaxOfficeModel.UserTable.Columns["AgentGUID"], t.Columns["CreateAgentGUID"]);
      createdBy = String.Format(columnExpression, createdBy);
      t.AddColumn("Created By", typeof(string), createdBy);

      t.DataSet.AddRelation(updatedBy, TaxOfficeModel.UserTable.Columns["AgentGUID"], t.Columns["LastAgentGUID"]);
      updatedBy = String.Format(columnExpression, updatedBy);
      t.AddColumn("Updated By", typeof(string), updatedBy);

      //only certain columns are editable
      foreach (DataColumn c in t.Columns) c.ReadOnly = true;
      t.Columns["RowGUID"].ReadOnly = false; //so that we can bring back the RowGUID after a new row save
      t.Columns["Alert"].ReadOnly = false;
      t.Columns["Title"].ReadOnly = false;
      t.Columns["Remarks"].ReadOnly = false;
      //we don't want these to be directly editable: t.Columns["DeleteReason"].ReadOnly = false; 
      //we don't want these to be directly editable: t.Columns["AlertResolved"].ReadOnly = false;
    }

    static public string ShowDeletedFilter(bool show)
    {
      return (show ? "" : "ISNULL(DeleteReason, '*NULL*') = '*NULL*'");
    }

    static public int Flip(int remarkTypeId, bool condition)
    {
      return (condition ? remarkTypeId : remarkTypeId * -1);
    }

    static public void SaveNew(string fkguid, int remarkTypeId, string remarks = null, string title = null, bool alert = false)
    {
// ReSharper disable InconsistentNaming
      using (var Remark_u = new iTRAACProc("Remark_u"))
// ReSharper restore InconsistentNaming
      {
        Remark_u["@FKRowGUID"] = fkguid;
        Remark_u["@RemarkTypeId"] = remarkTypeId;
        Remark_u["@Title"] = title;
        Remark_u["@Remarks"] = remarks;
        ModelBase.CacheTables(Remark_u);
      }
    }

    static public void AddNew(ModelBase model, DataView v, int remarkTypeId, string remarks = null, string title = null, bool alert = false)
    {
      DataRowView r = v.AddNew();
      r["TableId"] = (model is SponsorModel) ? 9 : 10;
      r["FKRowGUID"] = model.GUID;
      r["RemarkTypeId"] = remarkTypeId;
      r["Remarks"] = remarks;
      r["Title"] = title;
      r["Alert"] = alert;
      r["CreateDate"] = r["LastUpdate"] = DateTime.Now;
      r["CreateAgentGUID"] = UserModel.Current.GUID;
      r["CreateTaxOfficeId"] = SettingsModel.TaxOfficeId;
      r.Row.Table.Rows.Add(r.Row);
    }

    static public void SaveRemarks(string ownerGUIDFieldName, string ownerGUID, string userMessagePrefix, DataView v)
    {
      if (v == null) return;

      string savefilter = v.RowFilter;
      v.RowFilter = "";

// ReSharper disable InconsistentNaming
      using (var Remark_u = new iTRAACProc("Remark_u"))
// ReSharper restore InconsistentNaming
      foreach (var remark in v.Cast<DataRowView>().Where(remark => remark.IsDirty()))
      {
        Remark_u.AssignValues(remark);
        if (!Remark_u.ExecuteNonQuery(userMessagePrefix + " Save Remark: ")) continue;
        remark["RowGUID"] = Remark_u["@RowGUID"]; //for inserts
        remark.AcceptChanges();
      }
      v.RowFilter = savefilter;
    }

    static public void Remove(DataRowView remarkRow, string comments)
    {
      //if this remark row hasn't been saved to the DB yet, then just whack it here client side and forget about it
      if (remarkRow.Row.RowState == DataRowState.Added)
      {
        remarkRow.DetachRow();
        return;
      }

      remarkRow.Row.SetReadonlyField("DeleteReason", comments);

      /* this going straight to the DB approach was a little overzealous i think...
      RemarkRow.AcceptChanges(); //nix the dirty flag for this edit because we're saving to DB immediately

      //can't using() because scope drops and nulls out the Proc object out before the background thread executes
      //but ExecuteNonQueryBackground() calls Dispose when it's done so should be ok
      iTRAACProc Remark_d = new iTRAACProc("Remark_d");
      Remark_d["@RemarkGUID"] = RemarkRow["RowGUID"];
      Remark_d["@Reason"] = Comments;
      Remark_d.ExecuteNonQueryBackground("Delete Remark: ", false);
      */
    }

    static public bool DenyEdit(DataRowView r, string columnName, out string message)
    {
      message = null;

      if (!UserModel.Current.Access.IsAdmin && r.Field<bool>("Alert") && r.Field<int>("CreateTaxOfficeId") != SettingsModel.TaxOfficeId)
      {
        message = "Alerted Remarks can only be changed by originating office (or Admin)";
      }
      else if (r["CategoryId"].ToString() != "")
      {
        message = "This kind of sytem generated remark should only be changed via the corresponding special alert button located elsewhere";
      }
      //system generated Titles aren't editable since they come from the RemarkType table (see Remark_v)
      else if (columnName == "Title" && r.Field<int>("RemarkTypeId") != 0)
        message = "System generated Titles aren't editable";

      return (message != null);
    }

  }

}
