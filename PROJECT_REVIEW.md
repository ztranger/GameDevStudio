# GameDevStudio — технический аудит и план работ

Дата аудита: 2026-09-02  
Unity: `6000.3.21f1`  
Статус: исходная точка для последующей работы AI-агентов

Дата второго прохода: 2026-09-02  
Проверяющий: независимая сверка находок с текущим кодом (не рефакторинг, не фиксы).  
Правило: чинить позже по этому файлу; не поднимать приоритет без повторной проверки в коде.

## Второй проход — вердикт

Аудит точный, приоритеты в целом здравые, паддинга мало. Спот-чек совпал с кодом.
Опираться можно. Не чинить всё списком и не начинать с ARCH/PERF/PKG.

Согласен как с первым пакетом фиксов:

- `MOB-1`, `SIM-4`, `DATA-1`, `INC-1` + каркас EditMode-тестов;
- затем валидатор (`CFG-1`) и модель вклада/пиратства (`SIM-1`, `SIM-3`).

Не согласен с размещением в P0:

- `STATE-1` (сейв) — реальный продуктовый пробел, не крэш и не silent-bug симуляции;
- `TEST-1` целиком как P0 — нужен **каркас** asmdef + тесты на четыре бага выше, не сразу все 9 пунктов.

Деприоритизировать до «симуляция честная»:

- `ARCH-1` / `ARCH-2` (нарезка 2100/1180 строк) — без тестов размажет баги;
- `SIM-2` — сейчас `minutesPerTick` = 60, целочисленное деление часа не ломает календарь;
- `PKG-1`, `STRIP-1`, `ANDROID-1`, CI/build — релизный пайплайн, не геймплей;
- `PERF-1/2/3` — верно, офис крошечный, не оптимизировать первым;
- отрицательные деньги — продуктовое решение, не silent-bug.

Неподтверждённые пункты (URP 2D light, реальные device/WebGL сборки) оставить
неподтверждёнными. Этот файл — техдолг прототипа, не ревью баланса и «весело ли».

## Краткое заключение

Проект является работоспособным вертикальным прототипом Unity-игры. Runtime и
Editor C#-проекты компилируются без ошибок. Поставляемый `GameData.json`
ссылочно целостен, явных секретов в репозитории не обнаружено.

Основные ограничения production-готовности:

1. Нет автоматических тестов и CI.
2. Нет сохранения прогресса.
3. Внешняя конфигурация валидируется недостаточно.
4. Качество релиза зависит от назначенной в последний момент команды, а не от
   фактического вклада сотрудников.
5. `StudioSimulation` и `StudioHud` стали монолитными классами.
6. Android release pipeline не подготовлен.

Второй проход: пункты 1–3 и 6 — production, не блокеры прототипа. Пункты 4–5
верны; 4 чинить в шаге качества, 5 — только после EditMode-сетки. Для «можно
играть сессию не теряя прогресс» не хватает сейва, но это не первая очередь багов.

## Правила для AI-агента

- Перед исправлением повторно проверить актуальность находки в текущем коде.
- Исправлять проблемы небольшими независимыми изменениями.
- Сначала добавлять тест, воспроизводящий дефект, если это практически возможно.
- Не совмещать функциональные исправления с широким рефакторингом.
- Не менять баланс `GameData.json` без отдельного требования.
- После изменения C# проверять компиляцию и Unity Console.
- Для логики симуляции предпочитать EditMode-тесты без запуска сцены.
- Не считать отсутствие keystore или store assets дефектом development-сборки:
  это блокер именно release-процесса.
- Не считать подтверждённым обход модального окна инцидента кликом: текущий
  полноэкранный `Graphic` перехватывает ввод. Опасен сам fallback выбора.
- Не резать `StudioSimulation` / `StudioHud` и не чистить пакеты в том же
  изменении, что чинит поведение.
- Не считать `STATE-1` блокером стабилизации симуляции.
- Компилировать + Unity Console после C#; для sim-логики — EditMode-тест до/вместе с фиксом.

## P0 — стабилизация

> Второй проход: из этого раздела в первую итерацию брать `MOB-1`, `SIM-4`
> (он в P1 ниже), `DATA-1`, `INC-1`. `CFG-1` — сразу после. `STATE-1` сдвинуть
> ниже, когда понадобится сессия между запусками. `TEST-1` — каркас, не весь чеклист.

### CFG-1. Полная валидация `GameData.json`

Файлы:

