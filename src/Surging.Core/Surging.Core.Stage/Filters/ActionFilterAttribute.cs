﻿using Autofac;
using Microsoft.AspNetCore.Http;
using Surging.Core.ApiGateWay;
using Surging.Core.ApiGateWay.OAuth;
using Surging.Core.CPlatform.Messages;
using Surging.Core.CPlatform.Utilities;
using Surging.Core.KestrelHttpServer.Filters;
using Surging.Core.KestrelHttpServer.Filters.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Surging.Core.Stage.Filters
{
    public class ActionFilterAttribute : IActionFilter
    {
        private readonly IAuthorizationServerProvider _authorizationServerProvider;
        public ActionFilterAttribute()
        {
            _authorizationServerProvider = ServiceLocator.Current.Resolve<IAuthorizationServerProvider>();
        }

        public Task OnActionExecuted(ActionExecutedContext filterContext)
        {
            return Task.CompletedTask;
        }

        public async Task OnActionExecuting(ActionExecutingContext filterContext)
        {
            var gatewayAppConfig = AppConfig.Options.ApiGetWay;
            if (filterContext.Message.RoutePath == gatewayAppConfig.AuthorizationRoutePath)
            {
                var token = await _authorizationServerProvider.GenerateTokenCredential(new Dictionary<string, object>(filterContext.Message.Parameters));
                if (token != null)
                {
                    filterContext.Result  = HttpResultMessage<object>.Create(true, token);
                    filterContext.Result.StatusCode = (int)ServiceStatusCode.Success;
                }
                else
                {
                    filterContext.Result = new HttpResultMessage<object> { IsSucceed = false, StatusCode = (int)ServiceStatusCode.AuthorizationFailed, Message = "Invalid authentication credentials" };
                }
            }
        }
    }
}
