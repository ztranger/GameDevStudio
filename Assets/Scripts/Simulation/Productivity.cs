using GameDevStudio.Config;
using UnityEngine;

namespace GameDevStudio.Simulation
{
    public static class Productivity
    {
        public static float Curve(ProductivityConfigDto config, float daysOnAssignment)
        {
            if (config == null)
            {
                return 1f;
            }

            float ramp = Mathf.Max(0.01f, config.rampUpDays);
            if (daysOnAssignment < ramp)
            {
                return Mathf.Lerp(config.startMultiplier, config.peakMultiplier, daysOnAssignment / ramp);
            }

            float afterRamp = daysOnAssignment - ramp;
            if (afterRamp < config.peakDays)
            {
                return config.peakMultiplier;
            }

            float burnT = (afterRamp - config.peakDays) / Mathf.Max(0.01f, config.burnoutDays);
            return Mathf.Lerp(config.peakMultiplier, config.burnoutMultiplier, Mathf.Clamp01(burnT));
        }

        public static float SkillFactor(int skill, int minSkill)
        {
            if (minSkill <= 0)
            {
                return 1f;
            }

            if (skill >= minSkill)
            {
                return 1f + 0.06f * (skill - minSkill);
            }

            return Mathf.Clamp(0.35f + 0.65f * skill / minSkill, 0.25f, 1f);
        }

        public static float NeedsFactor(NeedsConfigDto needs, Employee employee)
        {
            float factor = 1f;
            if (employee.Energy < needs.lowEnergyThreshold)
            {
                factor *= needs.lowEnergyMultiplier;
            }

            if (employee.Mood < needs.lowEnergyThreshold)
            {
                factor *= needs.lowMoodMultiplier;
            }

            if (employee.Bladder >= needs.seekToiletAt * 0.85f)
            {
                float bladderMul = needs.lowBladderMultiplier > 0.01f ? needs.lowBladderMultiplier : 0.55f;
                factor *= bladderMul;
            }

            return factor;
        }

        public static string PhaseLabel(ProductivityConfigDto config, float daysOnAssignment)
        {
            if (config == null)
            {
                return string.Empty;
            }

            if (daysOnAssignment < config.rampUpDays)
            {
                return "въезжает";
            }

            if (daysOnAssignment < config.rampUpDays + config.peakDays)
            {
                return "на пике";
            }

            return "выгорает";
        }
    }
}