- `Assets/Scripts/Config/ConfigLoader.cs:52`
- `Assets/Scripts/Config/GameDataDto.cs`

Сейчас после `JsonUtility.FromJson` проверяются только корневой объект,
`genres` и `roles`. Не защищены чтение файла, десериализация, обязательные
секции, диапазоны значений, уникальность ID и ссылки между сущностями.

Риски:

- `NullReferenceException` при отсутствии `studio`, `time`, `needs`, массивов
  имён, оборудования, софта или инцидентов;
- `FormatException` в условиях инцидентов;
- boot coroutine завершается исключением вместо понятной ошибки.

Требуемое решение:

- добавить `GameDataValidator`;
- проверять обязательные секции, диапазоны, ID и ссылочную целостность;
- обернуть I/O и парсинг в `try/catch`;
- возвращать пользователю полный список ошибок конфигурации;
- покрыть валидатор EditMode-тестами.

Второй проход: согласен, валидация сейчас только `data` / `genres` / `roles`
(`ConfigLoader.cs` ~54). Это P0 конфигурации, не P0 рантайма: текущий JSON
ссылочно цел. Делать после четырёх мелких багов, до накопления contribution.

### SIM-1. Фиксировать фактический вклад в качество проекта

Файл: `Assets/Scripts/Simulation/StudioSimulation.cs:2015`

`PreviewQuality` использует сотрудников, назначенных на проект в момент
предпросмотра или релиза. История выполнения `WorkTrack` не сохраняется.

Эксплойт: выполнить проект дешёвой командой, перед релизом назначить сильных
сотрудников и получить повышенное качество.

Требуемое решение:

- накапливать weighted contribution на каждом треке во время `SimulateWork`;
- рассчитывать качество из сохранённого вклада;
- текущую команду учитывать только для будущей работы и live-бонусов.

Второй проход: подтверждено, `PreviewQuality` ~2015. Эксплойт «подставить звёзд
перед релизом» работает. Чинить вместе с `SIM-3`, не в самом первом пакете:
нужны тесты на текущее (неверное) поведение, иначе легко сломать polish/live.

### MOB-1. Корректно восстанавливать игру после application resume

Файл: `Assets/Scripts/Core/GameLoop.cs:107`

`OnApplicationPause(true)` устанавливает `_backgrounded = true`, но
`OnApplicationPause(false)` не сбрасывает состояние. Сброс зависит от отдельного
`OnApplicationFocus(true)`.

Требуемое решение:

- хранить отдельные флаги pause и focus;
- останавливать симуляцию при `pausedByOs || lostFocus`;
- добавить PlayMode/manual smoke-проверку pause/resume.

Второй проход: подтверждено. `OnApplicationPause(true)` ставит `_backgrounded`;
`OnApplicationPause(false)` флаг не снимает (`GameLoop.cs` ~107–113). Сброс
только через `OnApplicationFocus(true)`. На Android это реальный стоп симуляции
после возврата. Первый пакет фиксов.

### TEST-1. Создать защитную сетку тестов

`com.unity.test-framework` установлен, но тестов и test asmdef нет.

Минимальный первый набор EditMode-тестов:

1. `Productivity.Curve`, `SkillFactor`, `NeedsFactor`;
2. `TryHire`: успех и нехватка денег;
3. `TryStartProject`: закрытый жанр и отсутствующий движок;
4. `TryAssign`: роли, live-маркетолог, снятие с проекта;
5. `PreviewQuality`: вклад, optional tracks и пиратские штрафы;
6. условия, эффекты и cooldown инцидентов;
7. календарь, зарплаты и live revenue;
8. загрузка корректного и повреждённого конфига;
9. детерминированность при одинаковом seed.

Рекомендуемая структура:

```text
Assets/
  Scripts/
    GameDevStudio.Runtime.asmdef
  Tests/
    EditMode/
      GameDevStudio.EditModeTests.asmdef
    PlayMode/
      GameDevStudio.PlayModeTests.asmdef
```

Второй проход: asmdef + EditMode — да, в первой итерации. Не закрывать все 9
пунктов до фикса багов: сначала тесты на `NeedsFactor`/`SelectChoice`/
`ConditionMet`/pause-флаг, остальное наращивать рядом с фичами. PlayMode не
нужен, пока нет сейва и UI-регрессий.

### STATE-1. Добавить versioned save/load boundary

Файл: `Assets/Scripts/Simulation/Entities.cs:13`

