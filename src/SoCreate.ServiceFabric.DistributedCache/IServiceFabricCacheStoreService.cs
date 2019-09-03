﻿using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Threading.Tasks;

namespace SoCreate.ServiceFabric.DistributedCache
{
    public interface IServiceFabricCacheStoreService : IService, IDistributedCacheWithCreate
    {
    }
}