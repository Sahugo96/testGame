using System;
using System.Collections.Generic;
using Game.Server.Effects;
using Game.Server.Items;
using Game.Server.Players;
using Game.Server.Systems;
using Game.Server.World;
using Game.Shared.Content;
using Game.Shared.Content.Dto;
using Game.Shared.Core;
using Game.Shared.Protocol;
using Game.Shared.Rules;

namespace Game.Server
{
    /// <summary>
    /// Authoritative simulation of one zone (principles 1, 9): fixed tick, commands in, snapshots out.
    /// Pure C# without Unity or Mirror; the transport (local now, Mirror in module 1) only moves messages.
    /// </summary>
    public sealed class GameServer
    {
        private readonly ServerContext _ctx;
        private readonly ResourceSystem _resources;
        private readonly CombatSystem _combat;
        private readonly SkillSystem _skills;
        private readonly MovementSystem _movement;
        private readonly TargetingSystem _targeting;
        private readonly InventorySystem _inventory;
        private readonly StatSystem _stats;
        private readonly RespawnSystem _respawn;
        private readonly SnapshotBuilder _snapshots;
        private readonly ProgressionSystem _progression;
        private readonly UpgradeSystem _upgrades;
        private readonly WalletSystem _wallet;
        private readonly SocketSystem _sockets;
        private readonly ChatSystem _chat;
        private readonly FriendSystem _friends;
        private readonly TradeSystem _trade;
        private readonly MarketState _market = new MarketState();
        private readonly StationSystem _stations;
        private readonly MailSystem _mail;
        private readonly ShopSystem _shops;
        private readonly UpgradeOrderSystem _orders;
        private readonly SettingsSystem _settings;
        private readonly PanelSystem _panels;
        private float _marketCheckTimer;
        /// <summary>Shop, mail and order expiry is checked this often (real-time deadlines of hours and days).</summary>
        private const float MarketCheckSeconds = 1f;
        private readonly DuelSystem _duels;
        private readonly GroupSystem _groups;
        private readonly LootSystem _loot;
        private readonly CraftingSystem _crafting;
        private readonly StatusSystem _statuses;
        private readonly ThreatSystem _threat;
        private readonly MobAiSystem _mobAi;
        private readonly InterestSystem _interest;
        private readonly System.Diagnostics.Stopwatch _tickClock = new System.Diagnostics.Stopwatch();
        private int _tick;
        private int _nextCharacterNumber = 1;

        public GameServer(ContentDatabase content, IRandom random, IAuditLog audit)
        {
            _ctx = new ServerContext
            {
                Content = content,
                World = new ServerWorld(),
                Random = random,
                Audit = audit,
                Items = new ItemFactory(content, audit),
            };
            _ctx.Items.Random = random;
            _resources = new ResourceSystem(_ctx);
            _duels = new DuelSystem(_ctx, _resources);
            _statuses = new StatusSystem(_ctx);
            _combat = new CombatSystem(_ctx, _resources, _duels, _statuses);
            _statuses.BindCombat(_combat);
            var effects = new EffectHandlerRegistry();
            effects.Register(new DamageEffectHandler(_combat));
            effects.Register(new HealEffectHandler(_combat));
            effects.Register(new RestoreResourceEffectHandler(_resources));
            effects.Register(new ApplyStatusEffectHandler(_statuses));
            effects.Register(new DispelEffectHandler(_statuses));
            _skills = new SkillSystem(_ctx, _resources, _combat, effects, _statuses);
            _movement = new MovementSystem(_ctx, _skills, _statuses);
            _targeting = new TargetingSystem(_ctx, _combat);
            _stats = new StatSystem(_ctx) { ExtraModifiers = _statuses.ModifiersOf };
            _inventory = new InventorySystem(_ctx, _stats, _resources, _statuses);
            _threat = new ThreatSystem(_ctx);
            _mobAi = new MobAiSystem(_ctx, _combat, _statuses, _threat, effects);
            _respawn = new RespawnSystem(_ctx);
            _snapshots = new SnapshotBuilder(_ctx, _skills);
            _progression = new ProgressionSystem(_ctx, _stats);
            _wallet = new WalletSystem(_ctx);
            _sockets = new SocketSystem(_ctx, _inventory, _stats);
            _upgrades = new UpgradeSystem(_ctx, _inventory, _stats, _wallet);
            _groups = new GroupSystem(_ctx);
            _chat = new ChatSystem(_ctx, _groups);
            _friends = new FriendSystem(_ctx, _chat);
            _trade = new TradeSystem(_ctx, _inventory, _wallet, _resources);
            _stations = new StationSystem(_ctx);
            _mail = new MailSystem(_ctx, _market, _wallet, _inventory, _stations, _chat, _friends);
            _shops = new ShopSystem(_ctx, _market, _wallet, _inventory, _stations, _mail);
            _orders = new UpgradeOrderSystem(_ctx, _market, _wallet, _inventory, _upgrades, _mail, _friends);
            _settings = new SettingsSystem(_ctx);
            _panels = new PanelSystem(_ctx, _inventory);
            _settings.Changed += player => AccountSettingsChanged?.Invoke(player.EntityId);
            _groups.SystemMessage += (groupId, key) => _chat.SystemToGroup(groupId, key, null);
            _friends.Changed += player => AccountSettingsChanged?.Invoke(player.EntityId);
            _chat.TabsChanged += player => AccountSettingsChanged?.Invoke(player.EntityId);
            _loot = new LootSystem(_ctx, _groups, _inventory);
            _crafting = new CraftingSystem(_ctx, _inventory, _resources);
            _interest = new InterestSystem(_ctx, _groups, _duels);
            Metrics = new ServerMetrics(content.Config.MetricsWindowTicks);
            _combat.Damaged += _loot.OnDamaged;
            _combat.Healed += _loot.OnHealed;
            _combat.Damaged += _threat.OnDamaged;
            _combat.Healed += _threat.OnHealed;
            _combat.Died += (victim, killer) => _threat.OnDied(victim);
            _statuses.Changed += OnStatusesChanged;
            _statuses.Controlled += _skills.CancelCast;
            _combat.Died += OnKilled;
            _duels.Ended += OnDuelEnded;
            SpawnMobs();
            SpawnStations();
        }

