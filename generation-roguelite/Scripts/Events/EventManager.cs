using System;
using System.Collections.Generic;
using System.Linq;
using GenerationRoguelite.Character;
using GenerationRoguelite.Core;
using GenerationRoguelite.SLM;
using Godot;

namespace GenerationRoguelite.Events;

public sealed class EventManager : IDisposable
{
    private readonly RandomNumberGenerator _rng = new();
    private readonly Dictionary<LifePhase, List<EventTemplate>> _templatePool;
    private readonly PromptBuilder _promptBuilder = new();
    private readonly JsonParser _jsonParser = new();
    private readonly EventValidator _validator = new();
    private readonly EventCache _cache = new();
    private readonly SLMBridge _slmBridge = new();
    private readonly Queue<string> _recentCategories = new();
    private readonly HashSet<string> _consumedTemplateIds = new(StringComparer.Ordinal);

    private float _cacheRefillCooldown;

    public string LastGenerationStatus { get; private set; } = "template";

    public EventManager()
    {
        _rng.Randomize();
        _templatePool = BuildTemplatePool();
        AppendJsonTemplates(_templatePool);
        _cache.TargetSize = 24;
    }

    public void TickCache(double delta, EventGenerationContext context)
    {
        _cacheRefillCooldown -= (float)delta;
        if (_cacheRefillCooldown > 0f || _cache.Count >= _cache.TargetSize)
        {
            return;
        }

        if (TryGenerateViaSlm(context, out var generated))
        {
            _cache.Enqueue(NormalizeLimit(generated));
        }
        else
        {
            _cache.Enqueue(CreateTemplateEvent(context.Phase));
        }

        _cacheRefillCooldown = 0.22f;
    }

    public EventData CreateEvent(EventGenerationContext context)
    {
        if (_cache.TryDequeue(out var cached))
        {
            LastGenerationStatus = "cache";
            return NormalizeLimit(cached);
        }

        if (TryGenerateViaSlm(context, out var generated))
        {
            LastGenerationStatus = "slm";
            return NormalizeLimit(generated);
        }

        LastGenerationStatus = "template";
        return CreateTemplateEvent(context.Phase);
    }

    public EventData CreateTemplateEvent(LifePhase phase)
    {
        var pool = _templatePool[phase];
        var selected = SelectTemplate(pool);

        var limit = Mathf.Clamp(5f + selected.EventText.Length * 0.1f, 7f, 12f);
        return selected.Build(limit);
    }

    public EventResolution Resolve(EventChoice choice, CharacterData character, int checkBonus = 0)
    {
        var success = true;
        if (!string.IsNullOrWhiteSpace(choice.CheckStat))
        {
            var roll = (int)_rng.RandiRange(1, 20);
            var stat = character.Stats.GetValue(choice.CheckStat!);
            success = stat + roll + checkBonus >= choice.Difficulty;
        }

        var delta = success ? choice.SuccessDelta : choice.FailDelta;
        character.Stats.ApplyDelta(delta.Vitality, delta.Intelligence, delta.Charisma, delta.Luck, delta.Wealth);
        character.ApplyLifeDamage(success ? choice.SuccessLifeDamage : choice.FailLifeDamage);

        var text = success ? choice.SuccessText : choice.FailText;
        return new EventResolution(success, text);
    }

    public void ResetCache()
    {
        _cache.Clear();
        _cacheRefillCooldown = 0f;
    }

    public void Dispose()
    {
        _slmBridge.Dispose();
    }

    private static void AppendJsonTemplates(Dictionary<LifePhase, List<EventTemplate>> pool)
    {
        if (!EventTemplates.TryLoad(out var templates, out var error))
        {
            GD.Print($"[EventManager] EventTemplateData fallback: {error}");
            return;
        }

        foreach (var template in templates)
        {
            if (!pool.TryGetValue(template.Phase, out var phasePool))
            {
                phasePool = new List<EventTemplate>();
                pool[template.Phase] = phasePool;
            }

            phasePool.Add(new EventTemplate(
                template.EventText,
                template.TapChoice,
                template.SwipeChoice,
                template.TimeoutChoice,
                template.Terrain,
                template.Id,
                template.Category));
        }
    }

    private bool TryGenerateViaSlm(EventGenerationContext context, out EventData eventData)
    {
        eventData = null!;

        var prompt = _promptBuilder.BuildEventPrompt(context);
        if (!_slmBridge.TryGenerateEventJson(prompt, out var json, out var bridgeError))
        {
            LastGenerationStatus = $"template(fallback:{bridgeError})";
            return false;
        }

        if (!_jsonParser.TryParse(json, out var parsed, out var parseError))
        {
            LastGenerationStatus = $"template(fallback:{parseError})";
            return false;
        }

        if (!_validator.IsValid(parsed, out var validationError))
        {
            LastGenerationStatus = $"template(fallback:{validationError})";
            return false;
        }

        eventData = parsed;
        return true;
    }

