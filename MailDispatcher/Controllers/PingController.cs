using MailDispatcher.Core.Auth;
using Microsoft.AspNetCore.Mvc;

namespace MailDispatcher.Controllers
{
    [Route("ping")]
    public class PingController: Controller
    {

        [HttpGet, AllowPublic]
        public string Ping()
        {
            return "pong";
        }

    }
}
