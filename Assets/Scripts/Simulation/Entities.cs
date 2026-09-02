using System.Collections.Generic;

namespace GameDevStudio.Simulation
{
    public enum ProjectStatus
    {
        InDev,
        Ready,
        Live,
        Banned,
        Cancelled
    }

    public sealed class StudioState
    {
        public int Money;
        public int Day = 1;
        public int Hour = 9;
        public float PirateHeat;
        public int TotalReleases;
        public int NextId = 1;
        public float HoursSinceIncidentCheck;
        public readonly List<DeskSlot> Desks = new List<DeskSlot>();
        public readonly List<Employee> Employees = new List<Employee>();
        public readonly List<Candidate> HireMarket = new List<Candidate>();
        public readonly List<Project> Projects = new List<Project>();
        public readonly List<OwnedSoftware> Software = new List<OwnedSoftware>();
        public readonly List<OwnedEngine> Engines = new List<OwnedEngine>();
        public readonly List<Facility> Facilities = new List<Facility>();
        public readonly List<IncidentCooldown> IncidentCooldowns = new List<IncidentCooldown>();
        public IncidentLog PendingIncident;
        public string LastMessage;
        public int SpawnX = 7;
        public int SpawnY = 1;
    }

    public enum EmployeeActivity
    {
        Idle,
        Walking,
        Working,
        Managing,
        Coffee,
        Toilet,
        Rest
    }

    public sealed class Facility
    {
        public string Id;
        public string DisplayName;
        public string Need;
        public int TileX;
        public int TileY;
        public int OccupiedByEmployeeId;
    }

    public sealed class DeskSlot
    {
        public int Id;
        public string EquipmentId;
        public int OccupiedByEmployeeId;
        public int TileX;
        public int TileY;

        public bool HasWorkstation => !string.IsNullOrEmpty(EquipmentId);
    }

    public sealed class Employee
    {
        public int Id;
        public string Name;
        public string RoleId;
        public int Skill;
        public int SalaryPerDay;
        public float Energy = 100f;
        public float Mood = 100f;
        public float Bladder;
        public int AssignedProjectId;
        public float DaysOnAssignment;
        public int DeskId;
        public int StunnedHoursLeft;
        public bool ProducerAutoEnabled = true;
        public int TileX;
        public int TileY;
        public EmployeeActivity Activity = EmployeeActivity.Idle;
        public string FacilityId;
    }

    public sealed class Candidate
    {
        public int Id;
        public string Name;
        public string RoleId;
        public int Skill;
        public int HireCost;
        public int SalaryPerDay;
    }

    public sealed class Project
    {
        public int Id;
        public string Name;
        public string GenreId;
        public string EngineId;
        public int ProducerEmployeeId;
        public ProjectStatus Status = ProjectStatus.InDev;
        public readonly List<WorkTrack> Tracks = new List<WorkTrack>();
        public readonly List<WorkTrack> OptionalTracks = new List<WorkTrack>();
        public float Quality = 1f;
        public float Polish;
        public int Stars;
        public string Review;
        public bool UsedPirate;
        public int DaysLive;
        public int DailyRevenue;
        public int RevenueDaysLeft;
        public int BasePayout;
    }

    public sealed class WorkTrack
    {
        public string RoleId;
        public float Required;
        public float Current;
        public int MinSkill;

        public float Normalized => Required <= 0.001f ? 1f : Current / Required;
        public bool Complete => Current >= Required;
    }

    public sealed class OwnedEngine
    {
        public string EngineId;
        public bool Pirated;
    }

    public sealed class QualityReport
    {
        public float Quality = 1f;
        public int Stars = 3;
        public string Review = string.Empty;
        public string Breakdown = string.Empty;
        public bool UsedPirate;
        public int Payout;
        public int DailyEstimate;
    }

    public sealed class OwnedSoftware
    {
        public string SoftwareId;
        public string RoleId;
        public bool Pirated;
        public float Productivity;
    }

    public sealed class IncidentCooldown
    {
        public string Id;
        public int AvailableOnDay;
    }

    public sealed class IncidentLog
    {
        public string Id;
        public string Title;
        public string Body;
        public int ProjectId;
        public int EmployeeId;
    }

    public enum OfficePickKind
    {
        None,
        Employee,
        Desk,
        Facility
    }

    public sealed class OfficePick
    {
        public OfficePickKind Kind;
        public int EmployeeId;
        public int DeskId;
        public string FacilityId;
    }
}
