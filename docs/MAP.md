# MAP — карта проекта для ИИ (навигация с минимальным расходом контекста)

> Вход: `CLAUDE.md`. Источник правды — документы набора; эта карта только указывает, **где** читать.
> Номера строк (`:NN`) — подсказка на момент составления, документы обновляются после каждого модуля.
> Если строка «уехала» — `grep -n "^## \|^\*\*" docs/<файл>` и искать по якорю (заголовку).
> Читать кусок: `sed -n 'A,Bp' docs/<файл>`. Не читать файл целиком без нужды (кроме случаев из §0).

## 0. Когда что читать целиком

Инструкция для разработчика по запуску проекта в Unity — `docs/HOW_TO_RUN.md` (установка с нуля — разделы 0–4, проверка в Unity по шагам — раздел 10); короткая пошаговая «как сыграть вдвоём» — `docs/PLAY_WITH_FRIEND.md`.


| Ситуация | Читать |
|----------|--------|
| Начало модуля (AI_INSTRUCTIONS §4) | `AI_INSTRUCTIONS.md`, `00_INDEX.md`, `01_DEV_PLAN.md` целиком + тематические файлы модуля (§3 ниже) |
| Точечный вопрос / правка | только нужный раздел по этой карте |
| Конец модуля | `01_DEV_PLAN.md` §8–§11 + `00_INDEX.md` «Журнал изменений» (обновить, выдать полные версии) |

Размеры (≈токены): AI_INSTRUCTIONS ~5k · 00_INDEX ~3k · 01_DEV_PLAN ~19k · 02 ~7k · 03 ~5k · 04 ~6.5k · 05 ~3.5k · 06 ~3k.

## 1. Документы и их разделы (с якорями)

### `docs/AI_INSTRUCTIONS.md` — процесс работы ИИ (v3)
§1 Роль :10 · §2 Язык и код :16 · §3 Формат ответа :23 (3.1 начало модуля :25, 3.2 выдача кода :34, 3.3 варианты :43, **3.4 Чего не делать :50**) · §4 Работа по модулям :62 · §5 Что ИИ не может :78 · §6 Неопределённость :88 · **§7 Чек-лист :96** · **§8 Приоритет источников :109** · §9 Как улучшать :122 · §10 Журнал :131

### `docs/00_INDEX.md` — индекс набора (версия набора 40)
Как пользоваться :6 · Файлы набора :14 · **Карта «тема → файл» :27** · Глоссарий (термины маны, мастерство, связь) :46 · Как обновлять :60 · Журнал изменений набора :69

### `docs/01_DEV_PLAN.md` — проект, архитектура, план (v31)
| § | Якорь | Строка |
|---|-------|--------|
| 1 | О проекте (жанр, масштаб 10/50/1000+, стиль) | :11 |
| 2 | Разработчик → ссылка на AI_INSTRUCTIONS | :21 |
| 3 | Технические решения (стек; Unity 6000.3.19f1, локализация-гибрид, сборки) | :28 |
| 4 | **Архитектурные принципы 1–12** | :51 |
| 5 | Структура `Assets/_Project/` (от `My project/`) + **сборки** | :68 |
| 6 | **Соглашения по коду 🔴🟠🟡🟢** (6.1 Игрок :135) | :99 |
| 7.10 | Расширяемость контента (обработчики, ID, пакеты, мастера, `LegendaryDefinition`) | :143 |
| 8 | **План модулей 0–21** (таблица :185, описания+критерии :210) | :181 |
| 9 | **Реестр модулей: вертикальный срез (этапы 1, 1.1, 1.2, 1.3 — сеть, 1.4 — дуэли, 1.5 — группы, 1.6 — крафт, 1.7 — модуль 8, 1.8 — модуль 10, 1.9 — модуль 3, 1.10 — модуль 13, 1.11 — лут на земле, 1.12 — модуль 11, 1.13 — модуль 14, 1.13а — решения по вопросу 79, 1.14 — модуль 20, 1.15 — модуль 17, 1.16 — ревизия перед Unity, 1.17 — модуль 4, 1.18 — модуль 18, 1.19 — ревизия лишней работы)** | :258 |
| 10 | Проблемы и долги (записи v9, v11–v30 — ограничения среза, сети, дуэлей, групп, крафта, модулей 8, 10, 3, 13, 11, 14, 17, 20, 4, 18, лута, остаток ревизии) | :295 |
| 11 | Журнал решений (последние — v31; замеры модуля 3 — v19, ревизии — v31) | :327 |
| 12 | Как обновлять → AI_INSTRUCTIONS §4 | :388 |
| 13 | **Заморожено :398 / Гибко :409 / Отложено + крючки (с модулями) :414** | :394 |
| — | Открытые вопросы 29, 39–44, 69, 70, 75, 76, 80–84 | :429 |
| — | Рекомендации 1,5–10,17–19 | :450 |

### `docs/02_OVERLAY_UI.md` — оверлей, HUD, настройки (v8)
- **7.13 Оверлей** :11 — идея :13, зоны экрана :15, меню механик :30, правила окон :35, `HudWindowDefinition` :46, каталог окон :63, горячие клавиши :81, реализация (компоненты) :83, **правила UI-кода :85**, персонализация :92
- **7.7 Панели** :96 — общее :98, А навыки :104, Б расходники :111, В комплекты экипировки и смена оружия в бою :118, Г эмодзи :126
- **7.6 Настройки** :134 — категории и где хранятся :136, правила (apply/rollback, rebinding, JSON `schemaVersion`) :150
- Вопросы 10–13, 27, 28, 56, 86 :160 · Рекомендации 13, 14 :173

