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
    [BsonIgnoreExtraElements]
    public class BuffsEntity : IEntity, IConcurrentlyAccessible<BuffsEntity>
    {
        [BsonId]
        public ObjectId ObjectId { get; set; }

        [BsonIgnore]
        public EntityState State { get; set; }

        private int _tshockId;
        public int TShockId
        {
            get
                => _tshockId;
            set
            {
                _ = ModifyAsync(Builders<BuffsEntity>.Update.Set(x => x.TShockId, value));
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
                _ = ModifyAsync(Builders<BuffsEntity>.Update.Set(x => x.Buffs, value));
                _buffs = value;
            }
        }

        public async Task<bool> DeleteAsync()
            => await BuffsHelper.DeleteAsync(this);

        public async Task<bool> ModifyAsync(UpdateDefinition<BuffsEntity> update)
            => await BuffsHelper.ModifyAsync(this, update);

        public static async Task<BuffsEntity> GetAsync(int id)
            => await BuffsHelper.GetAsync(id);

        public void Dispose()
        {

        }
    }
}
