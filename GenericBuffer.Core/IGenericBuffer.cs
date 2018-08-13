namespace GenericBuffer.Core
{
    public interface IGenericBuffer<T>
    {
        void ForceRefresh();
        T GetValue();
        void Reset();
    }
}