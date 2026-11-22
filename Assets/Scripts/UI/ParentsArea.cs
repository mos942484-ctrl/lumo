using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lumo.Core;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Lumo.UI
{
    static class ParentsUiHelper
    {
        /// <summary>LXD Phase C3: kid/parent-friendly name for a tracked skill id.</summary>
        public static string SkillLabel(string skill)
        {
            switch (skill)
            {
                case "addition": return "Addition";
                case "subtraction": return "Subtraction";
                case "twoStep": return "Two-Step";
                case "counting": return "Counting";
                case "vocabulary": return "Vocabulary";
                case "spelling": return "Spelling";
                case "sentence": return "Sentences";
                case "directions": return "Directions";
                case "kindness": return "Kindness";
                case "science": return "Science";
                case "memory": return "Memory";
                case "art": return "Pixel Art";
                case "music": return "Music";
                default: return skill;
            }
        }

        /// <summary>LXD Phase C3: comma-joined medal labels for a save's mastered skills.</summary>
        public static string MedalsLabel(string[] medals)
        {
            if (medals == null || medals.Length == 0) return "none yet";
            var labels = new string[medals.Length];
            for (int i = 0; i < medals.Length; i++) labels[i] = SkillLabel(medals[i]);
            return string.Join(", ", labels);
        }

        public static void BuildKeypad(GameObject parent, Vector2 anchor, Vector2 size,
            Vector2 cellSize, string okLabel, Action<char> onDigit, Action onClear, Action onOk)
        {
            var grid = UIKit.Grid(parent, "Keypad", cellSize, new Vector2(14, 10), 3, TextAnchor.MiddleCenter);
            UIKit.Place(grid.GetComponent<RectTransform>(), anchor, Vector2.zero, size);

            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "Clear", "0", okLabel };
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                Color bg = key == "Clear" ? UIKit.Orange : (key == okLabel ? UIKit.Green : UIKit.Cream);
                int fontSize = (key == "Clear" || key == okLabel) ? 26 : 34;
                if (key == "Clear")
                {
                    UIKit.TextButton(grid.gameObject, "Key_Clear", key, cellSize, bg, onClear, GameAssets.Ui("ui_btn"), fontSize);
                }
                else if (key == okLabel)
                {
                    UIKit.TextButton(grid.gameObject, "Key_OK", key, cellSize, bg, onOk, GameAssets.Ui("ui_btn"), fontSize);
                }
                else
                {
                    char ch = key[0];
                    UIKit.TextButton(grid.gameObject, "Key_" + key, key, cellSize, bg, () => onDigit(ch), GameAssets.Ui("ui_btn"), fontSize);
                }
            }
        }

        public static void PollKeyboard(Action<char> onDigit, Action onBackspace, Action onEnter, Action onEscape)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.digit0Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame) onDigit?.Invoke('0');
            else if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) onDigit?.Invoke('1');
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) onDigit?.Invoke('2');
            else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) onDigit?.Invoke('3');
            else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) onDigit?.Invoke('4');
            else if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) onDigit?.Invoke('5');
            else if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame) onDigit?.Invoke('6');
            else if (kb.digit7Key.wasPressedThisFrame || kb.numpad7Key.wasPressedThisFrame) onDigit?.Invoke('7');
            else if (kb.digit8Key.wasPressedThisFrame || kb.numpad8Key.wasPressedThisFrame) onDigit?.Invoke('8');
            else if (kb.digit9Key.wasPressedThisFrame || kb.numpad9Key.wasPressedThisFrame) onDigit?.Invoke('9');

            if (kb.backspaceKey.wasPressedThisFrame) onBackspace?.Invoke();
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) onEnter?.Invoke();
            if (kb.escapeKey.wasPressedThisFrame) onEscape?.Invoke();
