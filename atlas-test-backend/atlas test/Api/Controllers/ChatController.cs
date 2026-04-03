using System.Text.Json;
using atlas_test.Application.Configuration;
using atlas_test.Application.DTOs;
using atlas_test.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace atlas_test.Api.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController(IRetrievalService retrievalService, IChatService chatService, IOptions<RetrievalOptions> retrievalOptions, ILogger<ChatController> logger) : ControllerBase
{
    [HttpPost]
    [Consumes("application/json")]
    [Produces("text/event-stream")]
    public async Task Chat([FromBody] ChatRequestDto request, CancellationToken cancellationToken)
    {
        ConfigureSseResponse();

        try
        {
            var chunks = await retrievalService.RetrieveAsync(request.Question, request.CompanyName, cancellationToken);
            await WriteEventAsync(new { type = "start", retrieved = chunks.Count }, cancellationToken);

            var answer = await chatService.StreamAnswerAsync(
                request.Question,
                chunks,
                token => WriteEventAsync(new { type = "token", content = token }, cancellationToken),
                cancellationToken);

            await WriteEventAsync(new { type = "sources", ticketIds = answer.TicketIds }, cancellationToken);

            var threshold = retrievalOptions.Value.ConfidenceThreshold;
            var isLowConfidence = answer.Confidence < threshold;
            await WriteEventAsync(new
            {
                type = "confidence",
                confidence = answer.Confidence,
                isLowConfidence,
                warning = isLowConfidence ? "Not confident enough — check with a senior engineer." : (string?)null
            }, cancellationToken);

            await WriteEventAsync(new { type = "done" }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Streaming chat request was canceled by the client");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Streaming chat failed");
            await WriteEventAsync(new { type = "error", message = ex.Message }, cancellationToken);
        }

        void ConfigureSseResponse()
        {
            Response.StatusCode = StatusCodes.Status200OK;
            Response.Headers.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";
            Response.Headers.Append("X-Accel-Buffering", "no");
        }

        async Task WriteEventAsync(object payload, CancellationToken ct)
        {
            var json = JsonSerializer.Serialize(payload);
            await Response.WriteAsync($"data: {json}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
    }
}

