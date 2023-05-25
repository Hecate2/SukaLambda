using SQLite;

namespace sukalambda
{
    public static partial class Utils
    {
        public static string? GetTableName(Type type)
        {
            var att = type.GetCustomAttributes(typeof(TableAttribute), true).FirstOrDefault();
            if (att != null)
                return ((TableAttribute)att).Name;
            return null;
        }
    }
}
