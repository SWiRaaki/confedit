using System;
using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;

class Database {
	public Database() {
		if ( !Directory.Exists( "db" ) )
			Directory.CreateDirectory( "db" );
		Connection = new SqliteConnection( "Data Source=db/std.db" );
		Connection.Open();
	}

	~Database() {
		Connection.Close();
	}

	public SqliteTransaction BeginTransaction() {
		return Connection.BeginTransaction();
	}

	public int Execute( string command ) {
		var cmd = Connection.CreateCommand();
		cmd.CommandText = command;

		return cmd.ExecuteNonQuery();
	}

	public int Execute( string command, SqliteTransaction transaction ) {
		var cmd = Connection.CreateCommand();
		cmd.CommandText = command;
		cmd.Transaction = transaction;

		return cmd.ExecuteNonQuery();
	}

	public DataTable Select( string command ) {
		DataTable result = new();

		var cmd = Connection.CreateCommand();
		cmd.CommandText = command;

		using ( var res = cmd.ExecuteReader() ) {
			result.Load( res );
		}

		return result;
	}

	public DataTable Select( string command, params (string key, object value)[] parameters ) {
		DataTable result = new();

		var cmd = Connection.CreateCommand();
		cmd.CommandText = command;

		foreach( var parameter in parameters ) {
			cmd.Parameters.AddWithValue( parameter.key, parameter.value );
		}

		using ( var res = cmd.ExecuteReader() ) {
			result.Load( res );
		}

		return result;
	}

	public SqliteConnection Connection { get; private set; }
}