        public ServerContext Context => _ctx;

        public float TickInterval => 1f / _ctx.Content.Config.TickRate;

        public int CurrentTick => _tick;

        /// <summary>An account setting of the player changed (auto-pickup): persistence saves the account.</summary>
        public event Action<EntityId> AccountSettingsChanged;

        /// <summary>
        /// Items or gold moved between owners this tick — shops, mail, orders (module 20) or a trade: persistence saves the
        /// touched characters and, when it changed, the market as one atomic commit (FileStorage.Commit), so a crash or a
        /// stop can neither duplicate nor lose an item. Arguments: the players, whether the market changed.
        /// </summary>
        public event Action<IReadOnlyList<EntityId>, bool> Committed;

        /// <summary>Market data for persistence (load once at start, export on Committed).</summary>
        public MarketState Market => _market;

        /// <summary>After persistence loaded the market: merchants of open shops appear on their spots.</summary>
        public void MarketLoaded() => _shops.SpawnMerchants();

        /// <summary>Raised after every tick when snapshots are ready (transports send them).</summary>
        public event Action Ticked;

        public bool HasClass(string classId) => classId != null && _ctx.Content.Classes.ContainsKey(classId);

        public EntityId AddPlayer(string classId) => AddPlayer(classId, null);

        public EntityId AddPlayer(string classId, string displayName) => AddPlayer(classId, displayName, null);

        /// <summary>A new character with the class's starting attributes, skills and items.</summary>
        public EntityId AddPlayer(string classId, string displayName, string characterId)
        {
            PlayerCharacter player = CreatePlayer(classId, displayName, characterId);
            foreach (var pair in player.Class.StartingAttributes)
            {
                player.Attributes[pair.Key] = pair.Value;
            }
            foreach (ClassSkill skill in player.Class.Skills)
            {
                player.Mastery[skill.SkillId] = skill.StartingMastery;
            }
            _stats.Recalculate(player);
            _inventory.GiveStartingItems(player);
            _panels.InitDefaults(player);
            _wallet.Credit(player, _ctx.Content.Config.GoldCurrencyId, _ctx.Content.Config.StartingGold, null, "start");
            FillResources(player.EntityId);
            Log.Info(LogTags.Server, $"Player {player.EntityId} joined as '{classId}'");
            return player.EntityId;
        }

