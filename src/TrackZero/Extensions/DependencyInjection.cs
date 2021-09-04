﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace TrackZero
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddTrackZero(this IServiceCollection serviceCollection, string projectApiKey, bool throwExcpetions = false)
        {
            serviceCollection
                .AddHttpClient("TrackZero", c =>
                {
                    c.BaseAddress = new Uri("https://api.trackzero.io");
                    c.DefaultRequestHeaders.Add("X-API-KEY", projectApiKey);
                    c.DefaultRequestHeaders.Add("X-API-VERSION", "1.0");
                });
            return serviceCollection.AddSingleton<TrackZeroClient>(sp =>
            {
                return new TrackZeroClient(sp, sp.GetRequiredService<IHttpClientFactory>(), throwExcpetions);
            });
        }
    }
}
