<a href="https://github.com/cloudtoid"><img src="https://raw.githubusercontent.com/cloudtoid/assets/master/logos/cloudtoid-black-red.png" width="100"></a>

# Gateway Core

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/cloudtoid/url-patterns/blob/master/LICENSE)

A modern API Gateway and Reverse Proxy library for .NET Core and beyond.

## V1 - TODOs

- Add functional tests
- Write documentation
  - Add a table of all config values with a description and links/anchors from the rest of the doc
  - URL Pattern Matching
- Add tests for trailing TrailingHeaderSetter
- Enable github build actions
- Publish NuGet
- Add multiple Sample Projects
  - Basic/simple
  - Multiple kestrel servers
  - Advanced extensibility
  - Use of options
  - Named HttpClient Management from the outside
- Side by side tests with nginx
- Kestrel should only listen on relevant routes defined in Options
- Benchmark
- Increase code coverage
- Add support for modification of the Path attribute in the Set-Cookie header
- Ensure that all 3 projects compile on macos and VS for Mac

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

## Getting Started

> TODO

## Tracing

GatewayCore uses two headers to relay trace identifiers to servers, as well as clients:

### Correlation-id header

Requests originated from clients can include a correlation-id header that is forwarded unchanged to proxied servers. The default correlation-id header is `x-correlation-id`, but it can be renamed as shown here:

```json
{
  "routes": {
    "/api/": {
      "proxy": {
        "to": "http://upstream/v1/",
        "correlationIdHeader": "x-request-id",
```

> GatewayCore generates a unique correlation identifier for requests that do not have a correlation-id header.

The correlation-id header is not included in response messages, but you can add it by explicitly enabling `addCorrelationId`.

```json
{
  "routes": {
    "/api/": {
      "proxy": {
        "to": "http://upstream/v1/",
        "downstreamResponse": {
          "headers": {
            "addCorrelationId": true
```

It is also possible to omit the correlation-id header from outbound requests using `skipCorrelationId` as shown below:

```json
{
  "routes": {
    "/api/": {
      "proxy": {
        "to": "http://upstream/v1/",
        "upstreamRequest": {
          "headers": {
            "skipCorrelationId": true,
```

### Call-id header

A unique call-id is generated and forwarded on every request received by GatewayCore. The header with this unique id is `x-call-id` and can be dropped from outbound upstream requests using `skipCallId`:

```json
{
  "routes": {
    "/api/": {
      "proxy": {
        "to": "http://upstream/v1/",
        "upstreamRequest": {
          "headers": {
            "skipCallId": true
```

The `x-call-id` header can also be included in responses sent to clients if `addCallId` is explicitly enabled:

```json
{
  "routes": {
    "/api/": {
      "proxy": {
        "to": "http://upstream/v1/",
        "downstreamResponse": {
          "headers": {
            "addCallId": true
```

> All inbound call-id headers are silently ignored.

## Route tracking

There are four types of route tracking: two that offer information on proxies, and two that carry client details.

### Via header

A proxy typically uses the [`Via`](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Via) header for tracking message forwards, avoiding request loops, and identifying the protocol capabilities of senders along the request/response chain.

This header is a comma-separated list of proxies along the message chain with the closest proxy to the sender being the left-most value.

The value added by GatewayCore includes the pseudonym of the proxy. This default name is `gwcore` but can be customized with `proxyName`:

