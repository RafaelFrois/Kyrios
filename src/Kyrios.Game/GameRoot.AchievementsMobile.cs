using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Kyrios.Game;

/// <summary>
/// Conquistas no celular: as mesmas abas e os mesmos cartões, só que maiores (duas colunas) e com rolagem vertical
/// contínua — arrastar o dedo rola, soltar com velocidade continua deslizando e freia sozinho. A lista é recortada
/// na própria área, então nenhum cartão invade o cabeçalho.
/// </summary>
public sealed partial class GameRoot
{
    private const int MobileAchievementColumns = 2;
    private const float MobileAchievementCardHeight = 100f;
    private const float MobileAchievementGap = 12f;
    private const float MobileAchievementListTop = 130f;

    private static readonly RasterizerState ClipRasterizer = new() { CullMode = CullMode.None, ScissorTestEnable = true };

    private float _achievementScrollPixels;
    private float _achievementVelocity;
    private bool _achievementDragging;

    private Rectangle MobileAchievementListRect => new(20, (int)MobileAchievementListTop, (int)AreaWidth - 52, (int)(AreaHeight - MobileAchievementListTop - 4f));

    private float MobileAchievementMaxScroll(int tab)
    {
        int rows = (AchievementsInTab(tab).Count + MobileAchievementColumns - 1) / MobileAchievementColumns;
        float content = (rows * (MobileAchievementCardHeight + MobileAchievementGap)) - MobileAchievementGap;
        return MathF.Max(0f, content - MobileAchievementListRect.Height);
    }

    private void UpdateMobileAchievementsPage()
    {
        if (_input.Back || WasBackButtonClicked())
        {
            _audio.PlayMenuConfirm();
            _state = _achievementsReturnState;
            return;
        }

        int tab = _achievementTab;
        if (_input.MenuRight)
        {
            tab = (tab + 1) % AchievementTabCount;
        }
        else if (_input.MenuLeft)
        {
            tab = (tab - 1 + AchievementTabCount) % AchievementTabCount;
        }

        Point point = LogicalMousePoint();
        if (MouseClicked)
        {
            for (int i = 0; i < AchievementTabCount; i++)
            {
                if (InflateRect(AchievementTabRect(i), 3f, 6f).Contains(point))
                {
                    tab = i;
                }
            }
        }

        if (tab != _achievementTab)
        {
            _achievementTab = tab;
            _achievementScrollPixels = 0f;
            _achievementVelocity = 0f;
            _audio.PlayMenuMove();
        }

        float pitch = MobileAchievementCardHeight + MobileAchievementGap;
        if (_input.MenuDown)
        {
            _achievementScrollPixels += pitch;
            _achievementVelocity = 0f;
        }
        else if (_input.MenuUp)
        {
            _achievementScrollPixels -= pitch;
            _achievementVelocity = 0f;
        }

        _achievementScrollPixels -= _input.ScrollWheelSteps * pitch;

        // Arrastar: a lista acompanha o dedo; ao soltar, segue na velocidade do gesto e vai freando.
        if (_input.WasMouseLeftJustPressed && MobileAchievementListRect.Contains(point))
        {
            _achievementDragging = true;
            _achievementVelocity = 0f;
        }

        float dt = MathF.Max(1f / 240f, _frameSeconds);
        if (_achievementDragging && _input.IsMouseLeftDown)
        {
            float delta = _input.DragDeltaY / MathF.Max(0.01f, CurrentScreenScale);
            _achievementScrollPixels -= delta;
            _achievementVelocity = MathHelper.Lerp(_achievementVelocity, -delta / dt, 0.5f);
        }
        else
        {
            _achievementDragging = false;
            _achievementScrollPixels += _achievementVelocity * dt;
            _achievementVelocity *= MathF.Exp(-4.5f * dt);
            if (MathF.Abs(_achievementVelocity) < 8f)
            {
                _achievementVelocity = 0f;
            }
        }

        float max = MobileAchievementMaxScroll(_achievementTab);
        if (_achievementScrollPixels < 0f || _achievementScrollPixels > max)
        {
            _achievementScrollPixels = Math.Clamp(_achievementScrollPixels, 0f, max);
            _achievementVelocity = 0f;
        }
    }

