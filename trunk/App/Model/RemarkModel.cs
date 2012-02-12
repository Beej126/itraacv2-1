using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace iTRAACv2
{
  class RemarkModel
  {
    static RemarkModel()
    {
      using (Proc RemarkTypes_s = new Proc("RemarkTypes_s"))
      {
        ModelBase.CacheTables(RemarkTypes_s);
      }
    }

    static public IEnumerable SingleRemarkType(object RemarkTypeId, bool PositiveState)
    {
      var row = ModelBase.dsCache.Tables["RemarkType"].DefaultView.FindRows(RemarkTypeId);
      return(row.Select(r => new { 
        Title = (PositiveState ? "" : "Un-") + r["Title"].ToString(),
        RemarkTypeId = (PositiveState ? 1 : -1) * Convert.ToInt16(r["RemarkTypeId"]) }));
    } 

    static public void CommonRemarkTableSettings(DataView v)
    {
      v.Sort = "Alert desc, LastUpdate desc"; //show Alerts first no matter what, then sort by Descending LastUpdated
      DataTable t = v.Table;

      string CreatedBy = t.TableName + "_CreatedBy";
      string UpdatedBy = t.TableName + "_UpdatedBy";
      string ColumnExpression = "Parent({0}).FirstName + ' ' + Parent({0}).LastName + ' (' + Parent({0}).TaxOfficeName + ')'";

      t.DataSet.AddRelation(CreatedBy, TaxOfficeModel.UserTable.Columns["AgentGUID"], t.Columns["CreateAgentGUID"]);
      CreatedBy = String.Format(ColumnExpression, CreatedBy);
      t.AddColumn("Created By", typeof(string), CreatedBy);

      t.DataSet.AddRelation(UpdatedBy, TaxOfficeModel.UserTable.Columns["AgentGUID"], t.Columns["LastAgentGUID"]);
      UpdatedBy = String.Format(ColumnExpression, UpdatedBy);
      t.AddColumn("Updated By", typeof(string), UpdatedBy);

      //only certain columns are editable
      foreach (DataColumn c in t.Columns) c.ReadOnly = true;
      t.Columns["RowGUID"].ReadOnly = false; //so that we can bring back the RowGUID after a new row save
      t.Columns["Alert"].ReadOnly = false;
      t.Columns["Title"].ReadOnly = false;
      t.Columns["Remarks"].ReadOnly = false;
      //we don't want these to be directly editable: t.Columns["DeleteReason"].ReadOnly = false; 
      //we don't want these to be directly editable: t.Columns["AlertResolved"].ReadOnly = false;
    }

    static public string ShowDeletedFilter(bool Show)
    {
      return (Show ? "" : "ISNULL(DeleteReason, '*NULL*') = '*NULL*'");
    }

    static public int Flip(int RemarkTypeId, bool condition)
    {
      return (condition ? RemarkTypeId : RemarkTypeId * -1);
    }

    static public void SaveNew(string FKGUID, int RemarkTypeId, string Remarks = null, string Title = null, bool Alert = false)
    {
      using (iTRAACProc Remark_u = new iTRAACProc("Remark_u"))
      {
        Remark_u["@FKRowGUID"] = FKGUID;
        Remark_u["@RemarkTypeId"] = RemarkTypeId;
        Remark_u["@Title"] = Title;
        Remark_u["@Remarks"] = Remarks;
        ModelBase.CacheTables(Remark_u);
      }
    }

    static public void AddNew(ModelBase Model, DataView v, int RemarkTypeId, string Remarks = null, string Title = null, bool Alert = false)
    {
      DataRowView r = v.AddNew();
      r["TableId"] = (Model is SponsorModel) ? 9 : 10;
      r["FKRowGUID"] = Model.GUID;
      r["RemarkTypeId"] = RemarkTypeId;
      r["Remarks"] = Remarks;
      r["Title"] = Title;
      r["Alert"] = Alert;
      r["CreateDate"] = r["LastUpdate"] = DateTime.Now;
      r["CreateAgentGUID"] = UserModel.Current.GUID;
      r["CreateTaxOfficeId"] = SettingsModel.TaxOfficeId;
      r.Row.Table.Rows.Add(r.Row);
    }

    static public void SaveRemarks(string OwnerGUIDFieldName, string OwnerGUID, string UserMessagePrefix, DataView v)
    {
      if (v == null) return;

      string savefilter = v.RowFilter;
      v.RowFilter = "";

      using (iTRAACProc Remark_u = new iTRAACProc("Remark_u"))
      foreach (DataRowView remark in v)
      {
        if (remark.IsDirty())
        {
          Remark_u.AssignValues(remark);
          if (Remark_u.ExecuteNonQuery(UserMessagePrefix + " Save Remark: ", false))
          {
            remark["RowGUID"] = Remark_u["@RowGUID"]; //for inserts
            remark.AcceptChanges();
          }
        }
      }
      v.RowFilter = savefilter;
    }

    static public void Remove(System.Data.DataRowView RemarkRow, string Comments)
    {
      //if this remark row hasn't been saved to the DB yet, then just whack it here client side and forget about it
      if (RemarkRow.Row.RowState == DataRowState.Added)
      {
        RemarkRow.DetachRow();
        return;
      }

      RemarkRow.Row.SetReadonlyField("DeleteReason", Comments);

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

    static public bool DenyEdit(DataRowView r, string ColumnName, out string Message)
    {
      Message = null;

      if (!UserModel.Current.Access.IsAdmin && r.Field<bool>("Alert") && r.Field<int>("CreateTaxOfficeId") != SettingsModel.TaxOfficeId)
      {
        Message = "Alerted Remarks can only be changed by originating office (or Admin)";
      }
      else if (r["CategoryId"].ToString() != "")
      {
        Message = "This kind of sytem generated remark should only be changed via the corresponding special alert button located elsewhere";
      }
      //system generated Titles aren't editable since they come from the RemarkType table (see Remark_v)
      else if (ColumnName == "Title" && r.Field<int>("RemarkTypeId") != 0)
        Message = "System generated Titles aren't editable";

      return (Message != null);
    }

  }

}
