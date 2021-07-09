<a href="https://github.com/cloudtoid"><img src="https://raw.githubusercontent.com/cloudtoid/assets/master/logos/cloudtoid-black-red.png" width="100"></a>

# Gateway Core

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)][License]

A modern API Gateway and Reverse Proxy library for .NET Core and beyond.

## V1 - TODOs

- Add more functional tests:
  - "upstreamRequest" -> "httpVersion" add a functional test that tests this setting.
  - "upstreamRequest" -> "sender". Not sure how to test all of these. Maybe check the HTTP Client to see if the values are set correctly?
  - Add tests for all HTTP methods (POST, DELETE, etc)
  - Fix HTTPS so it also works on Mac and Linux!
  - HttpClientName
  - Routing
  - Failed HTTP requests with and without content/body
  - Timeout (both at httpClient to upstream and inside of proxy)
  - Auto redirects
  - ProxyException and exception handling
  - When no route is found, do not return 200
  - Authentication
  - Run nginx side by side and ensure all headers and other properties match.
- Write documentation
  - Add a table of all config values with a description and links/anchors from the rest of the doc.
  - Mention the options-schema.json and how it can be used.
  - URL Pattern Matching
  - How to run teh test server
- Add unit tests for trailing TrailingHeaderSetter
- Enable github build actions
- Publish NuGet
- Add multiple Sample Projects
  - Basic/simple
  - Multiple kestrel servers in a single project
  - Advanced extensibility
  - Use of options
  - Named HttpClient Management from the outside
