using FastDown.Application.Commands;
using FastDown.Application.Queries;
using FastDown.Core;
using FastDown.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FastDown.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DownloadTasksController(IMediator mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<DownloadTask>>> GetAll()
        {
            var query = new GetAllDownloadTasksQuery();
            var result = await mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DownloadTask>> GetById(int id)
        {
            var query = new GetDownloadTaskByIdQuery { Id = id };
            var result = await mediator.Send(query);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<int>> Create(CreateDownloadTaskCommand command)
        {
            var id = await mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }

        [HttpPost("{id}/start")]
        public async Task<ActionResult<bool>> StartDownload(int id)
        {
            var command = new StartDownloadCommand { DownloadTaskId = id };
            var result = await mediator.Send(command);

            if (!result)
                return BadRequest("Failed to start download");

            return Ok(result);
        }
    }
}
