// <copyright file="PaymentIntentStatus.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Saad Sohail</author>
// <date>9/12/2025</date>
// <summary></summary>

namespace GMS.TifoXRCoreWebAPI.Utilities.Domain.Enums
{
    public enum PaymentIntentStatus
    {
        RequiresAction = 1,
        Processing = 2,
        Succeeded = 3
    }

    public enum RefundStatus
    {
        Pending = 1,
        Succeeded = 2,
        Failed = 3,
        Canceled = 4
    }
}
