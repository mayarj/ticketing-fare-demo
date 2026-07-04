# Ticketing & Fare Calculation Demo

A small ASP.NET Core backend demonstrating a fare calculation engine for a ticketing scenario. Built to showcase thoughtful engineering, data‑driven pricing, and point‑in‑time correctness.

## Design Decisions

### 1. Data‑Driven Policies and Modifications
Fare parameters (rates, minimums, surcharges) live in the database, not in code.  
- **Why:** Changing a price requires an `UPDATE` – no deployment, no downtime.  
- **What it buys:** Extensibility – adding a new modification is just a seed row; the engine reads it and applies it.  
- **Trade‑off:** JSON `params` are stringly‑typed; each policy validates its own shape.

### 2. Snapshot‑Only Point‑in‑Time Correctness
Every issued ticket freezes the exact inputs used to compute its fare.  
- **Why:** Editing a rate tomorrow must not change yesterday’s ticket.  
- **How:** `tickets` store `base_fare` and `total_fare` as typed decimals; `fare_calculation_snapshots` store the base‑fare formula and inputs; `applied_ticket_modifications` store each modification’s rule and surcharge at application time.  
- **Benefit:** A ticket forever explains “why €47?” – even after prices change.

### 3. Strategy Pattern for Base Fares
Each product type has its own `IFarePolicy` implementation (e.g., `PointToPointFarePolicy`, `DailyPassFarePolicy`).  
- **Why:** The calculation algorithm differs per product, but the `Ticket` entity stays flat – no EF inheritance mapping.  
- **Extensibility:** Adding a new product type = one new policy class + one DI registration + one seed row.

### 4. Single Rule‑Applier for Modifications
Instead of a separate class per modification (`FirstClassDecorator`, `ExtraLuggageDecorator`, …), a single `ModificationApplier` applies any rule based on its `rule_type`: `FIXED`, `PER_UNIT`, or `PERCENTAGE`.  
- **Why:** With only three behaviour types, a fold/reduce is simpler, more honest, and easier to test.  
- **Adding a modification:** one seed row only – no code change.

### 5. Server‑Assigned Ordering
Modification rules have a `priority` column. The server orders them ascending before applying them; client order is ignored.  
- **Why:** Prevents client‑side manipulation of pricing.  
- **Determinism:** Ties broken by code alphabetically; final order recorded in `applied_order`.

### 6. Type‑Safe Contexts
Each policy receives a strongly‑typed context subclass (`PointToPointContext`, `DailyPassContext`) carrying exactly the fields it needs.  
- **Why:** No dictionary bag, no null checks – each context documents its own input schema.

### 7. No Cache Layer (Deliberately Deferred)
Every calculation reads the current rate and rule rows from SQL Server.  
- **Why:** Cache invalidation is hard; at demo scale, a DB read is not a bottleneck. If needed, `IMemoryCache` with a short TTL is the minimal future step.

## Database Modeling

The schema uses **five tables**:

| Table | Purpose |
|-------|---------|
| `current_fare_rates` | Editable base‑fare parameters (one row per `policy_code`). |
| `current_modification_rules` | Editable modification rules with `priority`, `rule_type`, and JSON `params`. |
| `tickets` | Issued ticket with `base_fare`, `total_fare`, and ticket number. |
| `fare_calculation_snapshots` | Immutable base‑fare inputs and formula JSON (linked 1:1 to a ticket). |
| `applied_ticket_modifications` | Immutable rows for each modification applied to a ticket (surcharge, frozen `params_used`, `applied_order`). |

**Key design choices:**
- All JSON columns have `ISJSON` check constraints.
- Unique indexes on `ticket_number`, `policy_code`, and `modification_code`.
- Timestamps use `GETUTCDATE()` defaults; money is `decimal(10,2)`.
- Cascade delete ensures that deleting a ticket removes its snapshot and applied modifications automatically.

