using System;
using System.Collections.Generic;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

using Object = UnityEngine.Object;

namespace BetterCamera.Photo;

/// <summary>
/// The key hints in photo mode: every photo mode key in one stack at the bottom right, built from clones of the
/// vanilla build hint parts, in place of vanilla's hints. They're part of the HUD, so hiding the HUD for a shot
/// hides them too.
/// </summary>
internal static class PhotoKeyHints
{
    private const string ContainerName = "BetterCamera_PhotoHints";
    private const string ScrollIcon = "@scroll";

    /// <summary>
    /// In a hint's keys, the configured photo mode keys.
    /// </summary>
    private const string ToggleKey = "@toggle";
    private const string RollLeftKey = "@rollleft";
    private const string RollRightKey = "@rollright";
    private const string ResetKey = "@reset";
    private const string HideUiKey = "@hideui";

    private const float LabelFontSize = 16f;
    private const float IconHeight = 32f;
    private const float IconScale = 1.8f;
    private const float RowSpacing = 12f;

    private sealed class Hint
    {
        public string[] Keys;

        /// <summary>
        /// Shown between the keys: "+" for keys held together, "/" for either key.
        /// </summary>
        public string Separator = "+";

        public Func<string> Label;
        public TextMeshProUGUI LabelText;
        public string ShownLabel;
        public GameObject Row;
    }

    /// <summary>
    /// Top to bottom, with moving the camera at the bottom, next to the screen edge.
    /// </summary>
    private static readonly List<Hint> Hints =
    [
        new Hint { Keys = [ToggleKey], Label = () => "$bettercamera_photo_hint_exit" },
        new Hint { Keys = ["F11"], Label = () => "$bettercamera_photo_hint_screenshot" },
        new Hint { Keys = [HideUiKey], Label = () => "$bettercamera_photo_hint_hud" },
        new Hint { Keys = [ResetKey], Label = () => "$bettercamera_photo_hint_reset" },
        new Hint { Keys = [RollLeftKey, RollRightKey], Separator = "/", Label = () => "$bettercamera_photo_hint_roll" },
        new Hint { Keys = ["Left Alt", ScrollIcon], Label = () => "$bettercamera_photo_hint_speed" },
        new Hint { Keys = [ScrollIcon], Label = () => "$bettercamera_photo_hint_zoom" },
        new Hint
        {
            Keys = ["$KEY_SecondaryAttack"],
            Label = () => PhotoMode.Following ? "$bettercamera_photo_hint_unfollow" : "$bettercamera_photo_hint_follow",
        },
        new Hint
        {
            Keys = ["$KEY_Attack"],
            Label = () => PhotoMode.LookingAt ? "$bettercamera_photo_hint_unlook" : "$bettercamera_photo_hint_look",
        },
        new Hint { Keys = ["$KEY_Run"], Label = () => "$bettercamera_photo_hint_fast" },
        new Hint { Keys = ["$KEY_Jump", "$KEY_Crouch"], Separator = "/", Label = () => "$bettercamera_photo_hint_updown" },
        new Hint
        {
            Keys = ["$KEY_Forward", "$KEY_Left", "$KEY_Backward", "$KEY_Right"],
            Separator = null,
            Label = () => "$bettercamera_photo_hint_move",
        },
    ];

    private static readonly string[] AllLabels =
    [
        "$bettercamera_photo_hint_exit", "$bettercamera_photo_hint_screenshot", "$bettercamera_photo_hint_hud",
        "$bettercamera_photo_hint_reset", "$bettercamera_photo_hint_roll", "$bettercamera_photo_hint_speed",
        "$bettercamera_photo_hint_zoom", "$bettercamera_photo_hint_follow", "$bettercamera_photo_hint_unfollow",
        "$bettercamera_photo_hint_look", "$bettercamera_photo_hint_unlook", "$bettercamera_photo_hint_fast",
        "$bettercamera_photo_hint_updown", "$bettercamera_photo_hint_move",
    ];

    private static GameObject _container;
    private static RectTransform _stack;
    private static RectTransform _keyboard;
    private static bool _createFailed;
    private static bool _layoutDirty;

