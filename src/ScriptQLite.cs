using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Tomlyn;

internal class ScriptQLite : Script {
	internal override string Name => "ScriptQLite";

	internal override Result RunScript( string file, SqliteTransaction transaction, Dictionary<string, string>? placeholder = null, params (string key, object value)[] parameters ) {
		var script = File.ReadAllText( file );
		if ( placeholder != null ) {
			foreach( var pair in placeholder ) {
			var pholderregex = new Regex( @"(.*?)(?<!\\)(\[\{" + pair.Key + @"\}\])(.*)" );
				while( pholderregex.IsMatch( script ) ) {
					script = pholderregex.Replace(
						script,
						m => m.Groups[1].Value + placeholder[m.Groups[3].Value] + m.Groups[5].Value,
						1
					);
				}
			}
		}
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

	internal override Result<DataTable> RunScript( string file, Dictionary<string, string>? placeholder, params (string key, object value)[] parameters ) {
		var script = File.ReadAllText( file );
		if ( placeholder != null ) {
			foreach( var pair in placeholder ) {
			var pholderregex = new Regex( @"(.*?)(?<!\\)(\[\{" + pair.Key + @"\}\])(.*)" );
				while( pholderregex.IsMatch( script ) ) {
					script = pholderregex.Replace(
						script,
						m => m.Groups[1].Value + placeholder[m.Groups[3].Value] + m.Groups[5].Value,
						1
					);
				}
			}
		}
		var paramregex = new Regex( @"(.*?)(?<!\\)(\[GUID\])(.*)" );
		while ( paramregex.IsMatch( script ) ) {
			script = paramregex.Replace( 
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
