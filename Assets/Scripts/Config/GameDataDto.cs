using System;

namespace GameDevStudio.Config
{
    [Serializable]
    public sealed class GameDataDto
    {
        public StudioConfigDto studio;
        public TimeConfigDto time;
        public ProductivityConfigDto productivity;
        public NeedsConfigDto needs;
        public IncidentGlobalConfigDto incidentSettings;
        public RoleDto[] roles;
        public EngineDto[] engines;
        public TechDto[] technologies;
        public GenreDto[] genres;
        public EquipmentDto[] equipment;
        public SoftwareDto[] software;
        public IncidentDto[] incidents;
        public QualityConfigDto quality;
        public string[] reviewsHigh;
        public string[] reviewsMid;
        public string[] reviewsLow;
        public string[] firstNames;
        public string[] lastNames;
        public string[] projectAdjectives;
        public OfficeLayoutDto layout;

        public RoleDto FindRole(string id)
        {
            return FindById(roles, id, r => r.id);
        }

        public GenreDto FindGenre(string id)
        {
            return FindById(genres, id, g => g.id);
        }

        public EngineDto FindEngine(string id)
        {
            return FindById(engines, id, e => e.id);
        }

        public EquipmentDto FindEquipment(string id)
        {
            return FindById(equipment, id, e => e.id);
        }

        public SoftwareDto FindSoftware(string id)
        {
            return FindById(software, id, s => s.id);
        }

        public TechDto FindTech(string id)
        {
            return FindById(technologies, id, t => t.id);
        }

        public IncidentDto FindIncident(string id)
        {
            return FindById(incidents, id, n => n.id);
        }

        static T FindById<T>(T[] items, string id, Func<T, string> getId) where T : class
        {
            if (items == null || string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (int i = 0; i < items.Length; i++)
            {
                if (getId(items[i]) == id)
                {
                    return items[i];
                }
            }

            return null;
        }
    }

    [Serializable]
    public sealed class StudioConfigDto
    {
        public int startingMoney;
        public int startingDeskSlots;
        public int roomTilesX;
        public int roomTilesY;
        public int hireMarketSize;
        public int hireRefreshCost;
        public int maxActiveProjects;
        public int seed;
        public int producerUnlockAfterReleases;
        public int extraDeskSlotPrice;
        public int maxDeskSlots;
    }

    [Serializable]
    public sealed class TimeConfigDto
    {
        public int minutesPerTick;
        public int workStartHour;
        public int workEndHour;
        public float realSecondsPerTick;
    }

    [Serializable]
    public sealed class ProductivityConfigDto
    {
        public float rampUpDays;
        public float peakDays;
        public float burnoutDays;
        public float startMultiplier;
        public float peakMultiplier;
        public float burnoutMultiplier;
    }

    [Serializable]
    public sealed class NeedsConfigDto
    {
        public float maxValue;
        public float energyDecayPerWorkHour;
        public float moodDecayPerWorkHour;
        public float energyRestorePerIdleHour;
        public float moodRestorePerIdleHour;
        public float lowEnergyThreshold;
        public float lowEnergyMultiplier;
        public float lowMoodMultiplier;
        public float seekToiletAt;
        public float seekRestAt;
        public float seekCoffeeAt;
        public float resumeWorkEnergy;
        public float resumeWorkMood;
        public float resumeWorkBladder;
        public float bladderGainPerWorkHour;
        public float bladderGainPerIdleHour;
        public float coffeeMoodRestore;
        public float coffeeEnergyRestore;
        public float toiletBladderRestore;
        public float sofaEnergyPerHour;
        public float idleRestEnergyPerHour;
        public float walkTilesPerSecond;
        public float lowBladderMultiplier;
    }

    [Serializable]
    public sealed class IncidentGlobalConfigDto
    {
        public float checkEveryHours;
        public float baseChancePerCheck;
        public float pirateHeatPerPirate;
        public float pirateHeatDecayPerDay;
    }

    [Serializable]
    public sealed class RoleDto
    {
        public string id;
        public string displayName;
        public string color;
        public int hireCostBase;
        public int salaryBase;
        public int skillMin;
        public int skillMax;
        public string departmentId;
    }

    [Serializable]
    public sealed class EngineDto
    {
        public string id;
        public string displayName;
        public int tier;
        public int price;
        public float pirateRisk;
        public bool startingOwned;
    }

    [Serializable]
    public sealed class TechDto
    {
        public string id;
        public string displayName;
    }

    [Serializable]
    public sealed class GenreDto
    {
        public string id;
        public string displayName;
        public int tier;
        public int unlockAfterReleases;
        public string engineId;
        public string[] techIds;
        public int basePayout;
        public int dailyRevenue;
        public int revenueDays;
        public WorkTrackDto[] tracks;
        public WorkTrackDto[] optionalTracks;
    }

    [Serializable]
    public sealed class WorkTrackDto
    {
        public string roleId;
        public float points;
        public int minSkill;
    }

    [Serializable]
    public sealed class EquipmentDto
    {
        public string id;
        public string displayName;
        public int price;
        public string[] roleIds;
        public float productivity;
    }

    [Serializable]
    public sealed class SoftwareDto
    {
        public string id;
        public string displayName;
        public int price;
        public string roleId;
        public float productivity;
        public float pirateRisk;
    }

    [Serializable]
    public sealed class IncidentDto
    {
        public string id;
        public string displayName;
        public string body;
        public int weight;
        public int cooldownDays;
        public string[] conditions;
        public IncidentChoiceDto[] choices;
        public IncidentEffectDto[] effects;
    }

    [Serializable]
    public sealed class IncidentChoiceDto
    {
        public string id;
        public string label;
        public string hint;
        public bool requireMoney;
        public IncidentEffectDto[] effects;
    }

    [Serializable]
    public sealed class IncidentEffectDto
    {
        public string type;
        public string roleId;
        public float percent;
        public float points;
        public int amount;
        public int hours;
        public string text;
    }

    [Serializable]
    public sealed class QualityConfigDto
    {
        public float pirateTrackPenalty;
        public float pirateEnginePenalty;
        public float polishPerWorkHour;
        public float maxPolish;
        public float marketingLiveBonus;
        public float qaBonus;
        public float soundBonus;
    }

    [Serializable]
    public sealed class OfficeLayoutDto
    {
        public TileDto[] deskTiles;
        public FacilityDto[] facilities;
        public FloorLabelDto[] labels;
        public int spawnX;
        public int spawnY;
        public int doorX;
        public int doorY;
    }

    [Serializable]
    public sealed class FloorLabelDto
    {
        public string text;
        public int x;
        public int y;
        public string color;
    }

    [Serializable]
    public sealed class TileDto
    {
        public int x;
        public int y;
    }

    [Serializable]
    public sealed class FacilityDto
    {
        public string id;
        public string displayName;
        public int x;
        public int y;
        public string need;
    }
}
