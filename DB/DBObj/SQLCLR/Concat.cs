using System;
using System.Data;
using Microsoft.SqlServer.Server;
using System.Data.SqlTypes;
using System.IO;
using System.Text;

[Serializable]
[SqlUserDefinedAggregate(
Format.UserDefined, //use clr serialization to serialize the intermediate result
IsInvariantToNulls = true, //optimizer property
IsInvariantToDuplicates = false, //optimizer property
IsInvariantToOrder = false, //optimizer property
MaxByteSize = 8000) //maximum size in bytes of persisted value
]
public class Concat : IBinarySerialize {
  /// The variable that holds the intermediate result of the concatenation
  private StringBuilder intermediateResult;
  /// Initialize the internal data structures

  public void Init() {
    intermediateResult = new StringBuilder();
  }

  ///Accumulate the next value, not if the value is null
  /// <param name="value"></param>
  public void Accumulate(SqlString value) {
    if (value.IsNull) {
      return;
    }
    intermediateResult.Append(value.Value).Append(", ");
  }

  /// Merge the partially computed aggregate with this aggregate.
  /// <param name="other"></param>
  public void Merge(Concat other) {
    intermediateResult.Append(other.intermediateResult);
  }

  /// Called at the end of aggregation, to return the results of the aggregation.
  /// <returns></returns>
  public SqlString Terminate() {
    //delete the trailing comma, if any
    return((intermediateResult != null && intermediateResult.Length > 0)?
      new SqlString(intermediateResult.ToString(0, intermediateResult.Length - 2)):
      new SqlString());
  }

  public void Read(BinaryReader r) {
    intermediateResult = new StringBuilder(r.ReadString());
  }

  public void Write(BinaryWriter w) {
    w.Write(intermediateResult.ToString());
  }

}

