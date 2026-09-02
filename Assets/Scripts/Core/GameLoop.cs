using System.Collections;
using GameDevStudio.Config;
using GameDevStudio.Presentation;
using GameDevStudio.Simulation;
using GameDevStudio.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace GameDevStudio.Core
{
    public sealed class GameLoop : MonoBehaviour
    {
        StudioSimulation _sim;
        StudioHud _hud;
        float _accumulator;
        bool _ready;
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
            EnsureEventSystem();
            SetupCamera();

            GameDataDto data = null;
            string error = null;
            yield return ConfigLoader.Load(loaded => data = loaded, message => error = message);
            if (data == null)
            {
                Debug.LogError(error ?? "Config load failed");
                yield break;
            }

            _sim = new StudioSimulation(data);
            var officeGo = new GameObject("Office");
            officeGo.AddComponent<OfficeView>().Bind(_sim);
            _hud = gameObject.AddComponent<StudioHud>();
            _hud.Bind(_sim);
            GameEvents.RaiseToast("Кликайте по людям, столам, кофе и туалету. Нужды сами гоняют сотрудников по офису.");
            _ready = true;
        }

        void Update()
        {
            if (!_ready)
            {
                PlaybackSpeed = 0f;
                return;
            }

            if (_sim.State.PendingIncident != null || _hud.Paused)
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
