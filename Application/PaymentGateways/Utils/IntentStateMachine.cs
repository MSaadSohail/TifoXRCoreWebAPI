// <copyright file="IntentStateMachine.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/12/2025</date>
// <summary></summary>


// <copyright file="IntentStateMachine.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/12/2025</date>
// <summary></summary>

using GMS.TifoXRCoreWebAPI.Utilities.Domain.Enums;

namespace TifoXRCoreWebAPI.Application.PaymentGateways.Utils
{
    public static class IntentStateMachine
    {
        public static PaymentIntentStatus OnCaptureStarted(PaymentIntentStatus s) =>
            s == PaymentIntentStatus.RequiresAction
                ? PaymentIntentStatus.Processing
                : throw new Exception( $"Capture can start only from RequiresAction (got {s}). {409}");

        public static PaymentIntentStatus OnCaptureFailed(PaymentIntentStatus s) =>
            s == PaymentIntentStatus.Processing
                ? PaymentIntentStatus.RequiresAction
                : throw new Exception($"Failure rollback only from Processing (got {s}). {409}");

        public static PaymentIntentStatus OnCaptureSucceeded(PaymentIntentStatus s) =>
            s == PaymentIntentStatus.Processing
                ? PaymentIntentStatus.Succeeded
                : throw new Exception($"Success only from Processing (got {s}). {409}");
    }
}
