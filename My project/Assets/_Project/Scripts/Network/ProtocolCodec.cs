using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;

namespace Game.Network
{
    /// <summary>
    /// Binary wire format of all messages (temporary own transport, question 69). One message per frame:
    /// a <see cref="MessageType"/> byte followed by the fields in a fixed order. Strings are nullable.
    /// </summary>
    public static class ProtocolCodec
    {
        public static byte[] Encode(object message)
        {
            using (var stream = new MemoryStream())
            using (var w = new BinaryWriter(stream, Encoding.UTF8))
            {
                Write(w, message);
                w.Flush();
                return stream.ToArray();
            }
        }

        /// <summary>
        /// World snapshot for one peer (module 3 traffic): static fields (definition, name key, display name, faction) only
        /// for entities the peer does not know yet; `known` is updated (entities that left the view are forgotten, so they
        /// come back in full). The client side fills the missing fields from its cache (RemoteServerConnection).
        /// </summary>
        /// <summary>
        /// World snapshot for one client. <paramref name="sent"/> is what that client already has (the last snapshot sent to
        /// it, by entity): static fields go once per entity, and an entity whose position, health, statuses and target did
        /// not change is written as its id only. Null = everything in full.
        /// </summary>
        public static byte[] EncodeWorld(WorldSnapshot world, Dictionary<EntityId, EntitySnapshot> sent)
        {
            using (var stream = new MemoryStream())
            using (var w = new BinaryWriter(stream, Encoding.UTF8))
            {
                w.Write((byte)MessageType.World);
                WriteWorld(w, world, sent);
                w.Flush();
                return stream.ToArray();
            }
        }

        public static object Decode(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            using (var r = new BinaryReader(stream, Encoding.UTF8))
            {
                return Read(r);
            }
        }

