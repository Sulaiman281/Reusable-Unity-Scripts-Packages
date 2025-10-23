using SQLite;

namespace WitShells.Sqlite
{
    public static class SqliteUtils
    {
        public static bool DatabaseExists(string dbPath)
        {
            return System.IO.File.Exists(dbPath);
        }

        public static void DeleteDatabase(string dbPath)
        {
            if (DatabaseExists(dbPath))
            {
                System.IO.File.Delete(dbPath);
            }
        }

        public static SQLiteConnection CreateConnection(string dbPath)
        {
            return new SQLiteConnection(dbPath);
        }

        public static void CreateTable<T>(SQLiteConnection connection)
        {
            connection.CreateTable<T>();
        }

        public static void DropTable<T>(SQLiteConnection connection)
        {
            connection.DropTable<T>();
        }
    }
}