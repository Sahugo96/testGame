# Противоречия между rules

> Аудит **62 pending** + **6 `.mdc`** + `docs/architecture.md`.  
> Дата: 2026-07-07. Детали по урокам — в секции «⚠️ Противоречия» каждого `docs/lessons/*.md`.

**Как читать**

| Severity | Смысл |
|----------|--------|
| 🔴 **Blocker** | Нельзя писать `.mdc` без решения — rules будут конфликтовать |
| 🟠 **ADR** | Нужно зафиксировать в `vision.md` / `architecture.md` или выбрать ветку |
| 🟡 **Demo vs prod** | Курс показывает упрощение; rule уже помечен, нужно не смешивать слои |
| 🟢 **Снято** | Уже разрешено в pending или lesson doc |

---

## Сводка

| Severity | Кол-во | Действие |
|----------|--------|----------|
| 🔴 Blocker | **9** | Решить в approve (см. A–I ниже) |
| 🟠 ADR | **14** | Зафиксировать до массовой записи rules |
| 🟡 Demo vs prod | **8** | Пометить в `architecture.md` → Deprecated |
| 🟢 Снято | **6** | Только cross-ref при записи |

---

## 🔴 Blockers — решить первыми

### A. ECS: strict struct vs Hybrid default

| Источник | Rule | Утверждает |
|----------|------|------------|
| 1.2 pending | **1.2-02** 🔴 | `IComponentData` — только данные; логика в System |
| 1.3 pending | **1.3-01** 🔴 | **Hybrid ECS** — default архитектуры |
| 1.3 pending | **1.3-03** 🔴 | MB на entity — только bridge/authoring |
| ecs-code-standards | slide-03 | Managed `TimerComponent` в ForEach |

**Суть:** формулировка 1.2 звучит как «всё struct», 1.3 вводит MB-data.

**Рекомендация:** при записи `dots-ecs.mdc` — **1.3 побеждает**: struct = simulation data; MB = bridge; managed `*Component` — исключение hybrid, не mass sim.  
**Статус:** 🟢 снято комментарием 1.3-03, но текст 1.2-02 нужно смягчить при записи.

---

### B. Transform / LocalTransform в systems

| Источник | Rule | Утверждает |
|----------|------|------------|
| ecs-code-standards 1.4 | demo | ForEach двигает `Transform` |
| 1.4 pending | **1.4-08** | Не читать `Transform` в systems (prod) |
| 2.4, 3.3, 9.3 | demo + pending | Transform / Animator на GO в system path |

**Суть:** код курса ≠ production rule.

**Рекомендация:** в `ecs-code-standards.mdc` — секция **«Demo debt»**; в `dots-ecs.mdc` 🔴 — только ECS transform. Hybrid GO — presentation sync layer.  
**Статус:** 🟡 — нужна явная пометка при approve 1.4 + 2.4.

---

### C. Interaction pattern: `IAbility` / `ICollisionAbility` / composition

| Источник | Rule | Утверждает |
|----------|------|------------|
| 2.5 | **2.5-04** 🔴 | `IAbility` + `Execute()` — контракт input |
| 3.4 | **3.4-05** 🟠 | `ICollisionAbility : IAbility` |
| 3.5 | **3.5-01** 🔴 | **Композиция** `CollisionActions`, не subclass |
| 3.4 | **3.4-10** 🟡 | `TrapAbility` subclass — **deprecated** после 3.5 |

**Суть:** 3.4 учит наследование; 3.5 откатывает к композиции.

**Рекомендация:** канон = **3.5** в `gameplay-interactions.mdc`; `ICollisionAbility` оставить только как marker или убрать; `IAbility` — общий контракт (2.5 + 3.5).  
**Approve:** пункт **L** в [`approve-questions.md`](approve-questions.md).

---

### D. `FindObjectOfType` — запрет vs one-shot

| Источник | Rule | Утверждает |
|----------|------|------------|
| `ui-system.mdc` (existing) | 🔴 | **Не** использовать Find в UI runtime |
| 12.5 pending | **12.5-07** 🟡 | Find ViewModel — **max once at startup** |
| `unity-csharp.mdc` | 🟠 | Кешировать Find в Awake/Start |
| 14.3 pending | **14.3-01** 🔴 | No Find in Update |

**Суть:** абсолютный запрет vs «один раз при старте».

**Рекомендация:** обновить `ui-system.mdc`: запрет **per-frame / lazy Find**; допустимо **once in Awake/Start** с комментарием или serialized ref. Prefer inject/DI для ViewModel.  
**Статус:** 🔴 до правки `.mdc`.

---

### E. `[SerializeField] private` vs public Inspector fields курса

| Источник | Rule | Утверждает |
|----------|------|------------|
| `unity-csharp.mdc` | 🔴 | `[SerializeField] private`, не public |
| 1.4, 2.4, 4.2, 4.3, 12.2 | demo / pending | public fields, ShowInInspector (Odin) |

**Суть:** курс = demo style; project rules = stricter.

