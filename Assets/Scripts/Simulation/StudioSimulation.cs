using System.Collections.Generic;
using System.Text;
using GameDevStudio.Config;
using GameDevStudio.Core;
using UnityEngine;

namespace GameDevStudio.Simulation
{
    public sealed class StudioSimulation
    {
        public readonly GameDataDto Data;
        public readonly StudioState State = new StudioState();

        readonly System.Random _rng;

        public StudioSimulation(GameDataDto data)
        {
            Data = data;
            _rng = new System.Random(data.studio.seed);
            State.Money = data.studio.startingMoney;
            State.Hour = Mathf.Max(8, data.time.workStartHour - 1);
            SetupRoom();
            RefreshHireMarket();
        }

        void SetupRoom()
        {
            OfficeLayoutDto layout = Data.layout;
            if (layout != null)
            {
                State.SpawnX = layout.spawnX;
                State.SpawnY = layout.spawnY;
            }

            for (int i = 0; i < Data.studio.startingDeskSlots; i++)
            {
                var desk = new DeskSlot { Id = NextId() };
                if (layout != null && layout.deskTiles != null && i < layout.deskTiles.Length)
                {
                    desk.TileX = layout.deskTiles[i].x;
                    desk.TileY = layout.deskTiles[i].y;
                }
                else
                {
                    desk.TileX = 3 + i * 4;
                    desk.TileY = 3;
                }

                State.Desks.Add(desk);
            }

            if (layout != null && layout.facilities != null)
            {
                for (int i = 0; i < layout.facilities.Length; i++)
                {
                    FacilityDto dto = layout.facilities[i];
                    State.Facilities.Add(new Facility
                    {
                        Id = dto.id,
                        DisplayName = dto.displayName,
                        Need = dto.need,
                        TileX = dto.x,
                        TileY = dto.y
                    });
                }
            }

            if (Data.engines != null)
            {
                for (int i = 0; i < Data.engines.Length; i++)
                {
                    EngineDto engine = Data.engines[i];
                    if (engine.startingOwned)
                    {
                        State.Engines.Add(new OwnedEngine { EngineId = engine.id, Pirated = false });
                    }
                }
            }
        }

        public int NextId()
        {
            return State.NextId++;
        }

        public bool IsWorkHour =>
            State.Hour >= Data.time.workStartHour && State.Hour < Data.time.workEndHour;

        public IReadOnlyList<GenreDto> UnlockedGenres()
        {
            var list = new List<GenreDto>();
            for (int i = 0; i < Data.genres.Length; i++)
            {
                if (Data.genres[i].unlockAfterReleases <= State.TotalReleases)
                {
                    list.Add(Data.genres[i]);
                }
            }

            return list;
        }

        public int ActiveProjectCount()
        {
            int count = 0;
            for (int i = 0; i < State.Projects.Count; i++)
            {
                ProjectStatus status = State.Projects[i].Status;
                if (status == ProjectStatus.InDev || status == ProjectStatus.Ready)
                {
                    count++;
                }
            }

            return count;
        }

        public bool TryStartProject(string genreId, int producerEmployeeId, out string error)
        {
            error = null;
            GenreDto genre = Data.FindGenre(genreId);
            if (genre == null)
            {
                error = "Жанр не найден.";
                return false;
            }

            if (genre.unlockAfterReleases > State.TotalReleases)
            {
                error = "Жанр ещё закрыт.";
                return false;
            }

            if (ActiveProjectCount() >= Data.studio.maxActiveProjects)
            {
                error = "Слишком много проектов в работе.";
                return false;
            }

            if (!HasEngine(genre.engineId))
            {
                EngineDto missing = Data.FindEngine(genre.engineId);
                error = "Нужна лицензия на " + (missing != null ? missing.displayName : genre.engineId) +
                        ". Купите или спиратьте в магазине.";
                return false;
            }

            var project = new Project
            {
                Id = NextId(),
                Name = BuildProjectName(genre),
                GenreId = genre.id,
                EngineId = genre.engineId,
                ProducerEmployeeId = producerEmployeeId,
                DailyRevenue = genre.dailyRevenue,
                RevenueDaysLeft = genre.revenueDays,
                BasePayout = genre.basePayout
            };

            for (int i = 0; i < genre.tracks.Length; i++)
            {
                WorkTrackDto track = genre.tracks[i];
                project.Tracks.Add(new WorkTrack
                {
                    RoleId = track.roleId,
                    Required = track.points,
                    MinSkill = track.minSkill
                });
            }

            if (genre.optionalTracks != null)
            {
                for (int i = 0; i < genre.optionalTracks.Length; i++)
                {
                    WorkTrackDto track = genre.optionalTracks[i];
                    project.OptionalTracks.Add(new WorkTrack
                    {
                        RoleId = track.roleId,
                        Required = track.points,
                        MinSkill = track.minSkill
                    });
                }
            }

            StampPirateUsage(project);
            State.Projects.Add(project);
            if (producerEmployeeId == 0)
            {
                if (ActiveProjectCount() <= 1)
                {
                    AutoAssignIdleStaff(project);
                    GameEvents.RaiseToast("Стартовал проект: " + project.Name);
                }
                else
                {
                    GameEvents.RaiseToast("Стартовал «" + project.Name + "». Назначьте людей в Команде.");
                }
            }
            else
            {
                Employee producer = FindEmployee(producerEmployeeId);
                string who = producer != null ? producer.Name : "Продюсер";
                if (producer != null)
                {
                    producer.AssignedProjectId = project.Id;
                    producer.DaysOnAssignment = 0f;
                }

                GameEvents.RaiseToast(who + " стартовал «" + project.Name + "». Распределите людей в Команде.");
            }

            GameEvents.RaiseStateChanged();
            return true;
        }

        public bool TryHire(int candidateId, out string error)
        {
            error = null;
            Candidate candidate = FindCandidate(candidateId);
            if (candidate == null)
            {
                error = "Кандидат уже ушёл.";
                return false;
            }

            if (State.Money < candidate.HireCost)
            {
                error = "Не хватает денег на найм.";
                return false;
            }

            State.Money -= candidate.HireCost;
            var employee = new Employee
            {
                Id = NextId(),
                Name = candidate.Name,
                RoleId = candidate.RoleId,
                Skill = candidate.Skill,
                SalaryPerDay = candidate.SalaryPerDay,
                TileX = State.SpawnX,
                TileY = State.SpawnY,
                Activity = EmployeeActivity.Idle
            };
            State.Employees.Add(employee);
            State.HireMarket.Remove(candidate);
            TrySeatEmployee(employee);
            AutoAssignEmployee(employee);
            GameEvents.RaiseToast("Нанят: " + employee.Name);
            GameEvents.RaiseStateChanged();
            return true;
        }

        public bool TryBuyEquipment(string equipmentId, out string error)
        {
            return TryBuyEquipment(equipmentId, 0, out error);
        }

        public bool TryBuyEquipment(string equipmentId, int deskId, out string error)
        {
            error = null;
            EquipmentDto item = Data.FindEquipment(equipmentId);
            if (item == null)
            {
                error = "Оборудование не найдено.";
                return false;
            }

            DeskSlot slot = deskId != 0 ? FindDesk(deskId) : null;
            if (slot == null)
            {
                for (int i = 0; i < State.Desks.Count; i++)
                {
                    if (!State.Desks[i].HasWorkstation)
                    {
                        slot = State.Desks[i];
                        break;
                    }
                }
            }

            if (slot == null)
            {
                error = "Нет свободного места под рабочее место.";
                return false;
            }

            if (slot.HasWorkstation)
            {
                error = "Здесь уже стоит оборудование.";
                return false;
            }

            if (State.Money < item.price)
            {
                error = "Не хватает денег на оборудование.";
                return false;
            }

            State.Money -= item.price;
            slot.EquipmentId = item.id;
            SeatWaitingEmployees();
            GameEvents.RaiseToast("Куплено: " + item.displayName);
            GameEvents.RaiseStateChanged();
            return true;
        }

