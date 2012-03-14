using System;
using System.ComponentModel.DataAnnotations;

// ReSharper disable CheckNamespace
class MinStringLengthAttribute : StringLengthAttribute
// ReSharper restore CheckNamespace
{
  public MinStringLengthAttribute(int minLength) : base(int.MaxValue)
  {
    MinimumLength = minLength;
    ErrorMessage = String.Format("Must be {0} characters.", minLength);
  }
  public override bool IsValid(object value)
  {
    return (value != null) && base.IsValid(value); //nugget: somehow default IsValid returns true for null!?!?
  }
}
