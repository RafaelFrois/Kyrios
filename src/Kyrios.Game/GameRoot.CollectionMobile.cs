using Microsoft.Xna.Framework;

namespace Kyrios.Game;

/// <summary>
/// A coleção de skins/pistas no celular: em vez da grade miúda do desktop, uma vitrine grande do item atual com
/// ◄ ► (e deslizar o dedo na vitrine) pra passar, uma faixa com os vizinhos pra pular direto, e o painel com nome,
/// categoria, estado, o que falta pra liberar (com progresso) e o botão EQUIPAR. A regra é a mesma do desktop
/// (<see cref="ConfirmCollection"/>): olhar não troca a escolha; só EQUIPAR, e só se estiver liberado.
/// </summary>
public sealed partial class GameRoot
{
    private const int CollectionStripTiles = 7;

    private static readonly Rectangle MobileCollectionPrev = new(12, 150, 76, 150);
    private static readonly Rectangle MobileCollectionStage = new(96, 70, 528, 300);
    private static readonly Rectangle MobileCollectionNext = new(632, 150, 76, 150);
    private static readonly Rectangle MobileCollectionInfo = new(718, 70, 456, 402);

    private Rectangle MobileEquipButton => new(MobileCollectionInfo.X + 24, MobileCollectionInfo.Bottom - 90, MobileCollectionInfo.Width - 48, 74);

    /// <summary>Deslize em andamento na vitrine (pixels lógicos) e a animação de troca (-1..1, volta a 0).</summary>
    private float _collectionSwipe;
    private bool _collectionSwiping;
    private float _collectionSlide;
    private float _collectionArrowFlash;
    private int _collectionArrowFlashSide;

    private static Rectangle MobileStripTile(int slot)
    {
        const int size = 84;
        const int gap = 10;
        int width = (CollectionStripTiles * size) + ((CollectionStripTiles - 1) * gap);
        int left = MobileCollectionStage.Center.X - (width / 2);
        return new Rectangle(left + (slot * (size + gap)), 382, size, size);
    }

    /// <summary>O item mostrado numa casa da faixa (a do meio é o atual; as outras, os vizinhos, dando a volta).</summary>
    private int StripItem(int slot)
    {
        int count = CollectionCount;
        return (((CollectionIndex + slot - (CollectionStripTiles / 2)) % count) + count) % count;
    }

    private void StepCollection(int direction)
    {
        int count = CollectionCount;
        CollectionIndex = (((CollectionIndex + direction) % count) + count) % count;
        _collectionSlide = direction;
        _collectionArrowFlash = 0.2f;
        _collectionArrowFlashSide = direction;
        _audio.PlayMenuMove();
    }

    private void UpdateMobileCollection()
    {
        _collectionSlide = MathF.Abs(_collectionSlide) < 0.05f ? 0f : _collectionSlide * MathF.Exp(-14f * _frameSeconds);
        _collectionArrowFlash = MathF.Max(0f, _collectionArrowFlash - _frameSeconds);

        if (_input.Back || WasBackButtonClicked())
        {
            _audio.PlayMenuConfirm();
            _state = State.MainMenu;
            return;
        }

        // Controle/teclado continuam valendo (Chromebook, gamepad).
        if (_input.MenuLeft || _input.MenuUp)
        {
            StepCollection(-1);
        }
        else if (_input.MenuRight || _input.MenuDown)
        {
            StepCollection(1);
        }

        if (_input.Confirm)
        {
            ConfirmCollection();
        }

        Point point = LogicalMousePoint();
        if (MouseClicked)
        {
            if (InflateRect(MobileCollectionPrev, 10f, 10f).Contains(point))
            {
                StepCollection(-1);
                return;
            }

            if (InflateRect(MobileCollectionNext, 10f, 10f).Contains(point))
            {
                StepCollection(1);
                return;
            }

            if (MobileEquipButton.Contains(point))
            {
                ConfirmCollection();
                return;
            }

            for (int slot = 0; slot < CollectionStripTiles; slot++)
            {
                if (MobileStripTile(slot).Contains(point) && slot != CollectionStripTiles / 2)
                {
                    int direction = Math.Sign(slot - (CollectionStripTiles / 2));
                    CollectionIndex = StripItem(slot);
                    _collectionSlide = direction;
                    _audio.PlayMenuMove();
                    return;
                }
            }

            if (MobileCollectionStage.Contains(point))
            {
                _collectionSwiping = true;
                _collectionSwipe = 0f;
            }
        }

        if (!_collectionSwiping)
        {
            return;
        }

        _collectionSwipe += _input.DragDeltaX / MathF.Max(0.01f, CurrentScreenScale);
        if (!_input.IsMouseLeftDown)
        {
            // Deslizar pra esquerda mostra o próximo (como virar a página).
            if (MathF.Abs(_collectionSwipe) > 60f)
            {
                StepCollection(_collectionSwipe < 0f ? 1 : -1);
            }

            _collectionSwiping = false;
            _collectionSwipe = 0f;
        }
    }

