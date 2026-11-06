# MAP — детальная карта проекта

> Вход: `CLAUDE.md`. Здесь — где искать конкретный функционал за 1–2 вызова.
> Номера строк в больших файлах дрейфуют — искать по заголовкам (`grep -n "^## "`).

## 1. Рецепты навигации (дешёвые)

| Нужно | Команда / файл |
|-------|----------------|
| Какой урок про X | §3 (таблица уроков) или §4 (символ → урок) |
| Оглавление урока | `grep -n "^## " docs/lessons/<id>-*.md` |
| Только выводы урока | `sed -n '/^## Выводы/,/^## CODE/p' docs/lessons/<id>-*.md` |
| Только код урока | `sed -n '/^## CODE STANDARDS/,/^## Не выносить/p' docs/lessons/<id>-*.md` |
| Чеклист правил урока | `docs/rules-pending/<id>.md` (~1.5 КБ) |
| Блокеры перед записью rules | `grep -n "^### " docs/rules-contradictions.md` |
| Стратегические решения A–L | `sed -n '20,57p' docs/approve-questions.md` |
| Вопросы по модулю N | `grep -n "### Модуль" docs/approve-questions.md` → `sed -n` |
| Цитата/таймкод из видео | `grep -n "<слово>" "references/transcripts/<id> "*.vtt` (имена с пробелами и кириллицей — в кавычках) |
| Код со слайдов (OCR) | `grep -n "^## " docs/slides-code-reference.md` |
| Статус урока | `grep -n "| <id> |" docs/video-index.md` |

## 2. Анатомия документов

