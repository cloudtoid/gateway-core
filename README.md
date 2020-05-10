<a href="https://github.com/cloudtoid"><img src="https://raw.githubusercontent.com/cloudtoid/assets/master/logos/cloudtoid-black-red.png" width="100"></a>

# Gateway Core

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/cloudtoid/url-patterns/blob/master/LICENSE)

A modern API Gateway and Reverse Proxy library for .NET Core and beyond.

## V1 - TODOs

- Add functional tests
- Add support for certificates
- Test HTTP/2
- Write documentation
- Add tests for trailing TrailingHeaderSetter
- Enable github build actions
- Publish NuGet
- Add multiple Sample Projects
  - Basic/simple
  - Multiple kestrel servers
  - Advanced extensibility
  - Use of options
  - Named HttpClient Management from the outside
- Test SSL end to end
- Side by side tests with nginx
- Kestrel should only listen on relevant routes defined in Options
- Benchmark
- Increase code coverage

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

## Tracing

GatewayCore uses two headers to pass on trace identifiers to both the server and the client:

### Correlation-id header

A request originated from a client can include a correlation-id header that is passed on unchanged to the proxied server. The default correlation-id header is `x-correlation-id` but can be renamed as shown here:

```json
{
  "routes": {
    "/api/": {
      "proxy": {
        "to": "http://domain.com/upstream/",
        "correlationIdHeader": "x-request-id",
      }
```

In the case of a missing correlation-id header, GatewayCore generates a unique identifier instead.

The correlation-id header can also be added to the response message by explicitly enabling `includeCorrelationId`:

```json
{
  "routes": {
    "/api/": {
      "proxy": {
        "to": "http://domain.com/upstream/",
        "downstreamResponse": {
          "headers": {
            "includeCorrelationId": true
          }
```

It is also possible to omit this header from the request to the proxied server using `'ignoreCorrelationId'` as shown below:

```json
{
  "routes": {
    "/api/": {
      "proxy": {
        "to": "http://domain.com/upstream/",
        "upstreamRequest": {
          "headers": {
            "ignoreCorrelationId": true,
          }
        }
```

### Call-id header

A unique call-id is generated and forwarded for every request received by GatewayCore. The header that is appended is `x-call-id` and can be removed from outbound upstream requests using `ignoreCallId`. It can also be included in responses sent to the client by enabling `includeCallId` as shown in the sample below:

```json
{
  "routes": {
    "/api/": {
      "proxy": {
        "to": "http://domain.com/upstream/",
        "upstreamRequest": {
          "headers": {
            "ignoreCallId": true
          }
        },
        "downstreamResponse": {
          "headers": {
            "includeCallId": true
          }
        }
```

> An inbound call-id header received from the client or the proxied server is silently ignored.

## Tracking

A proxy typically uses the [HTTP Via header](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Via) for tracking message forwards, avoiding request loops, and identifying the protocol capabilities of senders along the request/response chain.

This header is a comma-separated list of proxies along the message chain with the closest proxy to the sender being the left-most value.

The value added by GatewayCore includes the pseudonym of the proxy. This name is `gwcore` but can be customized, as shown below:

```json
{
  "routes": {
    "/api/": {
      "proxy": {
        "to": "http://domain.com/upstream/",
        "proxyName": "my-proxy-name",
```

GatewayCore appends one of the following values:

| Sender's protocol | Value | Example |
|:--- |:--- |:-- |
| HTTP/1.0 | `1.0 \<proxy-name\>` | `1.0 gwcore` |
| HTTP/1.1 | `1.1 \<proxy-name\>` | `1.1 gwcore` |
| HTTP/2.0 | `2.0 \<proxy-name\>` | `2.0 gwcore` |
| HTTP/X.Y | `X.Y \<proxy-name\>` | `X.Y gwcore` |
| \<protocol>/\<version> | `<protocol>\<version> <proxy-name>` |  

> As per the above, GatewayCore omits the protocol for HTTP requests and responses.

The Via header is included by default on both the request to the proxied server and the response to the client. You can change this behavior using `ignoreVia` as shown below:

```json
{
  "routes": {
    "/api/": {
      "proxy": {
        "to": "http://domain.com/upstream/",
        "upstreamRequest": {
          "headers": {
            "ignoreVia": true,
            }
          }
        },
        "downstreamResponse": {
          "headers": {
            "ignoreVia": true,
```
