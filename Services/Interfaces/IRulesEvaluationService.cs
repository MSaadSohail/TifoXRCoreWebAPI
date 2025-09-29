// <copyright file="IRulesEvaluationService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public interface IRulesEvaluationService
    {
        Task<RulesEngineEvaluationResponse> EvaluateAsync(RulesEngineEvaluationRequest request, CancellationToken cancellationToken = default);
    }
}