### `docs/03_CLASSES_COMBAT.md` — ресурсы, навыки, классы, бой (v11)
Термины: пул A — **основная** мана, пул B — **резервная**, **боевая** мана — третий запас магов («Третье сердце»). Идентификатор `CombatMana` в `CostEntry` = пул A (вопрос 60).
- **7.1 Ресурсы и мана** :11 — пулы (A, B, боевая) :13, правило списания :24, `CostEntry` + база `PercentOfMax` :26, атомарная проверка (стоимость в HP может убить) :42, боевое состояние :48, **регенерация (HP и в бою), блокировка основного пула, расходники, боевая мана, перелив** :50, модификаторы :59
- **7.2 Навыки Shift/Ctrl** :63 — модификаторы ввода (последний нажатый) :65, `SkillDefinition`/`SkillVariant` :75, `CmdUseSkill` :93, правила (КД, GCD, баланс вариантов, **мастерство навыка**) :95
- **7.3 Классы** :106 — `ClassDefinition` :108, **опыт и уровни (очки характеристик и класса)** :121, черновик классов :123
- **7.12 Боевые характеристики** :135 — оборона до 100% / многоуровневый крит / порядок урона :137, **первичные характеристики** :144, `StatModifier` :146, статус-эффекты :155, **срез модуля 8** :175, зелья :177
- Вопросы 1–5, 9, 25, 26, 45, 46, 52–54, 59, 60, 63, 64, 67, 74 :186 · Рекомендации 2–4, 26, 27 :210

### `docs/04_ITEMS_EQUIPMENT.md` — предметы и экипировка (v13)
- **7.9 Предметы** :11 — `ItemDefinition`/`ItemInstance` :13, **рюкзак-отсеки** :40, **компоненты-свойства (реализация)** :42, редкость (6 уровней, истинные легендарки и реплики) :56, слоты/ингредиенты (рукотворные ядра, извлечение)/overcap :69, **срез модуля 11** :77, божественные (привязка при получении) + «Искра Гефеста» :79
- **7.9.1 Виды экипировки** :90 — слоты экипировки (кольца по классу) :94, типы брони :108, виды оружия :110, правила слотов (`SocketCap`) :119, модель данных :124
- **7.11 Реестр легендарных** (только божественные) :141 (`LegendaryRegistryEntry` :147)
- **7.14.1 Улучшение (`UpgradeLevel`; срез: 1 осколок, 100%→55%, до +10; неудача — минус уровень), «связь» божественных** :153 (`CmdUpgradeItem` :161)
- Вопросы 14–19, 22–24, 30–32, 36–38, 47, 48, 55, 57, 61, 65, 66, 78 :167 · Рекомендации 15, 16, 20 :195

### `docs/05_ECONOMY_PROFESSIONS.md` — профессии и экономика (v11)
- **7.4 Профессии** :11 — типы («ремесленник», объединение ядер) :13, модель данных :19, правила (сервер, шанс крафта, неудача уничтожает ингредиенты, кузнец) :27
- **7.14.2 Кошелёк** :39 (`CurrencyDefinition` со `Scope: Guild`, «Варх» :42–46, `Transact(debits, credits, idempotencyKey)` и переполнение `Cap` :47–48, **срез модуля 11** :50)
- **7.14.3 Торговля** :55 — личные магазины (торговец-NPC, срок аренды, 1 на игрока, комиссия 15/10/5%) :57, рынок :63, почта (`MailMessage`) :69, безопасность :81
- Вопросы 8, 33–35, 49, 50, 58, 62, 73, 85 :89 · Рекомендации 11, 21–25 :104

### `docs/06_GROUPS_DUNGEONS.md` — группы и подземелье (v11)
- **7.8 Группы и лут** :11 — группа/лидер :13, режимы лута :20, голосование и кворум (`LootModeVote`, `CmdVoteLootMode`) :28, правила раздачи (`LootEntry` :46) :37
- **7.5 Подземелье на 50** :50 — структура :52, энкаунтеры :58, лут :63, тех. бюджет (тик 20–30 Гц) :65, UI рейда :70
- Вопросы 6, 7, 20, 21, 51, 68, 71, 72, 77, 79 :74 · Рекомендация 12 :89

### `.cursor/rules/*.mdc` — производные от документов (README там же)
`core-always` (приоритеты, запрет ECS) · `unity-csharp` (= 01 §6) · `ui-system` (= 02 7.13) · `player-system` (= 01 §6.1).
Не входят в набор (legacy видеокурса): `ecs-code-standards.mdc`, `transcript-analysis.mdc`.

## 2. Нумерация разделов 7.x → файл

| Раздел | Тема | Файл |
|--------|------|------|
| 7.1 | ресурсы, двойная мана | 03 |
| 7.2 | навыки, варианты Shift/Ctrl | 03 |
| 7.3 | классы | 03 |
| 7.4 | профессии | 05 |
| 7.5 | подземелье на 50 | 06 |
| 7.6 | настройки пользователя | 02 |
| 7.7 | панели быстрого доступа | 02 |
| 7.8 | группы и лут | 06 |
| 7.9 / 7.9.1 | предметы / виды экипировки | 04 |
| 7.10 | расширяемость контента | 01 |
| 7.11 | реестр легендарных | 04 |
| 7.12 | бой, статусы, зелья | 03 |
| 7.13 | оверлей (HUD) | 02 |
| 7.14.1 / .2 / .3 | улучшение / кошелёк / торговля | 04 / 05 / 05 |

