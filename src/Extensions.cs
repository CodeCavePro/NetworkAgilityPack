using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using CodeCave.NetworkAgilityPack.Http;
using CodeCave.NetworkAgilityPack.Socks.Web;

namespace CodeCave.NetworkAgilityPack
{
    public static class WebHeaderCollectionExtensions
    {
        /// <summary>
        /// Determines whether the specified header collection contains header.
        /// </summary>
        /// <param name="headers">The header collection.</param>
        /// <param name="headerKey">Header name to look for.</param>
        /// <param name="headerKeyFound">The name of the matching header (might be slightly different from the supplied name).</param>
        /// <returns></returns>
        public static bool ContainsHeaderKey(this WebHeaderCollection headers, string headerKey, out string headerKeyFound)
        {
            headerKeyFound = null;
            if (headers == null || !headers.HasKeys())
                return false;

            var keyName = headers.Keys
                .Cast<string>()
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .FirstOrDefault(k =>
                    k.Equals(headerKey, StringComparison.OrdinalIgnoreCase) ||
                    k.Replace(HttpControlChars.SEP, string.Empty).Equals(headerKey, StringComparison.OrdinalIgnoreCase)
                );

            if (string.IsNullOrWhiteSpace(keyName))
                return false;

            headerKeyFound = keyName;
            return true;
        }

        /// <summary>
        /// Determines whether the specified header collection contains header.
        /// </summary>
        /// <param name="headers">The header collection.</param>
        /// <param name="headerKey">Header name to look for.</param>
        /// <param name="headerKeyFound">The matching header (might be slightly different from the supplied name).</param>
        /// <returns></returns>
        public static bool ContainsHeader(this WebHeaderCollection headers, HttpRequestHeader headerKey, out string headerKeyFound)
        {
            return headers.ContainsHeaderKey(headerKey.ToString(), out headerKeyFound);
        }
    }

    public static class HttpStatusCodeExtensions
    {
        /// <summary>
        /// Tries the get a valid status code from a string.
        /// </summary>
        /// <param name="value">The value to used to extract status code.</param>
        /// <param name="statusCode">The extracted status code.</param>
        /// <returns></returns>
        public static bool TryGetStatusCode(this string value, out HttpStatusCode statusCode)
        {
            statusCode = HttpStatusCode.BadRequest;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            // Remove junk characters
            value = value.Replace(HttpControlChars.CR, string.Empty);
            value = value.Replace(HttpControlChars.LF, string.Empty);
            value = value.Trim();

            var indexOfWhitespace = value.IndexOf(' ');
            var code = value.Substring(0, indexOfWhitespace);
            var reason = value.Substring(++indexOfWhitespace);
            var responseCode = (reason.Length > 0 && reason.Contains(HttpControlChars.SP))
                ? reason.Substring(0, reason.IndexOf(HttpControlChars.SP, StringComparison.InvariantCultureIgnoreCase))
                : null;

            int responseCodeNum;
            if (responseCode == null || !int.TryParse(responseCode, out responseCodeNum)) 
                return false;

            if (!Enum.IsDefined(typeof (HttpStatusCode), responseCodeNum))
                return false;

            statusCode = (HttpStatusCode) responseCodeNum;
            return true;
        }
    }

    public static class UriExtensions
    {
        /// <summary>
        /// Tries the resolve IP address using Uri object.
        /// </summary>
        /// <param name="uri">The URI to resolve.</param>
        /// <param name="address">Resolved IP address.</param>
        /// <returns></returns>
        public static bool TryResolveIpAddress(this Uri uri, out IPAddress address)
        {
            if (IPAddress.TryParse(uri.Host, out address))
                return true;

            try
            {
                address = Dns.GetHostEntry(uri.Host).AddressList.FirstOrDefault();
                return true;
            }
            catch
            {
                address = IPAddress.None;
                return false;
            }
        }
    }


