using System.Diagnostics.CodeAnalysis;

namespace FastDown.Core
{
    /// <summary>
    /// Marker interface for a request with a response type.
    /// </summary>
    [SuppressMessage("Major Code Smell", "S2326", Justification = "This is a marker interface")]
    public interface IRequest<TResponse> { }
}
