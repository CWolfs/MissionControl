using System;
using System.Reflection;

namespace SpawnVariation.Utils {
    public static class ReflectionHelper {
        public static object InvokePrivateMethod(object instance, string methodname, object[] parameters) {
            Type type = instance.GetType();
            MethodInfo methodInfo = type.GetMethod(methodname, BindingFlags.NonPublic | BindingFlags.Instance);
            return methodInfo.Invoke(instance, parameters);
        }

        public static object InvokePrivateMethod(object instance, string methodname, object[] parameters, Type[] types) {
            Type type = instance.GetType();
            MethodInfo methodInfo = type.GetMethod(methodname, BindingFlags.NonPublic | BindingFlags.Instance, null, types, null);
            return methodInfo.Invoke(instance, parameters);
        }

        public static void SetPrivateProperty(object instance, string propertyName, object value) {
            Type type = instance.GetType();
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance);
            property.SetValue(instance, value, null);
        }

        public static void SetReadOnlyProperty(object instance, string propertyName, object value) {
            Type type = instance.GetType();
            PropertyInfo property = type.GetProperty(propertyName);
            property.DeclaringType.GetProperty(propertyName);
            property.SetValue(instance, value, BindingFlags.NonPublic | BindingFlags.Instance, null, null, null);
        }

        public static void SetPrivateField(object instance, string fieldname, object value) {
            Type type = instance.GetType();
            FieldInfo field = type.GetField(fieldname, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(instance, value);
        }

        public static object GetPrivateField(object instance, string fieldname) {
            Type type = instance.GetType();
            FieldInfo field = type.GetField(fieldname, BindingFlags.NonPublic | BindingFlags.Instance);
            return field.GetValue(instance);
        }

        public static object GetPrivateStaticField(Type instance, string fieldname) {
            FieldInfo field = instance.GetField(fieldname, BindingFlags.NonPublic | BindingFlags.Static);
            return field.GetValue(instance);
        }
    }
}