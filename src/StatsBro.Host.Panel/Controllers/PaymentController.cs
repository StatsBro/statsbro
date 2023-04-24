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
﻿namespace StatsBro.Host.Panel.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using StatsBro.Domain.Models;
using StatsBro.Domain.Service.PayU;
using StatsBro.Host.Panel.Extensions;
using StatsBro.Host.Panel.Logic;
using StatsBro.Host.Panel.Models;
using StatsBro.Host.Panel.Models.Forms;

// TODO: localization tutorial: https://www.yogihosting.com/globalization-localization-resource-files-aspnet-core/

[Authorize(Roles = "Admin")]

public class PaymentController : Controller
{
    private readonly ILogger<PaymentController> _logger;
    private readonly UserLogic _userLogic;
    private readonly PaymentLogic _paymentLogic;
    private readonly OrganizationLogic _organizationLogic;
    private readonly IStringLocalizer<PaymentController> _localizer;
    private readonly string _payUClientId;
    private readonly ISubscriptionPlanGuard _subscriptionPlanGuard;
    
    public PaymentController(
        ILogger<PaymentController> logger,
        UserLogic userLogic,
        PaymentLogic paymentLogic,
        OrganizationLogic organizationLogic,
        IOptions<PayuConfig> payuSettingsOptions,
        IStringLocalizer<PaymentController> localizer,
        ISubscriptionPlanGuard subscriptionPlanGuard)
    {
        _logger = logger;
        _userLogic = userLogic;
        _paymentLogic = paymentLogic;
        _organizationLogic = organizationLogic;
        _localizer = localizer;
        _payUClientId = payuSettingsOptions.Value.ClientId;
        _subscriptionPlanGuard = subscriptionPlanGuard;
    }

    public async Task<IActionResult> IndexAsync()
    {
        var orgId = User.GetOrganizationId();
        var userId = User.GetUserId();
        var organization = await _organizationLogic.GetOrganizationAsync(orgId);
        var isSubscrptionExpired = _subscriptionPlanGuard.IsSubscriptionPlanExpired(organization);

        var model = new PaymentIndexModel
        {
            SubscriptionType = organization.SubscriptionType,
            SubscriptionValidTo = organization.SubscriptionValidTo,
            IsSubscriptionCancelled = organization.IsSubscriptionCancelled,
            SubscriptionName = ResolveSubscriptionName(organization.SubscriptionType),
            IsSubscriptionValid = isSubscrptionExpired,
            EligibleForSubscriptionTypes = await _subscriptionPlanGuard.GetEligibleSubscriptionPlansAsync(orgId, userId)
        };

        return View(model);
    }

