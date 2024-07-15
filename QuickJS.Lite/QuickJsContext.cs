using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using QuickJS.Native;
using static QuickJS.Native.QuickJSNativeApi;

namespace QuickJS;

public class QuickJsContext : IDisposable 
{
	private JSContext _ctx;
	private bool disposedValue;
	private readonly ILogger<QuickJsContext> _logger;
	private readonly QuickJsRuntime _rt;

	public QuickJsContext(QuickJsRuntime rt, ILogger<QuickJsContext> logger){
		this._logger = logger;
		_rt = rt;
		_ctx = JS_NewContext(rt.Runtime);
		var global_obj = JS_GetGlobalObject(_ctx);
		var console = JS_NewObject(_ctx);
		JS_SetPropertyStr(_ctx, console, "log", JS_NewCFunction(_ctx, ConsoleLog, "log", 1));
		JS_SetPropertyStr(_ctx, console, "warn", JS_NewCFunction(_ctx, ConsoleWarn, "warn", 1));
		JS_SetPropertyStr(_ctx, console, "error", JS_NewCFunction(_ctx, ConsoleErr, "error", 1));
		JS_SetPropertyStr(_ctx, global_obj, "console", console);
		JS_FreeValue(_ctx, global_obj);
	}

	public void EvalString(string source, string moduleName = "main"){
		var bytes = Encoding.UTF8.GetBytes(source);
		EvalBuffer(_ctx, bytes, moduleName, JSEvalFlags.Module);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				// TODO: dispose managed state (managed objects)
			}

			JS_FreeContext(_ctx);
			disposedValue = true;
		}
	}

	~QuickJsContext()
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

	private string LogString(JSContext ctx, JSValue[] argv) => string.Join(", ", argv.Select(x => x.ToString(ctx)).Where(x => x != null));

	public JSValue ConsoleLog(JSContext ctx, JSValue this_val, int argc, JSValue[] argv){
		_logger.LogInformation(LogString(ctx, argv));
		return JSValue.Undefined;
	}

	public JSValue ConsoleWarn(JSContext ctx, JSValue this_val, int argc, JSValue[] argv){
		_logger.LogWarning(LogString(ctx, argv));
		return JSValue.Undefined;
	}

	public JSValue ConsoleErr(JSContext ctx, JSValue this_val, int argc, JSValue[] argv){
		_logger.LogError(LogString(ctx, argv));
		return JSValue.Undefined;
	}

	private void DumpObj(JSContext ctx, LogLevel level, JSValue val)
	{
		var str = val.ToString(ctx);
		if(str is not null){
			_logger.Log(level, str);
		} else {
			_logger.Log(level, "[EXCEPTION]");
		}
	}

	private void DumpErrorVal(JSContext ctx, JSValue exception_val){
		bool is_error = JS_IsError(ctx, exception_val);
		DumpObj(ctx, LogLevel.Error, exception_val);
		if (is_error) {
			var val = JS_GetPropertyStr(ctx, exception_val, "stack");
			if (!JS_IsUndefined(val)) {
				DumpObj(ctx, LogLevel.Error, val);
			}
			JS_FreeValue(ctx, val);
		}
	}

	private void DumpError(JSContext ctx){
		var exception_val = JS_GetException(ctx);
		DumpErrorVal(ctx, exception_val);
		JS_FreeValue(ctx, exception_val);
	}

	private JSValue AwaitVal(JSContext ctx, JSValue obj)
	{
		JSValue ret;

		for(;;) {
			var state = JS_PromiseState(ctx, obj);
			if (state == JSPromiseState.Fullfilled) {
				ret = JS_PromiseResult(ctx, obj);
				JS_FreeValue(ctx, obj);
				break;
			} else if (state == JSPromiseState.Rejected) {
				ret = JS_Throw(ctx, JS_PromiseResult(ctx, obj));
				JS_FreeValue(ctx, obj);
				break;
			} else if (state == JSPromiseState.Pending) {
				JSContext ctx1;
				int err;
				err = JS_ExecutePendingJob(JS_GetRuntime(ctx), out ctx1);
				if (err < 0) {
					DumpError(ctx1);
				}
				// if (os_poll_func)
				// 	os_poll_func(ctx);
			} else {
				/* not a promise */
				ret = obj;
				break;
			}
		}
		return ret;
	}

	private unsafe byte[] CompileBuffer(JSContext ctx, byte[] bytes, string filename, JSEvalFlags eval_flags){
		fixed (byte* buf = bytes){
			/* compile the module */
			var func_val = JS_Eval(ctx, buf, bytes.Length, filename,
							JSEvalFlags.Module | JSEvalFlags.CompileOnly);
			if (JS_IsException(func_val))
				return null;
			var flags = JSReaderWriterFlags.ReadObjBytecode;
			var out_buf = JS_WriteObject(ctx,out var out_buf_len, func_val, flags);
			if (out_buf == IntPtr.Zero) {
				DumpError(ctx);
				return null;
			}
			js_free(ctx, out_buf);

			// TODO: What do we do with the module?
			/* the module is already referenced, so we must free it */
			//m = JS_VALUE_GET_PTR(func_val);

			JS_FreeValue(ctx, func_val);
			byte[] managedArray = new byte[out_buf_len];
 			Marshal.Copy(out_buf, managedArray, 0, (int)out_buf_len);
			return managedArray;
		}
		
	}
		
	private unsafe void EvalBuffer(JSContext ctx, byte[] bytes, string filename, JSEvalFlags eval_flags)
	{
		fixed (byte* buf = bytes){
			JSValue val;
			int ret;

			if ((eval_flags & JSEvalFlags.TypeMask) == JSEvalFlags.Module) {
				/* for the modules, we compile then run to be able to set
				import.meta */
				val = JS_Eval(ctx, buf, bytes.Length, filename,
							eval_flags | JSEvalFlags.CompileOnly);
				if (!JS_IsException(val)) {
					_rt.SetModuleImportMeta(ctx, val, true, true);
					val = JS_EvalFunction(ctx, val);
				}
				val = AwaitVal(ctx, val);
			} else {
				val = JS_Eval(ctx, buf, bytes.Length, filename, eval_flags);
			}
			if (JS_IsException(val)) {
				DumpError(ctx);
				// TODO: Throw exception?
			} 
			// TODO finally?
			JS_FreeValue(ctx, val);
		}
	}
}