The `current_*` tables are never referenced by issued tickets – they are read **only at calculation time**. This makes price updates safe and immediate.

## Fare Engine Structure

The engine follows **Clean Architecture** with four projects:

```
Ticketing.Api       → HTTP endpoints, DI wiring, startup migration
Ticketing.Application → Orchestration (issuer, factory), DTOs, ports
Ticketing.Domain    → Pure business logic (policies, contexts, entities)
Ticketing.Infrastructure → EF Core, repositories, seed, adapters
```

**Flow for a request (`POST /api/tickets/point-to-point`):**

1. Controller validates the DTO and calls `ITicketIssuer`.
2. Issuer builds the appropriate context (e.g., `PointToPointContext` with distance).
3. `FareCalculatorFactory`:
   - Resolves the correct `IFarePolicy` via a DI‑backed resolver.
   - Loads the active rate row and computes the base fare.
   - Loads the requested modification rules, orders them by `priority`, and folds them over the base fare using `ModificationApplier`.
4. Issuer generates a unique ticket number, constructs the `Ticket` aggregate (with its snapshot and applied modifications), and persists everything in a single transaction via `IUnitOfWork`.
5. Controller returns a `TicketResponse` containing the full breakdown, including each modification’s running `ResultingFare`.

**Key points:**
- The calculator **never writes** to the database – it only computes.
- Persistence is the issuer’s responsibility, using a transaction to guarantee atomicity.
- The aggregate root (`Ticket`) enforces the invariant that a ticket always has one snapshot and its applied rows.

## Assumptions

Documented here because they shape the design:

1. **No user/passenger entity.** Tickets are anonymous – no `passenger_id`.
2. **No ticket lifecycle.** Tickets are created and never transition state – no `status` field.
3. **Single currency.** All fares are `decimal(10,2)` – no `currency_code`.
4. **Distance provider:** Point‑to‑point distances are derived from an in‑memory station map (seeded at startup). This avoids a separate table for a demo; the port makes it swappable later.
5. **Daily pass fare** is a flat rate regardless of the travel date – the `ValidOn` field is accepted for realism but does not affect the fare in this version.
6. **Modifications are additive** and compose in a defined server‑assigned order. No modification depends on another’s result beyond simple composition.
7. **`quantity`** is meaningful only for `PER_UNIT` rules; `FIXED` and `PERCENTAGE` ignore it (but still record it).
8. **All money math** uses `decimal` – no floating‑point drift.
9. **All timestamps** are UTC.
10. **Concurrency:** Issuance is a single DB transaction. Ticket numbers are generated with a cryptographically random suffix and backed by a unique index – collisions are extremely rare and retried.
11. **No cache** – every calculation hits SQL Server; caching is a deliberate future extension.

## How to Run

### Prerequisites

- .NET SDK 9 
- set the conection to the sqlserver 2019 
Migrations are applied automatically on startup, and seed data is inserted if not already present.

### Swagger UI
Open `http://localhost:port/swagger` in your browser.

### Sample Calls

**Point‑to‑point ticket (First Class + Extra Luggage):**
```bash
curl -X POST http://localhost:port/api/tickets/point-to-point \
  -H "Content-Type: application/json" \
  -d '{
    "origin": "STATION_A",
    "destination": "STATION_C",
    "modifications": [
      { "code": "FIRST_CLASS", "quantity": 1 },
      { "code": "EXTRA_LUGGAGE", "quantity": 2 }
    ]
  }'
```

**Daily pass (VIP Class):**
```bash
curl -X POST http://localhost:port/api/tickets/daily-pass \
  -H "Content-Type: application/json" \
  -d '{
    "travelDate": "2026-08-05",
    "zone": "ZONE_A",
    "modifications": [
      { "code": "VIP_CLASS", "quantity": 1 }
    ]
  }'
```

Each response includes the ticket number, total fare, and a full breakdown showing the base fare formula and each modification in applied order.

---
