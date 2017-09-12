namespace Dexih.Utils.RealTimeBuffer
{
    public enum ERealTimeBufferStatus
    {
        NotComplete,
        Complete,
        Cancalled,
        TimeOut,
        Error
    }

    public class RealTimeBufferPackage<T>
    {
        public RealTimeBufferPackage()
        {

        }

        public RealTimeBufferPackage(ERealTimeBufferStatus status)
        {
            Status = status;
        }

        public RealTimeBufferPackage(T package, ERealTimeBufferStatus status)
        {
            Package = package;
            Status = status;
        }

        public T Package { get; set; }
        public ERealTimeBufferStatus Status { get; set; }
    }
}
