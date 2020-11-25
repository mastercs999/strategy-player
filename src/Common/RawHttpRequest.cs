using Common.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    /// <summary>
    /// https://blog.dotnetframework.org/2014/08/28/http-library-using-tcpclient-in-c/
    /// </summary>
    public class RawHttpRequest
    {
        public string DownloadString(string url, NameValueCollection headers)
        {
            string serializedHeaders = String.Join("\r\n", headers.ToKeyValuePairs().Select(x => $"{x.Key}: {x.Value}")) + "\r\n\r\n";

            return DownloadString(url, serializedHeaders);
        }
        public string DownloadString(string url, string headers)
        {
            string request = "GET " + new Uri(url).PathAndQuery + " HTTP/1.1\r\n" + headers;

            // Send the request
            string response = Send(url, request);

            // Remove headers
            response = response.Substring(response.IndexOf("\r\n\r\n") + 4);

            return response;
        }
        public void DownloadFile(string url, NameValueCollection headers, string fileName)
        {
            File.WriteAllText(fileName, DownloadString(url, headers));
        }
        public void DownloadFile(string url, string headers, string fileName)
        {
            File.WriteAllText(fileName, DownloadString(url, headers));
        }

        private string Send(string url, string request)
        {
            Uri uri = new Uri(url);
            bool isHttps = uri.Scheme == Uri.UriSchemeHttps;

            using (var tc = new TcpClient())
            {
                tc.Connect(uri.Host, isHttps ? 443 : 80);
                using (var ns = tc.GetStream())
                {
                    if (isHttps)
                    {
                        // Secure HTTP
                        using (var ssl = new SslStream(ns, false, (a, b, c, d) => true, null))
                        {
                            ssl.AuthenticateAsClient(uri.Host, null, SslProtocols.Tls12, false);
                            using (var sw = new System.IO.StreamWriter(ssl))
                            using (var sr = new System.IO.StreamReader(ssl))
                            {
                                sw.Write(request);
                                sw.Flush();
                                return sr.ReadToEnd();
                            }
                        }
                    }
                    else
                    {
                        // Normal HTTP
                        using (var sw = new System.IO.StreamWriter(ns))
                        using (var sr = new System.IO.StreamReader(ns))
                        {
                            sw.Write(request);
                            sw.Flush();
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}
