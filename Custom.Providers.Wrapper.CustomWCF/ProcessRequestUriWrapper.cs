using NewRelic.Agent.Extensions.Logging;
using NewRelic.Agent.Extensions.Providers.Wrapper;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Custom.Providers.Wrapper.CustomWCF
{
    public class ProcessRequestUriWrapper : IWrapper
    {
        public bool IsTransactionRequired => true;

        private Dictionary<string, string> configuredHeaders = null;

        private Type webOperationContextType = null;

        public CanWrapResponse CanWrap(InstrumentedMethodInfo instrumentedMethodInfo)
        {
            var method = instrumentedMethodInfo.Method;
            var canWrapMethod = method.MatchesAny(assemblyName: "System.Data.Services",
                typeName: "System.Data.Services.RequestUriProcessor",
                methodName: "ProcessRequestUri");
            return new CanWrapResponse(canWrapMethod);
        }

        public AfterWrappedMethodDelegate BeforeWrappedMethod(InstrumentedMethodCall instrumentedMethodCall, IAgentWrapperApi agentWrapperApi, ITransactionWrapperApi transactionWrapperApi)
        {
            /// Read and setup the configured headers from newrelic.config file
            if (configuredHeaders == null)
            {
                IReadOnlyDictionary<string, string> appSettings = agentWrapperApi.Configuration.GetAppSettings();
                string reqHeaders = null;
                if (appSettings.TryGetValue("requestHeaders", out reqHeaders))
                {
                    configuredHeaders = reqHeaders?.Split(',').Select(p => p.Trim()).ToDictionary(t => t, t => $"http.{t}");
                }
                else
                {
                    configuredHeaders = new Dictionary<string, string>();
                }
            }

            if (webOperationContextType == null)
            {
                Assembly webAssembly = Assembly.Load("System.ServiceModel.Web");
                webOperationContextType = webAssembly?.GetType("System.ServiceModel.Web.WebOperationContext");
            }

            if (configuredHeaders != null)
            {
                BindingFlags flags = BindingFlags.Static | BindingFlags.Public;
                object currentContext = webOperationContextType?.GetProperty("Current", flags)?.GetValue(null);
                object request = currentContext?.GetType()?.GetProperty("IncomingRequest")?.GetValue(currentContext);
                object headers = request?.GetType()?.GetProperty("Headers")?.GetValue(request);

                if (headers != null)
                {
                    WebHeaderCollection headerCollection = headers as System.Net.WebHeaderCollection;
                    if (headerCollection != null)
                    {
                        foreach (KeyValuePair<string, string> entry in configuredHeaders)
                        {
                            string headerValue = headerCollection.Get(entry.Key);
                            if (headerValue != null)
                            {
                                InternalApi.AddCustomParameter(entry.Value, headerValue);
                            }
                        }
                    }
                }
            }

            object[] methodArgs = instrumentedMethodCall.MethodCall.MethodArguments;
            if (methodArgs.Length > 1)
            {
                object requesturi = methodArgs[0];
                string localpath = requesturi?.GetType().GetProperty("LocalPath")?.GetValue(requesturi)?.ToString()?.TrimStart('/');
                if (!string.IsNullOrWhiteSpace(localpath))
                {
                    transactionWrapperApi.SetWebTransactionName(WebTransactionType.WCF, localpath, TransactionNamePriority.CustomTransactionName);
                }
            }

                /// does not work see-> https://referencesource.microsoft.com/#System.Data.Services/System/Data/Services/ProcessRequestArgs.cs,fe129b45e15d2da5
                /// interface declared internal
                /*
                if (configuredHeaders != null)
                {
                    // Captures the method argument to be used in the local method later on.
                    object[] methodArgs = instrumentedMethodCall.MethodCall.MethodArguments;
                    if (methodArgs.Length > 1)
                    {
                        object requesturi = methodArgs[0];
                        //Effectively calling something like this below: string header = controllerContext.HttpContext.Request.Headers.Get("HEADER_NAME"); 
                        object dataserviceObject = methodArgs[1];
                        if (dataserviceObject == null)
                            throw new NullReferenceException(nameof(dataserviceObject));
                        BindingFlags flags = BindingFlags.FlattenHierarchy | BindingFlags.NonPublic ;
                        object operationContext = dataserviceObject?.GetType()?.GetProperty("OperationContext", flags)?.GetValue(dataserviceObject);
                        Inspect.GetAllProperties(dataserviceObject);
                        Inspect.GetAllFields(dataserviceObject);
                        Inspect.GetAllMethods(dataserviceObject);
                        Inspect.GetAllMembers(dataserviceObject);
                        object headers = operationContext?.GetType()?.GetProperty("RequestHeaders")?.GetValue(operationContext);

                        if (headers != null)
                        {
                            NameValueCollection headerCollection = headers as NameValueCollection;
                            if (headerCollection != null)
                            {
                                foreach (var cHeader in configuredHeaders)
                                {
                                    string headerValue = headerCollection.Get(cHeader);
                                    if (headerValue != null)
                                    {
                                        InternalApi.AddCustomParameter("http." + cHeader, headerValue);
                                    }
                                }
                            }
                        }
                    }
                }
                */
                return Delegates.GetDelegateFor(
                onSuccess: () =>
                {
                });
        }
    }
}
