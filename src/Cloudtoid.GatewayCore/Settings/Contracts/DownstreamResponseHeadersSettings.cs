﻿using System;
using System.Collections.Generic;
using System.Linq;
using Cloudtoid.GatewayCore.Headers;

namespace Cloudtoid.GatewayCore.Settings
{
    public sealed class DownstreamResponseHeadersSettings
    {
        internal DownstreamResponseHeadersSettings(
            bool discardInboundHeaders,
            bool discardEmpty,
            bool discardUnderscore,
            bool addServer,
            bool addVia,
            IReadOnlyDictionary<string, CookieSettings> cookies,
            IReadOnlyDictionary<string, HeaderSettings> appends,
            IReadOnlyDictionary<string, HeaderSettings> overrides,
            ISet<string> discards)
        {
            DiscardInboundHeaders = discardInboundHeaders;
            DiscardEmpty = discardEmpty;
            DiscardUnderscore = discardUnderscore;
            AddServer = addServer;
            AddVia = addVia;
            Cookies = cookies;
            Appends = appends;
            Overrides = overrides;
            Discards = discards;

            DoNotTransferHeaders = HeaderTypes
                .DoNotTransferResponseHeaders
                .Concat(overrides.Keys)
                .Concat(discards)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// This is a list of headers that should not be passed on to the downstream client as they are.
        /// They can be transferred, but only when they are transformed. This set consists of
        /// <list type="bullet">
        /// <item>Headers generated by an instance of this proxy</item>
        /// <item><a href="https://datatracker.ietf.org/doc/html/rfc7540#section-8.1.2.4">HTTP/2 pseudo headers</a>.</item>
        /// <item>Standard hop-by-hop headers. See <see cref="HeaderTypes.StandardHopByHopeHeaders"/>.</item>
        /// <item><see cref="Overrides"/> headers.</item>
        /// <item><see cref="Discards"/> headers.</item>
        /// </list>
        /// </summary>
        public ISet<string> DoNotTransferHeaders { get; }

        public bool DiscardInboundHeaders { get; }

        public bool DiscardEmpty { get; }

        public bool DiscardUnderscore { get; }

        public bool AddServer { get; }

        public bool AddVia { get; }

        public IReadOnlyDictionary<string, CookieSettings> Cookies { get; }

        public IReadOnlyDictionary<string, HeaderSettings> Appends { get; }

        public IReadOnlyDictionary<string, HeaderSettings> Overrides { get; }

        public ISet<string> Discards { get; }
    }
}