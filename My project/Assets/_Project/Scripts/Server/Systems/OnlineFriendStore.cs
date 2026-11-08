using Game.Server.Players;
using Game.Shared.Core;

namespace Game.Server.Systems
{
    /// <summary>Friend store without saves (tests, a server without persistence): every online character is its own account.</summary>
    public sealed class OnlineFriendStore : IFriendStore
    {
        private readonly ServerContext _ctx;
        private readonly ChatSystem _chat;

        public OnlineFriendStore(ServerContext ctx, ChatSystem chat)
        {
            _ctx = ctx;
            _chat = chat;
        }

        public string AccountOf(string characterName) => Key(_chat.FindByName(characterName));

        public string AccountOf(EntityId player) => Key(_ctx.GetPlayer(player));

        public FriendLists Load(string account, out EntityId online)
        {
            PlayerCharacter player = _chat.FindByName(account);
            online = player != null ? player.EntityId : EntityId.None;
            return player != null ? player.Friends : new FriendLists();
        }

        public void Save(string account, FriendLists lists)
        {
        }

        private string Key(PlayerCharacter player) =>
            player == null ? null : (_ctx.World.Identities.Get(player.EntityId).DisplayName ?? string.Empty).ToLowerInvariant();
    }
}
