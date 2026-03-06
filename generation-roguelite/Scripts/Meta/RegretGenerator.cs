using System.Collections.Generic;
using System.Linq;

namespace GenerationRoguelite.Meta;

public sealed class RegretGenerator
{
    public string BuildRegret(int age, IReadOnlyCollection<string> recentEvents)
    {
        if (recentEvents.Count == 0)
        {
            return $"{age}歳。ふと立ち止まり、静かな空を見上げた。";
        }

        var topic = recentEvents.Last();
        return
            $"{age}歳の後悔: 『{topic}』を思い返す。"
            + " もし別の道を選んでいたら、何が変わっていたのだろう。";
    }
}
