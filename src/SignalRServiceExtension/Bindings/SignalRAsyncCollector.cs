// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public class SignalRAsyncCollector<T> : IAsyncCollector<T>
    {
        private readonly IAzureSignalRSender client;
        private readonly SignalROutputConverter converter;

        internal SignalRAsyncCollector(IAzureSignalRSender client)
        {
            this.client = client;
            converter = new SignalROutputConverter();
        }

        public async Task AddAsync(T item, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (item == null)
            {
                throw new ArgumentNullException("Binding Object");
            }

            var convertItem = converter.ConvertToSignalROutput(item);

            if (convertItem.GetType() == typeof(SignalRMessage))
            {
                SignalRMessage message = convertItem as SignalRMessage;
                var data = new SignalRData
                {
                    Target = message.Target,
                    Arguments = message.Arguments
                };

                if (!string.IsNullOrEmpty(message.ConnectionId))
                {
                    await client.SendToConnection(message.ConnectionId, data).ConfigureAwait(false);
                }
                else if (!string.IsNullOrEmpty(message.UserId))
                {
                    await client.SendToUser(message.UserId, data).ConfigureAwait(false);
                }
                else if (!string.IsNullOrEmpty(message.GroupName))
                {
                    await client.SendToGroup(message.GroupName, data).ConfigureAwait(false);
                }
                else
                {
                    await client.SendToAll(data).ConfigureAwait(false);
                }
            }
            else if (convertItem.GetType() == typeof(SignalRGroupAction))
            {
                SignalRGroupAction groupAction = convertItem as SignalRGroupAction;

                if (!string.IsNullOrEmpty(groupAction.ConnectionId))
                {
                    if (groupAction.Action == GroupAction.Add)
                    {
                        await client.AddConnectionToGroup(groupAction.ConnectionId, groupAction.GroupName).ConfigureAwait(false);
                    }
                    else
                    {
                        await client.RemoveConnectionFromGroup(groupAction.ConnectionId, groupAction.GroupName).ConfigureAwait(false);
                    }
                }
                else if (!string.IsNullOrEmpty(groupAction.UserId))
                {
                    if (groupAction.Action == GroupAction.Add)
                    {
                        await client.AddUserToGroup(groupAction.UserId, groupAction.GroupName).ConfigureAwait(false);
                    }
                    else
                    {
                        await client.RemoveUserFromGroup(groupAction.UserId, groupAction.GroupName).ConfigureAwait(false);
                    }
                }
                else
                {
                    throw new ArgumentException($"ConnectionId and UserId cannot be null or empty together");
                }
            }
            else
            {
                throw new ArgumentException("Unsupport Binding Type.");
            }
        }

        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }
    }
}