    private void DrawMobileCollection()
    {
        DimScreen(CollectionIsTracks ? Color.Black * 0.5f : MenuBackgroundDim);
        (int earned, int earnable) = CollectionIsTracks
            ? Unlockables.Count(TrackThemes.All, _saveData.UnlockedTrackIds)
            : Unlockables.Count(CarSkins.All, _saveData.UnlockedSkinIds);
        string noun = CollectionIsTracks ? L.T("PISTAS", "TRACKS") : "SKINS";
        DrawScreenHeader(noun, L.T($"{earned + 1}/{earnable + 1} {noun} DESBLOQUEADAS", $"{earned + 1}/{earnable + 1} {noun} UNLOCKED"));

        int index = CollectionIndex;
        bool unlocked = CollectionUnlocked(index);
        bool hidden = CollectionHidden(index);
        bool equipped = CollectionEquipped(index);

        // Vitrine (com o balanço de "bloqueado" e o deslize em andamento).
        float shake = _denyShake > 0f ? MathF.Sin(_denyShake * 60f) * 6f * (_denyShake / 0.35f) : 0f;
        float drag = _collectionSwiping ? Math.Clamp(_collectionSwipe * 0.35f, -40f, 40f) : 0f;
        Rectangle stage = MobileCollectionStage;
        Color frame = _equipFlash > 0f ? Color.Lerp(AccentColor, Color.White, _equipFlash) : equipped ? AccentColor : unlocked ? PanelBorderColor : new Color(80, 84, 100);
        DrawRoundedRect(InflateRect(stage, 4f, 4f), frame, 10f);
        DrawRoundedRect(stage, new Color(18, 20, 30), 8f);
        Rectangle content = stage;
        content.X += (int)(shake + drag + (_collectionSlide * 30f));
        if (CollectionIsTracks)
        {
            if (hidden)
            {
                DrawBigQuestionMark(stage);
            }
            else
            {
                // A miniatura mantém a proporção da pista (o fundo da tela inteira já é a prévia ao vivo).
                int height = (int)(content.Width / 2.25f);
                var preview = new Rectangle(content.X, content.Center.Y - (height / 2), content.Width, height);
                _scenery.DrawPreview(TrackThemes.All[index], preview, unlocked ? Color.White : new Color(70, 70, 82));
            }
        }
        else
        {
            DrawSkinStage(content, CarSkins.All[index], unlocked, hidden);
        }

        if (!unlocked)
        {
            DrawLock(stage.Center.ToVector2() + new Vector2(0f, 6f), 2.6f, AccentColor);
        }

        string counter = $"{index + 1}/{CollectionCount}";
        PixelFont.DrawShadowed(_spriteBatch, _pixel, counter, new Vector2(stage.Right - PixelFont.Measure(counter, 2f) - 12f, stage.Y + 10f), 2f, StatBadgeLabelColor);

        bool flash = _collectionArrowFlash > 0f;
        DrawArrowButton(MobileCollectionPrev, pointRight: false, hovered: false, flashing: flash && _collectionArrowFlashSide < 0);
        DrawArrowButton(MobileCollectionNext, pointRight: true, hovered: false, flashing: flash && _collectionArrowFlashSide > 0);

        for (int slot = 0; slot < CollectionStripTiles; slot++)
        {
            int item = StripItem(slot);
            bool current = slot == CollectionStripTiles / 2;
            float fade = 1f - (MathF.Abs(slot - (CollectionStripTiles / 2)) * 0.12f);
            Rectangle tile = MobileStripTile(slot);
            DrawCollectionTile(item, tile, current, hovered: false);
            if (!current)
            {
                DrawRoundedRect(tile, Color.Black * (1f - fade), 5f);
            }
        }

        DrawMobileCollectionInfo(index, unlocked, hidden, equipped);
    }

