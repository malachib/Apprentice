#if !NETCORE
using Castle.Core.Logging;
#else
using Microsoft.Framework.Logging;
#endif
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Fact.Net.Sockets
{
    /// <summary>
    /// Helper class to create and manage worker thread which continually
    /// pulls data over TCP/IP and pushes it through a processer method
    /// </summary>
    public class SocketReceiveWorker : IDisposable
    {
        readonly ILogger logger; // = LogManager.GetCurrentClassLogger();

        IPEndPoint ipEndPoint;
        int bufferSize;
        bool async;

        public IPEndPoint IPEndPoint { get { return ipEndPoint; } }

        Task worker;

        /// <summary>
        /// Initialize SocketReceiveWorker
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="bufferSize"></param>
        public SocketReceiveWorker(IPEndPoint ipAddress, int bufferSize)
        {
            this.bufferSize = bufferSize;
            this.ipEndPoint = ipAddress;
        }

        public void Start([Optional, DefaultParameterValue(false)] bool async)
        {
            if (async)
                throw new InvalidOperationException("Async mode not yet supported");

            this.async = async;
            worker = Task.Factory.StartNew(workerMethod);
        }

        /// <summary>
        /// </summary>
        /// <param name="wait">
        /// If wait is true, will block up to 10 seconds for completion, then throws an exception
        /// If wait is false, triggers a shutdown and immediately returns.  Use the ShutdownOccurred event
        /// </param>
        /// <remarks></remarks>
        public void Shutdown(bool wait)
        {
            isActive = false;

            if (wait && !worker.Wait(10000))
                throw new TimeoutException("Worker did not finish up cleanly");
        }

        bool isActive = true;

        /// <summary>
        /// If an exception occurs, this will fire with the exception info.  2nd parameter is
        /// when the worker thread will start over again to retry
        /// </summary>
        public event Action<Exception, DateTime> ExceptionOccured;

        /// <summary>
        /// Fired when we actually get real connection to socket
        /// </summary>
        public event Action Connected;

        /// <summary>
        /// Fired when manual shutdown has completed successfuly
        /// </summary>
        public event Action ShutdownCompleted;

        void beginReceiveResponder(IAsyncResult result)
        {
            socket.EndReceive(result);

            //socket.BeginReceive(holderBuffer, 0, bufferSize, SocketFlags.None, beginReceiveResponder, null);
        }

        // right now this instance variable only used for async mode, which itself is dormant
        Socket socket;

        void workerMethod()
        {
            logger.Debug("workerMethod entry (" + ipEndPoint.Address + ":" + ipEndPoint.Port + ")");

            try
            {
                //var endpoint = new IPEndPoint(ipAddress);
                var endpoint = ipEndPoint;
                var client = new TcpClient();
                client.Connect(endpoint);
                var holderBuffer = new byte[bufferSize];
                var socket = client.Client;

                if (Connected != null)
                    Connected();

                if (async)
                {
                    // POC code, inactive
                    var args = new SocketAsyncEventArgs();
                    socket.ReceiveAsync(args);

                    this.socket = socket;
                    socket.BeginReceive(holderBuffer, 0, bufferSize, SocketFlags.None, beginReceiveResponder, null);
                }
                else
                {
                    logger.Debug("workerMethod async = false");

                    while (isActive)
                    {
                        // FIX: Unclear whether receive waits to fill out entire holderBuffer
                        // since we wisely conservatively only used 1-byte buffer until we
                        // were able to test more, it's currently a moot point - clearly
                        // we'll want this resolved moving forward

                        // socket read/block for one character [inefficient code, really one wants to read larger buffers]
                        // also inefficient for scaling out, should use socket.ReceiveAsync so that we aren't holding up
                        // a whole thread for blocking 
                        socket.Receive(holderBuffer);

                        // TODO: re-enabled this once we up the buffer size from 1 byte
                        //logger.Trace("workerMethod received " + holderBuffer.Length + " bytes");

                        if (BytesReceived != null)
                            BytesReceived(holderBuffer);
                    }
                }

#if !NETCORE
                socket.Close();
#endif
                socket.Dispose();

                if (ShutdownCompleted != null)
                    ShutdownCompleted();
            }
            catch (Exception e)
            {
                logger.Debug("workerMethod exception: " + e.Message, e);

                var retryTimeout = TimeSpan.FromSeconds(30);

                // restart worker in 30 seconds to try again, whatever the exception was
                retryTimer = new Timer(state => worker = Task.Factory.StartNew(workerMethod),
                    null, TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(-1));

                if (ExceptionOccured != null)
                    ExceptionOccured(e, DateTime.Now.Add(retryTimeout));
            }

            logger.Debug("workerMethod exit");
        }

        /// <summary>
        /// We only use this to hold on to reference so that timer can start even when worker has finished
        /// </summary>
        Timer retryTimer;

        public event Action<byte[]> BytesReceived;

        public void Dispose()
        {
            //Shutdown(true);
        }
    }
}
