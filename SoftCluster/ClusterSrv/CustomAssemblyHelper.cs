using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace ClusterSrv
{
    public class CustomAssemblyHelper
    {
        public static object GetInstanceOf(string path, string typeDescr)
        {
            if (File.Exists(path))
            {
                Assembly dll = Assembly.LoadFrom(path);
                var type = dll.GetType(typeDescr);
                if (type != null)
                {
                    return Activator.CreateInstance(type);
                }
            }
            return null;
        }
        public static object GetPropertyValue(object o, string propertyName)
        {
            // Get the property we want to access
            PropertyInfo property = o.GetType().GetProperty(propertyName);

            // Retrieve the value of that property in the specified object o
            return property.GetValue(o);
        }
        public static void SetPropertyValue(object o, string propertyName, object value)
        {
            // Get the property we want to access
            PropertyInfo property = o.GetType().GetProperty(propertyName);

            // Set the value of the property for the specified object o
            property.SetValue(o, value);
        }
        public static object InvokeMethod(object o, string methodName, object[] arguments)
        {
            // First lets generate a Type[] for the parameters.  To do this we use some
            // LINQ to select the type of each element in the arguments array
            Type[] types = arguments.Select(x => x.GetType()).ToArray();

            // Get the MethodInfo for the method for the object specified
            MethodInfo method = o.GetType().GetMethod(methodName, types);

            // Invoke the method on the object we passed and return the result.
            return method.Invoke(o, arguments);
        }
        public static object GetEnumValue(Type enumType, string enumItemName)
        {
            // Get the underlying type used for each enum item
            Type enumUnderlyingType = enumType.GetEnumUnderlyingType();

            // Get a list of all the names in the enums
            List<string> enumNames = enumType.GetEnumNames().ToList();
            // Get an array of all the corresponding values in the enum
            Array enumValues = enumType.GetEnumValues();

            // Get the value where the corresponding name matches our specified name
            object enumValue = enumValues.GetValue(enumNames.IndexOf(enumItemName));

            // Convert the value to the underlying enum type and return it
            return Convert.ChangeType(enumValue, enumUnderlyingType);
        }
    }
}
