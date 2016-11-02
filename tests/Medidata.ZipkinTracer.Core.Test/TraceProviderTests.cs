﻿using System;
using System.Collections.Generic;
using Microsoft.Owin;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ploeh.AutoFixture;
using Rhino.Mocks;

namespace Medidata.ZipkinTracer.Core.Test
{
    [TestClass]
    public class TraceProviderTests
    {
        [TestMethod]
        public void ConstructorWithNullContext()
        {
            // Arrange & Act
            var traceProvider = new TraceProvider(new ZipkinConfig());

            // Assert
            Assert.AreEqual(traceProvider.TraceId, traceProvider.SpanId);
            Assert.AreEqual(string.Empty, traceProvider.ParentSpanId);
            Assert.AreEqual(false, traceProvider.IsSampled);
        }

        [TestMethod]
        public void ConstructorWithContextHavingAllIdValues()
        {
            // Arrange
            var fixture = new Fixture();
            var traceId = Convert.ToString(fixture.Create<long>(), 16);
            var spanId = Convert.ToString(fixture.Create<long>(), 16);
            var parentSpanId = Convert.ToString(fixture.Create<long>(), 16);
            var isSampled = fixture.Create<bool>();

            var context = MockRepository.GenerateStub<IOwinContext>();
            var request = MockRepository.GenerateStub<IOwinRequest>();
            var headers = new HeaderDictionary(new Dictionary<string, string[]>
            {
                { TraceProvider.TraceIdHeaderName, new [] { traceId } },
                { TraceProvider.SpanIdHeaderName, new [] { spanId } },
                { TraceProvider.ParentSpanIdHeaderName, new [] { parentSpanId } },
                { TraceProvider.SampledHeaderName, new [] { isSampled.ToString() } }
            });
            var environment = new Dictionary<string, object>();

            request.Stub(x => x.Headers).Return(headers);
            context.Stub(x => x.Request).Return(request);
            context.Stub(x => x.Environment).Return(environment);

            // Act
            var sut = new TraceProvider(new ZipkinConfig(), context);

            // Assert
            Assert.AreEqual(traceId, sut.TraceId);
            Assert.AreEqual(spanId, sut.SpanId);
            Assert.AreEqual(parentSpanId, sut.ParentSpanId);
            Assert.AreEqual(isSampled, sut.IsSampled);
        }

        [TestMethod]
        public void ConstructorWithContextHavingIdValuesExceptIsSampled()
        {
            // Arrange
            var fixture = new Fixture();
            var traceId = Convert.ToString(fixture.Create<long>(), 16);
            var spanId = Convert.ToString(fixture.Create<long>(), 16);
            var parentSpanId = Convert.ToString(fixture.Create<long>(), 16);

            var context = MockRepository.GenerateStub<IOwinContext>();
            var request = MockRepository.GenerateStub<IOwinRequest>();
            var headers = new HeaderDictionary(new Dictionary<string, string[]>
            {
                { TraceProvider.TraceIdHeaderName, new [] { traceId } },
                { TraceProvider.SpanIdHeaderName, new [] { spanId } },
                { TraceProvider.ParentSpanIdHeaderName, new [] { parentSpanId } },
            });
            var environment = new Dictionary<string, object>();

            request.Stub(x => x.Headers).Return(headers);
            context.Stub(x => x.Request).Return(request);
            context.Stub(x => x.Environment).Return(environment);

            var expectedIsSampled = fixture.Create<bool>();
            var sampleFilter = MockRepository.GenerateStub<IZipkinConfig>();
            sampleFilter.Expect(x => x.ShouldBeSampled(context, null)).Return(expectedIsSampled);

            // Act
            var sut = new TraceProvider(sampleFilter, context);

            // Assert
            Assert.AreEqual(traceId, sut.TraceId);
            Assert.AreEqual(spanId, sut.SpanId);
            Assert.AreEqual(parentSpanId, sut.ParentSpanId);
            Assert.AreEqual(expectedIsSampled, sut.IsSampled);
        }

