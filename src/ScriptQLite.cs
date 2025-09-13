using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Tomlyn;

internal class ScriptQLite : Script {
	internal override string Name => "ScriptQLite";

	internal override Result RunScript( string file, SqliteTransaction transaction, params (string key, object value)[] parameters ) {
		var script = File.ReadAllText( file );
		var regex = new Regex( @"(.*?)(?<!\\)(\[GUID\])(.*)" );
		while ( regex.IsMatch( script ) ) {
			script = regex.Replace( 
				script,
				m => m.Groups[1].Value + Guid.NewGuid().ToString( "N" ) + m.Groups[3].Value,
				1
			);
		}
		try {
			Program.Database.Execute( script, transaction, parameters );

			return new Result() {
				Code = 0,
				Message = $"{file}: OK"
			};
		}
		catch( Exception e ) {
			return new Result() {
				Code = -1,
				Message = $"{file}: Error: {e.Message}",
			};
		}
	}

	internal override Result<DataTable> RunScript( string file, params (string key, object value)[] parameters ) {
		var script = File.ReadAllText( file );
		var regex = new Regex( @"(.*?)(?<!\\)(\[GUID\])(.*)" );
		while ( regex.IsMatch( script ) ) {
			script = regex.Replace( 
				script,
				m => m.Groups[1].Value + Guid.NewGuid().ToString( "N" ) + m.Groups[3].Value,
				1
			);
		}
		try {
			var result = Program.Database.Select( script, parameters );
			return new Result<DataTable>() {
				Code = 0,
				Message = $"{file}: OK",
				Data = result
			};
		}
		catch( Exception e ) {
			return new Result<DataTable>() {
				Code = -1,
				Message = $"{file}: Error: {e.Message}",
				Data = null
			};
		}
	}
}
