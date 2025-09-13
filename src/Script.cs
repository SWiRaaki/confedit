using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;
using Tomlyn;

public class MetaRecord {
	public ulong Major { get; set; } = 1;
	public ulong Minor { get; set; } = 0;
	public ulong Patch { get; set; } = 0;
	public string Script { get; set; } = "";
}

public class RequiredRecord {
	public ulong Major { get; set; } = 1;
	public ulong Minor { get; set; } = 0;
	public ulong Patch { get; set; } = 0;
}

public class ScriptsRecord {
	public string Directory { get; set; } = "sql/";
	public List<string> Create { get; set; } = new();
	public List<string> Alter { get; set; } = new();
	public List<string> Insert { get; set; } = new();
	public List<string> Remove { get; set; } = new ();
}

public class PatchData {
	public string File { get; set; } = "";
	public MetaRecord Meta { get; set; } = new();
	public RequiredRecord? Required { get; set; }
	public ScriptsRecord Scripts { get; set; } = new();
}

internal abstract class Script {
	internal abstract string Name { get; }

	internal static bool CheckForPatches() {
		Console.WriteLine( $"Searching for database patch files" );
		var files = new List<string>( Directory.GetFiles( "sql", "*.patch" ) );
		if ( files.Count == 0 ) {
			Console.WriteLine( "No patch found, database up to date!" );
			return true;
		}

		for ( int i = 0; i < files.Count; ++i ) {
			if ( !RunPatch( files[i] ) ) {
				Console.WriteLine( "Patch failed, contact the admin for more help" );
				return false;
			}
		}

		return true;
	}

	internal static bool RunPatch( string patchfile ) {
		Console.WriteLine( $"Running patch {patchfile!} .." );
		if ( !File.Exists( patchfile ) ) {
			Console.WriteLine( $"Failed to read patch {patchfile}: File not found" );
			return false;
		}

		var options = new TomlModelOptions {
			ConvertPropertyName = name => name switch {
				"Directory" => "DIR",
				_ => name.ToUpperInvariant()
			}
		};
		var content = File.ReadAllText( patchfile );
		var patch = Toml.ToModel<PatchData>( content, patchfile, options );
		patch.File = patchfile;

		if ( IsPatchInstalled( patch ) ) {
			Console.WriteLine( $"Patch {patch.Meta.Major}.{patch.Meta.Minor}.{patch.Meta.Patch} already installed" );
			return true;
		}

		if ( patch.Required != null ) {
			Console.WriteLine( $"Found Requirement patch: {patch.Required.Major}.{patch.Required.Minor}.{patch.Required.Patch}" );
			if ( !RunPatch( $"v{patch.Required.Major}_{patch.Required.Minor}_{patch.Required.Patch}.patch" ) ) {
				Console.WriteLine( $"Installing required patch {patch.Required.Major}.{patch.Required.Minor}.{patch.Required.Patch} failed" );
				return false;
			}
		}

		Script script = Program.Scripter[patch.Meta.Script];
		if ( script == null ) {
			Console.WriteLine( $"Script engine {patch.Meta.Script} is not installed! Please update binaries to run patch!" );
			return false;
		}

		var transaction = Program.Database.BeginTransaction();
		try {
			for ( int i = 0; i < patch.Scripts.Create.Count; ++i ) {
				if ( !script.RunScript( $"{patch.Scripts.Directory}{patch.Scripts.Create[i]}", transaction ) ) {
					throw new Exception( $"{patch.Scripts.Create[i]} was not applicable" );
				}
			}
			for ( int i = 0; i < patch.Scripts.Alter.Count; ++i ) {
				if ( !script.RunScript( $"{patch.Scripts.Directory}{patch.Scripts.Alter[i]}", transaction ) ) {
					throw new Exception( $"{patch.Scripts.Alter[i]} was not applicable" );
				}
			}
			for ( int i = 0; i < patch.Scripts.Insert.Count; ++i ) {
				if ( !script.RunScript( $"{patch.Scripts.Directory}{patch.Scripts.Insert[i]}", transaction ) ) {
					throw new Exception( $"{patch.Scripts.Insert[i]} was not applicable" );
				}
			}
			for ( int i = 0; i < patch.Scripts.Remove.Count; ++i ) {
				if ( !script.RunScript( $"{patch.Scripts.Directory}{patch.Scripts.Remove[i]}", transaction ) ) {
					throw new Exception( $"{patch.Scripts.Remove[i]} was not applicable" );
				}
			}
		}
		catch ( Exception e ) {
			Console.WriteLine( $"Failed to install patch {patch.Meta.Major}.{patch.Meta.Minor}.{patch.Meta.Patch}: {e.Message}" );
			MovePatchFiles( patch, "failed" );
			transaction.Rollback();
			return false;
		}

		MovePatchFiles( patch, "ok" );
		transaction.Commit();
		Program.Database.Execute( $"insert into std_dbver(major, minor, patch, script_version) values({patch.Meta.Major},{patch.Meta.Minor},{patch.Meta.Patch},'ScriptQLite')" );
		return true;
	}

	internal static bool IsPatchInstalled( PatchData patch ) {
		try {
			var result = Program.Database.Select( $"select * from std_dbver where major={patch.Meta.Major} and minor={patch.Meta.Minor} and patch={patch.Meta.Patch}" );
			return result.Rows.Count == 1;
		}
		catch {
			return false;
		}
	}

	internal static void MovePatchFiles( PatchData patch, string destination ) {
		Directory.CreateDirectory( $"{patch.Scripts.Directory}{destination}" );
		if ( File.Exists( patch.File ) ) {
			File.Move( patch.File, $"{patch.Scripts.Directory}{destination}/{Path.GetFileName( patch.File )}" );
		}
		for ( int i = 0; i < patch.Scripts.Create.Count; ++i ) {
			var file = $"{patch.Scripts.Directory}{patch.Scripts.Create[i]}";
			if ( File.Exists( file ) ) {
				File.Move( file, $"{patch.Scripts.Directory}{destination}/{patch.Scripts.Create[i]}" );
			}
		}
		for ( int i = 0; i < patch.Scripts.Alter.Count; ++i ) {
			var file = $"{patch.Scripts.Directory}{patch.Scripts.Alter[i]}";
			if ( File.Exists( file ) ) {
				File.Move( file, $"{patch.Scripts.Directory}{destination}/{patch.Scripts.Alter[i]}" );
			}
		}
		for ( int i = 0; i < patch.Scripts.Insert.Count; ++i ) {
			var file = $"{patch.Scripts.Directory}{patch.Scripts.Insert[i]}";
			if ( File.Exists( file ) ) {
				File.Move( file, $"{patch.Scripts.Directory}{destination}/{patch.Scripts.Insert[i]}" );
			}
		}
		for ( int i = 0; i < patch.Scripts.Remove.Count; ++i ) {
			var file = $"{patch.Scripts.Directory}{patch.Scripts.Remove[i]}";
			if ( File.Exists( file ) ) {
				File.Move( file, $"{patch.Scripts.Directory}{destination}/{patch.Scripts.Remove[i]}" );
			}
		}
	}

	internal abstract Result RunScript( string file, SqliteTransaction transaction, Dictionary<string, string>? placeholder = null, params (string key, object value)[] parameters );

	internal abstract Result<DataTable> RunScript( string file, Dictionary<string, string>? placeholder, params (string key, object value)[] parameters );
}
