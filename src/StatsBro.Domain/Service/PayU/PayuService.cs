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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StatsBro.Domain.Service.PayU.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http.Headers;
using StatsBro.Domain.Models;
using System.Net.Http.Json;
using StatsBro.Domain.Exceptions;
using System.Text.Json;
using Microsoft.Extensions.Primitives;

namespace StatsBro.Domain.Service.PayU
{
    public class PayuService : IDisposable
    {
        public const string PaymentStatusCompleted = "COMPLETED";
        
        public const string PaymentStatusCanceled = "CANCELED";

        public const string HttpClientName = "PayU";

        private readonly PayuConfig _settings;

        private readonly IHttpClientFactory _clientFactory;

        private readonly ILogger<PayuService> _logger;

        private readonly object syncRoot = new object();

        private readonly Uri _continueUriBase;

        private readonly Uri? _notifyUrl = null;
                
        public PayuService(
            IOptions<PayuConfig> payuSettingsOptions, 
            IHttpClientFactory clientFactory, 
            ILogger<PayuService> logger)
        {
            _settings = payuSettingsOptions.Value;
            _clientFactory = clientFactory;
            _logger = logger;
            _continueUriBase = new Uri(this._settings.ContinueUrl);
            _notifyUrl = string.IsNullOrWhiteSpace(this._settings.NotifyUrl) ? null : new Uri(this._settings.NotifyUrl);
        }

