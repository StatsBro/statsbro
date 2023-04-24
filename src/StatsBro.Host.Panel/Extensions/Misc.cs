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
namespace StatsBro.Host.Panel.Extensions;

public static class Misc
{
    // datetime object must be UTC+0
    public static string TimeAgo(this DateTime dateTime)
    {
        string result = string.Empty;
        var timeSpan = DateTime.UtcNow.Subtract(dateTime);

        if (timeSpan <= TimeSpan.FromSeconds(60))
        {
            result = string.Format("{0} sekundy temu", timeSpan.Seconds);
        }
        else if (timeSpan <= TimeSpan.FromMinutes(60))
        {
            result = timeSpan.Minutes > 1 ?
                String.Format("około {0} minuty temu", timeSpan.Minutes) :
                "około minutę temu";
        }
        else if (timeSpan <= TimeSpan.FromHours(24))
        {
            result = timeSpan.Hours > 1 ?
                String.Format("około {0} godzin temu", timeSpan.Hours) :
                "około godzinę temu";
        }
        else if (timeSpan <= TimeSpan.FromDays(30))
        {
            result = timeSpan.Days > 1 ?
                String.Format("około {0} dni temu", timeSpan.Days) :
                "wczoraj";
        }
        else if (timeSpan <= TimeSpan.FromDays(365))
        {
            result = timeSpan.Days > 30 ?
                String.Format("około {0} miesięcy temu", timeSpan.Days / 30) :
                "około miesiąc temu";
        }
        else
        {
            result = timeSpan.Days > 365 ?
                String.Format("około {0} lat temu", timeSpan.Days / 365) :
                "około rok temu";
        }

        return result;
    }
}
