using System;
using System.Reflection;

public class ReflectionConverter
{
    public static object ChangeType(string value, Type conversionType)
    {
        // Handle Nullable types (e.g., int?)
        if (conversionType.IsGenericType && conversionType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            conversionType = Nullable.GetUnderlyingType(conversionType);
        }

        // Use Convert.ChangeType for the core conversion
        return Convert.ChangeType(value, conversionType);
    }
}
