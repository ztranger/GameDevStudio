using System.Collections.Generic;
using System.Text;
using GameDevStudio.Config;
using GameDevStudio.Core;
using GameDevStudio.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevStudio.UI
{
    public sealed class StudioHud : MonoBehaviour
    {
        StudioSimulation _sim;
        Text _money;
        Text _clock;
        Text _toast;
        float _toastUntil;
        bool _incidentVisible;
        RectTransform _projectContent;
        readonly Dictionary<int, List<Image>> _trackFills = new Dictionary<int, List<Image>>();
        readonly Dictionary<int, List<Text>> _trackPeople = new Dictionary<int, List<Text>>();
        readonly Dictionary<int, Text> _projectOwners = new Dictionary<int, Text>();
        readonly Dictionary<int, Text> _projectHints = new Dictionary<int, Text>();
        readonly Dictionary<int, Image> _polishFills = new Dictionary<int, Image>();
        readonly Dictionary<int, Text> _qualityLines = new Dictionary<int, Text>();
        string _projectSignature;
        RectTransform _modalRoot;
        RectTransform _inspector;
        Text _inspectorTitle;
        Text _inspectorBody;
        Image _needEnergy;
        Image _needMood;
        Image _needBladder;
        RectTransform _needBlock;
        RectTransform _inspectorActions;
        OfficePick _pick = new OfficePick();
        string _inspectorSignature;
        GenreDto _pendingGenre;
        int _speed = 1;

        public int Speed => _speed;
        public bool Paused { get; private set; }

        public void Bind(StudioSimulation sim)
        {
            _sim = sim;
            Canvas canvas = UiFactory.CreateCanvas(transform);
            BuildTop(canvas.transform);
            BuildBottom(canvas.transform);
            BuildProjects(canvas.transform);
            BuildInspector(canvas.transform);
            var modalGo = new GameObject("Modals", typeof(RectTransform));
            modalGo.transform.SetParent(canvas.transform, false);
            _modalRoot = modalGo.GetComponent<RectTransform>();
            UiFactory.Stretch(_modalRoot);
            GameEvents.StateChanged += Refresh;
            GameEvents.IncidentRaised += OnIncident;
            GameEvents.Toast += ShowToast;
            GameEvents.OfficePicked += OnOfficePicked;
            Refresh();
        }

        void OnDestroy()
        {
            GameEvents.StateChanged -= Refresh;
            GameEvents.IncidentRaised -= OnIncident;
            GameEvents.Toast -= ShowToast;
            GameEvents.OfficePicked -= OnOfficePicked;
        }

        void Update()
        {
            if (_toast != null && Time.unscaledTime > _toastUntil)
            {
                _toast.text = string.Empty;
            }
        }

        void BuildTop(Transform canvas)
        {
            RectTransform bar = UiFactory.Panel(canvas, "Top", UiFactory.PanelColor);
            UiFactory.Anchor(bar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -72f), Vector2.zero);
            _money = UiFactory.Label(bar, "$0", 32, TextAnchor.MiddleLeft, UiFactory.Accent, "Money");
            UiFactory.Anchor(_money.rectTransform, new Vector2(0f, 0f), new Vector2(0.28f, 1f), new Vector2(24f, 0f), Vector2.zero);
            _clock = UiFactory.Label(bar, "День 1  09:00", 28, TextAnchor.MiddleCenter, null, "Clock");
            UiFactory.Anchor(_clock.rectTransform, new Vector2(0.28f, 0f), new Vector2(0.62f, 1f), Vector2.zero, Vector2.zero);
            RectTransform speeds = UiFactory.Panel(bar, "Speeds", Color.clear);
            UiFactory.Anchor(speeds, new Vector2(0.62f, 0.15f), new Vector2(0.99f, 0.85f), Vector2.zero, Vector2.zero);
            var layout = speeds.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(8, 8, 0, 0);
            AddSpeedButton(speeds, "II", () => Paused = !Paused);
            AddSpeedButton(speeds, "1x", () => SetSpeed(1));
            AddSpeedButton(speeds, "2x", () => SetSpeed(2));
            AddSpeedButton(speeds, "4x", () => SetSpeed(4));
        }

        void AddSpeedButton(Transform parent, string caption, UnityEngine.Events.UnityAction action)
        {
            Button button = UiFactory.ButtonWidget(parent, caption, action);
            button.gameObject.AddComponent<LayoutElement>().minWidth = 70f;
        }

        void SetSpeed(int value)
        {
            Paused = false;
            _speed = value;
        }

        void BuildBottom(Transform canvas)
        {
            RectTransform bar = UiFactory.Panel(canvas, "Bottom", UiFactory.PanelColor);
            UiFactory.Anchor(bar, Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 96f));
            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.padding = new RectOffset(20, 20, 16, 16);
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            UiFactory.ButtonWidget(bar, "Начать проект", OpenGenreModal, UiFactory.Accent);
            UiFactory.ButtonWidget(bar, "Команда", () => OpenStaffingModal(0));
            UiFactory.ButtonWidget(bar, "Найм", OpenHireModal);
            UiFactory.ButtonWidget(bar, "Магазин", OpenShopModal);
            _toast = UiFactory.Label(canvas, string.Empty, 22, TextAnchor.LowerCenter, UiFactory.Muted, "Toast");
            UiFactory.Anchor(_toast.rectTransform, new Vector2(0.15f, 0f), new Vector2(0.7f, 0f), new Vector2(0f, 104f), new Vector2(0f, 148f));
        }

        void BuildProjects(Transform canvas)
        {
            RectTransform panel = UiFactory.Panel(canvas, "Projects", UiFactory.PanelColor);
            UiFactory.Anchor(panel, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-430f, 108f), new Vector2(-16f, -84f));
            Text title = UiFactory.Label(panel, "Проекты", 24, TextAnchor.MiddleLeft);
            UiFactory.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -48f), new Vector2(-16f, -8f));
            RectTransform scrollHost = UiFactory.Panel(panel, "Host", Color.clear);
            UiFactory.Anchor(scrollHost, Vector2.zero, Vector2.one, new Vector2(0f, 8f), new Vector2(0f, -48f));
            UiFactory.ScrollColumn(scrollHost, out _projectContent);
        }

        void BuildInspector(Transform canvas)
        {
            _inspector = UiFactory.Panel(canvas, "Inspector", UiFactory.PanelColor);
            UiFactory.Anchor(_inspector, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -520f), new Vector2(360f, -84f));
            _inspector.gameObject.SetActive(false);
            _inspectorTitle = UiFactory.Label(_inspector, string.Empty, 24, TextAnchor.MiddleLeft);
            UiFactory.Anchor(_inspectorTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -52f), new Vector2(-56f, -10f));
            Button close = UiFactory.ButtonWidget(_inspector, "X", ClearPick, UiFactory.Danger);
            UiFactory.Anchor(close.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-52f, -48f), new Vector2(-10f, -10f));
            _inspectorBody = UiFactory.Label(_inspector, string.Empty, 18, TextAnchor.UpperLeft, UiFactory.Muted);
            UiFactory.Anchor(_inspectorBody.rectTransform, new Vector2(0f, 0.42f), new Vector2(1f, 1f), new Vector2(16f, 8f), new Vector2(-16f, -56f));
            _needBlock = UiFactory.Panel(_inspector, "Needs", Color.clear);
            UiFactory.Anchor(_needBlock, new Vector2(0f, 0.28f), new Vector2(1f, 0.42f), new Vector2(16f, 0f), new Vector2(-16f, 0f));
            _needEnergy = MakeNeedRow(_needBlock, 0f, new Color(0.85f, 0.72f, 0.25f), "Энергия");
            _needMood = MakeNeedRow(_needBlock, 0.33f, new Color(0.85f, 0.4f, 0.55f), "Настроение");
            _needBladder = MakeNeedRow(_needBlock, 0.66f, new Color(0.4f, 0.7f, 0.85f), "Туалет");
            _inspectorActions = UiFactory.Panel(_inspector, "Actions", Color.clear);
            UiFactory.Anchor(_inspectorActions, Vector2.zero, new Vector2(1f, 0.28f), new Vector2(10f, 10f), new Vector2(-10f, -8f));
            var layout = _inspectorActions.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.padding = new RectOffset(4, 4, 4, 4);
        }

        Image MakeNeedRow(RectTransform parent, float y, Color color, string caption)
        {
            RectTransform row = UiFactory.Panel(parent, caption, Color.clear);
            UiFactory.Anchor(row, new Vector2(0f, 1f - y - 0.32f), new Vector2(1f, 1f - y), Vector2.zero, Vector2.zero);
            Text label = UiFactory.Label(row, caption, 14, TextAnchor.MiddleLeft);
            UiFactory.Anchor(label.rectTransform, Vector2.zero, new Vector2(0.38f, 1f), Vector2.zero, Vector2.zero);
            RectTransform barHost = UiFactory.Panel(row, "Bar", Color.clear);
            UiFactory.Anchor(barHost, new Vector2(0.38f, 0.2f), Vector2.one, Vector2.zero, Vector2.zero);
            return UiFactory.FillBar(barHost, color);
        }

        void OnOfficePicked(OfficePick pick)
        {
            _pick = pick ?? new OfficePick();
            _inspectorSignature = null;
            RefreshInspector();
        }

        void ClearPick()
        {
            GameEvents.RaiseOfficePicked(new OfficePick());
        }

        void RefreshInspector()
        {
            if (_inspector == null)
            {
                return;
            }

            if (_pick.Kind == OfficePickKind.None)
            {
                _inspector.gameObject.SetActive(false);
                return;
            }

            _inspector.gameObject.SetActive(true);
            string signature = _pick.Kind + ":" + _pick.EmployeeId + ":" + _pick.DeskId + ":" + _pick.FacilityId + ":" + InspectorStateToken();
            Employee employee = _pick.EmployeeId != 0 ? _sim.FindEmployee(_pick.EmployeeId) : null;
            if (_pick.Kind == OfficePickKind.Employee)
            {
                employee = _sim.FindEmployee(_pick.EmployeeId);
            }

            if (employee != null)
            {
                _needBlock.gameObject.SetActive(true);
                float max = Mathf.Max(1f, _sim.Data.needs.maxValue);
                _needEnergy.fillAmount = employee.Energy / max;
                _needMood.fillAmount = employee.Mood / max;
                _needBladder.fillAmount = employee.Bladder / max;
            }
            else
            {
                _needBlock.gameObject.SetActive(false);
            }

            if (signature == _inspectorSignature)
            {
                return;
            }

            _inspectorSignature = signature;
            FillInspectorTexts(employee);
            RebuildInspectorActions(employee);
        }

        string InspectorStateToken()
        {
            if (_pick.Kind == OfficePickKind.Employee)
            {
                Employee employee = _sim.FindEmployee(_pick.EmployeeId);
                return employee == null ? "gone" : employee.Activity + ":" + employee.AssignedProjectId + ":" + employee.DeskId + ":" + employee.ProducerAutoEnabled;
            }

            if (_pick.Kind == OfficePickKind.Desk)
            {
                DeskSlot desk = _sim.FindDesk(_pick.DeskId);
                return desk == null ? "gone" : desk.EquipmentId + ":" + desk.OccupiedByEmployeeId;
            }

            Facility facility = _sim.FindFacility(_pick.FacilityId);
            return facility == null ? "gone" : facility.OccupiedByEmployeeId.ToString();
        }

        void FillInspectorTexts(Employee employee)
        {
            if (_pick.Kind == OfficePickKind.Employee && employee != null)
            {
                RoleDto role = _sim.Data.FindRole(employee.RoleId);
                Project project = _sim.FindProject(employee.AssignedProjectId);
                _inspectorTitle.text = employee.Name;
                _inspectorBody.text = (role != null ? role.displayName : employee.RoleId) +
                                      "  ·  скилл " + employee.Skill + "\n" +
                                      EmployeeAi.ActivityLabel(employee.Activity) + "  ·  " + _sim.AssignmentPhase(employee) + "\n" +
                                      (employee.DeskId == 0 ? "нет стола" : "стол назначен") + "\n" +
                                      (project != null ? "проект: " + project.Name : "без проекта") +
                                      "\nоклад " + employee.SalaryPerDay + "$/день";
                return;
            }

            if (_pick.Kind == OfficePickKind.Desk)
            {
                DeskSlot desk = _sim.FindDesk(_pick.DeskId);
                if (desk == null)
                {
                    _inspectorTitle.text = "Стол";
                    _inspectorBody.text = "Слот исчез.";
                    return;
                }

                EquipmentDto equipment = desk.HasWorkstation ? _sim.Data.FindEquipment(desk.EquipmentId) : null;
                Employee seated = _sim.FindEmployee(desk.OccupiedByEmployeeId);
                _inspectorTitle.text = desk.HasWorkstation ? (equipment != null ? equipment.displayName : "Рабочее место") : "Пустой слот";
                _inspectorBody.text = desk.HasWorkstation
                    ? (seated != null ? "Назначен: " + seated.Name : "Стол свободен. Куплен, ждёт сотрудника.")
                    : "Купите ПК в магазине или прямо отсюда, чтобы посадить человека.";
                return;
            }

            Facility facility = _sim.FindFacility(_pick.FacilityId);
            if (facility == null)
            {
                _inspectorTitle.text = "Объект";
                _inspectorBody.text = string.Empty;
                return;
            }

            Employee user = _sim.FindEmployee(facility.OccupiedByEmployeeId);
            _inspectorTitle.text = facility.DisplayName;
            string need = facility.Need == "energy" ? "энергию" : facility.Need == "bladder" ? "туалет" : "настроение";
            _inspectorBody.text = "Сотрудники сами приходят сюда, когда кончается " + need + ".\n" +
                                  (user != null ? "Сейчас: " + user.Name : "Свободно.");
        }

        void RebuildInspectorActions(Employee employee)
        {
            for (int i = _inspectorActions.childCount - 1; i >= 0; i--)
            {
                Destroy(_inspectorActions.GetChild(i).gameObject);
            }

            if (_pick.Kind == OfficePickKind.Employee && employee != null)
            {
                int empId = employee.Id;
                AddInspectorButton("Снять с проекта", () => Assign(empId, 0));
                for (int p = 0; p < _sim.State.Projects.Count; p++)
                {
                    Project project = _sim.State.Projects[p];
                    if (!CanOfferAssignment(employee, project))
                    {
                        continue;
                    }

                    int projectId = project.Id;
                    string caption = (employee.AssignedProjectId == projectId ? "• " : "") + project.Name;
                    if (project.Status == ProjectStatus.Live)
                    {
                        caption += " (лайв)";
                    }

                    AddInspectorButton(caption, () => Assign(empId, projectId));
                }

                if (employee.RoleId == "producer")
                {
                    bool on = employee.ProducerAutoEnabled;
                    AddInspectorButton(on ? "Выключить автостарт" : "Включить автостарт", () =>
                    {
                        if (!_sim.TrySetProducerAuto(empId, !on, out string error))
                        {
                            ShowToast(error);
                        }
                    });
                }

                return;
            }

            if (_pick.Kind == OfficePickKind.Desk)
            {
                DeskSlot desk = _sim.FindDesk(_pick.DeskId);
                if (desk != null && !desk.HasWorkstation)
                {
                    for (int i = 0; i < _sim.Data.equipment.Length; i++)
                    {
                        EquipmentDto item = _sim.Data.equipment[i];
                        string id = item.id;
                        AddInspectorButton(item.displayName + "  " + item.price + "$", () => BuyEquipment(id));
                    }
                }
            }
        }

        void AddInspectorButton(string caption, UnityEngine.Events.UnityAction action)
        {
            Button button = UiFactory.ButtonWidget(_inspectorActions, caption, action);
            button.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;
        }

        public void Refresh()
        {
            if (_sim == null)
            {
                return;
            }

            StudioState state = _sim.State;
            _money.text = state.Money + "$";
            _clock.text = "День " + state.Day + "   " + state.Hour.ToString("00") + ":00";
            RebuildProjectsIfNeeded();
            UpdateProjectProgress();
            RefreshInspector();
        }

        void RebuildProjectsIfNeeded()
        {
            var signature = new StringBuilder();
            for (int i = 0; i < _sim.State.Projects.Count; i++)
            {
                Project project = _sim.State.Projects[i];
                signature.Append(project.Id).Append(':').Append((int)project.Status).Append(';');
            }

            string value = signature.ToString();
            if (value == _projectSignature)
            {
                return;
            }

            _projectSignature = value;
            _trackFills.Clear();
            _trackPeople.Clear();
            _projectOwners.Clear();
            _projectHints.Clear();
            _polishFills.Clear();
            _qualityLines.Clear();
            for (int i = _projectContent.childCount - 1; i >= 0; i--)
            {
                Destroy(_projectContent.GetChild(i).gameObject);
            }

            for (int i = 0; i < _sim.State.Projects.Count; i++)
            {
                BuildProjectCard(_sim.State.Projects[i]);
            }
        }

        void BuildProjectCard(Project project)
        {
            GenreDto genre = _sim.Data.FindGenre(project.GenreId);
            EngineDto engine = _sim.Data.FindEngine(project.EngineId);
            int extraRows = project.OptionalTracks.Count + (project.Status == ProjectStatus.Ready ? 1 : 0);
            RectTransform card = UiFactory.Panel(_projectContent, "P" + project.Id, UiFactory.PanelInner);
            card.gameObject.AddComponent<LayoutElement>().preferredHeight = 168f + (project.Tracks.Count + extraRows) * 34f +
                (project.Status == ProjectStatus.Ready ? 48f : 12f) +
                (project.Status == ProjectStatus.Live ? 56f : 0f);
            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 6f;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            UiFactory.Label(card, project.Name + "  ·  " + StatusText(project.Status), 20, TextAnchor.MiddleLeft);
            Text owner = UiFactory.Label(card, _sim.ProjectOwnerLabel(project), 16, TextAnchor.MiddleLeft, UiFactory.Muted);
            owner.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
            _projectOwners[project.Id] = owner;
            if (engine != null)
            {
                UiFactory.Label(card, engine.displayName + "  ·  " + TechLine(genre), 16, TextAnchor.MiddleLeft, UiFactory.Muted);
            }

            var fills = new List<Image>();
            var people = new List<Text>();
            AddTrackRows(card, project.Tracks, fills, people, false);
            AddTrackRows(card, project.OptionalTracks, fills, people, true);

            _trackFills[project.Id] = fills;
            _trackPeople[project.Id] = people;
            Text hint = UiFactory.Label(card, string.Empty, 15, TextAnchor.MiddleLeft, UiFactory.Muted, "Hint");
            hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
            _projectHints[project.Id] = hint;

            if (project.Status == ProjectStatus.Ready)
            {
                RectTransform row = UiFactory.Panel(card, "Polish", Color.clear);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
                Text polishName = UiFactory.Label(row, "Полировка", 15, TextAnchor.MiddleLeft);
                UiFactory.Anchor(polishName.rectTransform, Vector2.zero, new Vector2(0.42f, 1f), Vector2.zero, Vector2.zero);
                RectTransform barHost = UiFactory.Panel(row, "BarHost", Color.clear);
                UiFactory.Anchor(barHost, new Vector2(0.42f, 0.15f), Vector2.one, Vector2.zero, Vector2.zero);
                _polishFills[project.Id] = UiFactory.FillBar(barHost, UiFactory.Accent);
            }

            Text quality = UiFactory.Label(card, string.Empty, 15, TextAnchor.MiddleLeft, UiFactory.Muted, "Quality");
            quality.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
            _qualityLines[project.Id] = quality;

            if (project.Status == ProjectStatus.InDev || project.Status == ProjectStatus.Ready || project.Status == ProjectStatus.Live)
            {
                int id = project.Id;
                Button peopleBtn = UiFactory.ButtonWidget(card, "Люди", () => OpenStaffingModal(id));
                peopleBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            }

            if (project.Status == ProjectStatus.Ready)
            {
                int id = project.Id;
                Button release = UiFactory.ButtonWidget(card, "Выпустить в релиз", () => OpenReleaseModal(id), UiFactory.Accent);
                release.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
            }
            else if (project.Status == ProjectStatus.Live)
            {
                string stars = project.Stars > 0
                    ? new string('★', project.Stars) + new string('☆', 5 - project.Stars)
                    : "в сторе";
                UiFactory.Label(card, stars + "  ·  " + project.RevenueDaysLeft + " дн.", 16, TextAnchor.MiddleLeft, UiFactory.Accent);
            }
            else if (project.Status == ProjectStatus.Banned)
            {
                UiFactory.Label(card, "Заблокирован в сторе", 16, TextAnchor.MiddleLeft, UiFactory.Danger);
            }
        }

        void AddTrackRows(RectTransform card, List<WorkTrack> tracks, List<Image> fills, List<Text> people, bool optional)
        {
            for (int t = 0; t < tracks.Count; t++)
            {
                WorkTrack track = tracks[t];
                RoleDto role = _sim.Data.FindRole(track.RoleId);
                RectTransform row = UiFactory.Panel(card, track.RoleId, Color.clear);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
                string caption = role != null ? role.displayName : track.RoleId;
                if (optional)
                {
                    caption += " (опц.)";
                }

                Text name = UiFactory.Label(row, caption, 15, TextAnchor.MiddleLeft);
                UiFactory.Anchor(name.rectTransform, Vector2.zero, new Vector2(0.42f, 1f), Vector2.zero, Vector2.zero);
                people.Add(name);
                RectTransform barHost = UiFactory.Panel(row, "BarHost", Color.clear);
                UiFactory.Anchor(barHost, new Vector2(0.42f, 0.15f), Vector2.one, Vector2.zero, Vector2.zero);
                Color color = Color.white;
                if (role != null)
                {
                    ColorUtility.TryParseHtmlString(role.color, out color);
                }

                if (optional)
                {
                    color.a = 0.85f;
                }

                fills.Add(UiFactory.FillBar(barHost, color));
            }
        }

        void UpdateProjectProgress()
        {
            for (int i = 0; i < _sim.State.Projects.Count; i++)
            {
                Project project = _sim.State.Projects[i];
                if (_trackFills.TryGetValue(project.Id, out List<Image> fills))
                {
                    int idx = 0;
                    idx = ApplyTrackFills(fills, project.Tracks, idx);
                    ApplyTrackFills(fills, project.OptionalTracks, idx);
                }

                if (_trackPeople.TryGetValue(project.Id, out List<Text> people))
                {
                    int idx = 0;
                    idx = ApplyTrackPeople(people, project.Id, project.Tracks, idx, false);
                    ApplyTrackPeople(people, project.Id, project.OptionalTracks, idx, true);
                }

                if (_projectOwners.TryGetValue(project.Id, out Text owner))
                {
                    owner.text = _sim.ProjectOwnerLabel(project);
                }

                if (_polishFills.TryGetValue(project.Id, out Image polish) && _sim.MaxPolish() > 0.001f)
                {
                    polish.fillAmount = Mathf.Clamp01(project.Polish / _sim.MaxPolish());
                }

                if (_projectHints.TryGetValue(project.Id, out Text hint))
                {
                    if (project.Status == ProjectStatus.InDev)
                    {
                        hint.text = _sim.StaffingHint(project);
                    }
                    else if (project.Status == ProjectStatus.Ready)
                    {
                        hint.text = _sim.StaffingHint(project);
                    }
                    else
                    {
                        hint.text = string.Empty;
                    }
                }

                if (_qualityLines.TryGetValue(project.Id, out Text quality))
                {
                    if (project.Status == ProjectStatus.Ready)
                    {
                        QualityReport report = _sim.PreviewQuality(project);
                        quality.text = StarLine(report.Stars) + "  ·  релиз " + report.Payout + "$  ·  лайв ~" + report.DailyEstimate + "$/д";
                    }
                    else if (project.Status == ProjectStatus.Live)
                    {
                        quality.text = (string.IsNullOrEmpty(project.Review) ? string.Empty : project.Review);
                    }
                    else
                    {
                        quality.text = string.Empty;
                    }
                }
            }
        }

        static int ApplyTrackFills(List<Image> fills, List<WorkTrack> tracks, int idx)
        {
            for (int t = 0; t < tracks.Count && idx < fills.Count; t++, idx++)
            {
                fills[idx].fillAmount = Mathf.Clamp01(tracks[t].Normalized);
            }

            return idx;
        }

        int ApplyTrackPeople(List<Text> people, int projectId, List<WorkTrack> tracks, int idx, bool optional)
        {
            for (int t = 0; t < tracks.Count && idx < people.Count; t++, idx++)
            {
                WorkTrack track = tracks[t];
                RoleDto role = _sim.Data.FindRole(track.RoleId);
                string roleName = role != null ? role.displayName : track.RoleId;
                if (optional)
                {
                    roleName += " (опц.)";
                }

                people[idx].text = roleName + " · " + _sim.TrackAssigneeLine(projectId, track.RoleId);
            }

            return idx;
        }

        static string StarLine(int stars)
        {
            stars = Mathf.Clamp(stars, 1, 5);
            return new string('★', stars) + new string('☆', 5 - stars);
        }

        string TechLine(GenreDto genre)
        {
            if (genre == null || genre.techIds == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < genre.techIds.Length; i++)
            {
                TechDto tech = _sim.Data.FindTech(genre.techIds[i]);
                if (tech == null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(tech.displayName);
            }

            return builder.ToString();
        }

        static string StatusText(ProjectStatus status)
        {
            switch (status)
            {
                case ProjectStatus.InDev: return "в разработке";
                case ProjectStatus.Ready: return "готов";
                case ProjectStatus.Live: return "лайв";
                case ProjectStatus.Banned: return "бан";
                default: return status.ToString();
            }
        }

        void OpenStaffingModal(int focusProjectId)
        {
            CloseModals();
            RectTransform modal = MakeModal("Команда и проекты");
            ScrollRect scroll = UiFactory.ScrollColumn(modal, out RectTransform content);
            UiFactory.Anchor(scroll.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(16f, 16f), new Vector2(-16f, -70f));

            UiFactory.Label(content, "Человек может быть только на одном проекте. Смена сбрасывает въезд.", 16, TextAnchor.MiddleLeft, UiFactory.Muted)
                .gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;

            AddStaffingGroup(content, "Свободные", 0, focusProjectId);
            for (int i = 0; i < _sim.State.Projects.Count; i++)
            {
                Project project = _sim.State.Projects[i];
                if (project.Status != ProjectStatus.InDev && project.Status != ProjectStatus.Ready &&
                    project.Status != ProjectStatus.Live)
                {
                    continue;
                }

                string title = project.Name + "  ·  " + _sim.ProjectOwnerLabel(project);
                if (project.Status == ProjectStatus.Live)
                {
                    title += "  ·  лайв";
                }

                if (project.Id == focusProjectId)
                {
                    title = "► " + title;
                }

                AddStaffingGroup(content, title, project.Id, focusProjectId);
            }
        }

        void AddStaffingGroup(Transform content, string title, int projectId, int focusProjectId)
        {
            UiFactory.Label(content, title, 20, TextAnchor.MiddleLeft).gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
            bool any = false;
            for (int i = 0; i < _sim.State.Employees.Count; i++)
            {
                Employee employee = _sim.State.Employees[i];
                if (employee.AssignedProjectId != projectId)
                {
                    continue;
                }

                any = true;
                AddStaffingRow(content, employee, focusProjectId);
            }

            if (!any)
            {
                string empty = projectId == 0 ? "Все на проектах или ещё никого нет." : "Нет людей. Назначьте свободных сюда.";
                UiFactory.Label(content, empty, 16, TextAnchor.MiddleLeft, UiFactory.Muted)
                    .gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
            }
        }

        void AddStaffingRow(Transform content, Employee employee, int focusProjectId)
        {
            RoleDto role = _sim.Data.FindRole(employee.RoleId);
            RectTransform row = UiFactory.Panel(content, employee.Name, UiFactory.PanelInner);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;
            UiFactory.Label(row,
                employee.Name + "  ·  " + (role != null ? role.displayName : employee.RoleId) +
                "  ·  " + _sim.AssignmentPhase(employee) + "\n" + EmployeeAi.ActivityLabel(employee.Activity),
                16, TextAnchor.UpperLeft);
            RectTransform actions = UiFactory.Panel(row, "A", Color.clear);
            UiFactory.Anchor(actions, new Vector2(0f, 0f), new Vector2(1f, 0.45f), new Vector2(8f, 6f), new Vector2(-8f, 0f));
            var layout = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            int empId = employee.Id;
            if (employee.AssignedProjectId != 0)
            {
                UiFactory.ButtonWidget(actions, "Снять", () =>
                {
                    Assign(empId, 0);
                    OpenStaffingModal(focusProjectId);
                });
            }

            for (int p = 0; p < _sim.State.Projects.Count; p++)
            {
                Project project = _sim.State.Projects[p];
                if (!CanOfferAssignment(employee, project))
                {
                    continue;
                }

                if (project.Id == employee.AssignedProjectId)
                {
                    continue;
                }

                int projectId = project.Id;
                UiFactory.ButtonWidget(actions, "→ " + ShortName(project.Name), () =>
                {
                    Assign(empId, projectId);
                    OpenStaffingModal(focusProjectId);
                });
            }
        }

        static string ShortName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length <= 14)
            {
                return name;
            }

            return name.Substring(0, 12) + "…";
        }

        void OpenGenreModal()
        {
            CloseModals();
            RectTransform modal = MakeModal("Жанр проекта");
            ScrollRect scroll = UiFactory.ScrollColumn(modal, out RectTransform content);
            UiFactory.Anchor(scroll.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(16f, 16f), new Vector2(-16f, -70f));

            for (int i = 0; i < _sim.Data.genres.Length; i++)
            {
                GenreDto genre = _sim.Data.genres[i];
                bool unlocked = genre.unlockAfterReleases <= _sim.State.TotalReleases;
                string caption = genre.displayName + "  ·  тир " + genre.tier;
                if (!unlocked)
                {
                    caption += "  (после " + genre.unlockAfterReleases + " релизов)";
                }
                else if (!_sim.HasEngine(genre.engineId))
                {
                    EngineDto engine = _sim.Data.FindEngine(genre.engineId);
                    caption += "  (нужен " + (engine != null ? engine.displayName : genre.engineId) + ")";
                }

                GenreDto captured = genre;
                Button button = UiFactory.ButtonWidget(content, caption, () =>
                {
                    if (!unlocked)
                    {
                        ShowToast("Жанр ещё закрыт");
                        return;
                    }

                    OpenBrief(captured);
                }, unlocked ? UiFactory.ButtonColor : new Color(0.2f, 0.18f, 0.2f, 1f));
                button.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
            }
        }

        void OpenBrief(GenreDto genre)
        {
            _pendingGenre = genre;
            CloseModals();
            RectTransform modal = MakeModal(genre.displayName);
            var body = new StringBuilder();
            EngineDto engine = _sim.Data.FindEngine(genre.engineId);
            OwnedEngine owned = _sim.FindOwnedEngine(genre.engineId);
            body.AppendLine("Движок: " + (engine != null ? engine.displayName : genre.engineId) +
                            (owned == null ? "  — нет лицензии" : owned.Pirated ? "  — пиратская лицензия" : "  — лицензия есть"));
            body.AppendLine("Технологии: " + TechLine(genre));
            body.AppendLine("Релиз: " + genre.basePayout + "$  + " + genre.dailyRevenue + "$/день");
            body.AppendLine();
            body.AppendLine("Нужно сделать:");
            for (int i = 0; i < genre.tracks.Length; i++)
            {
                WorkTrackDto track = genre.tracks[i];
                RoleDto role = _sim.Data.FindRole(track.roleId);
                body.AppendLine("• " + (role != null ? role.displayName : track.roleId) +
                                "  " + track.points + " очков  (скилл ≥ " + track.minSkill + ")");
            }

            if (genre.optionalTracks != null && genre.optionalTracks.Length > 0)
            {
                body.AppendLine();
                body.AppendLine("Опционально (бонус к оценке, релиз не блокирует):");
                for (int i = 0; i < genre.optionalTracks.Length; i++)
                {
                    WorkTrackDto track = genre.optionalTracks[i];
                    RoleDto role = _sim.Data.FindRole(track.roleId);
                    body.AppendLine("• " + (role != null ? role.displayName : track.roleId) +
                                    "  " + track.points + " очков");
                }
            }

            Text text = UiFactory.Label(modal, body.ToString(), 22, TextAnchor.UpperLeft);
            UiFactory.Anchor(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(24f, 80f), new Vector2(-24f, -70f));

            RectTransform actions = UiFactory.Panel(modal, "Actions", Color.clear);
            UiFactory.Anchor(actions, Vector2.zero, new Vector2(1f, 0f), new Vector2(20f, 16f), new Vector2(-20f, 70f));
            var layout = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            UiFactory.ButtonWidget(actions, "Отмена", CloseModals);
            UiFactory.ButtonWidget(actions, "Подтвердить", ConfirmGenre, UiFactory.Accent);
        }

        void ConfirmGenre()
        {
            if (_pendingGenre == null)
            {
                return;
            }

            if (!_sim.TryStartProject(_pendingGenre.id, 0, out string error))
            {
                ShowToast(error);
                return;
            }

            _pendingGenre = null;
            CloseModals();
        }

        void OpenHireModal()
        {
            CloseModals();
            RectTransform modal = MakeModal("Найм и назначения");
            ScrollRect scroll = UiFactory.ScrollColumn(modal, out RectTransform content);
            UiFactory.Anchor(scroll.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(16f, 70f), new Vector2(-16f, -70f));

            UiFactory.Label(content, "Кандидаты", 20, TextAnchor.MiddleLeft).gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
            for (int i = 0; i < _sim.State.HireMarket.Count; i++)
            {
                Candidate candidate = _sim.State.HireMarket[i];
                RoleDto role = _sim.Data.FindRole(candidate.RoleId);
                int id = candidate.Id;
                string line = candidate.Name + "  ·  " + (role != null ? role.displayName : candidate.RoleId) +
                              "  скилл " + candidate.Skill + "  найм " + candidate.HireCost + "$  оклад " + candidate.SalaryPerDay + "$/д";
                Button button = UiFactory.ButtonWidget(content, line, () => Hire(id));
                button.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;
            }

            Button refresh = UiFactory.ButtonWidget(content, "Обновить пул  (−" + _sim.Data.studio.hireRefreshCost + "$)", RefreshMarket);
            refresh.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

            UiFactory.Label(content, "Команда", 20, TextAnchor.MiddleLeft).gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
            if (_sim.State.Employees.Count == 0)
            {
                UiFactory.Label(content, "Пока никого. Наймите программиста и художника.", 18, TextAnchor.MiddleLeft, UiFactory.Muted)
                    .gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            }

            for (int i = 0; i < _sim.State.Employees.Count; i++)
            {
                BuildEmployeeRow(content, _sim.State.Employees[i]);
            }
        }

        void BuildEmployeeRow(Transform parent, Employee employee)
        {
            RoleDto role = _sim.Data.FindRole(employee.RoleId);
            RectTransform row = UiFactory.Panel(parent, employee.Name, UiFactory.PanelInner);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 70f;
            string desk = employee.DeskId == 0 ? "без места" : "за столом";
            string work = employee.AssignedProjectId == 0 ? "свободен" : "проект #" + employee.AssignedProjectId;
            UiFactory.Label(row,
                employee.Name + "  ·  " + (role != null ? role.displayName : employee.RoleId) +
                "  скилл " + employee.Skill + "\n" + desk + "  ·  " + work + "  ·  энергия " + Mathf.RoundToInt(employee.Energy),
                17, TextAnchor.UpperLeft);
            RectTransform actions = UiFactory.Panel(row, "Assign", Color.clear);
            UiFactory.Anchor(actions, new Vector2(0f, 0f), new Vector2(1f, 0.42f), new Vector2(8f, 6f), new Vector2(-8f, 0f));
            var layout = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            int empId = employee.Id;
            Button idle = UiFactory.ButtonWidget(actions, "Снять", () => AssignFromHire(empId, 0));
            idle.gameObject.AddComponent<LayoutElement>();
            for (int p = 0; p < _sim.State.Projects.Count; p++)
            {
                Project project = _sim.State.Projects[p];
                if (!CanOfferAssignment(employee, project))
                {
                    continue;
                }

                int projectId = project.Id;
                Button button = UiFactory.ButtonWidget(actions, "#" + projectId, () => AssignFromHire(empId, projectId));
                button.gameObject.AddComponent<LayoutElement>();
            }
        }

        void OpenShopModal()
        {
            CloseModals();
            RectTransform modal = MakeModal("Оборудование, софт и движки");
            ScrollRect scroll = UiFactory.ScrollColumn(modal, out RectTransform content);
            UiFactory.Anchor(scroll.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(16f, 16f), new Vector2(-16f, -70f));

            UiFactory.Label(content, "Комната  ·  слоты " + _sim.State.Desks.Count + "/" +
                                    (_sim.Data.studio.maxDeskSlots > 0 ? _sim.Data.studio.maxDeskSlots : 6) +
                                    "  ·  ПК " + UsedDesks(), 18, TextAnchor.MiddleLeft)
                .gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
            int slotPrice = _sim.Data.studio.extraDeskSlotPrice > 0 ? _sim.Data.studio.extraDeskSlotPrice : 400;
            Button slotBtn = UiFactory.ButtonWidget(content, "Купить слот стола  ·  " + slotPrice + "$", BuyDeskSlot);
            slotBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 50f;

            UiFactory.Label(content, "Движки", 20, TextAnchor.MiddleLeft)
                .gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
            if (_sim.Data.engines != null)
            {
                for (int i = 0; i < _sim.Data.engines.Length; i++)
                {
                    EngineDto item = _sim.Data.engines[i];
                    OwnedEngine owned = _sim.FindOwnedEngine(item.id);
                    RectTransform row = UiFactory.Panel(content, item.id, Color.clear);
                    row.gameObject.AddComponent<LayoutElement>().preferredHeight = 54f;
                    var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                    layout.spacing = 8f;
                    layout.childForceExpandWidth = true;
                    layout.childForceExpandHeight = true;
                    string status = owned == null ? "нет лицензии" : owned.Pirated ? "пиратская лицензия" : "лицензия есть";
                    UiFactory.Label(row, item.displayName + "\n" + status, 16, TextAnchor.MiddleLeft)
                        .gameObject.AddComponent<LayoutElement>().preferredWidth = 280f;
                    if (owned == null)
                    {
                        string id = item.id;
                        if (item.price > 0)
                        {
                            UiFactory.ButtonWidget(row, "Купить " + item.price + "$", () => AcquireEngine(id, false), UiFactory.Accent);
                            UiFactory.ButtonWidget(row, "Спиратить", () => AcquireEngine(id, true), UiFactory.Danger);
                        }
                        else
                        {
                            UiFactory.ButtonWidget(row, "Взять", () => AcquireEngine(id, false), UiFactory.Accent);
                        }
                    }
                }
            }

            UiFactory.Label(content, "Оборудование в слот", 20, TextAnchor.MiddleLeft)
                .gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
            for (int i = 0; i < _sim.Data.equipment.Length; i++)
            {
                EquipmentDto item = _sim.Data.equipment[i];
                string id = item.id;
                Button button = UiFactory.ButtonWidget(content, item.displayName + "  ·  " + item.price + "$", () => BuyEquipment(id));
                button.gameObject.AddComponent<LayoutElement>().preferredHeight = 50f;
            }

            UiFactory.Label(content, "Софт: купить лицензию или спиратить", 20, TextAnchor.MiddleLeft)
                .gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
            for (int i = 0; i < _sim.Data.software.Length; i++)
            {
                SoftwareDto item = _sim.Data.software[i];
                RoleDto role = _sim.Data.FindRole(item.roleId);
                RectTransform row = UiFactory.Panel(content, item.id, Color.clear);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 54f;
                var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 8f;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = true;
                bool has = _sim.OwnsSoftwareForRole(item.roleId);
                string status = has ? "есть" : (role != null ? role.displayName : item.roleId);
                UiFactory.Label(row, item.displayName + "\n" + status, 16, TextAnchor.MiddleLeft)
                    .gameObject.AddComponent<LayoutElement>().preferredWidth = 280f;
                if (!has)
                {
                    string id = item.id;
                    UiFactory.ButtonWidget(row, "Купить " + item.price + "$", () => AcquireSoftware(id, false), UiFactory.Accent);
                    UiFactory.ButtonWidget(row, "Спиратить", () => AcquireSoftware(id, true), UiFactory.Danger);
                }
            }
        }

        int UsedDesks()
        {
            int used = 0;
            for (int i = 0; i < _sim.State.Desks.Count; i++)
            {
                if (_sim.State.Desks[i].HasWorkstation)
                {
                    used++;
                }
            }

            return used;
        }

        void OnIncident(IncidentLog log)
        {
            ShowIncidentModal(log);
        }

        void ShowIncidentModal(IncidentLog log)
        {
            if (_incidentVisible || log == null)
            {
                return;
            }

            CloseModals();
            _incidentVisible = true;
            IncidentDto dto = _sim.Data.FindIncident(log.Id);
            int choiceCount = dto != null && dto.choices != null && dto.choices.Length > 0 ? dto.choices.Length : 1;
            float actionsHeight = 20f + choiceCount * 58f;
            RectTransform modal = MakeModal("Инцидент: " + log.Title, false, 360f);
            Text body = UiFactory.Label(modal, log.Body, 22, TextAnchor.UpperLeft);
            UiFactory.Anchor(body.rectTransform, Vector2.zero, Vector2.one, new Vector2(24f, 16f + actionsHeight), new Vector2(-24f, -70f));
            RectTransform actions = UiFactory.Panel(modal, "Choices", Color.clear);
            UiFactory.Anchor(actions, Vector2.zero, new Vector2(1f, 0f), new Vector2(20f, 16f), new Vector2(-20f, actionsHeight));
            var layout = actions.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            if (dto != null && dto.choices != null && dto.choices.Length > 0)
            {
                for (int i = 0; i < dto.choices.Length; i++)
                {
                    IncidentChoiceDto choice = dto.choices[i];
                    string id = choice.id;
                    string caption = choice.label;
                    if (!string.IsNullOrEmpty(choice.hint))
                    {
                        caption += "\n" + choice.hint;
                    }

                    Color color = choice.requireMoney ? UiFactory.Accent : UiFactory.ButtonColor;
                    if (i == dto.choices.Length - 1 && dto.choices.Length > 1)
                    {
                        color = UiFactory.ButtonColor;
                    }

                    Button button = UiFactory.ButtonWidget(actions, caption, () => ResolveIncident(id), color);
                    button.gameObject.AddComponent<LayoutElement>().preferredHeight = 50f;
                }
            }
            else
            {
                Button ok = UiFactory.ButtonWidget(actions, "Понятно", () => ResolveIncident(null), UiFactory.Accent);
                ok.gameObject.AddComponent<LayoutElement>().preferredHeight = 50f;
            }
        }

        void ResolveIncident(string choiceId)
        {
            if (!_sim.TryResolveIncident(choiceId, out string error))
            {
                ShowToast(error);
                return;
            }

            CloseModals();
        }

        RectTransform MakeModal(string title, bool allowClose = true, float halfHeight = 320f)
        {
            RectTransform blocker = UiFactory.Panel(_modalRoot, title, new Color(0f, 0f, 0f, 0.55f));
            UiFactory.Stretch(blocker);
            RectTransform window = UiFactory.Panel(blocker, "Window", UiFactory.PanelColor);
            UiFactory.Anchor(window, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-420f, -halfHeight), new Vector2(420f, halfHeight));
            Text header = UiFactory.Label(window, title, 28, TextAnchor.MiddleLeft);
            UiFactory.Anchor(header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -58f), new Vector2(allowClose ? -80f : -20f, -8f));
            if (allowClose)
            {
                Button close = UiFactory.ButtonWidget(window, "X", CloseModals, UiFactory.Danger);
                UiFactory.Anchor(close.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-70f, -54f), new Vector2(-16f, -12f));
            }

            return window;
        }

        void CloseModals()
        {
            bool closedIncident = _incidentVisible;
            _incidentVisible = false;
            for (int i = _modalRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_modalRoot.GetChild(i).gameObject);
            }

            if (closedIncident && _sim != null && _sim.State.PendingIncident != null)
            {
                _sim.AcknowledgeIncident();
            }
        }

        void Hire(int id)
        {
            if (!_sim.TryHire(id, out string error))
            {
                ShowToast(error);
                return;
            }

            OpenHireModal();
        }

        void RefreshMarket()
        {
            if (!_sim.TryRefreshHireMarket(out string error))
            {
                ShowToast(error);
                return;
            }

            OpenHireModal();
        }

        void Assign(int employeeId, int projectId)
        {
            if (!_sim.TryAssign(employeeId, projectId, out string error))
            {
                ShowToast(error);
                return;
            }

            _inspectorSignature = null;
            RefreshInspector();
        }

        void AssignFromHire(int employeeId, int projectId)
        {
            Assign(employeeId, projectId);
            OpenHireModal();
        }

        void BuyDeskSlot()
        {
            if (!_sim.TryBuyDeskSlot(out string error))
            {
                ShowToast(error);
                return;
            }

            OpenShopModal();
        }

        void BuyEquipment(string id)
        {
            int deskId = _pick.Kind == OfficePickKind.Desk ? _pick.DeskId : 0;
            if (!_sim.TryBuyEquipment(id, deskId, out string error))
            {
                ShowToast(error);
                return;
            }

            _inspectorSignature = null;
            RefreshInspector();
        }

        void AcquireSoftware(string id, bool pirate)
        {
            if (!_sim.TryAcquireSoftware(id, pirate, out string error))
            {
                ShowToast(error);
                return;
            }

            OpenShopModal();
        }

        void AcquireEngine(string id, bool pirate)
        {
            if (!_sim.TryAcquireEngine(id, pirate, out string error))
            {
                ShowToast(error);
                return;
            }

            OpenShopModal();
        }

        void OpenReleaseModal(int projectId)
        {
            Project project = _sim.FindProject(projectId);
            if (project == null)
            {
                return;
            }

            QualityReport report = _sim.PreviewQuality(project);
            CloseModals();
            RectTransform modal = MakeModal("Релиз: " + project.Name);
            var body = new StringBuilder();
            body.AppendLine(StarLine(report.Stars));
            body.AppendLine(report.Review);
            body.AppendLine();
            body.AppendLine(report.Breakdown);
            body.AppendLine();
            body.AppendLine("Выплата: +" + report.Payout + "$");
            body.AppendLine("Лайв: ~" + report.DailyEstimate + "$/день");
            if (report.UsedPirate)
            {
                body.AppendLine();
                body.AppendLine("Пиратский софт или движок: оценка ниже, выше шанс бана в сторе.");
            }

            body.AppendLine();
            body.AppendLine("Можно закрыть и ещё полировать, пока люди за столами.");
            Text text = UiFactory.Label(modal, body.ToString(), 20, TextAnchor.UpperLeft);
            UiFactory.Anchor(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(24f, 80f), new Vector2(-24f, -70f));
            RectTransform actions = UiFactory.Panel(modal, "Actions", Color.clear);
            UiFactory.Anchor(actions, Vector2.zero, new Vector2(1f, 0f), new Vector2(20f, 16f), new Vector2(-20f, 70f));
            var layout = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            UiFactory.ButtonWidget(actions, "Ещё полировать", CloseModals);
            UiFactory.ButtonWidget(actions, "Выпустить", () => ConfirmRelease(projectId), UiFactory.Accent);
        }

        void ConfirmRelease(int projectId)
        {
            if (!_sim.TryRelease(projectId, out string error))
            {
                ShowToast(error);
                return;
            }

            CloseModals();
        }

        static bool CanOfferAssignment(Employee employee, Project project)
        {
            if (project.Status == ProjectStatus.InDev || project.Status == ProjectStatus.Ready)
            {
                return true;
            }

            return project.Status == ProjectStatus.Live && employee.RoleId == "marketer";
        }

        void Release(int projectId)
        {
            OpenReleaseModal(projectId);
        }

        void ShowToast(string message)
        {
            if (_toast == null)
            {
                return;
            }

            _toast.text = message;
            _toastUntil = Time.unscaledTime + 3.2f;
        }
    }
}