        public async Task<string> ListPaymentMethodsAsync()
        {
            var response = await ClientAuthenticated.GetAsync("/api/v2_1/paymethods");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<CreateOrderPayUResponse> CreateOrderAsync(
            string ourRefId,
            string description,
            IEnumerable<PurchaseItem> items,
            string cardToken,
            bool isFirsTimePayment,
            string continueUrl,
            string email,
            string firstName,
            string lastName,
            string city,
            string addressLine1,
            string postalCode,
            string? ip,
            string language = "pl"
            )
        {
            var request = new CreateOrderRequest
            {
                Description = description,
                MerchantPosId = _settings.ClientId,
                TotalAmount = NumberToString(items.Sum(x => x.Quantity * x.Price)),
                ExtOrderId = ourRefId,
                ContinueUrl = string.IsNullOrWhiteSpace(continueUrl) ? "" : new Uri(_continueUriBase, continueUrl).ToString(),
                NotifyUrl = _notifyUrl == null ? null : new Uri(_notifyUrl, ourRefId).ToString(),
                Recurring = isFirsTimePayment ? "FIRST" : "STANDARD",
                Buyer = new CreateOrderBuyer { 
                  Email = email,
                  FirstName = firstName,
                  LastName = lastName,
                  Language = language,
                  Delivery = new CreateOrderBuyerDelivery { 
                      City = city,
                      PostalCode = postalCode,
                      Street = addressLine1,                      
                  }
                },
                PayMethods = new PayMethods { 
                    PayMethod = new PayMethod { Value = cardToken }
                }
            };

            if (!string.IsNullOrWhiteSpace(ip))
            {
                request.CustomerIp = ip;
            }

            request.Products.AddRange(items.Select(x => new CreateOrderProduct(x.Name, NumberToString(x.Price * x.Quantity))));
            var response = await ClientAuthenticated.PostAsJsonAsync("/api/v2_1/orders", request, options: new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if (response.StatusCode == System.Net.HttpStatusCode.Found
                || response.StatusCode == System.Net.HttpStatusCode.Created
                || response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var responseObject = await response.Content.ReadFromJsonAsync<CreateOrderPayUResponse>();
                return responseObject!;
            }

            var errorGuid = System.Guid.NewGuid().ToString("N");
            _logger.LogError($"{errorGuid} FAILD creating new order in PayU, status code was: {response.StatusCode}, response: {await response.Content.ReadAsStringAsync()}");
            throw new PaymentException($"Something went wrong while creating PayU Order, check in logs under guid: {errorGuid}");
        }

        public async Task<Order?> GetOrderAsync(string orderId)
        {
            var response = await ClientAuthenticated.GetFromJsonAsync<GetOrderResponse>($"/api/v2_1/orders/{orderId}");
            return response!.Orders.SingleOrDefault();
        }

        public async Task DeleteTokenAsync(string token)
        {
            var response = await ClientAuthenticated.DeleteAsync($"/api/v2_1/tokens/{token}");
            response.EnsureSuccessStatusCode();
        }

        /* headers
               PayU-Processing-Time: 1000                                     // dla wybranych statusów
              Content-Type: application/json;charset=UTF-8
              User-Agent: Jakarta Commons-HttpClient/3.1
              Content-Length: 100
              Authorization: Basic MTIzNDU2Nzg6QUJDREVGR0hJSktMTU5PUFFSU1RVVldYWVo=
              OpenPayu-Signature: sender=checkout;signature=d47d8a771d558c29285887febddd9327;algorithm=MD5;content=DOCUMENT
              X-OpenPayU-Signature: sender=checkout;signature=d47d8a771d558c29285887febddd9327;algorithm=MD5;content=DOCUMENT
         /* 


        /*        
        { 
           "order":{ 
              "orderId":"LDLW5N7MF4140324GUEST000P01", 
              "extOrderId":"Id zamówienia w Twoim sklepie", 
              "orderCreateDate":"2012-12-31T12:00:00", 
              "notifyUrl":"http://tempuri.org/notify", 
              "customerIp":"127.0.0.1", 
              "merchantPosId":"{Id punktu płatności (pos_id)}", 
              "description":"Twój opis zamówienia", 
              "currencyCode":"PLN", 
              "totalAmount":"200", 
              "buyer":{ 
                 "email":"john.doe@example.org", 
                 "phone":"111111111", 
                 "firstName":"John", 
                 "lastName":"Doe",
                 "language":"pl"
              },
              "payMethod": {
                 "type": "PBL" //lub "CARD_TOKEN", "INSTALLMENTS"
              },
              "products":[
                 { 
                       "name":"Product 1", 
                       "unitPrice":"200", 
                       "quantity":"1" 
                 }
              ], 
              "status":"COMPLETED" 
           },
           "localReceiptDateTime": "2016-03-02T12:58:14.828+01:00",
           "properties": [
              {
                 "name": "PAYMENT_ID",
                 "value": "151471228"
              }
           ]
        }       
        */

        // TODO: consider also checking signature form header, for now it is not needed becase process is additionally checking the data in service directly
        public bool IsNotificationFromMe(IDictionary<string, StringValues> headers, string? ip)
        {
            var areHeadersOk = headers.ContainsKey("OpenPayu-Signature") || headers.ContainsKey("X-OpenPayU-Signature");
            var isIpOk = true;
            if (!string.IsNullOrWhiteSpace(_settings.NotifyAllowedIPs) && !string.IsNullOrWhiteSpace(ip))
            {
                isIpOk = _settings.NotifyAllowedIPs.Contains(ip.Trim());
            }

            return areHeadersOk && isIpOk;
        }

        public PaymentNotification? GetDataFromNotificationContent(string content, IDictionary<string, StringValues> headers)
        {
            var notificationContent = JsonSerializer.Deserialize<NotificationContent>(content, options: new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (notificationContent == null)
            {
                return null;
            }

            Guid ourPaymentId = Guid.Empty;
            var amount = decimal.Zero;
            var status = PaymentStatus.New;
            var payUOrderId = "";
            var notificationType = NotificationType.NotClasified;
            if (notificationContent.Order != null && notificationContent.Properties.Any(x => x.Name == "PAYMENT_ID"))
            {
                var notificationOrder = notificationContent.Order!;
                Guid.TryParse(notificationOrder.ExtOrderId, out ourPaymentId);
                notificationType = NotificationType.Payment;
                if (Enum.TryParse<PayuResponseStatusCode>(notificationOrder.Status, out var orderStatus))
                {
                    status = orderStatus switch
                    {
                        PayuResponseStatusCode.COMPLETED => PaymentStatus.Completed,
                        PayuResponseStatusCode.CANCELED => PaymentStatus.Cancelled,
                        PayuResponseStatusCode.WAITING_FOR_CONFIRMATION => PaymentStatus.Processing, 
                        PayuResponseStatusCode.PENDING => PaymentStatus.Processing,
                        _ => PaymentStatus.Error
                    };
                }

                amount = StringToNumber(notificationContent.Order.TotalAmount);
                payUOrderId = notificationOrder.OrderId;
            }
            else if (notificationContent.Refund != null)
            {
                notificationType = NotificationType.Refund;
                var refund = notificationContent.Refund!;
                amount = StringToNumber(refund.Amount);
                payUOrderId = notificationContent.OrderId;
                Guid.TryParse(notificationContent.ExtOrderId, out ourPaymentId);
                status = refund.Status switch
                {
                    "CANCELED" => PaymentStatus.Cancelled,
                    "FINALIZED" => PaymentStatus.Completed,
                    _ => PaymentStatus.Error
                };
            }

            return new PaymentNotification
            {
                PaymentId = ourPaymentId,
                Amont = amount,
                PaymentProvider = PaymentProvider.PayU,
                IdFromProvider = payUOrderId,
                NotificationType = notificationType,
                Status = status,
            };
        }

        private string NumberToString(decimal input)
        {
            return (input * 100).ToString("#########0");
        }

        private decimal StringToNumber(string input)
        {
            if (input == null)
            {
                return decimal.Zero;
            }

            if(!decimal.TryParse(input, out var number))
            {
                return decimal.Zero;
            }

            return number / 100m;
        }

        private HttpClient? _httpClientAuthenticated;

        private HttpClient ClientAuthenticated
        {
            get
            {
                if (_httpClientAuthenticated == null)
                {
                    lock (syncRoot)
                    {
                        if (_httpClientAuthenticated == null)
                        {
                            var token = GetBearerTokenAsync().Result;
                            var httpClient = GetClient();

                            var authorization = new AuthenticationHeaderValue("Bearer", token);
                            httpClient.DefaultRequestHeaders.Authorization = authorization;

                            _httpClientAuthenticated = httpClient;
                        }
                    }
                }

                return _httpClientAuthenticated;
            }
        }

        private HttpClient GetClient(HttpClientHandler? httpClientHandler = null)
        {
            var httpClient = _clientFactory.CreateClient(HttpClientName);
            httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            return httpClient;
        }

        private async Task<string> GetBearerTokenAsync()
        {
            var client = GetClient();
            
            var requestContent = new FormUrlEncodedContent(
                new List<KeyValuePair<string?, string?>>
                {
                    KeyValuePair.Create<string?, string?>("grant_type", "client_credentials"),
                    KeyValuePair.Create<string?, string?>("client_id", _settings.ClientId),
                    KeyValuePair.Create<string?, string?>("client_secret", _settings.ClientSecret)
                });

            var response = await client.PostAsync("/pl/standard/user/oauth/authorize", requestContent);

            response.EnsureSuccessStatusCode();
            
            var authResponse = await response.Content.ReadFromJsonAsync<AuthorizeResponse>();

            return authResponse!.AccessToken!;
        }

        public void Dispose()
        {
            _httpClientAuthenticated?.Dispose();
        }
    }
}
