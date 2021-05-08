using System;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.AspNetCore.Http;

namespace OrchardCore.Apis.GraphQL
{
    internal static class DocumentWriterExtensions
    {
        public static async Task WriteErrorAsync(this IDocumentWriter documentWriter, HttpContext context, string message, Exception e = null)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var errorResult = new ExecutionResult
            {
                Errors = new ExecutionErrors()
            };

            if (e == null)
            {
                errorResult.Errors.Add(new ExecutionError(message));
            }
            else
            {
                errorResult.Errors.Add(new ExecutionError(message, e));
            }

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = MediaTypeNames.Application.Json;

            await documentWriter.WriteAsync(context.Response.Body, errorResult);
        }
    }
}
