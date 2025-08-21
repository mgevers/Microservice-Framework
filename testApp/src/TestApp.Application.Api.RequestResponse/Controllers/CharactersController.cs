using Common.Infrastructure.ServiceBus.NServiceBus.RequestResponse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TestApp.Core.Boundary;

namespace TestApp.Application.Api.RequestResponse.Controllers
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
            var result = await this.messageSession.Request<CommandResult>(request);

            return result.Result.IsSuccess ? Ok(result.Result) : StatusCode(500, result.Result.Error);
        }

        [HttpPut()]
        public async Task<IActionResult> UpdateCharacter(UpdateCharacterCommand request)
        {
            var result = await this.messageSession.Request<CommandResult>(request);

            return result.Result.IsSuccess ? Ok(result.Result) : StatusCode(500, result.Result.Error);
        }

        [HttpDelete]
        public async Task<IActionResult> RemoveCharacter(RemoveCharacterCommand request)
        {
            var result = await this.messageSession.Request<CommandResult>(request);

            return result.Result.IsSuccess ? Ok(result.Result) : StatusCode(500, result.Result.Error);
        }
    }
}