        /// <summary>A saved character comes back (module 13): progress, items with their GUIDs, resources, position, statuses.</summary>
        public EntityId AddPlayer(Persistence.CharacterRecord record)
        {
            PlayerCharacter player = CreatePlayer(record.ClassId, record.DisplayName, record.CharacterId);
            player.Level = Math.Max(_ctx.Content.Config.StartingLevel, record.Level);
            player.Experience = record.Experience;
            player.AttributePoints = record.AttributePoints;
            player.ClassPoints = record.ClassPoints;
            foreach (Persistence.StatValueRecord attribute in record.Attributes ?? new Persistence.StatValueRecord[0])
            {
                if (Enum.TryParse(attribute.Stat, out StatId stat))
                {
                    player.Attributes[stat] = attribute.Value;
                }
            }
            foreach (ClassSkill skill in player.Class.Skills)
            {
                player.Mastery[skill.SkillId] = skill.StartingMastery;
            }
            foreach (Persistence.SkillValueRecord mastery in record.Mastery ?? new Persistence.SkillValueRecord[0])
            {
                player.Mastery[mastery.SkillId] = mastery.Value;
            }
            foreach (Persistence.ItemRecord item in record.Bag ?? new Persistence.ItemRecord[0])
            {
                ItemInstance instance = RestoreItem(item);
                if (instance != null && item.BagIndex >= 0 && item.BagIndex < player.Bag.Size && player.Bag.Get(item.BagIndex) == null)
                {
                    player.Bag.Set(item.BagIndex, instance);
                }
            }
            foreach (Persistence.ItemRecord item in record.Equipment ?? new Persistence.ItemRecord[0])
            {
                ItemInstance instance = RestoreItem(item);
                if (instance != null && !string.IsNullOrEmpty(item.SlotId))
                {
                    player.Equipment[item.SlotId] = instance;
                }
            }
            _stats.Recalculate(player);
            ServerWorld world = _ctx.World;
            ref VitalsData vitals = ref world.Vitals.Get(player.EntityId);
            vitals.Health = record.Health > 0f ? Math.Min(record.Health, vitals.MaxHealth) : vitals.MaxHealth;
            ref ManaData mana = ref world.Mana.Get(player.EntityId);
            mana.Main = Math.Min(record.MainMana, mana.MaxMain);
            mana.Reserve = Math.Min(record.ReserveMana, mana.MaxReserve);
            ResourceSystem.UpdateLock(ref mana);
            float half = _ctx.Content.Config.WorldHalfSize;
            world.Transforms.Get(player.EntityId).Position = new Vec3(Math.Max(-half, Math.Min(half, record.PositionX)), 0f, Math.Max(-half, Math.Min(half, record.PositionZ)));
            foreach (Persistence.CurrencyRecord currency in record.Currencies ?? new Persistence.CurrencyRecord[0])
            {
                if (_ctx.Content.Currencies.ContainsKey(currency.Id ?? string.Empty) && currency.Amount > 0)
                {
                    player.Wallet[currency.Id] = currency.Amount;
                }
            }
            foreach (Persistence.StatusRecord status in record.Statuses ?? new Persistence.StatusRecord[0])
            {
                _statuses.Restore(player.EntityId, status.StatusId, status.Stacks, status.Remaining, status.Power, status.ShieldLeft);
            }
            _panels.Load(player, record.SkillBar, record.ConsumableBar, ImportLoadouts(record.Loadouts));
            Log.Info(LogTags.Server, $"Player {player.EntityId} restored: '{record.DisplayName}' ({record.ClassId}, level {player.Level})");
            return player.EntityId;
        }

        /// <summary>Everything that survives a restart (module 13). Account login and timestamps are set by the caller.</summary>
        public Persistence.CharacterRecord ExportPlayer(EntityId id)
        {
            PlayerCharacter player = _ctx.GetPlayer(id);
            if (player == null)
            {
                return null;
            }
            ServerWorld world = _ctx.World;
            var attributes = new List<Persistence.StatValueRecord>();
            foreach (var pair in player.Attributes)
            {
                attributes.Add(new Persistence.StatValueRecord { Stat = pair.Key.ToString(), Value = pair.Value });
            }
            var mastery = new List<Persistence.SkillValueRecord>();
            foreach (var pair in player.Mastery)
            {
                mastery.Add(new Persistence.SkillValueRecord { SkillId = pair.Key, Value = pair.Value });
            }
            var bag = new List<Persistence.ItemRecord>();
            for (int i = 0; i < player.Bag.Size; i++)
            {
                ItemInstance item = player.Bag.Get(i);
                if (item != null)
                {
                    bag.Add(ExportItem(item, i, null));
                }
            }
            var equipment = new List<Persistence.ItemRecord>();
            foreach (var pair in player.Equipment)
            {
                equipment.Add(ExportItem(pair.Value, -1, pair.Key));
            }
            VitalsData vitals = world.Vitals.Get(id);
            ManaData mana = world.Mana.Get(id);
            Vec3 position = vitals.IsDead ? _respawn.PlayerSpawn() : world.Transforms.Get(id).Position;
            return new Persistence.CharacterRecord
            {
                SchemaVersion = Persistence.CharacterMigrations.Current,
                ClassId = player.Class.Id,
                DisplayName = world.Identities.Get(id).DisplayName,
                CharacterId = player.CharacterId,
                Level = player.Level,
                Experience = player.Experience,
                AttributePoints = player.AttributePoints,
                ClassPoints = player.ClassPoints,
                Attributes = attributes.ToArray(),
                Mastery = mastery.ToArray(),
                Bag = bag.ToArray(),
                Equipment = equipment.ToArray(),
                Health = vitals.IsDead ? vitals.MaxHealth : vitals.Health,
                MainMana = mana.Main,
                ReserveMana = mana.Reserve,
                PositionX = position.X,
                PositionZ = position.Z,
                Statuses = _statuses.Export(id),
                Currencies = ExportWallet(player),
                SkillBar = Array.ConvertAll(player.Panels.SkillBar, s => s ?? string.Empty),
                ConsumableBar = Array.ConvertAll(player.Panels.ConsumableBar, s => s ?? string.Empty),
                Loadouts = ExportLoadouts(player),
            };
        }

        private static Persistence.LoadoutRecord[] ExportLoadouts(PlayerCharacter player) =>
            Array.ConvertAll(player.Panels.Loadouts, l => new Persistence.LoadoutRecord
            {
                Items = l.Items.ConvertAll(i => new Persistence.LoadoutItemRecord { SlotId = i.SlotId, InstanceId = i.InstanceId, DefinitionId = i.DefinitionId }).ToArray(),
            });

