# GameDevStudio — технический аудит и план работ

Дата аудита: 2026-09-02  
Unity: `6000.3.21f1`  
Статус: исходная точка для последующей работы AI-агентов

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

## P0 — стабилизация

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

### MOB-1. Корректно восстанавливать игру после application resume

Файл: `Assets/Scripts/Core/GameLoop.cs:107`

`OnApplicationPause(true)` устанавливает `_backgrounded = true`, но
`OnApplicationPause(false)` не сбрасывает состояние. Сброс зависит от отдельного
`OnApplicationFocus(true)`.

Требуемое решение:

- хранить отдельные флаги pause и focus;
- останавливать симуляцию при `pausedByOs || lostFocus`;
- добавить PlayMode/manual smoke-проверку pause/resume.

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

## P1 — корректность симуляции

### SIM-2. Хранить время в минутах

Файл: `Assets/Scripts/Simulation/StudioSimulation.cs:708`

`State.Hour += minutes / 60` использует целочисленное деление. Значения
`minutesPerTick`, отличные от кратных 60, рассинхронизируют календарь, needs и
прогресс работы.

Решение: хранить абсолютные игровые минуты или `minuteOfDay`; переход суток и
UI рассчитывать из них.

### SIM-3. Не снимать прошлый пиратский штраф легализацией перед релизом

Файл: `Assets/Scripts/Simulation/StudioSimulation.cs:2043`

`Project.UsedPirate` хранит только флаг, а `pirateCut` вычисляется по текущему
состоянию лицензий. Легализация перед релизом снимает штраф за уже выполненную
на пиратском ПО работу.

Решение: фиксировать exposure и штраф в проекте при фактическом использовании.

### SIM-4. Разделить пороги энергии и настроения

Файл: `Assets/Scripts/Simulation/Productivity.cs:54`

`employee.Mood` сравнивается с `needs.lowEnergyThreshold`.

Решение: добавить `lowMoodThreshold` или использовать отдельный явно
документированный mood-порог; добавить параметризованные тесты.

### DATA-1. Условия инцидентов должны быть fail-closed

Файл: `Assets/Scripts/Simulation/StudioSimulation.cs:1034`

Неизвестное условие заканчивается `return true`, а числовые параметры
разбираются через `int.Parse`/`float.Parse`.

Решение:

- неизвестные условия считать ошибкой конфигурации;
- использовать `TryParse`;
- компилировать строки условий в типизированные predicates при загрузке.

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

### UX-1. Исправить подсказку о свободных рабочих местах

Файл: `Assets/Scripts/Simulation/StudioSimulation.cs:1622`

`freeDesks` считает любой стол с оборудованием, включая занятый.

Решение: учитывать `OccupiedByEmployeeId == 0` и совместимость оборудования с
ролью сотрудника.

### Экономика отрицательного баланса

Файл: `Assets/Scripts/Simulation/StudioSimulation.cs:747`

Зарплаты могут увести деньги ниже нуля. Это может быть осознанной механикой,
поэтому до изменения необходимо определить продуктовое правило:

- разрешённый долг;
- банкротство/game over;
- лимит долга;
- временная блокировка расходов.

После решения централизовать расходы через `TrySpend`.

## P2 — архитектура и производительность

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

### ARCH-3. Убрать `GameEvents` из доменного слоя

Файлы:

- `Assets/Scripts/Simulation/StudioSimulation.cs`
- `Assets/Scripts/Core/GameEvents.cs`

Симуляция напрямую публикует глобальные UI-события, поэтому headless-тесты
получают side effects, а каждый tick инициирует широкое обновление UI.

Решение: возвращать domain events/command results либо внедрить
`ISimulationEvents`; адаптер `GameEvents` оставить в composition root.

### PERF-1. Управлять жизненным циклом runtime sprites

Файлы:

- `Assets/Scripts/Presentation/PixelArtFactory.cs:273`
- `Assets/Scripts/Presentation/OfficeView.cs`

`Draw` создаёт `Texture2D` и `Sprite`; созданные native-ресурсы явно не
освобождаются. `StatusPip` создаётся отдельно для каждого сотрудника.

Решение: общий sprite cache/repository и явное освобождение ресурсов либо
импортированные assets/atlas.

### PERF-2. Ограничить число тиков за кадр

Файл: `Assets/Scripts/Core/GameLoop.cs:93`

Цикл обработки `_accumulator` не имеет `maxTicksPerFrame`.

Решение: лимит 5–10 тиков, определённая политика временного долга и
диагностическая метрика dropped ticks.

### PERF-3. Не обновлять весь HUD на каждый tick

Файлы:

- `Assets/Scripts/Simulation/StudioSimulation.cs:728`
- `Assets/Scripts/UI/StudioHud.cs:394`

Каждый tick вызывает `StateChanged`; HUD обходит проекты, треки и повторно
вычисляет `PreviewQuality` для готовых проектов.

Решение: dirty flags/domain events, кэш quality report и ограничение частоты
визуального refresh.

## P3 — сборка и зависимости

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

### STRIP-1. Убрать blanket preserve

Файл: `Assets/link.xml`

Сейчас весь `Assembly-CSharp` помечен `preserve="all"`.

После smoke-теста IL2CPP заменить на точечные правила только для типов,
действительно используемых через reflection/serialization.

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

## Технический долг низкого приоритета

- `energyRestorePerIdleHour` не используется;
- `StudioState.LastMessage` не используется;
- `StudioHud.Release` не используется;
- отсутствуют `.asmdef`;
- runtime принудительно выбирает Low quality независимо от platform defaults;
- analytics включена — перед публикацией проверить privacy requirements;
- шаблонная сцена содержит только Camera и Global Light, bootstrap выполняется
  через `RuntimeInitializeOnLoadMethod`; это следует описать в README.

## Не подтверждено и требует ручной проверки

1. Освещение runtime-спрайтов в URP 2D: у `Global Light 2D`
   `m_ApplyToSortingLayers: 00000000`. Проверить визуально в Play Mode перед
   изменением материалов или sorting layers.
2. Реальная Android IL2CPP build.
3. Реальная WebGL build и загрузка `StreamingAssets/GameData.json`.
4. Pause/resume на физическом Android-устройстве.
5. Работа safe area и модальных окон на разных aspect ratios.

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

## Базовые команды проверки

Независимая C#-компиляция:

```powershell
dotnet build .\Assembly-CSharp.csproj --no-restore --nologo
dotnet build .\Assembly-CSharp-Editor.csproj --no-restore --nologo
```

Эти `.csproj` генерируются Unity и не должны редактироваться вручную. Итоговая
проверка всегда должна включать Unity Console, EditMode/PlayMode tests и
целевую player build.
