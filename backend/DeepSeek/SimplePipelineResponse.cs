using System.ClientModel.Primitives;

namespace ChatBot.DeepSeek;

public class SimplePipelineResponse : PipelineResponse
{
    private readonly HttpResponseMessage _httpResponse;
    private readonly PipelineResponseHeaders _headers;
    private bool _disposed;
    private BinaryData? _bufferedContent;

    public SimplePipelineResponse(HttpResponseMessage httpResponse)
    {
        _httpResponse = httpResponse ?? throw new ArgumentNullException(nameof(httpResponse));
        _headers = new HttpResponseHeaderWrapper(_httpResponse);
    }

    public override int Status => (int)_httpResponse.StatusCode;

    public override string ReasonPhrase => _httpResponse.ReasonPhrase ?? string.Empty;

    protected override PipelineResponseHeaders HeadersCore => _headers;

    public override Stream? ContentStream
    {
        get => _httpResponse.Content?.ReadAsStream();
        set => throw new NotSupportedException("Setting the stream directly on this wrapper is not supported.");
    }

    // --- THE FIX: Implemented Content Property ---
    public override BinaryData Content
    {
        get
        {
            // If we haven't buffered it yet, we must buffer it now to return BinaryData.
            // This forces a synchronous read if the user accesses this property directly.
            if (_bufferedContent == null)
            {
                _bufferedContent = BufferContent(CancellationToken.None);
            }
            return _bufferedContent;
        }
    }

    public override BinaryData BufferContent(CancellationToken cancellationToken = default)
    {
        if (_bufferedContent != null)
        {
            return _bufferedContent;
        }

        if (_httpResponse.Content == null)
        {
            _bufferedContent = new BinaryData(Array.Empty<byte>());
            return _bufferedContent;
        }

        // Synchronously load content
        using var stream = _httpResponse.Content.ReadAsStream(cancellationToken);
        _bufferedContent = BinaryData.FromStream(stream);
        return _bufferedContent;
    }

    public override async ValueTask<BinaryData> BufferContentAsync(CancellationToken cancellationToken = default)
    {
        if (_bufferedContent != null)
        {
            return _bufferedContent;
        }

        if (_httpResponse.Content == null)
        {
            _bufferedContent = new BinaryData(Array.Empty<byte>());
            return _bufferedContent;
        }

        // Asynchronously load content
        var stream = await _httpResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        _bufferedContent = await BinaryData.FromStreamAsync(stream, cancellationToken).ConfigureAwait(false);
        return _bufferedContent;
    }

    public override void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _httpResponse.Dispose();
        }
        _disposed = true;
    }

    // --- Inner Class: Header Adapter ---
    private sealed class HttpResponseHeaderWrapper : PipelineResponseHeaders
    {
        private readonly HttpResponseMessage _response;

        public HttpResponseHeaderWrapper(HttpResponseMessage response)
        {
            _response = response;
        }

        public override bool TryGetValue(string name, out string? value)
        {
            if (_response.Headers.TryGetValues(name, out var values) ||
                (_response.Content != null && _response.Content.Headers.TryGetValues(name, out values)))
            {
                value = string.Join(",", values);
                return true;
            }
            value = null;
            return false;
        }

        public override bool TryGetValues(string name, out IEnumerable<string>? values)
        {
            if (_response.Headers.TryGetValues(name, out values) ||
                (_response.Content != null && _response.Content.Headers.TryGetValues(name, out values)))
            {
                return true;
            }
            values = null;
            return false;
        }

        public override IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            var allHeaders = _response.Headers.Concat(
                _response.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>()
            );

            foreach (var header in allHeaders)
            {
                yield return new KeyValuePair<string, string>(header.Key, string.Join(",", header.Value));
            }
        }
    }
}