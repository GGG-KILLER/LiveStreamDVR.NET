using System.Net;

namespace LiveStreamDVR.Api.Exceptions;

public sealed class TwitchRequestException(string message, Uri requestUri, string requestContent, HttpStatusCode responseStatusCode, string responseContent)
    : Exception(message)
{
    public Uri RequestUri { get; } = requestUri;
    public string RequestContent { get; } = requestContent;
    public HttpStatusCode ResponseStatusCode { get; } = responseStatusCode;
    public string ResponseContent { get; } = responseContent;
}
