# Вопросы на approve — сводка

> Сводный список для ревью перед записью в `.cursor/rules/`.  
> Детали, tier и «⚠️ Подвohи» — в [`docs/lessons/`](lessons/). Чеклисты — в [`docs/rules-pending/`](rules-pending/).

## Как одобрять

1. Открыть lesson doc модуля → таблица **Выводы** + **Вопросы на approve**
2. Отметить `[x]` в соответствующем `docs/rules-pending/{id}.md`
3. Для стратегических решений — ответить в этом файле (секция ниже) или обновить [`vision.md`](vision.md) / [`architecture.md`](architecture.md)
4. **«Одобряю» без «запиши»** → rules **не** попадают в `.mdc`
5. Запись в `.mdc` — только по явной команде: *«запиши одобренные rules X.Y»*

**Разобрано:** 62 урока · **Pending:** 62 чеклиста · **В `.mdc`:** только `ecs-code-standards.mdc`  
**Противоречия:** [`rules-contradictions.md`](rules-contradictions.md) — **9 blockers** перед записью rules  
**Не разобрано:** 15.2 (нет VTT, слайды есть в `references/slides/15.2/`)

---

## Стратегические решения (vision / architecture)

Эти пункты влияют на несколько модулей. Рекомендуется решить **до** массовой записи rules.

| # | Тема | Конфликт / выбор | Уроки | Рекомендация senior (8y) |
|---|------|------------------|-------|--------------------------|
| **A** | **Render Pipeline** | Курс → **URP** (7.4, 8.x); `vision.md` — TBD | 7.4, 8.2–8.4 | Зафиксировать **URP** в `vision.md`; Built-in — только legacy mention |
| **B** | **Input** | Legacy vs **Input System Package** | 2.2–2.5 | Input System only; Legacy — запрет в rules |
| **C** | **ECS scope** | DOTS на весь gameplay vs simulation subfolder | 1.2, 1.3, 1.4 | Hybrid default: MB = bridge/authoring; simulation в ECS subfolder |
| **C2** | **Transform в demo** | `Transform` в системах (1.4) vs `LocalTransform` | 1.4 | Rule «не читать Transform в systems» — сразу; demo пометить anti-pattern |
| **D** | **DI (Extenject)** | DI everywhere vs meta/settings only | 6.4, 4.3, 12.x | Extenject для **meta, save, settings, mocks**; не в hot ECS simulation |
| **E** | **Save / remote** | PlayerPrefs / JSON / Google Drive / server | 4.2–4.5 | Local: файл + JsonUtility (v1); Google — **dev-only**; ADR для backend |
| **F** | **UI binding** | Unity Weld MVVM vs events (`ui-system.mdc`) | 12.5 | Events + plain ViewModels без Weld для старта; Weld optional |
| **G** | **Networking** | Photon PUN2 vs theory-only / Fusion later | 13.2–13.5 | Theory + ADR; PUN2 — учебный stack, не prod default без review |
| **H** | **Async stack** | Coroutines / Task / Jobs / UniRx | 10.2–10.4 | Task + Jobs для gameplay; UniRx optional; Coroutines — legacy APIs only |
| **I** | **Odin Inspector** | Asset Store dependency | 11.4 | Optional; решение в `vision.md` (solo dev vs team) |
| **J** | **Audio** | Unity Audio vs **Wwise** | 16.2–16.4 | Wwise если уже в pipeline курса; иначе abstraction `IAudioService` |
| **J2** | **Monetization analytics** | Unity Analytics / Firebase / custom | 15.3–15.4 | Зафиксировать один provider в `vision.md` |
| **K** | **Code style** | `public` Inspector fields (курс) vs `[SerializeField] private` | 1.x, ecs-code-standards | Следовать `unity-csharp.mdc`; курс — reference, не override |
| **L** | **Interaction pattern** | `ICollisionAbility` (3.4) vs canonical actions (3.5) | 3.4, 3.5, 2.5 | Один канон: **3.5 collision actions**; 3.4 — merge или deprecate в rules |

### Ваши решения (заполнить)

```
A — URP: [ ] да  [ ] нет  [ ] отложить
B — Input System only: [ ] да  [ ] Both на курс
C — ECS scope: [ ] simulation subfolder  [ ] весь gameplay
D — Extenject scope: [ ] meta only  [ ] wider
E — Remote save: [ ] dev-only Google  [ ] другой backend: ___
F — UI: [ ] events  [ ] Unity Weld  [ ] hybrid
G — Networking: [ ] PUN2  [ ] theory/ADR only  [ ] другой: ___
H — UniRx: [ ] adopt  [ ] optional
I — Odin: [ ] yes  [ ] no
J — Audio: [ ] Wwise  [ ] Unity Audio
L — Interactions: [ ] 3.5 canonical  [ ] keep both
```

