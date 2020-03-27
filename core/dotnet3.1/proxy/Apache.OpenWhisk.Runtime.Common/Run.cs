/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Apache.OpenWhisk.Runtime.Common
{
    public class Run
    {
        private readonly Func<HttpRequest,Task<string>> _methodAsync;
        private readonly Func<HttpRequest,string> _method;

        private readonly bool _awaitableMethod;

        public Run(MethodInfo method, bool awaitableMethod)
        {
            if( awaitableMethod )
                _methodAsync = (Func<HttpRequest, Task<string>>)Delegate
                    .CreateDelegate( typeof( Func<HttpRequest, Task<string>> ), method );
            else
                _method = (Func<HttpRequest, string>)Delegate
                    .CreateDelegate( typeof( Func<HttpRequest, string> ), method );

            _awaitableMethod = awaitableMethod;
        }

        public async Task HandleRequest(HttpContext httpContext)
        {
            if ( _methodAsync == null && _method == null)
            {
                await httpContext.Response.WriteError("Cannot invoke an uninitialized action.");
                return;
            }

            try
            {
                //string body = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();

                //JObject inputObject = string.IsNullOrEmpty(body) ? null : JObject.Parse(body);

                //JObject valObject = null;

                //if (inputObject != null)
                //{
                //    valObject = inputObject["value"] as JObject;
                //    foreach (JToken token in inputObject.Children())
                //    {
                //        try
                //        {
                //            if (token.Path.Equals("value", StringComparison.InvariantCultureIgnoreCase))
                //                continue;
                //            string envKey = $"__OW_{token.Path.ToUpperInvariant()}";
                //            string envVal = token.First.ToString();
                //            Environment.SetEnvironmentVariable(envKey, envVal);
                //            //Console.WriteLine($"Set environment variable \"{envKey}\" to \"{envVal}\".");
                //        }
                //        catch (Exception)
                //        {
                //            await Console.Error.WriteLineAsync(
                //                $"Unable to set environment variable for the \"{token.Path}\" token.");
                //        }
                //    }
                //}

                try
                {
                    string output;
                    
                    if(_awaitableMethod) 
                        output = await _methodAsync(httpContext.Request);
                    else 
                        output = _method(httpContext.Request);

                    if (output == null)
                    {
                        await httpContext.Response.WriteError("The action returned null");
                        Console.Error.WriteLine("The action returned null");
                        return;
                    }
                    //httpContext.Response.WriteResponse
                    await httpContext.Response.WriteResponse(200, output.ToString());
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.StackTrace);
                    await httpContext.Response.WriteError(ex.Message
#if DEBUG
                                                          + ", " + ex.StackTrace
#endif
                    );
                }
            }
            finally
            {
                Startup.WriteLogMarkers();
            }
        }
    }
}
