﻿// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace Dapr.Client.Test
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Grpc.Core;
    using Grpc.Net.Client;
    using Moq;
    using Xunit;
    using Autogenerated = Dapr.Client.Autogen.Grpc.v1;

    public class SecretApiTest
    {
        [Fact]
        public async Task GetSecretAsync_ValidateRequest()
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var metadata = new Dictionary<string, string>();
            metadata.Add("key1", "value1");
            metadata.Add("key2", "value2");
            var task = daprClient.GetSecretAsync("testStore", "test_key", metadata);

            // Get Request and validate
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            var request = await GrpcUtils.GetRequestFromRequestMessageAsync<Autogenerated.GetSecretRequest>(entry.Request);
            request.StoreName.Should().Be("testStore");
            request.Key.Should().Be("test_key");
            request.Metadata.Count.Should().Be(2);
            request.Metadata.Keys.Contains("key1").Should().BeTrue();
            request.Metadata.Keys.Contains("key2").Should().BeTrue();
            request.Metadata["key1"].Should().Be("value1");
            request.Metadata["key2"].Should().Be("value2");
        }

        [Fact]
        public async Task GetSecretAsync_ReturnSingleSecret()
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var metadata = new Dictionary<string, string>();
            metadata.Add("key1", "value1");
            metadata.Add("key2", "value2");
            var task = daprClient.GetSecretAsync("testStore", "test_key", metadata);

            // Get Request and validate
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            var request = await GrpcUtils.GetRequestFromRequestMessageAsync<Autogenerated.GetSecretRequest>(entry.Request);
            request.StoreName.Should().Be("testStore");
            request.Key.Should().Be("test_key");
            request.Metadata.Count.Should().Be(2);
            request.Metadata.Keys.Contains("key1").Should().BeTrue();
            request.Metadata.Keys.Contains("key2").Should().BeTrue();
            request.Metadata["key1"].Should().Be("value1");
            request.Metadata["key2"].Should().Be("value2");

            // Create Response & Respond
            var secrets = new Dictionary<string, string>();
            secrets.Add("redis_secret", "Guess_Redis");
            await SendResponseWithSecrets(secrets, entry);

            // Get response and validate
            var secretsResponse= await task;
            secretsResponse.Count.Should().Be(1);
            secretsResponse.ContainsKey("redis_secret").Should().BeTrue();
            secretsResponse["redis_secret"].Should().Be("Guess_Redis");
        }

        [Fact]
        public async Task GetSecretAsync_ReturnMultipleSecrets()
        {
            // Configure Client
            var httpClient = new TestHttpClient();
            var daprClient = new DaprClientBuilder()
                .UseGrpcChannelOptions(new GrpcChannelOptions { HttpClient = httpClient })
                .Build();

            var metadata = new Dictionary<string, string>();
            metadata.Add("key1", "value1");
            metadata.Add("key2", "value2");
            var task = daprClient.GetSecretAsync("testStore", "test_key", metadata);

            // Get Request and validate
            httpClient.Requests.TryDequeue(out var entry).Should().BeTrue();
            var request = await GrpcUtils.GetRequestFromRequestMessageAsync<Autogenerated.GetSecretRequest>(entry.Request);
            request.StoreName.Should().Be("testStore");
            request.Key.Should().Be("test_key");
            request.Metadata.Count.Should().Be(2);
            request.Metadata.Keys.Contains("key1").Should().BeTrue();
            request.Metadata.Keys.Contains("key2").Should().BeTrue();
            request.Metadata["key1"].Should().Be("value1");
            request.Metadata["key2"].Should().Be("value2");

            // Create Response & Respond
            var secrets = new Dictionary<string, string>();
            secrets.Add("redis_secret", "Guess_Redis");
            secrets.Add("kafka_secret", "Guess_Kafka");
            await SendResponseWithSecrets(secrets, entry);

            // Get response and validate
            var secretsResponse = await task;
            secretsResponse.Count.Should().Be(2);
            secretsResponse.ContainsKey("redis_secret").Should().BeTrue();
            secretsResponse["redis_secret"].Should().Be("Guess_Redis");
            secretsResponse.ContainsKey("kafka_secret").Should().BeTrue();
            secretsResponse["kafka_secret"].Should().Be("Guess_Kafka");
        }

        [Fact]
        public async Task GetSecretAsync_WithCancelledToken()
        {
            // Configure Client
            var client = new MockClient();
            var response = 
                client.SetSecrets<string>()
                .Build();

            const string rpcExceptionMessage = "Call canceled by client";
            const StatusCode rpcStatusCode = StatusCode.Cancelled;
            const string rpcStatusDetail = "Call canceled";

            var rpcStatus = new Status(rpcStatusCode, rpcStatusDetail);
            var rpcException = new RpcException(rpcStatus, new Metadata(), rpcExceptionMessage);

            // Setup the mock client to throw an Rpc Exception with the expected details info
            client.Mock
                .Setup(m => m.GetSecretAsync(It.IsAny<Autogen.Grpc.v1.GetSecretRequest>(), It.IsAny<CallOptions>()))
                .Throws(rpcException);

            var ctSource = new CancellationTokenSource();
            CancellationToken ct = ctSource.Token;
            ctSource.Cancel();
            var task = client.DaprClient.GetSecretAsync("testStore", "test_key", new Dictionary<string, string>(), cancellationToken:ct);
            (await FluentActions.Awaiting(async () => await task).Should().ThrowAsync<OperationCanceledException>()).WithInnerException<Grpc.Core.RpcException>();
        }

        private async Task SendResponseWithSecrets(Dictionary<string, string> secrets, TestHttpClient.Entry entry)
        {
            var secretResponse = new Autogenerated.GetSecretResponse();
            secretResponse.Data.Add(secrets);

            var streamContent = await GrpcUtils.CreateResponseContent(secretResponse);
            var response = GrpcUtils.CreateResponse(HttpStatusCode.OK, streamContent);
            entry.Completion.SetResult(response);
        }
    }
}