    public IActionResult PaymentForm()
    {
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> CancelSubscriptionAsync()
    {
        var orgId = User.GetOrganizationId();
        await _paymentLogic.CancelSubscriptionAsync(orgId);

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlanSelected(SubscriptionType option)
    {
        var subscription = SubscriptionPlanDetails.Subscription(option);
        if (subscription == null)
        {
            return NotFound();
        }

        var orgId = User.GetOrganizationId();
        var invoiceAddressData = new InvoiceDataFormModel();
        var currentAddress = await _organizationLogic.GetOrganizationAddressAsync(orgId);
        if (currentAddress != null)
        {
            invoiceAddressData.AddressLine1 = currentAddress.AddressLine1;
            invoiceAddressData.Name = currentAddress.Name;
            invoiceAddressData.City = currentAddress.City;
            invoiceAddressData.NIP = currentAddress.NIP;
            invoiceAddressData.PostalCode = currentAddress.PostalCode;
        }

        var model = new PaymentFormModel
        {
            PayUClientId = this._payUClientId,
            PriceNet = subscription.PriceNett,
            Currency = subscription.Currency,
            SubscriptionType = subscription.SubscriptionType,
            Name = ResolveSubscriptionName(option),
            InviceAddressData = invoiceAddressData,
        };

        return View("PaymentForm", model);
    }

    [HttpPost]
    public async Task<IActionResult> ProceedPaymentAsync([FromBody] ProceedPaymentModel model)
    {
        var orgId = User.GetOrganizationId();
        var userId = User.GetUserId();

        var subscription = SubscriptionPlanDetails.Subscription(model.SubscriptionType);
        if (subscription == null
            || subscription.SubscriptionType == SubscriptionType.Trial)
        {
            _logger.LogWarning("organizationId: {organizationId} subscription plan not found, user passed {subscriptionType}", orgId, model.SubscriptionType);
            return NotFound();
        }

        if (!ModelState.IsValid 
            || string.IsNullOrWhiteSpace(model.CardToken) 
            || model.CardToken.Length < 10)
        {
            _logger.LogWarning("Invalid data passed during {methodName}, displaying INDEX", nameof(ProceedPaymentAsync));
            ModelState.AddModelError("validation","wrong card data");
            return BadRequest(ModelState);
        }

        var eligibleSubscriptions = await _subscriptionPlanGuard.GetEligibleSubscriptionPlansAsync(orgId, userId);
        if (!eligibleSubscriptions.Contains(model.SubscriptionType))
        {
            _logger.LogWarning("Invalid data passed during {methodName}, displaying INDEX, eligibleSubscriptions did not contain subscription: {subscription}", nameof(ProceedPaymentAsync), model.SubscriptionType.ToString());
            ModelState.AddModelError("validation", "wrong subscription option");
            return BadRequest(ModelState);
        }

        // TODO: This should go go Logic, also because we need to add recur payments logic
        var calculations = new PaymentFormModel 
        {
            PriceNet = subscription.PriceNett,
            VATValue = 23
        };

        var purchaseItem = new PurchaseItem 
        {
            Name = ResolveSubscriptionName(model.SubscriptionType),
            Price = calculations.TotalAmount,
            Quantity = 1
        };

        var providerPayload = new PurchasePayload {
            Provider = model.Provider,
            CardToken = model.CardToken,
            SubscriptionType = model.SubscriptionType,
            UserId = userId,
            OrganizationId = orgId,
            ThankYouPageUrl = Url.Action(nameof(ThankYou), new { paymentId = "PAYMENT_ID_PLACEHOLDER"})!,
            VatValue = calculations.VATValue,
            TotalAmountNet = calculations.PriceNet,
            TotalAmountGross = calculations.TotalAmount,
            TotalVatAmount = calculations.TotalVATAmount,
            Currency = subscription.Currency,
            ClientIp = Request.GetClientIp(),
        };

        var newPayment = await this._paymentLogic.MakePaymentAsync(purchaseItem, providerPayload);

        return Ok(newPayment);
    }

    [HttpPost()]
    public async Task<IActionResult> SaveInvoiceDataAsync([FromForm]InvoiceDataFormModel model)
    {
        if(!ModelState.IsValid)
        {
            _logger.LogWarning("failed in {method}, data was not valid, errors: {errors}", nameof(SaveInvoiceDataAsync), String.Join(", ", ModelState.Select(x => x.Value)));
            return BadRequest(ModelState);
        }

        var orgId = User.GetOrganizationId();
        await _organizationLogic.SaveOrganizationAddressAsync(orgId, model);

        return Ok();
    }

    [HttpGet("/thankyou/{paymentId}")]
    public async Task<IActionResult> ThankYou(
        string paymentId,
        [FromQuery]string statusCode,
        [FromQuery]string refReqId)
    {
        if (!Guid.TryParse(paymentId, out var thePaymentId))
        { 
            return NotFound();
        }

        var orgId = User.GetOrganizationId();
        var payment = await this._paymentLogic.CheckPaymentStatusAsync(orgId, thePaymentId);
        if (payment == null)
        {
            return NotFound();
        }

        var model = new PaymentThankYouModel
        {
            StatusCode = statusCode,
            RefReqId = refReqId,
            PaymentStatus = payment.Status,
            PaymentId = paymentId,
        };

        return View(model);
    }

    private string ResolveSubscriptionName(SubscriptionType option)
    {
        // TODO: should depend on user language
        // TODO: localization tutorial: https://www.yogihosting.com/globalization-localization-resource-files-aspnet-core/
        return option switch
        {
            SubscriptionType.Personal => "Subskrypcja StatsBro - pakiet Personal 30 dni",
            SubscriptionType.Business => "Subskrypcja StatsBro - pakiet Business 30 dni",
            SubscriptionType.Enterprise => "Subskrypcja StatsBro - pakiet Enterprise 30 dni",
            SubscriptionType.Trial => "StatsBro - okres próbny",
            _ => "",
        };
    }
}
