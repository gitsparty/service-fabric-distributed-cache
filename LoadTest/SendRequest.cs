﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace LoadTest
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.WebTesting;


    public class SendRequest : WebTest
    {
        private static Random _rand = new Random();

        public SendRequest()
        {
        }

        public override IEnumerator<WebTestRequest> GetRequestEnumerator()
        {
            int i = _rand.Next(1, 100000);
            yield return new WebTestRequest($"http://40.83.143.60/api/cachedemo/nas{i}");
        }
    }
}
