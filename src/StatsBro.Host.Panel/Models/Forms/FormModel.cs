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
﻿using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace StatsBro.Host.Panel.Models.Forms
{
    public class FormModel
    {
        public List<string> Errors { get; } = new List<string>();

        public void LoadErrors(ModelStateDictionary modelState)
        {
            foreach(var state in modelState.Values)
            {
                foreach(var er in state.Errors)
                {
                    Errors.Add(er.ErrorMessage);
                }
            }
        }

    }
}