        private static LoadoutData[] ImportLoadouts(Persistence.LoadoutRecord[] records)
        {
            if (records == null)
            {
                return null;
            }
            var result = new LoadoutData[records.Length];
            for (int i = 0; i < records.Length; i++)
            {
                result[i] = new LoadoutData();
                foreach (Persistence.LoadoutItemRecord item in records[i]?.Items ?? new Persistence.LoadoutItemRecord[0])
                {
                    if (!string.IsNullOrEmpty(item?.SlotId) && !string.IsNullOrEmpty(item.InstanceId))
                    {
                        result[i].Items.Add(new LoadoutItemData { SlotId = item.SlotId, InstanceId = item.InstanceId, DefinitionId = item.DefinitionId });
                    }
                }
            }
            return result;
        }

        private PlayerCharacter CreatePlayer(string classId, string displayName, string characterId)
        {
            if (!_ctx.Content.Classes.TryGetValue(classId ?? string.Empty, out ClassDefinition cls))
            {
                throw new ArgumentException($"Unknown class '{classId}'");
            }
            ServerWorld world = _ctx.World;
            EntityId id = world.CreateEntity();
            GameConfig cfg = _ctx.Content.Config;
            world.Identities.Add(id, new IdentityData { Kind = EntityKind.Player, DefinitionId = cls.Id, NameKey = cls.NameKey, DisplayName = displayName, Faction = cfg.PlayerFaction });
            world.Transforms.Add(id, new TransformData { Position = _respawn.PlayerSpawn() });
            world.Vitals.Add(id, new VitalsData());
            world.Mana.Add(id, new ManaData());
            world.CombatStates.Add(id, new CombatStateData());
            world.Casts.Add(id, new CastData());
            var player = new PlayerCharacter
            {
                EntityId = id,
                CharacterId = characterId ?? $"character-{_nextCharacterNumber++}",
                Class = cls,
                Level = cfg.StartingLevel,
                Bag = new Inventory(_ctx.Content.BagSize),
            };
            _ctx.Players.Add(id, player);
            return player;
        }

        private ItemInstance RestoreItem(Persistence.ItemRecord record) => Persistence.ItemRecords.Restore(_ctx.Content, record, _ctx.Time);

        private static Persistence.CurrencyRecord[] ExportWallet(PlayerCharacter player)
        {
            var result = new List<Persistence.CurrencyRecord>();
            foreach (KeyValuePair<string, long> balance in player.Wallet)
            {
                result.Add(new Persistence.CurrencyRecord { Id = balance.Key, Amount = balance.Value });
            }
            return result.ToArray();
        }

        private static Persistence.ItemRecord ExportItem(ItemInstance item, int bagIndex, string slotId) => Persistence.ItemRecords.Export(item, bagIndex, slotId);

        /// <summary>Removes a player and its entity (disconnect or character switch).</summary>
        public void RemovePlayer(EntityId id)
        {
            if (!_ctx.Players.ContainsKey(id))
            {
                return;
            }
            _duels.OnPlayerRemoved(id);
            _statuses.OnEntityRemoved(id);
            _threat.OnDied(id);
            _groups.OnPlayerRemoved(id);
            _trade.OnPlayerGone(id);
            _chat.OnPlayerRemoved(id);
            _shops.OnPlayerRemoved(id);
            _ctx.Players.Remove(id);
            foreach (PlayerCharacter other in _ctx.Players.Values)
            {
                if (other.TargetId == id)
                {
                    other.TargetId = EntityId.None;
                }
            }
            _ctx.World.DestroyEntity(id);
            Log.Info(LogTags.Server, $"Player {id} left");
        }

        public PlayerCharacter GetPlayer(EntityId id) => _ctx.GetPlayer(id);

        public void Submit(EntityId playerId, IGameCommand command)
        {
            PlayerCharacter player = _ctx.GetPlayer(playerId);
            player?.PendingCommands.Enqueue(command);
        }

        /// <summary>Tick time, players, entities and interest numbers (module 3).</summary>
        public ServerMetrics Metrics { get; }

