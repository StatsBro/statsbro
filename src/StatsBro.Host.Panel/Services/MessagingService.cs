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
﻿using Microsoft.Extensions.Options;
using StatsBro.Domain.Config;
using StatsBro.Domain.Models;

namespace StatsBro.Host.Panel.Services;

public interface IMessagingService
{
    void NewUserRegistrationAsync(User user);
}
public class MessagingService : IMessagingService
{
    private readonly SlackConfig _configSlack;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MessagingService> _logger;

    public MessagingService(
        IOptions<SlackConfig> slackConfigOptions,
        IHttpClientFactory httpClientFactory,
        ILogger<MessagingService> logger
        )
    {
        this._configSlack = slackConfigOptions.Value;
        this._httpClientFactory = httpClientFactory;
        this._logger = logger;
    }

    public void NewUserRegistrationAsync(User user)
    {
        // TODO add loging around this, like welcome email, activation email
        // slack notification that we have a new registration
        
        this.MessageSupportAboutNewUserRegistration(user);
    }

    private void MessageSupportAboutNewUserRegistration(User user)
    {
        if(_configSlack == null || string.IsNullOrWhiteSpace(_configSlack.NewUserRegistrationUrl))
        {
            return;
        }

        ThreadPool.QueueUserWorkItem(async _ => {
            var text = $":tada: Nowa rejestracja {user.Email} :tada: ";
            await this.SlackPushAsync(_configSlack.NewUserRegistrationUrl, text);
        });
    }

    private async Task<bool> SlackPushAsync(string url, string text)
    {
        var client = _httpClientFactory.CreateClient();
        var result = await client.PostAsJsonAsync(url, new { text });
        if (!result.IsSuccessStatusCode)
        {
            this._logger.LogError("Failed sending to slack responseCode: {code}, message not delivered: {message}", result.StatusCode, text);
            return false;
        }

        return true;
    }
}
