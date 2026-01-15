// Copyright 2025 Takuto Nakamura
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using RunCat365.Properties;
using FormsTimer = System.Windows.Forms.Timer;

namespace RunCat365
{
    internal class EndlessGameForm : Form
    {
        private const int JUMP_THREDHOLD = 17;
        private readonly FormsTimer timer;
        private readonly Theme systemTheme;
        private GameStatus status = GameStatus.NewGame;
        private Cat cat = new Cat.Running(Cat.Running.Frame.Frame0);
        private readonly List<Road> roads = [];
        private int counter = 0;
        private int limit = 5;
        private int score = 0;
        private bool isJumpRequested = false;
        private readonly bool isAutoPlay = false;

        internal EndlessGameForm(Theme systemTheme)
        {
            this.systemTheme = systemTheme;

            DoubleBuffered = true;
            ClientSize = new Size(600, 250);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = Strings.Window_EndlessGame;
            Icon = Resources.AppIcon;
            BackColor = systemTheme == Theme.Light ? Color.Gainsboro : Color.Gray;

            Paint += RenderScene;

            KeyDown += HandleKeyDown;

            timer = new FormsTimer
            {
                Interval = 100
            };
            timer.Tick += GameTick;

            Initialize();

            timer.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            timer.Stop();
            timer.Dispose();
        }

        private void Initialize()
        {
            counter = JUMP_THREDHOLD;
            isJumpRequested = false;
            score = 0;
            cat = new Cat.Running(Cat.Running.Frame.Frame0);
            roads.RemoveAll(r => r == Road.Sprout);

            var random = new Random();
            var roadsToAdd = 20 - roads.Count;
            for (int i = 0; i < roadsToAdd; i++)
            {
                roads.Add((Road)random.Next(0, 3));
            }
        }

        private bool Judge()
        {
            if (status != GameStatus.Playing) return false;

            var sproutIndices = roads
                .Select((road, index) => road == Road.Sprout ? index : (int?)null)
                .OfType<int>()
                .ToList();

            if (cat.ViolationIndices().HasCommonElements(sproutIndices))
            {
                status = GameStatus.GameOver;
                return false;
            }
            return true;
        }

        private void UpdateRoads()
        {
            var firstRoad = roads.First();
            roads.RemoveAt(0);
            if (firstRoad == Road.Sprout)
            {
                score += 1;
            }
            counter = counter > 0 ? counter - 1 : limit - 1;
            if (counter == 0)
            {
                var randomValue = new Random().Next(0, 27);
                var subRoads = new List<Road>();
                if (randomValue % 3 == 0)
                {
                    subRoads.Add(Road.Sprout);
                }
                if (randomValue % 9 == 0)
                {
                    subRoads.Add(Road.Sprout);
                }
                if (randomValue % 27 == 0)
                {
                    subRoads.Add(Road.Sprout);
                }
                roads.AddRange(subRoads);
                limit = subRoads.Count == 0 ? 5 : 10;
            }
            if (roads.Count < 20)
            {
                roads.Add((Road)(new Random().Next(0, 3)));
            }
        }

        private void UpdateCat()
        {
            if (cat is Cat.Running runningCat)
            {
                if (runningCat.CurrentFrame == Cat.Running.Frame.Frame4 && isJumpRequested)
                {
                    cat = new Cat.Jumping(Cat.Jumping.Frame.Frame0);
                    isJumpRequested = false;
                    return;
                }
            }
            else if (cat is Cat.Jumping jumpingCat)
            {
                if (jumpingCat.CurrentFrame == Cat.Jumping.Frame.Frame9)
                {
                    if (isJumpRequested)
                    {
                        cat = cat.Next();
                        isJumpRequested = false;
                        return;
                    }
                    else
                    {
                        cat = new Cat.Running(Cat.Running.Frame.Frame0);
                        return;
                    }
                }
            }
            cat = cat.Next();
        }

        private void AutoJump()
        {
            if (isAutoPlay && roads[JUMP_THREDHOLD - 1] == Road.Sprout)
            {
                isJumpRequested = true;
            }
        }

        private void GameTick(object? sender, EventArgs e)
        {
            if (Judge())
            {
                UpdateRoads();
                UpdateCat();
                AutoJump();
            }
            Invalidate();
        }

        private void HandleKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                switch (status)
                {
                    case GameStatus.NewGame:
                    case GameStatus.GameOver:
                        Initialize();
                        status = GameStatus.Playing;
                        break;
                    case GameStatus.Playing when !isAutoPlay:
                        isJumpRequested = true;
                        break;
                    default:
                        break;
                }
            }
        }

        private void RenderScene(object? sender, PaintEventArgs e)
        {
            var resourceManager = Resources.ResourceManager;
            var prefix = systemTheme.GetString();
            var textColor = systemTheme == Theme.Light ? Color.Black : Color.White;
            var graphics = e.Graphics;

            RenderScore(graphics, textColor);
            RenderRoads(graphics, resourceManager, prefix);
            RenderCat(graphics, resourceManager, prefix);
            RenderOverlay(graphics, textColor);
        }

        private void RenderScore(Graphics graphics, Color textColor)
        {
            using var font = new Font("Consolas", 15);
            using var brush = new SolidBrush(textColor);
            var stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Center
            };
            graphics.DrawString($"{Strings.Game_Score}: {score}", font, brush, new Rectangle(20, 0, 560, 50), stringFormat);
        }

        private void RenderRoads(Graphics graphics, System.Resources.ResourceManager resourceManager, string prefix)
        {
            for (int index = 0; index < Math.Min(20, roads.Count); index++)
            {
                var road = roads[index];
                var fileName = $"{prefix}_road_{road.GetString()}".ToLower();
                using var image = resourceManager.GetObject(fileName) as Bitmap;
                if (image is null) continue;
                graphics.DrawImage(image, new Rectangle(index * 30, 200, 30, 50));
            }
        }

        private void RenderCat(Graphics graphics, System.Resources.ResourceManager resourceManager, string prefix)
        {
            var fileName = $"{prefix}_cat_{cat.GetString()}".ToLower();
            using var image = resourceManager.GetObject(fileName) as Bitmap;
            if (image is null) return;
            graphics.DrawImage(image, new Rectangle(120, 130, 120, 100));
        }

        private void RenderOverlay(Graphics graphics, Color textColor)
        {
            if (status == GameStatus.Playing) return;

            using var fillBrush = new SolidBrush(Color.FromArgb(77, 0, 0, 0));
            graphics.FillRectangle(fillBrush, new Rectangle(0, 0, 600, 250));

            using var font = new Font("Segoe UI", 16, FontStyle.Bold);
            using var brush = new SolidBrush(textColor);
            var message = status == GameStatus.GameOver
                ? $"{Strings.Game_GameOver}\n{Strings.Game_PressSpaceToPlay}"
                : Strings.Game_PressSpaceToPlay;
            var stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            graphics.DrawString(message, font, brush, new Rectangle(0, 0, 600, 250), stringFormat);
        }
    }

    internal static class ListExtension
    {
        internal static bool HasCommonElements(this List<int> list1, List<int> list2)
        {
            return list1.Intersect(list2).Any();
        }
    }
}