        [TestMethod]
        public void ConstructorWithContextHavingInvalidIdValues()
        {
            // Arrange
            var fixture = new Fixture();
            var traceId = fixture.Create<string>();
            var spanId = fixture.Create<string>();
            var parentSpanId = fixture.Create<string>();
            var isSampled = fixture.Create<string>();

            var context = MockRepository.GenerateStub<IOwinContext>();
            var request = MockRepository.GenerateStub<IOwinRequest>();
            var headers = new HeaderDictionary(new Dictionary<string, string[]>
            {
                { TraceProvider.TraceIdHeaderName, new [] { traceId } },
                { TraceProvider.SpanIdHeaderName, new [] { spanId } },
                { TraceProvider.ParentSpanIdHeaderName, new [] { parentSpanId } },
                { TraceProvider.SampledHeaderName, new [] { isSampled } }
            });
            var environment = new Dictionary<string, object>();

            request.Stub(x => x.Headers).Return(headers);
            context.Stub(x => x.Request).Return(request);
            context.Stub(x => x.Environment).Return(environment);

            var expectedIsSampled = fixture.Create<bool>();
            var sampleFilter = MockRepository.GenerateStub<IZipkinConfig>();
            sampleFilter.Expect(x => x.ShouldBeSampled(context, isSampled)).Return(expectedIsSampled);

            // Act
            var sut = new TraceProvider(sampleFilter, context);

            // Assert
            Assert.AreNotEqual(traceId, sut.TraceId);
            Assert.AreEqual(sut.TraceId, sut.SpanId);
            Assert.AreEqual(string.Empty, sut.ParentSpanId);
            Assert.AreEqual(expectedIsSampled, sut.IsSampled);
        }

