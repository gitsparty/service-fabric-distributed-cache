using System;
using System.Collections.Generic;
using System.Text;

namespace SoCreate.Extensions.Caching.ServiceFabric
{
    public class CreateItemResult
    {
        public bool isConflict { get; set; }

        public CachedItem CachedItem { get; set; }
    }
}
