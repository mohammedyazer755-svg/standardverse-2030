using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace CampusPulse.Simulation
{
    [Serializable]
    public sealed class FacilityState
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Purpose;
        public int Capacity { get; internal set; }
        public int Occupants { get; internal set; }

        public float BaseWaterLpm { get; }
        public float BasePowerKw { get; }
        public float BaseWasteKgPerMin { get; }

        public float WaterLitersPerMinute => BaseWaterLpm;
        public float PowerKW => BasePowerKw;
        public float WasteKG => BaseWasteKgPerMin;

        public float BinCapacityKg { get; internal set; } = 60f;
        public float BinCurrentKg { get; internal set; }
        public float BinOverflowKg { get; internal set; }

        public int QueueCount { get; internal set; }

        public FacilityState(string id, string name, string purpose, int capacity, int occupants,
            float water, float power, float waste)
        {
            if (string.IsNullOrWhiteSpace(id) || capacity <= 0 || occupants < 0 || occupants > capacity)
                throw new ArgumentException("Invalid facility definition: " + id);
            Id = id;
            Name = name;
            Purpose = purpose;
            Capacity = capacity;
            Occupants = occupants;
            BaseWaterLpm = water;
            BasePowerKw = power;
            BaseWasteKgPerMin = waste;
        }

        internal FacilityState DeepClone()
        {
            var copy = new FacilityState(Id, Name, Purpose, Capacity, Occupants, BaseWaterLpm, BasePowerKw, BaseWasteKgPerMin)
            {
                BinCapacityKg = BinCapacityKg,
                BinCurrentKg = BinCurrentKg,
                BinOverflowKg = BinOverflowKg,
                QueueCount = QueueCount
            };
            return copy;
        }
    }

    public sealed class CampusState
    {
        readonly Dictionary<string, FacilityState> facilities = new Dictionary<string, FacilityState>();
        public IReadOnlyDictionary<string, FacilityState> Facilities => facilities;

        readonly List<ActionTicket> tickets = new List<ActionTicket>();
        public IReadOnlyList<ActionTicket> Tickets => tickets;

        readonly Dictionary<ActionKind, int> cooldownUntil = new Dictionary<ActionKind, int>();

        public int Minute { get; private set; } = 8 * 60; // 08:00 AM
        public int Seed { get; }
        public bool Paused { get; set; }

        // Core Economics & Workforce
        public int BudgetINR { get; private set; } = 150000;
        public int StaffTotal { get; } = 6;
        public int StaffAllocated { get; private set; }
        public int StaffAvailable => Math.Max(0, StaffTotal - StaffAllocated);

        // Performance Indices
        public int Satisfaction { get; private set; } = 78;
        public int Sustainability { get; private set; } = 64;

        // --- WATER SUBSYSTEM ---
        public float WaterCapacityLiters { get; } = 50000f;
        public float WaterStoredLiters { get; private set; } = 36000f;
        public bool MainLeakActive { get; internal set; } = true;
        public float MainLeakRateLpm { get; } = 85f;
        public PumpPolicy PumpPolicy { get; internal set; } = PumpPolicy.Standard;
        public float PumpDeliveredLpm { get; private set; }
        public float CurrentWaterDemandLpm { get; private set; }
        public float WaterShortageLiters { get; private set; }
        public float CumulativeWaterPumpedLiters { get; private set; }
        public float CumulativeWaterConsumedLiters { get; private set; }
        public float CumulativeWaterLeakedLiters { get; private set; }
        public float CumulativeWaterShortageLiters { get; private set; }
        public bool LowFlowInstalledAll { get; private set; }
        public int RationingUntilMinute { get; private set; } = -1;
        public int BoostPumpUntilMinute { get; private set; } = -1;

        // --- ELECTRICITY SUBSYSTEM ---
        public float GridCapacityKW { get; } = 380f;
        public float SolarCapacityKW { get; } = 120f;
        public float SolarGenerationKW { get; private set; }
        public bool BackupActive { get; internal set; }
        public float BackupCapacityKW { get; } = 150f;
        public float BackupFuelReserveKWh { get; private set; } = 60f;
        public float BackupDeliveredKW { get; private set; }
        public float TotalPowerDemandKW { get; private set; }
        public float TotalPowerSupplyKW { get; private set; }
        public float PowerSupplyKW => TotalPowerSupplyKW;
        public float PowerDeficitKW { get; private set; }
        public float CumulativeEnergyConsumedKWh { get; private set; }
        public bool LedInstalledAll { get; private set; }
        public bool LabsRescheduled { get; private set; }
        public int LoadSheddingUntilMinute { get; private set; } = -1;

        // --- WASTE SUBSYSTEM ---
        public float SegregationQuality { get; private set; } = 0.25f; // 25% default
        public int CollectionDurationMinutes { get; private set; } = 5;
        public float RecycledWasteKg { get; private set; }
        public float LandfillWasteKg { get; private set; }
        public float TotalBinWasteKg { get; private set; }
        public float TotalOverflowWasteKg { get; private set; }
        public float TotalWasteGeneratedKg => TotalBinWasteKg + TotalOverflowWasteKg + RecycledWasteKg + LandfillWasteKg;
        public bool CollectionVehicleActive { get; private set; }

        // --- CROWD SUBSYSTEM ---
        public int TotalPopulation { get; private set; }
        public bool ExtraCounterActive { get; private set; }
        public bool StaggerArrivalsActive { get; private set; }
        public bool GardenDiversionActive { get; private set; }
        public bool OverflowSeatingActive { get; private set; }

        public CampusState(int seed = 2030)
        {
            Seed = seed;
        }

        public void Add(FacilityState facility)
        {
            if (facility == null || facilities.ContainsKey(facility.Id))
                throw new ArgumentException("Duplicate or null facility: " + facility?.Id);
            facilities.Add(facility.Id, facility);
            TotalPopulation += facility.Occupants;
        }

        public int VisualCount(string id, int cap = 18)
        {
            if (cap < 0) throw new ArgumentOutOfRangeException(nameof(cap));
            return facilities.TryGetValue(id, out var f) ? Math.Min(cap, (f.Occupants + 9) / 10) : 0;
        }

        public bool TrySubmitAction(ActionKind kind, string facilityId, out ActionTicket ticket, out string reason)
        {
            ticket = null;
            var def = ActionCatalog.Get(kind);

            // 1. Check if an active ticket of this kind already exists (idempotency)
            foreach (var t in tickets)
            {
                if (t.Kind == kind && t.Active)
                {
                    reason = $"Action '{def.Name}' is already active/in-progress.";
                    return false;
                }
            }

            // 2. Check cooldown
            if (cooldownUntil.TryGetValue(kind, out int cd) && Minute < cd)
            {
                reason = $"Action '{def.Name}' is on cooldown for {cd - Minute} more minutes.";
                return false;
            }

            // 3. Check permanent upgrades
            if (kind == ActionKind.LowFlow && LowFlowInstalledAll)
            {
                reason = "Low-flow fixtures are already installed across campus.";
                return false;
            }
            if (kind == ActionKind.LEDs && LedInstalledAll)
            {
                reason = "LED lighting retrofit is already completed.";
                return false;
            }
            if (kind == ActionKind.Segregation && SegregationQuality >= 0.70f)
            {
                reason = "Waste segregation upgrade is already active.";
                return false;
            }
            if (kind == ActionKind.CollectionRoute && CollectionDurationMinutes <= 3)
            {
                reason = "Collection route is already optimized to 3 minutes.";
                return false;
            }

            // 4. Check budget & staff constraints
            if (BudgetINR < def.Cost)
            {
                reason = $"Insufficient funds: Requires INR {def.Cost:N0}, available INR {BudgetINR:N0}.";
                return false;
            }
            if (StaffAvailable < def.Staff)
            {
                reason = $"Insufficient maintenance staff: Requires {def.Staff}, available {StaffAvailable}.";
                return false;
            }

            // Accept and reserve resources
            BudgetINR -= def.Cost;
            StaffAllocated += def.Staff;

            ticket = new ActionTicket
            {
                CommandId = Guid.NewGuid().ToString("N").Substring(0, 8),
                FacilityId = facilityId,
                Kind = kind,
                Status = ActionStatus.Accepted,
                Message = $"Scheduled: {def.Name}",
                AcceptedMinute = Minute,
                StartMinute = Minute,
                FinishMinute = Minute + def.Duration,
                ReservedCost = def.Cost,
                ReservedStaff = def.Staff
            };

            tickets.Add(ticket);
            reason = "Accepted";
            return true;
        }

        public bool TryCancelAction(string commandId, out string reason)
        {
            for (int i = 0; i < tickets.Count; i++)
            {
                var t = tickets[i];
                if (t.CommandId == commandId)
                {
                    if (t.Status == ActionStatus.Accepted && Minute == t.AcceptedMinute)
                    {
                        // Unstarted refund
                        BudgetINR += t.ReservedCost;
                        StaffAllocated -= t.ReservedStaff;
                        t.Status = ActionStatus.Cancelled;
                        t.Message = "Cancelled with full refund.";
                        reason = "Action cancelled and refunded.";
                        return true;
                    }
                    reason = "Action has already commenced; non-refundable.";
                    return false;
                }
            }
            reason = "Command not found.";
            return false;
        }

        public void Tick()
        {
            if (Paused) return;

            // 1. Process Actions and Ticket Transitions
            ProcessTickets();

            // 2. Solar Generation & Power Flow
            ComputeElectricity();

            // 3. Water Supply, Demand, Pumping & Leaks
            ComputeWater();

            // 4. Waste Generation, Bins & Collection
            ComputeWaste();

            // 5. Crowd Flow & Conserved Population Movement
            ComputeCrowds();

            // 6. Overall Satisfaction & Sustainability Metrics
            ComputeIndices();

            Minute++;
        }

        void ProcessTickets()
        {
            foreach (var t in tickets)
            {
                if (!t.Active) continue;

                if (t.Status == ActionStatus.Accepted)
                {
                    t.Status = ActionStatus.InProgress;
                }

                if (Minute >= t.FinishMinute)
                {
                    t.Status = ActionStatus.Completed;
                    StaffAllocated -= t.ReservedStaff; // Release staff

                    var def = ActionCatalog.Get(t.Kind);
                    if (def.Cooldown > 0)
                        cooldownUntil[t.Kind] = Minute + def.Cooldown;

                    ApplyActionEffect(t);
                }
            }

            // Clean expired duration buffs
            if (RationingUntilMinute > 0 && Minute >= RationingUntilMinute) RationingUntilMinute = -1;
            if (BoostPumpUntilMinute > 0 && Minute >= BoostPumpUntilMinute)
            {
                BoostPumpUntilMinute = -1;
                PumpPolicy = PumpPolicy.Standard;
            }
            if (LoadSheddingUntilMinute > 0 && Minute >= LoadSheddingUntilMinute) LoadSheddingUntilMinute = -1;
        }

        void ApplyActionEffect(ActionTicket ticket)
        {
            switch (ticket.Kind)
            {
                case ActionKind.RepairLeak:
                    MainLeakActive = false;
                    ticket.Message = "Pipe leak repaired successfully. Main loss stopped.";
                    break;
                case ActionKind.LowFlow:
                    LowFlowInstalledAll = true;
                    ticket.Message = "Low-flow fixtures installed campus-wide (-35% water demand).";
                    break;
                case ActionKind.Ration:
                    RationingUntilMinute = Minute + 30;
                    ticket.Message = "Emergency water rationing active for 30 minutes (-40% consumption).";
                    break;
                case ActionKind.BoostPump:
                    BoostPumpUntilMinute = Minute + 30;
                    PumpPolicy = PumpPolicy.Boost;
                    ticket.Message = "Boost pumping active for 30 minutes (300 L/min requested).";
                    break;
                case ActionKind.SchedulePump:
                    PumpPolicy = PumpPolicy.OffPeak;
                    ticket.Message = "Off-peak pumping schedule applied.";
                    break;
                case ActionKind.RescheduleLabs:
                    LabsRescheduled = true;
                    ticket.Message = "Laboratory heavy equipment shifted to off-peak slots.";
                    break;
                case ActionKind.LEDs:
                    LedInstalledAll = true;
                    ticket.Message = "Campus LED retrofit complete (-25% power demand).";
                    break;
                case ActionKind.Backup:
                    BackupActive = true;
                    ticket.Message = "Emergency backup generator engaged.";
                    break;
                case ActionKind.ReduceLoad:
                    LoadSheddingUntilMinute = Minute + 25;
                    ticket.Message = "Nonessential load shed for 25 minutes (-25% demand).";
                    break;
                case ActionKind.CollectWaste:
                    ExecuteWasteCollection();
                    ticket.Message = "Campus waste collection round completed.";
                    break;
                case ActionKind.Segregation:
                    SegregationQuality = 0.75f;
                    ticket.Message = "Waste segregation protocol upgraded to 75% recovery.";
                    break;
                case ActionKind.MoreBins:
                    foreach (var f in facilities.Values) f.BinCapacityKg += 40f;
                    ticket.Message = "Bin capacity expanded by +40 kg at every facility.";
                    break;
                case ActionKind.CollectionRoute:
                    CollectionDurationMinutes = 3;
                    ticket.Message = "Waste collection route optimized to 3 minutes.";
                    break;
                case ActionKind.Stagger:
                    StaggerArrivalsActive = true;
                    ticket.Message = "Lunch arrival schedule staggered across departments.";
                    break;
                case ActionKind.Reroute:
                    GardenDiversionActive = true;
                    ticket.Message = "Students redirected toward reflection garden.";
                    break;
                case ActionKind.ExtraCounter:
                    ExtraCounterActive = true;
                    ticket.Message = "Additional service counter operational in canteen.";
                    break;
                case ActionKind.MoreCapacity:
                    OverflowSeatingActive = true;
                    if (facilities.TryGetValue("canteen", out var canteen)) canteen.Capacity += 80;
                    ticket.Message = "Overflow seating opened (+80 canteen capacity).";
                    break;
            }
        }

        void ComputeElectricity()
        {
            // Solar Curve: Day minute 360 (06:00) to 1080 (18:00)
            int dayMinute = Minute % 1440;
            if (dayMinute >= 360 && dayMinute <= 1080)
            {
                float solarPhase = (dayMinute - 360) / 720f * (float)Math.PI;
                SolarGenerationKW = (float)Math.Sin(solarPhase) * SolarCapacityKW;
            }
            else
            {
                SolarGenerationKW = 0f;
            }

            // Building Loads
            float facilityDemand = 0f;
            foreach (var f in facilities.Values)
            {
                float p = f.BasePowerKw;
                if (LedInstalledAll) p *= 0.75f;
                if (LoadSheddingUntilMinute > 0) p *= 0.75f;
                if (f.Id == "research" && LabsRescheduled && dayMinute >= 480 && dayMinute <= 600) p *= 0.40f;
                facilityDemand += p;
            }

            if (ExtraCounterActive) facilityDemand += 28f;

            // Pump Power Demand
            float pumpPowerDemand = 0f;
            bool isOffPeakRestricted = PumpPolicy == PumpPolicy.OffPeak && dayMinute >= 480 && dayMinute <= 600;
            if (!isOffPeakRestricted)
            {
                pumpPowerDemand = PumpPolicy == PumpPolicy.Boost ? 35f : 18f;
            }

            TotalPowerDemandKW = facilityDemand + pumpPowerDemand;

            // Backup Generator Delivery
            BackupDeliveredKW = 0f;
            if (BackupActive && BackupFuelReserveKWh > 0.05f)
            {
                BackupDeliveredKW = Math.Min(BackupCapacityKW, Math.Max(0f, TotalPowerDemandKW - (GridCapacityKW + SolarGenerationKW)));
                float energyUsedThisTick = BackupDeliveredKW / 60f;
                BackupFuelReserveKWh = Math.Max(0f, BackupFuelReserveKWh - energyUsedThisTick);
                if (BackupFuelReserveKWh <= 0.001f) BackupActive = false;
            }

            TotalPowerSupplyKW = GridCapacityKW + SolarGenerationKW + BackupDeliveredKW;
            PowerDeficitKW = Math.Max(0f, TotalPowerDemandKW - TotalPowerSupplyKW);

            float deliveredPower = Math.Min(TotalPowerDemandKW, TotalPowerSupplyKW);
            CumulativeEnergyConsumedKWh += deliveredPower / 60f;
        }

        void ComputeWater()
        {
            // Pumping capacity (L/min)
            int dayMinute = Minute % 1440;
            bool isOffPeakRestricted = PumpPolicy == PumpPolicy.OffPeak && dayMinute >= 480 && dayMinute <= 600;

            if (PowerDeficitKW > 20f || isOffPeakRestricted)
            {
                // Unpowered pump cannot pump!
                PumpDeliveredLpm = 0f;
            }
            else
            {
                float targetRate = PumpPolicy == PumpPolicy.Boost ? 300f : 160f;
                float roomInStorage = WaterCapacityLiters - WaterStoredLiters;
                PumpDeliveredLpm = Math.Max(0f, Math.Min(targetRate, roomInStorage));
            }

            // Facility Water Demand
            float demand = 0f;
            foreach (var f in facilities.Values)
            {
                float w = f.BaseWaterLpm;
                if (LowFlowInstalledAll) w *= 0.65f;
                if (RationingUntilMinute > 0) w *= 0.60f;
                demand += w;
            }
            CurrentWaterDemandLpm = demand;

            float leak = MainLeakActive ? MainLeakRateLpm : 0f;

            // Apply storage delta
            float totalOutflow = CurrentWaterDemandLpm + leak;
            WaterStoredLiters += PumpDeliveredLpm;

            if (WaterStoredLiters >= totalOutflow)
            {
                WaterStoredLiters -= totalOutflow;
                WaterShortageLiters = 0f;
            }
            else
            {
                float available = WaterStoredLiters;
                WaterStoredLiters = 0f;
                WaterShortageLiters = totalOutflow - available;
            }

            CumulativeWaterPumpedLiters += PumpDeliveredLpm;
            CumulativeWaterConsumedLiters += Math.Min(totalOutflow - leak, CurrentWaterDemandLpm);
            CumulativeWaterLeakedLiters += leak;
            CumulativeWaterShortageLiters += WaterShortageLiters;
        }

        void ComputeWaste()
        {
            TotalBinWasteKg = 0f;
            TotalOverflowWasteKg = 0f;

            foreach (var f in facilities.Values)
            {
                float rate = f.BaseWasteKgPerMin;
                if (f.Id == "canteen" && ExtraCounterActive) rate += 1.2f;

                f.BinCurrentKg += rate;
                if (f.BinCurrentKg > f.BinCapacityKg)
                {
                    float overflow = f.BinCurrentKg - f.BinCapacityKg;
                    f.BinCurrentKg = f.BinCapacityKg;
                    f.BinOverflowKg += overflow;
                }

                TotalBinWasteKg += f.BinCurrentKg;
                TotalOverflowWasteKg += f.BinOverflowKg;
            }

            CollectionVehicleActive = false;
            foreach (var t in tickets)
            {
                if (t.Kind == ActionKind.CollectWaste && t.Active)
                {
                    CollectionVehicleActive = true;
                    break;
                }
            }
        }

        void ExecuteWasteCollection()
        {
            float collectedThisRound = 0f;
            float maxCollectionCapacity = 450f;

            foreach (var f in facilities.Values)
            {
                if (collectedThisRound >= maxCollectionCapacity) break;

                // Empty overflow first
                if (f.BinOverflowKg > 0)
                {
                    float takeOverflow = Math.Min(f.BinOverflowKg, maxCollectionCapacity - collectedThisRound);
                    f.BinOverflowKg -= takeOverflow;
                    collectedThisRound += takeOverflow;
                }

                // Empty bins
                if (f.BinCurrentKg > 0 && collectedThisRound < maxCollectionCapacity)
                {
                    float takeBin = Math.Min(f.BinCurrentKg, maxCollectionCapacity - collectedThisRound);
                    f.BinCurrentKg -= takeBin;
                    collectedThisRound += takeBin;
                }
            }

            // Mass conservation to recovery yard
            RecycledWasteKg += collectedThisRound * SegregationQuality;
            LandfillWasteKg += collectedThisRound * (1f - SegregationQuality);

            TotalBinWasteKg = 0f;
            TotalOverflowWasteKg = 0f;
            foreach (var f in facilities.Values)
            {
                TotalBinWasteKg += f.BinCurrentKg;
                TotalOverflowWasteKg += f.BinOverflowKg;
            }
        }

        void ComputeCrowds()
        {
            int dayMinute = Minute % 1440;
            // Canteen Lunch Rush: 11:30 to 13:30 (Day minute 690 to 810)
            if (facilities.TryGetValue("canteen", out var canteen) && facilities.TryGetValue("academic", out var academic))
            {
                if (dayMinute >= 690 && dayMinute <= 810)
                {
                    // Flow students into canteen queue
                    int arrivalRate = StaggerArrivalsActive ? 4 : 12;
                    if (academic.Occupants >= arrivalRate)
                    {
                        academic.Occupants -= arrivalRate;
                        canteen.QueueCount += arrivalRate;
                    }

                    // Garden diversion
                    if (GardenDiversionActive && canteen.QueueCount > 20 && facilities.TryGetValue("garden", out var garden))
                    {
                        int diverted = Math.Min(8, canteen.QueueCount);
                        canteen.QueueCount -= diverted;
                        garden.Occupants += diverted;
                    }
                }
                else if (canteen.QueueCount > 0 && dayMinute > 810)
                {
                    // Return leftover students to academic block
                    academic.Occupants += canteen.QueueCount;
                    canteen.QueueCount = 0;
                }

                // Service students from queue into canteen seats (capped by available seats)
                int availableSeats = Math.Max(0, canteen.Capacity - canteen.Occupants);
                int serviceRate = ExtraCounterActive ? 18 : 10;
                int served = Math.Min(serviceRate, Math.Min(canteen.QueueCount, availableSeats));
                if (served > 0)
                {
                    canteen.QueueCount -= served;
                    canteen.Occupants += served;
                }

                // Students finish lunch and cycle back to academic block
                if (canteen.Occupants > 50)
                {
                    int returning = Math.Min(8, canteen.Occupants - 50);
                    canteen.Occupants -= returning;
                    academic.Occupants += returning;
                }
            }
        }

        void ComputeIndices()
        {
            // Dynamic Satisfaction Penalty / Recovery
            float penalty = 0f;
            if (PowerDeficitKW > 5f) penalty += 2f;
            if (WaterShortageLiters > 10f) penalty += 3f;
            if (TotalOverflowWasteKg > 15f) penalty += 1.5f;
            if (facilities.TryGetValue("canteen", out var canteen) && canteen.QueueCount > 35) penalty += 1.5f;
            if (RationingUntilMinute > 0) penalty += 1f;

            if (penalty > 0.1f)
            {
                Satisfaction = Math.Max(15, (int)(Satisfaction - penalty));
            }
            else if (Satisfaction < 85)
            {
                Satisfaction = Math.Min(95, Satisfaction + 1);
            }

            // Sustainability Score
            float score = 50f;
            if (!MainLeakActive) score += 12f;
            if (LowFlowInstalledAll) score += 10f;
            if (LedInstalledAll) score += 10f;
            if (SegregationQuality >= 0.70f) score += 12f;
            if (SolarGenerationKW > 30f) score += 6f;
            if (TotalOverflowWasteKg <= 1f) score += 5f;
            if (BackupActive) score -= 8f; // diesel emissions penalty

            Sustainability = Math.Clamp((int)score, 10, 98);
        }

        public CampusState DeepClone()
        {
            var clone = new CampusState(Seed)
            {
                Minute = Minute,
                Paused = Paused,
                BudgetINR = BudgetINR,
                StaffAllocated = StaffAllocated,
                Satisfaction = Satisfaction,
                Sustainability = Sustainability,

                WaterStoredLiters = WaterStoredLiters,
                MainLeakActive = MainLeakActive,
                PumpPolicy = PumpPolicy,
                PumpDeliveredLpm = PumpDeliveredLpm,
                CurrentWaterDemandLpm = CurrentWaterDemandLpm,
                WaterShortageLiters = WaterShortageLiters,
                CumulativeWaterPumpedLiters = CumulativeWaterPumpedLiters,
                CumulativeWaterConsumedLiters = CumulativeWaterConsumedLiters,
                CumulativeWaterLeakedLiters = CumulativeWaterLeakedLiters,
                CumulativeWaterShortageLiters = CumulativeWaterShortageLiters,
                LowFlowInstalledAll = LowFlowInstalledAll,
                RationingUntilMinute = RationingUntilMinute,
                BoostPumpUntilMinute = BoostPumpUntilMinute,

                SolarGenerationKW = SolarGenerationKW,
                BackupActive = BackupActive,
                BackupFuelReserveKWh = BackupFuelReserveKWh,
                BackupDeliveredKW = BackupDeliveredKW,
                TotalPowerDemandKW = TotalPowerDemandKW,
                TotalPowerSupplyKW = TotalPowerSupplyKW,
                PowerDeficitKW = PowerDeficitKW,
                CumulativeEnergyConsumedKWh = CumulativeEnergyConsumedKWh,
                LedInstalledAll = LedInstalledAll,
                LabsRescheduled = LabsRescheduled,
                LoadSheddingUntilMinute = LoadSheddingUntilMinute,

                SegregationQuality = SegregationQuality,
                CollectionDurationMinutes = CollectionDurationMinutes,
                RecycledWasteKg = RecycledWasteKg,
                LandfillWasteKg = LandfillWasteKg,
                TotalBinWasteKg = TotalBinWasteKg,
                TotalOverflowWasteKg = TotalOverflowWasteKg,
                CollectionVehicleActive = CollectionVehicleActive,

                TotalPopulation = TotalPopulation,
                ExtraCounterActive = ExtraCounterActive,
                StaggerArrivalsActive = StaggerArrivalsActive,
                GardenDiversionActive = GardenDiversionActive,
                OverflowSeatingActive = OverflowSeatingActive
            };

            foreach (var kvp in facilities)
                clone.facilities.Add(kvp.Key, kvp.Value.DeepClone());

            foreach (var t in tickets)
                clone.tickets.Add(t.Clone());

            foreach (var kvp in cooldownUntil)
                clone.cooldownUntil.Add(kvp.Key, kvp.Value);

            return clone;
        }

        public string Fingerprint()
        {
            var sb = new StringBuilder();
            sb.Append($"{Minute}|{BudgetINR}|{StaffAllocated}|{Satisfaction}|{Sustainability}|");
            sb.Append($"{WaterStoredLiters:0.0}|{MainLeakActive}|{PumpDeliveredLpm:0.0}|");
            sb.Append($"{TotalPowerDemandKW:0.0}|{TotalPowerSupplyKW:0.0}|{PowerDeficitKW:0.0}|");
            sb.Append($"{TotalBinWasteKg:0.0}|{TotalOverflowWasteKg:0.0}|{RecycledWasteKg:0.0}|{LandfillWasteKg:0.0}|");
            sb.Append($"{TotalPopulation}");
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
