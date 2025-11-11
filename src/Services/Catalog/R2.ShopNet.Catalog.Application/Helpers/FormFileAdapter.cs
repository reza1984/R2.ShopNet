using System.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace R2.ShopNet.Catalog.Application.Helpers;

/// <summary>
/// Adapter to convert byte array and metadata to IFormFile for file upload operations.
/// </summary>
public class FormFileAdapter : IFormFile
{
    private readonly Stream _stream;
    private readonly string _fileName;
    private readonly string _contentType;
    private readonly long _length;
    private static readonly IHeaderDictionary _emptyHeaders = new EmptyHeaderDictionary();

    public FormFileAdapter(Stream stream, string fileName, string contentType, long length)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        _contentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        _length = length;
    }

    public string ContentType => _contentType;

    public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{_fileName}\"";

    public IHeaderDictionary Headers => _emptyHeaders;

    public long Length => _length;

    public string Name => "file";

    public string FileName => _fileName;

    public void CopyTo(Stream target)
    {
        _stream.CopyTo(target);
    }

    public async Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
    {
        await _stream.CopyToAsync(target, cancellationToken);
    }

    public Stream OpenReadStream()
    {
        return _stream;
    }
}

/// <summary>
/// Empty implementation of IHeaderDictionary for FormFileAdapter.
/// </summary>
internal class EmptyHeaderDictionary : IHeaderDictionary
{
    public StringValues this[string key]
    {
        get => StringValues.Empty;
        set { }
    }

    public long? ContentLength
    {
        get => null;
        set { }
    }

    public ICollection<string> Keys => Array.Empty<string>();
    public ICollection<StringValues> Values => Array.Empty<StringValues>();
    public int Count => 0;
    public bool IsReadOnly => true;

    public void Add(string key, StringValues value) { }
    public void Add(KeyValuePair<string, StringValues> item) { }
    public void Clear() { }
    public bool Contains(KeyValuePair<string, StringValues> item) => false;
    public bool ContainsKey(string key) => false;
    public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex) { }
    public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator() => Enumerable.Empty<KeyValuePair<string, StringValues>>().GetEnumerator();
    public bool Remove(string key) => false;
    public bool Remove(KeyValuePair<string, StringValues> item) => false;
    public bool TryGetValue(string key, out StringValues value)
    {
        value = StringValues.Empty;
        return false;
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
