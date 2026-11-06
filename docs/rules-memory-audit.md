# Аудит «памяти» — `docs/lessons/*.md`

> Снимок для наглядности. Метрика: **~токены ≈ байты ÷ 3** (смесь RU/EN/код, погрешность ±15–20%).
>
> **Выводы** — строки таблицы `## Выводы (на одобрение)` (fat rules с tier, обоснованием, подвохами).
>
> Обновлено: 2026-06-30

---

## Итого (64 файла)

| Метрика | Значение |
|---------|----------|
| Файлов | 64 (62 `extracted` + 2 шаблона `pending`) |
| Байт | 338 852 |
| ~Токенов | **112 950** |
| Строк «Выводы» | 577 |
| CODE STANDARDS (таблица) | 227 |
| Строк «Противоречия» | 82 |
| Вопросов на approve | 199 |
| Средний урок (`extracted`) | ~5 450 байт ≈ **~1 820 токенов** |
| Средний 1 вывод (fat) | ~590 байт ≈ **~195 токенов** |

### Сравнение слоёв (тот же курс)

| Слой | Файлов | ~Токенов |
|------|--------|----------|
| `docs/lessons/*.md` | 64 | **112 950** |
| `docs/rules-pending/*.md` | 62 | **37 100** |
| `.cursor/rules/*.mdc` | 6 | **5 500** |
| **Все три слоя** | — | **~155 550** |

---

## Полная таблица — 64 lesson-файла

Порядок: шаблоны → модули 1–18. Ссылки ведут в `docs/lessons/`.