## 3. Модули → что читать

Описание и критерий готовности модуля N: `01_DEV_PLAN.md` :212 + 2·N (модуль 0 → :212, 5 → :222, 21 → :254).

| # | Модуль | Зависит | Разделы / файлы |
|---|--------|---------|-----------------|
| 0 | Подготовка (asmdef `Shared/Network/Server/Client/Bots`, лог, билды, починка репо — 01 §10) | — | 01 §3, §5, §6, §10 |
| 1 | Сеть: Mirror+KCP, dedicated, ID, тики | 0 | 01 §3, §4; 06 7.5 (частота тика) |
| 2 | Персонаж и камера (предсказание, интерполяция) | 1 | 01 §6.1; 02 7.6 (ввод) |
| 3 | Боты (только ходят), нагрузка, interest management, замеры CPU | 1–2 | 01 §4, §13 (ECS-крючок); 06 7.5 (бюджет) |
| 4 | Ядро данных, расширяемость, валидатор, мастера, локализация | 0 | 01 7.10, §3 |
| 5 | Характеристики и ресурсы | 4 | 03 7.1, 7.12 (`StatModifier`) |
| 6 | Таргет и автоатака (фракции/враждебность, манекен) | 2, 5 | 03 7.1 (реген за попадание); 01 §13 (крючок PvP) |
| 7 | Навыки + каркас оверлея + мин. панель; навыки у ботов | 5, 6 | 03 7.2; 02 7.13, 7.7А |
| 8 | Бой: урон, статусы, зелья, угроза, лог, `CombatContext` | 7 | 03 7.12 |
| 9 | Классы | 7, 8 | 03 7.3; 01 7.10 |
| 10 | Мобы и ИИ, лут-таблицы (соло-политика, `LootEntry`) | 8, 9 | 01 7.10; 06 7.8 |
| 11 | Предметы, инвентарь, кошелёк | 5, 10 | 04 7.9, 7.9.1, 7.14.1; 05 7.14.2 |
| 12 | Профессии, крафт, улучшение своих предметов | 4, 11 | 05 7.4; 04 7.9 (слоты), 7.14.1 |
| 13 | Аккаунты, PostgreSQL, реестр уникальных | 11 | 04 7.9 (божественные), 05 7.14 |
| 14 | Социалка: чат, группы, голосование лута, рейд, друзья, обмен | 13 | 06 7.8, вопрос 79 |
| 15 | Подземелье на 50 (инстанс — сцена в процессе сервера) | 3, 8–14 | 06 7.5 |
| 16 | Зоны и масштаб; зоны и инстансы — отдельные процессы | 3, 13, 15 | 01 §4, 7.10 (`ZoneDefinition`) |
| 17 | Настройки пользователя | 2, 7, 13 | 02 7.6, вопрос 86 |
| 18 | Панели, эмодзи, редактор HUD | 7, 11, 13, 14, 17 | 02 7.7, 7.13 |
| 19 | Реестр легендарных | 11, 13, 16 | 04 7.11 |
| 20 | Торговля: магазины, рынок, почта, заказы улучшения с эскроу | 11, 12, 13, 14 | 05 7.14.3, вопрос 85; 04 7.14.1 |
| 21 | Полировка | все | — |

Статус: модули 0–14 — **в работе** (вертикальный срез, этапы 1–1.13, ветка `claude/vertical-slice`, см. 01 §8–§9); остальные — не начат.

## 4. Индекс сущностей (тип / команда → где определено)

| Сущность | Где |
|----------|-----|
| `CostEntry` (Mana/CombatMana/ReserveMana/Health; Flat/PercentOfMax) | 03 7.1 :29 — ЗАМОРОЖЕНО |
| Мастерство навыка (данные персонажа + таблица кривых, вне `SkillDefinition`) | 03 7.2 :95 |
| Боевая мана / «Третье сердце» (третий запас магов) | 03 7.1 :50 |
| Первичные характеристики | 03 7.12 :144 |
| `SkillDefinition`, `SkillVariant` (Default/Empowered/Weakened) | 03 7.2 :78 — ЗАМОРОЖЕНО |
| `ClassDefinition` | 03 7.3 :111 |
| `StatModifier` (Add/PercentAdd/Multiply, `SourceId`) | 03 7.12 :149 |
| `StatusEffectDefinition` | 03 7.12 :158 |
| `EffectDefinition`, `IEffectHandler`, `EffectHandlerRegistry` | 01 7.10 :148–152 |
| `LegendaryDefinition` | 01 7.10 :168 |
| `MobDefinition`, `ZoneDefinition` (только упоминание) | 01 7.10 :160–163 |
| `ActivityDefinition`, `CombatContext` (крючки PvP) | 01 §13 :418 |
| Сборки `Game.Shared/Network/Server/Client/Bots` | 01 §5 :68 |
| `HudWindowDefinition` | 02 7.13 :49 |
| `HudRoot`, `HudZone`, `WindowManager`, `WindowRegistry`, `MenuBar`, `DragDropService`, `BadgeService` | 02 7.13 :83 |
| `ItemDefinition` (`SocketRules {AllowedSocketTypes, MaxSockets}`), `ItemInstance` (GUID, `UpgradeLevel`, `Sockets`) | 04 7.9 :16–31; компоненты :42 |
| `EquipmentSlotDefinition` (`SocketCap` — единственный источник), `ArmorTypeDefinition`, `WeaponTypeDefinition` | 04 7.9.1 :126 |
| `LegendaryRegistryEntry` | 04 7.11 :146 |
| `UpgradeRules` (в срезе — `upgrade_rules.json`) | 04 7.14.1 :154 |
| `ProfessionDefinition`, `Recipe`, `GatheringNode` | 05 7.4 :22 |
| `CurrencyDefinition`, `Wallet`, `Transact(...)` | 05 7.14.2 :40–46 |
| `MailMessage` | 05 7.14.3 :69 |
| `LootModeVote` | 06 7.8 :34 |
| `LootEntry` | 06 7.8 :46 |