    /// <summary>
    /// Called after vanilla's KeyHints.UpdateHints.
    /// </summary>
    internal static void AfterUpdateHints(KeyHints keyHints)
    {
        // The hints live under the HUD, which is rebuilt with each world load.
        if (!ReferenceEquals(_container, null) && _container == null)
        {
            Reset();
        }

        // Like vanilla's, they follow the game's key hints setting.
        // With the inventory open, vanilla's inventory hints apply instead.
        if (!PhotoMode.Active || ZInput.IsGamepadActive() || !keyHints.m_keyHintsEnabled || InventoryGui.IsVisible())
        {
            SetContainerActive(false);
            return;
        }

        if (_container == null && (_createFailed || !Create(keyHints)))
        {
            return;
        }

        // Vanilla's hints are for playing, which the character isn't doing; it sets them again every update.
        keyHints.m_buildHints.SetActive(false);
        keyHints.m_combatHints.SetActive(false);
        keyHints.m_fishingHints.SetActive(false);

        foreach (Hint hint in Hints)
        {
            string label = hint.Label();
            if (label != hint.ShownLabel)
            {
                hint.ShownLabel = label;
                hint.LabelText.text = Localization.instance.Localize(label);
            }
        }

        SetContainerActive(true);
        if (_layoutDirty)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_stack);
            _layoutDirty = false;
        }

        Align();
    }

    private static void SetContainerActive(bool active)
    {
        if (_container != null && _container.activeSelf != active)
        {
            _container.SetActive(active);
            _layoutDirty |= active;
        }
    }

    /// <summary>
    /// The stack's bottom right corner sits where vanilla's build hint row ends.
    /// </summary>
    private static void Align()
    {
        Vector3[] corners = new Vector3[4];
        _keyboard.GetWorldCorners(corners); // 0 = bottom left, 1 = top left, 2 = top right, 3 = bottom right
        HorizontalLayoutGroup layout = _keyboard.GetComponent<HorizontalLayoutGroup>();
        float right = layout != null ? layout.padding.right * _keyboard.lossyScale.x : 0f;
        float bottom = layout != null ? layout.padding.bottom * _keyboard.lossyScale.y : 0f;
        _stack.position = new Vector3(corners[3].x - right, corners[3].y + bottom, corners[3].z);
    }

    private static bool Create(KeyHints keyHints)
    {
        Transform keyboard = keyHints.m_buildHints.transform.Find("Keyboard");
        Transform copy = keyboard != null ? keyboard.Find("Copy") : null;
        Transform rotate = keyboard != null ? keyboard.Find("rotate") : null;
        Transform labelTemplate = copy != null ? copy.Find("Text") : null;
        Transform plusTemplate = copy != null ? copy.Find("Text (1)") : null;
        Transform keyTemplate = copy != null ? copy.Find("key_bkg") : null;
        Transform iconTemplate = rotate != null ? rotate.Find("mousew_icon") : null;
        HorizontalLayoutGroup rowLayout = copy != null ? copy.GetComponent<HorizontalLayoutGroup>() : null;

        if (labelTemplate == null || plusTemplate == null || keyTemplate == null || iconTemplate == null || rowLayout == null)
        {
            _createFailed = true;
            Jotunn.Logger.LogWarning("Vanilla key hints layout not as expected, not showing photo mode key hints.");
            return false;
        }

        // Under KeyHints itself rather than the build hints, which are turned off in photo mode.
        RectTransform container = new GameObject(ContainerName, typeof(RectTransform)).GetComponent<RectTransform>();
        container.SetParent(keyHints.transform, false);
        RectTransform stack = CreateStack(container);
        string longestLabel = LongestLabel();

        foreach (Hint hint in Hints)
        {
            // A vanilla multi-key hint (for its row settings), emptied: a label, then a group with the keys.
            GameObject item = Object.Instantiate(copy.gameObject, stack, false);
            item.name = "BetterCamera_PhotoHint";
            item.SetActive(true);
            for (int i = item.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(item.transform.GetChild(i).gameObject);
            }

            // Sized with the longest label first, so the column fits every label.
            hint.LabelText = AddText(labelTemplate, item.transform, longestLabel);
            hint.LabelText.alignment = TextAlignmentOptions.Right;
            hint.ShownLabel = null;
            hint.Row = item;

            Transform keys = CreateKeyGroup(item.transform, rowLayout);
            for (int i = 0; i < hint.Keys.Length; i++)
            {
                if (i > 0 && hint.Separator != null)
                {
                    _ = AddText(plusTemplate, keys, hint.Separator);
                }

                if (hint.Keys[i] == ScrollIcon)
                {
                    AddScrollIcon(iconTemplate, keys);
                }
                else
                {
                    GameObject key = Object.Instantiate(keyTemplate.gameObject, keys, false);
                    key.SetActive(true);
                    key.GetComponentInChildren<TextMeshProUGUI>().text = KeyText(hint.Keys[i]);
                }
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(stack);
        EqualiseWidths();
        foreach (Hint hint in Hints)
        {
            hint.LabelText.text = "";
        }

        _container = container.gameObject;
        _stack = stack;
        _keyboard = (RectTransform)keyboard;
        _layoutDirty = true;
        return true;
    }

    private static string KeyText(string key)
    {
        return key switch
        {
            ToggleKey => PhotoMode.ToggleKey.ToString(),
            RollLeftKey => PhotoMode.RollLeftKey.ToString(),
            RollRightKey => PhotoMode.RollRightKey.ToString(),
            ResetKey => PhotoMode.ResetKey.ToString(),
            HideUiKey => PhotoMode.HideUiKey.ToString(),
            _ => Localization.instance.Localize(key),
        };
    }

    private static string LongestLabel()
    {
        string longest = "";
        foreach (string token in AllLabels)
        {
            string text = Localization.instance.Localize(token);
            if (text.Length > longest.Length)
            {
                longest = text;
            }
        }

        return longest;
    }

    private static RectTransform CreateStack(RectTransform parent)
    {
        RectTransform stack = new GameObject(ContainerName + "_Stack", typeof(RectTransform)).GetComponent<RectTransform>();
        stack.SetParent(parent, false);
        stack.pivot = new Vector2(1f, 0f);

        VerticalLayoutGroup layout = stack.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = RowSpacing;
        layout.childAlignment = TextAnchor.LowerRight;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = stack.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return stack;
    }

    /// <summary>
    /// The keys of a row in their own group, laid out like a vanilla hint, starting from the left.
    /// </summary>
    private static Transform CreateKeyGroup(Transform row, HorizontalLayoutGroup template)
    {
        GameObject group = new("Keys", typeof(RectTransform));
        group.transform.SetParent(row, false);
        HorizontalLayoutGroup layout = group.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = template.spacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = template.childControlWidth;
        layout.childControlHeight = template.childControlHeight;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ((RectTransform)group.transform).sizeDelta = new Vector2(0f, IconHeight);
        LayoutElement size = group.AddComponent<LayoutElement>();
        size.minHeight = size.preferredHeight = IconHeight;
        return group.transform;
    }

    /// <summary>
    /// The scroll wheel icon, laid out the size of a key box but drawn bigger, as its sprite is padded.
    /// </summary>
    private static void AddScrollIcon(Transform template, Transform parent)
    {
        GameObject icon = Object.Instantiate(template.gameObject, parent, false);
        icon.SetActive(true);
        Image image = icon.GetComponent<Image>();
        image.preserveAspect = true;
        float aspect = image.sprite != null ? image.sprite.rect.width / image.sprite.rect.height : 0.6f;
        LayoutElement layout = icon.GetComponent<LayoutElement>() ?? icon.AddComponent<LayoutElement>();
        layout.minWidth = layout.preferredWidth = IconHeight * aspect * IconScale;
        layout.minHeight = layout.preferredHeight = IconHeight;
        layout.flexibleWidth = layout.flexibleHeight = 0f;
        icon.transform.localScale = new Vector3(IconScale, IconScale, 1f);
    }

    /// <summary>
    /// Gives every row's label and key group the same widths, so labels line up on the right and keys on the
    /// left, like a form.
    /// </summary>
    private static void EqualiseWidths()
    {
        for (int part = 0; part < 2; part++)
        {
            float width = 0f;
            foreach (Hint hint in Hints)
            {
                width = Mathf.Max(width, LayoutUtility.GetPreferredWidth((RectTransform)hint.Row.transform.GetChild(part)));
            }

            foreach (Hint hint in Hints)
            {
                GameObject child = hint.Row.transform.GetChild(part).gameObject;
                LayoutElement layout = child.GetComponent<LayoutElement>() ?? child.AddComponent<LayoutElement>();
                layout.minWidth = width;
                layout.preferredWidth = width;
            }
        }
    }

    private static TextMeshProUGUI AddText(Transform template, Transform parent, string text)
    {
        GameObject clone = Object.Instantiate(template.gameObject, parent, false);
        clone.SetActive(true);

        // Vanilla labels can have a fixed width sized for their own text.
        LayoutElement fixedSize = clone.GetComponent<LayoutElement>();
        if (fixedSize != null)
        {
            Object.DestroyImmediate(fixedSize);
        }

        TextMeshProUGUI label = clone.GetComponent<TextMeshProUGUI>();
        label.enableAutoSizing = false;
        label.fontSize = LabelFontSize;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.text = text;
        return label;
    }

    private static void Reset()
    {
        _container = null;
        _stack = null;
        _keyboard = null;
        _createFailed = false;
        foreach (Hint hint in Hints)
        {
            hint.LabelText = null;
            hint.ShownLabel = null;
            hint.Row = null;
        }
    }
}
