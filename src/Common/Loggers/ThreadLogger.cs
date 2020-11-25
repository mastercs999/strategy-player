using Common.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Loggers
{
    public class ThreadLogger : Logger
    {
        private readonly BlockingCollection<string> Messages;


        public ThreadLogger(Logger logger)
        {
            Messages = new BlockingCollection<string>();

            // Set up task
            CancellationTokenSource cancellation = new CancellationTokenSource();
            Thread flushThread = new Thread(() =>
            {
                string message = null;
                try
                {
                    while (true)
                    {
                        message = Messages.Take(cancellation.Token);
                        logger.FlushMessage(message);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Flush rest of the messages
                    while (Messages.TryTake(out message))
                        logger.FlushMessage(message);
                }
                catch (Exception ex)
                {
                    throw new LogException("Exception caught in log thread. Last log message was: " + message, ex);
                }
                finally
                {
                    Messages.Dispose();
                }
            });

            // Thread which ends flushThread when no other thread remains
            Thread monitoringThread = new Thread((thr) =>
            {
                Thread parentThread = thr as Thread;
                
                // Wait till main thread ends
                while (parentThread.IsAlive)
                    Thread.Sleep(500);

                // Cancel logging thread
                cancellation.Cancel();
            });

            // Start threads
            flushThread.Start();
            monitoringThread.Start(Thread.CurrentThread);
        }


        public override void FlushMessage(string message)
        {
            Messages.Add(message);
        }


        private class LogException : Exception
        {
            public LogException()
            {
            }

            public LogException(string message) : base(message)
            {
            }

            public LogException(string message, Exception innerException) : base(message, innerException)
            {
            }

            protected LogException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }
    }
}
