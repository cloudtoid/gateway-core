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

A unique call-id is generated and forwarded for every request received by GatewayCore. The header that is appended is `x-call-id` and can be removed from outbound upstream requests using `ignoreCallId`. It can also be included in responses to the client by enabling `includeCallId` as shown in the sample below:

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

