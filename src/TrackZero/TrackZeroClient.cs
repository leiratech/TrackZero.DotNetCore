﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using TrackZero.Abstract;
using TrackZero.DataTransfer;

namespace TrackZero
{
    /// <summary>
    /// Main class to use TrackZero
    /// </summary>
    public sealed class TrackZeroClient
    {
        /// <summary>
        /// 
        /// </summary>
        //public static TrackZeroClient Instance { get; internal set; }
        private readonly IHttpClientFactory clientFactory;
        private readonly ILogger logger;
        private readonly bool throwExceptions;

        /// <summary>
        /// Creates a new Instance of TrackZeroClient
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="clientFactory"></param>
        /// <param name="throwExceptions"></param>
        public TrackZeroClient(IServiceProvider serviceProvider, IHttpClientFactory clientFactory, bool throwExceptions)
        {
            this.clientFactory = clientFactory;
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            this.logger = loggerFactory?.CreateLogger<TrackZeroClient>();
            this.throwExceptions = throwExceptions;
            //Instance = this;
        }

        /// <summary>
        /// Deletes an entity and all events it emitted.
        /// CAUTION : this action cannot be undone.
        /// </summary>
        /// <param name="type">The type of the entity to delete.</param>
        /// <param name="id">The id of the entity to delete.</param>
        /// <param name="analyticsSpaceId">The analytics space id that hosts this entity.</param>
        /// <returns></returns>
        public async Task<TrackZeroOperationResult<EntityReference>> DeleteEntityAsync(string type, object id, string analyticsSpaceId)
        {
            if (string.IsNullOrEmpty(analyticsSpaceId))
            {
                var ex = new ArgumentException($"'{nameof(analyticsSpaceId)}' cannot be null or empty.", nameof(analyticsSpaceId));
                if (throwExceptions)
                    throw ex;

                return new TrackZeroOperationResult<EntityReference>(ex);
            }

            HttpClient httpClient = clientFactory.CreateClient("TrackZero");
            try
            {

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Delete,
                    RequestUri = new Uri($"/tracking/entities?analyticsSpaceId={HttpUtility.UrlEncode(analyticsSpaceId)}", UriKind.Relative),
                    Content = new StringContent(JsonConvert.SerializeObject(new EntityReference(type, id)), Encoding.UTF8, "application/json"),

                };

                var response = await httpClient.SendAsync(request).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    logger?.LogInformation("Deleting Entity Type = {type}, id = {id} successful.", type, id);
                    return new TrackZeroOperationResult<EntityReference>(new EntityReference(type, id));
                }

