# Справочник кода со слайдов курса

> **Назначение:** единый файл с точными примерами кода из `references/slides/`, чтобы **не перечитывать слайды заново**.
> **Источники:** OCR `references/slides/*.png` (скрипт `tools/ocr_slides.ps1`) + авторитетные тексты `.cursor/rules/ecs-code-standards.mdc` и `docs/lessons/*.md`.
> **Обновлено:** 2026-08-17.

---

## Результаты OCR по всем слайдам

| Модуль | Слайды | Содержимое | Код? |
|--------|--------|------------|------|
| 1.2 | page-02..05 | Компоненты, системы, job, reflection | ✅ **да — см. ниже** |
| 3.2 | page-01..02 | Физика: дорогие вычисления, коллайдеры, движки | ❌ теория |
| 4.4 | page-01 | Хранение данных (Google, сервер, Photon, cloud) | ❌ теория |
| 5.2 | page-01 | Задачи AI | ❌ теория |
| 5.3 | page-01 | Алгоритмы AI (FSM, BT, Utility...) | ❌ теория |
| 6.2 | page-01 | Паттерны — общее | ❌ теория |
| 6.3 | page-01..12 | Паттерны-диаграммы (MVC, ECS, Command, Observer, FSM, Factory, Pool, Singleton, EventBus, DI, ServiceLocator) | ⚠️ псевдокод (Singleton) |
| 7.2 | page-01 | Графический pipeline | ❌ теория |
| 7.3 | page-01..02 | Модели освещения, типы текстур | ❌ теория |
| 8.2 | page-01..03 | Render pipelines, Lighting Settings, Light modes | ❌ теория |
| 13.2 | page-01 | P2P vs Client-Server | ❌ теория |
| 13.3 | page-01 | Сеть «под капотом» (диаграмма) | ❌ теория |
| 13.4 | page-01 | TCP/UDP, prediction, интерполяция | ❌ теория |
| 14.3 | page-01..02 | Оптимизация (Update, Find, GC, Pool, batching) | ❌ теория |
| 14.4 | page-01..02 | UI-канвасы, атласы, IL2CPP | ❌ теория |
| 15.2 | page-01 | Монетизация | ❌ теория |
| 15.3 | page-01 | Аналитика | ❌ теория |
| 18.2 | page-01..02 | Методы тестирования, TDD | ❌ теория |

**Вывод OCR:** точный паттерн C#-код содержится только на слайдах **1.2** (уроки 1.2–1.3). Остальные слайды — диаграммы, схемы и псевдокод.

---

## 1.2/page-02 — Компоненты: tag и data

```csharp
// Tag — пустой IComponentData
public struct MoveByInputData : IComponentData
{
}

// Data — поля; позиция/скорость — float3 (Unity.Mathematics)
public struct MoveDirectlyData : IComponentData
{
    public float3 Position;
    public float3 Speed;
}
```

**Правила:**
- `public struct`, суффикс `Data`, `: IComponentData`
- поля — `public`
- tag = пустой struct (маркер включения в query)

---

## 1.2/page-03 — System: query в OnCreate + ForEach в OnUpdate (Hybrid)

```csharp
private EntityQuery _query;

protected override void OnCreate()
{
    _query = GetEntityQuery(
        ComponentType.ReadOnly<TimerData>(),
        ComponentType.ReadOnly<TimerComponent>());
}

protected override void OnUpdate()
{
    var dt = (float)Time.DeltaTime;
    Entities.With(_query).ForEach(
        (Entity entity, TimerComponent timer) =>
        {
            for (var i = timer.TimedActions.Count - 1; i >= 0; i--)
            {
                var timerAction = timer.TimedActions[i];
                timerAction.Delay -= dt;
                timer.TimedActions[i] = timerAction;

                if (!(timerAction.Delay <= 0f)) continue;
                timer.TimedActions[i].Act.Invoke();
                timer.TimedActions.RemoveAt(i);
            }
        });
}
```

> **Примечание:** `TimerComponent` на слайде (OCR) — managed (list + `Invoke`), это **Hybrid-слой** (не mass sim; 1.3 → MB bridge).
>
> `GetEntityQuery` вызывается в **`OnCreate`** (в VTT 1.2 речь путает с `Update` — **не копировать**); правильно `OnCreate`.

---

## 1.2/page-04 — System: Job + Burst (Pure)

