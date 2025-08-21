using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TestApp.Core.Boundary;

namespace TestApp.Application.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CharactersController : ControllerBase
    {
        private readonly IMediator mediator;

        public CharactersController(IMediator mediator)
        {
            this.mediator = mediator;
        }

        [HttpPost()]
        public async Task<IActionResult> AddCharacter(AddCharacterRequest request)
        {
            try
            {
                var result = await this.mediator.Send(request);

                return result.ToActionResult(this);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut()]
        public async Task<IActionResult> UpdateCharacter(UpdateCharacterRequest request)
        {
            var result = await this.mediator.Send(request);

            return result.ToActionResult(this);
        }

        [HttpDelete]
        public async Task<IActionResult> RemoveCharacter(RemoveCharacterRequest request)
        {
            var result = await this.mediator.Send(request);

            return result.ToActionResult(this);
        }
    }
}