| # | Урок | Статус | Байт | ~Токенов | Выводов | CS | Противор. | Вопросы | pending байт | pending ~ток | pending пунктов |
|---|------|--------|------|----------|---------|-----|-----------|---------|--------------|------------|-----------------|
| 1 | [01-project-setup](lessons/01-project-setup.md) | `pending` | 1 297 | 432 | 0 | 0 | 0 | 0 | — | — | — |
| 2 | [02-player-movement](lessons/02-player-movement.md) | `pending` | 1 296 | 432 | 0 | 0 | 0 | 0 | — | — | — |
| 3 | [1.2-ecs-vs-oop](lessons/1.2-ecs-vs-oop.md) | `extracted` | 9 851 | 3 284 | 17 | 0 | 0 | 3 | 3 961 | 1 320 | 16 |
| 4 | [1.3-pure-hybrid](lessons/1.3-pure-hybrid.md) | `extracted` | 8 355 | 2 785 | 14 | 0 | 0 | 3 | 3 158 | 1 053 | 14 |
| 5 | [1.4-practical-ecs](lessons/1.4-practical-ecs.md) | `extracted` | 7 183 | 2 394 | 14 | 0 | 0 | 3 | 2 624 | 875 | 13 |
| 6 | [2.2-input-types](lessons/2.2-input-types.md) | `extracted` | 3 969 | 1 323 | 7 | 0 | 0 | 3 | 759 | 253 | 7 |
| 7 | [2.3-input-system-setup](lessons/2.3-input-system-setup.md) | `extracted` | 6 790 | 2 263 | 10 | 5 | 2 | 3 | 2 369 | 790 | 14 |
| 8 | [2.4-input-movement](lessons/2.4-input-movement.md) | `extracted` | 10 085 | 3 362 | 12 | 5 | 4 | 4 | 2 652 | 884 | 17 |
| 9 | [2.5-input-shooting](lessons/2.5-input-shooting.md) | `extracted` | 10 365 | 3 455 | 12 | 6 | 4 | 4 | 2 820 | 940 | 18 |
| 10 | [3.2-physics-theory](lessons/3.2-physics-theory.md) | `extracted` | 6 168 | 2 056 | 10 | 3 | 2 | 3 | 2 315 | 772 | 13 |
| 11 | [3.3-physics-ecs](lessons/3.3-physics-ecs.md) | `extracted` | 5 927 | 1 976 | 11 | 6 | 3 | 3 | 2 417 | 806 | 17 |
| 12 | [3.4-collision-damage](lessons/3.4-collision-damage.md) | `extracted` | 5 148 | 1 716 | 10 | 4 | 2 | 3 | 2 162 | 721 | 14 |
| 13 | [3.5-collision-actions](lessons/3.5-collision-actions.md) | `extracted` | 6 204 | 2 068 | 12 | 6 | 2 | 4 | 2 745 | 915 | 18 |
| 14 | [4.2-serialization-scriptable-object](lessons/4.2-serialization-scriptable-object.md) | `extracted` | 5 297 | 1 766 | 8 | 4 | 2 | 3 | 1 857 | 619 | 12 |
| 15 | [4.3-serialization-json](lessons/4.3-serialization-json.md) | `extracted` | 5 648 | 1 883 | 10 | 4 | 2 | 3 | 1 986 | 662 | 14 |
| 16 | [4.4-remote-data-storage](lessons/4.4-remote-data-storage.md) | `extracted` | 5 138 | 1 713 | 8 | 0 | 1 | 3 | 1 460 | 487 | 8 |
| 17 | [4.5-google-drive](lessons/4.5-google-drive.md) | `extracted` | 4 568 | 1 523 | 8 | 4 | 1 | 3 | 1 615 | 538 | 12 |
| 18 | [5.2-ai-intro](lessons/5.2-ai-intro.md) | `extracted` | 4 601 | 1 534 | 8 | 0 | 1 | 3 | 1 369 | 456 | 8 |
| 19 | [5.3-ai-algorithms](lessons/5.3-ai-algorithms.md) | `extracted` | 4 731 | 1 577 | 10 | 0 | 1 | 3 | 1 496 | 499 | 10 |
| 20 | [5.4-utility-system](lessons/5.4-utility-system.md) | `extracted` | 5 784 | 1 928 | 11 | 6 | 2 | 3 | 1 969 | 656 | 17 |
| 21 | [5.5-ai-asset-store](lessons/5.5-ai-asset-store.md) | `extracted` | 4 045 | 1 348 | 8 | 0 | 1 | 3 | 1 255 | 418 | 8 |
| 22 | [6.2-design-patterns-intro](lessons/6.2-design-patterns-intro.md) | `extracted` | 4 448 | 1 483 | 8 | 0 | 1 | 2 | 1 570 | 523 | 8 |
| 23 | [6.3-design-patterns-overview](lessons/6.3-design-patterns-overview.md) | `extracted` | 7 166 | 2 389 | 12 | 10 | 3 | 3 | 2 371 | 790 | 23 |
| 24 | [6.4-dependency-injection](lessons/6.4-dependency-injection.md) | `extracted` | 5 856 | 1 952 | 11 | 5 | 3 | 3 | 1 978 | 659 | 16 |
| 25 | [7.2-graphics-pipeline](lessons/7.2-graphics-pipeline.md) | `extracted` | 5 349 | 1 783 | 8 | 4 | 1 | 2 | 1 967 | 656 | 13 |
| 26 | [7.3-graphics-materials](lessons/7.3-graphics-materials.md) | `extracted` | 5 587 | 1 862 | 9 | 4 | 1 | 2 | 1 994 | 665 | 11 |
| 27 | [7.4-shader-graph](lessons/7.4-shader-graph.md) | `extracted` | 5 486 | 1 829 | 10 | 5 | 1 | 2 | 1 836 | 612 | 11 |
| 28 | [7.5-vfx-particles](lessons/7.5-vfx-particles.md) | `extracted` | 5 406 | 1 802 | 12 | 4 | 0 | 2 | 1 915 | 638 | 14 |
| 29 | [8.2-lighting-theory](lessons/8.2-lighting-theory.md) | `extracted` | 5 481 | 1 827 | 10 | 5 | 1 | 2 | 1 668 | 556 | 12 |
| 30 | [8.3-lighting-window](lessons/8.3-lighting-window.md) | `extracted` | 5 188 | 1 729 | 10 | 4 | 1 | 2 | 1 647 | 549 | 11 |
| 31 | [8.4-rendering-pipelines](lessons/8.4-rendering-pipelines.md) | `extracted` | 5 931 | 1 977 | 10 | 5 | 1 | 2 | 1 746 | 582 | 11 |
| 32 | [9.2-animation-builtin](lessons/9.2-animation-builtin.md) | `extracted` | 5 231 | 1 744 | 9 | 4 | 1 | 2 | 1 424 | 475 | 10 |
| 33 | [9.3-animator-api](lessons/9.3-animator-api.md) | `extracted` | 5 117 | 1 706 | 9 | 5 | 1 | 2 | 1 528 | 509 | 10 |
| 34 | [9.4-procedural-animation](lessons/9.4-procedural-animation.md) | `extracted` | 4 666 | 1 555 | 8 | 4 | 1 | 2 | 1 267 | 422 | 9 |
| 35 | [9.5-advanced-animation](lessons/9.5-advanced-animation.md) | `extracted` | 3 974 | 1 325 | 7 | 2 | 0 | 2 | 1 287 | 429 | 8 |
| 36 | [10.2-async-theory](lessons/10.2-async-theory.md) | `extracted` | 4 628 | 1 543 | 8 | 5 | 1 | 2 | 1 550 | 517 | 9 |
| 37 | [10.3-async-jobs](lessons/10.3-async-jobs.md) | `extracted` | 5 726 | 1 909 | 11 | 6 | 1 | 2 | 1 642 | 547 | 12 |
| 38 | [10.4-reactive-unirx](lessons/10.4-reactive-unirx.md) | `extracted` | 5 670 | 1 890 | 9 | 5 | 1 | 2 | 1 249 | 416 | 10 |
| 39 | [11.2-editor-overview](lessons/11.2-editor-overview.md) | `extracted` | 4 405 | 1 468 | 5 | 2 | 1 | 3 | 1 463 | 488 | 7 |
| 40 | [11.3-custom-inspectors](lessons/11.3-custom-inspectors.md) | `extracted` | 5 675 | 1 892 | 12 | 11 | 2 | 4 | 1 288 | 429 | 13 |
| 41 | [11.4-odin-inspector](lessons/11.4-odin-inspector.md) | `extracted` | 4 494 | 1 498 | 10 | 6 | 2 | 4 | 1 033 | 344 | 11 |
| 42 | [12.2-level-up-system](lessons/12.2-level-up-system.md) | `extracted` | 5 677 | 1 892 | 8 | 4 | 2 | 4 | 1 887 | 629 | 12 |
| 43 | [12.3-inventory-system](lessons/12.3-inventory-system.md) | `extracted` | 5 246 | 1 749 | 8 | 5 | 2 | 4 | 2 041 | 680 | 13 |
| 44 | [12.4-crafting-system](lessons/12.4-crafting-system.md) | `extracted` | 4 631 | 1 544 | 8 | 4 | 1 | 4 | 1 738 | 579 | 12 |
| 45 | [12.5-ui-game-state-mvvm](lessons/12.5-ui-game-state-mvvm.md) | `extracted` | 5 365 | 1 788 | 8 | 4 | 2 | 4 | 1 661 | 554 | 12 |
| 46 | [13.2-networking-theory](lessons/13.2-networking-theory.md) | `extracted` | 5 899 | 1 966 | 9 | 2 | 1 | 4 | 2 051 | 684 | 12 |
| 47 | [13.3-networking-low-level](lessons/13.3-networking-low-level.md) | `extracted` | 5 259 | 1 753 | 8 | 3 | 1 | 4 | 1 618 | 539 | 11 |
| 48 | [13.4-networking-real-world](lessons/13.4-networking-real-world.md) | `extracted` | 4 918 | 1 639 | 8 | 3 | 1 | 4 | 1 473 | 491 | 11 |
| 49 | [13.5-photon-cloud](lessons/13.5-photon-cloud.md) | `extracted` | 5 895 | 1 965 | 12 | 7 | 2 | 4 | 1 576 | 525 | 13 |
| 50 | [14.2-profiler](lessons/14.2-profiler.md) | `extracted` | 4 539 | 1 513 | 8 | 3 | 1 | 4 | 1 693 | 564 | 11 |
| 51 | [14.3-optimization-cpu-gc](lessons/14.3-optimization-cpu-gc.md) | `extracted` | 4 687 | 1 562 | 8 | 3 | 1 | 4 | 1 625 | 542 | 11 |
| 52 | [14.4-optimization-ui](lessons/14.4-optimization-ui.md) | `extracted` | 5 070 | 1 690 | 8 | 3 | 2 | 4 | 1 729 | 576 | 11 |
| 53 | [15.3-user-analytics](lessons/15.3-user-analytics.md) | `extracted` | 5 035 | 1 678 | 8 | 3 | 1 | 4 | 1 612 | 537 | 11 |
| 54 | [15.4-in-app-purchases](lessons/15.4-in-app-purchases.md) | `extracted` | 4 581 | 1 527 | 8 | 4 | 1 | 4 | 1 745 | 582 | 12 |
| 55 | [16.2-audio-theory](lessons/16.2-audio-theory.md) | `extracted` | 4 456 | 1 485 | 8 | 3 | 1 | 4 | 1 619 | 540 | 12 |
| 56 | [16.3-wwise-integration](lessons/16.3-wwise-integration.md) | `extracted` | 4 598 | 1 533 | 8 | 3 | 1 | 4 | 1 536 | 512 | 11 |
| 57 | [16.4-wwise-editor](lessons/16.4-wwise-editor.md) | `extracted` | 4 493 | 1 498 | 8 | 3 | 1 | 4 | 1 530 | 510 | 11 |
| 58 | [17.2-sdk-integration](lessons/17.2-sdk-integration.md) | `extracted` | 4 361 | 1 454 | 8 | 3 | 1 | 4 | 1 583 | 528 | 11 |
| 59 | [17.3-android-build](lessons/17.3-android-build.md) | `extracted` | 4 699 | 1 566 | 8 | 3 | 1 | 4 | 1 493 | 498 | 11 |
| 60 | [17.4-ios-build](lessons/17.4-ios-build.md) | `extracted` | 4 097 | 1 366 | 8 | 3 | 1 | 4 | 1 380 | 460 | 11 |
| 61 | [17.5-store-publishing](lessons/17.5-store-publishing.md) | `extracted` | 4 252 | 1 417 | 8 | 3 | 1 | 4 | 1 550 | 517 | 11 |
| 62 | [18.2-testing-theory](lessons/18.2-testing-theory.md) | `extracted` | 4 569 | 1 523 | 8 | 3 | 1 | 4 | 1 696 | 565 | 13 |
| 63 | [18.3-nunit-setup](lessons/18.3-nunit-setup.md) | `extracted` | 4 157 | 1 386 | 8 | 3 | 1 | 4 | 1 408 | 469 | 11 |
| 64 | [18.4-test-automation](lessons/18.4-test-automation.md) | `extracted` | 4 434 | 1 478 | 8 | 3 | 1 | 4 | 1 567 | 522 | 11 |