**Сетевые команды (клиент → сервер):** `CmdUseSkill {skillId,targetId,modifier,clientSeq}` 03 :93 · `CmdUseConsumable` 02 :116 / 03 :180 · `CmdSwapLoadout {loadoutId}` 02 :124 · `CmdEmoji {emojiId}` 02 :130 · `CmdUpgradeItem {itemInstanceId,stationId}` 04 :160 · `CmdVoteLootMode {proposalId,accept}` 06 :34.
Сообщения — `struct : NetworkMessage` в `Game.Network`, данные — типы `Game.Shared` (01 §6). **Rpc:** `RpcDamageApplied` — только пример имени, 01 §6 :126.

Поиск любого другого имени: `grep -n "ИмяТипа" docs/0*.md`.

## 5. Открытые вопросы (сквозные №) → файл

| № | Файл | № | Файл | № | Файл |
|---|------|---|------|---|------|
| 1–5 | 03 | 14–19 | 04 | 29 | 01 |
| 6, 7 | 06 | 20, 21 | 06 | 30–32 | 04 |
| 8 | 05 | 22–24 | 04 | 33–35 | 05 |
| 9 | 03 | 25, 26 | 03 | 36–38 | 04 |
| 10–13 | 02 | 27, 28 | 02 | 39–44, 69, 70, 75, 76, 80–84 | 01 |
| 45, 46 | 03 | 47, 48 | 04 | 49, 50 | 05 |
| 51 | 06 | 52–54 | 03 | 55 | 04 |
| 56 | 02 | 57 | 04 | 58 | 05 |
| 59, 60 | 03 | 61 | 04 | 62 | 05 |
| 63, 64 | 03 | 65, 66 | 04 | 67 | 03 |
| 68 | 06 | 69, 70 | 01 | 71, 72 | 06 |
| 73 | 05 | 74 | 03 | 75, 76 | 01 |
| 77 | 06 | 78 | 04 | 79 | 06 |
| 80–84 | 01 | 85 | 05 | 86, 87 | 02 |

Найти: `grep -n "^<№>\. " docs/0*.md`. Последний номер — **87** (новый вопрос = 88). Номера не менять.

**Статус вопроса** виден в самой строке: `**РЕШЕНО (vN):** …` — решено разработчиком (ДЕФОЛТ снят); `**Отложено разработчиком:**` — вернуться позже, действует ДЕФОЛТ; без пометки — открыт, действует ДЕФОЛТ.
Сводка: `grep -n 'РЕШЕНО\|Отложено разработчиком' docs/0*.md`.

**Рекомендации** (тоже сквозные): 1, 5–10, 17–19 → 01 · 2–4, 26, 27 → 03 · 11, 21–25 → 05 · 12 → 06 · 13, 14 → 02 · 15, 16, 20 → 04. Последний номер — 27.

Пометки «ДЕФОЛТ»: решения для выбора — в «Открытых вопросах»; числовые ДЕФОЛТы в тексте — данные баланса (01 §13 «Гибко»). Найти все: `grep -n ДЕФОЛТ docs/0*.md`.

## 6. Состояние репозитория

- **Ветки:** `claude/hopeful-babbage-qws7t3` — документы; `claude/vertical-slice` — документы + **код среза** (см. §8). `main` отстаёт (без документов набора).
- **Unity-проект: `My project/`** (имя с пробелом — в командах кавычки). Unity **6000.3.19f1**, шаблон 3D URP, `activeInputHandler: 1` (только Input System).
- Пакеты (`Packages/manifest.json`): URP 17.3, Input System 1.19, uGUI 2.0, AI Navigation 2.0, Test Framework 1.6, Timeline, **Localization 1.5.14** и **Addressables 2.8.0** (с v29; Addressables закреплён явно — Localization просит старую линию 1.25; таблицы текстов собираются перед сборкой игры, `BuildMenu.BuildAddressables`). **Нет:** Mirror, Cinemachine; Addressables для своего контента не используется.
- `.gitignore` (ветка среза): игнорируются `Library`, `Temp`, `obj`, `Build(s)`, `Logs`, `UserSettings` проекта и `sim-tests/bin|obj`.

## 7. Legacy (видеокурс, не относится к игре; не читать без запроса)

`docs/lessons/`, `docs/rules-pending/`, `docs/approve-questions.md`, `docs/rules-contradictions.md`, `docs/rules-memory-audit.md`, `docs/video-index.md`, `docs/slides-code-reference.md`, `docs/vision.md`, `docs/architecture.md` (заменён 01 §4–§5), `references/` (VTT/PDF/PNG), `tools/`, `scripts/`, `.cursor/skills/`, `.cursor/rules/{ecs-code-standards,transcript-analysis}.mdc`, PNG/WEBP в корне.

## 8. Код (ветка `claude/vertical-slice`, корень `My project/Assets/_Project/`)