        private static void Write(BinaryWriter w, object message)
        {
            switch (message)
            {
                case HelloMessage m:
                    w.Write((byte)MessageType.Hello);
                    w.Write(m.ProtocolVersion);
                    Str(w, m.PlayerName);
                    Str(w, m.ClassId);
                    Str(w, m.Login);
                    Str(w, m.Password);
                    Str(w, m.Token);
                    Str(w, m.ContentHash);
                    break;
                case WelcomeMessage m:
                    w.Write((byte)MessageType.Welcome);
                    Id(w, m.PlayerId);
                    w.Write(m.TickRate);
                    w.Write(m.WorldEveryTicks);
                    Str(w, m.Error);
                    Str(w, m.Token);
                    break;
                case MoveCommand m:
                    w.Write((byte)MessageType.Move);
                    w.Write(m.DirX);
                    w.Write(m.DirZ);
                    break;
                case UseSkillCommand m:
                    w.Write((byte)MessageType.UseSkill);
                    Str(w, m.SkillId);
                    Id(w, m.TargetId);
                    w.Write((byte)m.Modifier);
                    w.Write(m.ClientSeq);
                    break;
                case SelectTargetCommand m:
                    w.Write((byte)MessageType.SelectTarget);
                    Id(w, m.TargetId);
                    break;
                case CycleTargetCommand _:
                    w.Write((byte)MessageType.CycleTarget);
                    break;
                case EquipItemCommand m:
                    w.Write((byte)MessageType.Equip);
                    w.Write(m.BagIndex);
                    break;
                case UnequipItemCommand m:
                    w.Write((byte)MessageType.Unequip);
                    Str(w, m.SlotId);
                    break;
                case UseConsumableCommand m:
                    w.Write((byte)MessageType.UseConsumable);
                    w.Write(m.BagIndex);
                    break;
                case UpgradeItemCommand m:
                    w.Write((byte)MessageType.UpgradeItem);
                    Str(w, m.ItemInstanceId);
                    Id(w, m.StationId);
                    break;
                case SpendAttributePointCommand m:
                    w.Write((byte)MessageType.SpendAttributePoint);
                    w.Write((int)m.Attribute);
                    break;
                case DuelRequestCommand m:
                    w.Write((byte)MessageType.DuelRequest);
                    Id(w, m.TargetId);
                    break;
                case DuelResponseCommand m:
                    w.Write((byte)MessageType.DuelResponse);
                    w.Write(m.Accept);
                    break;
                case DuelSurrenderCommand _:
                    w.Write((byte)MessageType.DuelSurrender);
                    break;
                case GroupInviteCommand m:
                    w.Write((byte)MessageType.GroupInvite);
                    Id(w, m.TargetId);
                    break;
                case GroupInviteResponseCommand m:
                    w.Write((byte)MessageType.GroupInviteResponse);
                    w.Write(m.Accept);
                    break;
                case GroupLeaveCommand _:
                    w.Write((byte)MessageType.GroupLeave);
                    break;
                case GroupKickCommand m:
                    w.Write((byte)MessageType.GroupKick);
                    Id(w, m.MemberId);
                    break;
                case GroupPromoteCommand m:
                    w.Write((byte)MessageType.GroupPromote);
                    Id(w, m.MemberId);
                    break;
                case LootModeProposeCommand m:
                    w.Write((byte)MessageType.LootModePropose);
                    w.Write((byte)m.Mode);
                    break;
                case LootModeVoteCommand m:
                    w.Write((byte)MessageType.LootModeVote);
                    w.Write(m.ProposalId);
                    w.Write(m.Accept);
                    break;
                case CraftItemCommand m:
                    w.Write((byte)MessageType.CraftItem);
                    Str(w, m.RecipeId);
                    Id(w, m.StationId);
                    break;
                case CancelCraftCommand _:
                    w.Write((byte)MessageType.CancelCraft);
                    break;
                case FriendAddCommand m:
                    w.Write((byte)MessageType.FriendAdd);
                    Str(w, m.Name);
                    break;
                case FriendRemoveCommand m:
                    w.Write((byte)MessageType.FriendRemove);
                    Str(w, m.Name);
                    break;
                case FriendRespondCommand m:
                    w.Write((byte)MessageType.FriendRespond);
                    Str(w, m.Name);
                    w.Write(m.Accept);
                    break;
                case MoveBagItemCommand m:
                    w.Write((byte)MessageType.MoveBagItem);
                    w.Write(m.FromIndex);
                    w.Write(m.ToIndex);
                    w.Write(m.Count);
                    break;
                case SetChatTabsCommand m:
                    w.Write((byte)MessageType.SetChatTabs);
                    WriteChatTabs(w, m.Tabs);
                    break;
                case OpenShopCommand m:
                    w.Write((byte)MessageType.OpenShop);
                    Id(w, m.SpotId);
                    Str(w, m.TierId);
                    Str(w, m.Name);
                    break;
                case CloseShopCommand _:
                    w.Write((byte)MessageType.CloseShop);
                    break;
                case ListLotCommand m:
                    w.Write((byte)MessageType.ListLot);
                    Str(w, m.ItemInstanceId);
                    w.Write(m.PricePerUnit);
                    break;
                case UnlistLotCommand m:
                    w.Write((byte)MessageType.UnlistLot);
                    w.Write(m.LotId);
                    break;
                case SetLotPriceCommand m:
                    w.Write((byte)MessageType.SetLotPrice);
                    w.Write(m.LotId);
                    w.Write(m.PricePerUnit);
                    break;
                case BuyLotCommand m:
                    w.Write((byte)MessageType.BuyLot);
                    w.Write(m.LotId);
                    w.Write(m.Count);
                    w.Write(m.PriceVersion);
                    Str(w, m.RequestId);
                    break;
                case ViewShopCommand m:
                    w.Write((byte)MessageType.ViewShop);
                    Id(w, m.MerchantId);
                    break;
                case MarketSearchCommand m:
                    w.Write((byte)MessageType.MarketSearch);
                    Strs(w, m.DefinitionIds);
                    Str(w, m.Category);
                    Str(w, m.ClassId);
                    w.Write(m.MinLevel);
                    w.Write(m.MaxLevel);
                    w.Write(m.MinSockets);
                    w.Write(m.MinUpgrade);
                    w.Write(m.Rarity);
                    w.Write(m.MaxPrice);
                    w.Write((byte)m.Sort);
                    w.Write(m.Page);
                    break;
                case ViewMailboxCommand _:
                    w.Write((byte)MessageType.ViewMailbox);
                    break;
                case SendMailCommand m:
                    w.Write((byte)MessageType.SendMail);
                    Str(w, m.Recipient);
                    Str(w, m.Subject);
                    Str(w, m.Body);
                    Strs(w, m.InstanceIds);
                    w.Write(m.Gold);
                    Str(w, m.RequestId);
                    break;
                case ClaimMailCommand m:
                    w.Write((byte)MessageType.ClaimMail);
                    w.Write(m.MailId);
                    break;
                case ReadMailCommand m:
                    w.Write((byte)MessageType.ReadMail);
                    w.Write(m.MailId);
                    break;
                case DeleteMailCommand m:
                    w.Write((byte)MessageType.DeleteMail);
                    w.Write(m.MailId);
                    break;
                case CreateUpgradeOrderCommand m:
                    w.Write((byte)MessageType.CreateUpgradeOrder);
                    Str(w, m.ItemInstanceId);
                    w.Write(m.Payment);
                    Str(w, m.SmithName);
                    Id(w, m.StationId);
                    break;
                case CancelUpgradeOrderCommand m:
                    w.Write((byte)MessageType.CancelUpgradeOrder);
                    w.Write(m.OrderId);
                    break;
                case FulfillUpgradeOrderCommand m:
                    w.Write((byte)MessageType.FulfillUpgradeOrder);
                    w.Write(m.OrderId);
                    Id(w, m.StationId);
                    break;
                case ViewUpgradeOrdersCommand _:
                    w.Write((byte)MessageType.ViewUpgradeOrders);
                    break;
                case SetAccountSettingsCommand m:
                    w.Write((byte)MessageType.SetAccountSettings);
                    WriteSettings(w, m.Settings ?? new AccountSettingsSnapshot());
                    break;
                case AccountSettingsMessage m:
                    w.Write((byte)MessageType.AccountSettings);
                    WriteSettings(w, m.Settings ?? new AccountSettingsSnapshot());
                    break;
                case SetSkillSlotCommand m:
                    w.Write((byte)MessageType.SetSkillSlot);
                    w.Write(m.Index);
                    Str(w, m.SkillId);
                    break;
                case SetConsumableSlotCommand m:
                    w.Write((byte)MessageType.SetConsumableSlot);
                    w.Write(m.Index);
                    Str(w, m.ItemId);
                    break;
                case SaveLoadoutCommand m:
                    w.Write((byte)MessageType.SaveLoadout);
                    w.Write(m.Index);
                    break;
                case ApplyLoadoutCommand m:
                    w.Write((byte)MessageType.ApplyLoadout);
                    w.Write(m.Index);
                    break;
                case PanelsMessage m:
                    w.Write((byte)MessageType.Panels);
                    Strs(w, m.SkillBar);
                    Strs(w, m.ConsumableBar);
                    WriteLoadouts(w, m.Loadouts);
                    break;
                case ShopViewMessage m:
                    w.Write((byte)MessageType.ShopView);
                    Id(w, m.MerchantId);
                    Str(w, m.Name);
                    Str(w, m.OwnerName);
                    Str(w, m.TierId);
                    w.Write(m.IsMine);
                    w.Write(m.SecondsLeft);
                    WriteLots(w, m.Lots);
                    break;
                case MarketPageMessage m:
                    w.Write((byte)MessageType.MarketPage);
                    w.Write(m.Total);
                    w.Write(m.Page);
                    w.Write(m.PageCount);
                    WriteLots(w, m.Lots);
                    break;
                case MailboxMessage m:
                    w.Write((byte)MessageType.Mailbox);
                    MailSnapshot[] mail = m.Mail ?? new MailSnapshot[0];
                    w.Write(mail.Length);
                    foreach (MailSnapshot letter in mail)
                    {
                        w.Write(letter.Id);
                        w.Write((byte)letter.Type);
                        Str(w, letter.From);
                        Str(w, letter.Subject);
                        Str(w, letter.Body);
                        Str(w, letter.SubjectKey);
                        Str(w, letter.ItemDefinitionId);
                        w.Write(letter.Number);
                        Str(w, letter.Detail);
                        ItemSlotSnapshot[] items = letter.Items ?? new ItemSlotSnapshot[0];
                        w.Write(items.Length);
                        foreach (ItemSlotSnapshot item in items)
                        {
                            WriteSlot(w, item);
                        }
                        w.Write(letter.Gold);
                        w.Write(letter.SecondsLeft);
                        w.Write(letter.Read);
                        w.Write(letter.Claimed);
                    }
                    break;
                case UpgradeOrdersMessage m:
                    w.Write((byte)MessageType.UpgradeOrders);
                    UpgradeOrderSnapshot[] orders = m.Orders ?? new UpgradeOrderSnapshot[0];
                    w.Write(orders.Length);
                    foreach (UpgradeOrderSnapshot order in orders)
                    {
                        w.Write(order.Id);
                        Str(w, order.OwnerName);
                        Str(w, order.SmithName);
                        WriteSlot(w, order.Item);
                        w.Write(order.Payment);
                        w.Write(order.SecondsLeft);
                        w.Write(order.IsMine);
                    }
                    break;
                case GroupConvertToRaidCommand _:
                    w.Write((byte)MessageType.GroupConvertToRaid);
                    break;
                case GroupMoveCommand m:
                    w.Write((byte)MessageType.GroupMove);
                    Id(w, m.MemberId);
                    w.Write(m.Subgroup);
                    break;
                case GroupSetAssistantCommand m:
                    w.Write((byte)MessageType.GroupSetAssistant);
                    Id(w, m.MemberId);
                    w.Write(m.Enabled);
                    break;
                case TradeRequestCommand m:
                    w.Write((byte)MessageType.TradeRequest);
                    Id(w, m.TargetId);
                    break;
                case TradeRespondCommand m:
                    w.Write((byte)MessageType.TradeRespond);
                    w.Write(m.Accept);
                    break;
                case TradeOfferCommand m:
                    w.Write((byte)MessageType.TradeOffer);
                    string[] offered = m.InstanceIds ?? new string[0];
                    w.Write(offered.Length);
                    foreach (string id in offered)
                    {
                        Str(w, id);
                    }
                    w.Write(m.Gold);
                    break;
                case TradeConfirmCommand _:
                    w.Write((byte)MessageType.TradeConfirm);
                    break;
                case TradeCancelCommand _:
                    w.Write((byte)MessageType.TradeCancel);
                    break;
                case SendChatCommand m:
                    w.Write((byte)MessageType.SendChat);
                    w.Write((byte)m.Channel);
                    Str(w, m.Text);
                    Str(w, m.Target);
                    break;
                case ChatMessage m:
                    w.Write((byte)MessageType.Chat);
                    w.Write((byte)m.Channel);
                    Id(w, m.SenderId);
                    Str(w, m.SenderName);
                    Str(w, m.TargetName);
                    Str(w, m.Text);
                    Str(w, m.TextKey);
                    break;
                case InsertIngredientCommand m:
                    w.Write((byte)MessageType.InsertIngredient);
                    Str(w, m.ItemInstanceId);
                    w.Write(m.SocketIndex);
                    w.Write(m.IngredientBagIndex);
                    Id(w, m.StationId);
                    break;
                case PickUpLootCommand m:
                    w.Write((byte)MessageType.PickUpLoot);
                    Id(w, m.LootId);
                    break;
                case SetAutoLootCommand m:
                    w.Write((byte)MessageType.SetAutoLoot);
                    w.Write(m.Enabled);
                    break;
                case EmoteCommand m:
                    w.Write((byte)MessageType.PlayEmote);
                    Str(w, m.EmoteId);
                    break;
                case EmoteMessage m:
                    w.Write((byte)MessageType.Emote);
                    Id(w, m.EntityId);
                    Str(w, m.EmoteId);
                    break;
                case WorldSnapshot m:
                    w.Write((byte)MessageType.World);
                    WriteWorld(w, m, null);
                    break;
                case PlayerStateSnapshot m:
                    w.Write((byte)MessageType.PlayerState);
                    WriteState(w, m);
                    break;
                case CombatEventMessage m:
                    w.Write((byte)MessageType.CombatEvent);
                    Id(w, m.SourceId);
                    Id(w, m.TargetId);
                    Str(w, m.SkillId);
                    w.Write((byte)m.Kind);
                    w.Write(m.Amount);
                    w.Write(m.CritTier);
                    w.Write(m.Absorbed);
                    break;
                case CommandRejectedMessage m:
                    w.Write((byte)MessageType.Rejected);
                    Str(w, m.ReasonKey);
                    break;
                case NotificationMessage m:
                    w.Write((byte)MessageType.Notification);
                    Str(w, m.TextKey);
                    w.Write(m.Number);
                    Str(w, m.ItemNameKey);
                    w.Write(m.IsPositive);
                    break;
                default:
                    throw new ArgumentException($"Cannot encode {message?.GetType().Name ?? "null"}");
            }
        }