---

## По модулям

Краткие вопросы; полный контекст — в lesson doc.

### Модуль 1 — ECS

| Урок | Lesson | Pending | Ключевые вопросы |
|------|--------|---------|------------------|
| 1.2 | [ecs-vs-oop](lessons/1.2-ecs-vs-oop.md) | [1.2](rules-pending/1.2.md) | Scope DOTS; reflection rule; `IJobForEach` как API курса |
| 1.3 | [pure-hybrid](lessons/1.3-pure-hybrid.md) | [1.3](rules-pending/1.3.md) | Convert на все prefabs; naming `*Data`; pure job ADR |
| 1.4 | [practical-ecs](lessons/1.4-practical-ecs.md) | [1.4](rules-pending/1.4.md) | Inject vs SubScene bake; rule Transform; git URL Entities |

**Slides:** `references/slides/1.2/` (общие с 1.3)

### Модуль 2 — Input

| Урок | Lesson | Pending | Ключевые вопросы |
|------|--------|---------|------------------|
| 2.2 | [input-types](lessons/2.2-input-types.md) | [2.2](rules-pending/2.2.md) | Legacy ban; `input-system.mdc` vs `player-system.mdc` |
| 2.3 | [input-system-setup](lessons/2.3-input-system-setup.md) | [2.3](rules-pending/2.3.md) | Input Settings Asset; один vs split `.inputactions` |
| 2.4 | [input-movement](lessons/2.4-input-movement.md) | [2.4](rules-pending/2.4.md) | System names; `MoveData` vs flags; speed storage |
| 2.5 | [input-shooting](lessons/2.5-input-shooting.md) | [2.5](rules-pending/2.5.md) | `IAbility` hierarchy; one-shot vs cooldown; SerializeReference |

### Модуль 3 — Физика

| Урок | Lesson | Pending | Ключевые вопросы |
|------|--------|---------|------------------|
| 3.2 | [physics-theory](lessons/3.2-physics-theory.md) | [3.2](rules-pending/3.2.md) | PhysX scope; hull naming; отдельный `physics-system.mdc` |
| 3.3 | [physics-ecs](lessons/3.3-physics-ecs.md) | [3.3](rules-pending/3.3.md) | Ability в Hybrid; `enabled=false`; блок в ecs-code-standards |
| 3.4 | [collision-damage](lessons/3.4-collision-damage.md) | [3.4](rules-pending/3.4.md) | `ICollisionAbility` mandat; `CharacterHealth` MB |
| 3.5 | [collision-actions](lessons/3.5-collision-actions.md) | [3.5](rules-pending/3.5.md) | Canonical pattern; LINQ ban; vs 3.4 |

**Slides:** `references/slides/3.2/`

### Модуль 4 — Сериализация

| Урок | Lesson | Pending | Ключевые вопросы |
|------|--------|---------|------------------|
| 4.2 | [scriptable-object](lessons/4.2-serialization-scriptable-object.md) | [4.2](rules-pending/4.2.md) | SO-only tables; Awake vs Start; naming `*Settings` |
| 4.3 | [json](lessons/4.3-serialization-json.md) | [4.3](rules-pending/4.3.md) | JsonUtility vs Newtonsoft; PlayerPrefs; GameSessionState + DI |
| 4.4 | [remote-storage](lessons/4.4-remote-data-storage.md) | [4.4](rules-pending/4.4.md) | Google для indie; server authority save |
| 4.5 | [google-drive](lessons/4.5-google-drive.md) | [4.5](rules-pending/4.5.md) | git URL пакета; dev-only remote |

**Slides:** `references/slides/4.4/`

### Модуль 5 — AI

| Урок | Lesson | Pending |
|------|--------|---------|
| 5.2 | [ai-intro](lessons/5.2-ai-intro.md) | [5.2](rules-pending/5.2.md) |
| 5.3 | [ai-algorithms](lessons/5.3-ai-algorithms.md) | [5.3](rules-pending/5.3.md) |
| 5.4 | [utility-system](lessons/5.4-utility-system.md) | [5.4](rules-pending/5.4.md) |
| 5.5 | [ai-asset-store](lessons/5.5-ai-asset-store.md) | [5.5](rules-pending/5.5.md) |