                logger?.LogError("Deleting Entity Type = {type}, id = {id} failed with status code {code}.", type, id, response.StatusCode);
                return TrackZeroOperationResult<EntityReference>.GenericFailure;
            }
            catch (Exception ex)
            {
                logger?.LogCritical(ex, "Deleting Entity Type = {type}, id = {id} threw an exception.", type, id);

                if (throwExceptions)
                    throw;

                return new TrackZeroOperationResult<EntityReference>(ex);
            }
            finally
            {
                httpClient.Dispose();
            }
        }

        /// <summary>
        /// Deletes an entity
        /// CAUTION : this action cannot be undone.
        /// </summary>
        /// <param name="entityReference">The entity reference to delete.</param>
        /// <param name="analyticsSpaceId">The analytics space id that hosts this entity.</param>
        /// <returns></returns>
        public async Task<TrackZeroOperationResult<EntityReference>> DeleteEntityAsync(EntityReference entityReference, string analyticsSpaceId)
        {
            return await DeleteEntityAsync(entityReference.Type, entityReference.Id, analyticsSpaceId).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds a new entity if it doesn't exist (based on Id and Type) or updates existing one if it exists.
        /// </summary>
        /// <param name="entity">Entity to create. Any EntityReference in CustomAttributes will automatically be created if do not exist.</param>
        /// <param name="analyticsSpaceId">The analytics space id that will hosts this entity.</param>
        /// <returns>Returns the operation results indicating success or failure along with the Entity object.</returns>
        public async Task<TrackZeroOperationResult<Entity>> UpsertEntityAsync(Entity entity, string analyticsSpaceId)
        {
            if (string.IsNullOrEmpty(analyticsSpaceId))
            {
                var ex = new ArgumentException($"'{nameof(analyticsSpaceId)}' cannot be null or empty.", nameof(analyticsSpaceId));
                if (throwExceptions)
                    throw ex;

                return new TrackZeroOperationResult<Entity>(ex);
            }

            HttpClient httpClient = clientFactory.CreateClient("TrackZero");
            try
            {
                entity.ValidateAndCorrect();

                var response = await httpClient.PostAsync($"/tracking/entities?analyticsSpaceId={HttpUtility.UrlEncode(analyticsSpaceId)}", new StringContent(JsonConvert.SerializeObject(entity), Encoding.UTF8, "application/json")).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    logger?.LogInformation("Upsert Entity Type = {type}, id = {id} successful.", entity.Type, entity.Id);
                    return new TrackZeroOperationResult<Entity>(entity);
                }

                logger?.LogError("Upsert Entity Type = {type}, id = {id} failed with status code {code}.", entity.Type, entity.Id, response.StatusCode);
                logger?.LogError(await response.Content.ReadAsStringAsync());
                return TrackZeroOperationResult<Entity>.GenericFailure;
            }
            catch (Exception ex)
            {
                logger?.LogCritical(ex, "Upsert Entity Type = {type}, id = {id} threw an exception.", entity.Type, entity.Id);

                if (throwExceptions)
                    throw;

                return new TrackZeroOperationResult<Entity>(ex);
            }
            finally
            {
                httpClient.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="analyticsSpaceId">The analytics space id that will hosts this entity.</param>
        /// <returns></returns>
        [Obsolete("This method requires a special permission granted by TrackZero staff for special usecases. Please contact support@trackzero.io with details on your usecase.")]
        public async Task<TrackZeroOperationResult<Dictionary<BulkUpsertEntityError, int>>> UpsertEntityAsync(IEnumerable<Entity> entities, string analyticsSpaceId)
        {
            if (string.IsNullOrEmpty(analyticsSpaceId))
            {
                var ex = new ArgumentException($"'{nameof(analyticsSpaceId)}' cannot be null or empty.", nameof(analyticsSpaceId));
                if (throwExceptions)
                    throw ex;

                return new TrackZeroOperationResult<Dictionary<BulkUpsertEntityError, int>>(ex);
            }

            if (entities.Count() > 1000)
            {
                var ex = new ArgumentException($"'{nameof(entities)}' cannot contain more than 200 items.", nameof(entities));
                if (throwExceptions)
                    throw ex;

                return new TrackZeroOperationResult<Dictionary<BulkUpsertEntityError, int>>(ex);
            }

            HttpClient httpClient = clientFactory.CreateClient("TrackZero");
            try
            {
                foreach (var e in entities)
                    e.ValidateAndCorrect();

                httpClient.Timeout = TimeSpan.FromMinutes(10);

                var response = await httpClient.PostAsync($"tracking/entities/bulk?analyticsSpaceId={HttpUtility.UrlEncode(analyticsSpaceId)}", new StringContent(JsonConvert.SerializeObject(entities), Encoding.UTF8, "application/json")).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    logger?.LogInformation("-----------------------------------------------------------------------");
                    logger?.LogInformation(await response.Content.ReadAsStringAsync());
                    logger?.LogInformation("-----------------------------------------------------------------------");
                    logger?.LogInformation("Bulk Upsert of {count} Entities successfull.", entities.Count());
                    //var resultGroups = Newtonsoft.Json.JsonConvert.DeserializeObject<List<QueueEntityError>>(await response.Content.ReadAsStringAsync().ConfigureAwait(false)).GroupBy(t => t);

                    //Dictionary<QueueEntityError, int> parsedResponse = new Dictionary<QueueEntityError, int>();
                    //foreach (var group in resultGroups)
                    //{
                    //    parsedResponse.Add(group.Key, group.Count());
                    //}
                    return new TrackZeroOperationResult<Dictionary<BulkUpsertEntityError, int>>(new Dictionary<BulkUpsertEntityError, int>() { { BulkUpsertEntityError.None, entities.Count() } });
                }
                else
                {

                }

                logger?.LogInformation("-----------------------------------------------------------------------");
                string r = await response.Content.ReadAsStringAsync();
                logger?.LogInformation(r);
                logger?.LogInformation("-----------------------------------------------------------------------");
                logger?.LogError("Bulk Upsert of {count} Entities failed with status code {code}.", entities.Count(), response.StatusCode);
                return TrackZeroOperationResult<Dictionary<BulkUpsertEntityError, int>>.GenericFailure;
            }
            catch (Exception ex)
            {
                logger?.LogCritical(ex, "Bulk Upsert of {count} Entities threw an exception.", entities.Count());

                if (throwExceptions)
                    throw;

                return new TrackZeroOperationResult<Dictionary<BulkUpsertEntityError, int>>(ex);
            }
            finally
            {
                httpClient.Dispose();
            }
        }

        /// <summary>
        /// Creates a new Analytics Space
        /// </summary>
        /// <param name="analyticsSpaceId">The id of the analytics space to create.</param>
        /// <returns></returns>
        public async Task<TrackZeroOperationResult> CreateAnalyticsSpaceAsync(string analyticsSpaceId)
        {
            if (string.IsNullOrEmpty(analyticsSpaceId))
            {
                var ex = new ArgumentException($"'{nameof(analyticsSpaceId)}' cannot be null or empty.", nameof(analyticsSpaceId));
                if (throwExceptions)
                    throw ex;

                return new TrackZeroOperationResult(ex);
            }

            HttpClient httpClient = clientFactory.CreateClient("TrackZero");
            try
            {
                var response = await httpClient.PostAsync($"/AnalyticsSpaces?analyticsSpaceId={HttpUtility.UrlEncode(analyticsSpaceId)}", null).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return new TrackZeroOperationResult(true, null);
                }

                logger?.LogError("Unable to get session for analyticsSpaceId = {analyticsSpaceId}. Status Code = {StatusCode}", analyticsSpaceId, response.StatusCode);
                logger?.LogError(await response.Content.ReadAsStringAsync());
                return TrackZeroOperationResult.GenericFailure;
            }
            catch (Exception ex)
            {
                logger?.LogCritical(ex, "CreateAnalyticsSpaceSession threw an exception.");

                if (throwExceptions)
                    throw;

                return new TrackZeroOperationResult(ex);
                //return new TrackZeroOperationResult<Entity>(ex);
            }
            finally
            {
                httpClient.Dispose();
            }
        }

        /// <summary>
        /// Deletes the analytics space and all of its data.
        /// CAUTION: This operation is irreversable.
        /// </summary>
        /// <param name="analyticsSpaceId"></param>
        /// <returns></returns>
        public async Task<TrackZeroOperationResult> DeleteAnalyticsSpaceAsync(string analyticsSpaceId)
        {
            if (string.IsNullOrEmpty(analyticsSpaceId))
            {
                var ex = new ArgumentException($"'{nameof(analyticsSpaceId)}' cannot be null or empty.", nameof(analyticsSpaceId));
                if (throwExceptions)
                    throw ex;

                return new TrackZeroOperationResult(ex);
            }

            HttpClient httpClient = clientFactory.CreateClient("TrackZero");
            try
            {
                var response = await httpClient.DeleteAsync($"/AnalyticsSpaces?analyticsSpaceId={HttpUtility.UrlEncode(analyticsSpaceId)}").ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return new TrackZeroOperationResult(true, null);
                }

                logger?.LogError("Unable to get session for analyticsSpaceId = {analyticsSpaceId}. Status Code = {StatusCode}", analyticsSpaceId, response.StatusCode);
                logger?.LogError(await response.Content.ReadAsStringAsync());
                return TrackZeroOperationResult.GenericFailure;
            }
            catch (Exception ex)
            {
                logger?.LogCritical(ex, "CreateAnalyticsSpaceSession threw an exception.");

                if (throwExceptions)
                    throw;

                return new TrackZeroOperationResult(ex);
                //return new TrackZeroOperationResult<Entity>(ex);
            }
            finally
            {
                httpClient.Dispose();
            }
        }

        /// <summary>
        /// Creates a customer portal session.
        /// </summary>
        /// <param name="analyticsSpaceId">The analytics space id to allow access to.</param>
        /// <param name="sessionDuration">The session duration in seconds. This can be between 300 and 3600</param>
        /// <returns></returns>
        public async Task<TrackZeroOperationResult<AnalyticsSpacePortalSession>> CreateAnalyticsSpacePortalSessionAsync(string analyticsSpaceId, TimeSpan sessionDuration, bool @readonly = false, bool allowAnalyticsEmails = true)
        {
            if (string.IsNullOrEmpty(analyticsSpaceId))
            {
                var ex = new ArgumentException($"'{nameof(analyticsSpaceId)}' cannot be null or empty.", nameof(analyticsSpaceId));
                if (throwExceptions)
                    throw ex;
                return new TrackZeroOperationResult<AnalyticsSpacePortalSession>(ex);
            }

            if (sessionDuration.TotalSeconds < 300 || sessionDuration.TotalSeconds > 3600)
            {
                var ex = new ArgumentOutOfRangeException(nameof(sessionDuration), "sessionDuration must be between 5 and 60 minutes");
                if (throwExceptions)
                    throw ex;
                return new TrackZeroOperationResult<AnalyticsSpacePortalSession>(ex);
            }

            HttpClient httpClient = clientFactory.CreateClient("TrackZero");
            try
            {
                var response = await httpClient.GetAsync($"/analyticsSpaces/session?analyticsSpaceId={HttpUtility.UrlEncode(analyticsSpaceId)}&ttl={sessionDuration.TotalSeconds}&readonly={@readonly}&allowAnalyticsEmails={allowAnalyticsEmails}").ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return new TrackZeroOperationResult<AnalyticsSpacePortalSession>(JsonConvert.DeserializeObject<AnalyticsSpacePortalSession>(await response.Content.ReadAsStringAsync().ConfigureAwait(false)));
                }

                logger?.LogError("Unable to get session for analyticsSpaceId = {analyticsSpaceId}. Status Code = {StatusCode}", analyticsSpaceId, response.StatusCode);
                logger?.LogError(await response.Content.ReadAsStringAsync());
                return TrackZeroOperationResult<AnalyticsSpacePortalSession>.GenericFailure;
            }
            catch (Exception ex)
            {
                logger?.LogCritical(ex, "CreateAnalyticsSpaceSession threw an exception.");

                if (throwExceptions)
                    throw;

                return new TrackZeroOperationResult<AnalyticsSpacePortalSession>(ex);
                //return new TrackZeroOperationResult<Entity>(ex);
            }
            finally
            {
                httpClient.Dispose();
            }
        }
    }
}
