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
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace StatsBro.Domain.Helpers
{
    public static class Validators
    {
        public const int EmailMaxLength = 96;

        public static bool IsValidEmail(string maybeEmail)
        {
            if (string.IsNullOrWhiteSpace(maybeEmail))
            {
                return false;
            }

            var matchTimeout = TimeSpan.FromMilliseconds(100);
            maybeEmail = maybeEmail[..Math.Min(maybeEmail.Length, EmailMaxLength)];
            maybeEmail = maybeEmail.ToLowerInvariant();

            try
            {
                string DomainMapper(Match match)
                {
                    var idn = new IdnMapping();
                    string domainName = idn.GetAscii(match.Groups[2].Value);
                    return match.Groups[1].Value + domainName;
                }

                maybeEmail = Regex.Replace(maybeEmail, @"(@)(.+)$", DomainMapper,
                                      RegexOptions.None, matchTimeout);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(maybeEmail,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, matchTimeout);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }
}
