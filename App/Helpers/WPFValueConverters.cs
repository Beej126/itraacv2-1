using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
//using System.Windows.Media; //brushes

// ReSharper disable CheckNamespace
namespace WPFValueConverters
// ReSharper restore CheckNamespace
{
  //by making the ValueConverter a MarkupExtension we avoid the typical step of needing to create an instance of the converter in the XAML Resources block

  public abstract class MarkupExtensionConverter : MarkupExtension
  {
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return this;
    }
  }

  /// <summary>
  /// value passed in is the string to slice chars from
  /// negative parameter means take X chars from the left, positive means take from the right
  /// </summary>
  public class LeftRightConverter : MarkupExtensionConverter, IValueConverter
  {
// ReSharper disable EmptyConstructor
    public LeftRightConverter() { } //to avoid an XAML annoying warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return (((int)parameter < 0) ? ((string)value).Left(Math.Abs((int)parameter)) : ((string)value).Right((int)parameter));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }


  public class BoolToItalicConverter : MarkupExtensionConverter, IValueConverter
  {
// ReSharper disable EmptyConstructor
    public BoolToItalicConverter() { } //to avoid an XAML annoying warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return ((bool)value ? FontStyles.Italic : FontStyles.Normal);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class InverseBooleanConverter : MarkupExtensionConverter, IValueConverter
  {
// ReSharper disable EmptyConstructor
    public InverseBooleanConverter() { } //to avoid an annoying XAML warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return !(bool)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter,
        CultureInfo culture)
    {
      return !(bool)value;
      //throw new NotSupportedException();
    }
  }

  public class BooleanToVisibilityConverter : MarkupExtensionConverter, IValueConverter
  {
    enum Direction
    {
// ReSharper disable UnusedMember.Local - Inverse is used in XAML
      Normal, Inverse
// ReSharper restore UnusedMember.Local
    }

// ReSharper disable EmptyConstructor
    public BooleanToVisibilityConverter() { } //to avoid an XAML annoying warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value == null || value == DBNull.Value) return (Visibility.Visible);

      var b = bool.Parse(value.ToString());
      var d = (parameter == null) ? Direction.Normal : (Direction)Enum.Parse(typeof(Direction), (string)parameter);

      return (((d == Direction.Normal) ? b : !b) ? Visibility.Visible : Visibility.Collapsed);
    }

    public object ConvertBack(object value, Type targetType,
        object parameter, CultureInfo culture)
    {
      return null;
    }
  }


  /// <summary>
  /// This converter's Parameter should specifiy a Path string which refers to a string Property on current DataContext
  /// That string Property should return another Path string which corresponds to the desired Property which will be pulled and returned
  /// This converter allows us to do a "double hop", otherwise unavailable in basic Binding syntax
  /// Don't use it for something that should change often, looping and reflection obviously aren't performant
  /// </summary>
  public class IndirectPropertyConverter : MarkupExtensionConverter, IValueConverter
  {
// ReSharper disable EmptyConstructor
    public IndirectPropertyConverter() { } //to avoid an XAML annoying warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value == null) return (null);

      var firstpass = true;
      var propertyPathPieces = new[] { parameter.ToString() };
      object[] indexer = null;
      var i = 0;
      while (i < propertyPathPieces.Length)
      {
        var propertyPathPiece = propertyPathPieces[i];
        var pi = value.GetType().GetProperty(propertyPathPiece); //first look for a normal propertyName
        if (pi == null)
        {
          pi = value.GetType().GetProperty("Item", new[] { typeof(string) }); //otherwise, look for a string indexer
          indexer = new object[] { propertyPathPiece };
        }
        var obj = pi.GetValue(value, indexer);
        indexer = null;

        if (firstpass) {
          propertyPathPieces = obj.ToString().Split('.');
          firstpass = false;
        }
        else
        {
          value = obj;
          i++;
        }
      }

      return (value.ToString());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  /// <summary>
  /// Parameter should be comma delimited string of Brush color strings for True, False (e.g. Gray, Black)
  /// </summary>
  public class BoolToSolidBrushConverter : MarkupExtensionConverter, IValueConverter
  {
// ReSharper disable EmptyConstructor
    public BoolToSolidBrushConverter() { } //to avoid an XAML annoying warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var colors = parameter.ToString().Split(',');
      return new System.Windows.Media.BrushConverter().ConvertFromString((bool)value ? colors[0].Trim() : colors[1].Trim());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class IntToSolidBrushConverter : MarkupExtensionConverter, IValueConverter
  {
// ReSharper disable EmptyConstructor
    public IntToSolidBrushConverter() { } //to avoid an XAML annoying warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return new System.Windows.Media.BrushConverter().ConvertFromString(parameter.ToString().Split(',')[(int)value]);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class BoolToParmStringConverter : MarkupExtensionConverter, IValueConverter
  {
// ReSharper disable EmptyConstructor
    public BoolToParmStringConverter() { } //to avoid an XAML annoying warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var strings = parameter.ToString().Split('|');
      return((bool)value ? strings[0] : strings[1]);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class BoolExpressionToVisibility : MarkupExtensionConverter, IValueConverter
  {
// ReSharper disable EmptyConstructor
    public BoolExpressionToVisibility() { } //to avoid an XAML annoying warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value == null || value == DBNull.Value) return (Visibility.Visible);
      return (StringEvaluator.EvalToBool(parameter.ToString().Replace("?", value.ToString())) ? Visibility.Visible : Visibility.Collapsed);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public delegate void VariableReplacementCallback(ref string expression);

  public class BoolExpressionToBool : MarkupExtensionConverter, IValueConverter //nugget: leverage the JScript.dll StringEvaluator to build dynamic ValueConverters
  {
    static public VariableReplacementCallback VariableReplacement;

// ReSharper disable EmptyConstructor
    public BoolExpressionToBool() { } //to avoid an XAML annoying warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var expression = parameter.ToString().Replace("?", (value??"").ToString());
      if (VariableReplacement != null) VariableReplacement(ref expression);
      return (StringEvaluator.EvalToBool(expression.ToLower())); //nugget:
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  
  public class StringsEqualToBool : MarkupExtensionConverter, IValueConverter
  {
// ReSharper disable EmptyConstructor
    public StringsEqualToBool() { } //to avoid an XAML annoying warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    static private long _lasttime = DateTime.Now.Ticks; 
    static private string _lastvalue;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return ((value ?? "").ToString() == (parameter ?? "").ToString());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      //total hack alert - will have to see if ticks winds up being the same over multiple slower machines
      //the basic issue is that this converter is bound to radio buttons (SettingsPage.xaml) and they fire in succession...
      //first the new selection fires and sets the bound property, then the old unselection fires and writes an unwanted blank into the property (bummer)

      if (DateTime.Now.Ticks == _lasttime) return (_lastvalue); 
      _lasttime = DateTime.Now.Ticks;
      _lastvalue = (bool)value ? parameter.ToString() : "";
      return (_lastvalue);
    }
  }

  public class StringsEqualToBoolMulti : MarkupExtensionConverter, IMultiValueConverter
  {
// ReSharper disable EmptyConstructor
    public StringsEqualToBoolMulti() { } //to avoid an XAML annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      return (values[0].ToString() == values[1].ToString());
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class RadioButtonGroupIsCheckedConverter : MarkupExtensionConverter, IValueConverter
  {
// ReSharper disable EmptyConstructor
    public RadioButtonGroupIsCheckedConverter() { } //to avoid an XAML annoying warning: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    static private long _lasttime = DateTime.Now.Ticks;
    static private object _lastvalue;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return (value == parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      //total hack alert - will have to see if ticks winds up being the same over multiple slower machines
      //the basic issue is that this converter is bound to radio buttons and they fire in succession...
      //first the new selection fires and sets the bound property, then the old "un"selection fires and clobbers what we want
      //so this logic is an attempt at "keep the first one" in a static context where there's no state to really know the difference between first and second

      if (DateTime.Now.Ticks == _lasttime) return (_lastvalue);
      _lasttime = DateTime.Now.Ticks;
      _lastvalue = (bool)value ? parameter : null;
      return (_lastvalue);
    }
  }

  public class True: MarkupExtensionConverter, IValueConverter
  {
// ReSharper disable EmptyConstructor
    public True() { } //to avoid an XAML annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return (true);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }



  public class AND : MarkupExtensionConverter, IMultiValueConverter
  {
// ReSharper disable EmptyConstructor
    public AND() { } //to avoid an XAML annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      var result = values.Aggregate(true, (current, value) => current && (!(value == DependencyProperty.UnsetValue || value == null) && (bool) value));

      return (result);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class ANDVis : MarkupExtensionConverter, IMultiValueConverter
  {
// ReSharper disable EmptyConstructor
    public ANDVis() { } //to avoid an XAML annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      var result = values.Aggregate(true, (current, value) => current && (!(value == DependencyProperty.UnsetValue || value == null) && (bool) value));

      return (result ? Visibility.Visible : Visibility.Collapsed);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class OR : MarkupExtensionConverter, IMultiValueConverter
  {
// ReSharper disable EmptyConstructor
    public OR() { } //to avoid an XAML annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      var result = values.Aggregate(false, (current, value) => current || (!(value == DependencyProperty.UnsetValue || value == null) && (bool) value));

      return (result);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }


  public class ORVis : MarkupExtensionConverter, IMultiValueConverter
  {
// ReSharper disable EmptyConstructor
    public ORVis() { } //to avoid an XAML annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      var result = values.Aggregate(false, (current, value) => current || (!(value == DependencyProperty.UnsetValue || value == null) && (bool) value));

      return (result ? Visibility.Visible : Visibility.Collapsed);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }



  public class DebugConverter : MarkupExtensionConverter, IValueConverter
  {
// ReSharper disable EmptyConstructor
    public DebugConverter() { } //to avoid an XAML annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      //System.Collections.ObjectModel.ReadOnlyObservableCollection<System.Windows.Controls.ValidationError> errors = value as System.Collections.ObjectModel.ReadOnlyObservableCollection<System.Windows.Controls.ValidationError>;
      //if (errors.Count > 0) return (errors[0].ToString());
      //else return (null);
      return (value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class IndirectMultiValue : MarkupExtensionConverter, IMultiValueConverter
  {
// ReSharper disable EmptyConstructor
    public IndirectMultiValue() { } //to avoid an XAML annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      var obj = values[0]; 
      var propertyName = values[1].ToString(); 
      object[] indexer = null;

      var pi = obj.GetType().GetProperty(propertyName); //first look for a normal propertyName

      if (pi == null)
      {
        pi = obj.GetType().GetProperty("Item", new[] { typeof(string) }); //otherwise, look for a string indexer
        if (pi == null) return (null);
        indexer = new object[] { propertyName };
      }

      return (pi.GetValue(obj, indexer));

    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }

  }

  public class NotEmptyToTrue : MarkupExtensionConverter, IValueConverter
  {
// ReSharper disable EmptyConstructor
    public NotEmptyToTrue() { } //to avoid an annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return (!EmptyToTrue.ReusableConverter(value, targetType, parameter, culture));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }

  }

  public class EmptyToTrue : MarkupExtensionConverter, IValueConverter
  {
// ReSharper disable EmptyConstructor
    public EmptyToTrue() { } //to avoid an annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return (ReusableConverter(value, targetType, parameter, culture));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }

    static public bool ReusableConverter(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return(value == null
        || (value is Guid && (Guid)value == Guid.Empty)
        || (value is IList && ((IList)value).Count == 0)
        || String.IsNullOrWhiteSpace(value.ToString()));
    }
  }

  public class EmptyToVisible : MarkupExtensionConverter, IValueConverter
  {
// ReSharper disable EmptyConstructor
    public EmptyToVisible() { } //to avoid an annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return (EmptyToTrue.ReusableConverter(value, targetType, parameter, culture)
        ? Visibility.Visible : Visibility.Collapsed);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class NotEmptyToVisible : MarkupExtensionConverter, IValueConverter
  {
// ReSharper disable EmptyConstructor
    public NotEmptyToVisible() { } //to avoid an annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return (!EmptyToTrue.ReusableConverter(value, targetType, parameter, culture)
        ? Visibility.Visible : Visibility.Collapsed);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class EmptyToParmStringConverter : MarkupExtensionConverter, IValueConverter
  {
// ReSharper disable EmptyConstructor
    public EmptyToParmStringConverter() { } //to avoid an annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      string[] strings = parameter.ToString().Split('|');
      return (EmptyToTrue.ReusableConverter(value, targetType, parameter, culture)
        ? strings[0] : strings[1]);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

  public class StringEvaluatorConverter : MarkupExtensionConverter, IValueConverter
  {
// ReSharper disable EmptyConstructor
    public StringEvaluatorConverter() { } //to avoid an annoying warning from XAML designer: "No constructor for type 'xyz' has 0 parameters."  Somehow the inherited one doesn't do the trick!?!  I guess it's a reflection buggy.
// ReSharper restore EmptyConstructor

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return (StringEvaluator.EvalToString(parameter.ToString().Replace("?", value.ToString()))); 
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

}