// <copyright file="IRulesEvaluationService.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/30/2025</date>
// <summary></summary>

using System.Threading;
using System.Threading.Tasks;
using GMS.TifoXRCoreWebAPI.Models;

namespace GMS.TifoXRCoreWebAPI.Services
{
    public interface IRulesEvaluationService
    {
        Task<RulesEngineEvaluationResponse> EvaluateAsync(RulesEngineEvaluationRequest request, CancellationToken cancellationToken = default);
        void Invalidate(int spaceId);
    }
}
