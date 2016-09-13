﻿using System;
using System.Linq;
using System.Reflection;
using HyperMock.Setups;

namespace HyperMock.Core
{
    internal sealed class MockProxyDispatcherHelper
    {
        private readonly IMockProxyDispatcher _dispatcher;

        internal MockProxyDispatcherHelper(IMockProxyDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        internal DispatcherResponse Handle(MethodBase method, object[] inArgs, object[] allArgs)
        {
            _dispatcher.Visits.Record(method, inArgs);

            var response = new DispatcherResponse {ReturnArgs = allArgs};

            var setupInfo = _dispatcher.Setups.FindBy(method.Name, inArgs);

            if (setupInfo == null)
            {
                BuildDefaultResponse(method, response);
            }
            else if (setupInfo.Exception != null)
            {
                response.Exception = setupInfo.Exception;
            }
            else
            {
                BuildSetupResponse(setupInfo, response);
            }

            return response;
        }

        private static void BuildDefaultResponse(MethodBase method, DispatcherResponse response)
        {
            var methodInfo = (MethodInfo)method;

#if WINDOWS_UWP
            if (methodInfo.ReturnType != typeof(void) && methodInfo.ReturnType.GetTypeInfo().IsValueType)
#else
            if (methodInfo.ReturnType != typeof(void) && methodInfo.ReturnType.IsValueType)
#endif
            {
                response.ReturnValue = Activator.CreateInstance(methodInfo.ReturnType);
            }
        }

        private static void BuildSetupResponse(SetupInfo setupInfo, DispatcherResponse response)
        {
            response.ReturnValue = setupInfo.Value;

            var outAndRefParams = setupInfo.Parameters.Where(
                p => p.Type == ParameterType.Out || p.Type == ParameterType.Ref);

            foreach (var outAndRefParam in outAndRefParams)
            {
                var index = Array.IndexOf(setupInfo.Parameters, outAndRefParam);
                response.ReturnArgs[index] = outAndRefParam.Value;
            }
        }
    }
}