Сборки: `Scripts/Shared` → `Game.Shared` (чистый C#), `Scripts/Network` → `Game.Network` (чистый C#, свой TCP), `Scripts/Bots` → `Game.Bots` (чистый C#, headless-боты), `Scripts/Server` → `Game.Server` (чистый C#), `Scripts/Client` → `Game.Client` (Unity, Input System, uGUI), `Scripts/Host` → `Game.Host` (запуск), `Scripts/Editor` → `Game.EditorTools` (только редактор: сборка билда), `Tests/EditMode` → `Game.Tests.EditMode`. Ни клиент, ни общая часть не ссылаются на сервер.

| Что | Где (от `Scripts/`) |
|-----|---------------------|
| Лог с тегами модулей | `Shared/Core/Log.cs`, `LogTags.cs` |
| `Vec3`, `EntityId`, `IRandom` | `Shared/Core/` |
| JSON-модели контента (DTO, PascalCase = ключи JSON) | `Shared/Content/Dto/*Dto.cs`, конфиг — `GameConfig.cs` |
| Перечисления (ресурсы, статы, редкость, …) | `Shared/Content/Enums/` |
| Навык (ЗАМОРОЖЕННАЯ структура), вариант, стоимость, эффект | `Shared/Content/SkillDefinition.cs`, `SkillVariant.cs`, `CostEntry.cs`, `EffectDefinition.cs` |
| Мастерство навыка (вне замороженной структуры) | `Shared/Content/SkillMasteryRule.cs`, `Shared/Rules/MasteryMath.cs` |
| Класс | `Shared/Content/ClassDefinition.cs` |
| **Предмет и компоненты-свойства** (+ реестр с зависимостями) | `Shared/Content/Items/` (`ItemDefinition`, `*Component`, `ItemComponentRegistry`) |
| Сборка и валидация контента | `Shared/Content/ContentBuilder.cs`, `ContentValidator.cs`, `ContentDatabase.cs`; серверная точка — `Server/ServerContentFactory.cs` |
| Формулы: статы, урон/крит, стоимость | `Shared/Rules/StatSheet.cs`, `CombatMath.cs`, `CostMath.cs` |
| Протокол: команды и снимки, коды отказов | `Shared/Protocol/` (`*Command`, `*Snapshot`, `RejectReasons`) |
| Фасад сервера (тик, игроки, команды) | `Server/GameServer.cs`, контекст — `ServerContext.cs` |
| Данные мира (struct-компоненты) | `Server/World/` (`ComponentStore`, `*Data`, `ServerWorld`) |
| Системы | `Server/Systems/`: `Movement`, `Resource` (мана, блокировка, реген, бой), `Skill`, `Combat` (урон, смерть, событие `Died(victim, killer)`), `Targeting`, `Inventory` (отсеки, стаки, экипировка, смена оружия, зелья, расход материалов), `Stat` (в т.ч. множитель улучшения), `Progression` (опыт, уровни, очки), `Upgrade` (наковальня), `Respawn`, `SnapshotBuilder`, `CostCalculator` |
| Эффекты по ключу (7.10) | `Server/Effects/` (`EffectHandlerRegistry`, `*EffectHandler`) |
| Экземпляры предметов, аудит, рюкзак | `Server/Items/` (`ItemFactory`, `ItemInstance`, `IAuditLog`, `Inventory`) |
| Игрок на сервере, таймеры смены оружия | `Server/Players/` (`PlayerCharacter`, `WeaponSwapState`, `PlayerOutbox`) |
| Связь клиента с сервером | `Client/Net/IServerConnection.cs`; локальная — `Host/LocalServerConnection.cs`; по сети — `Client/Net/RemoteServerConnection.cs` |
| Сеть: транспорт и кодек (вопрос 69) | `Network/` (`NetConnection` — кадры и потоки, `NetListener`, `NetworkClientSession`, `ProtocolCodec`, `MessageType`, `Hello`/`WelcomeMessage`, `NetworkConstants`) |
| Сервер в сети (приём игроков, рассылка) | `Server/Net/ServerNetworkHost.cs` (частота — `config.NetworkWorldEveryTicks`, `NetworkStateEveryTicks`, порт — `DefaultPort`) |
| **Кошелёк, слоты, ингредиенты, аффиксы** (модуль 11, вопрос 78) | данные `currencies.json`, `affixes.json`, `SocketCap` в `equipment_slots.json`, компоненты `Shared/Content/Items/SocketsComponent.cs`, `SocketIngredientComponent.cs`, `AffixesComponent.cs`; сервер `Server/Systems/WalletSystem.cs` (`Transact`), `SocketSystem.cs`, бросок слотов и аффиксов — `ItemFactory`, бонусы — `StatSystem`; протокол `Shared/Protocol/ItemDetailSnapshots.cs`, `InsertIngredientCommand`; клиент — `TooltipText`, валюты в `BackpackWindow`, вставка в `WhetstoneWindow`; тесты — `ItemsModule11Tests` |
| **Чат, друзья, обмен, рейды** (модуль 14, вопрос 79) | сервер `Server/Systems/ChatSystem.cs`, `FriendSystem.cs`, `TradeSystem.cs` (+ `Server/World/TradeData.cs`; отмена при шаге — `TradeMoveTolerance`), друзья по согласию — `FriendSystem` + `Server/Players/FriendLists.cs`, `IFriendStore.cs` (`CharacterPersistence` / `OnlineFriendStore.cs`), вкладки чата — `ChatSystem.SetTabs`, `config.ChatDefaultTabs`, `Windows/ChatTabsWindow.cs`, рейд — `GroupSystem` (`ConvertToRaid`, `MoveToSubgroup`, `SetAssistant`, `Voters`, `CloseMates`); друзья, заявки и вкладки в аккаунте — `AccountRecord.Friends`, `FriendRequestsIn/Out`, `ChatTabs`; протокол `Shared/Protocol/ChatMessages.cs`, `SocialMessages.cs`, `TradeMessages.cs`, `GroupCommands.cs`; клиент — `Client/UI/Widgets/ChatWidget.cs` (Enter — `GameInput.ChatSubmit`), `UiFocus.cs` (клавиши игры не работают, пока поле ввода в фокусе), `Windows/FriendsWindow.cs`, `TradeWindow.cs`, `GroupWindow.cs` (рейд), `Widgets/PagerWidget.cs`, `GroupPromptWidget.cs` (запрос обмена), `TargetFrameWidget.cs` (кнопка «Обмен»); тесты `Tests/EditMode/SocialTests.cs` |
| **Перетаскивание и деление стопок** (вопрос 79) | `Shared/Protocol/MoveBagItemCommand.cs` → `InventorySystem.MoveBagItem` (аудит `StackSplit`/`StackMerge`); клиент — `Client/UI/DragTrigger.cs`, `SlotView.BindDrag`, `BackpackWindow` (Shift — половина, Ctrl — один); клик после перетаскивания гасит `HoverTrigger`; тесты `Tests/EditMode/StackMoveTests.cs` |
| **Лавки, рынок, почта, заказы улучшения** (модуль 20, вопрос 85) | данные `config.ShopTiers` и лимиты в `config.json`, станции `Market` / `Mailbox` / `ShopSpot` и `MerchantVisual` в `world.json`; сервер `Server/World/MarketState.cs` (+ `ShopData`, `LotData`, `MailData`, `UpgradeOrderData`), `Server/Systems/ShopSystem.cs`, `MailSystem.cs`, `UpgradeOrderSystem.cs` (через `UpgradeSystem.Attempt`), `StationSystem.cs`, `RequestMemory.cs`, `MarketText.cs`, время — `Server/IClock.cs`; сохранение `Server/Persistence/MarketRecords.cs`, `MarketSerializer.cs`, `ItemRecords.cs`, `IMarketStore` (`FileStorage` → `market.json`), событие `GameServer.Committed` (персонажи и рынок одной записью, журнал `commit.json`); протокол `Shared/Protocol/MarketMessages.cs`, `MailMessages.cs`, `UpgradeOrderMessages.cs`, `MailSubjects.cs`, ответы — `PlayerOutbox.Replies` / `IServerConnection.ReplyReceived` / `ClientState.Last*`; клиент `Client/UI/Windows/MarketWindow.cs`, `ShopWindow.cs`, `MailWindow.cs`, `UpgradeOrdersWindow.cs`, `MarketUi.cs`; тесты `Tests/EditMode/MarketTests.cs`, `PersistenceTests.Market_*` |
| **Настройки пользователя** (модуль 17, вопрос 86) | данные `config.AccountSettings` (значения по умолчанию и пределы), `config.GraphicsPresets`, `FpsLimits`, `ResolutionConfirmSeconds`; сервер `Server/Systems/SettingsSystem.cs`, `AccountRecord.Settings` (`AccountSettingsRecord`), `GameServer.Load/ExportAccountSettings`; протокол `Shared/Protocol/AccountSettingsSnapshot.cs`; клиент `Client/Settings/` (`SettingsService`, `LocalSettings`, `LocalSettingsStore` → `settings_local.json`, `GraphicsSettingsApplier`), `Client/Input/KeyRebinder.cs` (+ `RebindSlot`, `RebindResult`, `KeyLabels`), `UI/Windows/SettingsWindow.cs`, `UI/Widgets/FpsCounterWidget.cs`, `ThirdPersonCamera.Configure`; тесты `Tests/EditMode/SettingsTests.cs`, `PersistenceTests.AccountSettings_*` |
| **Аккаунты и сохранения** (модуль 13, вопрос 76) | `Server/Persistence/` (`Records.cs` — записи, `Interfaces.cs` — репозитории и `IJsonCodec`, `FileStorage.cs`, `CharacterPersistence.cs` — вход, токены, автосохранение, `PasswordHasher.cs`, `CharacterMigrations.cs`, `FileAuditLog.cs`); `GameServer.AddPlayer(CharacterRecord)` / `ExportPlayer`; реестр уникальных — `ItemFactory.UniqueRegistry`; Unity — `Host/UnityJsonCodec.cs`, `GameRoot.CreateServer`; меню — поля логина в `StartMenuView`; протокол — `HelloMessage` (Login, Password, Token), `WelcomeMessage.Token`, ошибки `LoginErrors`; тесты — `PersistenceTests`, кодек тестов — `TestJsonCodec` |
| **Interest management, метрики, боты** (модуль 3) | `Server/World/SpatialGrid.cs`, `Server/Systems/InterestSystem.cs`, `Server/ServerMetrics.cs`; трафик — `ProtocolCodec.EncodeWorld` (неизменные поля один раз) + кэш в `Client/Net/RemoteServerConnection.cs`, `NetConnection.BytesSent/Received`; боты — `Scripts/Bots/` (`Game.Bots`: `BotClient`, `BotSwarm`), запуск `-bots N -connect` (`Host/LaunchOptions.cs`, `GameRoot.StartBots`); вне Unity — `tools/LoadTest/` (README); тесты — `InterestTests`, `BotTests` |
| **ИИ мобов** (модуль 10, вопрос 75) | данные `mobs.json` (`AiProfile`), `ai_profiles.json` → `AiProfile` (`Shared/Content/AiProfile.cs`), способности — навыки `mob_*` в `skills.json`; сервер `Server/Systems/MobAiSystem.cs`, `Server/World/MobAiData.cs` (Idle / Chase / Return); тесты — `Tests/EditMode/MobAiTests.cs` |
| **Статус-эффекты, угроза, боевой лог** (модуль 8; 03 §7.12, вопрос 74) | данные `status_effects.json` → `StatusEffectDefinition` (`Shared/Content/`); сервер `Server/Systems/StatusSystem.cs`, `ThreatSystem.cs`, `Server/World/StatusData.cs`, эффекты `Server/Effects/ApplyStatusEffectHandler.cs`, `DispelEffectHandler.cs`; клиент — `Client/UI/Widgets/StatusRowWidget.cs`, «цель цели» в `TargetFrameWidget`, `Client/UI/Windows/CombatLogWindow.cs` (`L`, события — `ClientState.CombatLog`); тесты — `Tests/EditMode/StatusEffectTests.cs` |
| **Лут на земле** (06 §7.8 `LootEntry`, вопрос 77) | `Server/World/LootData.cs`, `LootSystem` (`Give` — по режимам с выкладкой, `PickUp`, `Update` — исчезновение), `Shared/Protocol/PickUpLootCommand.cs`, `EntityKind.Loot`; клиент — `EntityViewManager` (шар цвета редкости), клик в `PlayerInputController`; тесты — `GroundLootTests` |
| **Группы и лут** (06 §7.8, вопросы 71, 72) | сервер `Server/Systems/GroupSystem.cs`, `LootSystem.cs` (участие в бою, выбор получателя), `Server/World/GroupData.cs`; `InventorySystem.CanFit`; протокол `Shared/Protocol/GroupCommands.cs`, `GroupSnapshot.cs`, `LootMode.cs`; клиент — `Client/UI/Windows/GroupWindow.cs` (`P`), `Widgets/GroupFramesWidget.cs`, `GroupPromptWidget.cs`, кнопка «В группу» в `TargetFrameWidget.cs`; тесты — `Tests/EditMode/GroupTests.cs` |
| **Дуэли** (вопрос 70) | сервер `Server/Systems/DuelSystem.cs`, `Server/World/DuelData.cs`; враждебность и помощь — `CombatSystem.AreHostile` / `CanAssist`, PvP — `Server/Systems/CombatContext.cs`; протокол `Shared/Protocol/DuelCommands.cs`, `DuelSnapshot.cs`; клиент — кнопка в `Client/UI/Widgets/TargetFrameWidget.cs`, `DuelWidget.cs`, круг `Client/World/DuelArenaView.cs`; тесты — `Tests/EditMode/DuelTests.cs` |
| Стартовое меню (одиночная / хост / подключение) | `Client/UI/StartMenuView.cs`; режимы и параметры командной строки — `Host/LaunchOptions.cs`, `Host/GameRoot.cs` |
| Сборка Windows в один клик (меню Game) | `Editor/BuildMenu.cs` → `My project/Builds/Windows/` (+ `StartServer.bat`, `StopServer.bat` — остановка с сохранением через `server.stop`, `StartHost.bat`); материал фигур для сборки — `Editor/WorldMaterialAsset.cs` → `Resources/Materials/WorldLit.mat`, фигуры — `Client/World/WorldPrimitives.cs` |
| **Экономия работы сервера и сети** (ревизия v31; правила — `CLAUDE.md`, «Памятка») | индекс имён и общий `NameOf` — `Server/ServerContext.cs`; поиск предмета в сумке — `Inventory.IndexOfInstance`; кэш идентификатора — `ItemInstance.SourceId`; снимки по тикам отправки — `PlayerOutbox.WorldEveryTicks` / `StateEveryTicks` (`GameServer.Tick`, выставляет `ServerNetworkHost`); только изменившееся — `ProtocolCodec.EncodeWorld` / `SameDynamic`, `NetworkClientSession.Complete`, `ServerNetworkHost.SentEntities` / `SentState`; изоляция ошибок команд — `GameServer.RethrowCommandErrors`; тест — `NetworkTests.UnchangedData_IsNotResent_ButTheClientStillSeesItAll`; замер — `tools/LoadTest` |
| **Панели быстрого доступа** (модуль 18, вопрос 87) | данные `config.SkillBarRows`, `SkillBarSlotsPerRow`, `ConsumableBarSlots`, `LoadoutCount`; сервер `Server/Systems/PanelSystem.cs`, `Server/Players/PanelData.cs` (`PlayerCharacter.Panels`), хранение — `CharacterRecord.SkillBar` / `ConsumableBar` / `Loadouts`; протокол `Shared/Protocol/PanelMessages.cs` (`SetSkillSlotCommand`, `SetConsumableSlotCommand`, `SaveLoadoutCommand`, `ApplyLoadoutCommand`, `PanelsMessage`); клиент `Client/UI/DragDropService.cs` (`ClientContext.Drag`), `ClientState.Panels`, `SkillBarWidget`, `ConsumableBarWidget`, перетаскивание в `SkillBookWindow` и `BackpackWindow`, комплекты — `CharacterWindow`; тесты `Tests/EditMode/PanelTests.cs` |
| **Ядро данных** (модуль 4, вопросы 80–84) | пакеты: `Resources/Content/packs.json`, `Resources/Content/<пакет>/pack.json` + файлы (основной — `base`), реестр ID — `Resources/Content/content_ids.json`, лимиты — `base/balance_limits.json`; код `Shared/Content/Packs/` (`ContentFileTable` — все типы файлов, `ContentPackLoader`, `IContentSource`, `FileContentSource`, `ContentHash`, `ContentSyncRule`), `Shared/Content/Ids/` (`ContentIdLedger`, `ContentDeprecation`), `Shared/Content/BalanceLimitsValidator.cs`, `Shared/Content/Csv/` (`CsvTable`, `CsvBalanceMapper`); игра — `Host/UnityContentLoader.cs`; хэш при входе — `HelloMessage.ContentHash`, `ServerNetworkHost.HandleHello`; редактор `Editor/Content/` (`Cards/*Card.cs`, `ContentSync`, `ContentSyncTarget`, `ContentBuildCheck`, `ContentWizardWindow`, `CsvBalanceWindow`, `ContentPackWindow`, `LocalizationSource`, `LocalizationSync`), карточки — `Assets/_Project/Content/<пакет>/`; тексты — `Client/Localization/UnityLocalizationService.cs`, `LocalizationFactory.cs`, `ILanguageService.cs`, ассеты Localization — `Assets/_Project/Localization/`; тесты `ContentPackTests`, `ContentDataTests`, `ContentSyncRuleTests`, эталонный пакет `Tests/EditMode/Fixtures/sample_pack/` |
| **Целостность и защита (ревизия v28)** | канонический логин — `CharacterPersistence.Canonical`; атомарная запись — `GameServer.Committed`, `ServerContext.Commit`, `ICommitStore` (`Persistence/Interfaces.cs`), `FileStorage.Commit` / журнал `commit.json`, `CommitRecord` (`MarketRecords.cs`); `null` после `JsonUtility` — `ItemRecords.NullIfEmpty`; сеть — `config.MaxCommandsPerTick`, `HelloTimeoutSeconds` (`ServerNetworkHost.Poll`), очередь приёма — `NetConnection`; движение `NaN` — `GameServer.Handle`; тесты — `Tests/EditMode/IntegrityTests.cs`, JSON как в Unity — `Tests/EditMode/JsonUtilityLike.cs` |
| Модель клиента и команды | `Client/State/ClientState.cs`, `Client/ClientCommands.cs`, сервисы — `Client/ClientContext.cs` |
| Локализация (гибрид) | `Client/Localization/` (`ILocalizationService`, JSON-таблица, `UiKeys`) |
| Ввод (Input Actions в коде), модификаторы | `Client/Input/GameInput.cs`, `ModifierTracker.cs`, `PlayerInputController.cs` |
| Камера | `Client/CameraRig/ThirdPersonCamera.cs` |
| Сущности в мире | `Client/World/EntityViewManager.cs`, `EntityView.cs` |
| Оверлей: корень, зоны, фабрика uGUI | `Client/UI/HudRoot.cs`, `UiFactory.cs`, `HudLayout.cs`, `HudPalette.cs` |
| Виджеты оверлея | `Client/UI/Widgets/` (рамки, панель навыков, расходники, меню, мини-карта, сообщения, числа урона, таблички, тексты подсказок) |
| Окна механик | `Client/UI/Windows/` (`WindowManager`, `WindowFactory`, `HudWindow` — база, `CharacterWindow` (+ раздача очков), `BackpackWindow` (вкладки-отсеки), `AnvilWindow`, `SkillBookWindow`, `MapWindow`, `SettingsWindow`, `GroupWindow`, `FriendsWindow`, `TradeWindow`, `PlaceholderWindow`) |
| Полоса опыта, сообщения и уведомления | `Client/UI/Widgets/ExperienceBarWidget.cs`, `MessageWidget.cs` |
| NPC-станции (точильный камень — улучшение, наковальня — крафт) | данные `world.json → Stations` (`Type`: `StationTypes`); сервер `Server/World/StationData.cs`, `UpgradeSystem`, `CraftingSystem` (рецепты — `recipes.json`, `RecipeDto`); окна `WhetstoneWindow`, `AnvilWindow`, выбор окна — `WindowFactory.ForStation`; клиент — клик в `PlayerInputController` → `HudRoot.OpenStation` |
| Кнопки окон слева от панели навыков | `Client/UI/Widgets/IconMenuWidget.cs` (зона `SkillBarLeft` в `hud_windows.json`) |
| Портрет персонажа (сцена-превью, слой 31) | `Client/World/CharacterPreview.cs`, `PreviewAttachments.cs`, `RenderLayers.cs` |
| Запуск и сборка всего | `Host/GameBootstrap.cs` → `Host/GameRoot.cs` |
| Контент | `Resources/Content/<пакет>/*.json` (в т.ч. `bag_sections`, `upgrade_rules`, `levels`; правятся карточками в `Assets/_Project/Content`); тексты — `Resources/Localization/ru.json` (+ таблица Unity Localization `Game`) |
| Тесты (EditMode, они же в .NET; сеть — `NetworkTests` на реальных сокетах) | `Tests/EditMode/`; запуск вне Unity: `cd sim-tests && dotnet test` (в корне репозитория) |

**Как добавить:** новое свойство предмета — компонент в `Shared/Content/Items/` + регистрация в `ItemComponentRegistry.CreateDefault`; новый эффект — `IEffectHandler` + регистрация в `GameServer` и ключ в `ServerContentFactory.BuiltInEffectKeys`; новое окно — запись в `hud_windows.json` + класс окна в `WindowFactory`; новый текст — ключ в `ru.json`; новый навык/предмет/класс/моб — только JSON.
