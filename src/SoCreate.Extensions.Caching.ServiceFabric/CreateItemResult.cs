using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.ServiceFabric.Data;
using System.IO;

namespace SoCreate.Extensions.Caching.ServiceFabric
{
    public sealed class CreateItemResult
    {
        public CreateItemResult(bool b, CachedItem ci)
        {
            this.isConflict = b;
            this.CachedItem = ci;
        }

        public bool isConflict { get; private set; }

        public CachedItem CachedItem { get; private set; }
    }

    class CreateItemResultSerializer : IStateSerializer<CreateItemResult>
    {
        CreateItemResult IStateSerializer<CreateItemResult>.Read(BinaryReader reader)
        {
            IStateSerializer<CachedItem> s = new CachedItemSerializer();

            bool b = reader.ReadBoolean();
            var ci = s.Read(reader);

            return new CreateItemResult(b, ci);
        }

        void IStateSerializer<CreateItemResult>.Write(CreateItemResult value, BinaryWriter writer)
        {
            IStateSerializer<CachedItem> s = new CachedItemSerializer();
            writer.Write(value.isConflict);
            if (value.CachedItem != null)
            {
                s.Write(value.CachedItem, writer);
            }
            else
            {
                writer.Write(string.Empty);
            }
        }

        // Read overload for differential de-serialization
        CreateItemResult IStateSerializer<CreateItemResult>.Read(CreateItemResult baseValue, BinaryReader reader)
        {
            return ((IStateSerializer<CreateItemResult>)this).Read(reader);
        }

        // Write overload for differential serialization
        void IStateSerializer<CreateItemResult>.Write(CreateItemResult baseValue, CreateItemResult newValue, BinaryWriter writer)
        {
            ((IStateSerializer<CreateItemResult>)this).Write(newValue, writer);
        }

        private string GetStringValueOrNull(string value)
        {
            return value == string.Empty ? null : value;
        }
    }

}
