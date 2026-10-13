# Phase 2 simulation contract (implementation target)

All quantities are fictional educational tuning. One tick equals one game-minute; the player can pause or accelerate ticking. Visual animation never creates water, energy, waste or students.

## Accounting

- Water in liters: ending storage = starting storage + electrically powered pumping - supplied facility use - actual leakage. Demand that cannot be met becomes recorded shortage. Pumping is capped by available reservoir space. kW is instantaneous power; kWh integrates delivered kW / 60 each minute.
- Waste in kilograms: initial waste + generated waste = material still in bins/overflow + collected recycling + tracked disposal. Collection moves a capped load into tracked destinations. It never silently deletes overflow.
- Occupants are counted exactly once per zone. Canteen queue is a subset of canteen occupants, not extra population. Arrivals transfer people from another zone; served/rerouted people move to another zone. Total population is conserved.
- Money is integral INR. Accepted actions reserve their cost and staff exactly once. Cancellation refunds only an unstarted action; started work has no refund. Terminal transitions release staff once. Permanent upgrades cannot be purchased repeatedly.

## Tick order

1. Advance time and process accepted actions in submission order; finish elapsed work.
2. Determine building/pump/counter demand, daylight solar and finite backup availability.
3. Allocate power and integrate energy; delivered pump power limits actual pumping.
4. Supply water, leak remaining water and record unmet demand.
5. Transfer population through arrival/service routes; generate facility waste and account for collection destinations.
6. Derive service quality, satisfaction, sustainability, alerts and scenario objective progress.

Temporary restrictions, backup fuel, action cooldowns and expiry are tick-based. Fixed ordering and stable facility IDs make the same initial state and command sequence reproducible. Snapshot objects privately own a deep-cloned state; restoring returns another clone so forecasts cannot mutate either the snapshot or the live session.

## Scenarios and presentation

Four playable scenario presets emphasize water loss, energy shortage, waste overflow or queue congestion while retaining all shared systems. Each supports multiple interventions. A successful scenario must resolve the focal problem while keeping services and satisfaction viable; the three-day story campaign remains Phase 3.

The HUD previews cost, staff, duration and cross-system effects before explicit confirmation. The world shows water fill/leaks, powered indicators, waste fill/litter and authoritative queue representations. An action ledger displays completion and rejection reasons. Future Lens work in this phase is snapshot/clone testing only.

Final values, implementation differences, test evidence and limitations must be recorded before Phase 2 is marked complete.
