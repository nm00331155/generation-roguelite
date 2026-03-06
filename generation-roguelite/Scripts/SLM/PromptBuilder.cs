using System.Linq;
using System.Text;
using GenerationRoguelite.Events;

namespace GenerationRoguelite.SLM;

public sealed class PromptBuilder
{
    public string BuildEventPrompt(EventGenerationContext context)
    {
        var recent = context.RecentEvents.Count == 0
            ? "なし"
            : string.Join(" / ", context.RecentEvents.Take(3));

        var bonds = context.Bonds.Count == 0
            ? "なし"
            : string.Join(", ", context.Bonds.Take(3));

        var traits = context.FamilyTraits.Count == 0
            ? "なし"
            : string.Join(", ", context.FamilyTraits.Take(3));

        var sb = new StringBuilder(900);
        sb.AppendLine("あなたはゲーム用イベント生成器です。説明文は不要、JSONのみ出力してください。");
        sb.AppendLine("JSON schema:");
        sb.AppendLine("{");
        sb.AppendLine("  \"event_text\": \"20-120文字\",");
        sb.AppendLine("  \"choices\": [");
        sb.AppendLine("    {\"text\":\"...\",\"check\":\"体力|知力|魅力|運|財力|null\",\"difficulty\":12,\"success\":\"...\",\"fail\":\"...\"}");
        sb.AppendLine("  ],");
        sb.AppendLine("  \"terrain\": {\"obstacle_density\":0.7,\"obstacle_type\":\"enemy\",\"speed_modifier\":1.0}");
        sb.AppendLine("}");
        sb.AppendLine("制約:");
        sb.AppendLine("- choicesは2〜3個");
        sb.AppendLine("- success/failの効果量は±10以内");
        sb.AppendLine("- 不正JSON禁止");
        sb.AppendLine();
        sb.AppendLine("入力パラメータ:");
        sb.AppendLine($"age={context.Age}");
        sb.AppendLine($"generation={context.Generation}");
        sb.AppendLine($"phase={context.Phase}");
        sb.AppendLine($"era={context.Era}");
        sb.AppendLine($"life_path={context.LifePath}");
        sb.AppendLine($"stats=体力:{context.Stats.Vitality},知力:{context.Stats.Intelligence},魅力:{context.Stats.Charisma},運:{context.Stats.Luck},財力:{context.Stats.Wealth}");
        sb.AppendLine($"bonds={bonds}");
        sb.AppendLine($"family_traits={traits}");
        sb.AppendLine($"recent_events={recent}");
        sb.AppendLine($"era_mechanic={context.EraMechanic}");

        return sb.ToString();
    }
}
