
using System.IO;
using System.Resources;
using System.Reflection;

namespace System.Resources;

public class ResourceLoader {
	public static string Load(string name){
		// Determine path
		var assembly = Assembly.GetExecutingAssembly();
		string resourcePath = name;
		// Format: "{Namespace}.{Folder}.{filename}.{Extension}"
		resourcePath = assembly.GetManifestResourceNames()
			.Single(str => str.EndsWith(name));

		using (Stream stream = assembly.GetManifestResourceStream(resourcePath)!)
		using (StreamReader reader = new StreamReader(stream))
		{
			return reader.ReadToEnd();
		}
	}
}