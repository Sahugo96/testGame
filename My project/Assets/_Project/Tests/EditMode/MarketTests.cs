using System;
using System.Linq;
using Game.Network;
using Game.Server.Items;
using Game.Shared.Content;
using Game.Shared.Core;
using Game.Shared.Protocol;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>Module 20: personal shops, market search and purchase, mail, upgrade orders with escrow (05 7.14.3, 04 7.14.1).</summary>
    public sealed class MarketTests
    {
        private ServerHarness _h;
        private ManualClock _clock;

        [SetUp]
        public void SetUp()
        {
            _h = new ServerHarness();
            _clock = new ManualClock();
            _h.Server.Context.Clock = _clock;
        }

        private EntityId Player(string classId, string name) => _h.Server.AddPlayer(classId, name);

        private long Gold(EntityId id) => _h.Player(id).Wallet.TryGetValue(_h.Content.Config.GoldCurrencyId, out long gold) ? gold : 0;

        private void GoTo(EntityId player, string stationType) => _h.PlaceNear(player, _h.FindStation(stationType), 1f);

        private T LastReply<T>(EntityId player) where T : class => _h.Player(player).Outbox.Replies.OfType<T>().LastOrDefault();

        private ItemInstance BagItem(EntityId player, string definitionId) => _h.Player(player).Bag.Get(_h.BagIndexOf(player, definitionId));

        private int CountOf(EntityId player, string definitionId)
        {
            int count = 0;
            for (int i = 0; i < _h.Player(player).Bag.Size; i++)
            {
                ItemInstance item = _h.Player(player).Bag.Get(i);
                count += item != null && item.DefinitionId == definitionId ? item.Count : 0;
            }
            return count;
        }

        /// <summary>Opens a small shop for the seller and lists the shard stack at the price; returns the merchant.</summary>
        private EntityId OpenShopWithShards(EntityId seller, long price)
        {
            GoTo(seller, StationTypes.ShopSpot);
            _h.Send(seller, new OpenShopCommand { SpotId = _h.FindStation(StationTypes.ShopSpot), TierId = "small", Name = "  Лавка\u0007 Ани  " });
            EntityId merchant = _h.Player(seller).Outbox.State.Market.MyMerchantId;
            Assert.That(merchant.IsValid, Is.True, string.Join(",", _h.Rejections(seller)));
            _h.PlaceNear(seller, merchant, 1f);
            _h.Send(seller, new ListLotCommand { ItemInstanceId = BagItem(seller, "philosopher_stone_shard").SourceId, PricePerUnit = price });
            return merchant;
        }

        private LotSnapshot SearchOne(EntityId buyer)
        {
            GoTo(buyer, StationTypes.Market);
            _h.Run(_h.Content.Config.MarketSearchMinIntervalSeconds);
            _h.Send(buyer, new MarketSearchCommand { Rarity = -1 });
            return LastReply<MarketPageMessage>(buyer).Lots.Single();
        }

        [Test]
        public void Shop_ListBuy_PartOfStack_CommissionByMail_PriceVersionAndRepeat()
        {
            EntityId seller = Player("warrior", "Аня");
            EntityId buyer = Player("mage", "Боб");
            long startGold = Gold(seller);
            string stackId = BagItem(seller, "philosopher_stone_shard").SourceId;
            EntityId merchant = OpenShopWithShards(seller, 3);
            Assert.That(Gold(seller), Is.EqualTo(startGold - 20), "rent of the small shop is a gold sink");
            Assert.That(_h.BagIndexOf(seller, "philosopher_stone_shard"), Is.EqualTo(-1), "the listed stack is held by the server");

            _h.Send(buyer, new ViewShopCommand { MerchantId = merchant });
            ShopViewMessage view = LastReply<ShopViewMessage>(buyer);
            Assert.That(view.Name, Is.EqualTo("Лавка Ани"), "name cleaned");
            Assert.That(view.IsMine, Is.False);

            LotSnapshot lot = SearchOne(buyer);
            Assert.That((lot.Item.DefinitionId, lot.Item.Count, lot.PricePerUnit, lot.ShopName), Is.EqualTo(("philosopher_stone_shard", 20, 3L, "Лавка Ани")));
            int buyerShards = CountOf(buyer, "philosopher_stone_shard");
            long buyerGold = Gold(buyer);

            var buy = new BuyLotCommand { LotId = lot.LotId, Count = 5, PriceVersion = lot.PriceVersion, RequestId = "r1" };
            _h.Send(buyer, buy);
            _h.Send(buyer, buy);
            Assert.That(Gold(buyer), Is.EqualTo(buyerGold - 15), "the repeated request is ignored");
            Assert.That(CountOf(buyer, "philosopher_stone_shard"), Is.EqualTo(buyerShards + 5));
            Assert.That(_h.Audit.Entries.Any(e => e.Action == AuditActions.ShopSale && e.Amount == 15), Is.True);

            // The seller changes the price: a buyer who saw the old version is refused.
            _h.PlaceNear(seller, merchant, 1f);
            _h.Send(seller, new SetLotPriceCommand { LotId = lot.LotId, PricePerUnit = 4 });
            _h.Send(buyer, new BuyLotCommand { LotId = lot.LotId, Count = 1, PriceVersion = lot.PriceVersion, RequestId = "r2" });
            Assert.That(_h.Rejections(buyer), Does.Contain(RejectReasons.MarketPriceChanged));
            Assert.That(SearchOne(buyer).Item.Count, Is.EqualTo(15));

            // Income minus 15% commission (floor 2 of 15) arrives by mail; claiming it pays the seller.
            GoTo(seller, StationTypes.Mailbox);
            _h.Send(seller, new ViewMailboxCommand());
            MailSnapshot sale = LastReply<MailboxMessage>(seller).Mail.Single(m => m.Type == MailType.Sale);
            Assert.That((sale.Gold, sale.Number, sale.SubjectKey), Is.EqualTo((13L, 5, MailSubjects.Sold)));
            long before = Gold(seller);
            _h.Send(seller, new ClaimMailCommand { MailId = sale.Id });
            Assert.That(Gold(seller), Is.EqualTo(before + 13));
            Assert.That(_h.Player(seller).Outbox.State.Market.UnreadMail, Is.EqualTo(0));

            // The rest of the stack is still the original instance.
            _h.PlaceNear(seller, merchant, 1f);
            _h.Send(seller, new UnlistLotCommand { LotId = lot.LotId });
            Assert.That(BagItem(seller, "philosopher_stone_shard").SourceId, Is.EqualTo(stackId));
            Assert.That(BagItem(seller, "philosopher_stone_shard").Count, Is.EqualTo(15));
        }

        [Test]
        public void Shop_TimeIsOver_MerchantLeaves_LotsComeBackByMail()
        {
            EntityId seller = Player("warrior", "Аня");
            string stackId = BagItem(seller, "philosopher_stone_shard").SourceId;
            EntityId merchant = OpenShopWithShards(seller, 3);
            _clock.Advance(TimeSpan.FromHours(_h.Content.Config.ShopTiers.First(t => t.Id == "small").DurationHours + 0.1));
            _h.Run(1.1f);
            Assert.That(_h.Player(seller).Outbox.State.Market.MyMerchantId.IsValid, Is.False);
            Assert.That(_h.World.Exists(merchant), Is.False, "the merchant left");
            Assert.That(_h.Player(seller).Outbox.State.Market.UnreadMail, Is.EqualTo(1));

            GoTo(seller, StationTypes.Mailbox);
            _h.Send(seller, new ViewMailboxCommand());
            MailSnapshot letter = LastReply<MailboxMessage>(seller).Mail.Single();
            Assert.That((letter.Type, letter.SecondsLeft), Is.EqualTo((MailType.Return, -1L)), "returned letters never expire");
            _h.Send(seller, new ClaimMailCommand { MailId = letter.Id });
            Assert.That(BagItem(seller, "philosopher_stone_shard").SourceId, Is.EqualTo(stackId), "the same instance came back");
        }

        [Test]
        public void TwoBuyers_SameLot_SameTick_OnlyOneGetsIt()
        {
            EntityId seller = Player("warrior", "Аня");
            EntityId first = Player("mage", "Боб");
            EntityId second = Player("mage", "Вика");
            OpenShopWithShards(seller, 2);
            LotSnapshot lot = SearchOne(first);
            GoTo(second, StationTypes.Market);
            long goldBefore = Gold(first) + Gold(second);
            _h.Server.Submit(first, new BuyLotCommand { LotId = lot.LotId, Count = 20, PriceVersion = lot.PriceVersion, RequestId = "a" });
            _h.Server.Submit(second, new BuyLotCommand { LotId = lot.LotId, Count = 20, PriceVersion = lot.PriceVersion, RequestId = "b" });
            _h.Server.Tick();
            int winners = (_h.Rejections(first).Contains(RejectReasons.MarketLotGone) ? 0 : 1) + (_h.Rejections(second).Contains(RejectReasons.MarketLotGone) ? 0 : 1);
            Assert.That(winners, Is.EqualTo(1));
            Assert.That(Gold(first) + Gold(second), Is.EqualTo(goldBefore - 40), "only one paid");
        }

        [Test]
        public void Mail_SendWithFee_Claim_ExpiredLetterReturns()
        {
            EntityId a = Player("warrior", "Аня");
            EntityId b = Player("mage", "Боб");
            GoTo(a, StationTypes.Mailbox);
            GoTo(b, StationTypes.Mailbox);
            ItemInstance axe = BagItem(a, "great_axe");
            long goldA = Gold(a);

            _h.Send(a, new SendMailCommand { Recipient = "Призрак", Subject = "x", RequestId = "1" });
            Assert.That(_h.Rejections(a), Does.Contain(RejectReasons.MailNoRecipient));
            _h.Send(a, new SendMailCommand { Recipient = "аня", Subject = "x", RequestId = "2" });
            Assert.That(_h.Rejections(a).Count(r => r == RejectReasons.MailNoRecipient), Is.EqualTo(2), "not to yourself");

            var send = new SendMailCommand { Recipient = "боб", Subject = "Топор", Body = "держи", InstanceIds = new[] { axe.SourceId }, Gold = 10, RequestId = "3" };
            _h.Send(a, send);
            _h.Send(a, send);
            long fee = _h.Content.Config.MailFeePerAttachment;
            Assert.That(Gold(a), Is.EqualTo(goldA - 10 - fee), "gold and the fee, once");
            Assert.That(_h.BagIndexOf(a, "great_axe"), Is.EqualTo(-1));

            _h.Send(b, new ViewMailboxCommand());
            MailSnapshot letter = LastReply<MailboxMessage>(b).Mail.Single();
            Assert.That((letter.From, letter.Subject, letter.Gold, letter.Items.Single().InstanceId), Is.EqualTo(("Аня", "Топор", 10L, axe.SourceId)));
            long goldB = Gold(b);
            _h.Send(b, new ClaimMailCommand { MailId = letter.Id });
            Assert.That(BagItem(b, "great_axe").SourceId, Is.EqualTo(axe.SourceId), "ownership moved, same GUID");
            Assert.That(Gold(b), Is.EqualTo(goldB + 10));
            _h.Send(b, new ClaimMailCommand { MailId = letter.Id });
            Assert.That(Gold(b), Is.EqualTo(goldB + 10), "a claimed letter gives nothing again");
            _h.Send(b, new DeleteMailCommand { MailId = letter.Id });

            // Not claimed for MailExpireDays: back to the sender, and that letter stays.
            ItemInstance helm = BagItem(b, "iron_helm");
            _h.Send(b, new SendMailCommand { Recipient = "Аня", Subject = "шлем", InstanceIds = new[] { helm.SourceId }, RequestId = "4" });
            _clock.Advance(TimeSpan.FromDays(_h.Content.Config.MailExpireDays + 1));
            _h.Run(1.1f);
            _h.Send(b, new ViewMailboxCommand());
            MailSnapshot back = LastReply<MailboxMessage>(b).Mail.Single();
            Assert.That((back.Type, back.SecondsLeft, back.Items.Single().InstanceId), Is.EqualTo((MailType.Return, -1L, helm.SourceId)));
        }

        [Test]
        public void UpgradeOrder_Escrow_SmithNeverHoldsTheItem_PaymentToSmith()
        {
            EntityId owner = Player("warrior", "Аня");
            EntityId smith = Player("mage", "Боб");
            EntityId whetstone = _h.FindStation(StationTypes.Whetstone);
            _h.PlaceNear(owner, whetstone, 1f);
            _h.PlaceNear(smith, whetstone, 1f);
            ItemInstance axe = BagItem(owner, "great_axe");
            long ownerGold = Gold(owner);
            long smithGold = Gold(smith);
            int smithShards = BagItem(smith, "philosopher_stone_shard").Count;

            _h.Send(owner, new CreateUpgradeOrderCommand { ItemInstanceId = axe.SourceId, Payment = 30, SmithName = "Вика", StationId = whetstone });
            Assert.That(_h.Rejections(owner), Does.Contain(RejectReasons.MailNoRecipient), "a named smith must exist");
            _h.Send(owner, new CreateUpgradeOrderCommand { ItemInstanceId = axe.SourceId, Payment = 30, StationId = whetstone });
            Assert.That(Gold(owner), Is.EqualTo(ownerGold - 30), "the payment is held in escrow");
            Assert.That(_h.BagIndexOf(owner, "great_axe"), Is.EqualTo(-1));

            _h.Send(smith, new ViewUpgradeOrdersCommand());
            UpgradeOrderSnapshot order = LastReply<UpgradeOrdersMessage>(smith).Orders.Single();
            Assert.That((order.OwnerName, order.Payment, order.Item.InstanceId, order.IsMine), Is.EqualTo(("Аня", 30L, axe.SourceId, false)));
            _h.Send(owner, new FulfillUpgradeOrderCommand { OrderId = order.Id, StationId = whetstone });
            Assert.That(_h.Rejections(owner), Does.Contain(RejectReasons.OrderNotForYou), "not your own order");

            _h.Send(smith, new FulfillUpgradeOrderCommand { OrderId = order.Id, StationId = whetstone });
            Assert.That(_h.BagIndexOf(smith, "great_axe"), Is.EqualTo(-1), "the smith never holds the item");
            Assert.That(BagItem(smith, "philosopher_stone_shard").Count, Is.EqualTo(smithShards - 1), "the smith's material");
            long stepCost = _h.Content.UpgradeRules["default"].Steps[0].GoldCost;
            Assert.That(Gold(smith), Is.EqualTo(smithGold - stepCost + 30), "the smith pays the step and gets the payment");

            GoTo(owner, StationTypes.Mailbox);
            _h.Send(owner, new ViewMailboxCommand());
            MailSnapshot letter = LastReply<MailboxMessage>(owner).Mail.Single();
            Assert.That((letter.SubjectKey, letter.Number, letter.Detail), Is.EqualTo((MailSubjects.OrderSuccess, 1, "Боб")));
            _h.Send(owner, new ClaimMailCommand { MailId = letter.Id });
            Assert.That(BagItem(owner, "great_axe").SourceId, Is.EqualTo(axe.SourceId));
            Assert.That(BagItem(owner, "great_axe").UpgradeLevel, Is.EqualTo(1));

            // Cancelling an open order gives everything back at once.
            _h.PlaceNear(owner, whetstone, 1f);
            long beforeCancel = Gold(owner);
            _h.Send(owner, new CreateUpgradeOrderCommand { ItemInstanceId = axe.SourceId, Payment = 5, StationId = whetstone });
            _h.Send(owner, new ViewUpgradeOrdersCommand());
            int mine = LastReply<UpgradeOrdersMessage>(owner).Orders.Single(o => o.IsMine).Id;
            _h.Send(owner, new CancelUpgradeOrderCommand { OrderId = mine });
            Assert.That(Gold(owner), Is.EqualTo(beforeCancel));
            Assert.That(BagItem(owner, "great_axe").SourceId, Is.EqualTo(axe.SourceId));
        }

        [Test]
        public void Codec_RoundTripsMarketMessages()
        {
            var page = (MarketPageMessage)ProtocolCodec.Decode(ProtocolCodec.Encode(new MarketPageMessage
            {
                Total = 1, Page = 0, PageCount = 1,
                Lots = new[] { new LotSnapshot { LotId = 7, ShopName = "Лавка", Item = new ItemSlotSnapshot { DefinitionId = "x", Count = 2 }, PricePerUnit = 9, PriceVersion = 3 } },
            }));
            Assert.That((page.Lots[0].LotId, page.Lots[0].ShopName, page.Lots[0].Item.Count, page.Lots[0].PriceVersion), Is.EqualTo((7L, "Лавка", 2, 3)));
            var search = (MarketSearchCommand)ProtocolCodec.Decode(ProtocolCodec.Encode(new MarketSearchCommand { DefinitionIds = new[] { "a", "b" }, Rarity = 2, Sort = MarketSort.Newest, Page = 4 }));
            Assert.That((search.DefinitionIds.Length, search.Rarity, search.Sort, search.Page), Is.EqualTo((2, 2, MarketSort.Newest, 4)));
            var mail = (MailboxMessage)ProtocolCodec.Decode(ProtocolCodec.Encode(new MailboxMessage
            {
                Mail = new[] { new MailSnapshot { Id = 5, Type = MailType.Sale, Gold = 13, SecondsLeft = -1, Items = new ItemSlotSnapshot[0] } },
            }));
            Assert.That((mail.Mail[0].Id, mail.Mail[0].Type, mail.Mail[0].Gold), Is.EqualTo((5L, MailType.Sale, 13L)));
        }
    }
}
