using Game.Server.Players;
using Game.Shared.Core;

namespace Game.Server.Systems
{
    /// <summary>
    /// Where friend lists live: the account of each character. Persistence implements it over the saves (offline accounts
    /// too); without persistence <see cref="OnlineFriendStore"/> knows only who is online. "Account" is an opaque key.
    /// </summary>
    public interface IFriendStore
    {
        /// <summary>Account that owns a character name (any case), or null if there is no such character.</summary>
        string AccountOf(string characterName);

        /// <summary>Account of an online player.</summary>
        string AccountOf(EntityId player);

        /// <summary>The live lists of an online account (<paramref name="online"/> = its player) or the saved ones.</summary>
        FriendLists Load(string account, out EntityId online);

        /// <summary>Saves the lists of an offline account.</summary>
        void Save(string account, FriendLists lists);
    }
}
