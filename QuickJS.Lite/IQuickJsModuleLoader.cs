namespace QuickJS;

public interface IQuickJsModuleLoader {
	string NormalizeModuleName(string name, string baseName);
	byte[] LoadModule(string name);
}