        public bool TryAcquireSoftware(string softwareId, bool pirate, out string error)
        {
            error = null;
            SoftwareDto item = Data.FindSoftware(softwareId);
            if (item == null)
            {
                error = "Софт не найден.";
                return false;
            }

            if (HasSoftwareForRole(item.roleId))
            {
                error = "Софт для этой роли уже есть.";
                return false;
            }

            if (!pirate)
            {
                if (State.Money < item.price)
                {
                    error = "Не хватает денег на лицензию.";
                    return false;
                }

                State.Money -= item.price;
            }
            else
            {
                State.PirateHeat += Data.incidentSettings.pirateHeatPerPirate + item.pirateRisk;
            }

            State.Software.Add(new OwnedSoftware
            {
                SoftwareId = item.id,
                RoleId = item.roleId,
                Pirated = pirate,
                Productivity = item.productivity
            });

            if (pirate)
            {
                StampPirateOnActiveProjects();
            }

            GameEvents.RaiseToast((pirate ? "Спиратили: " : "Купили: ") + item.displayName);
            GameEvents.RaiseStateChanged();
            return true;
        }

        public bool TryAcquireEngine(string engineId, bool pirate, out string error)
        {
            error = null;
            EngineDto item = Data.FindEngine(engineId);
            if (item == null)
            {
                error = "Движок не найден.";
                return false;
            }

            if (HasEngine(item.id))
            {
                error = "Лицензия на этот движок уже есть.";
                return false;
            }

            if (!pirate)
            {
                if (State.Money < item.price)
                {
                    error = "Не хватает денег на движок.";
                    return false;
                }

                State.Money -= item.price;
            }
            else
            {
                State.PirateHeat += Data.incidentSettings.pirateHeatPerPirate + item.pirateRisk;
            }

            State.Engines.Add(new OwnedEngine
            {
                EngineId = item.id,
                Pirated = pirate
            });

            if (pirate)
            {
                StampPirateOnActiveProjects();
            }

            GameEvents.RaiseToast((pirate ? "Спиратили: " : "Купили: ") + item.displayName);
            GameEvents.RaiseStateChanged();
            return true;
        }

        public bool TryAssign(int employeeId, int projectId, out string error)
        {
            error = null;
            Employee employee = FindEmployee(employeeId);
            if (employee == null)
            {
                error = "Сотрудник не найден.";
                return false;
            }

            if (projectId != 0)
            {
                Project project = FindProject(projectId);
                if (project == null)
                {
                    error = "Проект нельзя назначить.";
                    return false;
                }

                if (project.Status == ProjectStatus.Live)
                {
                    if (employee.RoleId != "marketer")
                    {
                        error = "На лайв можно посадить только маркетолога.";
                        return false;
                    }
                }
                else if (project.Status != ProjectStatus.InDev && project.Status != ProjectStatus.Ready)
                {
                    error = "Проект нельзя назначить.";
                    return false;
                }
                else if (!ProjectNeedsRole(project, employee.RoleId) && employee.RoleId != "producer" && employee.RoleId != "director")
                {
                    error = "Этому человеку тут нечего делать.";
                    return false;
                }
            }

            if (employee.AssignedProjectId != projectId)
            {
                int previousId = employee.AssignedProjectId;
                employee.DaysOnAssignment = 0f;
                employee.AssignedProjectId = projectId;
                if (previousId != 0 && projectId != 0)
                {
                    Project previous = FindProject(previousId);
                    Project next = FindProject(projectId);
                    GameEvents.RaiseToast(employee.Name + ": " +
                                          (previous != null ? previous.Name : "проект") +
                                          " → " + (next != null ? next.Name : "проект") +
                                          ". Въезд с нуля.");
                }
            }
            else
            {
                employee.AssignedProjectId = projectId;
            }

            GameEvents.RaiseStateChanged();
            return true;
        }

        public bool TryRelease(int projectId, out string error)
        {
            error = null;
            Project project = FindProject(projectId);
            if (project == null)
            {
                error = "Проект не найден.";
                return false;
            }

            if (project.Status != ProjectStatus.Ready && !AllTracksComplete(project))
            {
                error = "Проект ещё не готов.";
                return false;
            }

            QualityReport report = PreviewQuality(project);
            project.Status = ProjectStatus.Live;
            project.Quality = report.Quality;
            project.Stars = report.Stars;
            project.Review = report.Review;
            project.UsedPirate = report.UsedPirate;
            int payout = report.Payout;
            State.Money += payout;
            State.TotalReleases++;
            ResetStaffAssignments(project.Id);
            string stars = new string('★', report.Stars) + new string('☆', 5 - report.Stars);
            if (State.TotalReleases == ProducerUnlockReleases())
            {
                RefreshHireMarket();
                GameEvents.RaiseToast("Релиз " + stars + "  +" + payout + "$  ·  В найме появились продюсеры.");
            }
            else
            {
                GameEvents.RaiseToast("Релиз " + stars + "  +" + payout + "$  (" + project.Name + ")");
            }
            GameEvents.RaiseStateChanged();
            return true;
        }

        public bool TryRefreshHireMarket(out string error)
        {
            error = null;
            if (State.Money < Data.studio.hireRefreshCost)
            {
                error = "Не хватает денег на новый пул кандидатов.";
                return false;
            }

            State.Money -= Data.studio.hireRefreshCost;
            RefreshHireMarket();
            GameEvents.RaiseStateChanged();
            return true;
        }

        public bool TryBuyDeskSlot(out string error)
        {
            error = null;
            int max = Data.studio.maxDeskSlots > 0 ? Data.studio.maxDeskSlots : 6;
            if (State.Desks.Count >= max)
            {
                error = "Больше столов в этой комнате не влезет.";
                return false;
            }

            int price = Data.studio.extraDeskSlotPrice > 0 ? Data.studio.extraDeskSlotPrice : 400;
            if (State.Money < price)
            {
                error = "Не хватает денег на слот стола.";
                return false;
            }

            var desk = new DeskSlot { Id = NextId() };
            OfficeLayoutDto layout = Data.layout;
            int index = State.Desks.Count;
            if (layout != null && layout.deskTiles != null && index < layout.deskTiles.Length)
            {
                desk.TileX = layout.deskTiles[index].x;
                desk.TileY = layout.deskTiles[index].y;
            }
            else
            {
                desk.TileX = 3 + (index % 3) * 4;
                desk.TileY = 3 + (index / 3) * 2;
            }

            State.Money -= price;
            State.Desks.Add(desk);
            GameEvents.RaiseToast("Новый слот стола. Купите в него ПК.");
            GameEvents.RaiseStateChanged();
            return true;
        }

        public bool TrySetProducerAuto(int employeeId, bool enabled, out string error)
        {
            error = null;
            Employee employee = FindEmployee(employeeId);
            if (employee == null || employee.RoleId != "producer")
            {
                error = "Это не продюсер.";
                return false;
            }

            employee.ProducerAutoEnabled = enabled;
            GameEvents.RaiseToast(employee.Name + (enabled ? " снова сам стартует проекты." : " ждёт вашей команды."));
            GameEvents.RaiseStateChanged();
            return true;
        }

