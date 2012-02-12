using System;
using System.ComponentModel.DataAnnotations;

class MinStringLengthAttribute : StringLengthAttribute
{
  public MinStringLengthAttribute(int MinLength) : base(int.MaxValue)
  {
    MinimumLength = MinLength;
    ErrorMessage = String.Format("Must be {0} characters.", MinLength);
  }
  public override bool IsValid(object value)
  {
    return (value != null) && base.IsValid(value); //nugget: somehow default IsValid returns true for null!?!?
  }
}
