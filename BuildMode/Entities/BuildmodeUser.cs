using Auxiliary;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildMode.Entities
{
    public class BuildmodeUser : BsonModel
    {
        private int _tshockId;
        public int TShockId
        {
            get
                => _tshockId;
            set
            {
                _ = this.SaveAsync(x => x.TShockId, value);
                _tshockId = value;
            }
        }

        private int[] _buffs = Array.Empty<int>();
        public int[] Buffs
        {
            get
                => _buffs;
            set
            {
                _ = this.SaveAsync(x => x.Buffs, value);
                _buffs = value;
            }
        }

        public void Dispose()
        {

        }
    }
}
