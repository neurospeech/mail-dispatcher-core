using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailDispatcher.Core.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MailDispatcher.Pages
{
    [AllowPublic]
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
