using GameDevStudio.Config;
using UnityEngine;

namespace GameDevStudio.Simulation
{
    public static class EmployeeAi
    {
        public static string ActivityLabel(EmployeeActivity activity)
        {
            switch (activity)
            {
                case EmployeeActivity.Working: return "работает";
                case EmployeeActivity.Managing: return "ведёт проект";
                case EmployeeActivity.Coffee: return "пьёт кофе";
                case EmployeeActivity.Toilet: return "в туалете";
                case EmployeeActivity.Rest: return "отдыхает";
                case EmployeeActivity.Walking: return "идёт";
                default: return "без дела";
            }
        }

        public static void Tick(StudioSimulation sim, Employee employee, float hours)
        {
            NeedsConfigDto needs = sim.Data.needs;
            if (employee.StunnedHoursLeft > 0)
            {
                employee.Activity = EmployeeActivity.Idle;
                ReleaseFacility(sim, employee);
                return;
            }

            if (employee.Activity == EmployeeActivity.Coffee ||
                employee.Activity == EmployeeActivity.Toilet ||
                employee.Activity == EmployeeActivity.Rest)
            {
                ApplyFacility(sim, employee, hours);
                if (ShouldKeepFacility(needs, employee))
                {
                    return;
                }

                ReleaseFacility(sim, employee);
            }

            string seek = NextNeed(sim, employee);
            if (seek != null)
            {
                if (TryEnterFacility(sim, employee, seek))
                {
                    ApplyFacility(sim, employee, hours);
                    return;
                }

                GoNearFacility(sim, employee, seek);
                employee.Activity = EmployeeActivity.Walking;
                return;
            }

            ReleaseFacility(sim, employee);
            if (sim.IsWorkHour && CanGoToDesk(sim, employee))
            {
                DeskSlot desk = sim.FindDesk(employee.DeskId);
                if (desk != null)
                {
                    employee.TileX = desk.TileX;
                    employee.TileY = desk.TileY;
                    bool lead = employee.RoleId == "producer" || employee.RoleId == "director";
                    employee.Activity = lead ? EmployeeActivity.Managing : EmployeeActivity.Working;
                    return;
                }
            }

            employee.TileX = sim.State.SpawnX;
            employee.TileY = sim.State.SpawnY;
            employee.Activity = sim.IsWorkHour ? EmployeeActivity.Idle : EmployeeActivity.Rest;
        }

        static bool CanGoToDesk(StudioSimulation sim, Employee employee)
        {
            if (employee.DeskId == 0)
            {
                return false;
            }

            if (employee.RoleId == "producer" || employee.RoleId == "director")
            {
                return true;
            }

            Project project = sim.FindProject(employee.AssignedProjectId);
            if (project == null)
            {
                return false;
            }

            if (project.Status == ProjectStatus.InDev || project.Status == ProjectStatus.Ready)
            {
                return true;
            }

            return project.Status == ProjectStatus.Live && employee.RoleId == "marketer";
        }

        static string NextNeed(StudioSimulation sim, Employee employee)
        {
            NeedsConfigDto needs = sim.Data.needs;
            bool workingHours = sim.IsWorkHour;

            if (employee.Bladder >= needs.seekToiletAt)
            {
                return "toilet";
            }

            if (!workingHours)
            {
                return "sofa";
            }

            if (employee.Energy <= needs.seekRestAt)
            {
                return "sofa";
            }

            if (employee.Mood <= needs.seekCoffeeAt)
            {
                return "coffee";
            }

            return null;
        }

        static bool ShouldKeepFacility(NeedsConfigDto needs, Employee employee)
        {
            switch (employee.Activity)
            {
                case EmployeeActivity.Toilet:
                    return employee.Bladder > needs.resumeWorkBladder;
                case EmployeeActivity.Rest:
                    return employee.Energy < needs.resumeWorkEnergy;
                case EmployeeActivity.Coffee:
                    return employee.Mood < needs.resumeWorkMood;
                default:
                    return false;
            }
        }

        static bool TryEnterFacility(StudioSimulation sim, Employee employee, string facilityId)
        {
            Facility facility = sim.FindFacility(facilityId);
            if (facility == null)
            {
                return false;
            }

            if (facility.OccupiedByEmployeeId != 0 && facility.OccupiedByEmployeeId != employee.Id)
            {
                return false;
            }

            ReleaseFacility(sim, employee);
            facility.OccupiedByEmployeeId = employee.Id;
            employee.FacilityId = facility.Id;
            employee.TileX = facility.TileX;
            employee.TileY = facility.TileY;
            switch (facility.Need)
            {
                case "bladder":
                    employee.Activity = EmployeeActivity.Toilet;
                    break;
                case "energy":
                    employee.Activity = EmployeeActivity.Rest;
                    break;
                default:
                    employee.Activity = EmployeeActivity.Coffee;
                    break;
            }

            return true;
        }

        static void GoNearFacility(StudioSimulation sim, Employee employee, string facilityId)
        {
            Facility facility = sim.FindFacility(facilityId);
            if (facility == null)
            {
                return;
            }

            employee.TileX = Mathf.Max(0, facility.TileX - 1);
            employee.TileY = facility.TileY;
        }

        static void ApplyFacility(StudioSimulation sim, Employee employee, float hours)
        {
            NeedsConfigDto needs = sim.Data.needs;
            float max = needs.maxValue;
            switch (employee.Activity)
            {
                case EmployeeActivity.Coffee:
                    employee.Mood = Mathf.Min(max, employee.Mood + needs.coffeeMoodRestore * hours);
                    employee.Energy = Mathf.Min(max, employee.Energy + needs.coffeeEnergyRestore * hours);
                    break;
                case EmployeeActivity.Toilet:
                    employee.Bladder = Mathf.Max(0f, employee.Bladder - needs.toiletBladderRestore * hours);
                    break;
                case EmployeeActivity.Rest:
                    employee.Energy = Mathf.Min(max, employee.Energy + needs.sofaEnergyPerHour * hours);
                    employee.Mood = Mathf.Min(max, employee.Mood + needs.moodRestorePerIdleHour * hours);
                    break;
            }
        }

        static void ReleaseFacility(StudioSimulation sim, Employee employee)
        {
            if (string.IsNullOrEmpty(employee.FacilityId))
            {
                return;
            }

            Facility facility = sim.FindFacility(employee.FacilityId);
            if (facility != null && facility.OccupiedByEmployeeId == employee.Id)
            {
                facility.OccupiedByEmployeeId = 0;
            }

            employee.FacilityId = null;
        }
    }
}
