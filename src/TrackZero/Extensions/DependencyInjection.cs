﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace TrackZero
{
    /// <summary>
    /// 
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <param name="accountApiKey"></param>
        /// <param name="throwExcpetions"></param>
        /// <returns></returns>
        public static IServiceCollection AddTrackZero(this IServiceCollection serviceCollection, string accountApiKey, bool throwExcpetions = true)
        {
            return serviceCollection.AddTrackZero(accountApiKey, "https://api.trackzero.io", true, throwExcpetions);
        }

        public static IServiceCollection AddTrackZero(this IServiceCollection serviceCollection, string accountApiKey, string apiBaseUrl, bool validateSslCertificate, bool throwExcpetions = true)
        {
            var sc = serviceCollection
                .AddHttpClient("TrackZero", c =>
                {
                    c.BaseAddress = new Uri(apiBaseUrl);
                    c.DefaultRequestHeaders.Add("X-API-KEY", accountApiKey);
                    c.DefaultRequestHeaders.Add("X-API-VERSION", "1.0");
                });


            if (!validateSslCertificate)
            {
                sc.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
                });
            }

            return serviceCollection.AddSingleton<TrackZeroClient>(sp =>
            {
                return new TrackZeroClient(sp, sp.GetRequiredService<IHttpClientFactory>(), throwExcpetions);
            });
        }


    }
}
