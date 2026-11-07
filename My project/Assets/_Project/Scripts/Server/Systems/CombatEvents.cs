using Game.Server.Players;
using Game.Shared.Protocol;

namespace Game.Server.Systems
{
    /// <summary>Delivers combat events only to players who see the source or the target (interest management, module 3).</summary>
    public static class CombatEvents
    {
        public static void Broadcast(ServerContext ctx, CombatEventMessage message)
        {
            foreach (PlayerCharacter player in ctx.Players.Values)
            {
                if (InterestSystem.InRange(ctx, player, message.TargetId) || (message.SourceId.IsValid && InterestSystem.InRange(ctx, player, message.SourceId)))
                {
                    player.Outbox.CombatEvents.Add(message);
                }
            }
        }

        public static void Reject(PlayerCharacter player, string reasonKey) =>
            player.Outbox.Rejections.Add(new CommandRejectedMessage { ReasonKey = reasonKey });
    }
}
