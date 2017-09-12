using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Dexih.Utils.RealTimeBuffer
{

    /// <summary>
    /// RealTimeBuffer
    /// <para/> This class allows data to be sent from disonnected objects via a data buffer, through a push/pull mechanism.
    /// </summary>
	public class RealTimeBuffer<T>
	{
		private readonly ConcurrentQueue<T> _realtimeQueue;

        private readonly AutoResetEventAsync _popEvent = new AutoResetEventAsync();
        private readonly AutoResetEventAsync _pushEvent = new AutoResetEventAsync();

        /// <summary>
        /// The timeout (in milliseconds) when waiting for a new buffer to arrive.
        /// </summary>
        public int DefaulttimeOutMilliseconds { get; set; } = 5000; //default timeout 5 seconds

        /// <summary>
        /// The maximum buffers to be stored simultaneously
        /// </summary>
        public int MaxBufferCount { get; set; } = 2;

        /// <summary>
        /// The buffer has been cancelled.
        /// </summary>
        public bool IsCancelled { get; set; } = false;

        /// <summary>
        /// The buffer has failed.  See <see cref="Message"/> and <see cref="Exception"/> for details.
        /// </summary>
        public bool IsFailed { get; set; } = false;

        /// <summary>
        /// The buffer has been marked as finished.
        /// </summary>
        public bool IsFinished { get; set; }

        /// <summary>
        /// A message detailing any errors which occurred.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// An exception if an error occurred.
        /// 
        /// </summary>
        public Exception Exception { get; set; }


        private bool _awaitingPush;

        public RealTimeBuffer()
        {
        }


        public RealTimeBuffer(int maxBufferCount)
		{
			_realtimeQueue = new ConcurrentQueue<T>();
            MaxBufferCount = maxBufferCount;
		}

        public RealTimeBuffer(int maxBufferCount, int defaultTimeOutMilliseconds)
        {
            _realtimeQueue = new ConcurrentQueue<T>();
            MaxBufferCount = maxBufferCount;
            DefaulttimeOutMilliseconds = defaultTimeOutMilliseconds;
        }

        /// <summary>
        /// Add a new buffer into the cache.  If the buffer queue is great than the <see cref="MaxBufferCount"/> the function will wait until a buffer has been cleared before acepting the new buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public Task Push(T buffer)
        {
            return Push(buffer, false, CancellationToken.None, DefaulttimeOutMilliseconds);
        }

        /// <summary>
        /// <see cref="Push(T)"/> and marks this buffer as the final.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="isFinalBuffer"></param>
        /// <returns></returns>
        public Task Push(T buffer, bool isFinalBuffer)
        {
            return Push(buffer, isFinalBuffer, CancellationToken.None, DefaulttimeOutMilliseconds);
        }

        /// <summary>
        /// <see cref="Push(T)"/>
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task Push(T buffer, CancellationToken cancellationToken)
        {
            return Push(buffer, false, cancellationToken, DefaulttimeOutMilliseconds);
        }

        /// <summary>
        /// <see cref="Push(T)"/>
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="isFinalBuffer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task Push(T buffer, bool isFinalBuffer, CancellationToken cancellationToken)
        {
            return Push(buffer, isFinalBuffer, cancellationToken, DefaulttimeOutMilliseconds);
        }

	    /// <summary>
	    /// <see cref="Push(T)"/>
	    /// </summary>
	    /// <param name="buffer"></param>
	    /// <param name="isFinalBuffer"></param>
	    /// <param name="cancellationToken"></param>
	    /// <param name="timeOutMilliseconds"></param>
	    /// <returns></returns>
	    public async Task Push(T buffer, bool isFinalBuffer, CancellationToken cancellationToken, int timeOutMilliseconds)
        {
            try
            {
                if(IsFinished)
                {
                    throw new RealTimeBufferFinishedException("The push operation was attempted after the queue was marked as finished.");
                }

                // if the buffer is full, wait until something is popped
                while (_realtimeQueue.Count >= MaxBufferCount)
                {
                    if(_awaitingPush)
                    {
                        throw new RealTimeBufferPushExceededException("The push operation failed, as the buffer is at max capacity, and another task is waiting to push a buffer.");
                    }

                    _awaitingPush = true;

                    var popEvent = _popEvent.WaitAsync();
                    var timeoutEvent = Task.Delay(timeOutMilliseconds, cancellationToken);

                    var completedTask = await Task.WhenAny(popEvent, timeoutEvent);

                    _awaitingPush = false;

                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw new RealTimeBufferCancelledException("The push operation was cancelled");
                    }

                    if (completedTask == timeoutEvent)
                    {
                        throw new RealTimeBufferTimeOutException($"The push operation timed out after {timeOutMilliseconds} milliseconds.");
                    }
                }

                _realtimeQueue.Enqueue(buffer);
                IsFinished = isFinalBuffer;

                _pushEvent.Set();
            }
            catch (Exception ex)
            when(!(ex is RealTimeBufferCancelledException || ex is RealTimeBufferFinishedException || ex is RealTimeBufferTimeOutException || ex is RealTimeBufferPushExceededException))
            {
                throw new RealTimeBufferException("The push operation failed.  See inner exception for details.", ex);
            }
        }

        /// <summary>
        /// Mark the buffer as having an error.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public void SetError(string message, Exception exception)
        {
            IsFailed = true;
            IsFinished = true;
            Message = message;

            if (exception == null)
            {
                Exception = new RealTimeBufferException(message);
            }
            else
            {
                Exception = exception;
            }

            if (_pushEvent != null) _pushEvent.Set();
        }

        /// <summary>
        /// Retrieve a buffer from the queue.  If the buffer queue is empty, this will wait until a new buffer is available, or a timeout of <see cref="DefaulttimeOutMilliseconds"/> has occurred.
        /// </summary>
        /// <returns></returns>
        public Task<RealTimeBufferPackage<T>> Pop()
        {
            return Pop(CancellationToken.None, DefaulttimeOutMilliseconds);
        }

        /// <summary>
        /// <see cref="Pop"/>
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<RealTimeBufferPackage<T>> Pop(CancellationToken cancellationToken)
        {
            return Pop(cancellationToken, DefaulttimeOutMilliseconds);
        }

        /// <summary>
        /// <see cref="Pop"/>
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="timeOutMilliseconds"></param>
        /// <returns></returns>
        public async Task<RealTimeBufferPackage<T>> Pop(CancellationToken cancellationToken, int timeOutMilliseconds)
        {
            try
            {
                while (_realtimeQueue.Count == 0)
                {
                    if (IsFinished)
                    {
                        return new RealTimeBufferPackage<T>(ERealTimeBufferStatus.Complete);
                    }

                    var pushEvent = _pushEvent.WaitAsync();

                    if(IsFailed)
                    {
                        throw Exception;
                    }

                    var timeoutEvent = Task.Delay(timeOutMilliseconds, cancellationToken);

                    var completedTask = await Task.WhenAny(pushEvent, timeoutEvent);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return new RealTimeBufferPackage<T>(ERealTimeBufferStatus.Cancalled);
                    }

                    if (completedTask == timeoutEvent)
                    {
                        throw new RealTimeBufferTimeOutException($"The pull operation timed out after {timeOutMilliseconds} milliseconds.");
                    }
                }

                while (true)
                {
                    if (IsFailed)
                    {
                        throw Exception;
                    }

                    var canDequeue = _realtimeQueue.TryDequeue(out T result);
                    if (canDequeue)
                    {
                        ERealTimeBufferStatus status;
                        if (IsFinished && _realtimeQueue.Count == 0)
                        {
                            status = ERealTimeBufferStatus.Complete;
                        }
                        else
                        {
                            status = ERealTimeBufferStatus.NotComplete;
                        }
                        var package = new RealTimeBufferPackage<T>(result, status);
                        _popEvent.Set();
                        return package;
                    }
                    await Task.Delay(100, cancellationToken);
                }
            } catch(Exception ex)
            when (!(ex is RealTimeBufferCancelledException || ex is RealTimeBufferFinishedException || ex is RealTimeBufferTimeOutException))
            {
                throw new RealTimeBufferException("The pull operation failed.  See inner exception for details.", ex);
            }
        }
    }
}