        [TestMethod]
        public void ConstructorWithHavingTraceProviderInContext()
        {
            // Arrange
            var context = MockRepository.GenerateStub<IOwinContext>();
            var providerInContext = MockRepository.GenerateStub<ITraceProvider>();
            var environment = new Dictionary<string, object>
            {
                { "Medidata.ZipkinTracer.Core.TraceProvider", providerInContext }
            };
            context.Stub(x => x.Environment).Return(environment);

            // Act
            var sut = new TraceProvider(new ZipkinConfig(), context);

            // Assert
            Assert.AreEqual(providerInContext.TraceId, sut.TraceId);
            Assert.AreEqual(providerInContext.SpanId, sut.SpanId);
            Assert.AreEqual(providerInContext.ParentSpanId, sut.ParentSpanId);
            Assert.AreEqual(providerInContext.IsSampled, sut.IsSampled);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ConstructorWithContextHavingSameSpanAndParentSpan()
        {
            // Arrange
            var fixture = new Fixture();
            var traceId = Convert.ToString(fixture.Create<long>(), 16);
            var spanId = Convert.ToString(fixture.Create<long>(), 16);
            var parentSpanId = spanId;

            var context = MockRepository.GenerateStub<IOwinContext>();
            var request = MockRepository.GenerateStub<IOwinRequest>();
            var headers = new HeaderDictionary(new Dictionary<string, string[]>
            {
                { TraceProvider.TraceIdHeaderName, new [] { traceId } },
                { TraceProvider.SpanIdHeaderName, new [] { spanId } },
                { TraceProvider.ParentSpanIdHeaderName, new [] { parentSpanId } }
            });
            var environment = new Dictionary<string, object>();

            request.Stub(x => x.Headers).Return(headers);
            context.Stub(x => x.Request).Return(request);
            context.Stub(x => x.Environment).Return(environment);

            // Act
            new TraceProvider(new ZipkinConfig(), context);
        }

        [TestMethod]
        public void GetNext()
        {
            // Arrange
            var fixture = new Fixture();
            var traceId = Convert.ToString(fixture.Create<long>(), 16);
            var spanId = Convert.ToString(fixture.Create<long>(), 16);
            var parentSpanId = Convert.ToString(fixture.Create<long>(), 16);
            var isSampled = fixture.Create<bool>();

            var context = MockRepository.GenerateStub<IOwinContext>();
            var request = MockRepository.GenerateStub<IOwinRequest>();
            var headers = new HeaderDictionary(new Dictionary<string, string[]>
            {
                { TraceProvider.TraceIdHeaderName, new [] { traceId } },
                { TraceProvider.SpanIdHeaderName, new [] { spanId } },
                { TraceProvider.ParentSpanIdHeaderName, new [] { parentSpanId } },
                { TraceProvider.SampledHeaderName, new [] { isSampled.ToString() } }
            });
            var environment = new Dictionary<string, object>();

            request.Stub(x => x.Headers).Return(headers);
            context.Stub(x => x.Request).Return(request);
            context.Stub(x => x.Environment).Return(environment);

            var sut = new TraceProvider(new ZipkinConfig(), context);

            // Act
            var nextTraceProvider = sut.GetNext();

            // Assert
            Assert.AreEqual(sut.TraceId, nextTraceProvider.TraceId);
            Assert.AreEqual(sut.SpanId, nextTraceProvider.ParentSpanId);
            Assert.AreEqual(sut.IsSampled, nextTraceProvider.IsSampled);
        }

        [TestMethod]
        public void IdsWithLessThan32Characters()
        {
            // Arrange
            var fixture = new Fixture();
            var traceId = "48485a3953bb612";
            var spanId = "48485a3953bb613";
            var parentSpanId = "48485a3953bb614";
            var isSampled = fixture.Create<bool>();

            var context = GenerateContext(traceId, spanId, parentSpanId, isSampled);

            // Act
            var sut = new TraceProvider(new ZipkinConfig(), context);

            // Assert
            Assert.AreEqual(traceId, sut.TraceId);
            Assert.AreEqual(spanId, sut.SpanId);
            Assert.AreEqual(parentSpanId, sut.ParentSpanId);
        }

        [TestMethod]
        public void IdsWith32Characters()
        {
            // Arrange
            var fixture = new Fixture();
            var traceId = "48485a3953bb6124";
            var spanId = "48485a3953bb6125";
            var parentSpanId = "48485a3953bb6126";
            var isSampled = fixture.Create<bool>();

            var context = GenerateContext(traceId, spanId, parentSpanId, isSampled);

            // Act
            var sut = new TraceProvider(new ZipkinConfig(), context);

            // Assert
            Assert.AreEqual(traceId, sut.TraceId);
            Assert.AreEqual(spanId, sut.SpanId);
            Assert.AreEqual(parentSpanId, sut.ParentSpanId);
        }

        [TestMethod]
        public void IdsWith64Characters()
        {
            // Arrange
            var fixture = new Fixture();
            var traceIdLower16Chars = "48485a3953bb6124";
            var traceId = "18485a3953bb6124" + traceIdLower16Chars;
            var spanIdLower16Chars = "48485a3953bb6125";
            var spanId = "28485a3953bb6124" + spanIdLower16Chars;
            var parentSpanIdLower16Chars = "48485a3953bb6126";
            var parentSpanId = "38485a3953bb6124" + parentSpanIdLower16Chars;
            var isSampled = fixture.Create<bool>();

            var context = GenerateContext(traceId, spanId, parentSpanId, isSampled);

            // Act
            var sut = new TraceProvider(new ZipkinConfig(), context);

            // Assert
            Assert.AreEqual(traceIdLower16Chars, sut.TraceId);
            Assert.AreEqual(spanIdLower16Chars, sut.SpanId);
            Assert.AreEqual(parentSpanIdLower16Chars, sut.ParentSpanId);
        }

        private IOwinContext GenerateContext(string traceId, string spanId, string parentSpanId, bool isSampled)
        {
            var context = MockRepository.GenerateStub<IOwinContext>();
            var request = MockRepository.GenerateStub<IOwinRequest>();
            var headers = new HeaderDictionary(new Dictionary<string, string[]>
            {
                { TraceProvider.TraceIdHeaderName, new [] { traceId } },
                { TraceProvider.SpanIdHeaderName, new [] { spanId } },
                { TraceProvider.ParentSpanIdHeaderName, new [] { parentSpanId } },
                { TraceProvider.SampledHeaderName, new [] { isSampled.ToString() } }
            });
            var environment = new Dictionary<string, object>();

            request.Stub(x => x.Headers).Return(headers);
            context.Stub(x => x.Request).Return(request);
            context.Stub(x => x.Environment).Return(environment);

            return context;
        }
    }
}