**Slides:** `references/slides/5.2/`, `5.3/`

### Модуль 6 — Паттерны / DI

| Урок | Lesson | Pending | Ключевые вопросы |
|------|--------|---------|------------------|
| 6.2 | [patterns-intro](lessons/6.2-design-patterns-intro.md) | [6.2](rules-pending/6.2.md) | — |
| 6.3 | [patterns-overview](lessons/6.3-design-patterns-overview.md) | [6.3](rules-pending/6.3.md) | — |
| 6.4 | [dependency-injection](lessons/6.4-dependency-injection.md) | [6.4](rules-pending/6.4.md) | Extenject scope; ECS vs ITickable; Zenject factory |

**Slides:** `references/slides/6.2/`, `6.3/`

### Модуль 7 — Графика

| Урок | Lesson | Pending | Ключевые вопросы |
|------|--------|---------|------------------|
| 7.2 | [graphics-pipeline](lessons/7.2-graphics-pipeline.md) | [7.2](rules-pending/7.2.md) | Terminology; `graphics-memory.mdc` |
| 7.3 | [materials](lessons/7.3-graphics-materials.md) | [7.3](rules-pending/7.3.md) | URP Lit only; texture suffixes |
| 7.4 | [shader-graph](lessons/7.4-shader-graph.md) | [7.4](rules-pending/7.4.md) | **URP в vision.md** |
| 7.5 | [vfx-particles](lessons/7.5-vfx-particles.md) | [7.5](rules-pending/7.5.md) | VFX Graph scope; pooling ADR |

**Slides:** `references/slides/7.2/`, `7.3/`

### Модуль 8 — Освещение

| Урок | Lesson | Pending | Ключевые вопросы |
|------|--------|---------|------------------|
| 8.2 | [lighting-theory](lessons/8.2-lighting-theory.md) | [8.2](rules-pending/8.2.md) | Shadowmask default; Built-in deprecated |
| 8.3 | [lighting-window](lessons/8.3-lighting-window.md) | [8.3](rules-pending/8.3.md) | Auto-generate lighting; probe placement |
| 8.4 | [rendering-pipelines](lessons/8.4-rendering-pipelines.md) | [8.4](rules-pending/8.4.md) | SSAO tier; second URP Renderer |

**Slides:** `references/slides/8.2/`

### Модуль 9 — Анимация

| Урок | Lesson | Pending |
|------|--------|---------|
| 9.2 | [animation-builtin](lessons/9.2-animation-builtin.md) | [9.2](rules-pending/9.2.md) |
| 9.3 | [animator-api](lessons/9.3-animator-api.md) | [9.3](rules-pending/9.3.md) |
| 9.4 | [procedural-animation](lessons/9.4-procedural-animation.md) | [9.4](rules-pending/9.4.md) |
| 9.5 | [advanced-animation](lessons/9.5-advanced-animation.md) | [9.5](rules-pending/9.5.md) |

### Модуль 10 — Async

| Урок | Lesson | Pending | Ключевые вопросы |
|------|--------|---------|------------------|
| 10.2 | [async-theory](lessons/10.2-async-theory.md) | [10.2](rules-pending/10.2.md) | Coroutines для legacy; UniRx adopt |
| 10.3 | [async-jobs](lessons/10.3-async-jobs.md) | [10.3](rules-pending/10.3.md) | `async void` on UI; Allocator policy |
| 10.4 | [reactive-unirx](lessons/10.4-reactive-unirx.md) | [10.4](rules-pending/10.4.md) | UniRx in `_Project` |

### Модуль 11 — Editor

| Урок | Lesson | Pending | Ключевые вопросы |
|------|--------|---------|------------------|
| 11.2 | [editor-overview](lessons/11.2-editor-overview.md) | [11.2](rules-pending/11.2.md) | — |
| 11.3 | [custom-inspectors](lessons/11.3-custom-inspectors.md) | [11.3](rules-pending/11.3.md) | — |
| 11.4 | [odin-inspector](lessons/11.4-odin-inspector.md) | [11.4](rules-pending/11.4.md) | Odin optional vs mandatory |

### Модуль 12 — Meta / UI

