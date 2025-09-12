// <copyright file="PaymentGatewayRegistry.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>%UserDisplayName%</author>
// <date>9/8/2025</date>
// <summary>Class that handles payment gateway registration</summary>

using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using TifoXRCoreWebAPI.Application.PaymentGateways.Utils;

namespace GMS.TifoXRCoreWebAPI.Application.PaymentGateways
{
    public sealed class PaymentGatewayRegistry : IPaymentGatewayResolver
    {
        private readonly ConcurrentDictionary<string, IPaymentGateway> _byName =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<int, IPaymentGateway> _byId =
            new();

        public PaymentGatewayRegistry(
            IEnumerable<IPaymentGateway> gateways,
            IOptions<PaymentGatewayMapOptions> mapOptions,
            ILogger<PaymentGatewayRegistry> logger)
        {
            // register by name first
            foreach (var g in gateways)
                _byName[g.Name] = g; // e.g., "stripe", "paypal"

            var map = mapOptions?.Value?.IdToName ?? new();

            // sensible defaults if config missing
            if (map.Count == 0)
                map = new() { { 1, "stripe" }, { 2, "paypal" }, { 3, "crypto" } };

            // bind ids to impls via names
            foreach (var (id, name) in map)
            {
                if (_byName.TryGetValue(name, out var g))
                    _byId[id] = g;
                else
                    logger.LogWarning("Gateway name '{Name}' for id {Id} not registered (missing implementation).", name, id);
            }
        }

        public IPaymentGateway GetById(int gatewayId) =>
            _byId.TryGetValue(gatewayId, out var g)
                ? g
                : throw new KeyNotFoundException($"Gateway id {gatewayId} not registered.");

        public IPaymentGateway GetByName(string name) =>
            _byName.TryGetValue(name, out var g)
                ? g
                : throw new KeyNotFoundException($"Gateway '{name}' not registered.");
    }
}