    private void DrawMobileAchievementsPage()
    {
        DimScreen(MenuBackgroundDim);

        int unlockedCount = Achievements.UnlockedCount(_saveData);
        int totalCount = Achievements.All.Count;
        int secretsLeft = Achievements.All.Count(a => a.IsSecret && !IsAchievementUnlocked(a));
        DrawScreenHeader(L.T("CONQUISTAS", "ACHIEVEMENTS"), secretsLeft > 0 ? L.T($"{secretsLeft} SECRETAS AINDA ESCONDIDAS", $"{secretsLeft} SECRETS STILL HIDDEN") : L.T("TODAS AS SECRETAS REVELADAS!", "ALL SECRETS REVEALED!"));

        float fraction = totalCount == 0 ? 0f : unlockedCount / (float)totalCount;
        const float progressWidth = 420f;
        float progressX = AreaWidth - 20f - progressWidth;
        string countText = L.T($"{unlockedCount} / {totalCount}", $"{unlockedCount} / {totalCount}");
        string percentText = L.T($"{MathF.Floor(fraction * 100f):0}% CONCLUIDO", $"{MathF.Floor(fraction * 100f):0}% COMPLETE");
        PixelFont.DrawShadowed(_spriteBatch, _pixel, countText, new Vector2(progressX, 10f), 2.4f, TextColor);
        PixelFont.DrawShadowed(_spriteBatch, _pixel, percentText, new Vector2(AreaWidth - 20f - PixelFont.Measure(percentText, 2.2f), 12f), 2.2f, RecordColor);
        DrawProgressBar(new Rectangle((int)progressX, 38, (int)progressWidth, 14), fraction, AccentColor, 7f);

        for (int i = 0; i < AchievementTabCount; i++)
        {
            DrawAchievementTab(AchievementTabRect(i), i, selected: i == _achievementTab, hovered: false);
        }

        List<Achievement> items = AchievementsInTab(_achievementTab);
        Rectangle list = MobileAchievementListRect;
        float cardWidth = (list.Width - ((MobileAchievementColumns - 1) * MobileAchievementGap)) / MobileAchievementColumns;
        float pitch = MobileAchievementCardHeight + MobileAchievementGap;

        // Só os cartões visíveis são desenhados, recortados na área da lista.
        _spriteBatch.End();
        Rectangle previousScissor = GraphicsDevice.ScissorRectangle;
        GraphicsDevice.ScissorRectangle = LogicalRectToScreen(list);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: ClipRasterizer, transformMatrix: _screenTransform);

        int firstRow = Math.Max(0, (int)(_achievementScrollPixels / pitch));
        int lastRow = (int)((_achievementScrollPixels + list.Height) / pitch);
        for (int row = firstRow; row <= lastRow; row++)
        {
            for (int column = 0; column < MobileAchievementColumns; column++)
            {
                int i = (row * MobileAchievementColumns) + column;
                if (i >= items.Count)
                {
                    break;
                }

                var card = new Rectangle(
                    (int)(list.X + (column * (cardWidth + MobileAchievementGap))),
                    (int)(list.Y + (row * pitch) - _achievementScrollPixels),
                    (int)cardWidth,
                    (int)MobileAchievementCardHeight);
                DrawAchievementCard(items[i], card);
            }
        }

        _spriteBatch.End();
        GraphicsDevice.ScissorRectangle = previousScissor;
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _screenTransform);

        float max = MobileAchievementMaxScroll(_achievementTab);
        if (max > 0f)
        {
            var track = new Rectangle(list.Right + 12, list.Y, 8, list.Height);
            DrawRoundedRect(track, new Color(38, 42, 56), 4f);
            float visible = list.Height / (list.Height + max);
            int thumbHeight = Math.Max(36, (int)(track.Height * visible));
            int thumbY = track.Y + (int)((track.Height - thumbHeight) * (_achievementScrollPixels / max));
            DrawRoundedRect(new Rectangle(track.X, thumbY, track.Width, thumbHeight), AccentColor, 4f);
        }
    }

    /// <summary>Um retângulo lógico em pixels da tela (limitado à tela) — pra recortar com o scissor.</summary>
    private Rectangle LogicalRectToScreen(Rectangle logical)
    {
        Vector2 topLeft = LogicalToScreen(new Vector2(logical.X, logical.Y));
        Vector2 bottomRight = LogicalToScreen(new Vector2(logical.Right, logical.Bottom));
        int width = GraphicsDevice.PresentationParameters.BackBufferWidth;
        int height = GraphicsDevice.PresentationParameters.BackBufferHeight;
        int x = Math.Clamp((int)topLeft.X, 0, width);
        int y = Math.Clamp((int)topLeft.Y, 0, height);
        int right = Math.Clamp((int)MathF.Ceiling(bottomRight.X), x, width);
        int bottom = Math.Clamp((int)MathF.Ceiling(bottomRight.Y), y, height);
        return new Rectangle(x, y, right - x, bottom - y);
    }
}
