// <copyright file="PaymentController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Urvashi Dhingra</author>
// <date>08/29/2025</date>
// <summary>Controller to handle payment intents, charges, and refunds</summary>

using System;
using System.Threading.Tasks;
using GMS.TifoXRCoreWebAPI.Middleware;
using GMS.TifoXRCoreWebAPI.Middleware.Exceptions;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [Route("api/space")]
    [ApiController]
    public class PaymentController(IPaymentsRepository paymentsRepository) : ControllerBase
    {
        private readonly IPaymentsRepository _paymentsRepository = paymentsRepository;

        #region INTENTS: POST

        /// <summary>
        /// POST /api/space/{spaceId}/payments/intents
        /// Creates a payment intent (paypal | crypto_wallet | generic).
        /// </summary>
        [HttpPost("{spaceId}/payments/intents")]
        [ProducesResponseType(typeof(PaymentIntentData), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PaymentIntentData>> CreatePaymentIntent(
            [FromRoute] int spaceId,
            [FromBody] CreatePaymentIntentDto dto)
        {
            if (spaceId <= 0)
                throw new ArgumentException(GlobalException.FormatExceptionMessage(
                    "spaceId must be a positive integer.",
                    nameof(CreatePaymentIntent),
                    new { spaceId }));

            ArgumentNullException.ThrowIfNull(dto);

            if (string.IsNullOrWhiteSpace(dto.OrderId))
                throw new ArgumentException(GlobalException.FormatExceptionMessage(
                    "orderId is required.",
                    nameof(CreatePaymentIntent),
                    new { spaceId }), nameof(dto.OrderId));

            if (dto.PaymentGatewayId <= 0)
                throw new ArgumentException(GlobalException.FormatExceptionMessage(
                    "paymentGatewayId is required.",
                    nameof(CreatePaymentIntent),
                    new { spaceId }), nameof(dto.PaymentGatewayId));

            if (string.IsNullOrWhiteSpace(dto.IdempotencyKey))
                throw new ArgumentException(GlobalException.FormatExceptionMessage(
                    "idempotencyKey is required.",
                    nameof(CreatePaymentIntent),
                    new { spaceId }), nameof(dto.IdempotencyKey));

            var created = await _paymentsRepository.CreatePaymentIntentAsync(spaceId, dto);
            if (created is null)
                throw new InvalidOperationException(GlobalException.FormatExceptionMessage(
                    "Creation failed.",
                    nameof(CreatePaymentIntent),
                    new { spaceId, dto }));

            return CreatedAtAction(
                nameof(GetPaymentIntent),
                new { spaceId, intentId = created.Id },
                created
            );
        }

        #endregion

        #region INTENTS: GET

        /// <summary>
        /// GET /api/space/{spaceId}/payments/intents/{intentId}
        /// Returns a payment intent by id.
        /// </summary>
        [HttpGet("{spaceId}/payments/intents/{intentId}")]
        [ProducesResponseType(typeof(PaymentIntentData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PaymentIntentData>> GetPaymentIntent([FromRoute] int spaceId, [FromRoute] string intentId)
        {
            if (spaceId <= 0 || string.IsNullOrWhiteSpace(intentId))
                throw new ArgumentException(GlobalException.FormatExceptionMessage(
                    "spaceId must be positive and intentId is required.",
                    nameof(GetPaymentIntent),
                    new { spaceId, intentId }));

            var intent = await _paymentsRepository.GetPaymentIntentAsync(spaceId, intentId);

            return intent == null
                ? throw new ResourceNotFoundException(GlobalException.FormatExceptionMessage(
                    "Payment intent not found.",
                    nameof(GetPaymentIntent),
                    new { spaceId, intentId }))
                : (ActionResult<PaymentIntentData>)Ok(intent);
        }

        #endregion

        #region CHARGES: GET

        /// <summary>
        /// GET /api/space/{spaceId}/payments/charges/{chargeId}
        /// Returns a charge.
        /// </summary>
        [HttpGet("{spaceId}/payments/charges/{chargeId}")]
        [ProducesResponseType(typeof(PaymentChargeData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PaymentChargeData>> GetCharge([FromRoute] int spaceId, [FromRoute] string chargeId)
        {
            if (spaceId <= 0 || string.IsNullOrWhiteSpace(chargeId))
                throw new ArgumentException(GlobalException.FormatExceptionMessage(
                    "spaceId must be positive and chargeId is required.",
                    nameof(GetCharge),
                    new { spaceId, chargeId }));

            var charge = await _paymentsRepository.GetChargeAsync(spaceId, chargeId);

            return charge == null
                ? throw new ResourceNotFoundException(GlobalException.FormatExceptionMessage(
                    "Charge not found.",
                    nameof(GetCharge),
                    new { spaceId, chargeId }))
                : (ActionResult<PaymentChargeData>)Ok(charge);
        }

        #endregion

        #region REFUNDS: POST

        /// <summary>
        /// POST /api/space/{spaceId}/payments/refunds
        /// Creates a refund (no idempotency key in DTO, Teleport-style CreatedAtAction).
        /// </summary>
        [HttpPost("{spaceId}/payments/refunds")]
        [ProducesResponseType(typeof(PaymentRefundData), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PaymentRefundData>> CreateRefund(
            [FromRoute] int spaceId,
            [FromBody] CreateRefundDto dto)
        {
            if (spaceId <= 0)
                throw new ArgumentException(GlobalException.FormatExceptionMessage(
                    "spaceId must be a positive integer.",
                    nameof(CreateRefund),
                    new { spaceId }));

            ArgumentNullException.ThrowIfNull(dto);

            if (string.IsNullOrWhiteSpace(dto.PaymentChargeId) || dto.Amount <= 0)
                throw new ArgumentException(GlobalException.FormatExceptionMessage(
                    "paymentChargeId is required and amount must be positive.",
                    nameof(CreateRefund),
                    new { spaceId, dto.PaymentChargeId, dto.Amount }));

            var refund = await _paymentsRepository.CreateRefundAsync(spaceId, dto);

            return refund == null
                ? throw new InvalidOperationException(GlobalException.FormatExceptionMessage(
                    "Refund creation failed.",
                    nameof(CreateRefund),
                    new { spaceId, dto }))
                : (ActionResult<PaymentRefundData>)CreatedAtAction(
                    nameof(GetRefund),
                    new { spaceId, refundId = refund.Id },
                    refund);
        }

        #endregion

        #region REFUNDS: GET

        /// <summary>
        /// GET /api/space/{spaceId}/payments/refunds/{refundId}
        /// Returns a refund by id.
        /// </summary>
        [HttpGet("{spaceId}/payments/refunds/{refundId}")]
        [ProducesResponseType(typeof(PaymentRefundData), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PaymentRefundData>> GetRefund([FromRoute] int spaceId, [FromRoute] string refundId)
        {
            if (spaceId <= 0 || string.IsNullOrWhiteSpace(refundId))
                throw new ArgumentException(GlobalException.FormatExceptionMessage(
                    "spaceId must be positive and refundId is required.",
                    nameof(GetRefund),
                    new { spaceId, refundId }));

            var refund = await _paymentsRepository.GetRefundAsync(spaceId, refundId);

            return refund == null
                ? throw new ResourceNotFoundException(GlobalException.FormatExceptionMessage(
                    "Refund not found.",
                    nameof(GetRefund),
                    new { spaceId, refundId }))
                : (ActionResult<PaymentRefundData>)Ok(refund);
        }

        #endregion
    }
}
