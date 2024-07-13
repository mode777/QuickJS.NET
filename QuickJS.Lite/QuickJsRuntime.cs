using System;
using System.Dynamic;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using QuickJS.Native;
using static QuickJS.Native.QuickJSNativeApi;

namespace QuickJS;

public class QuickJsRuntime : IDisposable
{
	private bool _disposedValue;
    private readonly JSRuntime _runtime;
	private readonly ILogger<QuickJsRuntime> _logger;
	private readonly IQuickJsModuleLoader _loader;
	internal JSRuntime Runtime => _runtime;

	public QuickJsRuntime(ILogger<QuickJsRuntime> logger, IQuickJsModuleLoader loader)
    {
		_logger = logger;
		_loader = loader;
		_runtime = JS_NewRuntime();
		JSModuleLoaderFunc loaderFunc = ModuleLoaderDelegate;
		JSModuleNormalizeFunc normalizeFunc = ModuleNormalizeDelegate;
		JS_SetModuleLoaderFunc(_runtime, normalizeFunc, loaderFunc, IntPtr.Zero);
	}

	public QuickJsContext NewContext(){
		return new QuickJsContext(this, _logger);
	}

	private IntPtr ModuleNormalizeDelegate(JSContext ctx, string baseName, string name, IntPtr opaque)
	{
		var str = _loader.NormalizeModuleName(name, baseName);
		if(str == null) return IntPtr.Zero;
		byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
		var mem = js_malloc(ctx, bytes.Length+1);
		Marshal.Copy(bytes, 0, mem, bytes.Length+1);
        // Add null terminator at the end
        Marshal.WriteByte(mem, bytes.Length, 0);
		return mem;
	}

	private unsafe JSModuleDef ModuleLoaderDelegate(JSContext ctx, string module_name, IntPtr opaque)
	{
		var bytes = _loader.LoadModule(module_name);
		fixed (byte* p = bytes)
		{
			JSValue func_val = JS_Eval(ctx, p, bytes.Length, module_name, JSEvalFlags.Module | JSEvalFlags.CompileOnly);

			if (JS_IsException(func_val))
			{
				return new JSModuleDef();
			}

			SetModuleImportMeta(ctx, func_val, true, false);
			JSModuleDef m = (JSModuleDef)Marshal.PtrToStructure(func_val.ToPointer(), typeof(JSModuleDef));
			JS_FreeValue(ctx, func_val);
			return m;
		}
	}

	internal void SetModuleImportMeta(JSContext ctx, JSValue func_val, bool use_realpath, bool is_main)
	{
	// 	JSModuleDef *m;
	// 	char buf[1024 + 16];
	// 	JSValue meta_obj;
	// 	JSAtom module_name_atom;
	// 	const char *module_name;
		
	// 	assert(JS_VALUE_GET_TAG(func_val) == JS_TAG_MODULE);
	// 	m = JS_VALUE_GET_PTR(func_val);

	// 	module_name_atom = JS_GetModuleName(ctx, m);
	// 	module_name = JS_AtomToCString(ctx, module_name_atom);
	// 	JS_FreeAtom(ctx, module_name_atom);
	// 	if (!module_name)
	// 		return -1;
	// 	if (!strchr(module_name, ':')) {
	// 		strcpy(buf, "file://");
	// #if !defined(_WIN32)
	// 		/* realpath() cannot be used with modules compiled with qjsc
	// 		because the corresponding module source code is not
	// 		necessarily present */
	// 		if (use_realpath) {
	// 			char *res = realpath(module_name, buf + strlen(buf));
	// 			if (!res) {
	// 				JS_ThrowTypeError(ctx, "realpath failure");
	// 				JS_FreeCString(ctx, module_name);
	// 				return -1;
	// 			}
	// 		} else
	// #endif
	// 		{
	// 			pstrcat(buf, sizeof(buf), module_name);
	// 		}
	// 	} else {
	// 		pstrcpy(buf, sizeof(buf), module_name);
	// 	}
	// 	JS_FreeCString(ctx, module_name);
		
	// 	meta_obj = JS_GetImportMeta(ctx, m);
	// 	if (JS_IsException(meta_obj))
	// 		return -1;
	// 	JS_DefinePropertyValueStr(ctx, meta_obj, "url",
	// 							JS_NewString(ctx, buf),
	// 							JS_PROP_C_W_E);
	// 	JS_DefinePropertyValueStr(ctx, meta_obj, "main",
	// 							JS_NewBool(ctx, is_main),
	// 							JS_PROP_C_W_E);
	// 	JS_FreeValue(ctx, meta_obj);
	// 	return 0;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				// TODO: dispose managed state (managed objects)
			}
			JS_FreeRuntime(_runtime);

			_disposedValue = true;
		}
	}

	~QuickJsRuntime()
	{
	    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
	    Dispose(disposing: false);
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
