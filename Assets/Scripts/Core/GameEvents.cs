using System;
using GameDevStudio.Simulation;

namespace GameDevStudio.Core
{
    public static class GameEvents
    {
        public static event Action StateChanged;
        public static event Action<IncidentLog> IncidentRaised;
        public static event Action<string> Toast;
        public static event Action<OfficePick> OfficePicked;

        public static void RaiseStateChanged()
        {
            StateChanged?.Invoke();
        }

        public static void RaiseIncident(IncidentLog log)
        {
            IncidentRaised?.Invoke(log);
            RaiseStateChanged();
        }

        public static void RaiseToast(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            Toast?.Invoke(message);
        }

        public static void RaiseOfficePicked(OfficePick pick)
        {
            OfficePicked?.Invoke(pick ?? new OfficePick());
        }
    }
}