```json
{
  "routes": {
    "/api/": {
      "proxy": {
        "to": "http://upstream/v1/",
        "proxyName": "my-proxy-name",
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

The `Via` header is included by default on both requests to proxied servers, as well as responses to clients. You can change this behavior using `skipVia` as shown below:

```json
{
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
            "skipVia": true,
```

### Forwarded category of headers

The forwared class of headers contains information from the client-facing side of proxy servers that is altered or lost when a proxy is involved in the path of the request. This information is passed on using one of these techniques:

1. The [`Forwarded`](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Forwarded) header is what GatewayCore uses by default. This is the standardized version of the header.
1. The [`X-Forwarded-For`](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Forwarded-For), [`X-Forwarded-Host`](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Forwarded-Host), and [`X-Forwarded-Proto`](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Forwarded-Proto) headers which are considered the [de-facto standard](https://en.wikipedia.org/wiki/De_facto_standard) versions of the [`Forwared`](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Forwarded) header.

The information included in these headers typically consists of the IP address of the client, the IP address where the request came into the proxy server, the [`Host`](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Host) request header field as received by the proxy, and the protocol used by the request (typically "http" or "https").

> IP V6 addresses are quoted and enclosed in square brackets.

GatewayCore uses the `Forwarded` header by default and replaces all inbound `X-Forwarded-*` headers. You can enable `useXForwarded` to reverse this behavior and prefer `X-Forwarded-*` headers instead:

```json
{
  "routes": {
    "/api/": {
      "proxy": {
        "to": "http://upstream/v1/",
        "upstreamRequest": {
          "headers": {
            "useXForwarded": true,
```

It is also possible to omit these headers on outbound requests by using `skipForwarded`:

```json
{
  "routes": {
    "/api/": {
      "proxy": {
        "to": "http://upstream/v1/",
        "upstreamRequest": {
          "headers": {
            "skipForwarded": true,
```

### Server header

The ['Server'](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Server) HTTP response header describes the software used by the upstream server that handled the request and generated a response.

GatewayCore discards inbound `Server` headers and does not include a `Server` header on its outbound responses to clients. This default behavior can be changed to include a `Server` header with `gwcore` as its value:

```json
{
  "routes": {
    "/api/": {
      "proxy": {
        "to": "http://upstream/v1/",
        "downstreamResponse": {
          "headers": {
            "addServer": true,
```

> [Security through obscurity](https://en.wikipedia.org/wiki/Security_through_obscurity): A `Server` header can reveal information that might make it easier for attackers to exploit known security holes. It is recommended not to include this header.
> An upstream specified `Server` header is always ignored.

### External address header

GatewayCore can forward the IP address of an immediate downstream client. This IP address is sent using the custom `x-gwcore-external-address` header and can be enabled with the `addExternalAddress` option:

```json
{
  "routes": {
    "/api/": {
      "proxy": {
        "to": "http://upstream/v1/",
        "upstreamRequest": {
          "headers": {
            "addExternalAddress": true,
```

## Cookie handling

The [`Set-Cookie`](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Set-Cookie) HTTP response header is used to send cookies from the server to the client so that the client can send them back to the server later. To send multiple cookies, multiple `Set-Cookie` headers can be sent in the same response.

This header can include attributes such as:

- `Expires`: The maximum lifetime of the cookie as an HTTP-date timestamp.
- `Max-Age`: Number of seconds until the cookie expires. A zero or negative number will expire the cookie immediately. `Max-Age` has precedence over `Expires`.
- `Domain`: Host to which the cookie will be sent on subsequent calls.
- `Path`: A path that must exist in the requested URL, or the client won't send the `Cookie` header on subsequent requests.
- `Secure`: A secure cookie is only sent to the server when a request is made with the `https:` scheme.
- `HttpOnly`: Limits the scope of the cookie to HTTP
requests. In particular, the attribute instructs the client to omit the cookie when providing access to cookies via "non-HTTP" APIs such as JavaScript's `Document.cookie` API.
- `SameSite`: Helps with potential cross-site security issues.
  - If set to `none`, cookies are sent with both cross-site requests and same-site requests.
  - If set to `strict`, cookies are sent only for same-site requests.
  - If set to `lax`, same-site cookies are withheld on cross-site subrequests, such as calls to load images or frames, but will be sent when a user navigates to the URL from an external site; for example, by following a link. From Chrome version 80 and Edge 86, the default is `lax` and not `none`.

A reverse proxy such as GatewayCore often modifies the domain, path, and scheme (http/https) of proxied requests and responses. Therefore, it might be necessary to update the `Domain`, `Path`, `SameSite`, `Secure`, and `HttpOnly` attributes as demonstrated below:

```json
{
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
              },
```

In the example above, GatewayCore ensures that the `Set-Cookie` response header for a cookie named `sessionId` is modified such that:

- the `Secure` attribute is set,
- the `HttpOnly` attribute is removed if it was specified,
- the value of `SameSite` is changed to `lax`, and
- the `Domain` attribute is updated to `example.com`

> Set `domain` to an empty text (`"domain": ""`) if the `Domain` attribute should be fully removed from the `Set-Cookie` header.
> The `domain` value can be text or an [expression](#Expressions).

It is also possible to use the wildcard symbol `"*"` to provide a rule that applies to all cookies as shown below:

```json
{
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
              },
```

> A match of a non-wildcard rule supersedes a wildcard match.

GatewayCore pools [`HttpMessageHandler`](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpmessagehandler) instances and can reuse them for outbound upstream requests. Thus, local cookie handling is disabled by default as unanticipated [`CookieContainer`](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.socketshttphandler.cookiecontainer) object sharing often results in incorrect behavior. Although strongly discouraged, it is possible to change this behavior using `UseCookie`, as shown below.

```json
{
  "routes": {
    "/api/": {
      "proxy": {
        "to": "http://upstream/v1/",
        "upstreamRequest": {
          "sender": {
            "useCookies": true
```

> Avoid enabling `UseCookies` unless you are confident that this is the behavior that your application needs.

## Add, update, or discard headers

In addition to the controls offered through explicit configuration options, GatewayCore makes it easy to add, change, or remove headers on both outbound requests to downstream systems, as well as responses to clients.

### Adding headers

GatewayCore can add additional headers to requests sent to proxied servers, as well as responses forwarded to clients. In the example below, GatewayCore adds the `x-new-request-header` header with value `new-value` to proxied requests. A similar header is also added to responses with two values: `value-1` and `value-2`:

```json
{
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
{
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
{
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
            "discardEmpty": true,
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
            "discardUnderscore": true,
```

## Expressions

# Advanced extensibility and configuration

When using the GatewayCore as a library within your .net core application, you have full control over most portions of the proxy pipeline and other gateway components.

## Upstream request http client




## Gateway settings and options

- how to pass using DI
- A table of what they are

| Key | Can be an expression | Default value | Description |
|:--- |:---:|:-- |:-- |
| `system` | | | This is the section that contains all system wide configurations. |
| `system:routeCacheMaxCount` | | `100,000` cache entries| This is the maximum number of mappings between "inbound downstream request path" and "outbound upstream request URL" that can be cached in memory. |
| `routes` | | | This is the section in which proxy routes are defined. |
| `routes:<path>` | | | This is the url path pattern that if matched, the request is proxied to the address defined by it's `to` property. |
| `routes:<path>:to` | :heavy_check_mark: | | This is an expression that defines the URL of the upstream server to which the downstream request is forwarded to. This is a required property. |
| `routes:<path>:proxyName` | :heavy_check_mark: | `gwcore` | This is an expression that defines the name of this proxy. This value is used in the Via HTTP header send on the outbound upstream request, and also the outbound downstream response. If a value is specified, an `x-gwcore-proxy-name` header with this value is added to the outbound upstream request. |