        private static object Read(BinaryReader r)
        {
            var type = (MessageType)r.ReadByte();
            switch (type)
            {
                case MessageType.Hello:
                    return new HelloMessage { ProtocolVersion = r.ReadInt32(), PlayerName = Str(r), ClassId = Str(r), Login = Str(r), Password = Str(r), Token = Str(r), ContentHash = Str(r) };
                case MessageType.Welcome:
                    return new WelcomeMessage { PlayerId = Id(r), TickRate = r.ReadInt32(), WorldEveryTicks = r.ReadInt32(), Error = Str(r), Token = Str(r) };
                case MessageType.Move:
                    return new MoveCommand { DirX = r.ReadSingle(), DirZ = r.ReadSingle() };
                case MessageType.UseSkill:
                    return new UseSkillCommand { SkillId = Str(r), TargetId = Id(r), Modifier = (SkillModifier)r.ReadByte(), ClientSeq = r.ReadInt32() };
                case MessageType.SelectTarget:
                    return new SelectTargetCommand { TargetId = Id(r) };
                case MessageType.CycleTarget:
                    return new CycleTargetCommand();
                case MessageType.Equip:
                    return new EquipItemCommand { BagIndex = r.ReadInt32() };
                case MessageType.Unequip:
                    return new UnequipItemCommand { SlotId = Str(r) };
                case MessageType.UseConsumable:
                    return new UseConsumableCommand { BagIndex = r.ReadInt32() };
                case MessageType.UpgradeItem:
                    return new UpgradeItemCommand { ItemInstanceId = Str(r), StationId = Id(r) };
                case MessageType.SpendAttributePoint:
                    return new SpendAttributePointCommand { Attribute = (StatId)r.ReadInt32() };
                case MessageType.DuelRequest:
                    return new DuelRequestCommand { TargetId = Id(r) };
                case MessageType.DuelResponse:
                    return new DuelResponseCommand { Accept = r.ReadBoolean() };
                case MessageType.DuelSurrender:
                    return new DuelSurrenderCommand();
                case MessageType.GroupInvite:
                    return new GroupInviteCommand { TargetId = Id(r) };
                case MessageType.GroupInviteResponse:
                    return new GroupInviteResponseCommand { Accept = r.ReadBoolean() };
                case MessageType.GroupLeave:
                    return new GroupLeaveCommand();
                case MessageType.GroupKick:
                    return new GroupKickCommand { MemberId = Id(r) };
                case MessageType.GroupPromote:
                    return new GroupPromoteCommand { MemberId = Id(r) };
                case MessageType.LootModePropose:
                    return new LootModeProposeCommand { Mode = (LootMode)r.ReadByte() };
                case MessageType.LootModeVote:
                    return new LootModeVoteCommand { ProposalId = r.ReadInt32(), Accept = r.ReadBoolean() };
                case MessageType.CraftItem:
                    return new CraftItemCommand { RecipeId = Str(r), StationId = Id(r) };
                case MessageType.CancelCraft:
                    return new CancelCraftCommand();
                case MessageType.FriendAdd:
                    return new FriendAddCommand { Name = Str(r) };
                case MessageType.FriendRemove:
                    return new FriendRemoveCommand { Name = Str(r) };
                case MessageType.FriendRespond:
                    return new FriendRespondCommand { Name = Str(r), Accept = r.ReadBoolean() };
                case MessageType.MoveBagItem:
                    return new MoveBagItemCommand { FromIndex = r.ReadInt32(), ToIndex = r.ReadInt32(), Count = r.ReadInt32() };
                case MessageType.SetChatTabs:
                    return new SetChatTabsCommand { Tabs = ReadChatTabs(r) };
                case MessageType.OpenShop:
                    return new OpenShopCommand { SpotId = Id(r), TierId = Str(r), Name = Str(r) };
                case MessageType.CloseShop:
                    return new CloseShopCommand();
                case MessageType.ListLot:
                    return new ListLotCommand { ItemInstanceId = Str(r), PricePerUnit = r.ReadInt64() };
                case MessageType.UnlistLot:
                    return new UnlistLotCommand { LotId = r.ReadInt64() };
                case MessageType.SetLotPrice:
                    return new SetLotPriceCommand { LotId = r.ReadInt64(), PricePerUnit = r.ReadInt64() };
                case MessageType.BuyLot:
                    return new BuyLotCommand { LotId = r.ReadInt64(), Count = r.ReadInt32(), PriceVersion = r.ReadInt32(), RequestId = Str(r) };
                case MessageType.ViewShop:
                    return new ViewShopCommand { MerchantId = Id(r) };
                case MessageType.MarketSearch:
                    return new MarketSearchCommand
                    {
                        DefinitionIds = Strs(r), Category = Str(r), ClassId = Str(r), MinLevel = r.ReadInt32(), MaxLevel = r.ReadInt32(),
                        MinSockets = r.ReadInt32(), MinUpgrade = r.ReadInt32(), Rarity = r.ReadInt32(), MaxPrice = r.ReadInt64(),
                        Sort = (MarketSort)r.ReadByte(), Page = r.ReadInt32(),
                    };
                case MessageType.ViewMailbox:
                    return new ViewMailboxCommand();
                case MessageType.SendMail:
                    return new SendMailCommand { Recipient = Str(r), Subject = Str(r), Body = Str(r), InstanceIds = Strs(r), Gold = r.ReadInt64(), RequestId = Str(r) };
                case MessageType.ClaimMail:
                    return new ClaimMailCommand { MailId = r.ReadInt64() };
                case MessageType.ReadMail:
                    return new ReadMailCommand { MailId = r.ReadInt64() };
                case MessageType.DeleteMail:
                    return new DeleteMailCommand { MailId = r.ReadInt64() };
                case MessageType.CreateUpgradeOrder:
                    return new CreateUpgradeOrderCommand { ItemInstanceId = Str(r), Payment = r.ReadInt64(), SmithName = Str(r), StationId = Id(r) };
                case MessageType.CancelUpgradeOrder:
                    return new CancelUpgradeOrderCommand { OrderId = r.ReadInt32() };
                case MessageType.FulfillUpgradeOrder:
                    return new FulfillUpgradeOrderCommand { OrderId = r.ReadInt32(), StationId = Id(r) };
                case MessageType.ViewUpgradeOrders:
                    return new ViewUpgradeOrdersCommand();
                case MessageType.SetAccountSettings:
                    return new SetAccountSettingsCommand { Settings = ReadSettings(r) };
                case MessageType.AccountSettings:
                    return new AccountSettingsMessage { Settings = ReadSettings(r) };
                case MessageType.SetSkillSlot:
                    return new SetSkillSlotCommand { Index = r.ReadInt32(), SkillId = Str(r) };
                case MessageType.SetConsumableSlot:
                    return new SetConsumableSlotCommand { Index = r.ReadInt32(), ItemId = Str(r) };
                case MessageType.SaveLoadout:
                    return new SaveLoadoutCommand { Index = r.ReadInt32() };
                case MessageType.ApplyLoadout:
                    return new ApplyLoadoutCommand { Index = r.ReadInt32() };
                case MessageType.Panels:
                    return new PanelsMessage { SkillBar = Strs(r), ConsumableBar = Strs(r), Loadouts = ReadLoadouts(r) };
                case MessageType.ShopView:
                    return new ShopViewMessage { MerchantId = Id(r), Name = Str(r), OwnerName = Str(r), TierId = Str(r), IsMine = r.ReadBoolean(), SecondsLeft = r.ReadInt64(), Lots = ReadLots(r) };
                case MessageType.MarketPage:
                    return new MarketPageMessage { Total = r.ReadInt32(), Page = r.ReadInt32(), PageCount = r.ReadInt32(), Lots = ReadLots(r) };
                case MessageType.Mailbox:
                    var mailbox = new MailboxMessage { Mail = new MailSnapshot[r.ReadInt32()] };
                    for (int i = 0; i < mailbox.Mail.Length; i++)
                    {
                        var letter = new MailSnapshot
                        {
                            Id = r.ReadInt64(), Type = (MailType)r.ReadByte(), From = Str(r), Subject = Str(r), Body = Str(r), SubjectKey = Str(r),
                            ItemDefinitionId = Str(r), Number = r.ReadInt32(), Detail = Str(r),
                        };
                        letter.Items = new ItemSlotSnapshot[r.ReadInt32()];
                        for (int k = 0; k < letter.Items.Length; k++)
                        {
                            letter.Items[k] = ReadSlot(r);
                        }
                        letter.Gold = r.ReadInt64();
                        letter.SecondsLeft = r.ReadInt64();
                        letter.Read = r.ReadBoolean();
                        letter.Claimed = r.ReadBoolean();
                        mailbox.Mail[i] = letter;
                    }
                    return mailbox;
                case MessageType.UpgradeOrders:
                    var ordersMessage = new UpgradeOrdersMessage { Orders = new UpgradeOrderSnapshot[r.ReadInt32()] };
                    for (int i = 0; i < ordersMessage.Orders.Length; i++)
                    {
                        ordersMessage.Orders[i] = new UpgradeOrderSnapshot
                        {
                            Id = r.ReadInt32(), OwnerName = Str(r), SmithName = Str(r), Item = ReadSlot(r), Payment = r.ReadInt64(), SecondsLeft = r.ReadInt64(), IsMine = r.ReadBoolean(),
                        };
                    }
                    return ordersMessage;
                case MessageType.GroupConvertToRaid:
                    return new GroupConvertToRaidCommand();
                case MessageType.GroupMove:
                    return new GroupMoveCommand { MemberId = Id(r), Subgroup = r.ReadInt32() };
                case MessageType.GroupSetAssistant:
                    return new GroupSetAssistantCommand { MemberId = Id(r), Enabled = r.ReadBoolean() };
                case MessageType.TradeRequest:
                    return new TradeRequestCommand { TargetId = Id(r) };
                case MessageType.TradeRespond:
                    return new TradeRespondCommand { Accept = r.ReadBoolean() };
                case MessageType.TradeOffer:
                    var offer = new TradeOfferCommand { InstanceIds = new string[Count(r)] };
                    for (int i = 0; i < offer.InstanceIds.Length; i++)
                    {
                        offer.InstanceIds[i] = Str(r);
                    }
                    offer.Gold = r.ReadInt64();
                    return offer;
                case MessageType.TradeConfirm:
                    return new TradeConfirmCommand();
                case MessageType.TradeCancel:
                    return new TradeCancelCommand();
                case MessageType.SendChat:
                    return new SendChatCommand { Channel = (ChatChannel)r.ReadByte(), Text = Str(r), Target = Str(r) };
                case MessageType.Chat:
                    return new ChatMessage { Channel = (ChatChannel)r.ReadByte(), SenderId = Id(r), SenderName = Str(r), TargetName = Str(r), Text = Str(r), TextKey = Str(r) };
                case MessageType.InsertIngredient:
                    return new InsertIngredientCommand { ItemInstanceId = Str(r), SocketIndex = r.ReadInt32(), IngredientBagIndex = r.ReadInt32(), StationId = Id(r) };
                case MessageType.PickUpLoot:
                    return new PickUpLootCommand { LootId = Id(r) };
                case MessageType.SetAutoLoot:
                    return new SetAutoLootCommand { Enabled = r.ReadBoolean() };
                case MessageType.PlayEmote:
                    return new EmoteCommand { EmoteId = Str(r) };
                case MessageType.Emote:
                    return new EmoteMessage { EntityId = Id(r), EmoteId = Str(r) };
                case MessageType.World:
                    return ReadWorld(r);
                case MessageType.PlayerState:
                    return ReadState(r);
                case MessageType.CombatEvent:
                    return new CombatEventMessage
                    {
                        SourceId = Id(r), TargetId = Id(r), SkillId = Str(r), Kind = (CombatEventKind)r.ReadByte(), Amount = r.ReadInt32(), CritTier = r.ReadInt32(),
                        Absorbed = r.ReadInt32(),
                    };
                case MessageType.Rejected:
                    return new CommandRejectedMessage { ReasonKey = Str(r) };
                case MessageType.Notification:
                    return new NotificationMessage { TextKey = Str(r), Number = r.ReadInt32(), ItemNameKey = Str(r), IsPositive = r.ReadBoolean() };
                default:
                    throw new InvalidDataException($"Unknown message type {type}");
            }
        }