**`docs/lessons/<id>-<slug>.md`** (все одинаковые):
`## Мета` (VTT-путь, slides, зависит от, статус, следующий) → `## Суть урока` → `## Выводы (на одобрение)`
(таблица `# | Вывод (ID <id>-NN) | Tier | Источник(таймкод) | → rule file | Обоснование | ⚠️ Подвохи`) →
`## CODE STANDARDS` (код C# + таблица стандартов) → `## Не выносить в rules` → `## ⚠️ Противоречия` → `## Вопросы на approve` → `## После approve`.
Модуль 1 (1.2–1.4) — расширенный формат, плюс `## Слайды vs VTT — расхождения`.

**`docs/rules-pending/<id>.md`**: заголовки tier (🔴/🟠/🟡/🟢) → подзаголовок целевого `.mdc` → `- [ ] **<id>-NN** текст *(VTT mm:ss)*`.
ID вывода `<id>-NN` (напр. `3.4-03`) — сквозной ключ между lesson ↔ pending ↔ contradictions.

**Транскрипты**: `references/transcripts/<id> <Название>.vtt`; есть для всех x.2–x.5, кроме 15.2.
**Слайды**: `references/slides/<id>/page-NN.png` (18 папок: 1.2, 3.2, 4.4, 5.2, 5.3, 6.2, 6.3, 7.2, 7.3, 8.2, 13.2–13.4, 14.3, 14.4, 15.2, 15.3, 18.2) + PDF в `references/`.

## 3. Индекс уроков (62)

| id | Тема | Файл урока | Slides | → целевые .mdc | Ключевые типы / API |
|----|------|-----------|--------|----------------|---------------------|
| 1.2 | ECS vs ООП | `1.2-ecs-vs-oop.md` | pdf/png | dots-ecs, ecs-code-standards, optimization | IJobForEach, EntityManager, SetComponentObject, GetEntityQuery, EntityQuery, TimerComponent |
| 1.3 | Pure / Hybrid | `1.3-pure-hybrid.md` | pdf/png | dots-ecs, ecs-code-standards, development-process | DynamicBuffer, PlayerMovementJob, SetComponentObject, TimerComponent |
| 1.4 | Практика ECS | `1.4-practical-ecs.md` | — | dots-ecs, ecs-code-standards, development-process | ComponentSystemBase, DogMoveComponent, DogMoveSystem, ISystem, SystemBase, Translation, GetEntityQuery, LocalTransform, EntityQuery |
| 2.2 | Типы ввода | `2.2-input-types.md` | — | input-system, dots-ecs, ui-system | InputAction |
| 2.3 | Input System setup | `2.3-input-system-setup.md` | — | input-system, dots-ecs, ecs-code-standards | InputActions |
| 2.4 | Ввод → движение | `2.4-input-movement.md` | — | ecs-code-standards, input-system, dots-ecs | CharacterMoveSystem, MoveData, UserInputData, UserInputSystem, GetEntityQuery, InputAction, InputData, LocalTransform, EntityQuery |
| 2.5 | Ввод → стрельба | `2.5-input-shooting.md` | — | input-system, ecs-code-standards, gameplay-interactions | AbilityDispatchSystem, CharacterShootSystem, IInputAbility, SerializeReference, ShootData, MoveData, ShootAbility, UserInputData, UserInputSystem |
| 3.2 | Теория физики | `3.2-physics-theory.md` | pdf/png | physics-system, dots-ecs, development-process |  |
| 3.3 | Физика ECS | `3.3-physics-ecs.md` | — | ecs-code-standards, physics-system, unity-csharp | ActorColliderData, CapsuleCollider, CollisionSystem, EnumColliderType, CollisionAbility, LocalTransform, EntityQuery, IAbility, IConvertGameObjectToEntity |
| 3.4 | Урон | `3.4-collision-damage.md` | — | gameplay-interactions, dots-ecs, unity-csharp | CollisionContext, DamageRequest, TrapAbility, ICollisionAbility, CharacterHealth, IAbility |
| 3.5 | Collision actions | `3.5-collision-actions.md` | — | gameplay-interactions, physics-system, unity-csharp | CollisionActions, CollisionActionsAbilities, ICollisionContext, IAbilityTarget, TrapAbility, CollisionAbility, ICollisionAbility, IAbility |
| 4.2 | ScriptableObject | `4.2-serialization-scriptable-object.md` | — | save-data, unity-csharp, development-process | CharacterHealth |
| 4.3 | JSON / PlayerPrefs | `4.3-serialization-json.md` | — | save-data, architecture-patterns, unity-csharp | GameSessionState, JsonUtility, PlayerStats, ShootAbility, CharacterHealth |
| 4.4 | Remote storage | `4.4-remote-data-storage.md` | pdf/png | save-data, development-process |  |
| 4.5 | Google Drive | `4.5-google-drive.md` | — | save-data, development-process, architecture-patterns | GoogleDriveTools, IRemoteSaveProvider |
| 5.2 | Введение AI | `5.2-ai-intro.md` | pdf/png | gameplay-ai, development-process |  |
| 5.3 | Алгоритмы AI | `5.3-ai-algorithms.md` | pdf/png | gameplay-ai, development-process |  |
| 5.4 | Utility System | `5.4-utility-system.md` | — | gameplay-ai, dots-ecs, ecs-code-standards | AIAgent, AI_BehaveSystem, AI_EvaluateSystem, BehaviorManager, IBehaviour, RotateBehaviour, WaitBehaviour, FindObjectOfType, IConvertGameObjectToEntity |
| 5.5 | Asset Store AI | `5.5-ai-asset-store.md` | — | gameplay-ai, development-process |  |
| 6.2 | Понятие паттернов | `6.2-design-patterns-intro.md` | pdf/png | architecture-patterns, development-process |  |
| 6.3 | Обзор паттернов | `6.3-design-patterns-overview.md` | pdf/png | architecture-patterns, dots-ecs, gameplay-ai |  |
| 6.4 | DI / Extenject | `6.4-dependency-injection.md` | — | architecture-patterns, development-process | AsSingle, IInitializable, ITickable, InstallBindings, MonoInstaller, MyInstaller, NonLazy, SceneContext |
| 7.2 | Теория графики 1 | `7.2-graphics-pipeline.md` | pdf/png | graphics, optimization, shaders |  |
| 7.3 | Теория графики 2 | `7.3-graphics-materials.md` | pdf/png | shaders, graphics, graphics-lighting |  |
| 7.4 | Shader Graph / URP | `7.4-shader-graph.md` | — | shaders, graphics, optimization |  |
| 7.5 | VFX / Particles | `7.5-vfx-particles.md` | — | vfx, optimization | ParticleSystem |
| 8.2 | Теория освещения | `8.2-lighting-theory.md` | pdf/png | graphics-lighting, graphics |  |
| 8.3 | Lighting window | `8.3-lighting-window.md` | — | graphics-lighting, optimization |  |
| 8.4 | Rendering Pipelines | `8.4-rendering-pipelines.md` | — | graphics, optimization, graphics-lighting |  |
| 9.2 | Animator | `9.2-animation-builtin.md` | — | animation-system |  |
| 9.3 | Animator API + ECS | `9.3-animator-api.md` | — | animation-system, unity-csharp | AnimData, DogAnimSystem, NativeArray |
| 9.4 | DOTween | `9.4-procedural-animation.md` | — | animation-system | ShrinkAbility |
| 9.5 | Advanced | `9.5-advanced-animation.md` | — | animation-system, optimization |  |
| 10.2 | Теория async | `10.2-async-theory.md` | — | async-concurrency, optimization | IJob, IJobParallelFor, NativeArray |
| 10.3 | Task + Jobs | `10.3-async-jobs.md` | — | async-concurrency, optimization | IJobParallelForTransform, IJob, IJobParallelFor, NativeArray |
| 10.4 | UniRx | `10.4-reactive-unirx.md` | — | async-concurrency | CompositeDisposable, ObservableWWW, ObserveOnMainThread |
| 11.2 | Обзор редактора | `11.2-editor-overview.md` | — | editor-extensions |  |
| 11.3 | Custom inspectors | `11.3-custom-inspectors.md` | — | editor-extensions | ApplyModifiedProperties, AssetDatabase, CanEditMultipleObjects, CustomEditor, EditorWindow, GUIDToAssetPath, LoadAssetAtPath, MenuItem, OnInspectorGUI |
| 11.4 | Odin Inspector | `11.4-odin-inspector.md` | — | editor-extensions | HideMonoScript, LabelText, ShowInInspector |
| 12.2 | Прокачка | `12.2-level-up-system.md` | — | gameplay-meta, unity-csharp | CharacterData, ILevelUp, LevelUpActions, LevelUpHealth, CharacterHealth |
| 12.3 | Инвентарь | `12.3-inventory-system.md` | — | gameplay-meta, ui-system, ecs-code-standards | GiveItemPickupAbility, GiveScorePickup, IItem, InventoryRoot, PickupDestroySystem, StarItemAbility, UiItem, EntityManager, IAbilityTarget |
| 12.4 | Крафт | `12.4-crafting-system.md` | — | gameplay-meta, ui-system, ecs-code-standards | CraftController, CraftSettings, ICraftable |
| 12.5 | UI / MVVM | `12.5-ui-game-state-mvvm.md` | — | ui-system | INotifyPropertyChanged, OneWayPropertyBinding, PropertyChanged, FindObjectOfType |
| 13.2 | Теория сети | `13.2-networking-theory.md` | pdf/png | networking, monetization |  |
| 13.3 | Low-level | `13.3-networking-low-level.md` | pdf/png | networking |  |
| 13.4 | Real-world | `13.4-networking-real-world.md` | pdf/png | networking |  |
| 13.5 | Photon Cloud | `13.5-photon-cloud.md` | — | networking | ConnectUsingSettings, JoinOrCreateRoom, OnConnectedToMaster, PhotonAnimatorView, PhotonServerSettings, PhotonTransformView, PhotonView, RoomOptions, InputData |
| 14.2 | Profiler | `14.2-profiler.md` | — | optimization, ecs-code-standards |  |
| 14.3 | CPU / GC | `14.3-optimization-cpu-gc.md` | pdf/png | optimization, unity-csharp, ecs-code-standards | FindObjectOfType |
| 14.4 | UI perf | `14.4-optimization-ui.md` | pdf/png | ui-system, optimization |  |
| 15.3 | Аналитика | `15.3-user-analytics.md` | pdf/png | monetization, build-release |  |
| 15.4 | IAP | `15.4-in-app-purchases.md` | — | monetization, build-release |  |
| 16.2 | Теория звука | `16.2-audio-theory.md` | — | audio-system, optimization |  |
| 16.3 | Wwise интеграция | `16.3-wwise-integration.md` | — | audio-system | AkInitializer |
| 16.4 | Wwise editor | `16.4-wwise-editor.md` | — | audio-system | AkAudioListener, AkBank, AkEvent, AudioEvents |
| 17.2 | SDK | `17.2-sdk-integration.md` | — | build-release, monetization | SdkBootstrap |
| 17.3 | Android | `17.3-android-build.md` | — | build-release |  |
| 17.4 | iOS | `17.4-ios-build.md` | — | build-release |  |
| 17.5 | Stores | `17.5-store-publishing.md` | — | build-release, monetization |  |
| 18.2 | Теория тестов | `18.2-testing-theory.md` | pdf/png | testing, build-release, networking |  |
| 18.3 | NUnit | `18.3-nunit-setup.md` | — | testing |  |
| 18.4 | Automation | `18.4-test-automation.md` | — | testing, editor-extensions, development-process | CraftRecipeMatcher |

Целевые `.mdc` (ещё не созданы, кроме `ecs-code-standards`): см. `docs/approve-questions.md` → «Целевые rule-файлы».

## 4. Функционал → где описано

Игровой каркас курса (Hybrid ECS, MonoBehaviour-«Ability» + ECS-системы):

| Подсистема | Цепочка / ключевые типы | Уроки |
|------------|-------------------------|-------|
| ECS-основа | `IComponentData` (tag/data), `ComponentSystemBase`/`SystemBase`, `EntityQuery` в `OnCreate`, Convert and Inject GameObject, `IConvertGameObjectToEntity` | 1.2–1.4; правила: `.cursor/rules/ecs-code-standards.mdc` |
| Ввод | Input System → `UserInputSystem` пишет `InputData`/`UserInputData` → `CharacterMoveSystem` (`MoveData`) | 2.2–2.4 |
| Стрельба / abilities | `IAbility`, `ShootAbility`, `ShootData` (tag), `CharacterShootSystem`, `IInputAbility`, `AbilityDispatchSystem` | 2.5 |
| Коллизии | `CapsuleCollider` → `ActorColliderData` → `CollisionSystem` (`Overlap*NonAlloc`) → `CollisionAbility` | 3.3 |
| Урон / взаимодействия | `ICollisionAbility : IAbility`, `TrapAbility`, `CharacterHealth` (demo), `DamageRequest` (предложение) | 3.4 |
| Композиция действий | `CollisionActions` (список `IAbility`), `IAbilityTarget`, `ICollisionContext` — **канон** взамен 3.4 | 3.5 |
| Настройки / сохранения | `ScriptableObject Settings` → `JsonUtility`/PlayerPrefs (`PlayerStats`, `GameSessionState`) → remote (`IRemoteSaveProvider`, `GoogleDriveTools`) | 4.2–4.5 |
| AI | FSM/BT/Utility; `IBehaviour` (`Evaluate`/`Behave`), `BehaviorManager`, `AIAgent`, `AI_EvaluateSystem`, `AI_BehaveSystem` | 5.2–5.5 |
| Паттерны / DI | каталог 11 паттернов; Extenject: `SceneContext`, `MonoInstaller`, `Container.Bind…AsSingle().NonLazy()`, `[Inject]`, `ITickable` | 6.2–6.4 |
| Графика / свет | pipeline, материалы, Shader Graph (URP), VFX/Particles, Lighting window, URP Asset | 7.2–8.4 |
| Анимация | Animator FSM; `AnimData : IComponentData` + `DogAnimSystem`; DOTween (`ShrinkAbility`); Timeline/IK обзор | 9.2–9.5 |
| Async | Coroutines vs Task vs Jobs (`IJob`, `IJobParallelFor`, `NativeArray`) vs UniRx | 10.2–10.4 |
| Editor | `EditorWindow`+`MenuItem`, `CustomEditor`/`SerializedProperty`, Odin | 11.2–11.4 |
| Meta-gameplay | level-up (`CharacterData`, `ILevelUp`), инвентарь (`IItem`, `UiItem`, `InventoryRoot`, pickups), крафт (`CraftController`, `ICraftable`, `CraftSettings`) | 12.2–12.4 |
| UI | MVVM, UnityWeld, `INotifyPropertyChanged`; UI perf (не обновлять Text каждый кадр) | 12.5, 14.4 |
| Сеть | теория (P2P/client-server, lag, TCP/UDP) → Photon PUN2 (`PhotonView`, RPC, `PhotonNetwork.Instantiate`) | 13.2–13.5 |
| Оптимизация | Profiler/Frame Debugger, Update overhead, `Find*`/GC, UI rebuild | 14.2–14.4 |
| Монетизация | аналитика, Unity IAP (Consumable/Non-Consumable/Subscription) | 15.3–15.4 |
| Аудио | AudioSource/Listener; Wwise (`AkInitializer`, `AkBank`, `AkEvent`) | 16.2–16.4 |
| Сборка / релиз | SDK (Facebook, AppMetrica, `SdkBootstrap`), Android keystore, iOS Xcode, сторы | 17.2–17.5 |
| Тесты | типы тестов, Unity Test Framework, asmdef `Tests`, `[UnityTest]`, `CraftRecipeMatcher` | 18.2–18.4 |

Поиск любого другого символа: `grep -rln "ИмяТипа" docs/lessons/` (имена в документах — в бэктиках).

## 5. Нерешённые вопросы (кратко)

`docs/rules-contradictions.md` — 🔴 Blockers:
A — ECS strict struct vs Hybrid default · B — `Transform` vs `LocalTransform` в системах ·
C — `IAbility`/`ICollisionAbility`/композиция · D — `FindObjectOfType` запрет vs one-shot ·
E — `[SerializeField] private` vs public-поля курса · F — server authority vs Photon PUN demo ·
G — lifecycle Extenject vs ECS tick · H — Service Locator vs DI vs ui-system · I — Instantiate / Pool / Zenject factory.

`docs/approve-questions.md` — стратегические A–L (рекомендации senior): URP; Input System only; Hybrid ECS (simulation в подпапке);
Extenject только meta/save/settings; локальный save = файл+JsonUtility; UI = events + plain ViewModel; PUN2 только учебно;
Task+Jobs, UniRx optional; Odin optional; аудио через `IAudioService`; канон взаимодействий = 3.5. **Ни одно не утверждено.**

## 6. Unity-проект `MyGame/` (что известно без Assets)

- `MyGame.sln` + 5 csproj. Сгенерированы Unity **6000.3.19**; в csproj много ссылок на `Library/PackageCache` (не в git).
- Задуманная сборочная схема (по `Compile Include`): `Assets/_Project/Scripts/{Shared,Client,Server}/` → `Game.Shared`, `Game.Client` (ссылается на Shared), `Game.Server`.
  Это **расходится** с `docs/architecture.md` (Core/Systems/Gameplay/UI) — при появлении кода уточнить у пользователя.
- `MyGame/Assets`, `ProjectSettings`, `Packages` **не закоммичены** → открыть/собрать проект из репо нельзя.
- `.gitignore` исключает `MyGame/{Library,Temp,Obj,Build,Logs,UserSettings}`, но `MyGame/obj/` (нижний регистр) в git попал — мусор.

## 7. Служебные файлы (обычно не нужны)

| Файл | Что |
|------|-----|
| `scripts/_translate_rules_pending.py` | список пар замен EN→RU (~550 строк данных) + `translate_file()`/`main()` в конце; применяется к pending/lessons/contradictions |
| `tools/ocr_slides.ps1` | Windows OCR PNG-слайдов → txt; результаты `tools/ocr_output_*.txt` |
| `docs/rules-memory-audit.md` | размеры/токены lesson-файлов (lessons ~113k ток., pending ~37k, mdc ~5.5k) |
| корневые `*.png`, `*.webp` | скриншоты (Cursor и пр.), к коду не относятся |
