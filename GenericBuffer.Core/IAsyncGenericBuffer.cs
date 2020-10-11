using System.Threading;
using System.Threading.Tasks;

namespace GenericBuffer.Core
{
    public interface IAsyncGenericBuffer<T>
    {
        Task<T> ForceRefreshAsync(CancellationToken cancellationToken);
        Task<T> GetValueAsync(CancellationToken cancellationToken);
        Task ResetAsync(CancellationToken cancellationToken);
    }
}