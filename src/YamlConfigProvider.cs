using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

internal class YamlConfigProvider : IConfigProvider {
	public Result<ConfigTree> Load( string file ) {
		try {
			using var stream = File.OpenText( file );
			var yaml = new YamlStream();
			yaml.Load(stream);

			var tree = new ConfigTree() {
				Configuration = Path.GetFileName( file )
			};

			foreach ( var doc in yaml.Documents ) {
				var root = doc.RootNode;

				if ( root is not YamlMappingNode map ) {
					continue;
				}

				foreach ( var entry in map.Children ) {
					if ( entry.Key is not YamlScalarNode keynode ) {
						continue;
					}

					var name = keynode.Value!;
					if ( name.StartsWith( '%' ) ) {
						continue;
					}

					var value = entry.Value;
					map.Children.TryGetValue( new YamlScalarNode( $"%{name}" ), out var metaobj );
					var meta = metaobj as YamlMappingNode;
					var child = BuildNode( name, value, meta );
					tree.Items.Add( child );
				}
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
			var root = new YamlMappingNode();

			foreach( var item in data.Items ) {
				var element = BuildElement( item );
				root.Add( new YamlScalarNode( item.Name ), element.Token );
				if ( element.Meta != null ) {
					root.Add( new YamlScalarNode( $"%{item.Name}" ), element.Meta );
				}
			}

			var doc = new YamlDocument( root );
			var stream = new YamlStream( doc );

			using var writer = new StreamWriter( file, false, Encoding.UTF8 );
			stream.Save( writer, assignAnchors: false );

			return new Result() {
				Code = 0,
				Message = "OK"
			};
		}
		catch ( Exception e ) {
			return new Result() {
				Code = -1,
				Message = e.Message
			};
		}
	}

	internal ConfigNode BuildNode( string name, YamlNode value, YamlMappingNode? meta ) {
		var node = new ConfigNode() {
			Name = name,
			Type = "category",
			Value = ""
		};

		if ( meta != null ) {
			foreach( var entry in meta.Children ) {
				var key = (YamlScalarNode)entry.Key;
				var val = (YamlScalarNode)entry.Value;
				node.Meta[key.Value!] = val.Value!;
			}
		}

		switch( value ) {
		case YamlMappingNode obj:
			foreach( var entry in obj.Children ) {
				if ( entry.Key is not YamlScalarNode keynode ) {
					continue;
				}

				var keyname = keynode.Value!;
				if ( keyname.StartsWith( '%' ) ) {
					continue;
				}

				var entryval = entry.Value;
				obj.Children.TryGetValue( new YamlScalarNode( $"%{keyname}" ), out var metaobj );
				var entrymeta = metaobj as YamlMappingNode;
				var child = BuildNode( keyname, entryval, entrymeta );
				node.Children.Add( child );
			}
			break;
		case YamlSequenceNode arr:
			node.Type = "list";
			foreach( var item in arr.Children ) {
				var child = BuildNode( "li", item, null );
				node.Children.Add( child );
			}
			break;
		case YamlScalarNode itm:
			var raw = itm.Value!;
			node.Value = raw ?? "";

			if ( raw == null ) {
				node.Type = "null";
				node.Value = "";
				break;
			}

			if ( bool.TryParse( raw, out var val ) ) {
				node.Type = "bool";
				break;
			}

			if ( long.TryParse( raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer ) ) {
				node.Type = "integer";
				break;
			}

			if ( double.TryParse( raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var floating ) ) {
				node.Type = "float";
				break;
			}

			if ( DateTime.TryParse( raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt ) ) {
				node.Type = "datetime";
				break;
			}

			node.Type = "string";
			break;
		}

		return node;
	}

	internal (YamlNode Token, YamlMappingNode? Meta) BuildElement( ConfigNode node ) {
		(YamlNode Token, YamlMappingNode? Meta) result = new();
		result.Meta = null;
		if ( node.Meta.Count > 0 ) {
			result.Meta = new YamlMappingNode();
			foreach( var entry in node.Meta ) {
				result.Meta.Add( new YamlScalarNode( entry.Key ), new YamlScalarNode( entry.Value ) );
			}
		}

		switch( node.Type ) {
		case "category":
			var obj = new YamlMappingNode();
			foreach( var child in node.Children ) {
				var element = BuildElement( child );
				obj.Add( new YamlScalarNode( child.Name ), element.Token );
				if ( element.Meta != null ) {
					obj.Add( new YamlScalarNode( $"%{child.Name}" ), element.Meta );
				}
			}
			result.Token = obj;
			break;
		case "list":
			var arr = new YamlSequenceNode();
			foreach( var child in node.Children ) {
				var element = BuildElement( child );
				arr.Add( element.Token );
			}
			result.Token = arr;
			break;
		case "null":
			result.Token = new YamlScalarNode( (string?)null );
			break;
		case "bool":
		case "integer":
		case "float":
		case "datetime":
			result.Token = new YamlScalarNode( node.Value );
			break;
		default:
			result.Token = new YamlScalarNode( node.Value ) { Style = ScalarStyle.DoubleQuoted };
			break;
		}

		return result;
	}
}