    /// <summary>O histórico do jogador com o item liberado: partidas, vitórias na Mortal e o melhor resultado.</summary>
    private void DrawCollectionStats(Rectangle area, int index)
    {
        string id = CollectionItem(index).Id;
        int games = (CollectionIsTracks ? _saveData.GamesByTrack : _saveData.GamesBySkin).GetValueOrDefault(id);
        int wins = (CollectionIsTracks ? _saveData.WinsByTrack : _saveData.WinsBySkin).GetValueOrDefault(id);
        string best = CollectionIsTracks
            ? (_saveData.BestLapByTrack.TryGetValue(id, out float lap) ? TimeFormat.Precise(lap) : "--")
            : (_saveData.BestScoreBySkin.TryGetValue(id, out float score) ? $"{score:0} PTS" : "--");
        (string Label, string Value)[] stats =
        [
            (L.T("PARTIDAS", "RACES"), games.ToString()),
            (L.T("VITORIAS", "WINS"), wins.ToString()),
            (CollectionIsTracks ? L.T("MELHOR VOLTA", "BEST LAP") : L.T("RECORDE", "RECORD"), best),
        ];

        if (area.Height < 60)
        {
            return;
        }

        int width = (area.Width - 20) / stats.Length;
        for (int i = 0; i < stats.Length; i++)
        {
            var box = new Rectangle(area.X + (i * (width + 10)), area.Y, width, 64);
            DrawRoundedRect(box, new Color(26, 30, 44), 8f);
            DrawCenteredText(box, stats[i].Value, box.Y + 10f, FitTextSize(stats[i].Value, box.Width - 12f, 2.6f), TextColor, shadow: true);
            DrawCenteredText(box, stats[i].Label, box.Y + 40f, FitTextSize(stats[i].Label, box.Width - 12f, 1.7f), StatBadgeLabelColor);
        }
    }

    /// <summary>Painel da direita: nome, selos, descrição/requisito e o botão de equipar.</summary>
    private void DrawMobileCollectionInfo(int index, bool unlocked, bool hidden, bool equipped)
    {
        Rectangle info = MobileCollectionInfo;
        DrawPanel(info);
        var column = new Rectangle(info.X + 20, 0, info.Width - 40, 0);

        string name = hidden ? (CollectionIsTracks ? L.T("PISTA SECRETA", "SECRET TRACK") : L.T("SKIN SECRETA", "SECRET SKIN")) : CollectionIsTracks ? TrackThemes.All[index].Name : CarSkins.All[index].Name;
        DrawCenteredText(column, name, info.Y + 20f, FitTextSize(name, column.Width, 3.6f), unlocked ? TextColor : MutedLabelColor, shadow: true);
        DrawCollectionChips(new Vector2(info.Center.X, info.Y + 76f), index, unlocked, equipped, hidden);

        var body = new Rectangle(info.X + 20, info.Y + 104, info.Width - 40, info.Height - 104 - 100);
        if (unlocked)
        {
            string line = CollectionIsTracks ? TrackThemes.All[index].Tagline : equipped ? L.T("PRONTA PARA CORRER", "READY TO RACE") : L.T("TOQUE EM EQUIPAR PARA USAR", "TAP EQUIP TO USE IT");
            float y = body.Y + 8f;
            foreach (string wrapped in WrapText(line, body.Width, 2.1f, maxLines: 3))
            {
                DrawCenteredText(body, wrapped, y, 2.1f, StatBadgeLabelColor);
                y += PixelFont.LineHeight(2.1f) + 6f;
            }

            DrawCollectionStats(new Rectangle(body.X, (int)y + 14, body.Width, body.Bottom - (int)y - 14), index);
        }
        else if (hidden)
        {
            DrawCenteredText(body, "\"???\"", body.Y + 10f, 2.4f, AccentColor);
            float y = body.Y + 44f;
            foreach (string wrapped in WrapText(L.T("ALGUNS SEGREDOS SO APARECEM PRA QUEM PROCURA", "SOME SECRETS ONLY SHOW UP FOR THOSE WHO SEARCH"), body.Width, 1.9f, maxLines: 3))
            {
                DrawCenteredText(body, wrapped, y, 1.9f, StatBadgeLabelColor);
                y += PixelFont.LineHeight(1.9f) + 6f;
            }
        }
        else
        {
            DrawRequirementChecklist(body, CollectionItem(index).Requirement);
        }

        string label = !unlocked ? L.T("BLOQUEADA", "LOCKED") : equipped ? L.T("EQUIPADA", "EQUIPPED") : L.T("EQUIPAR", "EQUIP");
        DrawButton(MobileEquipButton, label, focused: unlocked && !equipped, primary: unlocked && !equipped, textSize: 3f);
        if (!unlocked)
        {
            DrawLock(new Vector2(MobileEquipButton.Right - 34f, MobileEquipButton.Center.Y + 4f), 1f, MutedLabelColor);
        }
    }
}
