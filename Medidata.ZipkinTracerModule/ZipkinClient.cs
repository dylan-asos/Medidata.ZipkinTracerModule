﻿using Medidata.ZipkinTracerModule.Collector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medidata.ZipkinTracerModule
{
    public class ZipkinClient
    {
        internal SpanCollector spanCollector;

        public ZipkinClient(IZipkinConfig zipkinConfig)
        {
            if ( String.IsNullOrEmpty(zipkinConfig.ZipkinServerName)
                || String.IsNullOrEmpty(zipkinConfig.ZipkinServerPort) )
            {
                throw new ArgumentNullException("zipkinConfig value is null");
            }
            
            int port;
            if ( !int.TryParse(zipkinConfig.ZipkinServerPort, out port) )
            {
                throw new ArgumentException("zipkinConfig port is not an int");
            }

            spanCollector = new SpanCollector(new ClientProvider(zipkinConfig.ZipkinServerName, port));
        }
    }
}
