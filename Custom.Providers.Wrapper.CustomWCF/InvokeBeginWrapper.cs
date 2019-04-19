using NewRelic.Agent.Extensions.Logging;
using NewRelic.Agent.Extensions.Providers.Wrapper;
using System;
using System.Net;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace Custom.Providers.Wrapper.CustomWCF
{
    public class InvokeBeginWrapper : IWrapper
    {
        // 	System.ServiceModel.dll!System.ServiceModel.Dispatcher.DispatchOperationRuntime.InvokeBegin(ref System.ServiceModel.Dispatcher.MessageRpc rpc = {System.ServiceModel.Dispatcher.MessageRpc})	Unknown	Non-user code. Skipped loading symbols.

        public bool IsTransactionRequired => false;

        private Dictionary<string, string> configuredHeaders = null;
        private Type webOperationContextType = null;

        public CanWrapResponse CanWrap(InstrumentedMethodInfo instrumentedMethodInfo)
        {
            var method = instrumentedMethodInfo.Method;
            string w = instrumentedMethodInfo.RequestedWrapperName;
            var canWrapMethod = method.MatchesAny(assemblyName: "System.ServiceModel",
                typeName: "System.ServiceModel.Dispatcher.DispatchOperationRuntime",
                methodName: "InvokeBegin");
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

            transactionWrapperApi = agentWrapperApi.CreateTransaction(true, "WCF", "Windows Communication Foundation", false);
            var segment = transactionWrapperApi.StartTransactionSegment(instrumentedMethodCall.MethodCall, "InvokeBegin");

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

            return Delegates.GetDelegateFor(
                onFailure: transactionWrapperApi.NoticeError,
                onComplete: () =>
                {
                    segment.End();
                    transactionWrapperApi.End();
                });
        }
    }
}
