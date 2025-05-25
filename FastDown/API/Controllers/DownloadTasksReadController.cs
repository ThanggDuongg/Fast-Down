using FastDown.Application.Queries;
using FastDown.Core;
using FastDown.Domain.ReadModels;
using Microsoft.AspNetCore.Mvc;

namespace FastDown.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DownloadTasksReadController(IMediator mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<DownloadTaskReadModel>>> GetAll()
        {
            var query = new GetAllDownloadTasksReadQuery();
            var result = await mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DownloadTaskReadModel>> GetById(int id)
        {
            var query = new GetDownloadTaskReadByIdQuery { Id = id };
            var result = await mediator.Send(query);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet("status/{status}")]
        public async Task<ActionResult<List<DownloadTaskReadModel>>> GetByStatus(string status)
        {
            var query = new GetDownloadTasksByStatusQuery { Status = status };
            var result = await mediator.Send(query);
            return Ok(result);
        }
    }
}
