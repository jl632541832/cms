﻿using System;

namespace SS.CMS.Abstractions
{
    public class Token
    {
        public int UserId { get; set; }

        public string UserName { get; set; }

        public DateTimeOffset ExpiresAt { get; set; }
    }
}