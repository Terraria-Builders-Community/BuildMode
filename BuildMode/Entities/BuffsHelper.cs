using Auxiliary;
using Auxiliary.Configuration;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildMode.Entities
{
    public static class BuffsHelper
    {
        private static readonly Collection<BuffsEntity> _client = new("Buildmode");

        public static async Task<bool> ModifyAsync(BuffsEntity user, UpdateDefinition<BuffsEntity> update)
        {
            if (user.State is EntityState.Deserializing)
                return false;

            if (user.State is EntityState.Deleted)
                throw new InvalidOperationException($"{nameof(user)} cannot be modified post-deletion.");

            return await _client.ModifyDocumentAsync(user, update);
        }

        public static async Task<bool> DeleteAsync(BuffsEntity user)
        {
            user.State = EntityState.Deleted;

            return await _client.DeleteDocumentAsync(user);
        }

        public static async Task<BuffsEntity> GetAsync(int id)
        {
            var document = (await _client.FindDocumentAsync(x => x.TShockId == id)) ?? await CreateAsync(id);

            if (document is null)
                return null!;

            document.State = EntityState.Initialized;
            return document;
        }

        private static async Task<BuffsEntity> CreateAsync(int tshockId)
        {
            var entity = new BuffsEntity
            {
                TShockId = tshockId,
                Buffs = Configuration<BuildModeSettings>.Settings.DefaultBuffs
            };

            await _client.InsertDocumentAsync(entity);
            return entity;
        }
    }
}