`StudioState` существует только в памяти. Закрытие приложения или выгрузка
Android/WebGL уничтожает весь прогресс.

Требуемое решение:

- отдельный сериализуемый `SaveGameDto`;
- номер версии формата и миграции;
- mapper между runtime state и save DTO;
- атомарная запись в `Application.persistentDataPath`;
- валидация загруженного состояния;
- тесты round-trip и миграций.

Второй проход: **не P0 стабилизации.** Прогресс реально исчезает при закрытии,
но это не ломает текущую сессию. После честной модели качества/пиратства, если
нужна играемая петля между запусками. Не смешивать с нарезкой классов.

## P1 — корректность симуляции

### SIM-2. Хранить время в минутах

Файл: `Assets/Scripts/Simulation/StudioSimulation.cs:708`

`State.Hour += minutes / 60` использует целочисленное деление. Значения
`minutesPerTick`, отличные от кратных 60, рассинхронизируют календарь, needs и
прогресс работы.

Решение: хранить абсолютные игровые минуты или `minuteOfDay`; переход суток и
UI рассчитывать из них.

Второй проход: код подтверждён (`Hour += minutes / 60`, ~710). Сейчас в JSON
`minutesPerTick: 60` — календарь, needs и работа идут с шагом 1 час (`hours =
minutes / 60f`). Ломается, только если тик станет не кратен 60. Не чинить в
первом пакете; если трогать — вместе с тестом на переход суток.

### SIM-3. Не снимать прошлый пиратский штраф легализацией перед релизом

Файл: `Assets/Scripts/Simulation/StudioSimulation.cs:2043`

`Project.UsedPirate` хранит только флаг, а `pirateCut` вычисляется по текущему
состоянию лицензий. Легализация перед релизом снимает штраф за уже выполненную
на пиратском ПО работу.

Решение: фиксировать exposure и штраф в проекте при фактическом использовании.

Второй проход: согласен. `UsedPirate` — флаг, `pirateCut` считается по текущим
лицензиям (~2043). Легализация перед релизом снимает штраф за уже сделанную
работу. Один пакет с `SIM-1`.

### SIM-4. Разделить пороги энергии и настроения

Файл: `Assets/Scripts/Simulation/Productivity.cs:54`

`employee.Mood` сравнивается с `needs.lowEnergyThreshold`.

Решение: добавить `lowMoodThreshold` или использовать отдельный явно
документированный mood-порог; добавить параметризованные тесты.

Второй проход: подтверждено, `Mood` сравнивается с `needs.lowEnergyThreshold`
(`Productivity.cs:54`). В JSON есть `lowMoodMultiplier`, отдельного mood-порога
нет. Баг, не задумка. Первый пакет; тест на NeedsFactor обязателен. Порог в JSON
менять только если явно попросят баланс.

### DATA-1. Условия инцидентов должны быть fail-closed

Файл: `Assets/Scripts/Simulation/StudioSimulation.cs:1034`

Неизвестное условие заканчивается `return true`, а числовые параметры
разбираются через `int.Parse`/`float.Parse`.

Решение:

- неизвестные условия считать ошибкой конфигурации;
- использовать `TryParse`;
- компилировать строки условий в типизированные predicates при загрузке.

Второй проход: подтверждено, неизвестное условие `return true` (~1034),
`int.Parse`/`float.Parse`. Fail-open опаснее, чем падение загрузчика. Первый
пакет: неизвестное = false + лог/ошибка валидатора, `TryParse`. Компиляция в
predicates — можно отложить, не обязательно в том же PR.

Пустая строка условия сейчас `return true` — для «нет условия» это ок; не
ломать это вместе с неизвестными ключами.

### INC-1. Не выбирать последний исход инцидента по умолчанию

Файлы:

- `Assets/Scripts/Simulation/StudioSimulation.cs:660`
- `Assets/Scripts/UI/StudioHud.cs:1157`

Пустой или неизвестный `choiceId` приводит к выбору последнего элемента
`choices`. `CloseModals` также содержит автоматический `AcknowledgeIncident`.

Решение:

- неизвестный/пустой ID должен возвращать ошибку;
- автоматическое подтверждение допустимо только для инцидента без choices;
- закрытие окна не должно выбирать исход.

Примечание: прямой click-through нижней панели сейчас не подтверждён, так как
полноэкранный blocker модального окна перехватывает raycast.

