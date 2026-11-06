# CLAUDE.md — точка входа (читать первой)

> Подробная карта: **`docs/MAP.md`** (индекс 62 уроков + индекс типов/API → урок).
> Правило экономии контекста: **не читать целиком** VTT, PDF, PNG, `docs/lessons/*`, `MyGame/*.csproj`, `scripts/_translate_rules_pending.py`. Сначала `docs/MAP.md` → нужная секция → `grep`/`sed -n`.

## Что это за репо

База знаний курса по Unity (ECS/DOTS, 18 модулей) + заготовка Unity-проекта.
**C#-кода в репозитории нет** (0 файлов `.cs`). Основная ценность — документы `docs/` и правила `.cursor/`.

| Факт | Значение |
|------|----------|
| Unity | **6000.3.19** (из define `UNITY_6000_3_19` в `MyGame/*.csproj`) |
| Unity-проект | `MyGame/` — закоммичены только `.sln`/`.csproj`/`obj/*.cache`; **`MyGame/Assets/` в git нет** |
| Asmdef-сборки (по csproj) | `Game.Shared` (`Scripts/Shared/Log.cs`), `Game.Client` → Shared (`Scripts/Client/ClientBootProbe.cs`), `Game.Server` (`Scripts/Server/ServerBootProbe.cs`); + шаблонные `Assembly-CSharp(-Editor)` (TutorialInfo Readme) |
| Корневой `Assets/_Project/Scripts/` | пусто (`.gitkeep`) — целевое место кода по `docs/architecture.md` |
| Язык документов | русский; идентификаторы/API — латиницей |

## Дерево (что где)

```
CLAUDE.md                  ← этот файл
docs/MAP.md                ← детальная карта (уроки, символы, процессы)
docs/vision.md             шаблон vision (почти всё TBD)
docs/architecture.md       слои Presentation→Gameplay→Systems→Core→Data, структура Assets/, именование
docs/video-index.md        каталог курса: урок → slides / lesson / pending / статус (203 стр.)
docs/approve-questions.md  стратегические решения A–L + вопросы по модулям + целевые .mdc (256 стр.)
docs/rules-contradictions.md 9 blockers (A–I), 14 ADR, demo vs prod (278 стр.)
docs/rules-memory-audit.md размеры/токены lesson-файлов (служебное)
docs/slides-code-reference.md код, распознанный со слайдов (OCR) 1.2–1.4, 6.3
docs/lessons/{id}-{slug}.md   разбор урока: Мета, Суть, Выводы(tier), CODE STANDARDS, Противоречия, Вопросы (~70 стр., ~1.8k ток.)
docs/lessons/01-project-setup.md, 02-player-movement.md  — пустые шаблоны
docs/rules-pending/{id}.md чеклисты [ ] правил на одобрение, сгруппированы по tier и целевому .mdc
.cursor/rules/*.mdc        действующие правила (6 файлов, см. ниже)
.cursor/skills/process-lesson/SKILL.md  процесс разбора урока
references/transcripts/*.vtt  транскрипты (1.5 МБ!) — только grep
references/*.pdf, references/slides/{id}/page-NN.png  слайды
references/videos/lesson-01/  пустой шаблон
tools/ocr_slides.ps1 (+ ocr_output_*.txt)  OCR слайдов через Windows OCR
scripts/_translate_rules_pending.py  разовый скрипт замены EN→RU фраз в docs
*.png/*.webp в корне       скриншоты, к проекту не относятся
```

## Действующие правила `.cursor/rules/`

| Файл | Применение | Суть |
|------|-----------|------|
| `core-always.mdc` | всегда | читать vision/architecture; код в `Assets/_Project/`; приоритет architecture над уроком |
| `unity-csharp.mdc` | `**/*.cs` | файл = имя типа; `[SerializeField] private`; кеш GetComponent; CompareTag |
| `ecs-code-standards.mdc` | `**/*.cs` | код ECS из 1.2–1.4: query в `OnCreate`, suffix `Component`/`Data`, не `Transform` в системах |
| `transcript-analysis.mdc` | всегда | разбор VTT: tier на каждый вывод, senior-review, **запись в .mdc только по явной команде** |
| `player-system.mdc` | `Assets/**/Player/**` | код игрока в `Scripts/Gameplay/Player/`; разделять ввод/движение/анимацию |
| `ui-system.mdc` | `Assets/**/UI/**` | UI в `Scripts/UI/`; без gameplay-логики; без `FindObjectOfType` |

Tier: 🔴 критично · 🟠 важно · 🟡 умеренно · 🟢 было бы хорошо.

## Ключевые процессные правила (не нарушать)

1. «Разбери урок/VTT» → только анализ (таблица выводов с tier и «⚠️ Подвохи»), **файлы не трогать**.
2. Писать в `.cursor/rules/*.mdc` — **только** по явной команде («запиши правила …»). «Одобряю» ≠ «запиши».
3. Перед записью — сверить с `docs/rules-contradictions.md` (9 blockers не решены).
4. Конфликт урока с `architecture.md` → прав `architecture.md`, конфликт фиксировать в lesson doc.

## Состояние (на момент составления карты)

- 62 урока `extracted` (x.1 — intro, пропущены; 15.2 — нет VTT). В `.mdc` перенесено только `ecs-code-standards`.
- Стратегические решения A–L в `docs/approve-questions.md` не приняты; `vision.md` не заполнен.

## Быстрый поиск

```bash
grep -rn "ICollisionAbility" docs/            # где описан тип/API
grep -n "^## " docs/lessons/3.5-*.md          # оглавление урока
sed -n '/^## CODE STANDARDS/,/^## Не выносить/p' docs/lessons/2.4-*.md   # только код урока
grep -n "12:3" "references/transcripts/3.4 "*.vtt                       # таймкод в транскрипте
```
