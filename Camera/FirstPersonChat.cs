using System.Collections.Generic;

using TMPro;

using UnityEngine;

namespace BetterCamera.Camera;

/// <summary>
/// The game shows what you say above your head. In first person your head is at the camera, so the game pins the
/// text to whichever screen edge your head is on, and it jumps between the left and right as you turn. In first
/// person your own text is shown at the top centre of the screen instead, below the raid name and boss health bars
/// when they're up, and a little bigger and in bold so it's easy to read. Other players' text is unchanged.
/// </summary>
internal static class FirstPersonChat
{
    /// <summary>Without it, your own chat text in first person jumps between the screen edges as in vanilla.</summary>
    public const string Feature = "Own chat at top in first person";

    /// <summary>Space above the text, and between it and anything above it, in HUD units.</summary>
    private const float Margin = 16f;

    private const float SizeScale = 1.25f;

    /// <summary>The text's size and style before it was enlarged, to put back when leaving first person.</summary>
    private static readonly Dictionary<TextMeshProUGUI, (float Size, FontStyles Style)> Enlarged = [];

    private static readonly Vector3[] Corners = new Vector3[4];

    /// <summary>Called after the game has placed every chat text this frame.</summary>
    public static void Place(Chat chat)
    {
        Player player = Player.m_localPlayer;
        GameCamera camera = GameCamera.instance;
        bool firstPerson = player != null && camera != null && !camera.m_freeFly && camera.m_distance <= 0f && !player.IsDead();

        foreach (Chat.WorldTextInstance text in chat.m_worldTexts)
        {
            if (text?.m_gui == null || text.m_textMeshField == null)
            {
                continue;
            }

            bool own = firstPerson && text.m_go != null && text.m_go == player.gameObject;
            Style(text.m_textMeshField, own);
            if (own)
            {
                PlaceAtTop((RectTransform)text.m_gui.transform);
            }
        }

        // Texts the game has removed.
        if (Enlarged.Count > 0)
        {
            List<TextMeshProUGUI> gone = null;
            foreach (TextMeshProUGUI field in Enlarged.Keys)
            {
                if (field == null)
                {
                    (gone ??= []).Add(field);
                }
            }

            gone?.ForEach(field => Enlarged.Remove(field));
        }
    }

    private static void Style(TextMeshProUGUI field, bool enlarge)
    {
        bool enlarged = Enlarged.TryGetValue(field, out (float Size, FontStyles Style) original);
        if (enlarge && !enlarged)
        {
            Enlarged[field] = (field.fontSize, field.fontStyle);
            field.fontSize *= SizeScale;
            field.fontStyle |= FontStyles.Bold;
        }
        else if (!enlarge && enlarged)
        {
            field.fontSize = original.Size;
            field.fontStyle = original.Style;
            _ = Enlarged.Remove(field);
        }
    }

    /// <summary>
    /// Puts the text's top edge just below the top of the screen, or below the lowest of the raid name, a boss's
    /// health bar and the "new biome" message when one of them is showing at the top.
    /// </summary>
    private static void PlaceAtTop(RectTransform rect)
    {
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        RectTransform screen = (RectTransform)canvas.rootCanvas.transform;
        screen.GetWorldCorners(Corners); // 0 = bottom left, 1 = top left, 2 = top right
        float centreX = (Corners[1].x + Corners[2].x) / 2f;
        float screenTop = Corners[1].y;
        float middle = (Corners[0].y + Corners[1].y) / 2f;
        float scale = screen.lossyScale.y;

        float top = screenTop - (Margin * scale);
        top = Mathf.Min(top, BelowIfAtTop(Hud.instance != null ? Hud.instance.m_eventBar : null, middle, scale));
        top = Mathf.Min(top, BelowIfAtTop(MessageHud.instance != null ? MessageHud.instance.m_biomeMsgInstance : null, middle, scale));
        if (EnemyHud.instance != null)
        {
            foreach (EnemyHud.HudData hud in EnemyHud.instance.m_huds.Values)
            {
                if (hud?.m_character != null && hud.m_character.IsBoss())
                {
                    top = Mathf.Min(top, BelowIfAtTop(hud.m_gui, middle, scale));
                }
            }
        }

        float height = rect.rect.height * rect.lossyScale.y;
        float y = top - ((1f - rect.pivot.y) * height);
        float width = rect.rect.width * rect.lossyScale.x;
        float x = centreX + ((rect.pivot.x - 0.5f) * width);
        rect.position = new Vector3(x, y, rect.position.z);
    }

    /// <summary>
    /// Just below the element, if it's showing in the top half of the screen; otherwise no limit.
    /// </summary>
    private static float BelowIfAtTop(GameObject element, float middle, float scale)
    {
        if (element == null || !element.activeInHierarchy || element.transform is not RectTransform rect)
        {
            return float.MaxValue;
        }

        rect.GetWorldCorners(Corners);
        float bottom = Corners[0].y;
        return bottom > middle ? bottom - (Margin * scale) : float.MaxValue;
    }
}
