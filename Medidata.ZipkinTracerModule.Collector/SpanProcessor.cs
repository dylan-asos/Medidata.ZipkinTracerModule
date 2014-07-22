﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Thrift;
using Thrift.Protocol;
using Thrift.Transport;

namespace Medidata.ZipkinTracerModule.Collector
{
    public class SpanProcessor
    {
        //wait time to poll for dequeuing
        private const int WAIT_INTERVAL_TO_DEQUEUE_MS = 1000;

        //send contents of queue if it has been empty for 2 polls
        internal const int MAX_SUBSEQUENT_EMPTY_QUEUE = 2;

        //# of spans we submit to scribe in one go
        internal const int MAX_BATCH_SIZE = 20;

        private TBinaryProtocol.Factory protocolFactory;
        private BlockingCollection<Span> spanQueue;
        private IClientProvider clientProvider;

        internal List<LogEntry> logEntries;
        internal CancellationTokenSource cancellationTokenSource;
        internal SpanProcessorTaskFactory spanProcessorTaskFactory;
        internal int subsequentEmptyQueueCount;
        internal int retries;

        public SpanProcessor(BlockingCollection<Span> spanQueue, IClientProvider clientProvider)
        {
            if ( spanQueue == null) 
            {
                throw new ArgumentNullException("spanQueue is null");
            }

            if ( clientProvider == null) 
            {
                throw new ArgumentNullException("clientProvider is null");
            }

            this.spanQueue = spanQueue;
            this.clientProvider = clientProvider;
            logEntries = new List<LogEntry>(MAX_BATCH_SIZE);
            protocolFactory = new TBinaryProtocol.Factory();
            spanProcessorTaskFactory = new SpanProcessorTaskFactory();
        }

        public virtual void Stop()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
        }

        public virtual void Start()
        {
            cancellationTokenSource = new CancellationTokenSource();
            spanProcessorTaskFactory.CreateAndStart(() => LogSubmittedSpansWrapper(), cancellationTokenSource);
        }

        internal void LogSubmittedSpansWrapper()
        {
            while(!cancellationTokenSource.Token.IsCancellationRequested )
            {
                LogSubmittedSpans();
            } 
        }

        internal void LogSubmittedSpans()
        {
            Span span;
            spanQueue.TryTake(out span, WAIT_INTERVAL_TO_DEQUEUE_MS);
            if (span != null)
            {
                logEntries.Add(Create(span));
                subsequentEmptyQueueCount = 0;
            }
            else
            {
                subsequentEmptyQueueCount++;
            }

            if (logEntries.Count() >= MAX_BATCH_SIZE
                || logEntries.Any() && cancellationTokenSource.Token.IsCancellationRequested
                || logEntries.Any() && subsequentEmptyQueueCount > MAX_SUBSEQUENT_EMPTY_QUEUE)
            {
                Log(clientProvider, logEntries);
                logEntries.Clear();
                subsequentEmptyQueueCount = 0;
            }
        }

        internal void Log(IClientProvider client, List<LogEntry> logEntries)
        {
            try
            {
                clientProvider.Log(logEntries);
                retries = 0;
            }
            catch (TException tEx)
            {
                if ( retries < 3 )
                {
                    retries++;
                    Log(client, logEntries);
                }
                else
                {
                    throw tEx;
                }
            }
        }

        private LogEntry Create(Span span)
        {
            var spanAsString = Convert.ToBase64String(ConvertSpanToBytes(span));
            return new LogEntry()
            {
                Category = "zipkin",
                Message = spanAsString
            };
        }

        private byte[] ConvertSpanToBytes(Span span)
        {
            var buf = new MemoryStream();
            TProtocol protocol = protocolFactory.GetProtocol(new TStreamTransport(buf, buf));
            span.Write(protocol);
            return buf.ToArray();
        }
    }
}