        private const byte EntityHasStatic = 1;
        private const byte EntityHasDynamic = 2;

        private static void WriteWorld(BinaryWriter w, WorldSnapshot m, Dictionary<EntityId, EntitySnapshot> sent)
        {
            w.Write(m.Tick);
            w.Write(m.ServerTime);
            w.Write(m.Entities.Length);
            Dictionary<EntityId, EntitySnapshot> now = sent == null ? null : new Dictionary<EntityId, EntitySnapshot>(m.Entities.Length);
            foreach (EntitySnapshot e in m.Entities)
            {
                EntitySnapshot before = default;
                bool known = sent != null && sent.TryGetValue(e.Id, out before);
                bool dynamic = !known || !SameDynamic(e, before);
                Id(w, e.Id);
                w.Write((byte)e.Kind);
                w.Write((byte)((known ? 0 : EntityHasStatic) | (dynamic ? EntityHasDynamic : 0)));
                if (!known)
                {
                    Str(w, e.DefinitionId);
                    Str(w, e.NameKey);
                    Str(w, e.DisplayName);
                    Str(w, e.Faction);
                }
                if (dynamic)
                {
                    w.Write(e.Position.X);
                    w.Write(e.Position.Y);
                    w.Write(e.Position.Z);
                    w.Write(e.Yaw);
                    w.Write(e.Health);
                    w.Write(e.MaxHealth);
                    w.Write(e.IsDead);
                    w.Write(e.IsCasting);
                    StatusSnapshot[] statuses = e.Statuses ?? new StatusSnapshot[0];
                    w.Write(statuses.Length);
                    foreach (StatusSnapshot s in statuses)
                    {
                        Str(w, s.StatusId);
                        w.Write(s.Stacks);
                        w.Write(s.Remaining);
                    }
                    Id(w, e.TargetOfId);
                }
                if (now != null)
                {
                    now[e.Id] = e;
                }
            }
            if (sent != null)
            {
                // Exactly what the client has now: an entity that left its view is sent in full when it comes back.
                sent.Clear();
                foreach (KeyValuePair<EntityId, EntitySnapshot> pair in now)
                {
                    sent[pair.Key] = pair.Value;
                }
            }
        }

