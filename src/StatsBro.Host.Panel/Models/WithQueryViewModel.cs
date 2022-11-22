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
﻿using System.Web;

namespace StatsBro.Host.Panel.Models
{
    public class WithQueryViewModel
    {
        public string ExistingQuery { get; set; } = null!;
        public string FieldName { get; set; } = null!;

        public string GetLinkWithSubquery(string value)
        {
            value = ESQueryEncode(value);

            if (string.IsNullOrEmpty(ExistingQuery))
            {
                return $"?Query={HttpUtility.UrlEncode(FieldName)}:({value})";
            }

            if (ExistingQuery.Contains($"{FieldName}:"))
            {
                var fieldNamePos = ExistingQuery.IndexOf($"{FieldName}:");
                var valStart = ExistingQuery.IndexOf("(", fieldNamePos);
                var valEnd = ExistingQuery.IndexOf(")", valStart);
                var val = ExistingQuery.Substring(valStart + 1, valEnd - valStart - 1);
                return "?Query=" + ExistingQuery.Replace($"{HttpUtility.UrlEncode(FieldName)}:({val})", $"{HttpUtility.UrlEncode(FieldName)}:({value})");
            }
            else
            {
                return $"?Query={ExistingQuery} AND {HttpUtility.UrlEncode(FieldName)}:({value})";
            }
        }

        private string ESQueryEncode(string value)
        {
            return value
                .Replace("+", "\\+")
                .Replace("-", "\\-")
                .Replace("=", "\\=")
                .Replace("&&", "\\&&")
                .Replace("||", "\\||")
                .Replace(">", "\\>")
                .Replace("<", "\\<")
                .Replace("!", "\\!")
                .Replace("(", "\\(")
                .Replace(")", "\\)")
                .Replace("{", "\\{")
                .Replace("}", "\\}")
                .Replace("[", "\\[")
                .Replace("]", "\\]")
                .Replace("^", "\\^")
                .Replace("\"", "\\\"")
                .Replace("~", "\\~")
                .Replace("*", "\\*")
                .Replace("?", "\\?")
                .Replace(":", "\\:")
                .Replace("\\", "\\\\")
                .Replace("/", "\\/")
                ;
        }
    }
}