- Side by side tests with nginx
- Kestrel should only listen on relevant exact routes defined in Options
- Benchmark
- Increase code coverage
- Add support for modification of the Path attribute in the Set-Cookie header
- Publish options-schema.json on a website so it can be referenced in JSON GatewayOptions files
- Build and test on Linux & MacOS
- Right now, some upstream HTTP errors are simply converted to 502/BadGateway. Look into the options here.
- Implement the proxy OPTIONS section: [here](https://datatracker.ietf.org/doc/html/rfc7230#section-5.3.4)
- Add host filtering to the proxy. Something like this but not a middleware: https://github.com/dotnet/aspnetcore/blob/6427d9cc718f8093c506b62b6fd12544411b477f/src/Middleware/HostFiltering/src/HostFilteringMiddleware.cs
- HTTP2 Client Push
- Wire up more of SocketsHttpHandler properties in SettingsProvider.
- Add tests for content headers both for requests and also responses. Right now, we are only using GET requests that don't really have content length.
- Default timeout for upstream requests is set to 100 seconds. Should these be changed?

## Future version

- Protocols
  - Add support for web-sockets
  - Test gRPC
  - Test SignalR
- Features
  - Request Aggregation
  - Caching
  - Retry policies / QoS
  - Load Balancing
- Platforms
  - Kubernetes
  - Service Fabric
- Fixes
  - Search for `TODO`s and remove the ones that are fixed.

## Getting Started

> TODO

## Default proxy behavior

The default HTTP protocol version for upstream requests is `HTTP/2`. This can be changed as shown below:

```json
"routes": {
  "/api/": {
    "proxy": {
      "to": "http://upstream/v1/",
      "upstreamRequest": {
          "httpVersion": "1.1"
```

### Request headers

- The values of [`Host`][HostHeader] and `:authority` headers are redefined as the name and port of the upstream server. This can be changed as shown below:

  ```json
  "routes": {
    "/api/": {
      "proxy": {
        "to": "http://upstream/v1/",
        "upstreamRequest": {
            "headers": {
              "overrides": {
                "Host": [ "$host" ]
  ```

- A [`Via`][ViaHeader] header is added. See [this](#via-header) for more information.
- A [`Forwarded`][ForwardedHeader] header is added/appended/redefined. See [this](#forwarded-category-of-headers) for more information.
- The following headers are discarded:
  - Headers with an empty value
  - Headers with an underscore (`_`) in their name
  - GatewayCore headers: `x-gwcore-external-address`, `x-gwcore-proxy-name`
  - HTTP/2 [Pseudo Headers][RequestPseudoHeaders]: `:method`, `:authority`, `scheme`, and `:path`
  - Standard hop-by-hop headers: `Keep-Alive`, `Transfer-Encoding`, `TE`, `Connection`, `Trailer`, `Upgrade`, `Proxy-Authorization`, and `Proxy-Authentication`.
  - Non-standard hop-by-hop headers defined by the [`Connection`][ConnectionHeader] header.
- All other headers from the downstream are typically passed as they are.

### Response headers

- The following headers are discarded:
  - Headers with an empty value
  - Headers with an underscore (`_`) in their name
  - Standard headers: [`Via`][ViaHeader] and [`Server`][ServerHeader]
  - GatewayCore headers: `x-gwcore-external-address`, `x-gwcore-proxy-name`
  - HTTP/2 [Pseudo Header][ResponsePseudoHeaders]: `:status`
  - Standard hop-by-hop headers: `Keep-Alive`, `Transfer-Encoding`, `TE`, `Connection`, `Trailer`, `Upgrade`, `Proxy-Authorization`, and `Proxy-Authentication`.
  - Non-standard hop-by-hop headers defined by the [`Connection`][ConnectionHeader] header.
- All other headers from the upstream are typically passed as they are.

## Route tracking

There are four types of route tracking: two that offer information on proxies, and two that carry client details.

### Via header

A proxy typically uses the [`Via`][ViaHeader] header for tracking message forwards, avoiding request loops, and identifying the protocol capabilities of senders along the request/response chain.

According to [RFC7230][ViaHeaderSpec], a proxy must send an appropriate [`Via`][ViaHeader] header field in each message that it forwards. An HTTP-to-HTTP gateway must send an appropriate [`Via`][ViaHeader] header field in each inbound request message and may send a [`Via`][ViaHeader] header field in forwarded response messages.

This header is a comma-separated list of proxies along the message chain with the closest proxy to the sender being the left-most value.

The value added by GatewayCore includes the pseudonym of the proxy. This default name is `gwcore` but can be customized with `proxyName`:

```json
"routes": {
  "/api/": {
    "proxy": {
      "to": "http://upstream/v1/",
      "proxyName": "my-proxy-name"
```

GatewayCore appends one of the following values:

| Sender's protocol | Value | Example |
|:--- |:--- |:-- |
| `HTTP/1.0` | `1.0 \<proxy-name>` | `1.0 gwcore` |
| `HTTP/1.1` | `1.1 \<proxy-name>` | `1.1 gwcore` |
| `HTTP/2.0` | `2.0 \<proxy-name>` | `2.0 gwcore` |
| `HTTP/X.Y` | `X.Y \<proxy-name>` | `X.Y gwcore` |
| `<protocol>\<version>` | `<protocol>\<version> <proxy-name>` |  

> As illustrated above, GatewayCore omits the protocol for HTTP requests and responses.

The [`Via`][ViaHeader] header is included by default in requests to proxied servers but not in forwarded responses. You can change this behavior using `skipVia` and `addVia` as shown below:

```json
"routes": {
  "/api/": {
    "proxy": {
      "to": "http://upstream/v1/",
      "upstreamRequest": {
        "headers": {
          "skipVia": true,
          }
        }
      },
      "downstreamResponse": {
        "headers": {
          "addVia": true
```

### Forwarded category of headers

The [`Forwarded`][ForwardedHeader] class of headers contains information from the client-facing side of proxy servers that is altered or lost when a proxy is involved in the path of the request. This information is passed on using one of these techniques:

1. The [[`Forwarded`][ForwardedHeader]][ForwardedHeader] header is what GatewayCore uses by default. This is the standardized version of the header.
1. The [`X-Forwarded-For`][XForwardedForHeader], [`X-Forwarded-Host`][XForwardedHostHeader], and [`X-Forwarded-Proto`][XForwardedProtoHeader] headers which are considered the [de-facto standard][DefactoWiki] versions of the [[`Forwarded`][ForwardedHeader]][ForwardedHeader] header.

The information included in these headers typically consists of the IP address of the client, the IP address where the request came into the proxy server, the [`Host`][HostHeader] request header field as received by the proxy, and the protocol used by the request (typically "http" or "https").

> IP V6 addresses are quoted and enclosed in square brackets.

GatewayCore uses the [`Forwarded`][ForwardedHeader] header by default and replaces all inbound `X-Forwarded-*` headers. You can enable `useXForwarded` to reverse this behavior and prefer `X-Forwarded-*` headers instead:

```json
"routes": {
  "/api/": {
    "proxy": {
      "to": "http://upstream/v1/",
      "upstreamRequest": {
        "headers": {
          "useXForwarded": true
```

It is also possible to omit these headers on outbound requests by using `skipForwarded`:

```json
"routes": {
  "/api/": {
    "proxy": {
      "to": "http://upstream/v1/",
      "upstreamRequest": {
        "headers": {
          "skipForwarded": true
```

### Server header

The [`Server`][ServerHeader] HTTP response header describes the software used by the upstream server that handled the request and generated a response.

GatewayCore discards inbound [`Server`][ServerHeader] headers and does not include a [`Server`][ServerHeader] header on its outbound responses to clients. This default behavior can be changed to include a [`Server`][ServerHeader] header with `gwcore` as its value:

```json
"routes": {
  "/api/": {
    "proxy": {
      "to": "http://upstream/v1/",
      "downstreamResponse": {
        "headers": {
          "addServer": true
```

> [Security through obscurity][Obscurity]: A [`Server`][ServerHeader] header can reveal information that might make it easier for attackers to exploit known security holes. It is recommended not to include this header.
> An upstream specified [`Server`][ServerHeader] header is always ignored.

### External address header

GatewayCore can forward the IP address of an immediate downstream client. This IP address is sent using the custom `x-gwcore-external-address` header and can be enabled with the `addExternalAddress` option:

```json
"routes": {
  "/api/": {
    "proxy": {
      "to": "http://upstream/v1/",
      "upstreamRequest": {
        "headers": {
          "addExternalAddress": true
```

## Cookie handling

The [`Set-Cookie`][SetCookieHeader] HTTP response header is used to send cookies from the server to the client so that the client can send them back to the server later. To send multiple cookies, multiple [`Set-Cookie`][SetCookieHeader] headers can be sent in the same response.

This header can include attributes such as:

- `Expires`: The maximum lifetime of the cookie as an HTTP-date timestamp.
- `Max-Age`: Number of seconds until the cookie expires. A zero or negative number will expire the cookie immediately. `Max-Age` has precedence over `Expires`.
- `Domain`: Host to which the cookie will be sent on subsequent calls.
- `Path`: A path that must exist in the requested URL, or the client won't send the cookie header on subsequent requests.
- `Secure`: A secure cookie is only sent to the server when a request is made with the `https:` scheme.
- `HttpOnly`: Limits the scope of the cookie to HTTP
requests. In particular, the attribute instructs the client to omit the cookie when providing access to cookies via "non-HTTP" APIs such as JavaScript's `Document.cookie` API.
- `SameSite`: Helps with potential cross-site security issues.
  - If set to `none`, cookies are sent with both cross-site requests and same-site requests.
  - If set to `strict`, cookies are sent only for same-site requests.
  - If set to `lax`, same-site cookies are withheld on cross-site subrequests, such as calls to load images or frames, but will be sent when a user navigates to the URL from an external site; for example, by following a link. From Chrome version 80 and Edge 86, the default is `lax` and not `none`.

A reverse proxy such as GatewayCore often modifies the domain, path, and scheme (http/https) of proxied requests and responses. Therefore, it might be necessary to update the `Domain`, `Path`, `SameSite`, `Secure`, and `HttpOnly` attributes as demonstrated below:

```json
"routes": {
  "/api/": {
    "proxy": {
      "to": "http://upstream/v1/",
      "downstreamResponse": {
        "headers": {
          "cookies": {
            "sessionId": {
              "secure": true,
              "httpOnly": false,
              "sameSite": "lax",
              "domain": "example.com"
```

In the example above, GatewayCore ensures that the [`Set-Cookie`][SetCookieHeader] response header for a cookie named `sessionId` is modified such that:

- the `Secure` attribute is set,
- the `HttpOnly` attribute is removed if it was specified,
- the value of `SameSite` is changed to `lax`, and
- the `Domain` attribute is updated to `example.com`

> Set `domain` to an empty text (`"domain": ""`) if the `Domain` attribute should be fully removed from the [`Set-Cookie`][SetCookieHeader] header.
> The `domain` value can be text or an [expression](#Expressions).

It is also possible to use the wildcard symbol `"*"` to provide a rule that applies to all cookies as shown below:

```json
"routes": {
  "/api/": {
    "proxy": {
      "to": "http://upstream/v1/",
      "downstreamResponse": {
        "headers": {
          "cookies": {
            "*": {
              "secure": true,
              "httpOnly": false,
              "sameSite": "strict",
              "domain": "example.com"
```

> A match of a non-wildcard rule supersedes a wildcard match.

GatewayCore pools [`HttpMessageHandler`][HttpMessageHandler] instances and can reuse them for outbound upstream requests. Thus, local cookie handling is disabled by default as unanticipated [`CookieContainer`][CookieContainer] object sharing often results in incorrect behavior. Although strongly discouraged, it is possible to change this behavior using `UseCookie`, as shown below.

```json
"routes": {
  "/api/": {
    "proxy": {
      "to": "http://upstream/v1/",
      "upstreamRequest": {
        "sender": {
          "useCookies": true
```

> Avoid enabling `UseCookies` unless you are confident that this is the behavior that your application needs.

## Trace Context

GatewayCore passes forward the [W3C ratified][TraceContext] `traceparent` and `tracestate` headers with no modifications. It uses the activity model in .NET as described [here][DistributedTracing].

## Append, add, update, or discard headers

In addition to the controls offered through explicit configuration options, GatewayCore makes it easy to append, add, update, or remove headers on both outbound requests to upstream systems, as well as responses to clients.

### Append headers

GatewayCore can append additional headers to requests sent to proxied servers, as well as responses forwarded to clients. In the example below, GatewayCore appends the `x-append-header` header with value `new-value` to proxied requests. A similar header is also added to responses with two values: `value-1` and `value-2`. If these headers already exist, these values are appended to the existing values:

```json
"routes": {
  "/api/": {
    "proxy": {
      "to": "http://upstream/v1/",
      "upstreamRequest": {
        "headers": {
          "overrides": {
            "x-append-header": [ "new-value" ]
          }
        }
      },
      "downstreamResponse": {
        "headers": {
          "overrides": {
            "x-append-header": [ "value-1", "value-2" ]
```

> A header value can be text or an [expression](#Expressions).

### Add headers

GatewayCore can add additional headers to requests sent to proxied servers, as well as responses forwarded to clients. In the example below, GatewayCore adds the `x-new-request-header` header with value `new-value` to proxied requests. A similar header is also added to responses with two values: `value-1` and `value-2`:

```json
"routes": {
  "/api/": {
    "proxy": {
      "to": "http://upstream/v1/",
      "upstreamRequest": {
        "headers": {
          "overrides": {
            "x-new-request-header": [ "new-value" ]
          }
        }
      },
      "downstreamResponse": {
        "headers": {
          "overrides": {
            "x-new-response-header": [ "value-1", "value-2" ]
```

> A header value can be text or an [expression](#Expressions).

### Update headers

GatewayCore can update headers that it proxies. In the example below, it changes the value of `x-request-header` header to `updated-value`. The values of a similar response header are also replaced with `value-1` and `value-2`:

```json
"routes": {
  "/api/": {
    "proxy": {
      "to": "http://upstream/v1/",
      "upstreamRequest": {
        "headers": {
          "overrides": {
            "x-request-header": [ "updated-value" ]
          }
        }
      },
      "downstreamResponse": {
        "headers": {
          "overrides": {
            "x-response-header": [ "value-1", "value-2" ]
```

> A header value can be text or an [expression](#Expressions).

### Discard headers

The value of inbound headers can be discarded from proxied requests, as well as responses. Use the `discards` option to ignore the value of these headers:

```json
"routes": {
  "/api/": {
    "proxy": {
      "to": "http://upstream/v1/",
      "upstreamRequest": {
        "headers": {
          "discards": [ "x-header-1", "x-header-2" ]
        }
      },
      "downstreamResponse": {
        "headers": {
          "discards": [ "x-header-1", "x-header-2" ]
```

### Discard inbound headers

It is possible to discard the values of all inbound headers. Use `discardInboundHeaders` to drop all inbound client request headers, as well as all outbound response headers:

```json
"routes": {
  "/api/": {
    "proxy": {
      "to": "http://upstream/v1/",
      "upstreamRequest": {
        "headers": {
          "discardInboundHeaders": true
        }
      },
      "downstreamResponse": {
        "headers": {
          "discardInboundHeaders": true
```

### Empty headers

It is typically unexpected to receive headers that do not have a value, but it is perfectly valid to have headers such as `HTTP2-Settings` with an empty value. You can set `discardEmpty` to `true` to discard headers with no value:

```json
"routes": {
  "/api/": {
    "proxy": {
      "to": "http://upstream/v1/",
      "upstreamRequest": {
        "headers": {
          "discardEmpty": true,
        }
      },
      "downstreamResponse": {
        "headers": {
          "discardEmpty": true
```

### Headers with underscore

Some clients and servers do not expect an underscore character (`_`) in header names. Use `discardUnderscore` to remove these headers:

```json
"routes": {
  "/api/": {
    "proxy": {
      "to": "http://upstream/v1/",
      "upstreamRequest": {
        "headers": {
          "discardUnderscore": true,
        }
      },
      "downstreamResponse": {
        "headers": {
          "discardUnderscore": true
```

## Expressions

Some of the configurations support the use of variables. These variables are:

| Variable | Description |
|:--- |:--- |
|`$content_length`|The value of the `Content-Length` request header.|
|`$content_type`|The value of the `Content-Type` request header.|
|`$host`|The value of the [`Host`][HostHeader] request header.|
|`$request_method`|The HTTP method of the inbound downstream request.|
|`$request_scheme`|The scheme (`HTTP` or `HTTPS`) used by the inbound downstream request.|
|`$request_path_base`|The unescaped path base value.|
|`$request_path`|The unescaped path value.|
|`$request_query_string`|The escaped query string with the leading '?' character.|
|`$request_encoded_url`|The original escaped request URL including the query string portion (`scheme + host + path-base + path + query-string`).|
|`$remote_address`|The IP address of the remote client/caller.|
|`$remote_port`|The IP port number of the remote client/caller.|
|`$server_name`|The name of the server which accepted the request.|
|`$server_address`|The IP address of the server which accepted the request.|
|`$server_port`|The IP port number of the server which accepted the request.|
|`$server_protocol`|The protocol of the inbound downstream request, usually `HTTP/1.0`, `HTTP/1.1`, or `HTTP/2.0`.|

For example, the following configuration adds `x-my-custom-header` HTTP header to the proxied call to the upstream. The value of the header includes the protocol and the port number of the server:

```json
"routes": {
  "/api/": {
    "proxy": {
      "to": "http://upstream/v1/",
      "upstreamRequest": {
        "headers": {
          "overrides": {
            "x-my-custom-header": [ "$server_protocol : $server_port" ]
```

The header will like this:`x-my-custom-header: HTTP/1.1 : 5099`

## Advanced extensibility and configuration

When using the GatewayCore as a library within your .net core application, you have full control over most portions of the proxy pipeline and other gateway components.

### Upstream request http client

TODO

### Gateway settings and options

TODO

- how to pass using DI
- A table of what they are

| Key | Can be an expression | Default value | Description |
|:--- |:---:|:-- |:-- |
| `system` | | | This is the section that contains all system wide configurations. |
| `system:routeCacheMaxCount` | | `100,000` cache entries| This is the maximum number of mappings between "inbound downstream request path" and "outbound upstream request URL" that can be cached in memory. |
| `routes` | | | This is the section in which proxy routes are defined. |
| `routes:<path>` | | | This is the url pattern that if matched, the request is proxied to the address defined by its `to` property. |
| `routes:<path>:proxy` | | | This is the proxy configuration section for this url pattern match. |
| `routes:<path>:proxy:to` | :heavy_check_mark: | | This is an expression that defines the URL of the upstream server to which the downstream request is forwarded to. This is a required property. |
| `routes:<path>:proxy:proxyName` | :heavy_check_mark: | `gwcore` | This is an expression that defines the name of this proxy. This value is used in the Via HTTP header send on the outbound upstream request, and the outbound downstream response. If a value is specified, an `x-gwcore-proxy-name` header with this value is also added to the outbound upstream request. |

[ConnectionHeader]:https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Connection
[ViaHeader]:https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Via
[ViaHeaderSpec]:https://datatracker.ietf.org/doc/html/rfc7230#section-5.7.1
[ForwardedHeader]:https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Forwarded
[XForwardedForHeader]:https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Forwarded-For
[XForwardedHostHeader]:https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Forwarded-Host
[XForwardedProtoHeader]:https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Forwarded-Proto
[HostHeader]:https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Host
[ServerHeader]:https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Server
[SetCookieHeader]:https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Set-Cookie
[RequestPseudoHeaders]:https://datatracker.ietf.org/doc/html/rfc7540#section-8.1.2.3
[ResponsePseudoHeaders]:https://datatracker.ietf.org/doc/html/rfc7540#section-8.1.2.4

[DefactoWiki]:https://en.wikipedia.org/wiki/De_facto_standard
[ObscurityWiki]:https://en.wikipedia.org/wiki/Security_through_obscurity

[License]:https://github.com/cloudtoid/gateway-core/blob/master/LICENSE
[HttpMessageHandler]:https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpmessagehandler
[CookieContainer]:https://docs.microsoft.com/en-us/dotnet/api/system.net.http.socketshttphandler.cookiecontainer
[TraceContext]:https://www.w3.org/TR/trace-context/
[DistributedTracing]:https://devblogs.microsoft.com/aspnet/improvements-in-net-core-3-0-for-troubleshooting-and-monitoring-distributed-apps/