        public string ProjectOwnerLabel(Project project)
        {
            if (project.ProducerEmployeeId == 0)
            {
                return "ваш проект";
            }

            Employee producer = FindEmployee(project.ProducerEmployeeId);
            return producer != null ? "продюсер: " + producer.Name : "продюсер";
        }

        public string TrackAssigneeLine(int projectId, string roleId)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < State.Employees.Count; i++)
            {
                Employee employee = State.Employees[i];
                if (employee.AssignedProjectId != projectId || employee.RoleId != roleId)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(employee.Name);
            }

            return builder.Length == 0 ? "никто" : builder.ToString();
        }

        public string AssignmentPhase(Employee employee)
        {
            if (employee.AssignedProjectId == 0)
            {
                return "свободен";
            }

            return Productivity.PhaseLabel(Data.productivity, employee.DaysOnAssignment);
        }

        int ProducerUnlockReleases()
        {
            return Data.studio.producerUnlockAfterReleases > 0 ? Data.studio.producerUnlockAfterReleases : 1;
        }

        public void AcknowledgeIncident()
        {
            TryResolveIncident(null, out _);
        }

        public bool TryResolveIncident(string choiceId, out string error)
        {
            error = null;
            IncidentLog pending = State.PendingIncident;
            if (pending == null)
            {
                return true;
            }

            IncidentDto incident = Data.FindIncident(pending.Id);
            IncidentChoiceDto choice = SelectChoice(incident, choiceId);
            IncidentEffectDto[] effects = choice != null ? choice.effects : incident != null ? incident.effects : null;
            if (choice != null && choice.requireMoney)
            {
                int cost = FineCost(choice.effects);
                if (State.Money < cost)
                {
                    error = "Не хватает денег (" + cost + "$).";
                    return false;
                }
            }

            string result = ApplyEffects(effects, pending.ProjectId, pending.EmployeeId);
            StartCooldown(incident);
            State.PendingIncident = null;
            if (!string.IsNullOrEmpty(result))
            {
                GameEvents.RaiseToast(result);
            }

            GameEvents.RaiseStateChanged();
            return true;
        }

        static IncidentChoiceDto SelectChoice(IncidentDto incident, string choiceId)
        {
            if (incident == null || incident.choices == null || incident.choices.Length == 0)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(choiceId))
            {
                for (int i = 0; i < incident.choices.Length; i++)
                {
                    if (incident.choices[i].id == choiceId)
                    {
                        return incident.choices[i];
                    }
                }
            }

            return incident.choices[incident.choices.Length - 1];
        }

        static int FineCost(IncidentEffectDto[] effects)
        {
            if (effects == null)
            {
                return 0;
            }

            int cost = 0;
            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i].type == "fine")
                {
                    cost += effects[i].amount;
                }
            }

            return cost;
        }

        public void Tick()
        {
            if (State.PendingIncident != null)
            {
                return;
            }

            int minutes = Mathf.Max(1, Data.time.minutesPerTick);
            State.Hour += minutes / 60;
            while (State.Hour >= 24)
            {
                State.Hour -= 24;
                StartNewDay();
            }

            float hours = minutes / 60f;
            RunProducers();
            for (int i = 0; i < State.Employees.Count; i++)
            {
                EmployeeAi.Tick(this, State.Employees[i], hours);
            }

            SimulateWork(hours);
            UpdateNeeds(hours);
            CheckReadyProjects();
            MaybeFireIncident(hours);
            GameEvents.RaiseStateChanged();
        }

        void StartNewDay()
        {
            State.Day++;
            PaySalaries();
            CollectLiveRevenue();
            State.PirateHeat = Mathf.Max(0f, State.PirateHeat - Data.incidentSettings.pirateHeatDecayPerDay);

            for (int i = 0; i < State.Employees.Count; i++)
            {
                if (State.Employees[i].AssignedProjectId != 0)
                {
                    State.Employees[i].DaysOnAssignment += 1f;
                }
            }
        }

        void PaySalaries()
        {
            int total = 0;
            for (int i = 0; i < State.Employees.Count; i++)
            {
                total += State.Employees[i].SalaryPerDay;
            }

            State.Money -= total;
            if (total > 0)
            {
                GameEvents.RaiseToast("Зарплаты: −" + total + "$");
            }
        }

        void CollectLiveRevenue()
        {
            int total = 0;
            for (int i = 0; i < State.Projects.Count; i++)
            {
                Project project = State.Projects[i];
                if (project.Status != ProjectStatus.Live || project.RevenueDaysLeft <= 0)
                {
                    continue;
                }

                int gain = Mathf.RoundToInt(project.DailyRevenue * project.Quality * (1f + MarketingBonus(project)));
                State.Money += gain;
                total += gain;
                project.DaysLive++;
                project.RevenueDaysLeft--;
            }

            if (total > 0)
            {
                GameEvents.RaiseToast("Сторы принесли +" + total + "$");
            }
        }

        void RunProducers()
        {
            if (!IsWorkHour)
            {
                return;
            }

            for (int i = 0; i < State.Employees.Count; i++)
            {
                Employee employee = State.Employees[i];
                if (employee.RoleId != "producer" || !employee.ProducerAutoEnabled)
                {
                    continue;
                }

                if (HasActiveProjectManagedBy(employee.Id))
                {
                    continue;
                }

                GenreDto genre = PickGenreForProducer();
                if (genre == null)
                {
                    continue;
                }

                TryStartProject(genre.id, employee.Id, out _);
            }
        }

        GenreDto PickGenreForProducer()
        {
            IReadOnlyList<GenreDto> unlocked = UnlockedGenres();
            GenreDto best = null;
            for (int i = 0; i < unlocked.Count; i++)
            {
                GenreDto genre = unlocked[i];
                if (GenreInProgress(genre.id))
                {
                    continue;
                }

                if (!HasEngine(genre.engineId))
                {
                    continue;
                }

                if (best == null || genre.tier < best.tier ||
                    (genre.tier == best.tier && genre.basePayout < best.basePayout))
                {
                    best = genre;
                }
            }

            return best;
        }

        bool GenreInProgress(string genreId)
        {
            for (int i = 0; i < State.Projects.Count; i++)
            {
                Project project = State.Projects[i];
                if (project.GenreId == genreId &&
                    (project.Status == ProjectStatus.InDev || project.Status == ProjectStatus.Ready))
                {
                    return true;
                }
            }

            return false;
        }

        void SimulateWork(float hours)
        {
            if (!IsWorkHour)
            {
                return;
            }

            for (int i = 0; i < State.Employees.Count; i++)
            {
                Employee employee = State.Employees[i];
                if (employee.StunnedHoursLeft > 0)
                {
                    employee.StunnedHoursLeft = Mathf.Max(0, employee.StunnedHoursLeft - Mathf.RoundToInt(hours));
                    continue;
                }

                if (employee.RoleId == "producer" || employee.RoleId == "director")
                {
                    continue;
                }

                Project project = FindProject(employee.AssignedProjectId);
                if (project == null || project.Status == ProjectStatus.Banned || project.Status == ProjectStatus.Cancelled)
                {
                    continue;
                }

                if (project.Status == ProjectStatus.Live)
                {
                    continue;
                }

                WorkTrack track = FindIncompleteTrack(project, employee.RoleId);
                if (employee.Activity != EmployeeActivity.Working)
                {
                    continue;
                }

                if (!CanWork(employee, out _))
                {
                    continue;
                }

                int minSkill = track != null ? track.MinSkill : 2;
                float points = hours *
                               Productivity.Curve(Data.productivity, employee.DaysOnAssignment) *
                               Productivity.SkillFactor(employee.Skill, minSkill) *
                               Productivity.NeedsFactor(Data.needs, employee) *
                               EquipmentMultiplier(employee) *
                               SoftwareMultiplier(employee.RoleId);

                if (track != null)
                {
                    track.Current = Mathf.Min(track.Required, track.Current + points);
                    continue;
                }

                if (project.Status == ProjectStatus.Ready)
                {
                    float cap = MaxPolish();
                    project.Polish = Mathf.Min(cap, project.Polish + points * PolishPerHour());
                }
            }
        }

        void UpdateNeeds(float hours)
        {
            NeedsConfigDto needs = Data.needs;
            for (int i = 0; i < State.Employees.Count; i++)
            {
                Employee employee = State.Employees[i];
                float max = needs.maxValue;
                if (employee.Activity == EmployeeActivity.Working)
                {
                    employee.Energy = Mathf.Max(0f, employee.Energy - needs.energyDecayPerWorkHour * hours);
                    employee.Mood = Mathf.Max(0f, employee.Mood - needs.moodDecayPerWorkHour * hours);
                    employee.Bladder = Mathf.Min(max, employee.Bladder + needs.bladderGainPerWorkHour * hours);
                }
                else if (employee.Activity == EmployeeActivity.Idle || employee.Activity == EmployeeActivity.Walking)
                {
                    employee.Energy = Mathf.Min(max, employee.Energy + needs.idleRestEnergyPerHour * hours * 0.35f);
                    employee.Mood = Mathf.Min(max, employee.Mood + needs.moodRestorePerIdleHour * hours);
                    employee.Bladder = Mathf.Min(max, employee.Bladder + needs.bladderGainPerIdleHour * hours);
                }
            }
        }

        void CheckReadyProjects()
        {
            for (int i = 0; i < State.Projects.Count; i++)
            {
                Project project = State.Projects[i];
                if (project.Status == ProjectStatus.InDev && AllTracksComplete(project))
                {
                    project.Status = ProjectStatus.Ready;
                    GameEvents.RaiseToast("Готово к релизу: " + project.Name + ". Можно полировать или выпустить.");
                }
            }
        }

        void MaybeFireIncident(float hours)
        {
            State.HoursSinceIncidentCheck += hours;
            if (State.HoursSinceIncidentCheck < Data.incidentSettings.checkEveryHours)
            {
                return;
            }

            State.HoursSinceIncidentCheck = 0f;
            float chance = Data.incidentSettings.baseChancePerCheck * (1f + State.PirateHeat);
            if (_rng.NextDouble() > chance)
            {
                return;
            }

            IncidentDto picked = PickIncident();
            if (picked == null)
            {
                return;
            }

            PresentIncident(picked);
        }

        IncidentDto PickIncident()
        {
            int total = 0;
            var eligible = new List<IncidentDto>();
            for (int i = 0; i < Data.incidents.Length; i++)
            {
                IncidentDto incident = Data.incidents[i];
                if (OnCooldown(incident.id) || !ConditionsMet(incident.conditions))
                {
                    continue;
                }

                eligible.Add(incident);
                total += Mathf.Max(1, incident.weight);
            }

            if (total <= 0)
            {
                return null;
            }

            int roll = _rng.Next(total);
            for (int i = 0; i < eligible.Count; i++)
            {
                roll -= Mathf.Max(1, eligible[i].weight);
                if (roll < 0)
                {
                    return eligible[i];
                }
            }

            return eligible[eligible.Count - 1];
        }

        bool ConditionsMet(string[] conditions)
        {
            if (conditions == null)
            {
                return true;
            }

            for (int i = 0; i < conditions.Length; i++)
            {
                if (!ConditionMet(conditions[i]))
                {
                    return false;
                }
            }

            return true;
        }

        bool ConditionMet(string condition)
        {
            if (string.IsNullOrEmpty(condition))
            {
                return true;
            }

            if (condition == "projectInDev")
            {
                return FindFirst(p => p.Status == ProjectStatus.InDev) != null;
            }

            if (condition == "hasLiveProject")
            {
                return FindFirst(p => p.Status == ProjectStatus.Live) != null;
            }

            if (condition == "hasPiratedSoftware" || condition == "hasPiratedLicense")
            {
                for (int i = 0; i < State.Software.Count; i++)
                {
                    if (State.Software[i].Pirated)
                    {
                        return true;
                    }
                }

                if (condition == "hasPiratedLicense")
                {
                    for (int i = 0; i < State.Engines.Count; i++)
                    {
                        if (State.Engines[i].Pirated)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            if (condition == "hasAssignedStaff")
            {
                for (int i = 0; i < State.Employees.Count; i++)
                {
                    if (State.Employees[i].AssignedProjectId != 0)
                    {
                        return true;
                    }
                }

                return false;
            }

            if (condition.StartsWith("hasRole:"))
            {
                string roleId = condition.Substring("hasRole:".Length);
                for (int i = 0; i < State.Employees.Count; i++)
                {
                    if (State.Employees[i].RoleId == roleId)
                    {
                        return true;
                    }
                }

                return false;
            }

            if (condition.StartsWith("employeeCountAtLeast:"))
            {
                int need = int.Parse(condition.Substring("employeeCountAtLeast:".Length),
                    System.Globalization.CultureInfo.InvariantCulture);
                return State.Employees.Count >= need;
            }

            if (condition.StartsWith("progressAtLeast:"))
            {
                float need = float.Parse(condition.Substring("progressAtLeast:".Length),
                    System.Globalization.CultureInfo.InvariantCulture);
                Project project = FindFirst(p => p.Status == ProjectStatus.InDev);
                return project != null && AverageProgress(project) >= need;
            }

            return true;
        }

        void PresentIncident(IncidentDto incident)
        {
            Project project;
            Employee employee;
            PickIncidentTargets(incident, out project, out employee);
            string body = incident.body;
            if (string.IsNullOrEmpty(body))
            {
                body = incident.displayName;
            }

            string projectName = project != null ? project.Name : "проект";
            string employeeName = employee != null ? employee.Name : "кто-то из команды";
            string roleName = "сотрудник";
            if (employee != null)
            {
                RoleDto role = Data.FindRole(employee.RoleId);
                roleName = role != null ? role.displayName : employee.RoleId;
            }

            body = body.Replace("{project}", projectName)
                .Replace("{employee}", employeeName)
                .Replace("{role}", roleName);

            var log = new IncidentLog
            {
                Id = incident.id,
                Title = incident.displayName,
                Body = body,
                ProjectId = project != null ? project.Id : 0,
                EmployeeId = employee != null ? employee.Id : 0
            };
            State.PendingIncident = log;
            GameEvents.RaiseIncident(log);
        }

        void PickIncidentTargets(IncidentDto incident, out Project project, out Employee employee)
        {
            project = null;
            employee = null;
            string[] conditions = incident.conditions;
            bool wantLive = HasCondition(conditions, "hasLiveProject");
            bool wantDev = HasCondition(conditions, "projectInDev");
            if (wantLive)
            {
                project = FindFirst(p => p.Status == ProjectStatus.Live && p.UsedPirate) ??
                          FindFirst(p => p.Status == ProjectStatus.Live);
            }
            else if (wantDev)
            {
                project = FindFirst(p => p.Status == ProjectStatus.InDev);
            }

            if (project == null)
            {
                project = FindFirst(p => p.Status == ProjectStatus.InDev || p.Status == ProjectStatus.Ready ||
                                        p.Status == ProjectStatus.Live);
            }

            string roleId = RoleFromConditions(conditions);
            if (!string.IsNullOrEmpty(roleId))
            {
                employee = RandomEmployee(e => e.RoleId == roleId);
            }

            if (employee == null && project != null)
            {
                employee = RandomEmployee(e => e.AssignedProjectId == project.Id);
            }

            if (employee == null)
            {
                employee = RandomEmployee(e => e.AssignedProjectId != 0) ?? RandomEmployee(_ => true);
            }
        }

        static bool HasCondition(string[] conditions, string value)
        {
            if (conditions == null)
            {
                return false;
            }

            for (int i = 0; i < conditions.Length; i++)
            {
                if (conditions[i] == value)
                {
                    return true;
                }
            }

            return false;
        }

        static string RoleFromConditions(string[] conditions)
        {
            if (conditions == null)
            {
                return null;
            }

            for (int i = 0; i < conditions.Length; i++)
            {
                if (conditions[i] != null && conditions[i].StartsWith("hasRole:"))
                {
                    return conditions[i].Substring("hasRole:".Length);
                }
            }

            return null;
        }

        Employee RandomEmployee(System.Predicate<Employee> match)
        {
            var list = new List<Employee>();
            for (int i = 0; i < State.Employees.Count; i++)
            {
                if (match(State.Employees[i]))
                {
                    list.Add(State.Employees[i]);
                }
            }

            if (list.Count == 0)
            {
                return null;
            }

            return list[_rng.Next(list.Count)];
        }

        string ApplyEffects(IncidentEffectDto[] effects, int projectId, int employeeId)
        {
            if (effects == null)
            {
                return null;
            }

            var body = new StringBuilder();
            for (int i = 0; i < effects.Length; i++)
            {
                IncidentEffectDto effect = effects[i];
                if (!string.IsNullOrEmpty(effect.text))
                {
                    if (body.Length > 0)
                    {
                        body.Append('\n');
                    }

                    body.Append(effect.text);
                }

                switch (effect.type)
                {
                    case "loseProgress":
                        LoseProgress(projectId, effect.roleId, effect.percent);
                        break;
                    case "addWorkPoints":
                        AddWorkPoints(projectId, effect.roleId, effect.points);
                        break;
                    case "banLiveProject":
                        BanLiveProject(projectId);
                        break;
                    case "fine":
                        State.Money -= effect.amount;
                        break;
                    case "grantMoney":
                        State.Money += effect.amount;
                        break;
                    case "addPirateHeat":
                        State.PirateHeat = Mathf.Max(0f, State.PirateHeat + effect.percent);
                        break;
                    case "stunEmployee":
                        StunRandomEmployee(effect.hours);
                        break;
                    case "stunTarget":
                        StunEmployee(employeeId, effect.hours);
                        break;
                    case "moodAll":
                        ShiftNeedsAll(effect.amount, 0);
                        break;
                    case "energyAll":
                        ShiftNeedsAll(0, effect.amount);
                        break;
                    case "moodTarget":
                        ShiftNeeds(employeeId, effect.amount, 0);
                        break;
                    case "energyTarget":
                        ShiftNeeds(employeeId, 0, effect.amount);
                        break;
                    case "polishHit":
                        HitPolish(projectId, effect.percent);
                        break;
                    case "qualityHit":
                        HitQuality(projectId, effect.percent);
                        break;
                    case "legalizeOne":
                        LegalizeOne();
                        break;
                    case "fireTarget":
                        FireEmployee(employeeId);
                        break;
                }
            }

            return body.Length > 0 ? body.ToString() : null;
        }

        bool OnCooldown(string incidentId)
        {
            for (int i = 0; i < State.IncidentCooldowns.Count; i++)
            {
                IncidentCooldown cooldown = State.IncidentCooldowns[i];
                if (cooldown.Id == incidentId)
                {
                    return State.Day < cooldown.AvailableOnDay;
                }
            }

            return false;
        }

        void StartCooldown(IncidentDto incident)
        {
            if (incident == null)
            {
                return;
            }

            int days = incident.cooldownDays > 0 ? incident.cooldownDays : 3;
            for (int i = 0; i < State.IncidentCooldowns.Count; i++)
            {
                if (State.IncidentCooldowns[i].Id == incident.id)
                {
                    State.IncidentCooldowns[i].AvailableOnDay = State.Day + days;
                    return;
                }
            }

            State.IncidentCooldowns.Add(new IncidentCooldown
            {
                Id = incident.id,
                AvailableOnDay = State.Day + days
            });
        }

        void LoseProgress(int projectId, string roleId, float percent)
        {
            Project project = projectId != 0 ? FindProject(projectId) : FindFirst(p => p.Status == ProjectStatus.InDev);
            if (project == null)
            {
                return;
            }

            for (int i = 0; i < project.Tracks.Count; i++)
            {
                WorkTrack track = project.Tracks[i];
                if (!string.IsNullOrEmpty(roleId) && track.RoleId != roleId)
                {
                    continue;
                }

                track.Current = Mathf.Max(0f, track.Current * (1f - percent));
            }

            if (project.Status == ProjectStatus.Ready)
            {
                project.Status = ProjectStatus.InDev;
            }
        }

        void AddWorkPoints(int projectId, string roleId, float points)
        {
            Project project = projectId != 0 ? FindProject(projectId) : FindFirst(p => p.Status == ProjectStatus.InDev);
            if (project == null)
            {
                return;
            }

            for (int i = 0; i < project.Tracks.Count; i++)
            {
                if (project.Tracks[i].RoleId == roleId)
                {
                    project.Tracks[i].Required += points;
                    project.Status = ProjectStatus.InDev;
                }
            }
        }

        void BanLiveProject(int projectId)
        {
            Project project = projectId != 0 ? FindProject(projectId) : null;
            if (project == null || project.Status != ProjectStatus.Live)
            {
                project = FindFirst(p => p.Status == ProjectStatus.Live && p.UsedPirate) ??
                          FindFirst(p => p.Status == ProjectStatus.Live);
            }

            if (project == null)
            {
                return;
            }

            project.Status = ProjectStatus.Banned;
            project.RevenueDaysLeft = 0;
        }

        void HitPolish(int projectId, float percent)
        {
            Project project = projectId != 0 ? FindProject(projectId) : FindFirst(p => p.Status == ProjectStatus.InDev || p.Status == ProjectStatus.Ready);
            if (project == null)
            {
                return;
            }

            project.Polish = Mathf.Max(0f, project.Polish * (1f - Mathf.Clamp01(percent)));
        }

        void HitQuality(int projectId, float percent)
        {
            Project project = projectId != 0 ? FindProject(projectId) : FindFirst(p => p.Status == ProjectStatus.Live);
            if (project == null)
            {
                return;
            }

            project.Quality = Mathf.Max(0.4f, project.Quality * (1f - Mathf.Clamp01(percent)));
        }

        void ShiftNeedsAll(int moodDelta, int energyDelta)
        {
            for (int i = 0; i < State.Employees.Count; i++)
            {
                ShiftNeeds(State.Employees[i].Id, moodDelta, energyDelta);
            }
        }

        void ShiftNeeds(int employeeId, int moodDelta, int energyDelta)
        {
            Employee employee = FindEmployee(employeeId);
            if (employee == null)
            {
                return;
            }

            float max = Data.needs != null ? Data.needs.maxValue : 100f;
            employee.Mood = Mathf.Clamp(employee.Mood + moodDelta, 0f, max);
            employee.Energy = Mathf.Clamp(employee.Energy + energyDelta, 0f, max);
        }

        void StunEmployee(int employeeId, int hours)
        {
            Employee employee = FindEmployee(employeeId);
            if (employee == null)
            {
                StunRandomEmployee(hours);
                return;
            }

            employee.StunnedHoursLeft = Mathf.Max(1, hours);
        }

        void LegalizeOne()
        {
            for (int i = 0; i < State.Software.Count; i++)
            {
                if (State.Software[i].Pirated)
                {
                    State.Software[i].Pirated = false;
                    SoftwareDto item = Data.FindSoftware(State.Software[i].SoftwareId);
                    GameEvents.RaiseToast("Обелили: " + (item != null ? item.displayName : State.Software[i].SoftwareId));
                    return;
                }
            }

            for (int i = 0; i < State.Engines.Count; i++)
            {
                if (State.Engines[i].Pirated)
                {
                    State.Engines[i].Pirated = false;
                    EngineDto item = Data.FindEngine(State.Engines[i].EngineId);
                    GameEvents.RaiseToast("Обелили: " + (item != null ? item.displayName : State.Engines[i].EngineId));
                    return;
                }
            }
        }

        void FireEmployee(int employeeId)
        {
            Employee employee = FindEmployee(employeeId);
            if (employee == null)
            {
                return;
            }

            if (employee.DeskId != 0)
            {
                DeskSlot desk = FindDesk(employee.DeskId);
                if (desk != null && desk.OccupiedByEmployeeId == employee.Id)
                {
                    desk.OccupiedByEmployeeId = 0;
                }
            }

            if (!string.IsNullOrEmpty(employee.FacilityId))
            {
                Facility facility = FindFacility(employee.FacilityId);
                if (facility != null && facility.OccupiedByEmployeeId == employee.Id)
                {
                    facility.OccupiedByEmployeeId = 0;
                }
            }

            State.Employees.Remove(employee);
        }

        void StunRandomEmployee(int hours)
        {
            var working = new List<Employee>();
            for (int i = 0; i < State.Employees.Count; i++)
            {
                if (State.Employees[i].AssignedProjectId != 0)
                {
                    working.Add(State.Employees[i]);
                }
            }

            if (working.Count == 0)
            {
                return;
            }

            working[_rng.Next(working.Count)].StunnedHoursLeft = Mathf.Max(1, hours);
        }

        public bool CanWork(Employee employee, out string reason)
        {
            reason = null;
            if (employee.DeskId == 0)
            {
                reason = "нет рабочего места";
                return false;
            }

            if (!HasSoftwareForRole(employee.RoleId))
            {
                RoleDto role = Data.FindRole(employee.RoleId);
                reason = "нет софта (" + (role != null ? role.displayName : employee.RoleId) + ")";
                return false;
            }

            if (employee.Activity != EmployeeActivity.Working)
            {
                reason = EmployeeAi.ActivityLabel(employee.Activity);
                return false;
            }

            return true;
        }

        public string StaffingHint(Project project)
        {
            var builder = new StringBuilder();
            for (int t = 0; t < project.Tracks.Count; t++)
            {
                WorkTrack track = project.Tracks[t];
                RoleDto role = Data.FindRole(track.RoleId);
                string roleName = role != null ? role.displayName : track.RoleId;
                int assigned = CountAssigned(project.Id, track.RoleId);
                if (assigned == 0)
                {
                    AppendHint(builder, "нужен " + roleName);
                }

                if (!HasSoftwareForRole(track.RoleId))
                {
                    AppendHint(builder, "нужен софт: " + roleName);
                }
            }

            for (int t = 0; t < project.OptionalTracks.Count; t++)
            {
                WorkTrack track = project.OptionalTracks[t];
                if (track.Complete)
                {
                    continue;
                }

                RoleDto role = Data.FindRole(track.RoleId);
                string roleName = role != null ? role.displayName : track.RoleId;
                if (CountAssigned(project.Id, track.RoleId) == 0)
                {
                    AppendHint(builder, roleName + " даст бонус к оценке");
                }

                if (!HasSoftwareForRole(track.RoleId))
                {
                    AppendHint(builder, "нужен софт: " + roleName);
                }
            }

            int freeDesks = 0;
            for (int i = 0; i < State.Desks.Count; i++)
            {
                if (State.Desks[i].HasWorkstation)
                {
                    freeDesks++;
                }
            }

            if (freeDesks == 0)
            {
                AppendHint(builder, "купите оборудование");
            }

            for (int i = 0; i < State.Employees.Count; i++)
            {
                Employee employee = State.Employees[i];
                if (employee.AssignedProjectId != project.Id)
                {
                    continue;
                }

                if (employee.Activity != EmployeeActivity.Working && employee.Activity != EmployeeActivity.Idle)
                {
                    AppendHint(builder, employee.Name + " " + EmployeeAi.ActivityLabel(employee.Activity));
                }
            }

            return builder.ToString();
        }

        static void AppendHint(StringBuilder builder, string text)
        {
            if (builder.Length > 0)
            {
                builder.Append(" · ");
            }

            builder.Append(text);
        }

        public void RefreshHireMarket()
        {
            State.HireMarket.Clear();
            int size = Mathf.Max(1, Data.studio.hireMarketSize);
            for (int i = 0; i < size; i++)
            {
                RoleDto role = PickHireRole(i);
                int skill = _rng.Next(role.skillMin, role.skillMax + 1);
                float skillFactor = 0.7f + 0.12f * skill;
                State.HireMarket.Add(new Candidate
                {
                    Id = NextId(),
                    Name = RandomName(),
                    RoleId = role.id,
                    Skill = skill,
                    HireCost = Mathf.RoundToInt(role.hireCostBase * skillFactor),
                    SalaryPerDay = Mathf.RoundToInt(role.salaryBase * skillFactor)
                });
            }
        }

        RoleDto PickHireRole(int index)
        {
            if (State.TotalReleases == 0 && index < 2)
            {
                return Data.FindRole(index == 0 ? "programmer" : "artist");
            }

            int size = Mathf.Max(1, Data.studio.hireMarketSize);
            if (!HasEmployeeRole("producer") && State.TotalReleases >= ProducerUnlockReleases() &&
                index == size - 1)
            {
                RoleDto producer = Data.FindRole("producer");
                if (producer != null)
                {
                    return producer;
                }
            }

            var pool = new List<RoleDto>();
            for (int i = 0; i < Data.roles.Length; i++)
            {
                RoleDto role = Data.roles[i];
                if (IsRoleRelevant(role.id))
                {
                    pool.Add(role);
                }
            }

            if (pool.Count == 0)
            {
                return Data.roles[0];
            }

            return pool[_rng.Next(pool.Count)];
        }

        bool HasEmployeeRole(string roleId)
        {
            for (int i = 0; i < State.Employees.Count; i++)
            {
                if (State.Employees[i].RoleId == roleId)
                {
                    return true;
                }
            }

            return false;
        }

        bool IsRoleRelevant(string roleId)
        {
            if (roleId == "producer")
            {
                return State.TotalReleases >= ProducerUnlockReleases();
            }

            if (roleId == "director")
            {
                return State.TotalReleases >= ProducerUnlockReleases() + 1;
            }

            IReadOnlyList<GenreDto> unlocked = UnlockedGenres();
            for (int i = 0; i < unlocked.Count; i++)
            {
                if (GenreHasRole(unlocked[i], roleId))
                {
                    return true;
                }
            }

            return false;
        }

        static bool GenreHasRole(GenreDto genre, string roleId)
        {
            if (genre.tracks != null)
            {
                for (int t = 0; t < genre.tracks.Length; t++)
                {
                    if (genre.tracks[t].roleId == roleId)
                    {
                        return true;
                    }
                }
            }

            if (genre.optionalTracks != null)
            {
                for (int t = 0; t < genre.optionalTracks.Length; t++)
                {
                    if (genre.optionalTracks[t].roleId == roleId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        void AutoAssignIdleStaff(Project project)
        {
            for (int i = 0; i < State.Employees.Count; i++)
            {
                Employee employee = State.Employees[i];
                if (employee.AssignedProjectId != 0)
                {
                    continue;
                }

                if (ProjectNeedsRole(project, employee.RoleId))
                {
                    employee.AssignedProjectId = project.Id;
                    employee.DaysOnAssignment = 0f;
                }
            }
        }

        void AutoAssignEmployee(Employee employee)
        {
            if (employee.RoleId == "producer" || employee.RoleId == "director")
            {
                return;
            }

            int needy = 0;
            Project target = null;
            for (int i = 0; i < State.Projects.Count; i++)
            {
                Project project = State.Projects[i];
                if (project.Status != ProjectStatus.InDev)
                {
                    continue;
                }

                if (ProjectNeedsRole(project, employee.RoleId) && CountAssigned(project.Id, employee.RoleId) == 0)
                {
                    needy++;
                    target = project;
                }
            }

            if (needy == 1 && target != null)
            {
                employee.AssignedProjectId = target.Id;
                employee.DaysOnAssignment = 0f;
                return;
            }

            if (needy > 1)
            {
                GameEvents.RaiseToast(employee.Name + " свободен. Назначьте через Команду.");
            }
        }

        void SeatWaitingEmployees()
        {
            for (int i = 0; i < State.Employees.Count; i++)
            {
                if (State.Employees[i].DeskId == 0)
                {
                    TrySeatEmployee(State.Employees[i]);
                }
            }
        }

        void TrySeatEmployee(Employee employee)
        {
            for (int i = 0; i < State.Desks.Count; i++)
            {
                DeskSlot desk = State.Desks[i];
                if (!desk.HasWorkstation || desk.OccupiedByEmployeeId != 0)
                {
                    continue;
                }

                EquipmentDto equipment = Data.FindEquipment(desk.EquipmentId);
                if (equipment != null && equipment.roleIds != null && equipment.roleIds.Length > 0)
                {
                    bool matches = false;
                    for (int r = 0; r < equipment.roleIds.Length; r++)
                    {
                        if (equipment.roleIds[r] == employee.RoleId)
                        {
                            matches = true;
                            break;
                        }
                    }

                    if (!matches)
                    {
                        continue;
                    }
                }

                desk.OccupiedByEmployeeId = employee.Id;
                employee.DeskId = desk.Id;
                return;
            }
        }

        void ResetStaffAssignments(int projectId)
        {
            for (int i = 0; i < State.Employees.Count; i++)
            {
                Employee employee = State.Employees[i];
                if (employee.AssignedProjectId != projectId)
                {
                    continue;
                }

                if (employee.RoleId == "marketer")
                {
                    continue;
                }

                employee.AssignedProjectId = 0;
                employee.DaysOnAssignment = 0f;
            }
        }

        bool HasActiveProjectManagedBy(int producerId)
        {
            for (int i = 0; i < State.Projects.Count; i++)
            {
                Project project = State.Projects[i];
                if (project.ProducerEmployeeId == producerId &&
                    (project.Status == ProjectStatus.InDev || project.Status == ProjectStatus.Ready))
                {
                    return true;
                }
            }

            return false;
        }

        bool HasSoftwareForRole(string roleId)
        {
            if (roleId == "producer" || roleId == "director")
            {
                return true;
            }

            for (int i = 0; i < State.Software.Count; i++)
            {
                if (State.Software[i].RoleId == roleId)
                {
                    return true;
                }
            }

            return false;
        }

        bool ProjectNeedsRole(Project project, string roleId)
        {
            return FindTrack(project, roleId) != null;
        }

        int CountAssigned(int projectId, string roleId)
        {
            int count = 0;
            for (int i = 0; i < State.Employees.Count; i++)
            {
                Employee employee = State.Employees[i];
                if (employee.AssignedProjectId == projectId && employee.RoleId == roleId)
                {
                    count++;
                }
            }

            return count;
        }

        static bool AllTracksComplete(Project project)
        {
            for (int i = 0; i < project.Tracks.Count; i++)
            {
                if (!project.Tracks[i].Complete)
                {
                    return false;
                }
            }

            return true;
        }

        static float AverageProgress(Project project)
        {
            if (project.Tracks.Count == 0)
            {
                return 0f;
            }

            float sum = 0f;
            for (int i = 0; i < project.Tracks.Count; i++)
            {
                sum += project.Tracks[i].Normalized;
            }

            return sum / project.Tracks.Count;
        }

        public bool OwnsSoftwareForRole(string roleId)
        {
            return HasSoftwareForRole(roleId);
        }

        public bool HasEngine(string engineId)
        {
            return FindOwnedEngine(engineId) != null;
        }

        public OwnedEngine FindOwnedEngine(string engineId)
        {
            if (string.IsNullOrEmpty(engineId))
            {
                return null;
            }

            for (int i = 0; i < State.Engines.Count; i++)
            {
                if (State.Engines[i].EngineId == engineId)
                {
                    return State.Engines[i];
                }
            }

            return null;
        }

        public QualityReport PreviewQuality(Project project)
        {
            var report = new QualityReport();
            var breakdown = new StringBuilder();
            float skillSum = 0f;
            int skillCount = 0;
            for (int i = 0; i < State.Employees.Count; i++)
            {
                Employee employee = State.Employees[i];
                if (employee.AssignedProjectId != project.Id)
                {
                    continue;
                }

                WorkTrack track = FindRequiredTrack(project, employee.RoleId);
                if (track == null)
                {
                    continue;
                }

                skillSum += Productivity.SkillFactor(employee.Skill, track.MinSkill);
                skillCount++;
            }

            float skill = skillCount == 0 ? 0.7f : skillSum / skillCount;
            breakdown.Append("скилл команды ×").Append(skill.ToString("0.00"));

            float pirateCut = 0f;
            for (int i = 0; i < State.Software.Count; i++)
            {
                OwnedSoftware owned = State.Software[i];
                if (owned.Pirated && ProjectNeedsRole(project, owned.RoleId))
                {
                    pirateCut += PirateTrackPenalty();
                    report.UsedPirate = true;
                }
            }

            OwnedEngine engine = FindOwnedEngine(project.EngineId);
            if (engine != null && engine.Pirated)
            {
                pirateCut += PirateEnginePenalty();
                report.UsedPirate = true;
            }

            if (project.UsedPirate)
            {
                report.UsedPirate = true;
            }

            if (pirateCut > 0f)
            {
                breakdown.Append("  ·  пират −").Append(pirateCut.ToString("0.00"));
            }

            float extra = project.Polish;
            breakdown.Append("  ·  полировка +").Append(project.Polish.ToString("0.00"));

            if (OptionalComplete(project, "qa"))
            {
                extra += QaBonus();
                breakdown.Append("  ·  QA +").Append(QaBonus().ToString("0.00"));
            }

            if (OptionalComplete(project, "sound_designer"))
            {
                extra += SoundBonus();
                breakdown.Append("  ·  звук +").Append(SoundBonus().ToString("0.00"));
            }

            report.Quality = Mathf.Clamp(skill - pirateCut + extra, 0.4f, 1.55f);
            report.Stars = StarsFromQuality(report.Quality);
            report.Review = PickReview(report.Stars, project.Id);
            report.Breakdown = breakdown.ToString();
            report.Payout = Mathf.RoundToInt(project.BasePayout * report.Quality);
            report.DailyEstimate = Mathf.RoundToInt(project.DailyRevenue * report.Quality * (1f + MarketingBonus(project)));
            return report;
        }

        public float MarketingBonus(Project project)
        {
            float bonus = 0f;
            for (int i = 0; i < State.Employees.Count; i++)
            {
                Employee employee = State.Employees[i];
                if (employee.AssignedProjectId == project.Id && employee.RoleId == "marketer" &&
                    HasSoftwareForRole("marketer"))
                {
                    bonus += 0.12f * Productivity.SkillFactor(employee.Skill, 2);
                }
            }

            return Mathf.Clamp(bonus, 0f, MarketingLiveCap());
        }

        public float MaxPolish()
        {
            QualityConfigDto quality = Data.quality;
            return quality != null && quality.maxPolish > 0f ? quality.maxPolish : 0.22f;
        }

        float PolishPerHour()
        {
            QualityConfigDto quality = Data.quality;
            return quality != null && quality.polishPerWorkHour > 0f ? quality.polishPerWorkHour : 0.025f;
        }

        float PirateTrackPenalty()
        {
            QualityConfigDto quality = Data.quality;
            return quality != null && quality.pirateTrackPenalty > 0f ? quality.pirateTrackPenalty : 0.12f;
        }

        float PirateEnginePenalty()
        {
            QualityConfigDto quality = Data.quality;
            return quality != null && quality.pirateEnginePenalty > 0f ? quality.pirateEnginePenalty : 0.18f;
        }

        float QaBonus()
        {
            QualityConfigDto quality = Data.quality;
            return quality != null && quality.qaBonus > 0f ? quality.qaBonus : 0.08f;
        }

        float SoundBonus()
        {
            QualityConfigDto quality = Data.quality;
            return quality != null && quality.soundBonus > 0f ? quality.soundBonus : 0.06f;
        }

        float MarketingLiveCap()
        {
            QualityConfigDto quality = Data.quality;
            return quality != null && quality.marketingLiveBonus > 0f ? quality.marketingLiveBonus : 0.4f;
        }

        static int StarsFromQuality(float quality)
        {
            if (quality < 0.65f)
            {
                return 1;
            }

            if (quality < 0.85f)
            {
                return 2;
            }

            if (quality < 1.05f)
            {
                return 3;
            }

            if (quality < 1.25f)
            {
                return 4;
            }

            return 5;
        }

        string PickReview(int stars, int projectId)
        {
            string[] pool = stars >= 4 ? Data.reviewsHigh : stars <= 2 ? Data.reviewsLow : Data.reviewsMid;
            if (pool == null || pool.Length == 0)
            {
                return stars >= 4 ? "Стор принял тепло." : stars <= 2 ? "Стор ворчит." : "Средняя оценка.";
            }

            return pool[Mathf.Abs(projectId * 17 + stars) % pool.Length];
        }

        static bool OptionalComplete(Project project, string roleId)
        {
            for (int i = 0; i < project.OptionalTracks.Count; i++)
            {
                if (project.OptionalTracks[i].RoleId == roleId)
                {
                    return project.OptionalTracks[i].Complete;
                }
            }

            return false;
        }

        void StampPirateOnActiveProjects()
        {
            for (int i = 0; i < State.Projects.Count; i++)
            {
                StampPirateUsage(State.Projects[i]);
            }
        }

        void StampPirateUsage(Project project)
        {
            if (project.UsedPirate || project.Status == ProjectStatus.Banned || project.Status == ProjectStatus.Cancelled)
            {
                return;
            }

            OwnedEngine engine = FindOwnedEngine(project.EngineId);
            if (engine != null && engine.Pirated)
            {
                project.UsedPirate = true;
                return;
            }

            for (int i = 0; i < State.Software.Count; i++)
            {
                OwnedSoftware owned = State.Software[i];
                if (owned.Pirated && ProjectNeedsRole(project, owned.RoleId))
                {
                    project.UsedPirate = true;
                    return;
                }
            }
        }

        float EquipmentMultiplier(Employee employee)
        {
            DeskSlot desk = FindDesk(employee.DeskId);
            if (desk == null)
            {
                return 1f;
            }

            EquipmentDto equipment = Data.FindEquipment(desk.EquipmentId);
            return equipment != null ? Mathf.Max(0.1f, equipment.productivity) : 1f;
        }

        float SoftwareMultiplier(string roleId)
        {
            for (int i = 0; i < State.Software.Count; i++)
            {
                if (State.Software[i].RoleId == roleId)
                {
                    return Mathf.Max(0.1f, State.Software[i].Productivity);
                }
            }

            return 1f;
        }

        static WorkTrack FindRequiredTrack(Project project, string roleId)
        {
            for (int i = 0; i < project.Tracks.Count; i++)
            {
                if (project.Tracks[i].RoleId == roleId)
                {
                    return project.Tracks[i];
                }
            }

            return null;
        }

        static WorkTrack FindTrack(Project project, string roleId)
        {
            WorkTrack required = FindRequiredTrack(project, roleId);
            if (required != null)
            {
                return required;
            }

            for (int i = 0; i < project.OptionalTracks.Count; i++)
            {
                if (project.OptionalTracks[i].RoleId == roleId)
                {
                    return project.OptionalTracks[i];
                }
            }

            return null;
        }

        static WorkTrack FindIncompleteTrack(Project project, string roleId)
        {
            WorkTrack required = FindRequiredTrack(project, roleId);
            if (required != null && !required.Complete)
            {
                return required;
            }

            for (int i = 0; i < project.OptionalTracks.Count; i++)
            {
                WorkTrack track = project.OptionalTracks[i];
                if (track.RoleId == roleId && !track.Complete)
                {
                    return track;
                }
            }

            return null;
        }

        Project FindFirst(System.Predicate<Project> match)
        {
            for (int i = 0; i < State.Projects.Count; i++)
            {
                if (match(State.Projects[i]))
                {
                    return State.Projects[i];
                }
            }

            return null;
        }

        public Project FindProject(int id)
        {
            if (id == 0)
            {
                return null;
            }

            for (int i = 0; i < State.Projects.Count; i++)
            {
                if (State.Projects[i].Id == id)
                {
                    return State.Projects[i];
                }
            }

            return null;
        }

        public Employee FindEmployee(int id)
        {
            for (int i = 0; i < State.Employees.Count; i++)
            {
                if (State.Employees[i].Id == id)
                {
                    return State.Employees[i];
                }
            }

            return null;
        }

        Candidate FindCandidate(int id)
        {
            for (int i = 0; i < State.HireMarket.Count; i++)
            {
                if (State.HireMarket[i].Id == id)
                {
                    return State.HireMarket[i];
                }
            }

            return null;
        }

        public DeskSlot FindDesk(int id)
        {
            if (id == 0)
            {
                return null;
            }

            for (int i = 0; i < State.Desks.Count; i++)
            {
                if (State.Desks[i].Id == id)
                {
                    return State.Desks[i];
                }
            }

            return null;
        }

        public Facility FindFacility(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (int i = 0; i < State.Facilities.Count; i++)
            {
                if (State.Facilities[i].Id == id)
                {
                    return State.Facilities[i];
                }
            }

            return null;
        }

        string BuildProjectName(GenreDto genre)
        {
            string adj = Data.projectAdjectives != null && Data.projectAdjectives.Length > 0
                ? Data.projectAdjectives[_rng.Next(Data.projectAdjectives.Length)]
                : "Новый";
            return adj + " " + genre.displayName;
        }

        string RandomName()
        {
            string first = Data.firstNames[_rng.Next(Data.firstNames.Length)];
            string last = Data.lastNames[_rng.Next(Data.lastNames.Length)];
            return first + " " + last;
        }
    }
}