    private static EventData NormalizeLimit(EventData source)
    {
        var limit = Mathf.Clamp(5f + source.EventText.Length * 0.1f, 7f, 12f);
        return new EventData(
            source.EventText,
            source.TapChoice,
            source.SwipeChoice,
            source.TimeoutChoice,
            limit,
            source.Terrain);
    }

    private EventTemplate SelectTemplate(IReadOnlyList<EventTemplate> pool)
    {
        var candidates = pool
            .Where(template => string.IsNullOrWhiteSpace(template.Id) || !_consumedTemplateIds.Contains(template.Id))
            .ToList();

        if (candidates.Count == 0)
        {
            _consumedTemplateIds.Clear();
            candidates = pool.ToList();
        }

        if (_recentCategories.Count >= 3)
        {
            var recentSet = _recentCategories.ToHashSet(StringComparer.Ordinal);
            var nonRecent = candidates
                .Where(template => !recentSet.Contains(template.Category))
                .ToList();

            if (nonRecent.Count > 0 && _rng.Randf() < 0.5f)
            {
                candidates = nonRecent;
            }
        }

        var selected = candidates[(int)_rng.RandiRange(0, candidates.Count - 1)];

        if (!string.IsNullOrWhiteSpace(selected.Id))
        {
            _consumedTemplateIds.Add(selected.Id);
        }

        if (!string.IsNullOrWhiteSpace(selected.Category))
        {
            _recentCategories.Enqueue(selected.Category);
            while (_recentCategories.Count > 3)
            {
                _recentCategories.Dequeue();
            }
        }

        return selected;
    }

