using Game.Server.Players;
using Game.Shared.Protocol;

namespace Game.Server.Systems
{
    /// <summary>Delivers combat events. Until interest management (module 3) every player receives every event.</summary>
    public static class CombatEvents
    {
        public static void Broadcast(ServerContext ctx, CombatEventMessage message)
        {
            foreach (PlayerCharacter player in ctx.Players.Values)
            {
                player.Outbox.CombatEvents.Add(message);
            }
        }

        public static void Reject(PlayerCharacter player, string reasonKey) =>
            player.Outbox.Rejections.Add(new CommandRejectedMessage { ReasonKey = reasonKey });
    }
}
