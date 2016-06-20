﻿using System;
using System.Collections.Generic;
using Gauge.CSharp.Core;
using Gauge.Messages;

namespace NGauge.Specs.Reader
{
    public sealed class GaugeSpecificationsService : IGaugeSpecificationsService
    {
        private readonly GaugeApiConnection _connection;

        public GaugeSpecificationsService(GaugeApiConnection connection)
        {
            _connection = connection;
        }

        IEnumerable<ProtoSpec> IGaugeSpecificationsService.GetSpecs()
        {
            return GetSpecsFromGauge(_connection);
        }

        private static IEnumerable<ProtoSpec> GetSpecsFromGauge(GaugeApiConnection apiConnection)
        {
            var specsRequest = GetAllSpecsRequest.DefaultInstance;

            var apiMessage = APIMessage.CreateBuilder()
                .SetMessageId(GenerateMessageId())
                .SetMessageType(APIMessage.Types.APIMessageType.GetAllSpecsRequest)
                .SetAllSpecsRequest(specsRequest)
                .Build();

            var apiResponse = apiConnection.WriteAndReadApiMessage(apiMessage);

            return apiResponse
                .AllSpecsResponse
                .SpecsList;
        }

        private static long GenerateMessageId()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }
    }
}