using System;
using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;

class Database
{
    public Database()
    {
        if (!Directory.Exists("db"))
            Directory.CreateDirectory("db");
        Connection = new SqliteConnection("Data Source=db/std.db");
        Connection.Open();
    }

    ~Database()
    {
        Connection.Close();
    }

    public int Execute(string command)
    {
        var cmd = Connection.CreateCommand();
        cmd.CommandText = command;

        return cmd.ExecuteNonQuery();
    }

    public DataTable Select(string command)
    {
        DataTable result = new();

        var cmd = Connection.CreateCommand();
        cmd.CommandText = command;

        using (var res = cmd.ExecuteReader())
        {
            result.Load(res);
        }

        return result;
    }

    public SqliteConnection Connection { get; private set; }
}