```csharp
[BurstCompile]
private struct PlayerMovementJob : IJobForEach<PlayerInputData, ActorMovementData>
{
    public void Execute(ref PlayerInputData input, ref ActorMovementData movement)
    {
        movement.Input = MathUtils.RotateVector(input.Move, 0 - input.CompensateAngle);
    }
}

[BurstCompile]
protected override JobHandle OnUpdate(JobHandle inputDeps)
{
    var job = new PlayerMovementJob();
    return job.Schedule(this, inputDeps);
}
```

**Правила:**
- job — `private struct` внутри system
- `[BurstCompile]` на job и на `OnUpdate`
- `Execute` — компоненты через `ref`
- `OnUpdate` → `Schedule(this, inputDeps)`
- `IJobForEach` — **legacy API** курса; для нового кода смотреть Entities версию (1.4)

---

## 1.2/page-05 — Reflection: «так делать нельзя, но иногда приходится»

```csharp
private delegate void SetComponentObjectDelegate(
    EntityManager n, Entity entity, ComponentType componentType, object componentObject);

private static readonly SetComponentObjectDelegate SetComponentObject =
    (SetComponentObjectDelegate)typeof(EntityManager)
        .GetMethod(
            name: "SetComponentObject",
            bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            callConvention: CallingConventions.Any,
            types: new[] { typeof(Entity), typeof(ComponentType), typeof(object) },
            modifiers: null)
        ?.CreateDelegate(typeof(SetComponentObjectDelegate));
```

**Правила:**
- один `static readonly` delegate; reflection **не** в hot path
- изолировать в отдельный adapter, не размножать по проекту
- prefer public API / Hybrid authoring; upgrade Entities = QA block

---

## 1.3 — tag + data + Convert to Entity (общие слайды 1.2)

```csharp
public struct TimerData : IComponentData
{
}
```

- выключить entity из system → снять tag (`TimerData`), data оставить
- Convert to Entity: на gameplay prefab — **Convert and Inject GameObject**; GO остаётся, связан с Entity
- один экземпляр каждого component-типа на entity

---

## 1.4 — Hybrid MB-data компонент (VTT, слайдов нет)

```csharp
public class DogMoveComponent : MonoBehaviour
{
    public float Speed = 1f;
}
```

- suffix **`Component`** для hybrid MB-data
- логика — в `*System`, не в MB

## 1.4 — System (VTT)

```csharp
public class DogMoveSystem : ComponentSystemBase
{
    private EntityQuery _query;

    protected override void OnCreate()
    {
        _query = GetEntityQuery(
            ComponentType.ReadOnly<DogMoveComponent>());
    }

    protected override void OnUpdate()
    {
        Entities.With(_query).ForEach(
            (Entity entity, Transform transform, DogMoveComponent dogMove) =>
            {
                var pos = transform.position;
                pos.y += dogMove.Speed * Time.deltaTime;
                transform.position = pos;
            });
    }
}
```

> Demo двигал `Transform` — для production менять на **ECS transform** (`LocalTransform` / `Translation`), иначе ECS-данные рассинхрон.

---

## 6.3/page-09 — Singleton (псевдокод с диаграммы, НЕ production)

```
public sealed class Singleton
{
    private static Singleton instance = null;
    private Singleton() { }

    public static Singleton Instance
    {
        get
        {
            if (instance == null)
            {
                // thread lock здесь при multithreading
                instance = new Singleton();
            }
            return instance;
        }
    }
}
```

> **Важно:** это **псевдокод** со слайдов паттернов (OCR: `if (instance null) instance = new Singleton();`). Паттерн помечен в rules как **антипаттерн / legacy-only** (см. также Service Locator). НЕ использовать как основной DI.

---

## Как пользоваться этим файлом

1. При работе над ECS-кодом — ссылаться на этот файл **вместо** повторного анализа слайдов.
2. Точные паттерны (1.2) — есть и в `.cursor/rules/ecs-code-standards.mdc`; этот файл — полный свод с перекрёстными ссылками.
3. Если слайды изменятся — пере-прогнать `tools/ocr_slides.ps1` и обновить эту страницу.

---

## Распознавание: известные опечатки OCR

| OCR | Правильный код |
|-----|----------------|
| `public` / `pub1ic` | `public` |
| `f10at3` | `float3` |
| `0nCreate` / `De1taTime` | `OnCreate` / `DeltaTime` |
| `P1ayerMovementJob` | `PlayerMovementJob` |
| `MathUti1s` | `MathUtils` |
| `CompensateAng1e` | `CompensateAngle` |
| `SetComponent0bjectDe1egate` | `SetComponentObjectDelegate` |
| `readon1y` | `readonly` |
| `BindingF1ags` | `BindingFlags` |
| `CaUingConventions` | `CallingConventions` |

Полный сырой OCR-текст: `tools/ocr_output_all.txt`.