using Npgsql;

namespace MyMediaList.Server
{
    public static class Database
    {
        private static readonly string ConnectionString =
            "Host=localhost;Port=5432;Database=mymedialist;Username=user;Password=password";

        public static NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(ConnectionString);
        }
    }
}