using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Tomlyn;

internal class ScriptQLite : Script {
	internal override string Name => "ScriptQLite";

	internal override bool RunScript( string file, SqliteTransaction transaction ) {
		Console.WriteLine( $"Running script {file}" );
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
			Program.Database.Execute( script, transaction );
		}
		catch( Exception e ) {
			Console.WriteLine( $"Script failed: {e.Message}\nCommand: {script}" );
			return false;
		}
		return true;
	}
}