    public static class WebRequestExtensions
    {
        /// <summary>
        /// Sets the expect100 continue header/request property.
        /// </summary>
        /// <param name="request">The request to alter.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public static void SetExpect100Continue(this WebRequest request, bool value)
        {
            if (request is FtpWebRequest)
                return;

            const string expectedHeader = "Expect";
            const string expectedValue = "100-continue";
            string headerName;

            if (value)
            { 
                if (request.Headers.ContainsHeaderKey(expectedHeader, out headerName))
                    request.Headers[headerName] = expectedValue;
                else
                    request.Headers.Add(headerName, expectedValue);
            }
            else
            {
                if (request.Headers.ContainsHeaderKey(expectedHeader, out headerName))
                    request.Headers.Remove(headerName);
            }

            if (request is HttpWebRequest)
            {
                ((HttpWebRequest)request).ServicePoint.Expect100Continue = value;
            }
        }
    }

    public static class WebResponseExtensions
    {
        /// <summary>
        /// Gets the status code.
        /// </summary>
        /// <param name="response">The response object to inspect.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        public static int GetStatusCode(this WebResponse response)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));

            if (response is HttpWebResponse)
                return (int)((HttpWebResponse)response).StatusCode;

            if (response is HttpWebResponseSocks)
                return (int)((HttpWebResponseSocks)response).StatusCode;

            if (response is FtpWebResponse)
                return (int)((FtpWebResponse)response).StatusCode;

            throw new NotImplementedException($"Unknown type of {response}: '{response.GetType()}'");
        }
    }

    public static class EnumExtensions
    {
        /// <summary>
        /// Gets the value from description attribute.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="description">The description.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentException">Not found.;description</exception>
        public static T GetValueFromDescription<T>(string description)
        {
            var type = typeof(T);
            if (!type.IsEnum) throw new InvalidOperationException();
            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                        return (T)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (T)field.GetValue(null);
                }
            }
            throw new ArgumentException("Not found.", nameof(description));
        }

        /// <summary>
        /// Gets the enum description.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string GetEnumDescription(Enum value)
        {
            var fi = value.GetType().GetField(value.ToString());
            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return (attributes.Length > 0)
                ? attributes[0].Description 
                : value.ToString();
        }
    }

    public static class IPAddressExtensions
    {
        /// <summary>
        /// Resolves the host using DNS.
        /// </summary>
        /// <param name="hostname">The hostname to resolve.</param>
        /// <returns></returns>
        public static IPAddress ResolveHostDns(this string hostname)
        {
            var ipHostEntry = Dns.GetHostEntry(hostname);
            foreach (var address in ipHostEntry.AddressList.Where(address => address.AddressFamily == AddressFamily.InterNetwork))
            {
                return address;
            }

            return (ipHostEntry.AddressList.Length > 0)
                ? ipHostEntry.AddressList[0]
                : null;
        }
    }

    public static class NetworkCredentialExtensions
    {
        /// <summary>
        /// Tries the parse authentication domain and resolve it to IP address.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <param name="ipAddress">The IP address.</param>
        /// <param name="port">The port.</param>
        /// <returns></returns>
        public static bool TryParseDomainx(this NetworkCredential credentials, out IPAddress ipAddress, out int port)
        {
            try
            {
                Uri uri;
                if (Uri.TryCreate(credentials.Domain, UriKind.Absolute, out uri))
                {
                    uri.TryResolveIpAddress(out ipAddress);
                    port = uri.Port;
                }
                else
                {
                    var proxyAddress = credentials.Domain.Split(new[] {':'}, 2);
                    IPAddress.TryParse(proxyAddress[0], out ipAddress);
                    int.TryParse((proxyAddress.Length == 2) ? proxyAddress[1] : "0", out port);
                }
                return true;
            }
            catch
            {
                ipAddress = null;
                port = 0;
                return false;
            }
        }
    }

    public static class KnownHttpVerbExtensions
    {
        /// <summary>
        /// To the known HTTP verb.
        /// </summary>
        /// <param name="stringValue">The string value.</param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public static KnownHttpVerb ToKnownHttpVerb(this string stringValue)
        {
            KnownHttpVerb verb;
            if (!Enum.TryParse(stringValue, true, out verb))
                throw new InvalidDataException($"Invalid KnownHttpVerb value: {stringValue}");
            return verb;
        }
    }
}
