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
﻿using StatsBro.Domain.Models;
using System.Data;

namespace StatsBro.Host.Panel.Models
{
    public class StatsViewModel
    {
        public string Query { get; set; } = null!;
        public Site Site { get; set; } = null!;
        public StatsSummaryViewModel Summary { get; set; } = null!;

        public LineChartViewModel UsersInTime { get; set; } = null!;
        public LineChartViewModel PageViewsInTime { get; set; } = null!;
        public LineChartViewModel AverageVisitLengthInTime { get; set; } = null!;

        public PieChartViewModel TrafficSources { get; set; } = null!;
        public PieChartViewModel ScreenSizes { get; set; } = null!;
        public PieChartViewModel TouchScreens { get; set; } = null!;
        public PieChartViewModel Browsers { get; set; } = null!;
        public PieChartViewModel OSs { get; set; } = null!;

        public TableViewModel Campaigns { get; set; } = null!;
        public TableViewModel PageViews { get; set; } = null!;

        public TableViewModel EntryPages { get; set; } = null!;
        public TableViewModel PagesWithMostEngagement { get; set; } = null!;
        public TableViewModel Languages { get;  set; } = null!;
        public TableViewModel Cities { get; set; } = null!;
        public TableViewModel Countries { get; set; } = null!;

        public string GetChangeValueClass(int? change) => change >= 0 ? "text-success" : "text-danger";
        public string GetChangeValueText(int? change) => change >= 0 ? "wzrost" : "spadek";

    }
}
