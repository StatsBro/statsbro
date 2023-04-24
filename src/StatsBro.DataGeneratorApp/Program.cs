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
﻿// See https://aka.ms/new-console-template for more information
namespace StatsBro.DataGeneratorApp;

public class Program
{
    public static void Main() 
    {
        var staticGenerator = new StaticDataGenerator();
        staticGenerator.Run();

        //var dummyGenerator = new DummyDataGenerator();
        //dummyGenerator.Run();

        var engagedUserGenerator = new EngagedUsersDataGenerator();
        engagedUserGenerator.Run();
    }
}
