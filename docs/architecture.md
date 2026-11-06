# Architecture

> Слои, папки, зависимости. Источник правды для структуры `Assets/`.

## Слои

```
Presentation  →  UI, VFX, Audio triggers
Gameplay      →  Player, enemies, interactions
Systems       →  Inventory, save, events
Core          →  Bootstrap, services, shared utilities
Data          →  ScriptableObjects, configs
```

**Правило зависимостей:** верхние слои могут ссылаться на нижние, не наоборот.

## Структура Assets/

```
Assets/
├── _Project/              # весь код и контент проекта
│   ├── Scenes/
│   ├── Scripts/
│   │   ├── Core/
│   │   ├── Systems/
│   │   ├── Gameplay/
│   │   │   └── Player/
│   │   └── UI/
│   ├── Data/              # ScriptableObjects
│   ├── Prefabs/
│   ├── Art/
│   └── Audio/
└── Plugins/               # сторонние ассеты (если есть)
```

## Паттерны

| Область | Решение | Примечание |
|---------|---------|------------|
| Конфигурация | ScriptableObject | _(уточнить по урокам)_ |
| Связь систем | Events / ScriptableObject events | _(уточнить)_ |
| DI / сервисы | _(ServiceLocator / Zenject / manual)_ | зафиксировать после урока |
| Сохранения | _(PlayerPrefs / JSON / …)_ | TBD |

## Именование

- **Скрипты:** `PascalCase`, суффиксы `Controller`, `Service`, `View`, `SO`
- **Префабы:** `PascalCase`, префикс роли (`Player_`, `UI_`)
- **Сцены:** `SC_` + имя (`SC_MainMenu`, `SC_Game`)

## Deprecated / устаревшее

> Подходы из ранних уроков, которые больше не используем.

- _(пусто — добавлять при конфликтах между уроками)_
