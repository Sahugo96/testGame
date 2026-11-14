using Game.Client.Net;
using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Client
{
    /// <summary>The only way views and input send intents to the server (views never touch the connection directly).</summary>
    public sealed class ClientCommands
    {
        private readonly IServerConnection _connection;
        private MoveCommand _lastMove;
        private int _clientSeq;

        public ClientCommands(IServerConnection connection) => _connection = connection;

        public void Move(float dirX, float dirZ)
        {
            if (dirX == _lastMove.DirX && dirZ == _lastMove.DirZ)
            {
                return;
            }
            _lastMove = new MoveCommand { DirX = dirX, DirZ = dirZ };
            _connection.Send(_lastMove);
        }

        public void UseSkill(string skillId, EntityId target, SkillModifier modifier) =>
            _connection.Send(new UseSkillCommand { SkillId = skillId, TargetId = target, Modifier = modifier, ClientSeq = ++_clientSeq });

        public void SelectTarget(EntityId target) => _connection.Send(new SelectTargetCommand { TargetId = target });

        public void CycleTarget() => _connection.Send(new CycleTargetCommand());

        public void Equip(int bagIndex) => _connection.Send(new EquipItemCommand { BagIndex = bagIndex });

        public void Unequip(string slotId) => _connection.Send(new UnequipItemCommand { SlotId = slotId });

        public void UseConsumable(int bagIndex) => _connection.Send(new UseConsumableCommand { BagIndex = bagIndex });

        /// <summary>Module 18: panel slots and equipment loadouts (null id clears a slot).</summary>
        public void SetSkillSlot(int index, string skillId) => _connection.Send(new SetSkillSlotCommand { Index = index, SkillId = skillId });

        public void SetConsumableSlot(int index, string itemId) => _connection.Send(new SetConsumableSlotCommand { Index = index, ItemId = itemId });

        public void SaveLoadout(int index) => _connection.Send(new SaveLoadoutCommand { Index = index });

        public void ApplyLoadout(int index) => _connection.Send(new ApplyLoadoutCommand { Index = index });

        public void UpgradeItem(string itemInstanceId, EntityId stationId) =>
            _connection.Send(new UpgradeItemCommand { ItemInstanceId = itemInstanceId, StationId = stationId });

        public void Craft(string recipeId, EntityId stationId) => _connection.Send(new CraftItemCommand { RecipeId = recipeId, StationId = stationId });

        public void CancelCraft() => _connection.Send(new CancelCraftCommand());

        public void InsertIngredient(string itemInstanceId, int socketIndex, int ingredientBagIndex, EntityId stationId) =>
            _connection.Send(new InsertIngredientCommand { ItemInstanceId = itemInstanceId, SocketIndex = socketIndex, IngredientBagIndex = ingredientBagIndex, StationId = stationId });

        public void SendChat(ChatChannel channel, string text, string target) => _connection.Send(new SendChatCommand { Channel = channel, Text = text, Target = target });

        public void AddFriend(string name) => _connection.Send(new FriendAddCommand { Name = name });

        public void RemoveFriend(string name) => _connection.Send(new FriendRemoveCommand { Name = name });

        public void RespondFriend(string name, bool accept) => _connection.Send(new FriendRespondCommand { Name = name, Accept = accept });

        public void MoveBagItem(int fromIndex, int toIndex, int count) => _connection.Send(new MoveBagItemCommand { FromIndex = fromIndex, ToIndex = toIndex, Count = count });

        public void SetChatTabs(ChatTabSnapshot[] tabs) => _connection.Send(new SetChatTabsCommand { Tabs = tabs });

        public void OpenShop(EntityId spot, string tierId, string name) => _connection.Send(new OpenShopCommand { SpotId = spot, TierId = tierId, Name = name });

        public void CloseShop() => _connection.Send(new CloseShopCommand());

        public void ListLot(string itemInstanceId, long pricePerUnit) => _connection.Send(new ListLotCommand { ItemInstanceId = itemInstanceId, PricePerUnit = pricePerUnit });

        public void UnlistLot(long lotId) => _connection.Send(new UnlistLotCommand { LotId = lotId });

        public void SetLotPrice(long lotId, long pricePerUnit) => _connection.Send(new SetLotPriceCommand { LotId = lotId, PricePerUnit = pricePerUnit });

        /// <summary>Each click is a new request id: the server ignores a repeat of the same one (idempotency).</summary>
        public void BuyLot(long lotId, int count, int priceVersion) =>
            _connection.Send(new BuyLotCommand { LotId = lotId, Count = count, PriceVersion = priceVersion, RequestId = System.Guid.NewGuid().ToString("N") });

        public void ViewShop(EntityId merchant) => _connection.Send(new ViewShopCommand { MerchantId = merchant });

        public void SearchMarket(MarketSearchCommand search) => _connection.Send(search);

        public void ViewMailbox() => _connection.Send(new ViewMailboxCommand());

        public void SendMail(string recipient, string subject, string body, string[] instanceIds, long gold) =>
            _connection.Send(new SendMailCommand { Recipient = recipient, Subject = subject, Body = body, InstanceIds = instanceIds, Gold = gold, RequestId = System.Guid.NewGuid().ToString("N") });

        public void ClaimMail(long mailId) => _connection.Send(new ClaimMailCommand { MailId = mailId });

        public void ReadMail(long mailId) => _connection.Send(new ReadMailCommand { MailId = mailId });

        public void DeleteMail(long mailId) => _connection.Send(new DeleteMailCommand { MailId = mailId });

        public void CreateUpgradeOrder(string itemInstanceId, long payment, string smithName, EntityId station) =>
            _connection.Send(new CreateUpgradeOrderCommand { ItemInstanceId = itemInstanceId, Payment = payment, SmithName = smithName, StationId = station });

        public void CancelUpgradeOrder(int orderId) => _connection.Send(new CancelUpgradeOrderCommand { OrderId = orderId });

        public void FulfillUpgradeOrder(int orderId, EntityId station) => _connection.Send(new FulfillUpgradeOrderCommand { OrderId = orderId, StationId = station });

        public void ViewUpgradeOrders() => _connection.Send(new ViewUpgradeOrdersCommand());

        public void SetAccountSettings(AccountSettingsSnapshot settings) => _connection.Send(new SetAccountSettingsCommand { Settings = settings });

        public void ConvertToRaid() => _connection.Send(new GroupConvertToRaidCommand());

        public void MoveToSubgroup(EntityId member, int subgroup) => _connection.Send(new GroupMoveCommand { MemberId = member, Subgroup = subgroup });

        public void SetAssistant(EntityId member, bool enabled) => _connection.Send(new GroupSetAssistantCommand { MemberId = member, Enabled = enabled });

        public void RequestTrade(EntityId target) => _connection.Send(new TradeRequestCommand { TargetId = target });

        public void RespondTrade(bool accept) => _connection.Send(new TradeRespondCommand { Accept = accept });

        public void OfferTrade(string[] instanceIds, long gold) => _connection.Send(new TradeOfferCommand { InstanceIds = instanceIds, Gold = gold });

        public void ConfirmTrade() => _connection.Send(new TradeConfirmCommand());

        public void CancelTrade() => _connection.Send(new TradeCancelCommand());

        public void PlayEmote(string emoteId) => _connection.Send(new EmoteCommand { EmoteId = emoteId });

        public void SetAutoLoot(bool enabled) => _connection.Send(new SetAutoLootCommand { Enabled = enabled });

        public void PickUpLoot(EntityId lootId) => _connection.Send(new PickUpLootCommand { LootId = lootId });

        public void SpendAttributePoint(StatId attribute) => _connection.Send(new SpendAttributePointCommand { Attribute = attribute });

        public void RequestDuel(EntityId target) => _connection.Send(new DuelRequestCommand { TargetId = target });

        public void RespondDuel(bool accept) => _connection.Send(new DuelResponseCommand { Accept = accept });

        public void SurrenderDuel() => _connection.Send(new DuelSurrenderCommand());

        public void InviteToGroup(EntityId target) => _connection.Send(new GroupInviteCommand { TargetId = target });

        public void RespondGroupInvite(bool accept) => _connection.Send(new GroupInviteResponseCommand { Accept = accept });

        public void LeaveGroup() => _connection.Send(new GroupLeaveCommand());

        public void KickFromGroup(EntityId member) => _connection.Send(new GroupKickCommand { MemberId = member });

        public void PromoteToLeader(EntityId member) => _connection.Send(new GroupPromoteCommand { MemberId = member });

        public void ProposeLootMode(LootMode mode) => _connection.Send(new LootModeProposeCommand { Mode = mode });

        public void VoteLootMode(int proposalId, bool accept) => _connection.Send(new LootModeVoteCommand { ProposalId = proposalId, Accept = accept });
    }
}