        private static bool SameDynamic(EntitySnapshot a, EntitySnapshot b)
        {
            if (a.Position.X != b.Position.X || a.Position.Y != b.Position.Y || a.Position.Z != b.Position.Z || a.Yaw != b.Yaw
                || a.Health != b.Health || a.MaxHealth != b.MaxHealth || a.IsDead != b.IsDead || a.IsCasting != b.IsCasting
                || a.TargetOfId != b.TargetOfId)
            {
                return false;
            }
            int count = a.Statuses?.Length ?? 0;
            if (count != (b.Statuses?.Length ?? 0))
            {
                return false;
            }
            for (int i = 0; i < count; i++)
            {
                if (a.Statuses[i].StatusId != b.Statuses[i].StatusId || a.Statuses[i].Stacks != b.Statuses[i].Stacks || a.Statuses[i].Remaining != b.Statuses[i].Remaining)
                {
                    return false;
                }
            }
            return true;
        }

        private static WorldSnapshot ReadWorld(BinaryReader r)
        {
            var m = new WorldSnapshot { Tick = r.ReadInt32(), ServerTime = r.ReadSingle() };
            m.Entities = new EntitySnapshot[r.ReadInt32()];
            for (int i = 0; i < m.Entities.Length; i++)
            {
                var e = new EntitySnapshot { Id = Id(r), Kind = (EntityKind)r.ReadByte() };
                byte flags = r.ReadByte();
                if ((flags & EntityHasStatic) != 0)
                {
                    e.DefinitionId = Str(r);
                    e.NameKey = Str(r);
                    e.DisplayName = Str(r);
                    e.Faction = Str(r);
                }
                if ((flags & EntityHasDynamic) != 0)
                {
                    e.Position = new Vec3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                    e.Yaw = r.ReadSingle();
                    e.Health = r.ReadSingle();
                    e.MaxHealth = r.ReadSingle();
                    e.IsDead = r.ReadBoolean();
                    e.IsCasting = r.ReadBoolean();
                    var statuses = new StatusSnapshot[r.ReadInt32()];
                    for (int k = 0; k < statuses.Length; k++)
                    {
                        statuses[k] = new StatusSnapshot { StatusId = Str(r), Stacks = r.ReadInt32(), Remaining = r.ReadSingle() };
                    }
                    e.Statuses = statuses;
                    e.TargetOfId = Id(r);
                }
                else
                {
                    e.DynamicOmitted = true;
                }
                m.Entities[i] = e;
            }
            return m;
        }

