using System.Collections.Generic;
using System.Numerics;
using Content.Server.CharacterAppearance.Components;
using Content.Server.Rejuvenate;
using Content.Shared.Maps;
using Content.Shared.Tag;
using Content.Shared.Veil.Cult.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Veil.Cult;

public sealed partial class RathvarSystem : EntitySystem
{

    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    
    private static readonly ProtoId<TagPrototype> WindowTag = "Window";
    private static readonly ProtoId<TagPrototype> WallTag = "Wall";
    private static readonly ProtoId<TagPrototype> GrilleTag = "Grille";
    private static readonly ProtoId<TagPrototype> AirlockTag = "Airlock";
    
    public override void Initialize()
    {
        base.Initialize();
        SubыcribeLocalEvent<RathvarComponent, ComponentInit>(OnInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var rathvarQuery = EntityQueryEnumerator<RathvarComponent>();
        while (rathvarQuery.MoveNext(out var rathvar, out var rathvarComponent))
        {
            if (rathvarComponent.NextTimeTick <= 0)
            {
                rathvarComponent.NextTimeTick = 3f;
                var tileDef = (ContentTileDefinition)_tileDefinitionManager["FloorBrassFilled"];
                var mapPos = Transform(rathvar).Coordinates;
                var box = Box2.CenteredAround(mapPos, new Vector2(4, 4));
                var grids = new List<Entity<MapGridComponent>>();
                _mapMan.FindGridsIntersecting(Transform(rathvar).MapId, box, ref tiles);

                foreach (var tile in tiles)
                {
                    if (!_random.Prob(0.8f))
                        continue;

                    var delay = TimeSpan.FromSeconds(_random.NextFloat(0.1f, 1f));
                    Timer.Spawn(delay, () => _tile.ReplaceTile(tile, tileDef));
                }
                
                var nearbyObjects = _entityLookup.GetEntitiesInRange(mapPos, 4f)
                foreach (var target in nearbyObjects)
                {
                    if (!_random.Prob(0.8f))
                        continue;
                    
                    var delay = TimeSpan.FromSeconds(_random.NextFloat(0.1f, 1f));
                    Timer.Spawn(delay, () => 
                    {
                        if (_tag.HasTag(target, WallTag))
                        {
                            Spawn("WallClock" Transform(target).Coordinates);
                            QueueDel(target);
                            continue;
                        }
                        if (_tag.HasTag(target, WindowTag))
                        {
                            Spawn("ClockworkWindow" Transform(target).Coordinates);
                            QueueDel(target);
                            continue;
                        }
                        if (_tag.HasTag(target, GrilleTag))
                        {
                            Spawn("ClockworkGrille" Transform(target).Coordinates);
                            QueueDel(target);
                            continue;
                        }
                        if (_tag.HasTag(target, AirlockTag))
                        {
                            Spawn("PinionAirlock" Transform(target).Coordinates);
                            QueueDel(target);
                            continue;
                        }
                    });
                }
            }
            rathvarComponent.NextTimeTick =- frameTime;
        }
    }
    
    private void OnInit(EntityUid uid, RathvarComponent comp, ComponentInit args)
    {
        var players = EntityQueryEnumerator<HumanoidProfileComponent>();
        foreach (var player in players)
        {
            if (Transform(player).MapId == Transform(uid).MapId)
            {
                EnsureComp<AutoVeilCultistComponent>(player);
                _rejuvenate.PerformRejuvenate(player);
            }
        }
    }
}