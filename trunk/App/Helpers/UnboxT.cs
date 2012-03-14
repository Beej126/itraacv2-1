using System;
using System.Globalization;
using System.Reflection;

// ReSharper disable CheckNamespace
internal static class UnboxT<T>
// ReSharper restore CheckNamespace
{
  internal static readonly Converter<object, T> Unbox = Create(typeof(T));

  private static Converter<object, T> Create(Type type)
  {
    if (!type.IsValueType)
    {
      return ReferenceField;
    }
    if ((type.IsGenericType && !type.IsGenericTypeDefinition) && (typeof(Nullable<>) == type.GetGenericTypeDefinition()))
    {
      MethodInfo nullableFieldMethod = typeof(UnboxT<T>).GetMethod("NullableField", BindingFlags.NonPublic | BindingFlags.Static);
      MethodInfo genericMethod = nullableFieldMethod.MakeGenericMethod(new[] { type.GetGenericArguments()[0] });

      return (Converter<object, T>)Delegate.CreateDelegate(typeof(Converter<object, T>), genericMethod);
    }
    return ValueField;
  }

// ReSharper disable UnusedMember.Local
  private static TElem? NullableField<TElem>(object value) where TElem : struct
// ReSharper restore UnusedMember.Local
  {
    if (DBNull.Value == value)
    {
      return null;
    }
    return (TElem?)value;
  }

  private static T ReferenceField(object value)
  {
    if (DBNull.Value != value)
    {
      return (T)value;
    }
    return default(T);
  }

  /// <exception cref="InvalidCastException"><c>InvalidCastException</c>.</exception>
  private static T ValueField(object value)
  {
    if (DBNull.Value == value)
    {
      throw new InvalidCastException(
          string.Format(CultureInfo.CurrentCulture, "Type: {0} cannot be casted to Nullable type", typeof(T)));
    }
    return (T)value;
  }
}
