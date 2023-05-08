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
using System;
using System.Collections.Generic;
using SendGrid;
using System.Threading.Tasks;
using SendGrid.Helpers.Mail;
using StatsBro.Domain.Models;

namespace StatsBro.Domain.Service.Sendgrid
{
    public  class SendgridService
    {
        private readonly SendgridConfig _settings;
        private readonly ILogger<SendgridService> _logger;

        public SendgridService(
            IOptions<SendgridConfig> settingsOptions,
            ILogger<SendgridService> logger)
        {
            _settings = settingsOptions.Value;
            _logger = logger;
        }

        public async Task SendLoginByMagicLinkAsync(string recipientEmail, EmailPayloadMagicLink payload)
        {
            await SendEmailAsync(SendgridConfig.TemplateNameLoginByMagicLink, recipientEmail, new { link = payload.Link });
        }

        public async Task SendEmailAsync
           (
            string templateName, 
            string recipientEmail, 
            dynamic payload
           )
        {
            var clientOptions = new SendGridClientOptions { ApiKey = _settings.ApiKey };
            var client = new SendGrid.SendGridClient(clientOptions);
            var msg = new SendGridMessage
            {
                From = new EmailAddress(_settings.SenderEmail),
                TemplateId = GetTemplateId(templateName),
                
            };
            
            msg.AddTo(new EmailAddress(recipientEmail));
            msg.SetTemplateData(payload);            

            var sendEmailResponse = await client.SendEmailAsync(msg);
            
            if(!sendEmailResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed email of type {emailType}, {code}", templateName, sendEmailResponse.StatusCode);
                throw new Exception($"Error sending email for {recipientEmail}, response {sendEmailResponse.StatusCode}");
            }
        }

        private string GetTemplateId(string templateName)
        {
            if(_settings.Templates.TryGetValue(templateName, out var templateId))
            {
                return templateId;
            }

            throw new KeyNotFoundException($"TemplateId for email {templateName} not found in config");
        }
    }
}
