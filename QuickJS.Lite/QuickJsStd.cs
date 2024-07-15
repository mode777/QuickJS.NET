// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Runtime.InteropServices;
// using System.Text;
// using System.Threading.Tasks;
// using QuickJS.Native;
// using QuickJS;
// using static QuickJS.Native.QuickJSNativeApi;
// using System.IO;

// namespace QuickJS.Lite
// {
// 	public static class QuickJsStd
// 	{
// 		public static unsafe JSModuleDef my_module_loader(JSContext ctx, string module_name, IntPtr opaque)
// 		{
// 			var bytes = File.ReadAllBytes(module_name);
// 			fixed (byte* p = bytes)
// 			{
// 				JSValue func_val = JS_Eval(ctx, p, bytes.Length, module_name, JSEvalFlags.Module | JSEvalFlags.CompileOnly);

// 				if (JS_IsException(func_val))
// 				{
// 					return new JSModuleDef();
// 				}

// 				js_module_set_import_meta(ctx, func_val, true, false);
// 				JSModuleDef m = (JSModuleDef)Marshal.PtrToStructure(func_val.ToPointer(), typeof(JSModuleDef));
// 				JS_FreeValue(ctx, func_val);
// 				return m;
// 			}
// 		}

// 		public static void my_host_promise_rejection_tracker(JSContext ctx, JSValue promise, JSValue reason, bool is_handled, IntPtr opaque)
// 		{
// 			if (!is_handled) {
// 				Console.Error.WriteLine("Possibly unhandled promise rejection: ");
// 				dump_error_val(ctx, reason);
// 			}
// 		}

// 		public static string ValueToString(JSContext ctx, JSValue value){
// 			var ptr = JS_ToCString(ctx, value);
// 			if(ptr == IntPtr.Zero){
// 				return null;
// 			}
// 			var str = Marshal.PtrToStringUTF8(ptr);
// 			JS_FreeCString(ctx, ptr);
// 			return str;
// 		}

// 		public static JSValue js_print(JSContext ctx, JSValue this_val, int argc, JSValue[] argv){
// 			foreach(var val in argv){
// 				var str = ValueToString(ctx,val);
// 				if(str is null) return JSValue.Exception;
// 				Console.WriteLine(str);
// 			}
// 			return JSValue.Undefined;
// 		}

// 		private static void js_dump_obj(JSContext ctx, TextWriter writer, JSValue val)
// 		{
// 			var str = ValueToString(ctx, val);
// 			if(str is not null){
// 				writer.WriteLine(str);
// 			} else {
// 				writer.WriteLine("[exception]");
// 			}
// 		}

// 		private static void dump_error_val(JSContext ctx, JSValue exception_val){
// 			bool is_error = JS_IsError(ctx, exception_val);
// 			js_dump_obj(ctx, Console.Error, exception_val);
// 			if (is_error) {
// 				var val = JS_GetPropertyStr(ctx, exception_val, "stack");
// 				if (!JS_IsUndefined(val)) {
// 					js_dump_obj(ctx, Console.Error, val);
// 				}
// 				JS_FreeValue(ctx, val);
// 			}
// 		}

// 		private static void dump_error(JSContext ctx){
// 			var exception_val = JS_GetException(ctx);
// 			dump_error_val(ctx, exception_val);
// 			JS_FreeValue(ctx, exception_val);
// 		}

// 		private static JSValue js_std_await(JSContext ctx, JSValue obj)
// 		{
// 			JSValue ret;

// 			for(;;) {
// 				var state = JS_PromiseState(ctx, obj);
// 				if (state == JSPromiseState.Fullfilled) {
// 					ret = JS_PromiseResult(ctx, obj);
// 					JS_FreeValue(ctx, obj);
// 					break;
// 				} else if (state == JSPromiseState.Rejected) {
// 					ret = JS_Throw(ctx, JS_PromiseResult(ctx, obj));
// 					JS_FreeValue(ctx, obj);
// 					break;
// 				} else if (state == JSPromiseState.Pending) {
// 					JSContext ctx1;
// 					int err;
// 					err = JS_ExecutePendingJob(JS_GetRuntime(ctx), out ctx1);
// 					if (err < 0) {
// 						dump_error(ctx1);
// 					}
// 					// if (os_poll_func)
// 					// 	os_poll_func(ctx);
// 				} else {
// 					/* not a promise */
// 					ret = obj;
// 					break;
// 				}
// 			}
// 			return ret;
// 		}
		