        private static void WriteState(BinaryWriter w, PlayerStateSnapshot m)
        {
            Id(w, m.EntityId);
            Str(w, m.ClassId);
            w.Write(m.Level);
            w.Write(m.Experience);
            w.Write(m.ExperienceToNext);
            w.Write(m.AttributePoints);
            w.Write(m.ClassPoints);
            w.Write(m.Health);
            w.Write(m.MaxHealth);
            w.Write(m.MainMana);
            w.Write(m.MaxMainMana);
            w.Write(m.ReserveMana);
            w.Write(m.MaxReserveMana);
            w.Write(m.MainManaLocked);
            w.Write(m.InCombat);
            w.Write(m.IsDead);
            Id(w, m.TargetId);
            Str(w, m.CastSkillId);
            w.Write(m.CastProgress);
            w.Write(m.GlobalCooldownRemaining);
            w.Write(m.GlobalCooldownDuration);
            w.Write(m.MainHandSwapLockRemaining);
            w.Write(m.MainHandSwapRiskRemaining);
            w.Write(m.OffHandSwapLockRemaining);

            w.Write(m.Cooldowns.Length);
            foreach (SkillCooldownSnapshot c in m.Cooldowns)
            {
                Str(w, c.SkillId);
                w.Write(c.Remaining);
                w.Write(c.Duration);
            }
            w.Write(m.Skills.Length);
            foreach (SkillStateSnapshot s in m.Skills)
            {
                Str(w, s.SkillId);
                w.Write(s.Unlocked);
                w.Write(s.Mastery);
                w.Write(s.Efficiency);
                w.Write(s.EmpoweredAvailable);
                w.Write(s.WeakenedAvailable);
            }
            // Sparse bag (module 3 traffic): the size, then only the occupied slots.
            w.Write(m.Bag.Length);
            int occupied = 0;
            foreach (ItemSlotSnapshot b in m.Bag)
            {
                occupied += b.DefinitionId != null ? 1 : 0;
            }
            w.Write(occupied);
            foreach (ItemSlotSnapshot b in m.Bag)
            {
                if (b.DefinitionId == null)
                {
                    continue;
                }
                w.Write(b.Index);
                Str(w, b.InstanceId);
                Str(w, b.DefinitionId);
                w.Write(b.Count);
                w.Write(b.UpgradeLevel);
                WriteItemDetails(w, b.Sockets, b.Affixes);
            }
            w.Write(m.Equipment.Length);
            foreach (EquipmentSlotSnapshot e in m.Equipment)
            {
                Str(w, e.SlotId);
                Str(w, e.InstanceId);
                Str(w, e.DefinitionId);
                w.Write(e.UpgradeLevel);
                WriteItemDetails(w, e.Sockets, e.Affixes);
            }
            w.Write(m.Stats.Length);
            foreach (StatSnapshot s in m.Stats)
            {
                w.Write((int)s.Stat);
                w.Write(s.Value);
            }
            w.Write(m.ConsumableCooldowns.Length);
            foreach (CooldownGroupSnapshot g in m.ConsumableCooldowns)
            {
                Str(w, g.Group);
                w.Write(g.Remaining);
            }
            w.Write(m.AutoLoot);
            CurrencySnapshot[] currencies = m.Currencies ?? new CurrencySnapshot[0];
            w.Write(currencies.Length);
            foreach (CurrencySnapshot c in currencies)
            {
                Str(w, c.CurrencyId);
                w.Write(c.Amount);
            }
            Str(w, m.CraftRecipeId);
            w.Write(m.CraftProgress);
            w.Write((byte)m.Duel.Phase);
            Id(w, m.Duel.OpponentId);
            w.Write(m.Duel.Remaining);
            w.Write(m.Duel.CenterX);
            w.Write(m.Duel.CenterZ);
            w.Write(m.Duel.Radius);
            WriteGroup(w, m.Group ?? new GroupSnapshot());
            FriendSnapshot[] friends = m.Friends ?? new FriendSnapshot[0];
            w.Write(friends.Length);
            foreach (FriendSnapshot f in friends)
            {
                Str(w, f.Name);
                w.Write(f.Online);
                w.Write((byte)f.State);
            }
            WriteChatTabs(w, m.ChatTabs);
            MarketStateSnapshot market = m.Market ?? new MarketStateSnapshot();
            Id(w, market.MyMerchantId);
            w.Write(market.UnreadMail);
            w.Write(market.MailVersion);
            w.Write(market.MarketVersion);
            w.Write(market.OrdersVersion);
            WriteTrade(w, m.Trade ?? new TradeSnapshot());
        }

        private static void WriteGroup(BinaryWriter w, GroupSnapshot g)
        {
            w.Write(g.InGroup);
            Id(w, g.LeaderId);
            w.Write((byte)g.LootMode);
            Ids(w, g.Members);
            w.Write(g.IsRaid);
            int[] subgroups = g.Subgroups ?? new int[0];
            w.Write(subgroups.Length);
            foreach (int sub in subgroups)
            {
                w.Write((byte)sub);
            }
            Ids(w, g.Assistants);
            Ids(w, g.Voters);
            Id(w, g.InviteFromId);
            w.Write(g.InviteRemaining);
            w.Write(g.VoteActive);
            w.Write(g.VoteProposalId);
            Id(w, g.VoteProposerId);
            w.Write((byte)g.VoteMode);
            w.Write(g.VoteRemaining);
            Ids(w, g.VotesFor);
            w.Write(g.ProposeCooldownRemaining);
            w.Write(g.Log.Length);
            foreach (GroupLogEntry e in g.Log)
            {
                Str(w, e.RecipientName);
                Str(w, e.ItemDefinitionId);
                w.Write(e.Count);
                w.Write((byte)e.Mode);
            }
        }

        private static GroupSnapshot ReadGroup(BinaryReader r)
        {
            var g = new GroupSnapshot
            {
                InGroup = r.ReadBoolean(),
                LeaderId = Id(r),
                LootMode = (LootMode)r.ReadByte(),
                Members = Ids(r),
                IsRaid = r.ReadBoolean(),
                Subgroups = ReadSubgroups(r),
                Assistants = Ids(r),
                Voters = Ids(r),
                InviteFromId = Id(r),
                InviteRemaining = r.ReadSingle(),
                VoteActive = r.ReadBoolean(),
                VoteProposalId = r.ReadInt32(),
                VoteProposerId = Id(r),
                VoteMode = (LootMode)r.ReadByte(),
                VoteRemaining = r.ReadSingle(),
                VotesFor = Ids(r),
                ProposeCooldownRemaining = r.ReadSingle(),
            };
            g.Log = new GroupLogEntry[r.ReadInt32()];
            for (int i = 0; i < g.Log.Length; i++)
            {
                g.Log[i] = new GroupLogEntry { RecipientName = Str(r), ItemDefinitionId = Str(r), Count = r.ReadInt32(), Mode = (LootMode)r.ReadByte() };
            }
            return g;
        }

        /// <summary>
        /// Element count of an array a client sends: never negative and never more than the bytes left (every element
        /// takes at least one), so a forged count cannot make the server allocate a huge array.
        /// </summary>
        private static int Count(BinaryReader r)
        {
            int count = r.ReadInt32();
            if (count < 0 || count > r.BaseStream.Length - r.BaseStream.Position)
            {
                throw new InvalidDataException("bad array length");
            }
            return count;
        }

        /// <summary>An item with its details (sockets, affixes) for market, mail and order windows.</summary>
        private static void WriteSlot(BinaryWriter w, ItemSlotSnapshot item)
        {
            w.Write(item.Index);
            Str(w, item.InstanceId);
            Str(w, item.DefinitionId);
            w.Write(item.Count);
            w.Write(item.UpgradeLevel);
            WriteItemDetails(w, item.Sockets, item.Affixes);
        }

        private static ItemSlotSnapshot ReadSlot(BinaryReader r)
        {
            var item = new ItemSlotSnapshot { Index = r.ReadInt32(), InstanceId = Str(r), DefinitionId = Str(r), Count = r.ReadInt32(), UpgradeLevel = r.ReadInt32() };
            ReadItemDetails(r, out item.Sockets, out item.Affixes);
            return item;
        }

        private static void WriteLots(BinaryWriter w, LotSnapshot[] lots)
        {
            lots = lots ?? new LotSnapshot[0];
            w.Write(lots.Length);
            foreach (LotSnapshot lot in lots)
            {
                w.Write(lot.LotId);
                Str(w, lot.ShopName);
                Id(w, lot.MerchantId);
                WriteSlot(w, lot.Item);
                w.Write(lot.PricePerUnit);
                w.Write(lot.PriceVersion);
            }
        }

