/* Copyright StatsBro.io and/or licensed to StatsBro.io under one
 * or more contributor license agreements.
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the Server Side Public License, version 1

 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * Server Side Public License for more details.

 * You should have received a copy of the Server Side Public License
 * along with this program. If not, see
 * <https://github.com/StatsBro/statsbro/blob/main/LICENSE>.
 */
using StatsBro.Domain.Exceptions;
using StatsBro.Domain.Models;
using StatsBro.Domain.Models.DTO;
using StatsBro.Domain.Service.PayU;
using StatsBro.Host.Panel.Models.Objects;
using StatsBro.Host.Panel.Services;
using StatsBro.Storage.Database;

namespace StatsBro.Host.Panel.Logic;

public class PaymentLogic
{
    public const int ProlongSubscriptionDays = 30;

    private readonly IDbRepository _repository;
    private readonly INotificationService _notification;
    private readonly PayuService _payuService;
    private readonly ILogger<PaymentLogic> _logger;

    public PaymentLogic(
        PayuService payuService,
        IDbRepository repository, 
        INotificationService notification,
        ILogger<PaymentLogic> logger)
    {
        _payuService = payuService;
        _repository = repository;
        _notification = notification;
        _logger = logger;
    }

    public async Task<MakePaymentResult> MakePaymentAsync(
        PurchaseItem purchaseItem, 
        PurchasePayload payload)
    {
        var user = await _repository.GetUserAsync(payload.UserId);
        var org = await _repository.GetOrganizationAsync(payload.OrganizationId);
        var paymentId = _repository.NewId();

        var continueUrl =
            string.IsNullOrWhiteSpace(payload.ThankYouPageUrl)
            ?
            ""
            :
            payload.ThankYouPageUrl.Replace("PAYMENT_ID_PLACEHOLDER", paymentId.ToString(), StringComparison.InvariantCultureIgnoreCase);

        var address = await _repository.GetOrganizationAddressLatestAsync(payload.OrganizationId);
        if (address == null)
        {
            throw new Exception("not able to process payment, address is missing");
        }

        var nameSplit = address.Name.Split(' ');
        var first = nameSplit[0];
        var second = "";
        if (nameSplit.Length > 1)
        {
            second = String.Join(" ", nameSplit[1..]);
        }

        var orderCreateResponse = await this._payuService.CreateOrderAsync(
            ourRefId: paymentId.ToString(),
            purchaseItem.Name, 
            new[] { purchaseItem },
            payload.CardToken,
            isFirsTimePayment: true,
            continueUrl: continueUrl,
            email: user!.Email,
            firstName: first,
            lastName: second,
            city: address.City,
            addressLine1: address.AddressLine1,
            postalCode: address.PostalCode,
            ip: payload.ClientIp);

        var transaction = await _repository.BeginTransactionAsync();
        try
        {
            await this._repository.CreatePaymentAsync(
                new Domain.Models.DTO.PaymentDTO
                {
                    Id = paymentId.ToString(),
                    IdFromProvider = orderCreateResponse.OrderId,
                    OrganizationId = payload.OrganizationId.ToString(),
                    Provider = payload.Provider,
                    Source = PaymentSource.Manual,
                    Status = PaymentStatus.Processing,
                    UserId = payload.UserId.ToString(),
                }, transaction);

            await this._repository.CreateCreditCardTokenAsync(
                new Domain.Models.DTO.CreaditCardTokenDTO
                {
                    OrganizationId = payload.OrganizationId,
                    UserId = payload.UserId,
                    Provider = payload.Provider,
                    Token = orderCreateResponse.PayMethods.PayMethod.Value,
                    CardNumberMasked = orderCreateResponse.PayMethods.PayMethod.Card.Number,
                    CardExpirationMonth = orderCreateResponse.PayMethods.PayMethod.Card.ExpirationMonth,
                    CardExpirationYear = orderCreateResponse.PayMethods.PayMethod.Card.ExpirationYear,
                }, transaction);

            await this._repository.CreatePaymentTransactionAsync(
                new Domain.Models.DTO.PaymentTransactionDTO
                {
                    PaymentId = paymentId.ToString(),
                    AmountGross = payload.TotalAmountGross,
                    AmountNet = payload.TotalAmountNet,
                    VatAmount = payload.TotalVatAmount,
                    VatValue = payload.VatValue,
                    Currency = payload.Currency,
                    OrganizationId = payload.OrganizationId.ToString(),
                    Status = PaymentTransactionStatus.New,
                    SubscriptionType = payload.SubscriptionType,
                    SubscriptionContinueTo = DateTime.UtcNow.Date.AddDays(ProlongSubscriptionDays + 1).AddMilliseconds(-1),
                }, transaction);

            await _repository.CommitTransactionAsync(transaction);

            return new MakePaymentResult
            {
                RedirectUri = orderCreateResponse.RedirectUri
            };
        }
        catch (Exception)
        {
            await _repository.RollbackTransactionAsync(transaction);            
            throw;
        }
    }