---

## Топ-10 по размеру (lesson)

| # | Урок | ~Токенов | Выводов | Примечание |
|---|------|----------|---------|------------|
| 1 | 2.5-input-shooting | 3 455 | 12 | + блоки кода |
| 2 | 2.4-input-movement | 3 362 | 12 | + блоки кода |
| 3 | 1.2-ecs-vs-oop | 3 284 | 17 | максимум выводов |
| 4 | 1.3-pure-hybrid | 2 785 | 14 | |
| 5 | 6.3-design-patterns-overview | 2 389 | 12 | 10 CODE STANDARDS |
| 6 | 1.4-practical-ecs | 2 394 | 14 | |
| 7 | 2.3-input-system-setup | 2 263 | 10 | |
| 8 | 3.2-physics-theory | 2 056 | 10 | |
| 9 | 6.4-dependency-injection | 1 952 | 11 | |
| 10 | 11.3-custom-inspectors | 1 892 | 12 | 11 CODE STANDARDS |

---

## Топ-10 по размеру (rules-pending)

| # | Урок | ~Токенов | Пунктов `- [ ]` |
|---|------|----------|-----------------|
| 1 | 1.2 | 1 320 | 16 |
| 2 | 2.5 | 940 | 18 |
| 3 | 3.5 | 915 | 18 |
| 4 | 2.4 | 884 | 17 |
| 5 | 1.4 | 875 | 13 |
| 6 | 3.3 | 806 | 17 |
| 7 | 6.3 | 790 | 23 |
| 8 | 2.3 | 790 | 14 |
| 9 | 3.2 | 772 | 13 |
| 10 | 13.2 | 684 | 12 |

---

## Как читать таблицу

| Колонка | Смысл |
|---------|--------|
| **Байт / ~Токенов** | Весь файл целиком в контексте LLM |
| **Выводов** | Fat rules — основной «дорогой» слой |
| **CS** | CODE STANDARDS в lesson (отдельная таблица) |
| **Противор.** | Строки в `## ⚠️ Противоречия` |
| **Вопросы** | Пронумерованные вопросы на approve |
| **pending** | Сжатый чеклист `docs/rules-pending/X.Y.md` для того же урока |

**Approve одного урока** ≈ lesson ~токенов + pending ~токенов + ~3 000 (always `.mdc`).

Пример **16.4**: 1 498 + 510 + 3 000 ≈ **5 000 токенов** только rules-слой.
