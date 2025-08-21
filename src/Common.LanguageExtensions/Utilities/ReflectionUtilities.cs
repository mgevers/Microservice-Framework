namespace Common.LanguageExtensions.Utilities
{
    public static class ReflectionUtilities
    {
        public static void ExecuteFunction(this object obj, string funcName, object[]? parameters = null)
        {
            var method = obj.GetType().GetMethod(funcName);
            method!.Invoke(obj, parameters);
        }

        public static void ExecuteFunction(this object obj, string funcName, object parameter)
        {
            var method = obj.GetType().GetMethod(funcName);
            method!.Invoke(obj, new[] { parameter });
        }

        public static T ExecuteFunction<T>(this object obj, string funcName, object[]? parameters = null)
        {
            var method = obj.GetType().GetMethod(funcName);
            var ret = method!.Invoke(obj, parameters)!;

            return (T)ret;
        }

        public static T ExecuteFunction<T>(this object obj, string funcName, object parameter)
        {
            var method = obj.GetType().GetMethod(funcName);
            var ret = method!.Invoke(obj, new[] { parameter })!;

            return (T)ret;
        }
    }
}
