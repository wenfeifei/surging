﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Surging.Core.KestrelHttpServer;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Surging.Core.Stage.Internal.Implementation
{
    public class WebServerListener : IWebServerListener
    {
        private readonly ILogger<WebServerListener> _logger;
        public WebServerListener(ILogger<WebServerListener> logger)
        {
            _logger = logger;
        }

        void IWebServerListener.Listen(WebHostContext context)
        {

            var httpsPorts = AppConfig.Options.HttpsPort?.Split(",");
            var httpPorts = AppConfig.Options.HttpPorts?.Split(",");
            if (AppConfig.Options.EnableHttps)
            {
                foreach (var httpsPort in httpsPorts)
                {
                    int.TryParse(httpsPort, out int port);
                    if (string.IsNullOrEmpty(AppConfig.Options.HttpsPort)) port = 443;
                    if (port > 0)
                    {
                        context.KestrelOptions.Listen(context.Address, port, listOptions =>
                    {
                        X509Certificate2 certificate2 = null;
                        var pfxFile = Path.Combine(AppContext.BaseDirectory, AppConfig.Options.CertificateFileName);
                        if (File.Exists(pfxFile))
                            certificate2 = new X509Certificate2(pfxFile, AppConfig.Options.CertificatePassword);
                        else
                        {
                            var paths = GetPaths(AppConfig.Options.CertificateLocation);
                            foreach (var path in paths)
                            {
                                pfxFile = Path.Combine(path, AppConfig.Options.CertificateFileName);
                                if (File.Exists(pfxFile))
                                    certificate2 = new X509Certificate2(pfxFile, AppConfig.Options.CertificatePassword);

                            }
                        }
                        listOptions = certificate2 == null ? listOptions.UseHttps() : listOptions.UseHttps(certificate2);
                    });
                    }
                }
            }

            foreach (var httpPort in httpPorts)
            {
                int.TryParse(httpPort, out int port);
                if (port > 0)
                    context.KestrelOptions.Listen(context.Address, port);
            }

        }

        private string[] GetPaths(params string[] virtualPaths)
        {
            var result = new List<string>();
            string rootPath = string.IsNullOrEmpty(CPlatform.AppConfig.ServerOptions.RootPath) ?
                AppContext.BaseDirectory : CPlatform.AppConfig.ServerOptions.RootPath; 
            foreach (var virtualPath in virtualPaths)
            {

                var path = Path.Combine(rootPath, virtualPath);
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug($"准备查找路径{path}下的目录证书。");
                if (Directory.Exists(path))
                {
                    var dirs = Directory.GetDirectories(path);
                    result.AddRange(dirs.Select(dir => Path.Combine(rootPath,virtualPath, new DirectoryInfo(dir).Name)));
                }
                else
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                        _logger.LogDebug($"未找到路径：{path}。");
                }
            }
            return result.ToArray();
        }
    }
}