    public async Task CancelSubscriptionAsync(Guid organizationId)
    {
        var organization = await this._repository.GetOrganizationAsync(organizationId);
        if(organization.IsSubscriptionCancelled)
        {
            this._logger.LogWarning("It was requested to cancel subscription for {organizationId} but the subscription is already cancelled.", organizationId);
            return;
        }

        var transaction = await _repository.BeginTransactionAsync();
        try
        {
            await this._repository.SetOrganizationSubscriptionCancelledAsync(organizationId, transaction);
            await this._repository.ClearOrganizationCreaditCardTokensAsync(organizationId, transaction);
            await _repository.CommitTransactionAsync(transaction);
        }
        catch(Exception)
        {
            await _repository.RollbackTransactionAsync(transaction);
            throw;
        }
    }

    public async Task<Domain.Models.DTO.PaymentDTO?> CheckPaymentStatusAsync(Guid orgId, Guid paymentId)
    {
        var payment = await this._repository.GetPaymentAsync(orgId, paymentId);
        if (payment == null)
        {
            return null;
        }

        if(payment.Status == PaymentStatus.Processing)
        {
            payment = await SyncPaymentStatusWithProviderAsync(payment);
        }

        return payment;
    }

    private async Task<Domain.Models.DTO.PaymentDTO> SyncPaymentStatusWithProviderAsync(Domain.Models.DTO.PaymentDTO payment)
    {
        var order = await _payuService.GetOrderAsync(payment.IdFromProvider);
        if (order == null)
        {
            return payment;
        }

        // we only have PayU
        var transaction = await _repository.BeginTransactionAsync();
        try
        {
            switch (order.Status)
            {
                case PayuService.PaymentStatusCanceled:
                    payment.Status = PaymentStatus.Cancelled;
                    await _repository.SavePaymentAsync(payment, transaction);
                    await _repository.SetPaymentTransactionStatusAsync(payment.Id, PaymentTransactionStatus.Cancelled, transaction);
                    break;
                case PayuService.PaymentStatusCompleted:
                    payment.Status = PaymentStatus.Completed;
                    await _repository.SavePaymentAsync(payment, transaction);
                    await _repository.SetPaymentTransactionStatusAsync(payment.Id, PaymentTransactionStatus.Paid, transaction);
                    var pt = await _repository.GetPaymentTransactionAsync(payment.Id, transaction);
                    await _repository.SetOrganizationSubscriptionAsync(pt.OrganizationId, pt.SubscriptionType, pt.SubscriptionContinueTo, transaction);
                    break;
            }

            await _repository.CommitTransactionAsync(transaction);
        }
        catch (Exception)
        {
            await _repository.RollbackTransactionAsync(transaction);
            throw;
        }

        return payment;
    }
    /*
     Headers from PayU:
        PayU-Processing-Time: 1000                                     // dla wybranych statusów
        Content-Type: application/json;charset=UTF-8
        User-Agent: Jakarta Commons-HttpClient/3.1
        Content-Length: 100
        Authorization: Basic MTIzNDU2Nzg6QUJDREVGR0hJSktMTU5PUFFSU1RVVldYWVo=
        OpenPayu-Signature: sender=checkout;signature=d47d8a771d558c29285887febddd9327;algorithm=MD5;content=DOCUMENT
        X-OpenPayU-Signature: sender=checkout;signature=d47d8a771d558c29285887febddd9327;algorithm=MD5;content=DOCUMENT
    */

    public async Task HandleNotificationAsync(string id, string content, IHeaderDictionary headers, string? ip)
    {
        // we only support PayU now, so there will be no logic on provider selection

        if (!Guid.TryParse(id, out Guid paymentId))
        {
            throw new PaymentNotificationException("given id does not look like Guid");
        }

        if (string.IsNullOrEmpty(content))
        {
            throw new PaymentNotificationException("content is empty");
        }

        if (_payuService.IsNotificationFromMe(headers, ip))
        {
            var notification = _payuService.GetDataFromNotificationContent(content, headers);                
            if (notification == null)
            {
                // TODO: content may contain sensitive data, remove it from logs
                throw new PaymentNotificationException($"was not able to get data from notifiaction content for PayU, content: {content}");
            }

            if (notification.NotificationType == NotificationType.Payment
                && (notification.Status == PaymentStatus.Completed || notification.Status == PaymentStatus.Cancelled))
            {
                // get payu orderID
                var payment = await _repository.GetPaymentAsync(paymentId, notification.IdFromProvider);
                if (payment == null)
                {
                    throw new PaymentNotificationException($"payment from notification not found: paymentId: {paymentId}, idFromProvider: {notification.IdFromProvider}");
                }

                await this.SyncPaymentStatusWithProviderAsync(payment);

            }
            else if(notification.Status == PaymentStatus.Processing) 
            {
                return;
            }
            else
            {
                _logger.LogInformation("I am not processing notification: {object}", Newtonsoft.Json.JsonConvert.SerializeObject(notification));
            }           

            return;
        }

        _logger.LogWarning("no notification handling was done for incoming notification");
    }
}