        private static LotSnapshot[] ReadLots(BinaryReader r)
        {
            var lots = new LotSnapshot[r.ReadInt32()];
            for (int i = 0; i < lots.Length; i++)
            {
                lots[i] = new LotSnapshot { LotId = r.ReadInt64(), ShopName = Str(r), MerchantId = Id(r), Item = ReadSlot(r), PricePerUnit = r.ReadInt64(), PriceVersion = r.ReadInt32() };
            }
            return lots;
        }

        private static void WriteSettings(BinaryWriter w, AccountSettingsSnapshot s)
        {
            w.Write(s.SchemaVersion);
            KeyBindingSnapshot[] bindings = s.Bindings ?? new KeyBindingSnapshot[0];
            w.Write(bindings.Length);
            foreach (KeyBindingSnapshot binding in bindings)
            {
                Str(w, binding.BindingId);
                Str(w, binding.Path);
            }
            w.Write(s.CameraSensitivity);
            w.Write(s.InvertY);
            w.Write(s.ZoomSpeed);
            w.Write(s.UiScale);
            w.Write(s.PanelOpacity);
            w.Write(s.DamageNumbers);
            w.Write(s.Nameplates);
            w.Write(s.NameplateHealth);
        }

        /// <summary>Also read from client commands: the binding count is bounded (see Count).</summary>
        private static AccountSettingsSnapshot ReadSettings(BinaryReader r)
        {
            var s = new AccountSettingsSnapshot { SchemaVersion = r.ReadInt32(), Bindings = new KeyBindingSnapshot[Count(r)] };
            for (int i = 0; i < s.Bindings.Length; i++)
            {
                s.Bindings[i] = new KeyBindingSnapshot { BindingId = Str(r), Path = Str(r) };
            }
            s.CameraSensitivity = r.ReadSingle();
            s.InvertY = r.ReadBoolean();
            s.ZoomSpeed = r.ReadSingle();
            s.UiScale = r.ReadSingle();
            s.PanelOpacity = r.ReadSingle();
            s.DamageNumbers = r.ReadBoolean();
            s.Nameplates = r.ReadBoolean();
            s.NameplateHealth = r.ReadBoolean();
            return s;
        }

        private static void WriteLoadouts(BinaryWriter w, LoadoutSnapshot[] loadouts)
        {
            loadouts = loadouts ?? new LoadoutSnapshot[0];
            w.Write(loadouts.Length);
            foreach (LoadoutSnapshot loadout in loadouts)
            {
                LoadoutItemSnapshot[] items = loadout?.Items ?? new LoadoutItemSnapshot[0];
                w.Write(items.Length);
                foreach (LoadoutItemSnapshot item in items)
                {
                    Str(w, item.SlotId);
                    Str(w, item.InstanceId);
                    Str(w, item.DefinitionId);
                }
            }
        }

        private static LoadoutSnapshot[] ReadLoadouts(BinaryReader r)
        {
            var loadouts = new LoadoutSnapshot[Count(r)];
            for (int i = 0; i < loadouts.Length; i++)
            {
                var items = new LoadoutItemSnapshot[Count(r)];
                for (int k = 0; k < items.Length; k++)
                {
                    items[k] = new LoadoutItemSnapshot { SlotId = Str(r), InstanceId = Str(r), DefinitionId = Str(r) };
                }
                loadouts[i] = new LoadoutSnapshot { Items = items };
            }
            return loadouts;
        }

        /// <summary>String arrays a client sends (bounded by Count).</summary>
        private static void Strs(BinaryWriter w, string[] values)
        {
            values = values ?? new string[0];
            w.Write(values.Length);
            foreach (string value in values)
            {
                Str(w, value);
            }
        }

        private static string[] Strs(BinaryReader r)
        {
            var values = new string[Count(r)];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = Str(r);
            }
            return values;
        }

        private static void WriteChatTabs(BinaryWriter w, ChatTabSnapshot[] tabs)
        {
            tabs = tabs ?? new ChatTabSnapshot[0];
            w.Write(tabs.Length);
            foreach (ChatTabSnapshot tab in tabs)
            {
                Str(w, tab?.Name);
                Str(w, tab?.NameKey);
                ChatChannel[] channels = tab?.Channels ?? new ChatChannel[0];
                w.Write(channels.Length);
                foreach (ChatChannel channel in channels)
                {
                    w.Write((byte)channel);
                }
            }
        }

        /// <summary>Also read from client commands, so the counts are bounded (see Count).</summary>
        private static ChatTabSnapshot[] ReadChatTabs(BinaryReader r)
        {
            var tabs = new ChatTabSnapshot[Count(r)];
            for (int i = 0; i < tabs.Length; i++)
            {
                var tab = new ChatTabSnapshot { Name = Str(r), NameKey = Str(r) };
                tab.Channels = new ChatChannel[Count(r)];
                for (int k = 0; k < tab.Channels.Length; k++)
                {
                    tab.Channels[k] = (ChatChannel)r.ReadByte();
                }
                tabs[i] = tab;
            }
            return tabs;
        }