| Урок | Lesson | Pending | Ключевые вопросы |
|------|--------|---------|------------------|
| 12.2 | [level-up](lessons/12.2-level-up-system.md) | [12.2](rules-pending/12.2.md) | — |
| 12.3 | [inventory](lessons/12.3-inventory-system.md) | [12.3](rules-pending/12.3.md) | — |
| 12.4 | [crafting](lessons/12.4-crafting-system.md) | [12.4](rules-pending/12.4.md) | — |
| 12.5 | [ui-mvvm](lessons/12.5-ui-game-state-mvvm.md) | [12.5](rules-pending/12.5.md) | Unity Weld; Find ban; ViewModel scope |

### Модуль 13 — Networking

| Урок | Lesson | Pending | Ключевые вопросы |
|------|--------|---------|------------------|
| 13.2 | [theory](lessons/13.2-networking-theory.md) | [13.2](rules-pending/13.2.md) | — |
| 13.3 | [low-level](lessons/13.3-networking-low-level.md) | [13.3](rules-pending/13.3.md) | — |
| 13.4 | [real-world](lessons/13.4-networking-real-world.md) | [13.4](rules-pending/13.4.md) | — |
| 13.5 | [photon-cloud](lessons/13.5-photon-cloud.md) | [13.5](rules-pending/13.5.md) | PUN2 stack; AppId gitignore; Fusion migration |

**Slides:** `references/slides/13.2/` … `13.4/`

### Модуль 14 — Optimization

| Урок | Lesson | Pending |
|------|--------|---------|
| 14.2 | [profiler](lessons/14.2-profiler.md) | [14.2](rules-pending/14.2.md) |
| 14.3 | [cpu-gc](lessons/14.3-optimization-cpu-gc.md) | [14.3](rules-pending/14.3.md) |
| 14.4 | [ui-perf](lessons/14.4-optimization-ui.md) | [14.4](rules-pending/14.4.md) |

**Slides:** `references/slides/14.3/`, `14.4/`

### Модуль 15 — Monetization

| Урок | Lesson | Pending | Примечание |
|------|--------|---------|------------|
| 15.2 | — | — | **VTT нет** — только слайды `15.2/` |
| 15.3 | [analytics](lessons/15.3-user-analytics.md) | [15.3](rules-pending/15.3.md) | |
| 15.4 | [iap](lessons/15.4-in-app-purchases.md) | [15.4](rules-pending/15.4.md) | |

**Slides:** `references/slides/15.2/`, `15.3/`

### Модули 16–18

| Модуль | Уроки | Slides |
|--------|-------|--------|
| 16 Audio | [16.2](lessons/16.2-audio-theory.md) … [16.4](lessons/16.4-wwise-editor.md) | — |
| 17 Build | [17.2](lessons/17.2-sdk-integration.md) … [17.5](lessons/17.5-store-publishing.md) | — |
| 18 Testing | [18.2](lessons/18.2-testing-theory.md) … [18.4](lessons/18.4-test-automation.md) | `18.2/` |

Полный индекс: [`video-index.md`](video-index.md)

---

## Целевые rule-файлы (ещё не в `.mdc`)

| Файл | Модули-источники |
|------|------------------|
| `dots-ecs.mdc` | 1.x |
| `input-system.mdc`, `player-system.mdc` | 2.x |
| `physics-system.mdc`, `gameplay-interactions.mdc` | 3.x, 2.5 |
| `save-data.mdc` | 4.x |
| `gameplay-ai.mdc` | 5.x |
| `architecture-patterns.mdc` | 6.x |
| `graphics.mdc`, `shaders.mdc`, `vfx.mdc`, `graphics-lighting.mdc` | 7.x, 8.x |
| `animation-system.mdc` | 9.x |
| `async-concurrency.mdc` | 10.x |
| `editor-extensions.mdc` | 11.x |
| `gameplay-meta.mdc` | 12.x |
| `networking.mdc` | 13.x |
| `optimization.mdc` | 14.x |
| `monetization.mdc` | 15.x |
| `audio-system.mdc` | 16.x |
| `build-release.mdc` | 17.x |
| `testing.mdc` | 18.x |

---

## Рекомендуемый порядок ревью

1. **Стратегические A–L** (этот файл) → `vision.md`
2. **Модуль 1** (ECS foundation) — уже детально разобран
3. **Модули 2–3** (input + physics) — напрямую в gameplay
4. **Модуль 6** (DI) — до meta/save rules
5. **Модули 4, 12** (data + UI)
6. Остальные по приоритету курса

После approve: *«запиши одобренные rules 1.2»* или *«запиши все одобренные из pending»*.
