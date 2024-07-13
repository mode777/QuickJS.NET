using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using QuickJS;
using QuickJS.Native;
using static QuickJS.Native.QuickJSNativeApi;

namespace TestApp2;

internal class Program
{
	private static void Main(string[] args)
	{
		var rt = JS_NewRuntime();
		var ctx = JS_NewContext(rt);
		JSModuleLoaderFunc loader_func = QuickJsStd.my_module_loader;
		JSModuleNormalizeFunc norm_func = null;
		JS_SetModuleLoaderFunc(rt, norm_func, loader_func, nint.Zero);
		//JSHostPromiseRejectionTracker tracker = QuickJsStd.my_host_promise_rejection_tracker;
		//JS_SetHostPromiseRejectionTracker(rt, tracker, nint.Zero);
		
		var global_obj = JS_GetGlobalObject(ctx);
		var console = JS_NewObject(ctx);
		JS_SetPropertyStr(ctx, console, "log", JS_NewCFunction(ctx, QuickJsStd.js_print, "log", 1));
		JS_SetPropertyStr(ctx, global_obj, "console", console);
		JS_FreeValue(ctx, global_obj);
		
		var bytes = Encoding.UTF8.GetBytes("throw new Error('Hello World')");
		//var bytes = Encoding.UTF8.GetBytes("console.log('Hello World!')");
		QuickJsStd.js_eval_buf(ctx, bytes, "main", JSEvalFlags.Module);
		
		JS_FreeContext(ctx);
		JS_FreeRuntime(rt);
	}
}