        private static int[] ReadSubgroups(BinaryReader r)
        {
            var result = new int[r.ReadInt32()];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = r.ReadByte();
            }
            return result;
        }

        private static void WriteTrade(BinaryWriter w, TradeSnapshot t)
        {
            w.Write((byte)t.Phase);
            Id(w, t.PartnerId);
            w.Write(t.Remaining);
            WriteTradeItems(w, t.MyItems);
            w.Write(t.MyGold);
            w.Write(t.MyConfirmed);
            WriteTradeItems(w, t.TheirItems);
            w.Write(t.TheirGold);
            w.Write(t.TheirConfirmed);
        }

        private static TradeSnapshot ReadTrade(BinaryReader r) => new TradeSnapshot
        {
            Phase = (TradePhase)r.ReadByte(),
            PartnerId = Id(r),
            Remaining = r.ReadSingle(),
            MyItems = ReadTradeItems(r),
            MyGold = r.ReadInt64(),
            MyConfirmed = r.ReadBoolean(),
            TheirItems = ReadTradeItems(r),
            TheirGold = r.ReadInt64(),
            TheirConfirmed = r.ReadBoolean(),
        };

        private static void WriteTradeItems(BinaryWriter w, ItemSlotSnapshot[] items)
        {
            items = items ?? new ItemSlotSnapshot[0];
            w.Write(items.Length);
            foreach (ItemSlotSnapshot item in items)
            {
                w.Write(item.Index);
                Str(w, item.InstanceId);
                Str(w, item.DefinitionId);
                w.Write(item.Count);
                w.Write(item.UpgradeLevel);
            }
        }

        private static ItemSlotSnapshot[] ReadTradeItems(BinaryReader r)
        {
            var items = new ItemSlotSnapshot[r.ReadInt32()];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = new ItemSlotSnapshot { Index = r.ReadInt32(), InstanceId = Str(r), DefinitionId = Str(r), Count = r.ReadInt32(), UpgradeLevel = r.ReadInt32() };
            }
            return items;
        }

        private static void WriteItemDetails(BinaryWriter w, SocketSnapshot[] sockets, AffixSnapshot[] affixes)
        {
            sockets = sockets ?? new SocketSnapshot[0];
            affixes = affixes ?? new AffixSnapshot[0];
            w.Write((byte)sockets.Length);
            foreach (SocketSnapshot s in sockets)
            {
                Str(w, s.Type);
                w.Write(s.IsOvercap);
                Str(w, s.IngredientId);
            }
            w.Write((byte)affixes.Length);
            foreach (AffixSnapshot a in affixes)
            {
                w.Write((byte)a.Stat);
                w.Write((byte)a.Op);
                w.Write(a.Value);
            }
        }

        private static void ReadItemDetails(BinaryReader r, out SocketSnapshot[] sockets, out AffixSnapshot[] affixes)
        {
            sockets = new SocketSnapshot[r.ReadByte()];
            for (int i = 0; i < sockets.Length; i++)
            {
                sockets[i] = new SocketSnapshot { Type = Str(r), IsOvercap = r.ReadBoolean(), IngredientId = Str(r) };
            }
            affixes = new AffixSnapshot[r.ReadByte()];
            for (int i = 0; i < affixes.Length; i++)
            {
                affixes[i] = new AffixSnapshot { Stat = (StatId)r.ReadByte(), Op = (ModifierOp)r.ReadByte(), Value = r.ReadSingle() };
            }
        }

        private static void Ids(BinaryWriter w, EntityId[] ids)
        {
            ids = ids ?? new EntityId[0];
            w.Write(ids.Length);
            foreach (EntityId id in ids)
            {
                Id(w, id);
            }
        }

        private static EntityId[] Ids(BinaryReader r)
        {
            var ids = new EntityId[r.ReadInt32()];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = Id(r);
            }
            return ids;
        }

        private static PlayerStateSnapshot ReadState(BinaryReader r)
        {
            var m = new PlayerStateSnapshot
            {
                EntityId = Id(r),
                ClassId = Str(r),
                Level = r.ReadInt32(),
                Experience = r.ReadInt32(),
                ExperienceToNext = r.ReadInt32(),
                AttributePoints = r.ReadInt32(),
                ClassPoints = r.ReadInt32(),
                Health = r.ReadSingle(),
                MaxHealth = r.ReadSingle(),
                MainMana = r.ReadSingle(),
                MaxMainMana = r.ReadSingle(),
                ReserveMana = r.ReadSingle(),
                MaxReserveMana = r.ReadSingle(),
                MainManaLocked = r.ReadBoolean(),
                InCombat = r.ReadBoolean(),
                IsDead = r.ReadBoolean(),
                TargetId = Id(r),
                CastSkillId = Str(r),
                CastProgress = r.ReadSingle(),
                GlobalCooldownRemaining = r.ReadSingle(),
                GlobalCooldownDuration = r.ReadSingle(),
                MainHandSwapLockRemaining = r.ReadSingle(),
                MainHandSwapRiskRemaining = r.ReadSingle(),
                OffHandSwapLockRemaining = r.ReadSingle(),
            };
            m.Cooldowns = new SkillCooldownSnapshot[r.ReadInt32()];
            for (int i = 0; i < m.Cooldowns.Length; i++)
            {
                m.Cooldowns[i] = new SkillCooldownSnapshot { SkillId = Str(r), Remaining = r.ReadSingle(), Duration = r.ReadSingle() };
            }
            m.Skills = new SkillStateSnapshot[r.ReadInt32()];
            for (int i = 0; i < m.Skills.Length; i++)
            {
                m.Skills[i] = new SkillStateSnapshot
                {
                    SkillId = Str(r), Unlocked = r.ReadBoolean(), Mastery = r.ReadSingle(), Efficiency = r.ReadSingle(),
                    EmpoweredAvailable = r.ReadBoolean(), WeakenedAvailable = r.ReadBoolean(),
                };
            }
            m.Bag = new ItemSlotSnapshot[r.ReadInt32()];
            for (int i = 0; i < m.Bag.Length; i++)
            {
                m.Bag[i] = new ItemSlotSnapshot { Index = i };
            }
            int occupiedSlots = r.ReadInt32();
            for (int i = 0; i < occupiedSlots; i++)
            {
                var slot = new ItemSlotSnapshot { Index = r.ReadInt32(), InstanceId = Str(r), DefinitionId = Str(r), Count = r.ReadInt32(), UpgradeLevel = r.ReadInt32() };
                ReadItemDetails(r, out slot.Sockets, out slot.Affixes);
                if (slot.Index >= 0 && slot.Index < m.Bag.Length)
                {
                    m.Bag[slot.Index] = slot;
                }
            }
            m.Equipment = new EquipmentSlotSnapshot[r.ReadInt32()];
            for (int i = 0; i < m.Equipment.Length; i++)
            {
                m.Equipment[i] = new EquipmentSlotSnapshot { SlotId = Str(r), InstanceId = Str(r), DefinitionId = Str(r), UpgradeLevel = r.ReadInt32() };
                ReadItemDetails(r, out m.Equipment[i].Sockets, out m.Equipment[i].Affixes);
            }
            m.Stats = new StatSnapshot[r.ReadInt32()];
            for (int i = 0; i < m.Stats.Length; i++)
            {
                m.Stats[i] = new StatSnapshot { Stat = (StatId)r.ReadInt32(), Value = r.ReadSingle() };
            }
            m.ConsumableCooldowns = new CooldownGroupSnapshot[r.ReadInt32()];
            for (int i = 0; i < m.ConsumableCooldowns.Length; i++)
            {
                m.ConsumableCooldowns[i] = new CooldownGroupSnapshot { Group = Str(r), Remaining = r.ReadSingle() };
            }
            m.AutoLoot = r.ReadBoolean();
            m.Currencies = new CurrencySnapshot[r.ReadInt32()];
            for (int i = 0; i < m.Currencies.Length; i++)
            {
                m.Currencies[i] = new CurrencySnapshot { CurrencyId = Str(r), Amount = r.ReadInt64() };
            }
            m.CraftRecipeId = Str(r);
            m.CraftProgress = r.ReadSingle();
            m.Duel = new DuelSnapshot
            {
                Phase = (DuelPhase)r.ReadByte(), OpponentId = Id(r), Remaining = r.ReadSingle(), CenterX = r.ReadSingle(), CenterZ = r.ReadSingle(), Radius = r.ReadSingle(),
            };
            m.Group = ReadGroup(r);
            m.Friends = new FriendSnapshot[r.ReadInt32()];
            for (int i = 0; i < m.Friends.Length; i++)
            {
                m.Friends[i] = new FriendSnapshot { Name = Str(r), Online = r.ReadBoolean(), State = (FriendState)r.ReadByte() };
            }
            m.ChatTabs = ReadChatTabs(r);
            m.Market = new MarketStateSnapshot { MyMerchantId = Id(r), UnreadMail = r.ReadInt32(), MailVersion = r.ReadInt32(), MarketVersion = r.ReadInt32(), OrdersVersion = r.ReadInt32() };
            m.Trade = ReadTrade(r);
            return m;
        }

        private static void Id(BinaryWriter w, EntityId id) => w.Write(id.Value);

        private static EntityId Id(BinaryReader r) => new EntityId(r.ReadInt32());

        private static void Str(BinaryWriter w, string value)
        {
            w.Write(value != null);
            if (value != null)
            {
                w.Write(value);
            }
        }

        private static string Str(BinaryReader r) => r.ReadBoolean() ? r.ReadString() : null;
    }
}
