using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using CodeCave.NetworkAgilityPack.Http;
using CodeCave.NetworkAgilityPack.Socks.Web;

#if !NET40

#endif

namespace CodeCave.NetworkAgilityPack.Web
{
    public class WebRequestFactory
    {

        public static IWebRequestResult Create(
            Uri uri, 
            KnownHttpVerb type = KnownHttpVerb.Get, 
            Dictionary<string, string> requestData = null,
            WebRequestSettings settings = null
        )
        {
            if (!uri.Scheme.Equals("http") && !uri.Scheme.Equals("https"))
                throw new NotImplementedException("Only http://xxxx and https://xxxx URI are supported!");

            var requestDataString = string.Empty;
            if (requestData != null && requestData.Any())
            {
                // Agregate all the parameters in a query string
                requestDataString = requestData.Aggregate(string.Empty, (current, varSet) => current + $"{HttpUtility.UrlPathEncode(varSet.Key)}={HttpUtility.UrlEncode(varSet.Value)}");
            }

            // Append data in GET/HEAD format
            switch (type)
            {
                case KnownHttpVerb.Get:
                case KnownHttpVerb.Head:
                    var ub = new UriBuilder(uri);
                    ub.Query = (string.IsNullOrWhiteSpace(ub.Query))
                        ? requestDataString
                        : $"{ub.Query}&{requestDataString}";
                    uri = ub.Uri;
                    break;
            }

            // Create web request (request type is based on proxy type)
            var requestResult = (settings?.Proxy is WebProxySocks)
                ? HttpWebRequestSocksResult.Create(uri)
                : HttpWebRequestResult.Create(uri);

            // Set request proxy
            requestResult.Request.Proxy = settings?.Proxy;

            // Set HTTP request type (POST, GET etc)
            requestResult.Request.Method = type.ToString().ToUpperInvariant();

            // Forcibly set a ContentType header on POST/PUT/DELETE request
            switch (type)
            {
                case KnownHttpVerb.Post:
                case KnownHttpVerb.Put:
                    // Set content type if it is empty
                    if (string.IsNullOrWhiteSpace(requestResult.Request.ContentType))
                        requestResult.Request.ContentType = "application/x-www-form-urlencoded";

                    // Send request BODY if needed
                    var postDataBytes = (settings?.Encoding ?? Encoding.UTF8).GetBytes(requestDataString);
                    requestResult.Request.ContentLength = postDataBytes.Length;
                    using (var stream = requestResult.Request.GetRequestStream())
                    {
                        stream.Write(postDataBytes, 0, postDataBytes.Length);
                        stream.Close();
                    }
                    break;
            }

            return requestResult;
        }
    }
}