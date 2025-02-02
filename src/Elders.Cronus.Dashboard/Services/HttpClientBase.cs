﻿using System.Text.Json;

namespace Elders.Cronus.Dashboard.Models
{
    public abstract class HttpClientBase
    {
        public oAuth auth;
        private readonly HttpClient client;
        private readonly JsonSerializerOptions serializerOptions;

        public HttpClientBase(HttpClient client)
        {
            this.client = client;

            client.Timeout = TimeSpan.FromSeconds(45); // This is a temporary solution to the Commerce funnel timeouting and not providing a response for large periods of time (5min).

            serializerOptions = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        protected async Task<(HttpResponseMessage Response, T Data)> ExecuteRequestAsync<T>(HttpRequestMessage request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            if (request.Headers.Authorization is null && string.IsNullOrEmpty(auth?.Tenant) == false)
            {
                request.Headers.Add("tenant", auth.Tenant);
            }

            using (HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false))
            {
                if (response.IsSuccessStatusCode)
                {
                    using (var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        contentStream.Position = 0;
                        T responseObject = await JsonSerializer.DeserializeAsync<T>(contentStream, serializerOptions).ConfigureAwait(false);
                        return (response, responseObject);
                    }
                }
                else
                {
                    //string errorResponseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    //log.Error(errorResponseString)

                    return (response, default);
                }
            }
        }

        protected HttpRequestMessage CreateJsonPostRequest<T>(T model, string resource)
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, resource) { Content = new StringContent(JsonSerializer.Serialize(model), System.Text.Encoding.UTF8, ContentType.Json) };

            return httpRequestMessage;
        }

        internal static class ContentType
        {
            public static string Json = "application/json";
        }
    }
}
