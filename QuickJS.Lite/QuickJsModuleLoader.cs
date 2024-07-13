using System.IO;

namespace QuickJS;

public class QuickJsModuleLoader : IQuickJsModuleLoader
{
	public byte[] LoadModule(string name)
	{
		return File.ReadAllBytes(name);
	}

	public string NormalizeModuleName(string name, string baseName)
	{
		return name;
	}
}
