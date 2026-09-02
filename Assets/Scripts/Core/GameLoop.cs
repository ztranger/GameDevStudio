using System.Collections;
using GameDevStudio.Config;
using GameDevStudio.Presentation;
using GameDevStudio.Simulation;
using GameDevStudio.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GameDevStudio.Core
{
    public sealed class GameLoop : MonoBehaviour
    {
        StudioSimulation _sim;
        StudioHud _hud;
        float _accumulator;
        bool _ready;
        bool _backgrounded;
        Text _bootText;
        public static float PlaybackSpeed = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            if (FindAnyObjectByType<GameLoop>() != null)
            {
                return;
            }

            var go = new GameObject("GameLoop");
            DontDestroyOnLoad(go);
            go.AddComponent<GameLoop>();
        }

        void Awake()
        {
            StartCoroutine(Boot());
        }

        IEnumerator Boot()
        {
            ApplyPlatform();
            EnsureEventSystem();
            SetupCamera();
            ShowBoot("Загрузка данных студии…");

            GameDataDto data = null;
            string error = null;
            yield return ConfigLoader.Load(loaded => data = loaded, message => error = message);
            if (data == null)
            {
                ShowBoot(error ?? "Не удалось загрузить GameData.json");
                Debug.LogError(error ?? "Config load failed");
                yield break;
            }

            HideBoot();
            _sim = new StudioSimulation(data);
            var officeGo = new GameObject("Office");
            officeGo.AddComponent<OfficeView>().Bind(_sim);
            _hud = gameObject.AddComponent<StudioHud>();
            _hud.Bind(_sim);
            GameEvents.RaiseToast("На телефоне тапайте по людям и столам. Назад закрывает окна.");
            _ready = true;
        }

        void Update()
        {
            if (WantsBack())
            {
                if (_hud != null)
                {
                    _hud.HandleBack();
                }
            }

            if (!_ready)
            {
                PlaybackSpeed = 0f;
                return;
            }

            if (_backgrounded || _sim.State.PendingIncident != null || _hud.Paused)
            {
                _accumulator = 0f;
                PlaybackSpeed = 0f;
                return;
            }

            PlaybackSpeed = Mathf.Max(1, _hud.Speed);
            _accumulator += Time.unscaledDeltaTime * PlaybackSpeed;
            float step = Mathf.Max(0.05f, _sim.Data.time.realSecondsPerTick);
            while (_accumulator >= step)
            {
                _accumulator -= step;
                _sim.Tick();
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            _backgrounded = !hasFocus;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                _backgrounded = true;
            }
        }

        static bool WantsBack()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return true;
            }

            return Input.GetKeyDown(KeyCode.Escape);
        }

        static void ApplyPlatform()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            QualitySettings.vSyncCount = 0;
            int low = 0;
            string[] names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] == "Low")
                {
                    low = i;
                    break;
                }
            }

            QualitySettings.SetQualityLevel(low, true);
        }

        void ShowBoot(string message)
        {
            if (_bootText == null)
            {
                Canvas canvas = UiFactory.CreateCanvas(transform);
                canvas.sortingOrder = 200;
                canvas.gameObject.name = "BootCanvas";
                RectTransform panel = UiFactory.Panel(canvas.transform, "Boot", new Color(0.09f, 0.07f, 0.08f, 1f));
                UiFactory.Stretch(panel);
                _bootText = UiFactory.Label(panel, message, 28, TextAnchor.MiddleCenter);
            }
            else
            {
                _bootText.text = message;
            }
        }

        void HideBoot()
        {
            if (_bootText == null)
            {
                return;
            }

            Transform canvas = _bootText.canvas != null ? _bootText.canvas.transform : _bootText.transform.root;
            Destroy(canvas.gameObject);
            _bootText = null;
        }

        static void EnsureEventSystem()
        {
            EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
            GameObject go;
            if (eventSystem == null)
            {
                go = new GameObject("EventSystem");
                DontDestroyOnLoad(go);
                eventSystem = go.AddComponent<EventSystem>();
            }
            else
            {
                go = eventSystem.gameObject;
            }

            InputSystemUIInputModule module = go.GetComponent<InputSystemUIInputModule>();
            if (module == null)
            {
                module = go.AddComponent<InputSystemUIInputModule>();
            }

            module.AssignDefaultActions();

            StandaloneInputModule legacy = go.GetComponent<StandaloneInputModule>();
            if (legacy != null)
            {
                legacy.enabled = false;
            }
        }

        static void SetupCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                var go = new GameObject("Main Camera", typeof(Camera));
                go.tag = "MainCamera";
                camera = go.GetComponent<Camera>();
                camera.orthographic = true;
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.07f, 0.08f, 1f);
            camera.allowMSAA = false;
            if (camera.GetComponent<PixelCameraFit>() == null)
            {
                camera.gameObject.AddComponent<PixelCameraFit>();
            }
        }
    }
}
