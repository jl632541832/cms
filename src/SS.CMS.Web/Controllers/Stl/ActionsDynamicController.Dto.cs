﻿namespace SS.CMS.Web.Controllers.Stl
{
    public partial class ActionsDynamicController
    {
        public class SubmitRequest
        {
            public string Value { get; set; }
            public int Page { get; set; }
        }

        public class SubmitResult
        {
            public bool Value { get; set; }
            public string Html { get; set; }
        }
    }
}