Второй проход: подтверждено. `SelectChoice` при пустом/неизвестном id возвращает
последний choice (~680). Модалка инцидента: `MakeModal(..., allowClose: false)` —
крестика нет. `HandleBack` при `_incidentVisible` ничего не закрывает.
Опасный путь — `CloseModals` → `AcknowledgeIncident()` (~1157–1169), не
click-through нижней панели. `ShowIncidentModal` сначала зовёт `CloseModals`,
но в этот момент `_incidentVisible` ещё false, так что ложного ack при открытии
нет. Первый пакет: неизвестный id = ошибка, CloseModals не выбирает исход.

### UX-1. Исправить подсказку о свободных рабочих местах

Файл: `Assets/Scripts/Simulation/StudioSimulation.cs:1622`

`freeDesks` считает любой стол с оборудованием, включая занятый.

Решение: учитывать `OccupiedByEmployeeId == 0` и совместимость оборудования с
ролью сотрудника.

Второй проход: подтверждено, считает любой стол с `HasWorkstation` (~1622), без
`OccupiedByEmployeeId == 0`. Баг подсказки, не экономики. Чинить отдельно от
найма/покупки ПК; низкий P1.

### Экономика отрицательного баланса

Файл: `Assets/Scripts/Simulation/StudioSimulation.cs:747`

Зарплаты могут увести деньги ниже нуля. Это может быть осознанной механикой,
поэтому до изменения необходимо определить продуктовое правило:

- разрешённый долг;
- банкротство/game over;
- лимит долга;
- временная блокировка расходов.

После решения централизовать расходы через `TrySpend`.

Второй проход: не баг. Не вводить банкротство «заодно». Когда будет правило —
тогда `TrySpend`. До того не трогать.

## P2 — архитектура и производительность

> Второй проход: не начинать отсюда. Сначала честная симуляция и тесты.

### ARCH-1. Декомпозировать `StudioSimulation`

Файл: `Assets/Scripts/Simulation/StudioSimulation.cs`

Класс занимает около 2 100 строк и управляет календарём, экономикой, наймом,
рабочими местами, лицензиями, проектами, качеством, продюсерами и инцидентами.

Целевое разделение:

- `CalendarSystem`;
- `EconomySystem`;
- `StaffingSystem`;
- `ProjectSystem`;
- `LicenseSystem`;
- `IncidentSystem`;
- общий `SimulationContext` и явные command results.

Второй проход: размер файла верный повод, момент — нет. Резать только после
EditMode на команды и после `ARCH-3` (иначе UI-события расползутся по новым
классам). Не смешивать с фиксом поведения.

### ARCH-2. Разделить `StudioHud`

Файл: `Assets/Scripts/UI/StudioHud.cs`

Около 1 180 строк создают и обновляют весь HUD, магазин, найм, staffing,
инспектор и модальные окна.

Целевое разделение:

- `TopBarView`;
- `ProjectsPanel`;
- `InspectorPanel`;
- `StaffingDialog`;
- `HireDialog`;
- `ShopDialog`;
- `IncidentDialog`;
- view models/presenters без прямого доступа к mutable `StudioState`.

Второй проход: то же. Имена панелей ок как карта, не как обязательный первый
сплит. Convention: в UiFactory не называть члены `Image`/`Panel`/`Button`.

### ARCH-3. Убрать `GameEvents` из доменного слоя

Файлы:

- `Assets/Scripts/Simulation/StudioSimulation.cs`
- `Assets/Scripts/Core/GameEvents.cs`

Симуляция напрямую публикует глобальные UI-события, поэтому headless-тесты
получают side effects, а каждый tick инициирует широкое обновление UI.

Решение: возвращать domain events/command results либо внедрить
`ISimulationEvents`; адаптер `GameEvents` оставить в composition root.

Второй проход: для headless-тестов полезно, но симуляция уже тестируема без сцены,
если не подписываться на `GameEvents`. Делать перед нарезкой классов, не вместо
P0/P1 багов.

### PERF-1. Управлять жизненным циклом runtime sprites

Файлы:

- `Assets/Scripts/Presentation/PixelArtFactory.cs:273`
- `Assets/Scripts/Presentation/OfficeView.cs`

`Draw` создаёт `Texture2D` и `Sprite`; созданные native-ресурсы явно не
освобождаются. `StatusPip` создаётся отдельно для каждого сотрудника.

Решение: общий sprite cache/repository и явное освобождение ресурсов либо
импортированные assets/atlas.

