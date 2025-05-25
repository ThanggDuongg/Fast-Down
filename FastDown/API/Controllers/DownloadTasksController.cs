using FastDown.Application.Commands;
using FastDown.Application.Queries;
using FastDown.Core;
using FastDown.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

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

        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadFile(int id, CancellationToken cancellationToken)
        {
            var query = new GetDownloadTaskByIdQuery { Id = id };
            var downloadTask = await mediator.Send(query, cancellationToken);

            if (downloadTask == null)
                return NotFound();

            if (downloadTask.Status != "Completed")
                return BadRequest("File is not ready for download");

            var downloadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Downloads");
            var filePath = Path.Combine(downloadsPath, downloadTask.FileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound("File not found on server");

            var memory = new MemoryStream();

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                await stream.CopyToAsync(memory, cancellationToken);
            }

            memory.Position = 0;

            return File(memory, GetContentType(filePath), downloadTask.FileName);
        }

        [HttpGet("{id}/stream")]
        public async Task<IActionResult> StreamFile(int id, CancellationToken cancellationToken)
        {
            var query = new GetDownloadTaskByIdQuery { Id = id };
            var downloadTask = await mediator.Send(query, cancellationToken);

            if (downloadTask == null)
                return NotFound();

            if (downloadTask.Status != "Completed")
                return BadRequest("File is not ready for download");

            var downloadsPath = Path.Combine(Directory.GetCurrentDirectory(), "Downloads");
            var filePath = Path.Combine(downloadsPath, downloadTask.FileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound("File not found on server");

            var fileInfo = new FileInfo(filePath);

            Response.Headers.ContentDisposition = $"attachment; filename={downloadTask.FileName}";
            Response.Headers["Content-Length"] = fileInfo.Length.ToString();

            return new FileStreamResult(
                new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read),
                GetContentType(filePath)
            );
        }

        private static string GetContentType(string filePath)
        {
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filePath, out var contentType))
            {
                contentType = "application/octet-stream";
            }
            return contentType;
        }
    }
}
