using System.IO;
using System.Text;
using Tomlyn;
using Tomlyn.Model;

internal class TomlConfigProvider: IConfigProvider {
	public Result<ConfigTree> Load( string file ) {
		try {
			var content = File.ReadAllText( file );
			var doc = Toml.Parse( content );

			if ( doc.HasErrors ) {
				return new Result<ConfigTree>() {
					Code = -1,
					Message = string.Join( " - ", doc.Diagnostics ),
					Data = null
				};
			}

			var root = doc.ToModel();
			var tree = new ConfigTree() {
				Configuration = Path.GetFileName( file )
			};

			foreach( var token in root ) {
				if ( token.Key.StartsWith( "%" ) ) {
					continue;
				}

				root.TryGetValue( $"%{token.Key}", out var metaobj );
				TomlTable? met = metaobj as TomlTable;
				var child = BuildNode( token.Key, token.Value, met );
				tree.Items.Add( child );
			}

			return new Result<ConfigTree>() {
				Code = 0,
				Message = "OK",
				Data = tree
			};
		}
		catch ( Exception e ) {
			return new Result<ConfigTree>() {
				Code = -1,
				Message = e.Message,
				Data = null
			};
		}
	}

	public Result Save( string file, ConfigTree data ) {
		try {
			var root = new TomlTable();
			foreach( var item in data.Items ) {
				var element = BuildElement( item );
				root[item.Name] = element.Token;

				if ( element.Meta != null ) {
					root[$"%{item.Name}"] = element.Meta;
				}
			}

			var content = Toml.FromModel( root );
			File.WriteAllText( file, content, Encoding.UTF8 );

			return new Result() {
				Code = 0,
				Message = "OK"
			};
		}
		catch( Exception e ) {
			return new Result() {
				Code = -1,
				Message = e.Message
			};
		}
	}

	internal ConfigNode BuildNode( string name, object? value, TomlTable? meta ) {
		var node = new ConfigNode() {
			Name = name,
			Type = "category",
			Value = ""
		};

		if ( meta != null ) {
			foreach( var entry in meta.Keys ) {
				node.Meta[entry] = (string)meta[entry];
			}
		}

		switch( value ) {
		case null:
			node.Type = "null";
			node.Value = "";
			break;
		case TomlTable obj:
			foreach( var token in obj ) {
				if ( token.Key.StartsWith( "%" ) ) {
					continue;
				}

				obj.TryGetValue( $"%{token.Key}", out var metaobj );
				TomlTable? met = metaobj as TomlTable;
				var child = BuildNode( token.Key, token.Value, met );
				node.Children.Add( child );
			}
			break;
		case TomlTableArray tarr:
			node.Type = "list";
			foreach( var token in tarr ) {
				var child = BuildNode( "li", token, null );
				node.Children.Add( child );
			}
			break;
		case TomlArray arr:
			node.Type = "list";
			foreach( var token in arr ) {
				var child = BuildNode( "li", token, null );
				node.Children.Add( child );
			}
			break;
		case bool b:
			node.Type = "bool";
			node.Value = b ? "true" : "false";
			break;
		case sbyte or byte or short or ushort or int or uint or long:
			node.Type = "integer";
			node.Value = value.ToString();
			break;
		case ulong:
			node.Type = "unsigned";
			node.Value = value.ToString();
			break;
		case float or double or decimal:
			node.Type = "float";
			node.Value = value.ToString();
			break;
		case DateTime dt:
			node.Type = "datetime";
			node.Value = dt.ToUniversalTime().ToString( "O" );
			break;
		case DateTimeOffset dto:
			node.Type = "datetime";
			node.Value = dto.ToUniversalTime().ToString( "O" );
			break;
		default:
			node.Type = "string";
			node.Value = value.ToString();
			break;
		}

		return node;
	}

	internal (object Token, TomlTable? Meta) BuildElement( ConfigNode node ) {
		(object Token, TomlTable? Meta) result = new();

		if ( node.Meta.Count > 0 ) {
			result.Meta = new TomlTable();
			foreach( var meta in node.Meta ) {
				result.Meta[meta.Key] = meta.Value;
			}
		}

		switch( node.Type ) {
		case "category":
			var obj = new TomlTable();
			foreach( var child in node.Children ) {
				var element = BuildElement( child );
				obj[child.Name] = element.Token;
				if ( element.Meta != null ) {
					obj[$"%{child.Name}"] = element.Meta;
				}
			}
			result.Token = obj;
			break;
		case "list":
			var arr = new TomlArray();
			foreach( var child in node.Children ) {
				var element = BuildElement( child );
				arr.Add( element.Token );
			}
			result.Token = arr;
			break;
		case "null":
			result.Token = null;
			break;
		case "bool":
			result.Token = bool.Parse( node.Value );
			break;
		case "integer":
			result.Token = long.Parse( node.Value );
			break;
		case "float":
			result.Token = double.Parse( node.Value );
			break;
		case "datetime":
			result.Token = DateTimeOffset.Parse( node.Value );
			break;
		default:
			result.Token = node.Value;
			break;
		}

		return result;
	}
}
