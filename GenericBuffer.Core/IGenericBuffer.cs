namespace GenericBuffer.Core
{
    public interface IGenericBuffer<T>
    {
        T ForceRefresh();
        T GetValue();
        void Reset();
    }
}