#region License, Terms and Author(s)
//
// From ELMAH Sandbox
// Copyright (c) 2010-11 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      Roberto Vespa
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

namespace ElmahR.Elmah
{
    #region Imports

    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Web;
    using System.Diagnostics;
    using System.Net;
    using System.Text;
    using global::Elmah;

    #endregion

    // This module builds a json representation of
    // a trapped error and POSTs it to a destination.
    // So far it does not do anything else, like
    // handling authentication or other stuff.

    public class ErrorPostModule : HttpModuleBase
    {
        private Uri _targetUrl;
        private Uri _infoUrl;
        private string _secret;

        protected override void OnInit(HttpApplication application)
        {
            if (application == null)
                throw new ArgumentNullException("application");

            // Get the configuration section of this module.
            // If it's not there then there is nothing to initialize or do.
            // In this case, the module is as good as mute.

            var config = (IDictionary)GetConfig();
            if (config == null)
                return;

            // The module is expecting one parameter,
            // called 'targetUrl', which identifies the destination
            // of the HTTP POST that the module will perform.
            // You can also optionally supply an 'infoUrl' which 
            // can be used to call back for infos (the most logical
            // use is to specify where elmah.axd can be reached).

            var targetUrl       = new Uri(GetSetting(config, "targetUrl"), UriKind.Absolute);
            var infoUrlSetting  = GetOptionalSetting(config, "infoUrl", string.Empty);
            var infoUrl         = !string.IsNullOrEmpty(infoUrlSetting)
                                ? new Uri(infoUrlSetting, UriKind.Absolute)
                                : null;
            var secret          = GetOptionalSetting(config, "secret", string.Empty);

            var errorLogModule = application.Modules.GetSingleModule<ErrorLogModule>();
            if (errorLogModule == null)
                return;

            errorLogModule.Logged += (_, args) =>
            {
                if (args == null) throw new ArgumentNullException("args");
                SetError(HttpContext.Current, args.Entry);
            };

            _targetUrl = targetUrl;
            _infoUrl = infoUrl;
            _secret = secret;
        }

        private void SetError(HttpContext context, ErrorLogEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException("entry");

            var error = entry.Error;

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(_targetUrl);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";

                // See http://blogs.msdn.com/shitals/archive/2008/12/27/9254245.aspx
                request.ServicePoint.Expect100Continue = false;

                // The idea is to post to an url the json representation
                // of the intercepted error. We do a base 64 encoding
                // to fool the other side just in case some sort of
                // automated post validation is performed (do we have a 
                // better way to do this?). We post also the application
                // name and the handshaking token.

                var token = GetSourceId(context);
                var payload = ErrorJson.EncodeString(error);
                payload = string.IsNullOrEmpty(_secret)
                        ? payload
                        : Crypto.EncryptStringAes(payload, _secret);

                var form = new NameValueCollection
                {
                    { "error",          Base64Encode(payload) },
                    { "errorId",        entry.Id },
                    { "sourceId",       token },
                    { "infoUrl",        _infoUrl != null ? _infoUrl.AbsoluteUri : null },
                };

                // Get the bytes to determine
                // and set the content length.

                var data = Encoding.ASCII.GetBytes(W3.ToW3FormEncoded(form));
                Debug.Assert(data.Length > 0);
                request.ContentLength = data.Length;

                // Post it! (asynchronously)

                request.BeginGetRequestStream(ErrorReportingAsyncCallback(ar =>
                {
                    using (var output = request.EndGetRequestStream(ar))
                        output.Write(data, 0, data.Length);
                    request.BeginGetResponse(ErrorReportingAsyncCallback(rar => request.EndGetResponse(rar).Close() /* Not interested; assume OK */), null);
                }), null);
            }
            catch (Exception e)
            {
                // IMPORTANT! We swallow any exception raised during the 
                // logging and send them out to the trace . The idea 
                // here is that logging of exceptions by itself should not 
                // be  critical to the overall operation of the application.
                // The bad thing is that we catch ANY kind of exception, 
                // even system ones and potentially let them slip by.

                OnWebPostError(/* request, */ e);
            }
        }

        private static AsyncCallback ErrorReportingAsyncCallback(AsyncCallback callback)
        {
            return ar =>
            {
                if (ar == null) throw new ArgumentNullException("ar");

                try
                {
                    callback(ar);
                }
                catch (Exception e)
                {
                    OnWebPostError(e);
                }
            };
        }

        protected virtual string GetSourceId(HttpContext context)
        {
            var config = (IDictionary)GetConfig();
            return config == null 
                 ? string.Empty 
                 : GetSetting(config, "sourceId");
        }

        public static string Base64Encode(string str)
        {
            var encbuff = Encoding.UTF8.GetBytes(str);
            return Convert.ToBase64String(encbuff);
        }

        private static void OnWebPostError(/* WebRequest request, */ Exception e)
        {
            Debug.Assert(e != null);
            Trace.WriteLine(e);
        }

        #region Configuration

        internal const string GroupName = "elmah";
        internal const string GroupSlash = GroupName + "/";

        public static object GetSubsection(string name)
        {
            return GetSection(GroupSlash + name);
        }

        public static object GetSection(string name)
        {
            return ConfigurationManager.GetSection(name);
        }

        protected virtual object GetConfig()
        {
            return GetSubsection("errorPost");
        }

        private static string GetSetting(IDictionary config, string name)
        {
            Debug.Assert(config != null);
            Debug.Assert(!string.IsNullOrEmpty(name));

            var value = ((string)config[name]) ?? string.Empty;

            if (value.Length == 0)
            {
                throw new global::Elmah.ApplicationException(string.Format(
                    "The required configuration setting '{0}' is missing for the error posting module.", name));
            }

            return value;
        }

        private static string GetOptionalSetting(IDictionary config, string name, string defaultValue = null)
        {
            Debug.Assert(config != null);
            Debug.Assert(!string.IsNullOrEmpty(name));

            var value = ((string)config[name]) ?? string.Empty;

            if (value.Length == 0)
                return defaultValue;

            return value;
        }

        #endregion
    }
}
