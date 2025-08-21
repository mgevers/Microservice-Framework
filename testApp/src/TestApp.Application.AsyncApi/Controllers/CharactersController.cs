using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TestApp.Core.Boundary;

namespace TestApp.Application.AsyncApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CharactersController : ControllerBase
    {
        private readonly IMessageSession messageSession;

        public CharactersController(IMessageSession messageSession)
        {
            this.messageSession = messageSession;
        }

        [HttpPost()]
        public async Task<IActionResult> AddCharacter(AddCharacterCommand request)
        {
            await this.messageSession.Send(request);
            return Accepted();
        }

        [HttpPut()]
        public async Task<IActionResult> UpdateCharacter(UpdateCharacterCommand request)
        {
            await this.messageSession.Send(request);
            return Accepted();
        }

        [HttpDelete]
        public async Task<IActionResult> RemoveCharacter(RemoveCharacterCommand request)
        {
            await this.messageSession.Send(request);
            return Accepted();
        }
    }
}