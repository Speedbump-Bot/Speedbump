using Microsoft.AspNetCore.Mvc;

namespace Speedbump.Endpoints
{
    [Route("api/{controller}")]
    public class UserController : ControllerBase
    {
        public ContentResult Index()
        {
            return this.Respond(this.User(), System.Net.HttpStatusCode.OK);
        }
    }
}
