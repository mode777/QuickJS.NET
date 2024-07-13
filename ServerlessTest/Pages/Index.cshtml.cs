using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickJS;
using QuickJS.Lite;
using QuickJS.Native;
using static QuickJS.Native.QuickJSNativeApi;

namespace ServerlessTest.Pages;

public class IndexModel : PageModel
{
	private readonly QuickJsRuntime _runtime;

	[BindProperty]
    [Required]
    public string SourceCode { get; set; }

    public IndexModel(QuickJsRuntime runtime)
    {
		this._runtime = runtime;
	}

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        using var ctx = _runtime.NewContext();
        ctx.EvalString(SourceCode);

        // Redirect to a confirmation page or display a success message
        TempData["Message"] = "Source code submitted successfully!";
        return RedirectToPage("/Index");
    }
}