    private static Dictionary<LifePhase, List<EventTemplate>> BuildTemplatePool()
    {
        return new Dictionary<LifePhase, List<EventTemplate>>
        {
            {
                LifePhase.Childhood,
                new List<EventTemplate>
                {
                    new(
                        "道端で迷子の子犬が震えている。どうする？",
                        new EventChoice(
                            "助ける",
                            "charisma",
                            11,
                            new StatDelta(0, 0, 1, 1, 0),
                            new StatDelta(-1, 0, 0, 0, 0),
                            0f,
                            0.3f,
                            "子犬は懐いてくれた。魅力+1 / 運+1",
                            "噛まれて転んだ。体力-1"),
                        new EventChoice(
                            "周囲に相談する",
                            "intelligence",
                            10,
                            new StatDelta(0, 1, 0, 0, 0),
                            StatDelta.Zero,
                            0f,
                            0f,
                            "大人に引き渡せた。知力+1",
                            "誰にも見つからず時間だけが過ぎた。"),
                        new EventChoice(
                            "見送る",
                            null,
                            0,
                            StatDelta.Zero,
                            StatDelta.Zero,
                            0f,
                            0f,
                            "そのまま歩き出した。",
                            "そのまま歩き出した。"),
                        TerrainProfile.ForPhase(LifePhase.Childhood)),
                }
            },
            {
                LifePhase.Youth,
                new List<EventTemplate>
                {
                    new(
                        "山道で盗賊に追われる旅人を見つけた。",
                        new EventChoice(
                            "戦って助ける",
                            "vitality",
                            14,
                            new StatDelta(1, 0, 0, 0, 4),
                            new StatDelta(-1, 0, 0, 0, -2),
                            0f,
                            2.0f,
                            "旅人を救い報酬を得た。体力+1 / 財力+4",
                            "深手を負った。体力-1 / 財力-2 / 寿命-2"),
                        new EventChoice(
                            "言葉で時間を稼ぐ",
                            "charisma",
                            13,
                            new StatDelta(0, 0, 1, 1, 2),
                            new StatDelta(0, 0, -1, 0, -3),
                            0f,
                            1.2f,
                            "説得が成功した。魅力+1 / 運+1 / 財力+2",
                            "交渉失敗。魅力-1 / 財力-3 / 寿命-1.2"),
                        new EventChoice(
                            "危険を避ける",
                            null,
                            0,
                            new StatDelta(0, 0, 0, 1, 0),
                            StatDelta.Zero,
                            0f,
                            0f,
                            "遠回りしたが無事だった。運+1",
                            "遠回りしたが無事だった。運+1"),
                        TerrainProfile.ForPhase(LifePhase.Youth)),
                    new(
                        "橋の先で荷馬車が横転し、道が塞がれている。",
                        new EventChoice(
                            "力ずくで押しのける",
                            "vitality",
                            13,
                            new StatDelta(1, 0, 0, 0, 2),
                            new StatDelta(-1, 0, 0, 0, -1),
                            0f,
                            1.4f,
                            "道を開いて感謝された。体力+1 / 財力+2",
                            "腰を痛めた。体力-1 / 財力-1 / 寿命-1.4"),
                        new EventChoice(
                            "迂回ルートを探す",
                            "intelligence",
                            12,
                            new StatDelta(0, 1, 0, 1, 1),
                            new StatDelta(0, -1, 0, 0, 0),
                            0f,
                            0.8f,
                            "近道を見つけた。知力+1 / 運+1 / 財力+1",
                            "遠回りになった。知力-1 / 寿命-0.8"),
                        new EventChoice(
                            "通り過ぎる",
                            null,
                            0,
                            new StatDelta(0, 0, 0, 1, 0),
                            StatDelta.Zero,
                            0f,
                            0f,
                            "危険を避けて進んだ。運+1",
                            "危険を避けて進んだ。運+1"),
                        TerrainProfile.ForPhase(LifePhase.Youth)),
                }
            },
            {
                LifePhase.Midlife,
                new List<EventTemplate>
                {
                    new(
                        "商隊から短期護衛の依頼が届いた。",
                        new EventChoice(
                            "受ける",
                            "vitality",
                            15,
                            new StatDelta(0, 0, 0, 0, 6),
                            new StatDelta(-1, 0, 0, 0, -3),
                            0f,
                            2.5f,
                            "護衛成功。財力+6",
                            "襲撃に遭った。体力-1 / 財力-3 / 寿命-2.5"),
                        new EventChoice(
                            "条件交渉する",
                            "intelligence",
                            14,
                            new StatDelta(0, 1, 0, 0, 4),
                            new StatDelta(0, -1, 0, 0, -2),
                            0f,
                            1.2f,
                            "好条件を引き出した。知力+1 / 財力+4",
                            "交渉は決裂した。知力-1 / 財力-2 / 寿命-1.2"),
                        new EventChoice(
                            "見送る",
                            null,
                            0,
                            new StatDelta(0, 0, 0, 0, 1),
                            StatDelta.Zero,
                            0f,
                            0f,
                            "地道な仕事に戻った。財力+1",
                            "地道な仕事に戻った。財力+1"),
                        TerrainProfile.ForPhase(LifePhase.Midlife)),
                }
            },
            {
                LifePhase.Elderly,
                new List<EventTemplate>
                {
                    new(
                        "急な坂道。古い杖がきしむ音を立てた。",
                        new EventChoice(
                            "慎重に進む",
                            "luck",
                            14,
                            new StatDelta(0, 0, 0, 1, 0),
                            new StatDelta(-1, 0, 0, -1, 0),
                            0f,
                            2.8f,
                            "転ばず渡り切った。運+1",
                            "足をひねった。体力-1 / 運-1 / 寿命-2.8"),
                        new EventChoice(
                            "昔の知恵を使う",
                            "intelligence",
                            13,
                            new StatDelta(0, 1, 0, 0, 2),
                            new StatDelta(0, -1, 0, 0, -1),
                            0f,
                            1.6f,
                            "安全な迂回路を思い出した。知力+1 / 財力+2",
                            "読み違えた。知力-1 / 財力-1 / 寿命-1.6"),
                        new EventChoice(
                            "引き返す",
                            null,
                            0,
                            new StatDelta(0, 0, 0, 0, 0),
                            StatDelta.Zero,
                            0f,
                            0f,
                            "今日は無理をしない。",
                            "今日は無理をしない。"),
                        TerrainProfile.ForPhase(LifePhase.Elderly)),
                }
            },
        };
    }

    private sealed class EventTemplate
    {
        public string Id { get; }
        public string Category { get; }
        public string EventText { get; }
        public EventChoice TapChoice { get; }
        public EventChoice SwipeChoice { get; }
        public EventChoice TimeoutChoice { get; }
        public TerrainProfile Terrain { get; }

        public EventTemplate(
            string eventText,
            EventChoice tapChoice,
            EventChoice swipeChoice,
            EventChoice timeoutChoice,
            TerrainProfile terrain,
            string id = "",
            string category = "generic")
        {
            Id = id;
            Category = category;
            EventText = eventText;
            TapChoice = tapChoice;
            SwipeChoice = swipeChoice;
            TimeoutChoice = timeoutChoice;
            Terrain = terrain;
        }

        public EventData Build(float limit)
        {
            return new EventData(EventText, TapChoice, SwipeChoice, TimeoutChoice, limit, Terrain);
        }
    }
}