#endif
        }
    }

    /// <summary>
    /// Adult gate protecting the Parents Area (optional 4-digit PIN or adult multiplication+addition prompt).
    /// </summary>
    public class ParentalGatePanel : OverlayPanel
    {
        public override string Name { get { return "ParentalGate"; } }

        Action _onUnlocked;
        string _entry = "";
        bool _isPinMode;
        int _expectedAnswer;
        TMP_Text _subtitle, _display, _status;

        public void OpenFor(Action onUnlocked)
        {
            _onUnlocked = onUnlocked;
            M.ShowOverlay("ParentalGate");
        }

        public override void Build(UIManager m)
        {
            base.Build(m);
            var dim = UIKit.Panel(Panel, "Dim", new Color(0.05f, 0.10f, 0.18f, 0.72f));
            UIKit.Stretch(dim.rectTransform);

            var card = UIKit.Card(Panel, "Card", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680, 760));

            var plaque = UIKit.Panel(card.gameObject, "HeaderPlaque", UIKit.DeepNavy, GameAssets.Ui("ui_btn"));
            plaque.raycastTarget = false;
            UIKit.Place(plaque.rectTransform, new Vector2(0.5f, 0.875f), Vector2.zero, new Vector2(590, 108));

            _subtitle = UIKit.Label(card.gameObject, "Subtitle", "Parents Only", 30, UIKit.Cream, TextAlignmentOptions.Center);
            UIKit.Place(_subtitle.rectTransform, new Vector2(0.5f, 0.875f), Vector2.zero, new Vector2(560, 100));

            var dispCard = UIKit.Card(card.gameObject, "DisplayBox", UIKit.Cream, GameAssets.Ui("ui_card"));
            UIKit.Place(dispCard.rectTransform, new Vector2(0.5f, 0.74f), Vector2.zero, new Vector2(420, 72));
            _display = UIKit.Label(dispCard.gameObject, "Display", "", 42, UIKit.Ink, TextAlignmentOptions.Center);
            UIKit.Stretch(_display.rectTransform, 16, 6);

            _status = UIKit.Label(card.gameObject, "Status", "", 22, UIKit.Coral, TextAlignmentOptions.Center);
            UIKit.Place(_status.rectTransform, new Vector2(0.5f, 0.66f), Vector2.zero, new Vector2(580, 40));

            ParentsUiHelper.BuildKeypad(
                card.gameObject,
                new Vector2(0.5f, 0.38f),
                new Vector2(460, 340),
                new Vector2(140, 76),
                "OK",
                AppendDigit,
                ClearEntry,
                TrySubmit
            );

            var cancel = UIKit.TextButton(card.gameObject, "Cancel", "Cancel", new Vector2(240, 62), UIKit.Coral,
                () => M.HideOverlay("ParentalGate"), GameAssets.Ui("ui_btn"), 30);
            UIKit.Place(cancel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(240, 62));
            UIKit.AttachButtonIcon(cancel, GameAssets.Ui("ui_icon_close"), 28f);
        }

        public override void OnShow()
        {
            if (SessionTimeManager.Instance != null)
                SessionTimeManager.Instance.TrackingPaused = true;

            _entry = "";
            var pin = (SaveManager.Instance != null && SaveManager.Instance.Data != null)
                ? SaveManager.Instance.Data.parentPin
                : "";
            _isPinMode = !string.IsNullOrEmpty(pin);
            if (_isPinMode)
                _subtitle.text = "Parents Only - Enter 4-Digit PIN";
            else
                GenerateMathPrompt();
            _status.text = "";
            UpdateDisplay();
        }

        public override void OnHide()
        {
            if (SessionTimeManager.Instance != null)
                SessionTimeManager.Instance.TrackingPaused = M.IsOverlayOpen("Parents") || M.IsOverlayOpen("TimeLimit");
        }

        void GenerateMathPrompt()
        {
            int a = UnityEngine.Random.Range(6, 10);
            int b = UnityEngine.Random.Range(4, 9);
            int c = UnityEngine.Random.Range(11, 30);
            _expectedAnswer = (a * b) + c;
            _subtitle.text = "Parents Only\nWhat is (" + a + " x " + b + ") + " + c + " ?";
        }

        void AppendDigit(char ch)
        {
            if (_entry.Length >= 4) return;
            _entry += ch;
            _status.text = "";
            UpdateDisplay();
        }

        void ClearEntry()
        {
            _entry = "";
            _status.text = "";
            UpdateDisplay();
        }

        void Backspace()
        {
            if (_entry.Length > 0)
            {
                _entry = _entry.Substring(0, _entry.Length - 1);
                UpdateDisplay();
            }
        }

        void UpdateDisplay()
        {
            if (_isPinMode)
                _display.text = _entry.Length == 0 ? "----" : new string('*', _entry.Length);
            else
                _display.text = _entry.Length == 0 ? "?" : _entry;
        }

        void TrySubmit()
        {
            if (string.IsNullOrEmpty(_entry)) return;
            if (_isPinMode)
            {
                string pin = SaveManager.Instance != null && SaveManager.Instance.Data != null
                    ? SaveManager.Instance.Data.parentPin
                    : "";
                if (_entry == pin)
                {
                    Unlock();
                }
                else
                {
                    _entry = "";
                    _status.text = "Incorrect PIN. Please try again.";
                    UpdateDisplay();
                }
                return;
            }

            int val;
            if (int.TryParse(_entry, out val) && val == _expectedAnswer)
            {
                Unlock();
            }
            else
            {
                _entry = "";
                GenerateMathPrompt();
                _status.text = "Not quite - try the new question.";
                UpdateDisplay();
            }
        }

        void Unlock()
        {
            var cb = _onUnlocked;
            _onUnlocked = null;
            M.HideOverlay("ParentalGate");
            cb?.Invoke();
        }

        public override void Tick(float dt)
        {
            ParentsUiHelper.PollKeyboard(AppendDigit, Backspace, TrySubmit, () => M.HideOverlay("ParentalGate"));
        }
    }

    /// <summary>
    /// Parents & Guardians dashboard: screen time, skill progress, age bands, limits, Calm Mode, audio, and resets.
    /// </summary>
    public class ParentsPanel : OverlayPanel
    {
        public override string Name { get { return "Parents"; } }

        static readonly int[] LimitCycle = { 0, 15, 30, 45, 60 };
        static readonly Color ArmedRed = UIKit.Hex("#D32F2F");

        TMP_Text _summaryText, _skillsText, _skillsRightText, _ageDesc;
        Button[] _bandBtns = new Button[3];
        Image[] _bandImgs = new Image[3];

        Button _limitBtn, _extraBtn, _calmBtn, _musicBtn, _sfxBtn, _voiceBtn;
        Button _myWordsBtn; // LXD Phase D5
        Button _freeSailBtn, _timedBtn, _practiceBtn;
        Button _setPinBtn, _removePinBtn;
        Button _resetProgressBtn, _eraseAllBtn;

        bool _resetProgressArmed, _eraseAllArmed;

        // Inline 4-digit PIN setup modal sub-panel
        GameObject _pinModal;
        TMP_Text _pinDisplay, _pinStatus;
        string _pinDraft = "";

        public override void Build(UIManager m)
        {
            base.Build(m);
            var dim = UIKit.Panel(Panel, "Dim", new Color(0.05f, 0.10f, 0.18f, 0.72f));
            UIKit.Stretch(dim.rectTransform);

            var card = UIKit.Card(Panel, "Card", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1660, 920));

            var plaque = UIKit.Panel(card.gameObject, "HeaderPlaque", UIKit.DeepNavy, GameAssets.Ui("ui_btn"));
            plaque.raycastTarget = false;
            UIKit.Place(plaque.rectTransform, new Vector2(0.5f, 0.935f), Vector2.zero, new Vector2(1590, 74));

            var title = UIKit.Label(card.gameObject, "Title", "Parents & Guardians", 42, UIKit.Gold, TextAlignmentOptions.Left);
            UIKit.Place(title.rectTransform, new Vector2(0.04f, 0.935f), Vector2.zero, new Vector2(600, 56), new Vector2(0f, 0.5f));

            var sub = UIKit.Label(card.gameObject, "Subtitle", "Offline family controls & learning summary", 23, UIKit.Cream, TextAlignmentOptions.Right);
            UIKit.Place(sub.rectTransform, new Vector2(0.96f, 0.935f), Vector2.zero, new Vector2(700, 44), new Vector2(1f, 0.5f));

            BuildLeftColumn(card.gameObject);
            BuildRightColumn(card.gameObject);
            BuildPinModal(Panel);
        }

        void BuildLeftColumn(GameObject card)
        {
            var col = UIKit.VLayout(card, "LeftColumn", 10f, TextAnchor.UpperLeft, false);
            UIKit.Place(col.GetComponent<RectTransform>(), new Vector2(0.255f, 0.455f), Vector2.zero, new Vector2(750, 770));

            var h1 = UIKit.Label(col.gameObject, "H1", "Progress & Screen Time", 28, UIKit.WarmWood, TextAlignmentOptions.Left);
            UIKit.Flex(h1.gameObject, 750, 40);

            var sumCard = UIKit.Card(col.gameObject, "SummaryCard", UIKit.SoftGold, GameAssets.Ui("ui_card"));
            UIKit.Flex(sumCard.gameObject, 750, 232);
            _summaryText = UIKit.Label(sumCard.gameObject, "SummaryText", "", 21, UIKit.Ink, TextAlignmentOptions.TopLeft);
            _summaryText.enableAutoSizing = true;
            _summaryText.fontSizeMin = 15;
            _summaryText.fontSizeMax = 21;
            _summaryText.overflowMode = TextOverflowModes.Truncate;
            UIKit.Stretch(_summaryText.rectTransform, 22, 12);

            var h2 = UIKit.Label(col.gameObject, "H2", "Subjects & Skills Practised", 28, UIKit.WarmWood, TextAlignmentOptions.Left);
            UIKit.Flex(h2.gameObject, 750, 40);

            var skCard = UIKit.Card(col.gameObject, "SkillsCard", UIKit.Cream, GameAssets.Ui("ui_card"));
            UIKit.Flex(skCard.gameObject, 750, 356);
            var skRow = UIKit.HLayout(skCard.gameObject, "SkillsColumns", 16f, TextAnchor.UpperLeft, false);
            skRow.padding = new RectOffset(18, 18, 14, 14);
            UIKit.Stretch(skRow.GetComponent<RectTransform>());
            _skillsText = UIKit.Label(skRow.gameObject, "SkillsLeft", "", 18, UIKit.Ink, TextAlignmentOptions.TopLeft);
            _skillsText.enableAutoSizing = true;
            _skillsText.fontSizeMin = 12;
            _skillsText.fontSizeMax = 18;
            _skillsText.overflowMode = TextOverflowModes.Truncate;
            UIKit.Flex(_skillsText.gameObject, 348, 328);
            _skillsRightText = UIKit.Label(skRow.gameObject, "SkillsRight", "", 18, UIKit.Ink, TextAlignmentOptions.TopLeft);
            _skillsRightText.enableAutoSizing = true;
            _skillsRightText.fontSizeMin = 12;
            _skillsRightText.fontSizeMax = 18;
            _skillsRightText.overflowMode = TextOverflowModes.Truncate;
            UIKit.Flex(_skillsRightText.gameObject, 348, 328);

            // LXD Phase D2/D5/E2: scaffolding prompts + focus words + session report entry row
            var extrasRow = UIKit.HLayout(col.gameObject, "ExtrasRow", 12f, TextAnchor.MiddleCenter, false);
            UIKit.Flex(extrasRow.gameObject, 750, 62);
            MakeColBtn(extrasRow.gameObject, "ParentTips", "Help Them Learn", 242, 58, UIKit.Green, () =>
            {
                M.ShowOverlay("ParentTips");
            }, 21);
            _myWordsBtn = MakeColBtn(extrasRow.gameObject, "MyWords", "My Words", 242, 58, UIKit.SkyBlue, () =>
            {
                M.ShowOverlay("MyWords");
            }, 21);
            MakeColBtn(extrasRow.gameObject, "CopyReport", "Copy Report", 242, 58, UIKit.Purple, () =>
            {
                var bank = GameManager.Instance != null ? GameManager.Instance.Bank : null;
                string csv = SessionReport.BuildCsv(PlayLog.Snapshot(), bank != null ? bank.skills : null);
                GUIUtility.systemCopyBuffer = csv;
                Debug.Log("[LXD] session report copied to clipboard:\n" + csv);
                M.Toast("Session report copied to clipboard");
            }, 21);
        }

        void BuildRightColumn(GameObject card)
        {
            var col = UIKit.VLayout(card, "RightColumn", 12f, TextAnchor.UpperCenter, false);
            UIKit.Place(col.GetComponent<RectTransform>(), new Vector2(0.74f, 0.455f), Vector2.zero, new Vector2(790, 770));

            var h1 = UIKit.Label(col.gameObject, "H1", "Controls & Safety", 28, UIKit.Blue, TextAlignmentOptions.Left);
            UIKit.Flex(h1.gameObject, 790, 38);

            // Age bands
            var bandBox = UIKit.VLayout(col.gameObject, "AgeBandBox", 8f, TextAnchor.UpperLeft, false);
            UIKit.Flex(bandBox.gameObject, 790, 96);
            _ageDesc = UIKit.Label(bandBox.gameObject, "AgeDesc", "", 22, UIKit.Ink, TextAlignmentOptions.Left);
            UIKit.Flex(_ageDesc.gameObject, 790, 30);

            var bandRow = UIKit.HLayout(bandBox.gameObject, "BandRow", 14f, TextAnchor.MiddleCenter, false);
            UIKit.Flex(bandRow.gameObject, 790, 56);
            for (int b = 0; b < 3; b++)
            {
                int bandIdx = b;
                var btn = MakeColBtn(bandRow.gameObject, "Band" + b, "Ages " + GameRules.AgeBandShort(b), 254, 54, UIKit.Cream, () =>
                {
                    DisarmResets();
                    SaveManager.Instance.Data.ageBand = bandIdx;
                    SaveManager.Instance.Save();
                    Refresh();
                }, 24);
                _bandBtns[b] = btn;
                _bandImgs[b] = btn.GetComponent<Image>();
            }

            // Daily limit + extra time
            var limitRow = UIKit.HLayout(col.gameObject, "LimitRow", 14f, TextAnchor.MiddleCenter, false);
            UIKit.Flex(limitRow.gameObject, 790, 58);
            _limitBtn = MakeColBtn(limitRow.gameObject, "Limit", "Daily Limit: Off", 500, 56, UIKit.SkyBlue, () =>
            {
                DisarmResets();
                var d = SaveManager.Instance.Data;
                int cur = d.dailyLimitMinutes;
                int next = LimitCycle[0];
                for (int i = 0; i < LimitCycle.Length; i++)
                {
                    if (LimitCycle[i] == cur)
                    {
                        next = LimitCycle[(i + 1) % LimitCycle.Length];
                        break;
                    }
                }
                d.dailyLimitMinutes = next;
                if (next == 0) d.extraMinutesToday = 0;
                SaveManager.Instance.Save();
                if (SessionTimeManager.Instance != null) SessionTimeManager.Instance.NotifySettingsChanged();
                Refresh();
            }, 24);

            _extraBtn = MakeColBtn(limitRow.gameObject, "ExtraTime", "+15m Today", 276, 56, UIKit.Green, () =>
            {
                DisarmResets();
                if (SessionTimeManager.Instance != null) SessionTimeManager.Instance.GrantExtraMinutes(15);
                Refresh();
                M.Toast("Added 15 minutes for today");
            }, 24);

            // Calm Mode
            _calmBtn = MakeColBtn(col.gameObject, "CalmMode", "Calm Mode: OFF", 790, 56, UIKit.Cream, () =>
            {
                DisarmResets();
                SaveManager.Instance.Data.calmMode = !SaveManager.Instance.Data.calmMode;
                SaveManager.Instance.Save();
                Refresh();
            }, 24);

            // Audio switches (3 independent)
            var audioRow = UIKit.HLayout(col.gameObject, "AudioRow", 14f, TextAnchor.MiddleCenter, false);
            UIKit.Flex(audioRow.gameObject, 790, 58);
            _musicBtn = MakeColBtn(audioRow.gameObject, "Music", "Music: ON", 254, 56, UIKit.Cream, () =>
            {
                DisarmResets();
                SaveManager.Instance.Data.musicOn = !SaveManager.Instance.Data.musicOn;
                SaveManager.Instance.Save();
                if (AudioManager.Instance != null) AudioManager.Instance.ApplySettings();
                Refresh();
            }, 23);
            _sfxBtn = MakeColBtn(audioRow.gameObject, "Sfx", "SFX: ON", 254, 56, UIKit.Cream, () =>
            {
                DisarmResets();
                SaveManager.Instance.Data.soundOn = !SaveManager.Instance.Data.soundOn;
                SaveManager.Instance.Save();
                Refresh();
            }, 23);
            _voiceBtn = MakeColBtn(audioRow.gameObject, "Voice", "Voice: ON", 254, 56, UIKit.Cream, () =>
            {
                DisarmResets();
                SaveManager.Instance.Data.voiceOn = !SaveManager.Instance.Data.voiceOn;
                SaveManager.Instance.Save();
                if (!SaveManager.Instance.Data.voiceOn && AudioManager.Instance != null) AudioManager.Instance.StopVoice();
                Refresh();
            }, 23);

            // Gameplay modes (3 switches)
            var modeRow = UIKit.HLayout(col.gameObject, "ModeRow", 14f, TextAnchor.MiddleCenter, false);
            UIKit.Flex(modeRow.gameObject, 790, 58);
            _freeSailBtn = MakeColBtn(modeRow.gameObject, "FreeSail", "Free Sail: ON", 254, 56, UIKit.Cream, () =>
            {
                DisarmResets();
                SaveManager.Instance.Data.freeExplore = !SaveManager.Instance.Data.freeExplore;
                SaveManager.Instance.Save();
                Refresh();
            }, 22);
            _timedBtn = MakeColBtn(modeRow.gameObject, "TimedMode", "Timed: OFF", 254, 56, UIKit.Cream, () =>
            {
                DisarmResets();
                SaveManager.Instance.Data.timedMode = !SaveManager.Instance.Data.timedMode;
                SaveManager.Instance.Save();
                Refresh();
            }, 22);
            _practiceBtn = MakeColBtn(modeRow.gameObject, "PracticeMode", "Practice: OFF", 254, 56, UIKit.Cream, () =>
            {
                DisarmResets();
                SaveManager.Instance.Data.practiceMode = !SaveManager.Instance.Data.practiceMode;
                SaveManager.Instance.Save();
                Refresh();
            }, 22);

            // PIN row
            var pinRow = UIKit.HLayout(col.gameObject, "PinRow", 14f, TextAnchor.MiddleCenter, false);
            UIKit.Flex(pinRow.gameObject, 790, 58);
            _setPinBtn = MakeColBtn(pinRow.gameObject, "SetPin", "Set 4-Digit PIN", 388, 56, UIKit.Purple, () =>
            {
                DisarmResets();
                OpenPinModal();
            }, 24);
            _removePinBtn = MakeColBtn(pinRow.gameObject, "RemovePin", "Remove PIN", 388, 56, UIKit.Brown, () =>
            {
                DisarmResets();
                SaveManager.Instance.Data.parentPin = "";
                SaveManager.Instance.Save();
                Refresh();
                M.Toast("PIN removed");
            }, 24);

            // Reset / Erase row (two-step confirmation)
            var resetRow = UIKit.HLayout(col.gameObject, "ResetRow", 14f, TextAnchor.MiddleCenter, false);
            UIKit.Flex(resetRow.gameObject, 790, 58);
            _resetProgressBtn = MakeColBtn(resetRow.gameObject, "ResetProgress", "Reset Progress", 388, 56, UIKit.Orange, () =>
            {
                if (!_resetProgressArmed)
                {
                    _eraseAllArmed = false;
                    _resetProgressArmed = true;
                    UpdateResetButtons();
                    M.Toast("Tap again to reset progress (settings kept)");
                    return;
                }
                DisarmResets();
                SaveManager.Instance.ResetProgressKeepSettings();
                Refresh();
                M.Toast("Progress reset (settings kept)!");
            }, 23);

            _eraseAllBtn = MakeColBtn(resetRow.gameObject, "EraseAll", "Erase All Data", 388, 56, UIKit.Coral, () =>
            {
                if (!_eraseAllArmed)
                {
                    _resetProgressArmed = false;
                    _eraseAllArmed = true;
                    UpdateResetButtons();
                    M.Toast("Tap again to erase ALL data");
                    return;
                }
                DisarmResets();
                SaveManager.Instance.ResetSave();
                if (SessionTimeManager.Instance != null) SessionTimeManager.Instance.NotifySettingsChanged();
                Refresh();
                M.Toast("All data erased!");
            }, 23);

            // Done row
            var doneRow = UIKit.HLayout(col.gameObject, "DoneRow", 14f, TextAnchor.MiddleRight, false);
            UIKit.Flex(doneRow.gameObject, 790, 66);
            var doneBtn = MakeColBtn(doneRow.gameObject, "Done", "Done", 280, 62, UIKit.Teal, CloseParents, 30);
            UIKit.AttachButtonIcon(doneBtn, GameAssets.Ui("ui_check"), 30f);
        }

        Button MakeColBtn(GameObject parent, string name, string label, float w, float h, Color bg, Action onClick, int fontSize)
        {
            var b = UIKit.TextButton(parent, name, label, new Vector2(w, h), bg, onClick, GameAssets.Ui("ui_btn"), fontSize);
            UIKit.Flex(b.gameObject, w, h);
            return b;
        }

        void BuildPinModal(GameObject root)
        {
            _pinModal = UIKit.Node(root, "PinModal");
            UIKit.Stretch(_pinModal.GetComponent<RectTransform>());

            var dim = UIKit.Panel(_pinModal, "Dim", new Color(0, 0, 0, 0.55f));
            UIKit.Stretch(dim.rectTransform);

            var card = UIKit.Card(_pinModal, "Card", UIKit.Paper, GameAssets.Ui("ui_panel"));
            UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 620));

            var title = UIKit.Label(card.gameObject, "Title", "Set 4-Digit PIN", 36, UIKit.Orange, TextAlignmentOptions.Center);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.89f), Vector2.zero, new Vector2(460, 50));

            var box = UIKit.Panel(card.gameObject, "Box", UIKit.Cream, GameAssets.Ui("ui_panel"));
            UIKit.Place(box.rectTransform, new Vector2(0.5f, 0.77f), Vector2.zero, new Vector2(340, 66));
            _pinDisplay = UIKit.Label(box.gameObject, "Display", "----", 40, UIKit.Ink, TextAlignmentOptions.Center);
            UIKit.Stretch(_pinDisplay.rectTransform, 12, 4);

            _pinStatus = UIKit.Label(card.gameObject, "Status", "Enter 4 digits (0-9)", 22, UIKit.Ink, TextAlignmentOptions.Center);
            UIKit.Place(_pinStatus.rectTransform, new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(440, 36));

            ParentsUiHelper.BuildKeypad(
                card.gameObject,
                new Vector2(0.5f, 0.40f),
                new Vector2(420, 310),
                new Vector2(126, 68),
                "Save",
                AppendPinDigit,
                ClearPinDraft,
                SavePinDraft
            );

            var cancel = UIKit.TextButton(card.gameObject, "Cancel", "Cancel", new Vector2(220, 56), UIKit.Coral,
                ClosePinModal, GameAssets.Ui("ui_btn"), 28);
            UIKit.Place(cancel.GetComponent<RectTransform>(), new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(220, 56));

            _pinModal.SetActive(false);
        }

        void OpenPinModal()
        {
            _pinDraft = "";
            if (_pinStatus != null)
            {
                _pinStatus.color = UIKit.Ink;
                _pinStatus.text = "Enter 4 digits (0-9)";
            }
            UpdatePinDisplay();
            if (_pinModal != null)
            {
                _pinModal.SetActive(true);
                _pinModal.transform.SetAsLastSibling();
            }
        }

        void ClosePinModal()
        {
            if (_pinModal != null) _pinModal.SetActive(false);
        }

        void AppendPinDigit(char ch)
        {
            if (_pinDraft.Length >= 4) return;
            _pinDraft += ch;
            if (_pinStatus != null)
            {
                _pinStatus.color = UIKit.Ink;
                _pinStatus.text = "Enter 4 digits (0-9)";
            }
            UpdatePinDisplay();
        }

        void ClearPinDraft()
        {
            _pinDraft = "";
            UpdatePinDisplay();
        }

        void BackspacePinDraft()
        {
            if (_pinDraft.Length > 0)
            {
                _pinDraft = _pinDraft.Substring(0, _pinDraft.Length - 1);
                UpdatePinDisplay();
            }
        }

        void UpdatePinDisplay()
        {
            if (_pinDisplay != null)
                _pinDisplay.text = _pinDraft.Length == 0 ? "----" : new string('*', _pinDraft.Length);
        }

        void SavePinDraft()
        {
            if (!SaveManager.IsValidPin(_pinDraft))
            {
                if (_pinStatus != null)
                {
                    _pinStatus.color = UIKit.Coral;
                    _pinStatus.text = "Please enter all 4 digits";
                }
                return;
            }
            SaveManager.Instance.Data.parentPin = _pinDraft;
            SaveManager.Instance.Save();
            ClosePinModal();
            Refresh();
            M.Toast("4-digit PIN saved");
        }

        void DisarmResets()
        {
            _resetProgressArmed = false;
            _eraseAllArmed = false;
            UpdateResetButtons();
        }

        void UpdateResetButtons()
        {
            if (_resetProgressBtn != null)
            {
                SetButtonStyle(_resetProgressBtn,
                    _resetProgressArmed ? "Sure? Reset Progress" : "Reset Progress",
                    _resetProgressArmed ? ArmedRed : UIKit.Orange,
                    Color.white);
            }
            if (_eraseAllBtn != null)
            {
                SetButtonStyle(_eraseAllBtn,
                    _eraseAllArmed ? "Sure? Erase ALL!" : "Erase All Data",
                    _eraseAllArmed ? ArmedRed : UIKit.Coral,
                    Color.white);
            }
        }

        void CloseParents()
        {
            M.HideOverlay("Parents");
            if (SessionTimeManager.Instance != null)
            {
                SessionTimeManager.Instance.NotifySettingsChanged();
                if (!SessionTimeManager.Instance.IsDailyLimitReached && M.IsOverlayOpen("TimeLimit"))
                {
                    M.HideOverlay("TimeLimit");
                    if (TimerManager.Instance != null && !M.IsOverlayOpen("Pause"))
                        TimerManager.Instance.Resume();
                }
                SessionTimeManager.Instance.TrackingPaused = M.IsOverlayOpen("TimeLimit");
            }
            if (M.CurrentScreen is WorldMapScreen wm)
                wm.Refresh();
            else if (M.CurrentScreen is MainMenuScreen mm)
                mm.RefreshStats();
        }

        public override void OnShow()
        {
            if (SessionTimeManager.Instance != null)
                SessionTimeManager.Instance.TrackingPaused = true;
            ClosePinModal();
            DisarmResets();
            Refresh();
        }

        public override void OnHide()
        {
            ClosePinModal();
            DisarmResets();
            if (SessionTimeManager.Instance != null)
                SessionTimeManager.Instance.TrackingPaused = M.IsOverlayOpen("TimeLimit") || M.IsOverlayOpen("ParentalGate");
        }

        public override void Tick(float dt)
        {
            if (_pinModal != null && _pinModal.activeSelf)
                ParentsUiHelper.PollKeyboard(AppendPinDigit, BackspacePinDraft, SavePinDraft, ClosePinModal);
        }

        void Refresh()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.Data == null) return;
            var d = SaveManager.Instance.Data;
            var bank = GameManager.Instance != null ? GameManager.Instance.Bank : null;

            int shards = ScoreManager.Instance != null ? ScoreManager.Instance.ShardsCollected() : 0;
            int visited = 0;
            for (int i = 0; i < 13; i++)
            {
                bool hasTime = d.islandSeconds != null && i < d.islandSeconds.Length && d.islandSeconds[i] > 0;
                bool completed = d.levelRecords != null && i < d.levelRecords.Length && d.levelRecords[i] != null && d.levelRecords[i].completed;
                if (hasTime || completed) visited++;
            }
            int effLimit = SessionTimeManager.Instance != null ? SessionTimeManager.Instance.EffectiveDailyLimitMinutes : d.dailyLimitMinutes;
            int todayMin = d.todaySecondsPlayed / 60;
            int totalMin = d.totalSecondsPlayed / 60;

            string favIsland = GetMostExploredIsland(d);
            int favSecs = GetIslandSecondsForName(d, favIsland);
            string favDisplay = favSecs >= 60 ? favIsland + " (" + (favSecs / 60) + "m)" : (favSecs > 0 ? favIsland + " (<1m)" : favIsland);
            string nextIsland = GetSuggestedNextIsland(d);

            var sumSb = new StringBuilder();
            sumSb.AppendLine("Stars: " + d.totalStars + " / " + GameRules.DynamicTotalStars() + "    |    Crystal Shards: " + shards + " / 12");
            sumSb.AppendLine("XP: " + d.totalXp + "    |    Islands Visited: " + visited + " / 13");
            // LXD Phase C3: medals count alongside badges
            sumSb.AppendLine("Badges: " + d.badges.Length + "    |    Mastery Medals: " + d.medals.Length);
            sumSb.AppendLine("Today Played: " + todayMin + " min    |    Daily Limit: " + (effLimit > 0 ? effLimit + " min" : "Off"));
            sumSb.AppendLine("Lifetime Played: " + totalMin + " min");
            sumSb.AppendLine("Favorite: " + favDisplay + "  |  Try Next: " + nextIsland);
            // LXD Phase D3: the child's latest reflection from the result screen
            int feelLevel;
            string feel = SaveManager.LastLevelFeel(d, out feelLevel);
            if (feel != null && feelLevel >= 1 && feelLevel <= 13)
                sumSb.AppendLine("Last reflection: " + IslandCatalog.Get(feelLevel).IslandName + " felt \"" + feel + "\"");
            _summaryText.text = sumSb.ToString();

            // LXD Phase D5: focus-word count on the My Words entry button
            if (_myWordsBtn != null)
            {
                int fw = d.focusWords != null ? d.focusWords.Length : 0;
                SetButtonStyle(_myWordsBtn, "My Words: " + fw + " of 5", UIKit.SkyBlue, UIKit.Ink);
            }

            var leftSb = new StringBuilder();
            AppendSkillGroup(leftSb, bank, d, "Math:", new[]
            {
                ("addition", "Addition"),
                ("subtraction", "Subtraction"),
                ("twoStep", "Two-Step"),
                ("counting", "Counting")
            });
            AppendSkillGroup(leftSb, bank, d, "Reading & Words:", new[]
            {
                ("vocabulary", "Vocabulary"),
                ("spelling", "Spelling"),
                ("sentence", "Sentences")
            });
            _skillsText.text = leftSb.ToString();

            if (_skillsRightText != null)
            {
                var rightSb = new StringBuilder();
                AppendSkillGroup(rightSb, bank, d, "Science & Kindness:", new[]
                {
                    ("science", "Science"),
                    ("kindness", "Kindness")
                });
                AppendSkillGroup(rightSb, bank, d, "Puzzles & Arts:", new[]
                {
                    ("directions", "Directions"),
                    ("memory", "Memory"),
                    ("art", "Pixel Art"),
                    ("music", "Music")
                });
                _skillsRightText.text = rightSb.ToString();
            }

            if (_ageDesc != null)
                _ageDesc.text = "Age Band: " + GameRules.AgeBandDescription(d.ageBand);
            for (int b = 0; b < 3; b++)
            {
                bool active = d.ageBand == b;
                SetButtonStyle(_bandBtns[b], "Ages " + GameRules.AgeBandShort(b),
                    active ? UIKit.Blue : UIKit.Cream,
                    active ? Color.white : UIKit.Ink);
            }

            if (_limitBtn != null)
            {
                string limText = d.dailyLimitMinutes == 0 ? "Daily Limit: Off" : "Daily Limit: " + d.dailyLimitMinutes + " min";
                if (d.dailyLimitMinutes > 0 && d.extraMinutesToday > 0) limText += " (+" + d.extraMinutesToday + "m)";
                SetButtonStyle(_limitBtn, limText, UIKit.SkyBlue, UIKit.Ink);
            }
            if (_extraBtn != null)
                _extraBtn.interactable = d.dailyLimitMinutes > 0;

            SetToggleStyle(_calmBtn, "Calm Mode (Reduced Motion): ", d.calmMode, UIKit.Teal);
            SetToggleStyle(_musicBtn, "Music: ", d.musicOn, UIKit.Green);
            SetToggleStyle(_sfxBtn, "SFX: ", d.soundOn, UIKit.Green);
            SetToggleStyle(_voiceBtn, "Voice: ", d.voiceOn, UIKit.Green);
            SetToggleStyle(_freeSailBtn, "Free Sail: ", d.freeExplore, UIKit.Teal);
            SetToggleStyle(_timedBtn, "Timed Mode: ", d.timedMode, UIKit.Orange);
            SetToggleStyle(_practiceBtn, "Practice: ", d.practiceMode, UIKit.Purple);

            bool hasPin = !string.IsNullOrEmpty(d.parentPin);
            if (_setPinBtn != null)
                SetButtonStyle(_setPinBtn, hasPin ? "Change 4-Digit PIN" : "Set 4-Digit PIN", UIKit.Purple, Color.white);
            if (_removePinBtn != null)
                _removePinBtn.interactable = hasPin;

            UpdateResetButtons();
        }

        /// <summary>Returns the island name with the most recorded exploration time (or stars/progress fallback).</summary>
        public static string GetMostExploredIsland(SaveData data)
        {
            if (data == null) return IslandCatalog.Get(1).IslandName;

            int bestId = -1;
            int bestSecs = 0;
            if (data.islandSeconds != null)
            {
                int count = Mathf.Min(13, data.islandSeconds.Length);
                for (int i = 0; i < count; i++)
                {
                    if (data.islandSeconds[i] > bestSecs)
                    {
                        bestSecs = data.islandSeconds[i];
                        bestId = i + 1;
                    }
                }
            }
            if (bestId >= 1) return IslandCatalog.Get(bestId).IslandName;

            int bestStars = 0;
            if (data.levelRecords != null)
            {
                int count = Mathf.Min(13, data.levelRecords.Length);
                for (int i = 0; i < count; i++)
                {
                    var rec = data.levelRecords[i];
                    if (rec == null) continue;
                    int s = SaveManager.SumStars(rec);
                    if (s > bestStars || (rec.completed && bestId < 0))
                    {
                        bestStars = s;
                        bestId = i + 1;
                    }
                }
            }
            if (bestId >= 1) return IslandCatalog.Get(bestId).IslandName;

            return IslandCatalog.Get(Mathf.Clamp(data.lastLevel, 1, 13)).IslandName;
        }

        /// <summary>Returns the next recommended island name based on completion, unlock state, and time spent.</summary>
        public static string GetSuggestedNextIsland(SaveData data)
        {
            if (data == null) return IslandCatalog.Get(1).IslandName;

            bool allCoreComplete = true;
            for (int id = 1; id <= 12; id++)
            {
                var rec = (data.levelRecords != null && id - 1 < data.levelRecords.Length) ? data.levelRecords[id - 1] : null;
                if (rec == null || !rec.completed) { allCoreComplete = false; break; }
            }

            int highest = Mathf.Clamp(data.highestUnlocked, 1, 13);
            for (int id = 1; id <= 13; id++)
            {
                var rec = (data.levelRecords != null && id - 1 < data.levelRecords.Length) ? data.levelRecords[id - 1] : null;
                bool done = rec != null && rec.completed;
                if (done) continue;

                bool accessible = data.freeExplore || (id <= 12 ? id <= highest : allCoreComplete);
                if (accessible) return IslandCatalog.Get(id).IslandName;
            }

            int fewestId = 1;
            int fewestStars = int.MaxValue;
            int fewestSecs = int.MaxValue;
            for (int id = 1; id <= 13; id++)
            {
                var rec = (data.levelRecords != null && id - 1 < data.levelRecords.Length) ? data.levelRecords[id - 1] : null;
                int stars = SaveManager.SumStars(rec);
                int secs = (data.islandSeconds != null && id - 1 < data.islandSeconds.Length) ? data.islandSeconds[id - 1] : 0;
                if (stars < fewestStars || (stars == fewestStars && secs < fewestSecs))
                {
                    fewestStars = stars;
                    fewestSecs = secs;
                    fewestId = id;
                }
            }
            return IslandCatalog.Get(fewestId).IslandName;
        }

        static int GetIslandSecondsForName(SaveData data, string islandName)
        {
            if (data == null || data.islandSeconds == null || string.IsNullOrEmpty(islandName)) return 0;
            for (int id = 1; id <= 13; id++)
            {
                if (IslandCatalog.Get(id).IslandName == islandName && id - 1 < data.islandSeconds.Length)
                    return Mathf.Max(0, data.islandSeconds[id - 1]);
            }
            return 0;
        }

        static void AppendSkillGroup(StringBuilder sb, LevelBank bank, SaveData d, string heading, (string key, string label)[] items)
        {
            sb.AppendLine(heading);
            for (int i = 0; i < items.Length; i++)
            {
                int idx = bank != null ? AdaptiveManager.SkillIndex(bank, items[i].key) : -1;
                SkillStats st = (idx >= 0 && d.skills != null && idx < d.skills.Length) ? d.skills[idx] : null;
                // LXD Phase B: group each skill under its owning level's ILO kidText
                // (first ILO that covers the skill).
                string ilo = bank != null ? GetOwningIloKidText(bank, items[i].key) : null;
                if (st == null || st.attempts == 0)
                {
                    sb.AppendLine("  " + items[i].label + (string.IsNullOrEmpty(ilo) ? ": Not tried" : " - " + ilo + " (Not tried)"));
                }
                else
                {
                    int pct = Mathf.RoundToInt(100f * st.firstTryCorrect / st.attempts);
                    string[] masteryNames = { "Learning", "Practicing", "Good", "Mastered" };
                    string mastery = masteryNames[Mathf.Clamp(st.mastery, 0, 3)];
                    sb.AppendLine("  " + items[i].label + (string.IsNullOrEmpty(ilo) ? "" : " - " + ilo)
                        + " (" + st.attempts + "x, " + pct + "% 1st, " + mastery + ")");
                }
            }
        }

        /// <summary>LXD Phase B: the kidText of the first ILO (across levels) that covers the skill.</summary>
        public static string GetOwningIloKidText(LevelBank bank, string skill)
        {
            if (bank == null || bank.levels == null) return null;
            foreach (var lv in bank.levels)
            {
                if (lv == null || lv.challenges == null || lv.ilos == null) continue;
                bool usesSkill = false;
                foreach (var c in lv.challenges)
                    if (c != null && c.skill == skill) { usesSkill = true; break; }
                if (!usesSkill) continue;
                for (int i = 0; i < lv.ilos.Length && i < 3; i++)
                {
                    var ilo = lv.ilos[i];
                    if (ilo == null || ilo.skills == null) continue;
                    foreach (var s in ilo.skills)
                        if (s == skill) return ilo.kidText;
                }
                return null;
            }
            return null;
        }

        /// <summary>LXD Phase B: the mastery label (max over the ILO's skills) for the progress report; null when never attempted.</summary>
        public static string GetIloMasteryLabel(LevelBank bank, SaveData d, LevelData lv, IloData ilo)
        {
            if (bank == null || d == null || lv == null || ilo == null || ilo.skills == null) return null;
            int best = -1;
            bool attempted = false;
            foreach (var s in ilo.skills)
            {
                int idx = AdaptiveManager.SkillIndex(bank, s);
                if (idx < 0 || d.skills == null || idx >= d.skills.Length || d.skills[idx] == null) continue;
                var st = d.skills[idx];
                if (st.attempts > 0) attempted = true;
                best = Mathf.Max(best, st.mastery);
            }
            if (!attempted) return null;
            string[] masteryNames = { "Learning", "Practicing", "Good", "Mastered" };
            return masteryNames[Mathf.Clamp(best, 0, 3)];
        }

        static void SetToggleStyle(Button btn, string prefix, bool on, Color onColor)
        {
            SetButtonStyle(btn, prefix + (on ? "ON" : "OFF"), on ? onColor : UIKit.Cream, on ? Color.white : UIKit.Ink);
        }

        static void SetButtonStyle(Button btn, string text, Color bg, Color textColor)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = bg;
            var lbl = btn.GetComponentInChildren<TMP_Text>(true);
            if (lbl != null)
            {
                lbl.text = text;
                lbl.color = textColor;
            }
        }
    }

    /// <summary>
    /// LXD Phase D2: parent scaffolding prompts per skill (out-game actions #1/#4) —
    /// one concrete "while playing" and one home-context "after playing" prompt each.
    /// </summary>
    public static class ParentTips
    {
        public static readonly string[] Skills =
        {
            "addition", "subtraction", "twoStep", "counting",
            "vocabulary", "spelling", "sentence",
            "kindness", "science", "directions", "memory", "art", "music"
        };

        public static string While(string skill)
        {
            switch (skill)
            {
                case "addition": return "While: ask them to count real objects and add two groups.";
                case "subtraction": return "While: let them take pieces away and count what is left.";
                case "twoStep": return "While: ask what to do first, then what comes next.";
                case "counting": return "While: count stairs, cars or apples out loud together.";
                case "vocabulary": return "While: point at things and say the English word together.";
                case "spelling": return "While: say the word slowly and clap each letter sound.";
                case "sentence": return "While: have them read the sentence aloud after building it.";
                case "kindness": return "While: when a scenario comes up, ask 'what would you do?'";
                case "science": return "While: wonder out loud - 'why do you think it rains?'";
                case "directions": return "While: play 'two steps forward, one step right' around the house.";
                case "memory": return "While: play 'what is missing?' with three household objects.";
                case "art": return "While: name colors out loud while they draw or build.";
                case "music": return "While: hum the melody back together after each round.";
                default: return "";
            }
        }

        public static string After(string skill)
        {
            switch (skill)
            {
                case "addition": return "After: set the table - how many forks and spoons altogether?";
                case "subtraction": return "After: you had 8 grapes and ate 3 - how many are left?";
                case "twoStep": return "After: plan two steps together - first we shop, then we cook.";
                case "counting": return "After: how many steps from the door to your room?";
                case "vocabulary": return "After: name three things you saw today in English.";
                case "spelling": return "After: spell your favorite word with magnets or on paper.";
                case "sentence": return "After: tell me one full sentence about your day.";
                case "kindness": return "After: ask about a time a friend needed help today.";
                case "science": return "After: ask them to teach you one new fact they learned.";
                case "directions": return "After: hide a toy and give them three steps to find it.";
                case "memory": return "After: ask them to recall the shopping list from memory.";
                case "art": return "After: copy a small picture together, square by square.";
                case "music": return "After: clap a rhythm for them to repeat.";
                default: return "";
            }
        }
    }

    /// <summary>
    /// LXD Phase D2: all 13 skills with their while/after scaffolding prompts, in a
    /// two-column read-only panel. Reached from the Parents Area (kept out of the
    /// per-skill stat rows so the dashboard stays compact).
    /// </summary>
    public class ParentTipsPanel : OverlayPanel
    {
        public override string Name { get { return "ParentTips"; } }

        public override void Build(UIManager m)
        {
            base.Build(m);
            var dim = UIKit.Panel(Panel, "Dim", new Color(0.05f, 0.10f, 0.18f, 0.72f));
            UIKit.Stretch(dim.rectTransform);

            var card = UIKit.Card(Panel, "Card", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1560, 880));

            var plaque = UIKit.Panel(card.gameObject, "HeaderPlaque", UIKit.DeepNavy, GameAssets.Ui("ui_btn"));
            plaque.raycastTarget = false;
            UIKit.Place(plaque.rectTransform, new Vector2(0.5f, 0.93f), Vector2.zero, new Vector2(1490, 72));

            var title = UIKit.Label(card.gameObject, "Title", "Help Them Learn - While & After", 38, UIKit.Gold, TextAlignmentOptions.Left);
            UIKit.Place(title.rectTransform, new Vector2(0.03f, 0.93f), Vector2.zero, new Vector2(1200, 54), new Vector2(0f, 0.5f));

            var body = UIKit.Label(card.gameObject, "Body", "", 19, UIKit.Ink, TextAlignmentOptions.TopLeft);
            body.enableAutoSizing = true;
            body.fontSizeMin = 15;
            body.fontSizeMax = 19;
            body.overflowMode = TextOverflowModes.Truncate;
            UIKit.Stretch(body.rectTransform, 44, 84);

            var close = UIKit.TextButton(card.gameObject, "Close", "Close", new Vector2(320, 68), UIKit.Teal,
                () => M.HideOverlay("ParentTips"), GameAssets.Ui("ui_btn"), 30);
            UIKit.Place(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0.055f), Vector2.zero, new Vector2(320, 68));
            UIKit.AttachButtonIcon(close, GameAssets.Ui("ui_check"), 28f);
        }

        public override void OnShow()
        {
            var body = Panel.transform.Find("Card/Body").GetComponent<TMP_Text>();
            var sb = new System.Text.StringBuilder();
            int half = (ParentTips.Skills.Length + 1) / 2;
            for (int i = 0; i < half; i++)
            {
                string left = ParentTips.Skills[i];
                int rIdx = i + half;
                bool hasRight = rIdx < ParentTips.Skills.Length;
                string right = hasRight ? ParentTips.Skills[rIdx] : null;

                sb.AppendLine(ParentsUiHelper.SkillLabel(left) + ":" +
                    (hasRight ? "<pos=50%>" + ParentsUiHelper.SkillLabel(right) + ":" : ""));
                sb.AppendLine("   " + ParentTips.While(left) +
                    (hasRight ? "<pos=50%>   " + ParentTips.While(right) : ""));
                sb.AppendLine("   " + ParentTips.After(left) +
                    (hasRight ? "<pos=50%>   " + ParentTips.After(right) : ""));
            }
            body.text = sb.ToString();
        }
    }

    /// <summary>
    /// LXD Phase D5: "My Words" - parents pick up to 5 focus words from the
    /// picture-word pool; focus words are served before random review words.
    /// </summary>
    public class MyWordsPanel : OverlayPanel
    {
        public override string Name { get { return "MyWords"; } }
        TMP_Text _status;

        /// <summary>Every picture-word used by word-picture challenges, lowercased,
        /// function words excluded, restricted to words the review round can render.</summary>
        public static List<string> CollectPictureWords(LevelBank bank)
        {
            var pool = new List<string>();
            if (bank == null) return pool;
            void AddWord(string raw)
            {
                if (string.IsNullOrEmpty(raw)) return;
                string w = raw.Trim().ToLower();
                if (Lumo.Core.AdaptiveManager.IsFunctionWord(w)) return;
                if (!Lumo.Core.GameManager.IsReviewableWord(w)) return;
                if (!pool.Contains(w)) pool.Add(w);
            }
            void AddFrom(ChallengeData[] list)
            {
                if (list == null) return;
                foreach (var c in list)
                {
                    if (c == null || c.type != "word-picture") continue;
                    if (c.media != null)
                    {
                        AddWord(c.media.word);
                        if (c.media.pictures != null)
                            foreach (var p in c.media.pictures)
                                if (p != null) AddWord(p.id);
                    }
                    if (c.words != null)
                        foreach (var w in c.words)
                            AddWord(w);
                }
            }
            if (bank.levels != null)
                foreach (var lv in bank.levels)
                    AddFrom(lv.challenges);
            AddFrom(bank.variants);
            pool.Sort(System.StringComparer.Ordinal);
            return pool;
        }

        public override void Build(UIManager m)
        {
            base.Build(m);
            var dim = UIKit.Panel(Panel, "Dim", new Color(0.05f, 0.10f, 0.18f, 0.72f));
            UIKit.Stretch(dim.rectTransform);

            var card = UIKit.Card(Panel, "Card", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1060, 820));

            var plaque = UIKit.Panel(card.gameObject, "HeaderPlaque", UIKit.DeepNavy, GameAssets.Ui("ui_btn"));
            plaque.raycastTarget = false;
            UIKit.Place(plaque.rectTransform, new Vector2(0.5f, 0.925f), Vector2.zero, new Vector2(990, 74));

            var title = UIKit.Label(card.gameObject, "Title", "My Words", 40, UIKit.Gold, TextAlignmentOptions.Center);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.925f), Vector2.zero, new Vector2(920, 58));

            var subtitle = UIKit.Label(card.gameObject, "Subtitle", "Pick up to 5 words to practice - they come back first!", 24, UIKit.Ink, TextAlignmentOptions.Center);
            UIKit.Place(subtitle.rectTransform, new Vector2(0.5f, 0.86f), Vector2.zero, new Vector2(980, 40));

            var grid = UIKit.Grid(card.gameObject, "WordGrid", new Vector2(216, 64), new Vector2(12, 10), 4, TextAnchor.UpperCenter);
            UIKit.Place(grid.GetComponent<RectTransform>(), new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(940, 460));

            _status = UIKit.Label(card.gameObject, "Status", "", 26, UIKit.WarmWood, TextAlignmentOptions.Center);
            UIKit.Place(_status.rectTransform, new Vector2(0.5f, 0.16f), Vector2.zero, new Vector2(960, 44));

            var done = UIKit.TextButton(card.gameObject, "Done", "Done", new Vector2(300, 68), UIKit.Teal,
                () =>
                {
                    M.HideOverlay("MyWords");
                    var pp = M.GetOverlay<ParentsPanel>();
                    if (pp != null) pp.OnShow();
                }, GameAssets.Ui("ui_btn"), 30);
            UIKit.Place(done.GetComponent<RectTransform>(), new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(300, 68));
            UIKit.AttachButtonIcon(done, GameAssets.Ui("ui_check"), 28f);
        }

        public override void OnShow()
        {
            var grid = Panel.transform.Find("Card/WordGrid").gameObject;
            // rebuild the grid each open (bank may have changed)
            for (int i = grid.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(grid.transform.GetChild(i).gameObject);

            var bank = GameManager.Instance != null ? GameManager.Instance.Bank : null;
            var pool = CollectPictureWords(bank);
            var focus = new List<string>(SaveManager.Instance.Data.focusWords);
            for (int i = 0; i < pool.Count; i++)
            {
                string word = pool[i];
                bool on = focus.Contains(word);
                UIKit.TextButton(grid, "W_" + word, word, new Vector2(216, 64),
                    on ? UIKit.Green : UIKit.Cream, () => ToggleWord(word), GameAssets.Ui("ui_btn"), 26);
            }
            RefreshStatus();
        }

        void ToggleWord(string word)
        {
            var data = SaveManager.Instance.Data;
            var list = new List<string>(data.focusWords != null ? data.focusWords : new string[0]);
            string lower = word.ToLower();
            if (list.Contains(lower))
            {
                list.Remove(lower);
            }
            else
            {
                if (list.Count >= 5)
                {
                    if (_status != null) _status.text = "Five words is plenty - remove one first!";
                    return;
                }
                list.Add(lower);
            }
            data.focusWords = list.ToArray();
            SaveManager.Instance.Save();
            OnShow(); // re-render toggle states
        }

        void RefreshStatus()
        {
            if (_status == null) return;
            int n = SaveManager.Instance.Data.focusWords != null ? SaveManager.Instance.Data.focusWords.Length : 0;
            _status.text = n == 0 ? "No focus words yet" : n + " of 5 focus words set";
        }
    }

    /// <summary>
    /// Calm rest screen shown when today's configured daily time limit is reached.
    /// </summary>
    public class TimeLimitPanel : OverlayPanel
    {
        public override string Name { get { return "TimeLimit"; } }

        TMP_Text _minutesText;

        public override void Build(UIManager m)
        {
            base.Build(m);
            var dim = UIKit.Panel(Panel, "Dim", new Color(0.05f, 0.10f, 0.18f, 0.72f));
            UIKit.Stretch(dim.rectTransform);

            var card = UIKit.Card(Panel, "Card", UIKit.Parchment, GameAssets.Ui("ui_panel"));
            UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860, 560));

            var plaque = UIKit.Panel(card.gameObject, "HeaderPlaque", UIKit.DeepNavy, GameAssets.Ui("ui_btn"));
            plaque.raycastTarget = false;
            UIKit.Place(plaque.rectTransform, new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(620, 82));

            var title = UIKit.Label(card.gameObject, "Title", "Time for a rest!", 52, UIKit.Gold, TextAlignmentOptions.Center);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(580, 76));

            var lumoHalo = UIKit.Img(card.gameObject, "LumoHalo", GameAssets.Ui("ui_node"), UIKit.SoftGold);
            lumoHalo.raycastTarget = false;
            UIKit.Place(lumoHalo.rectTransform, new Vector2(0.18f, 0.46f), Vector2.zero, new Vector2(210, 210));

            var lumo = UIKit.Img(card.gameObject, "Lumo", GameAssets.Char("char_lumo"), Color.white);
            UIKit.Place(lumo.rectTransform, new Vector2(0.18f, 0.46f), Vector2.zero, new Vector2(180, 180));

            var body = UIKit.Label(card.gameObject, "Body",
                "You had a great learning adventure today!\nCome back tomorrow, or ask a parent for more time.",
                28, UIKit.Ink, TextAlignmentOptions.Left);
            UIKit.Place(body.rectTransform, new Vector2(0.60f, 0.53f), Vector2.zero, new Vector2(510, 140));

            _minutesText = UIKit.Label(card.gameObject, "Minutes", "", 28, UIKit.WarmWood, TextAlignmentOptions.Left);
            UIKit.Place(_minutesText.rectTransform, new Vector2(0.60f, 0.34f), Vector2.zero, new Vector2(510, 50));

            var parents = UIKit.TextButton(card.gameObject, "Parents", "Parents", new Vector2(300, 76), UIKit.WarmWood,
                () => M.OpenParentsArea(), GameAssets.Ui("ui_btn"), 32);
            UIKit.Place(parents.GetComponent<RectTransform>(), new Vector2(0.30f, 0.14f), Vector2.zero, new Vector2(300, 76));
            UIKit.AttachButtonIcon(parents, GameAssets.Ui("ui_icon_settings"), 32f);

            // LXD Phase D1: out-game reflection entry point from the rest screen
            var recap = UIKit.TextButton(card.gameObject, "SeeMyDay", "See my day", new Vector2(300, 76), UIKit.Teal,
                () =>
                {
                    M.HideOverlay("TimeLimit");
                    M.Show("SessionRecap");
                }, GameAssets.Ui("ui_btn"), 32);
            UIKit.Place(recap.GetComponent<RectTransform>(), new Vector2(0.70f, 0.14f), Vector2.zero, new Vector2(300, 76));
            UIKit.AttachButtonIcon(recap, GameAssets.Ui("ui_icon_report"), 32f);
        }

        public override void OnShow()
        {
            if (SessionTimeManager.Instance != null)
                SessionTimeManager.Instance.TrackingPaused = true;

            int min = (SaveManager.Instance != null && SaveManager.Instance.Data != null)
                ? SaveManager.Instance.Data.todaySecondsPlayed / 60
                : 0;
            if (_minutesText != null)
                _minutesText.text = "Played today: " + min + " min";
        }

        public override void OnHide()
        {
            if (SessionTimeManager.Instance != null)
                SessionTimeManager.Instance.TrackingPaused = M.IsOverlayOpen("Parents") || M.IsOverlayOpen("ParentalGate");
        }
    }
}
