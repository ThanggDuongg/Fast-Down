namespace FastDown.Core
{
    /// <summary>
    /// Marker interface for queries that return a response.
    /// </summary>
    public interface IQuery<TResponse> : IRequest<TResponse> { }
}
