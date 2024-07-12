using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using QuickJS.Native;
using QuickJS;
using static QuickJS.Native.QuickJSNativeApi;

namespace TestApp2
{
	internal static class QuickJsStd
	{
		internal static unsafe JSModuleDef my_module_loader(JSContext ctx, string module_name, IntPtr opaque)
		{
			var bytes = File.ReadAllBytes(module_name);
			var module_name_bytes = Encoding.ASCII.GetBytes(module_name);
			fixed (byte* p = bytes)
			{
				// Convert module_name to ASCII byte*
				JSValue func_val = JS_Eval(ctx, p, new SizeT(bytes.Length), module_name, JSEvalFlags.Module | JSEvalFlags.CompileOnly);

				if (JS_IsException(func_val))
				{
					return new JSModuleDef();
				}

				js_module_set_import_meta(ctx, func_val, true, false);
				JSModuleDef m = (JSModuleDef)Marshal.PtrToStructure(func_val.ToPointer(), typeof(JSModuleDef));
				JS_FreeValue(ctx, func_val);
				return m;
			}
		}

		internal static void my_host_promise_rejection_tracker(JSContext ctx, JSValue promise, JSValue reason, bool is_handled, IntPtr opaque)
		{
			if (!is_handled)
			{
				if (JS_IsError(ctx, reason))
				{
					js_std_dump_error(ctx);
				}
			}
		}
	}
}
