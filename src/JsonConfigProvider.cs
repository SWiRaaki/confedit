using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal class JsonConfigProvider : IConfigProvider {
	public Result<ConfigTree> Load( string file ) {
		try {
			var content = File.ReadAllText( file );
			var doc = JToken.Parse( content );

			if ( doc.Type != JTokenType.Object ) {
				return new Result<ConfigTree>() {
					Code = -1,
					Message = "Invalid configuration: root must be a JSON object",
					Data = null
				};
			}

			var tree = new ConfigTree() {
				Configuration = Path.GetFileName( file )
			};

			var root = (JObject)doc;
			foreach( var token in root.Properties() ) {
				if ( token.Name.StartsWith( "%" ) ) {
					continue;
				}

				root.TryGetValue( $"%{token.Name}", out var metaobj );
				JObject? met = metaobj as JObject;
				var child = BuildNode( token, met );
				tree.Items.Add( child );
			}

			return new Result<ConfigTree>() {
				Code = 0,
				Message = "OK",
				Data = tree
			};
		}
		catch( Exception e ) {
			return new Result<ConfigTree>() {
				Code = -1,
				Message = e.Message,
				Data = null
			};
		}
	}

	public Result Save( string file, ConfigTree data ) {
		try {
			var root = new JObject();

			foreach( var item in data.Items ) {
				var element = BuildElement( item );
				root[item.Name] = element.Token;
				if ( element.Meta != null ) {
					root[$"%{item.Name}"] =  element.Meta;
				}
			}

			File.WriteAllText( file, root.ToString( Formatting.Indented ), Encoding.UTF8 );
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

	internal ConfigNode BuildNode( JProperty item, JObject? meta ) {
		return BuildNode( item.Name, item.Value, meta );
	}

	internal ConfigNode BuildNode( string name, JToken value, JObject? meta ) {
		var node = new ConfigNode() {
			Name = name,
			Type = "category",
			Value = ""
		};

		if ( meta != null ) {
			foreach( var token in meta.Properties() ) {
				node.Meta[token.Name] = (string)token.Value;
			}
		}

		switch( value.Type ) {
		case JTokenType.Object:
			var obj = (JObject)value;
			foreach( var token in obj.Properties() ) {
				if ( token.Name.StartsWith( "%" ) ) {
					continue;
				}

				obj.TryGetValue( $"%{token.Name}", out var metaobj );
				JObject? met = metaobj as JObject;
				var child = BuildNode( token, met );
				node.Children.Add( child );
			}
			break;
		case JTokenType.Array:
			node.Type = "list";
			var arr = (JArray)value;
			foreach( var token in arr ) {
				var child = BuildNode( "li", token, null );
				node.Children.Add( child );
			}
			break;
		case JTokenType.Integer:
			node.Type = "integer";
			node.Value = ((long)value).ToString();
			break;
		case JTokenType.Float:
			node.Type = "float";
			node.Value = ((double)value).ToString();
			break;
		case JTokenType.Boolean:
			node.Type = "bool";
			node.Value = ((bool)value).ToString();
			break;
		case JTokenType.Null:
			node.Type = "null";
			node.Value = "";
			break;
		case JTokenType.Date:
			node.Type = "datetime";
			node.Value = (string)value;
			break;
		default:
			node.Type = "string";
			node.Value = (string)value;
			break;
		}

		return node;
	}

	internal (JToken Token, JObject? Meta) BuildElement( ConfigNode node ) {
		(JToken Token, JObject? Meta) result = new();

		if ( node.Meta.Count != 0 ) {
			result.Meta = new JObject();
			foreach( var meta in node.Meta ) {
				result.Meta[meta.Key] = meta.Value;
			}
		}

		switch( node.Type ) {
		case "category":
			{
				var obj = new JObject();
				foreach( var child in node.Children ) {
					var element = BuildElement( child );
					obj[child.Name] = element.Token;
					if ( element.Meta != null ) {
						obj[$"%{child.Name}"] = element.Meta;
					}
				}
				result.Token = obj;
				break;
			}
		case "list":
			{
				var arr = new JArray();
				foreach( var child in node.Children ) {
					var element = BuildElement( child );

					arr.Add( element.Token );
				}
				result.Token = arr;
				break;
			}
		case "null":
			result.Token = JValue.CreateNull();
			break;
		case "bool":
			result.Token = new JValue( bool.Parse( node.Value ) );
			break;
		case "integer":
			result.Token = new JValue( long.Parse( node.Value ) );
			break;
		case "float":
			result.Token = new JValue( double.Parse( node.Value ) );
			break;
		default:
			result.Token = new JValue( node.Value );
			break;
		}

		return result;
	}
}
