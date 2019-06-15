﻿using System;
using Npgsql.TypeHandlers;

namespace SS.CMS.Data
{
    [AttributeUsage(AttributeTargets.Property)]
    public class TableColumnAttribute : Attribute
    {
        public int Length { get; set; }

        public bool Text { get; set; }

        public bool Extend { get; set; }
    }
}
