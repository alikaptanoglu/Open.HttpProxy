using System;
using System.Diagnostics;
using System.Security.Policy;

namespace Open.HttpProxy
{
	public class Request : HttpMessage
	{
		public Request()
		{
		}

		public Request(RequestLine requestLine, HttpHeaders headers, byte[] body = null)
		{
			RequestLine = requestLine;
			Headers = headers;
			if(body!=null) Body = body;
		}

		public RequestLine RequestLine { get; set; }

		public DnsEndPoint EndPoint => new DnsEndPoint(Uri.Host, Uri.Port);

		public bool IsHttps =>  RequestLine != null && RequestLine.IsVerb("CONNECT");

		internal Uri Uri
		{
			get
			{
				var uri = RequestLine.Uri;

				string scheme;
				var parts = uri.Split(new [] { "://"}, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length >= 2)
				{
					scheme = parts[0];
					uri = parts[1];
				}
				else
				{
					scheme = IsHttps ? "https" : "http";
					uri = parts[0];
				}
				
				string authority=null;
				if (uri[0] != '/')
				{
					authority = IsHttps ? uri : uri.Substring(0, uri.IndexOf("/", StringComparison.Ordinal));
				}

				int port = IsHttps ? 443 : 80;
				string host = null;
				if (authority != null)
				{
					int c = authority.IndexOf(':');
					if (c < 0)
					{
						host = authority;
					}
					else if (c == authority.Length - 1)
					{
						host = authority.TrimEnd('/');
					}
					else
					{
						host = authority.Substring(0, c);
						port = int.Parse(authority.Substring(c + 1));
					}
				}

				if (host == null)
				{
					host = Headers.Host;

					int cp = host.IndexOf(':');
					if (cp >= 0)
					{
						if (cp == host.Length - 1)
							host = host.TrimEnd('/');
						else
						{
							host = host.Substring(0, cp);
							port = int.Parse(host.Substring(cp + 1));
						}
					}
				}
				return new Uri($"{scheme}://{host}:{port}");
			}
		}

		protected override ProtocolVersion GetVersion()
		{
			return RequestLine.Version;
		}

		protected override bool HasContent()
		{
			return (Headers.ContentLength.HasValue && Headers.ContentLength.Value > 0) || IsChunked;
		}

		protected override bool VerifyWebSocketHandshake()
		{
			return "websocket".Equals(Headers.Upgrade, StringComparison.OrdinalIgnoreCase)
				&& RequestLine != null && RequestLine.IsVerb("GET")
				&& ("ws".Equals(Uri.Scheme, StringComparison.OrdinalIgnoreCase)
					|| "wss".Equals(Uri.Scheme, StringComparison.OrdinalIgnoreCase));
		}
	}
}