        public void Tick()
        {
            _tickClock.Restart();
            float dt = TickInterval;
            _ctx.Time += dt;
            _tick++;
            foreach (PlayerCharacter player in _ctx.Players.Values)
            {
                while (player.PendingCommands.Count > 0)
                {
                    Handle(player, player.PendingCommands.Dequeue());
                }
            }
            _movement.Update(dt);
            _mobAi.Update(dt);
            _skills.Update(dt);
            _statuses.Update(dt);
            _inventory.Update(dt);
            _resources.Update(dt);
            _respawn.Update(dt);
            _duels.Update();
            _crafting.Update(dt);
            _loot.Update();
            _trade.Update();
            _groups.Update();
            _marketCheckTimer += dt;
            if (_marketCheckTimer >= MarketCheckSeconds)
            {
                _marketCheckTimer = 0f;
                _shops.Update();
                _mail.Update();
                _orders.Update();
            }

            WorldSnapshot worldSnapshot = _snapshots.BuildWorld(_tick);
            for (int i = 0; i < worldSnapshot.Entities.Length; i++)
            {
                EntityId id = worldSnapshot.Entities[i].Id;
                worldSnapshot.Entities[i].Statuses = _statuses.Snapshot(id);
                PlayerCharacter owner = _ctx.GetPlayer(id);
                worldSnapshot.Entities[i].TargetOfId = owner != null ? owner.TargetId : _threat.TopThreat(id);
            }
            _interest.Rebuild(worldSnapshot);
            foreach (PlayerCharacter player in _ctx.Players.Values)
            {
                player.Outbox.World = _interest.BuildFor(player, worldSnapshot);
                player.Outbox.State = _snapshots.BuildPlayer(player);
                player.Outbox.State.Duel = _duels.Snapshot(player.EntityId);
                player.Outbox.State.CraftRecipeId = player.Craft.Active ? player.Craft.RecipeId : null;
                player.Outbox.State.AutoLoot = player.AutoLoot;
                player.Outbox.State.CraftProgress = _crafting.Progress(player);
                player.Outbox.State.Group = _groups.Snapshot(player.EntityId);
                player.Outbox.State.Friends = _friends.Snapshot(player);
                player.Outbox.State.ChatTabs = _chat.Tabs(player);
                player.Outbox.State.Trade = _trade.Snapshot(player.EntityId);
                var market = new MarketStateSnapshot
                {
                    MyMerchantId = _shops.MerchantOf(player),
                    MarketVersion = _market.MarketVersion,
                    OrdersVersion = _market.OrdersVersion,
                };
                _mail.Fill(player, market);
                player.Outbox.State.Market = market;
                _settings.Flush(player);
                _panels.Flush(player);
            }
            if (_market.Dirty || _ctx.Committed.Count > 0)
            {
                var touched = new List<EntityId>(_market.Touched);
                foreach (EntityId id in _ctx.Committed)
                {
                    if (!_market.Touched.Contains(id))
                    {
                        touched.Add(id);
                    }
                }
                bool marketChanged = _market.Dirty;
                _market.Dirty = false;
                _market.Touched.Clear();
                _ctx.Committed.Clear();
                Committed?.Invoke(touched, marketChanged);
            }
            _tickClock.Stop();
            Metrics.RecordTick(_tickClock.Elapsed.TotalMilliseconds, _ctx.Players.Count, worldSnapshot.Entities.Length, _interest.LastTickEntitiesSent);
            Ticked?.Invoke();
        }