Второй проход: верно технически. Офис крошечный, утечка не доказана как проблема
сейчас. Не в первой очереди.

### PERF-2. Ограничить число тиков за кадр

Файл: `Assets/Scripts/Core/GameLoop.cs:93`

Цикл обработки `_accumulator` не имеет `maxTicksPerFrame`.

Решение: лимит 5–10 тиков, определённая политика временного долга и
диагностическая метрика dropped ticks.

Второй проход: имеет смысл после смены `minutesPerTick` или на слабых устройствах.
Пока тик ≈ 1 игровой час, не трогать.

### PERF-3. Не обновлять весь HUD на каждый tick

Файлы:

- `Assets/Scripts/Simulation/StudioSimulation.cs:728`
- `Assets/Scripts/UI/StudioHud.cs:394`

Каждый tick вызывает `StateChanged`; HUD обходит проекты, треки и повторно
вычисляет `PreviewQuality` для готовых проектов.

Решение: dirty flags/domain events, кэш quality report и ограничение частоты
визуального refresh.

Второй проход: верно (`StateChanged` каждый tick, `PreviewQuality` в HUD).
Оптимизировать после dirty-events/`ARCH-3`, не раньше. Не кэшировать quality,
пока формула ещё врёт (`SIM-1`).

## P3 — сборка и зависимости

> Второй проход: production, не прототип. Не смешивать с фиксами симуляции.

### PKG-1. Удалить неиспользуемые пакеты

Файл: `Packages/manifest.json`

Кандидаты на удаление после проверки:

- `com.unity.ai.assistant` (`2.19.0-pre.2`);
- `com.unity.ai.inference`;
- неиспользуемые 2D Animation/Aseprite/PSD/SpriteShape/Tilemap packages;
- Multiplayer Center;
- Timeline;
- Visual Scripting;
- Collab Proxy.

Независимая компиляция показала конфликты версий `System.Net.Http` и
`System.IO.Compression` в транзитивных AI assemblies, но C#-ошибок нет.

Второй проход: список пакетов в `manifest.json` совпадает. Удалять пакетами,
когда понадобится чистая player-сборка, не «заодно с багом настроения».

### BUILD-1. Сделать build entry point пригодным для CI

Файл: `Assets/Editor/PlatformBuildMenu.cs`

Добавить:

- проверку списка сцен и platform module;
- `BuildOptions.StrictMode`;
- подробный `BuildReport`;
- ненулевой exit code/исключение при ошибке в batchmode;
- документированные команды Android/WebGL build.

### BUILD-2. Проверять активную build platform

Файл: `Assets/Editor/PlatformBuildMenu.cs:24`

Скрипт задаёт `BuildPlayerOptions.target`, но не проверяет активную платформу и
не выполняет её безопасное переключение.

Решение:

- `BuildPipeline.IsBuildTargetSupported`;
- проверка `EditorUserBuildSettings.activeBuildTarget`;
- интерактивное переключение при необходимости;
- обязательный `-buildTarget` в CI.

### ANDROID-1. Создать отдельный release profile

Файл: `ProjectSettings/ProjectSettings.asset`

Текущее состояние:

- custom keystore/key alias не настроены;
- Android launcher icons пусты;
- Target SDK установлен в Auto;
- build menu создаёт APK, а не AAB.

Release profile должен включать:

- AAB;
- signing из защищённых CI variables;
- adaptive и legacy icons;
- фиксированный policy-compatible Target SDK;
- автоматический `versionCode`.

Второй проход: не дефект development-сборки. Меню `Hpg/Build Android APK`
достаточно, пока нет стора.

### STRIP-1. Убрать blanket preserve

Файл: `Assets/link.xml`

Сейчас весь `Assembly-CSharp` помечен `preserve="all"`.

После smoke-теста IL2CPP заменить на точечные правила только для типов,
действительно используемых через reflection/serialization.

Второй проход: `preserve="all"` сейчас правильный костыль до IL2CPP smoke.
Снимать только после реальной Android/WebGL сборки, иначе JsonUtility/DTO
отрежутся первыми.

### INPUT-1. Удалить шаблонный Input Actions либо начать его использовать

Файл: `Assets/InputSystem_Actions.inputactions`

Код читает `Touchscreen`, `Mouse` и `Keyboard` напрямую, а asset содержит
шаблонные actions. После миграции на реальные `Tap`/`Back` actions можно
переключить `activeInputHandler` с Both на новый Input System.

### WebGL

