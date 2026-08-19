namespace Galaxy.Domain.Entities;

public enum FleetStatus
{
    Landed = 1,
    Orbiting = 2,
    Executing = 3,
    Patrolling = 4,
    Mining = 5,
    InBattle = 6
}

public enum FleetLocationType
{
    Planet = 1,
    DeepSpace = 2,
    ResourceField = 3,
    DebrisField = 4
}

public enum FlightCommandType
{
    Flight = 1,
    Patrol = 2,
    Attack = 3,
    Return = 4,
    Recon = 5,
    Mine = 6,
    LoadUnload = 7
}

public enum FlightCommandStatus
{
    Planned = 1,
    Active = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}

public enum FlightSpeedMode
{
    Economy = 1,
    Cruise = 2,
    Boost = 3
}

public enum SpaceObjectType
{
    ResourceField = 1,
    DebrisField = 2
}

public enum ResourceFieldType
{
    AsteroidBelt = 1,
    IceCloud = 2,
    MixedCluster = 3
}

public enum PirateCellState
{
    Dormant = 1,
    Scouting = 2,
    Raiding = 3,
    Entrenched = 4,
    Weakened = 5
}

public enum BattleStatus
{
    AwaitingOrders = 1,
    Resolving = 2,
    Completed = 3
}

public enum ShipServiceType
{
    ShieldRecharge = 1,
    HullRepair = 2
}

public enum GameEventType
{
    ReconReport = 1,
    IncomingAttack = 2
}

public class Fleet
{
    public Guid Id { get; set; }
    public Guid? PlayerId { get; set; }
    public Guid? HomePlanetId { get; set; }
    public Guid? PirateCellId { get; set; }
    public int HomeGalaxyNumber { get; set; }
    public int HomeSystemNumber { get; set; }
    public int HomePosition { get; set; }
    public string Name { get; set; } = null!;
    public bool IsPirate { get; set; }
    public FleetStatus Status { get; set; }
    public FleetLocationType LocationType { get; set; }
    public int GalaxyNumber { get; set; }
    public int SystemNumber { get; set; }
    public int Position { get; set; }
    public decimal MaterialsCargo { get; set; }
    public decimal DeuteriumCargo { get; set; }
    public decimal FuelReserve { get; set; }
    public int CurrentCommandSequence { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<FleetShip> Ships { get; set; } = new List<FleetShip>();
    public ICollection<FlightCommand> Commands { get; set; } = new List<FlightCommand>();
}

public class FleetShip
{
    public Guid Id { get; set; }
    public Guid FleetId { get; set; }
    public Fleet Fleet { get; set; } = null!;
    public Guid? ShipId { get; set; }
    public Ship? Ship { get; set; }
    public string Name { get; set; } = null!;
    public string BlueprintName { get; set; } = null!;
    public decimal LocalSpeed { get; set; }
    public decimal InterSystemSpeed { get; set; }
    public decimal CargoCapacity { get; set; }
    public decimal MiningRatePerMinute { get; set; }
    public decimal ScanRange { get; set; }
    public decimal MaxHull { get; set; }
    public decimal Hull { get; set; }
    public decimal MaxShield { get; set; }
    public decimal Shield { get; set; }
    public decimal LaserShieldDamage { get; set; }
    public decimal LaserHullDamage { get; set; }
    public decimal MissileShieldDamage { get; set; }
    public decimal MissileHullDamage { get; set; }
    public decimal ComponentMaterials { get; set; }
    public decimal ComponentDeuterium { get; set; }
    public string ComponentCodesJson { get; set; } = "[]";
}

public class FlightCommand
{
    public Guid Id { get; set; }
    public Guid FleetId { get; set; }
    public Fleet Fleet { get; set; } = null!;
    public int Sequence { get; set; }
    public FlightCommandType Type { get; set; }
    public FlightCommandStatus Status { get; set; }
    public FlightSpeedMode SpeedMode { get; set; }
    public int? TargetGalaxy { get; set; }
    public int? TargetSystem { get; set; }
    public int? TargetPosition { get; set; }
    public Guid? TargetFleetId { get; set; }
    public Guid? TargetObjectId { get; set; }
    public int DurationMinutes { get; set; }
    public decimal ManifestMaterials { get; set; }
    public decimal ManifestDeuterium { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletesAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Outcome { get; set; }
}

public class ResourceField
{
    public Guid Id { get; set; }
    public Guid StarSystemId { get; set; }
    public string Name { get; set; } = null!;
    public int Position { get; set; }
    public ResourceFieldType Type { get; set; }
    public decimal Materials { get; set; }
    public decimal Deuterium { get; set; }
    public decimal MaxMaterials { get; set; }
    public decimal MaxDeuterium { get; set; }
    public decimal RegenPerHour { get; set; }
    public decimal ThroughputPerHour { get; set; }
    public int Threat { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class DebrisField
{
    public Guid Id { get; set; }
    public int GalaxyNumber { get; set; }
    public int SystemNumber { get; set; }
    public int Position { get; set; }
    public decimal Materials { get; set; }
    public decimal Deuterium { get; set; }
    public Guid? ExclusivePlayerId { get; set; }
    public DateTime? ExclusiveUntil { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string ComponentsJson { get; set; } = "[]";
}

public class PirateCell
{
    public Guid Id { get; set; }
    public Guid StarSystemId { get; set; }
    public PirateCellState State { get; set; }
    public int Threat { get; set; }
    public decimal Materials { get; set; }
    public decimal Deuterium { get; set; }
    public DateTime LastActedAt { get; set; }
}

public class Battle
{
    public Guid Id { get; set; }
    public Guid AttackerFleetId { get; set; }
    public Guid DefenderFleetId { get; set; }
    public BattleStatus Status { get; set; }
    public int Round { get; set; }
    public DateTime OrderDeadline { get; set; }
    public DateTime ResolveAt { get; set; }
    public Guid? WinnerFleetId { get; set; }
    public string ReportJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class BattleOrder
{
    public Guid Id { get; set; }
    public Guid BattleId { get; set; }
    public Guid FleetId { get; set; }
    public int Round { get; set; }
    public string TargetPriority { get; set; } = "Weakest";
    public bool Retreat { get; set; }
    public DateTime SubmittedAt { get; set; }
}

public class GameEvent
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public GameEventType Type { get; set; }
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string DataJson { get; set; } = "{}";
    public Guid? SourceCommandId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

public class ShipServiceOrder
{
    public Guid Id { get; set; }
    public Guid FleetShipId { get; set; }
    public Guid PlanetId { get; set; }
    public ShipServiceType Type { get; set; }
    public decimal MaterialsCost { get; set; }
    public decimal DeuteriumCost { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletesAt { get; set; }
}
