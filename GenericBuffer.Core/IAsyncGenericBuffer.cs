using System.Threading.Tasks;

namespace GenericBuffer.Core
{
    public interface IAsyncGenericBuffer<T>
    {
        Task<T> ForceRefreshAsync();
        Task<T> GetValueAsync();
        Task ResetAsync();
    }
}