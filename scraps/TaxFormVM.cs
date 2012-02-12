using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iTRAACv2
{
  class TaxFormVM
  {

    public TaxFormVM(string TaxFormGUID) : this(TaxFormBO.Lookup(TaxFormGUID)) {}

    public TaxFormVM(TaxFormBO taxform)
    {
      TaxForm = taxform;
    }

    public TaxFormBO TaxForm { get; private set; }

    public bool Unlockable
    {
      get
      {
        return (User.Current.Access.HasUnlockForm);
      }
    }

    public string LockToolTip
    {
      get
      {
        return (
          TaxForm.IsReadOnly ? //form locked?
          (Unlockable ? "Enable Form Corrections" : "Requires [HasUnlockForm] Access" ) //yes
          : "Form Not Filed / Editable"
        );
      }
    }
    
  }
}
