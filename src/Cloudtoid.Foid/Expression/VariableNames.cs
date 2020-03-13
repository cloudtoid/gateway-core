namespace Cloudtoid.Foid.Expression
{
    // Based on NGINX: https://nginx.org/en/docs/http/ngx_http_core_module.html?&_ga=2.254306688.966016521.1583780354-1842431965.1581627980#variables
    public static class VariableNames
    {
        public const string ContentLength = "content_length";
        public const string ContentType = "content_type";
        public const string CorrelationId = "correlation_id";
        public const string CallId = "call_id";
        public const string Host = "host";
        public const string RequestMethod = "request_method";
        public const string RequestScheme = "request_scheme";
        public const string RequestPathBase = "request_path_base";
        public const string RequestPath = "request_path";
        public const string RequestQueryString = "request_query_string";
        public const string RequestEncodedUri = "request_encoded_uri";
        public const string RemoteAddress = "remote_address";
        public const string RemotePort = "remote_port";
        public const string ServerName = "server_name";
        public const string ServerAddress = "server_address";
        public const string ServerPort = "server_port";
        public const string ServerProtocol = "server_protocol";

        public static bool IsValidVariableChar(char c)
        {
            int a = c;
            return (a > 64 && a < 91) || (a > 96 && a < 123) || a == 95;
        }
    }
}
