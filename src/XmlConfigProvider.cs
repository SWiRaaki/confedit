using System.Xml.Linq;
using Newtonsoft.Json;

internal class XmlConfigProvider : IConfigProvider {
	public Result<ConfigTree> Load( string file ) {
		try {
			var content = File.ReadAllText( file );
			var doc = XDocument.Parse( content );
			var tree = new ConfigTree() {
				Configuration = Path.GetFileName( file )
			};

			foreach ( var item in doc.Root!.Elements() ) {
				tree.Items.Add( BuildNode( item ) );
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
		var doc = new XDocument( new XElement( "Config" ) );
		foreach( ConfigNode node in data.Items ) {
			doc.Root!.Add( BuildElement( node ) );
		}

		try {
			doc.Save( file );
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

	internal ConfigNode BuildNode( XElement item ) {
		var node = new ConfigNode() {
			Name = item.Name.LocalName,
			Type = "category",
			Value = ""
		};

		foreach( var attribute in item.Attributes() ) {
			if ( attribute.Name.LocalName == "Type" )
				continue;

			node.Meta[attribute.Name.LocalName] = attribute.Value;
		}

		foreach( var child in item.Elements() ) {
			node.Children.Add( BuildNode( child ) );
		}

		if ( !item.HasElements ) {
			node.Type = item.Attribute( "Type" )?.Value ?? "string";
			node.Value = item.Value;
		} else if ( item.Elements().All( e => e.Name.LocalName == "li" ) ) {
			node.Type = "list";
		}

		return node;
	}

	internal XElement BuildElement( ConfigNode node ) {
		var element = new XElement( node.Name );
		element.SetAttributeValue( "Type", node.Type );
		switch( node.Type ) {
		case "category":
			element.SetAttributeValue( "Type", null );
			foreach( var child in node.Children ) {
				element.Add( BuildElement( child ) );
			}
			break;
		case "list":
			foreach( var child in node.Children ) {
				XElement item = BuildElement( child );
				item.Name = "li";
				element.Add( item );
			}
			break;
		case "null":
			break;
		case "string":
			element.SetAttributeValue( "Type", null );
			element.Value = node.Value;
			break;
		default:
			element.Value = node.Value;
			break;
		}

		foreach( var meta in node.Meta ) {
			element.SetAttributeValue( meta.Key, meta.Value );
		}

		return element;
	}
}