Файлы:

- `Assets/WebGLTemplates/Studio/index.html:96`
- `ProjectSettings/ProjectSettings.asset`

Замечания:

- заменить `bar.innerHTML = message` на `textContent`;
- для production рассмотреть Brotli;
- профилировать память до изменения стартового heap.

Второй проход: `textContent` вместо `innerHTML` — мелкий hygiene, можно при
касании шаблона. Brotli/heap — только после реального WebGL прогона.

## Технический долг низкого приоритета

- `energyRestorePerIdleHour` не используется;
- `StudioState.LastMessage` не используется;
- `StudioHud.Release` не используется;
- отсутствуют `.asmdef`;
- runtime принудительно выбирает Low quality независимо от platform defaults;
- analytics включена — перед публикацией проверить privacy requirements;
- шаблонная сцена содержит только Camera и Global Light, bootstrap выполняется
  через `RuntimeInitializeOnLoadMethod`; это следует описать в README.

Второй проход: неиспользуемые поля подтверждены по именам. Не удалять
`energyRestorePerIdleHour` из JSON «заодно» — может понадобиться для idle-restore.
README про bootstrap — когда будут писать docs, не блокер.

## Не подтверждено и требует ручной проверки

1. Освещение runtime-спрайтов в URP 2D: у `Global Light 2D`
   `m_ApplyToSortingLayers: 00000000`. Проверить визуально в Play Mode перед
   изменением материалов или sorting layers.
2. Реальная Android IL2CPP build.
3. Реальная WebGL build и загрузка `StreamingAssets/GameData.json`.
4. Pause/resume на физическом Android-устройстве.
5. Работа safe area и модальных окон на разных aspect ratios.

Второй проход: оставить как есть. Не чинить sorting layers и heap «на всякий
случай». `MOB-1` всё равно чинить в коде; пункт 4 — проверка на устройстве после фикса.

## Подтверждённые положительные свойства

- Runtime и Editor csproj: `0 errors`.
- Текущий `GameData.json`: уникальные ID и корректные ссылки.
- Unity version и package lock зафиксированы.
- Единственная build scene существует, её GUID совпадает с `.meta`.
- URP asset chain и WebGL custom template существуют.
- Android использует IL2CPP.
- Input System настроен в режиме Both.
- Подписки `StudioHud` и `OfficeView` снимаются в `OnDestroy`.
- `System.Random` и seed позволяют сделать симуляцию детерминированной.
- Секреты, API keys и private keys не обнаружены.
- `.gitignore` исключает основные Unity cache/build artifacts.

## Рекомендуемая последовательность итераций

1. Исправить `MOB-1`, `SIM-4`, `DATA-1` и `INC-1` с тестами.
2. Добавить `GameDataValidator`.
3. Ввести EditMode test assembly и покрыть критические команды симуляции.
4. Исправить модель contribution/quality и piracy exposure.
5. Определить save format и реализовать persistence.
6. Развязать simulation от `GameEvents`.
7. Декомпозировать `StudioSimulation` и `StudioHud`.
8. Добавить CI compile/tests/build smoke.
9. Очистить packages и stripping rules.
10. Подготовить отдельный Android release profile.

Второй проход — скорректированный порядок (чинить позже по нему):

1. `MOB-1`, `SIM-4`, `DATA-1`, `INC-1` + минимальный EditMode asmdef под эти четыре.
2. `GameDataValidator` (`CFG-1`), без переписывания загрузчика.
3. Нарастить EditMode на команды (`TryHire` / `TryStartProject` / `TryAssign`).
4. `SIM-1` + `SIM-3` (вклад и пиратский exposure), тесты на эксплойты.
5. `UX-1`. `SIM-2` только если меняем `minutesPerTick`.
6. Продуктовое правило денег — иначе не трогать. Затем сейв (`STATE-1`), если нужна сессия между запусками.
7. `ARCH-3`, потом нарезка `StudioSimulation` / `StudioHud`.
8. CI / packages / stripping / Android release — когда будет player smoke.

## Базовые команды проверки

Независимая C#-компиляция:

```powershell
dotnet build .\Assembly-CSharp.csproj --no-restore --nologo
dotnet build .\Assembly-CSharp-Editor.csproj --no-restore --nologo
```

Эти `.csproj` генерируются Unity и не должны редактироваться вручную. Итоговая
проверка всегда должна включать Unity Console, EditMode/PlayMode tests и
целевую player build.
