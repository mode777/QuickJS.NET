using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using QuickJS;
using QuickJS.Native;
using TestApp2;
using static QuickJS.Native.QuickJSNativeApi;
// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var rt = JS_NewRuntime();
var ctx = JS_NewContext(rt);
JSModuleLoaderFunc loader_func = QuickJsStd.my_module_loader;
JSModuleNormalizeFunc norm_func = null;
JS_SetModuleLoaderFunc(rt, norm_func, loader_func, IntPtr.Zero);
JSHostPromiseRejectionTracker tracker = QuickJsStd.my_host_promise_rejection_tracker;
JS_SetHostPromiseRejectionTracker(rt, tracker, IntPtr.Zero);
var bytes = Encoding.UTF8.GetBytes("throw new Error('Hello World')");
QuickJsStd.js_eval_buf(ctx, bytes, "main", JSEvalFlags.Module);