        private void Handle(PlayerCharacter player, IGameCommand command)
        {
            switch (command)
            {
                case MoveCommand move:
                    // A forged NaN or infinity would put the player (and its save) at NaN: such a direction is "stand still".
                    player.LastMove = IsFinite(move.DirX) && IsFinite(move.DirZ) ? move : default;
                    break;
                case UseSkillCommand useSkill:
                    _skills.TryUse(player, useSkill);
                    break;
                case SelectTargetCommand select:
                    _targeting.Select(player, select.TargetId);
                    break;
                case CycleTargetCommand _:
                    _targeting.Cycle(player);
                    break;
                case EquipItemCommand equip:
                    _inventory.Equip(player, equip);
                    break;
                case UnequipItemCommand unequip:
                    _inventory.Unequip(player, unequip);
                    break;
                case UseConsumableCommand consume:
                    _inventory.UseConsumable(player, consume);
                    break;
                case MoveBagItemCommand move:
                    _inventory.MoveBagItem(player, move);
                    break;
                case UpgradeItemCommand upgrade:
                    _upgrades.TryUpgrade(player, upgrade);
                    break;
                case CraftItemCommand craft:
                    _crafting.Start(player, craft);
                    break;
                case CancelCraftCommand _:
                    _crafting.Cancel(player, true);
                    break;
                case SendChatCommand chat:
                    _chat.Send(player, chat);
                    break;
                case FriendAddCommand friendAdd:
                    _friends.Request(player, friendAdd.Name);
                    break;
                case FriendRespondCommand friendRespond:
                    _friends.Respond(player, friendRespond.Name, friendRespond.Accept);
                    break;
                case SetChatTabsCommand chatTabs:
                    _chat.SetTabs(player, chatTabs.Tabs);
                    break;
                case OpenShopCommand openShop:
                    _shops.Open(player, openShop);
                    break;
                case CloseShopCommand _:
                    _shops.Close(player);
                    break;
                case ListLotCommand listLot:
                    _shops.List(player, listLot);
                    break;
                case UnlistLotCommand unlistLot:
                    _shops.Unlist(player, unlistLot);
                    break;
                case SetLotPriceCommand setPrice:
                    _shops.SetPrice(player, setPrice);
                    break;
                case BuyLotCommand buy:
                    _shops.Buy(player, buy);
                    break;
                case ViewShopCommand viewShop:
                    _shops.View(player, viewShop);
                    break;
                case MarketSearchCommand search:
                    _shops.Search(player, search);
                    break;
                case ViewMailboxCommand _:
                    _mail.View(player);
                    break;
                case SendMailCommand sendMail:
                    _mail.Send(player, sendMail);
                    break;
                case ClaimMailCommand claim:
                    _mail.Claim(player, claim);
                    break;
                case ReadMailCommand read:
                    _mail.Read(player, read);
                    break;
                case DeleteMailCommand deleteMail:
                    _mail.Delete(player, deleteMail);
                    break;
                case CreateUpgradeOrderCommand createOrder:
                    _orders.Create(player, createOrder);
                    break;
                case CancelUpgradeOrderCommand cancelOrder:
                    _orders.Cancel(player, cancelOrder);
                    break;
                case FulfillUpgradeOrderCommand fulfill:
                    _orders.Fulfill(player, fulfill);
                    break;
                case ViewUpgradeOrdersCommand _:
                    _orders.View(player);
                    break;
                case SetAccountSettingsCommand settings:
                    _settings.Set(player, settings);
                    break;
                case SetSkillSlotCommand skillSlot:
                    _panels.SetSkillSlot(player, skillSlot);
                    break;
                case SetConsumableSlotCommand consumableSlot:
                    _panels.SetConsumableSlot(player, consumableSlot);
                    break;
                case SaveLoadoutCommand saveLoadout:
                    _panels.SaveLoadout(player, saveLoadout);
                    break;
                case ApplyLoadoutCommand applyLoadout:
                    _panels.ApplyLoadout(player, applyLoadout);
                    break;
                case FriendRemoveCommand friendRemove:
                    _friends.Remove(player, friendRemove.Name);
                    break;
                case GroupConvertToRaidCommand _:
                    _groups.ConvertToRaid(player);
                    break;
                case GroupMoveCommand move:
                    _groups.MoveToSubgroup(player, move.MemberId, move.Subgroup);
                    break;
                case GroupSetAssistantCommand assistant:
                    _groups.SetAssistant(player, assistant.MemberId, assistant.Enabled);
                    break;
                case TradeRequestCommand tradeRequest:
                    _trade.Request(player, tradeRequest.TargetId);
                    break;
                case TradeRespondCommand tradeRespond:
                    _trade.Respond(player, tradeRespond.Accept);
                    break;
                case TradeOfferCommand tradeOffer:
                    _trade.SetOffer(player, tradeOffer);
                    break;
                case TradeConfirmCommand _:
                    _trade.Confirm(player);
                    break;
                case TradeCancelCommand _:
                    _trade.Cancel(player);
                    break;
                case InsertIngredientCommand insert:
                    _sockets.Insert(player, insert);
                    break;
                case PickUpLootCommand pickUp:
                    _loot.PickUp(player, pickUp.LootId);
                    break;
                case SetAutoLootCommand autoLoot:
                    player.AutoLoot = autoLoot.Enabled;
                    AccountSettingsChanged?.Invoke(player.EntityId);
                    break;
                case SpendAttributePointCommand spend:
                    _progression.SpendAttributePoint(player, spend);
                    break;
                case DuelRequestCommand duelRequest:
                    _duels.Request(player, duelRequest.TargetId);
                    break;
                case DuelResponseCommand duelResponse:
                    _duels.Respond(player, duelResponse.Accept);
                    break;
                case DuelSurrenderCommand _:
                    _duels.Surrender(player);
                    break;
                case GroupInviteCommand invite:
                    _groups.Invite(player, invite.TargetId);
                    break;
                case GroupInviteResponseCommand inviteResponse:
                    _groups.RespondInvite(player, inviteResponse.Accept);
                    break;
                case GroupLeaveCommand _:
                    _groups.Leave(player);
                    break;
                case GroupKickCommand kick:
                    _groups.Kick(player, kick.MemberId);
                    break;
                case GroupPromoteCommand promote:
                    _groups.Promote(player, promote.MemberId);
                    break;
                case LootModeProposeCommand propose:
                    _groups.Propose(player, propose.Mode);
                    break;
                case LootModeVoteCommand vote:
                    _groups.Vote(player, vote.ProposalId, vote.Accept);
                    break;
                default:
                    Log.Warning(LogTags.Server, $"Unknown command {command.GetType().Name}");
                    break;
            }
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        /// <summary>
        /// Gold from a kill (05, 7.14.2): a roll between GoldMin and GoldMax, split equally between those who share the
        /// experience (temporarily, DEFAULT, question 78); the remainder goes to the killer (the first recipient).
        /// </summary>
        private void AwardGold(MobDto mob, List<PlayerCharacter> recipients, EntityId victim)
        {
            if (mob.GoldMax <= 0)
            {
                return;
            }
            long total = mob.GoldMin + (long)(_ctx.Random.NextFloat() * (mob.GoldMax - mob.GoldMin + 1));
            total = Math.Min(total, mob.GoldMax);
            long part = total / recipients.Count;
            string gold = _ctx.Content.Config.GoldCurrencyId;
            for (int i = 0; i < recipients.Count; i++)
            {
                long amount = part + (i == 0 ? total - part * recipients.Count : 0);
                if (amount > 0 && _wallet.Credit(recipients[i], gold, amount, $"kill:{victim.Value}:{_tick}:{recipients[i].CharacterId}", "kill"))
                {
                    recipients[i].Outbox.Notifications.Add(new NotificationMessage { TextKey = NotificationKeys.GoldGained, Number = (int)amount, IsPositive = true });
                }
            }
        }

        /// <summary>Account settings from the record (persistence, on login); a broken or newer document falls back to the defaults.</summary>
        public void LoadAccountSettings(EntityId id, Persistence.AccountSettingsRecord record)
        {
            PlayerCharacter player = GetPlayer(id);
            if (player == null)
            {
                return;
            }
            // JsonUtility has no null object: an account that never changed its settings comes back with SchemaVersion 0.
            if (record == null || record.SchemaVersion <= 0)
            {
                _settings.Load(player, null);
                return;
            }
            var bindings = new List<KeyBindingSnapshot>();
            foreach (Persistence.KeyBindingRecord binding in record.Bindings ?? new Persistence.KeyBindingRecord[0])
            {
                bindings.Add(new KeyBindingSnapshot { BindingId = binding?.BindingId, Path = binding?.Path });
            }
            var saved = new AccountSettingsSnapshot
            {
                SchemaVersion = record.SchemaVersion, Bindings = bindings.ToArray(), CameraSensitivity = record.CameraSensitivity, InvertY = record.InvertY,
                ZoomSpeed = record.ZoomSpeed, UiScale = record.UiScale, PanelOpacity = record.PanelOpacity, DamageNumbers = record.DamageNumbers,
                Nameplates = record.Nameplates, NameplateHealth = record.NameplateHealth,
            };
            if (_settings.Clean(saved) == null)
            {
                Log.Warning(LogTags.Server, $"Account settings of {player.CharacterId} (schema {record.SchemaVersion}) not accepted: defaults used");
                saved = null;
            }
            _settings.Load(player, saved);
        }

        /// <summary>Account settings for the record; null while the player keeps the defaults.</summary>
        public Persistence.AccountSettingsRecord ExportAccountSettings(EntityId id)
        {
            AccountSettingsSnapshot s = GetPlayer(id)?.AccountSettings;
            if (s == null)
            {
                return null;
            }
            var bindings = new Persistence.KeyBindingRecord[s.Bindings.Length];
            for (int i = 0; i < bindings.Length; i++)
            {
                bindings[i] = new Persistence.KeyBindingRecord { BindingId = s.Bindings[i].BindingId, Path = s.Bindings[i].Path };
            }
            return new Persistence.AccountSettingsRecord
            {
                SchemaVersion = s.SchemaVersion, Bindings = bindings, CameraSensitivity = s.CameraSensitivity, InvertY = s.InvertY, ZoomSpeed = s.ZoomSpeed,
                UiScale = s.UiScale, PanelOpacity = s.PanelOpacity, DamageNumbers = s.DamageNumbers, Nameplates = s.Nameplates, NameplateHealth = s.NameplateHealth,
            };
        }

        /// <summary>Chat tabs from the account record (persistence, on login); broken or missing = the defaults.</summary>
        public void LoadChatTabs(EntityId id, Persistence.ChatTabRecord[] records)
        {
            PlayerCharacter player = GetPlayer(id);
            if (player == null || records == null || records.Length == 0)
            {
                return;
            }
            var tabs = new ChatTabSnapshot[records.Length];
            for (int i = 0; i < records.Length; i++)
            {
                var channels = new List<ChatChannel>();
                foreach (string name in records[i]?.Channels ?? new string[0])
                {
                    if (Enum.TryParse(name, out ChatChannel channel))
                    {
                        channels.Add(channel);
                    }
                }
                tabs[i] = new ChatTabSnapshot { Name = records[i]?.Name, NameKey = records[i]?.NameKey, Channels = channels.ToArray() };
            }
            player.ChatTabs = _chat.CleanTabs(tabs);
        }

        /// <summary>Chat tabs for the account record; null while the player keeps the defaults.</summary>
        public Persistence.ChatTabRecord[] ExportChatTabs(EntityId id)
        {
            ChatTabSnapshot[] tabs = GetPlayer(id)?.ChatTabs;
            if (tabs == null)
            {
                return null;
            }
            var records = new Persistence.ChatTabRecord[tabs.Length];
            for (int i = 0; i < tabs.Length; i++)
            {
                var channels = new string[tabs[i].Channels.Length];
                for (int k = 0; k < channels.Length; k++)
                {
                    channels[k] = tabs[i].Channels[k].ToString();
                }
                records[i] = new Persistence.ChatTabRecord { Name = tabs[i].Name, NameKey = tabs[i].NameKey, Channels = channels };
            }
            return records;
        }

        /// <summary>Friends (module 14): persistence sets its Store so offline accounts can be asked.</summary>
        public FriendSystem Friends => _friends;

        /// <summary>Wallets (05, 7.14.2) for trade, tests and tools.</summary>
        public WalletSystem Wallet => _wallet;

        /// <summary>Statuses changed what the entity's stats are (03, 7.12).</summary>
        private void OnStatusesChanged(EntityId id)
        {
            PlayerCharacter player = _ctx.GetPlayer(id);
            if (player != null)
            {
                _stats.Recalculate(player);
            }
            else
            {
                _stats.RecalculateMob(id);
            }
        }

        /// <summary>Threat on mobs (module 8) for tests and the coming mob AI (module 10).</summary>
        public ThreatSystem Threat => _threat;

        /// <summary>Damage and healing (03, 7.12) for tests and tools.</summary>
        public CombatSystem Combat => _combat;

        /// <summary>Statuses (03, 7.12) for tests and tools.</summary>
        public StatusSystem Statuses => _statuses;

        /// <summary>After a duel nobody keeps hitting: casts aimed at the former opponent are cancelled.</summary>
        private void OnDuelEnded(EntityId a, EntityId b)
        {
            _skills.CancelCastAt(a, b);
            _skills.CancelCastAt(b, a);
        }

        private void SpawnMobs()
        {
            ServerWorld world = _ctx.World;
            foreach (SpawnDto spawn in _ctx.Content.World.Spawns ?? new SpawnDto[0])
            {
                MobDto mob = _ctx.Content.Mobs[spawn.MobId];
                EntityId id = world.CreateEntity();
                var position = new Vec3(spawn.X, 0f, spawn.Z);
                world.Identities.Add(id, new IdentityData { Kind = EntityKind.Mob, DefinitionId = mob.Id, NameKey = mob.NameKey, Faction = mob.Faction });
                world.Transforms.Add(id, new TransformData { Position = position });
                world.Vitals.Add(id, new VitalsData { Health = mob.MaxHealth, MaxHealth = mob.MaxHealth, HealthRegen = mob.HealthRegen, Defense = mob.Defense });
                world.CombatStates.Add(id, new CombatStateData());
                world.Mobs.Add(id, new MobData { SpawnPosition = position, RespawnSeconds = mob.RespawnSeconds });
                _ctx.Content.AiProfiles.TryGetValue(mob.AiProfile ?? string.Empty, out AiProfile profile);
                world.MobAi.Add(id, MobAiSystem.Create(profile));
            }
        }

        /// <summary>
        /// Kill rewards: experience to the killer (question 72); drops by the loot rules — solo to the killer, in a group
        /// by the group's loot mode among eligible members (06, 7.8; LootSystem).
        /// </summary>
        private void OnKilled(EntityId victim, EntityId killer)
        {
            if (!_ctx.World.Mobs.Has(victim))
            {
                return;
            }
            PlayerCharacter player = _ctx.GetPlayer(killer);
            if (player != null && victim != killer)
            {
                MobDto mob = _ctx.Content.Mobs[_ctx.World.Identities.Get(victim).DefinitionId];
                List<PlayerCharacter> recipients = _loot.ExperienceRecipients(player, victim);
                var levels = new int[recipients.Count];
                for (int i = 0; i < levels.Length; i++)
                {
                    levels[i] = recipients[i].Level;
                }
                int[] shares = GroupExperienceMath.Split(mob.ExperienceReward, levels, _ctx.Content.Config.GroupXpGapShares);
                for (int i = 0; i < recipients.Count; i++)
                {
                    if (shares[i] > 0)
                    {
                        _progression.AwardExperience(recipients[i], shares[i]);
                    }
                }
                AwardGold(mob, recipients, victim);
                foreach (MobDropDto drop in mob.Drops ?? new MobDropDto[0])
                {
                    if (_ctx.Random.NextFloat() < drop.Chance)
                    {
                        _loot.Give(player, victim, drop);
                    }
                }
            }
            _loot.OnMobLooted(victim);
        }

        private void SpawnStations()
        {
            ServerWorld world = _ctx.World;
            foreach (StationDto station in _ctx.Content.World.Stations ?? new StationDto[0])
            {
                EntityId id = world.CreateEntity();
                world.Identities.Add(id, new IdentityData { Kind = EntityKind.Npc, DefinitionId = station.Id, NameKey = station.NameKey, Faction = null });
                world.Transforms.Add(id, new TransformData { Position = new Vec3(station.X, 0f, station.Z) });
                world.Stations.Add(id, new StationData { StationId = station.Id, Type = station.Type });
            }
        }

        private void FillResources(EntityId id)
        {
            ref VitalsData vitals = ref _ctx.World.Vitals.Get(id);
            vitals.Health = vitals.MaxHealth;
            ref ManaData mana = ref _ctx.World.Mana.Get(id);
            mana.Main = mana.MaxMain;
            mana.Reserve = mana.MaxReserve;
            mana.MainLocked = false;
        }
    }
}