// 		public static unsafe void js_eval_buf(JSContext ctx, byte[] bytes, string filename, JSEvalFlags eval_flags)
// 		{
// 			fixed (byte* buf = bytes){
// 				JSValue val;
// 				int ret;

// 				if ((eval_flags & JSEvalFlags.TypeMask) == JSEvalFlags.Module) {
// 					/* for the modules, we compile then run to be able to set
// 					import.meta */
// 					val = JS_Eval(ctx, buf, bytes.Length, filename,
// 								eval_flags | JSEvalFlags.CompileOnly);
// 					if (!JS_IsException(val)) {
// 						js_module_set_import_meta(ctx, val, true, true);
// 						val = JS_EvalFunction(ctx, val);
// 					}
// 					val = js_std_await(ctx, val);
// 				} else {
// 					val = JS_Eval(ctx, buf, bytes.Length, filename, eval_flags);
// 				}
// 				if (JS_IsException(val)) {
// 					dump_error(ctx);
// 					// TODO: Throw exception?
// 				} 
// 				// TODO finally?
// 				JS_FreeValue(ctx, val);
// 			}

// 		}

// 		static void js_module_set_import_meta(JSContext ctx, JSValue func_val, bool use_realpath, bool is_main)
// 		{
// 		// 	JSModuleDef *m;
// 		// 	char buf[1024 + 16];
// 		// 	JSValue meta_obj;
// 		// 	JSAtom module_name_atom;
// 		// 	const char *module_name;
			
// 		// 	assert(JS_VALUE_GET_TAG(func_val) == JS_TAG_MODULE);
// 		// 	m = JS_VALUE_GET_PTR(func_val);

// 		// 	module_name_atom = JS_GetModuleName(ctx, m);
// 		// 	module_name = JS_AtomToCString(ctx, module_name_atom);
// 		// 	JS_FreeAtom(ctx, module_name_atom);
// 		// 	if (!module_name)
// 		// 		return -1;
// 		// 	if (!strchr(module_name, ':')) {
// 		// 		strcpy(buf, "file://");
// 		// #if !defined(_WIN32)
// 		// 		/* realpath() cannot be used with modules compiled with qjsc
// 		// 		because the corresponding module source code is not
// 		// 		necessarily present */
// 		// 		if (use_realpath) {
// 		// 			char *res = realpath(module_name, buf + strlen(buf));
// 		// 			if (!res) {
// 		// 				JS_ThrowTypeError(ctx, "realpath failure");
// 		// 				JS_FreeCString(ctx, module_name);
// 		// 				return -1;
// 		// 			}
// 		// 		} else
// 		// #endif
// 		// 		{
// 		// 			pstrcat(buf, sizeof(buf), module_name);
// 		// 		}
// 		// 	} else {
// 		// 		pstrcpy(buf, sizeof(buf), module_name);
// 		// 	}
// 		// 	JS_FreeCString(ctx, module_name);
			
// 		// 	meta_obj = JS_GetImportMeta(ctx, m);
// 		// 	if (JS_IsException(meta_obj))
// 		// 		return -1;
// 		// 	JS_DefinePropertyValueStr(ctx, meta_obj, "url",
// 		// 							JS_NewString(ctx, buf),
// 		// 							JS_PROP_C_W_E);
// 		// 	JS_DefinePropertyValueStr(ctx, meta_obj, "main",
// 		// 							JS_NewBool(ctx, is_main),
// 		// 							JS_PROP_C_W_E);
// 		// 	JS_FreeValue(ctx, meta_obj);
// 		// 	return 0;
// 		}
// 	}
// }
