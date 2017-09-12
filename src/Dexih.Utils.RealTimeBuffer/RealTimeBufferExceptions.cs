using System;

namespace Dexih.Utils.RealTimeBuffer
{
    public class RealTimeBufferException : Exception
    {
        public RealTimeBufferException()
        {
        }
        public RealTimeBufferException(string message) : base(message)
        {
        }
        public RealTimeBufferException(string message, Exception innerException): base(message, innerException)
		{
        }
    }

    public class RealTimeBufferTimeOutException: RealTimeBufferException
    {
        public RealTimeBufferTimeOutException(string message) : base(message)
        {
        }
    }

    public class RealTimeBufferCancelledException : RealTimeBufferException
    {
        public RealTimeBufferCancelledException(string message) : base(message)
        {
        }
    }

    public class RealTimeBufferFinishedException : RealTimeBufferException
    {
        public RealTimeBufferFinishedException(string message) : base(message)
        {
        }
    }

    public class RealTimeBufferPushExceededException : RealTimeBufferException
    {
        public RealTimeBufferPushExceededException(string message) : base(message)
        {
        }
    }
}