**Рекомендация:** **project rule побеждает**; в `ecs-code-standards` и lesson docs — «форма курса / reference». Odin `ShowInInspector` — optional слой поверх private.  
**Approve:** пункт **K**.

---

### F. Server authority vs Photon PUN demo

| Источник | Rule | Утверждает |
|----------|------|------------|
| 13.2 | **13.2-06** 🔴 | Authoritative server, clients → input |
| 13.2 | **13.2-05** 🟠 | Client-side physics relay = слабый anti-cheat |
| 13.4 | **13.4-04** 🔴 | Never trust client для outcomes |
| 13.5 | **13.5-06** 🟠 | `PhotonTransformView` sync с client |
| 3.4 | **3.4-07** 🟡 | Client-side damage — demo |

**Суть:** теория требует server auth; PUN2 demo — host/client relay.

**Рекомендация:** PUN2 = **учебный stack**, не prod default; в `networking.mdc` — theory rules 🔴; PUN — appendix с пометкой «не anti-cheat». Для MP gameplay — server validate abilities (13.4 ↔ mod 3).  
**Approve:** пункт **G**.

---

### G. Extenject lifecycle vs ECS tick

| Источник | Rule | Утверждает |
|----------|------|------------|
| 6.4 | **6.4-07** 🟡 | `IInitializable` / `ITickable` |
| 6.4 | **6.4-09** 🟡 | DI scope: meta/settings/save |
| 1.x, 14.3 | ECS systems | Hot path в ECS, не ITickable |
| 6.3 | **6.3-10** 🟠 | DI at composition root |

**Суть:** два «Update loop» — DI tickables и ECS systems.

**Рекомендация:** **разделение слоёв**: simulation = ECS; meta/UI/save = Extenject ITickable. Запретить ITickable для movement/combat/collision.  
**Approve:** пункт **D**.

---

### H. Service Locator vs DI vs ui-system

| Источник | Rule | Утверждает |
|----------|------|------------|
| 6.3 | **6.3-11** 🟠 | Service Locator — антипаттерн |
| 6.4 | Extenject | inject, не Resolve() |
| `ui-system.mdc` | 🟠 | «…или задокументированный **service locator**» |

**Суть:** ui-system допускает SL; patterns module запрещает.

**Рекомендация:** убрать SL из `ui-system.mdc` при merge 12.5; заменить на **interface + DI** или serialized presenter ref.  
**Статус:** 🔴 при записи ui rules.

---

### I. Instantiate / Pool / Zenject factory

| Источник | Rule | Утверждает |
|----------|------|------------|
| 14.3 | **14.3-06**–**07** 🟠 | Pool; anti Instantiate/Destroy hot path |
| 6.4 | **6.4-08** 🟠 | Prefabs через **Zenject factory** |
| 2.5, 12.3 | demo | raw `Instantiate` для bullets/pickup |
| 14.4 | **14.4-04** 🟠 | `Graphic.enabled` vs SetActive (UI pool conflict) |

**Суть:** три паттерна spawn без иерархии приоритетов.

**Рекомендация:** таблица в `architecture-patterns.mdc`:

| Контекст | Spawn |
|----------|-------|
| DI-managed prefabs | Zenject factory |
| High-frequency gameplay (VFX, bullets) | Object pool |
| UI elements | Pool или enable/disable (`Graphic.enabled`) |
| Course demo | raw Instantiate OK с tech-debt tag |

---

## 🟠 ADR — зафиксировать в vision / architecture

| # | Тема | Конфликт | Уроки | Решение (senior default) |
|---|------|----------|-------|--------------------------|
| 1 | **Render pipeline** | Built-in demo → URP prod | 7.3 Specular vs 7.4 Metallic; 7.4-01 | URP only в prod; legacy maps → migration note |
| 2 | **Input stack** | Legacy vs NIS | 2.2 vs vision TBD | NIS only (approve **B**) |
| 3 | **Pathfinding** | NavMesh vs A* asset | 5.2 vs 5.5 | ADR per project; default NavMesh + Utility |
| 4 | **Analytics** | AppMetrica vs Unity Analytics | 15.3 vs 15.4 IAP auto-enable | Один primary provider в vision |
| 5 | **Audio middleware** | Unity Audio vs Wwise | 16.2 vs 16.3–16.4 | Wwise для prod курса; `AkAudioListener` replaces `AudioListener` |
| 6 | **UI binding** | Unity Weld vs events | 12.5 vs `ui-system.mdc` | Events + INotifyPropertyChanged; Weld optional |
| 7 | **Remote save** | Google Sheets vs prod backend | 4.4–4.5 vs architecture TBD | Google = dev-only; prod ADR |
| 8 | **Item IDs** | Name strings vs SO ids | 12.4 craft vs 4.2 SO | Unified item registry ADR |
| 9 | **Event wiring** | EventBus vs local events | 6.3-09 vs 3.5 inspector vs UI | Local events default; global bus — meta only |
| 10 | **CustomEditor vs Odin** | Must-know vs don't write | 11.3 🔴 vs 11.4 | Vanilla CustomEditor обязателен знать; Odin optional |
| 11 | **Naming** | `*System/*Data` vs `Controller/Service` | 1.2 vs architecture.md | Dual naming table в architecture |
| 12 | **Tests location** | `_Project/Tests/` | 18.3 vs architecture | Добавить `Scripts/Tests/` + asmdef в architecture |
| 13 | **SDK / Plugins** | Vendor in Plugins/ vs `_Project` | 17.2 vs architecture | Plugins = vendor; bootstrap in Core |
| 14 | **Dynamic Batching** | Course on vs docs de-emphasize | 8.4-07 | URP: SRP Batcher first; Dynamic — profile-guided |

