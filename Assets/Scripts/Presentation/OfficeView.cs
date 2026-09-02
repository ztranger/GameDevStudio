using System.Collections.Generic;
using GameDevStudio.Config;
using GameDevStudio.Core;
using GameDevStudio.Simulation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GameDevStudio.Presentation
{
    public sealed class OfficeView : MonoBehaviour
    {
        sealed class PersonView
        {
            public int Id;
            public Transform Root;
            public SpriteRenderer Body;
            public SpriteRenderer Marker;
            public SpriteRenderer Pad;
            public Vector3 Visual;
        }

        StudioSimulation _sim;
        Transform _floorRoot;
        Transform _propRoot;
        Transform _deskRoot;
        Transform _peopleRoot;
        Sprite _floor;
        Sprite _wall;
        Sprite _desk;
        Sprite _empty;
        Sprite _pad;
        readonly Dictionary<int, PersonView> _people = new Dictionary<int, PersonView>();
        readonly Dictionary<int, SpriteRenderer> _deskSprites = new Dictionary<int, SpriteRenderer>();
        readonly Dictionary<string, Sprite> _bodySprites = new Dictionary<string, Sprite>();
        OfficePick _pick = new OfficePick();
        int _deskSignature = int.MinValue;

        public void Bind(StudioSimulation sim)
        {
            _sim = sim;
            _floor = PixelArtFactory.FloorTile();
            _wall = PixelArtFactory.Wall();
            _desk = PixelArtFactory.Desk();
            _empty = PixelArtFactory.EmptySlot();
            _pad = PixelArtFactory.SelectionPad();
            _floorRoot = CreateChild("Floor");
            _propRoot = CreateChild("Props");
            _deskRoot = CreateChild("Desks");
            _peopleRoot = CreateChild("People");
            BuildRoom();
            BuildProps();
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.OfficePicked += OnPicked;
            OnStateChanged();
        }

        void OnDestroy()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.OfficePicked -= OnPicked;
        }

        void OnPicked(OfficePick pick)
        {
            _pick = pick ?? new OfficePick();
        }

        Transform CreateChild(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.transform;
        }

        void BuildRoom()
        {
            int w = _sim.Data.studio.roomTilesX;
            int h = _sim.Data.studio.roomTilesY;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isWall = y == h - 1;
                    SpriteRenderer renderer = CreateSprite(isWall ? _wall : _floor, _floorRoot, isWall ? 2 : 0);
                    renderer.transform.position = OfficeGrid.World(w, h, x, y);
                }
            }
        }

        void BuildProps()
        {
            int w = _sim.Data.studio.roomTilesX;
            int h = _sim.Data.studio.roomTilesY;
            OfficeLayoutDto layout = _sim.Data.layout;
            if (layout != null)
            {
                SpriteRenderer door = CreateSprite(PixelArtFactory.Door(), _propRoot, 3);
                door.transform.position = OfficeGrid.World(w, h, layout.doorX, layout.doorY);
            }

            for (int i = 0; i < _sim.State.Facilities.Count; i++)
            {
                Facility facility = _sim.State.Facilities[i];
                Sprite sprite = FacilitySprite(facility.Id);
                SpriteRenderer renderer = CreateSprite(sprite, _propRoot, 5);
                renderer.name = "facility:" + facility.Id;
                renderer.transform.position = OfficeGrid.World(w, h, facility.TileX, facility.TileY);
            }

            if (layout != null && layout.labels != null)
            {
                for (int i = 0; i < layout.labels.Length; i++)
                {
                    FloorLabelDto label = layout.labels[i];
                    var go = new GameObject("label:" + label.text);
                    go.transform.SetParent(_propRoot, false);
                    go.transform.position = OfficeGrid.World(w, h, label.x, label.y) + new Vector3(0f, 0.08f, 0f);
                    var mesh = go.AddComponent<TextMesh>();
                    mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (mesh.font == null)
                    {
                        mesh.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    }

                    mesh.text = label.text;
                    mesh.anchor = TextAnchor.MiddleCenter;
                    mesh.alignment = TextAlignment.Center;
                    mesh.characterSize = 0.09f;
                    mesh.fontSize = 36;
                    mesh.fontStyle = FontStyle.Bold;
                    Color color = new Color(0.55f, 0.52f, 0.5f, 0.85f);
                    if (!string.IsNullOrEmpty(label.color))
                    {
                        ColorUtility.TryParseHtmlString(label.color, out color);
                        color.a = 0.88f;
                    }

                    mesh.color = color;
                    MeshRenderer renderer = go.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.sortingOrder = 4;
                        if (mesh.font != null && mesh.font.material != null)
                        {
                            renderer.sharedMaterial = mesh.font.material;
                        }
                    }
                }
            }
        }

        static Sprite FacilitySprite(string id)
        {
            if (id == "toilet")
            {
                return PixelArtFactory.Toilet();
            }

            if (id == "sofa")
            {
                return PixelArtFactory.Sofa();
            }

            return PixelArtFactory.CoffeeMachine();
        }

        void OnStateChanged()
        {
            if (_sim == null)
            {
                return;
            }

            RefreshDesks();
            SyncPeople();
        }

        void RefreshDesks()
        {
            int signature = 0;
            for (int i = 0; i < _sim.State.Desks.Count; i++)
            {
                DeskSlot desk = _sim.State.Desks[i];
                signature += desk.Id * 13 + (desk.HasWorkstation ? 7 : 1) + desk.TileX + desk.TileY * 17;
            }

            if (signature == _deskSignature)
            {
                return;
            }

            _deskSignature = signature;
            _deskSprites.Clear();
            for (int i = _deskRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_deskRoot.GetChild(i).gameObject);
            }

            int w = _sim.Data.studio.roomTilesX;
            int h = _sim.Data.studio.roomTilesY;
            for (int i = 0; i < _sim.State.Desks.Count; i++)
            {
                DeskSlot desk = _sim.State.Desks[i];
                SpriteRenderer renderer = CreateSprite(desk.HasWorkstation ? _desk : _empty, _deskRoot, 4);
                renderer.name = "desk:" + desk.Id;
                renderer.transform.position = OfficeGrid.World(w, h, desk.TileX, desk.TileY);
                _deskSprites[desk.Id] = renderer;
            }
        }

        void SyncPeople()
        {
            var live = new HashSet<int>();
            for (int i = 0; i < _sim.State.Employees.Count; i++)
            {
                Employee employee = _sim.State.Employees[i];
                live.Add(employee.Id);
                if (!_people.ContainsKey(employee.Id))
                {
                    _people[employee.Id] = CreatePerson(employee);
                }

                PersonView view = _people[employee.Id];
                view.Body.sprite = BodySprite(employee);
                view.Marker.color = MarkerColor(employee.Activity);
            }

            var remove = new List<int>();
            foreach (KeyValuePair<int, PersonView> pair in _people)
            {
                if (!live.Contains(pair.Key))
                {
                    remove.Add(pair.Key);
                }
            }

            for (int i = 0; i < remove.Count; i++)
            {
                Destroy(_people[remove[i]].Root.gameObject);
                _people.Remove(remove[i]);
            }
        }

        PersonView CreatePerson(Employee employee)
        {
            int w = _sim.Data.studio.roomTilesX;
            int h = _sim.Data.studio.roomTilesY;
            var root = new GameObject(employee.Name);
            root.transform.SetParent(_peopleRoot, false);
            Vector3 spawn = OfficeGrid.World(w, h, employee.TileX, employee.TileY);
            root.transform.position = spawn;
            var pad = CreateSprite(_pad, root.transform, 5);
            pad.enabled = false;
            pad.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            var body = CreateSprite(BodySprite(employee), root.transform, 8);
            var marker = CreateSprite(PixelArtFactory.StatusPip(Color.white), root.transform, 12);
            marker.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            return new PersonView
            {
                Id = employee.Id,
                Root = root.transform,
                Body = body,
                Marker = marker,
                Pad = pad,
                Visual = spawn
            };
        }

        void Update()
        {
            if (_sim == null)
            {
                return;
            }

            MovePeople();
            HandleClick();
        }

        void MovePeople()
        {
            int w = _sim.Data.studio.roomTilesX;
            int h = _sim.Data.studio.roomTilesY;
            float speed = Mathf.Max(0.5f, _sim.Data.needs.walkTilesPerSecond) * Mathf.Max(0f, GameLoop.PlaybackSpeed);
            float step = speed * Time.unscaledDeltaTime;
            foreach (KeyValuePair<int, PersonView> pair in _people)
            {
                Employee employee = _sim.FindEmployee(pair.Key);
                if (employee == null)
                {
                    continue;
                }

                PersonView view = pair.Value;
                Vector3 target = OfficeGrid.World(w, h, employee.TileX, employee.TileY);
                view.Visual = Vector3.MoveTowards(view.Visual, target, step);
                view.Root.position = view.Visual;
                float dx = target.x - view.Visual.x;
                if (Mathf.Abs(dx) > 0.04f)
                {
                    Vector3 scale = view.Body.transform.localScale;
                    scale.x = dx < 0f ? -1f : 1f;
                    view.Body.transform.localScale = scale;
                }

                view.Body.sortingOrder = 20 + Mathf.RoundToInt((h - view.Visual.y) * 10f);
                view.Pad.enabled = _pick.Kind == OfficePickKind.Employee && _pick.EmployeeId == employee.Id;
                view.Marker.color = MarkerColor(employee.Activity);
            }
        }

        void HandleClick()
        {
            if (!WasPrimaryClick(out Vector2 screen))
            {
                return;
            }

            if (IsPointerOverUi(screen))
            {
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Vector3 sp = screen;
            sp.z = Mathf.Abs(camera.transform.position.z);
            Vector3 world = camera.ScreenToWorldPoint(sp);
            world.z = 0f;

            OfficePick pick = RayPick(world);
            _pick = pick;
            GameEvents.RaiseOfficePicked(pick);
        }

        OfficePick RayPick(Vector3 world)
        {
            float personRadius = Touchscreen.current != null ? 1.15f : 0.7f;
            float propRadius = Touchscreen.current != null ? 1.25f : 0.85f;
            float best = personRadius;
            Employee bestEmployee = null;
            for (int i = 0; i < _sim.State.Employees.Count; i++)
            {
                Employee employee = _sim.State.Employees[i];
                if (!_people.TryGetValue(employee.Id, out PersonView view))
                {
                    continue;
                }

                float dist = Vector2.Distance(world, view.Visual + new Vector3(0f, 0.5f, 0f));
                if (dist < best)
                {
                    best = dist;
                    bestEmployee = employee;
                }
            }

            if (bestEmployee != null)
            {
                return new OfficePick { Kind = OfficePickKind.Employee, EmployeeId = bestEmployee.Id };
            }

            best = propRadius;
            DeskSlot bestDesk = null;
            for (int i = 0; i < _sim.State.Desks.Count; i++)
            {
                DeskSlot desk = _sim.State.Desks[i];
                if (!_deskSprites.TryGetValue(desk.Id, out SpriteRenderer renderer))
                {
                    continue;
                }

                float dist = Vector2.Distance(world, renderer.transform.position + new Vector3(0f, 0.4f, 0f));
                if (dist < best)
                {
                    best = dist;
                    bestDesk = desk;
                }
            }

            if (bestDesk != null)
            {
                return new OfficePick { Kind = OfficePickKind.Desk, DeskId = bestDesk.Id, EmployeeId = bestDesk.OccupiedByEmployeeId };
            }

            best = propRadius;
            Facility bestFacility = null;
            int w = _sim.Data.studio.roomTilesX;
            int h = _sim.Data.studio.roomTilesY;
            for (int i = 0; i < _sim.State.Facilities.Count; i++)
            {
                Facility facility = _sim.State.Facilities[i];
                Vector3 pos = OfficeGrid.World(w, h, facility.TileX, facility.TileY);
                float dist = Vector2.Distance(world, pos + new Vector3(0f, 0.4f, 0f));
                if (dist < best)
                {
                    best = dist;
                    bestFacility = facility;
                }
            }

            if (bestFacility != null)
            {
                return new OfficePick { Kind = OfficePickKind.Facility, FacilityId = bestFacility.Id, EmployeeId = bestFacility.OccupiedByEmployeeId };
            }

            return new OfficePick();
        }

        static bool WasPrimaryClick(out Vector2 screen)
        {
            screen = Vector2.zero;
            Touchscreen touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                screen = touch.primaryTouch.position.ReadValue();
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screen = Mouse.current.position.ReadValue();
                return true;
            }

            return false;
        }

        static bool IsPointerOverUi(Vector2 screen)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            var data = new PointerEventData(eventSystem) { position = screen };
            var results = new List<RaycastResult>();
            eventSystem.RaycastAll(data, results);
            return results.Count > 0;
        }

        Sprite BodySprite(Employee employee)
        {
            string key = employee.RoleId ?? string.Empty;
            if (_bodySprites.TryGetValue(key, out Sprite sprite))
            {
                return sprite;
            }

            sprite = PixelArtFactory.Character(ParseColor(_sim.Data.FindRole(employee.RoleId)));
            _bodySprites[key] = sprite;
            return sprite;
        }

        static Color MarkerColor(EmployeeActivity activity)
        {
            switch (activity)
            {
                case EmployeeActivity.Working: return new Color(0.35f, 0.75f, 0.45f, 1f);
                case EmployeeActivity.Managing: return new Color(0.85f, 0.75f, 0.25f, 1f);
                case EmployeeActivity.Coffee: return new Color(0.7f, 0.45f, 0.2f, 1f);
                case EmployeeActivity.Toilet: return new Color(0.4f, 0.65f, 0.85f, 1f);
                case EmployeeActivity.Rest: return new Color(0.85f, 0.75f, 0.3f, 1f);
                case EmployeeActivity.Walking: return new Color(0.9f, 0.9f, 0.9f, 1f);
                default: return new Color(0.6f, 0.6f, 0.6f, 1f);
            }
        }

        static Color32 ParseColor(RoleDto role)
        {
            if (role != null && ColorUtility.TryParseHtmlString(role.color, out Color color))
            {
                return color;
            }

            return new Color32(180, 180, 180, 255);
        }

        static SpriteRenderer CreateSprite(Sprite sprite, Transform parent, int order)
        {
            var go = new GameObject(sprite != null ? sprite.name : "Sprite");
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
