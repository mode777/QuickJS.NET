using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickJS;
using QuickJS.Native;
using static QuickJS.Native.QuickJSNativeApi;

namespace ServerlessTest.Pages;

public class IndexModel : PageModel
{
	private readonly QuickJsContext _jsContext;

	[BindProperty]
    [Required]
    public string SourceCode { get; set; }

    public IndexModel(QuickJsContext jsContext)
    {
		this._jsContext = jsContext;
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

        _jsContext.EvalString(SourceCode);

        // Redirect to a confirmation page or display a success message
        TempData["Message"] = "Source code submitted successfully!";
        return RedirectToPage("/Index");
    }
}