---

## 🟡 Demo vs production — не смешивать в `.mdc`

| Паттерн курса | Где | Production rule |
|---------------|-----|-----------------|
| `Transform` в ForEach | 1.4, 2.4, 3.3 | `LocalTransform` / sync layer |
| `public` Inspector fields | 1.4, 4.x, 12.x | `[SerializeField] private` |
| `GetComponent<CharacterHealth>()` | 3.4 | ECS health / events |
| `TrapAbility : CollisionAbility` | 3.4 | 3.5 composition |
| Evaluate utility AI every frame | 5.4 demo | 5.4-07 human delay |
| Resources folder (PUN prefabs) | 13.5 | Addressables + ADR |
| Codeless IAP | 15.4 | `IStoreListener` + server receipt |
| Client-side collision abilities | 3.4 + MP | Server validate (13.4) |

**Действие:** секция **Deprecated** в `docs/architecture.md` — заполнить при первой записи rules mod 1–3.

---

## 🟢 Уже согласовано (cross-ref при записи)

| Пара | Статус |
|------|--------|
| 1.2 Jobs/Burst ↔ 10.2–10.3 | Согласовано |
| 1.3 Hybrid ↔ 1.2-02 | Снято #1.3-03 |
| 2.2 NIS ↔ 2.3 setup | Согласовано |
| 7.2 SRP ↔ 7.4 URP ↔ 8.2 | Согласовано |
| 3.2 PhysX hybrid ↔ 1.3 Hybrid | Согласовано (разные слои) |
| 1.2-04 system state ↔ slide-03 | Заменено #1.2-04 / #1.2-05 |
| 14.4 MVVM on-change ↔ 12.5 | Reinforce same rule |

---

## Противоречия pending ↔ existing `.mdc`

| Existing | Pending конфликтует | Файлы pending |
|----------|---------------------|---------------|
| `unity-csharp.mdc` SerializeField | public fields, Odin ShowInInspector | 1.4, 2.4, 4.2, 4.3, 11.4, 12.2 |
| `unity-csharp.mdc` cache GetComponent | 14.3-01 duplicate (merge) | 14.3 |
| `ui-system.mdc` no Find | 12.5-07 once OK | 12.5 |
| `ui-system.mdc` events binding | 12.5 Unity Weld MVVM | 12.5 |
| `ui-system.mdc` | 14.4 Graphic.enabled vs SetActive pools | 14.4, 14.3 |
| `player-system.mdc` → lesson 02 | 2.2–2.5 полный input ECS stack | 2.x |
| `ecs-code-standards.mdc` | 14.3-04 mandate ECS for hot path | 14.3 |
| `core-always.mdc` architecture priority | architecture.md TBD (DI, save, naming) | 4.x, 6.4, 12.x |

---

## Рекомендуемый порядок разрешения

```
1. A + B + C  (ECS + interactions)     → dots-ecs, gameplay-interactions, ecs-code-standards
2. D + E + H  (C# style + UI Find)    → unity-csharp, ui-system update
3. G + I      (DI scope + spawn)      → architecture-patterns, architecture.md
4. F          (networking)            → networking.mdc + vision stack
5. ADR 1–14   (vision.md fill)        → unblock graphics, audio, analytics, save
6. Запись .mdc модуль за модулем      → только после [x] approve
```

---

## Быстрые решения для approve (скопировать ответ)

```
A — Hybrid default, struct sim, MB bridge: [x]
B — Demo Transform = debt; prod LocalTransform: [x]
C — IAbility общий; 3.5 composition canonical; 3.4 subclass deprecated: [x]
D — Find только Awake/Start once, не runtime: [x]
E — SerializeField побеждает над demo public: [x]
F — PUN2 учебный; server auth для prod MP: [x]
G — ITickable meta only; gameplay = ECS: [x]
H — Убрать service locator из ui-system: [x]
I — Spawn table (factory / pool / demo): [x]
```

---

## Связанные файлы

- Стратегические решения: [`approve-questions.md`](approve-questions.md) (A–L)
- Чеклисты: [`rules-pending/`](rules-pending/)
- Deprecated patterns: [`architecture.md`](architecture.md) § Deprecated
- Per-lesson детали: `docs/lessons/*.md` → «⚠️ Противоречия»
