using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CeoShootMain
{
    public class WelcomeForm : Form
    {
        public bool IsAutostartEnabled { get; private set; } = true;
        public string SelectedFormat { get; private set; } = "PNG";
        public string SelectedColorHex { get; private set; } = "#00F0FF";

        private int _currentStep = 1;
        private const int TotalSteps = 3;

        private readonly Color _bgColor = Color.FromArgb(11, 11, 16);
        private readonly Color _cardColor = Color.FromArgb(20, 20, 28);
        private readonly Color _mutedText = Color.FromArgb(130, 130, 150);

        private readonly string[] _neonPalette = { "#00F0FF", "#FF0055", "#00FF66", "#9900FF" };

        private Rectangle _btnNext, _btnBack, _btnClose;
        private Rectangle _cardAutostart, _cardPng, _cardJpg;
        private Rectangle[] _colorCircles = new Rectangle[4];

        private bool _isNextHover, _isBackHover, _isCloseHover, _isDragging;
        private Point _dragStart;

        public WelcomeForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(620, 460);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true;

            UpdateLayoutGeometry();
            BindEvents();
        }

        private void UpdateLayoutGeometry()
        {
            _btnClose = new Rectangle(this.Width - 40, 15, 25, 25);
            _btnNext = new Rectangle(this.Width - 140, this.Height - 60, 110, 40);
            _btnBack = new Rectangle(30, this.Height - 60, 110, 40);

            _cardAutostart = new Rectangle(50, 135, this.Width - 100, 60);
            _cardPng = new Rectangle(50, 235, 250, 75);
            _cardJpg = new Rectangle(this.Width - 300, 235, 250, 75);

            for (int i = 0; i < 4; i++)
            {
                _colorCircles[i] = new Rectangle(50 + (i * 55), 355, 36, 36);
            }
        }

        private void BindEvents()
        {
            this.MouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Left) return;

                if (_btnClose.Contains(e.Location))
                {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    return;
                }

                if (_btnNext.Contains(e.Location))
                {
                    if (_currentStep < TotalSteps)
                    {
                        _currentStep++;
                        this.Invalidate();
                    }
                    else
                    {
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    return;
                }

                if (_btnBack.Contains(e.Location) && _currentStep > 1)
                {
                    _currentStep--;
                    this.Invalidate();
                    return;
                }

                if (_currentStep == 2)
                {
                    if (_cardAutostart.Contains(e.Location))
                    {
                        IsAutostartEnabled = !IsAutostartEnabled;
                        this.Invalidate();
                        return;
                    }
                    if (_cardPng.Contains(e.Location))
                    {
                        SelectedFormat = "PNG";
                        this.Invalidate();
                        return;
                    }
                    if (_cardJpg.Contains(e.Location))
                    {
                        SelectedFormat = "JPG";
                        this.Invalidate();
                        return;
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        if (_colorCircles[i].Contains(e.Location))
                        {
                            SelectedColorHex = _neonPalette[i];
                            this.Invalidate();
                            return;
                        }
                    }
                }

                if (e.Y < 60)
                {
                    _isDragging = true;
                    _dragStart = e.Location;
                }
            };

            this.MouseMove += (s, e) =>
            {
                if (_isDragging)
                {
                    Point p = PointToScreen(e.Location);
                    this.Location = new Point(p.X - _dragStart.X, p.Y - _dragStart.Y);
                    return;
                }

                _isNextHover = _btnNext.Contains(e.Location);
                _isBackHover = _btnBack.Contains(e.Location) && _currentStep > 1;
                _isCloseHover = _btnClose.Contains(e.Location);

                bool isHand = _isNextHover || _isBackHover || _isCloseHover;
                if (_currentStep == 2)
                {
                    if (_cardAutostart.Contains(e.Location) || _cardPng.Contains(e.Location) || _cardJpg.Contains(e.Location))
                    {
                        isHand = true;
                    }
                    if (!isHand)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            if (_colorCircles[i].Contains(e.Location))
                            {
                                isHand = true;
                                break;
                            }
                        }
                    }
                }

                this.Cursor = isHand ? Cursors.Hand : Cursors.Default;
                this.Invalidate();
            };

            this.MouseUp += (s, e) => _isDragging = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Color accent = ColorTranslator.FromHtml(SelectedColorHex);

            using (SolidBrush bgBrush = new SolidBrush(_bgColor))
            {
                g.FillRectangle(bgBrush, 0, 0, Width, Height);
            }
            using (Pen borderPen = new Pen(Color.FromArgb(50, accent), 2))
            {
                g.DrawRectangle(borderPen, 1, 1, Width - 2, Height - 2);
            }

            DrawStepTracker(g, accent);

            if (_isCloseHover)
            {
                using (SolidBrush closeBg = new SolidBrush(Color.FromArgb(30, 255, 255, 255)))
                {
                    g.FillEllipse(closeBg, _btnClose);
                }
            }
            using (Font fCross = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (SolidBrush crossBrush = new SolidBrush(_isCloseHover ? Color.White : _mutedText))
            {
                g.DrawString("✕", fCross, crossBrush, _btnClose.X + 4, _btnClose.Y + 2);
            }

            switch (_currentStep)
            {
                case 1: DrawWelcomePage(g, accent); break;
                case 2: DrawCustomizationPage(g, accent); break;
                case 3: DrawFinalPage(g, accent); break;
            }

            DrawNavigation(g, accent);
        }

        private void DrawStepTracker(Graphics g, Color accent)
        {
            int startX = 60;
            int endX = this.Width - 60;
            int y = 45;

            using (Pen baseLine = new Pen(Color.FromArgb(35, 35, 45), 4))
            {
                g.DrawLine(baseLine, startX, y, endX, y);
            }

            int fillWidth = startX + ((this.Width - 120) / (TotalSteps - 1)) * (_currentStep - 1);
            if (_currentStep > 1)
            {
                using (Pen progressLine = new Pen(accent, 4))
                {
                    g.DrawLine(progressLine, startX, y, fillWidth, y);
                }
            }

            for (int i = 1; i <= TotalSteps; i++)
            {
                int cx = startX + ((this.Width - 120) / (TotalSteps - 1)) * (i - 1);
                bool activeOrPassed = i <= _currentStep;
                using (SolidBrush b = new SolidBrush(activeOrPassed ? accent : Color.FromArgb(45, 45, 55)))
                {
                    g.FillEllipse(b, cx - 8, y - 6, 14, 14);
                }

                if (i == _currentStep)
                {
                    using (Pen glow = new Pen(Color.FromArgb(90, accent), 3))
                    {
                        g.DrawEllipse(glow, cx - 12, y - 10, 22, 22);
                    }
                }
            }
        }

        private void DrawWelcomePage(Graphics g, Color accent)
        {
            using (Font fTitle = new Font("Segoe UI", 24f, FontStyle.Bold))
            using (Font fSubtitle = new Font("Segoe UI", 12f, FontStyle.Regular))
            using (SolidBrush accentBrush = new SolidBrush(accent))
            using (SolidBrush textBrush = new SolidBrush(_mutedText))
            {
                g.DrawString("CEOSHOOT", fTitle, accentBrush, 50, 120);
                g.DrawString("Initial environment deployment.", fSubtitle, Brushes.White, 50, 175);
                string desc = "Welcome. This wizard will initialize your workspace, configure local files, and hook capture keys.";
                g.DrawString(desc, new Font("Segoe UI", 10.5f), textBrush, new RectangleF(50, 215, this.Width - 100, 120));
            }
        }

        private void DrawCustomizationPage(Graphics g, Color accent)
        {
            using (GraphicsPath p = GetRoundedPath(_cardAutostart, 10))
            using (SolidBrush cardBrush = new SolidBrush(_cardColor))
            {
                g.FillPath(cardBrush, p);
            }

            using (Pen border = new Pen(IsAutostartEnabled ? accent : Color.FromArgb(45, 45, 55), 1.5f))
            using (GraphicsPath p = GetRoundedPath(_cardAutostart, 10))
            {
                g.DrawPath(border, p);
            }

            using (Font f = new Font("Segoe UI", 11f, FontStyle.Bold))
            {
                g.DrawString("Launch CEOSHOOT on system startup", f, Brushes.White, _cardAutostart.X + 20, _cardAutostart.Y + 18);
            }

            Rectangle toggle = new Rectangle(_cardAutostart.Right - 70, _cardAutostart.Y + 18, 46, 24);
            using (GraphicsPath tp = GetRoundedPath(toggle, 11))
            using (SolidBrush toggleBg = new SolidBrush(IsAutostartEnabled ? accent : Color.FromArgb(65, 65, 75)))
            {
                g.FillPath(toggleBg, tp);
            }
            g.FillEllipse(Brushes.White, IsAutostartEnabled ? toggle.X + 24 : toggle.X + 4, toggle.Y + 3, 18, 18);

            DrawFormatOption(g, _cardPng, "PNG Image Format", "Lossless compression (Recommended)", SelectedFormat == "PNG", accent);
            DrawFormatOption(g, _cardJpg, "JPG Image Format", "Compact disk footprint storage size", SelectedFormat == "JPG", accent);

            using (Font fSec = new Font("Segoe UI", 11f, FontStyle.Bold))
            {
                g.DrawString("Interface accent identity:", fSec, Brushes.White, 50, 325);
            }

            for (int i = 0; i < 4; i++)
            {
                using (SolidBrush cb = new SolidBrush(ColorTranslator.FromHtml(_neonPalette[i])))
                {
                    g.FillEllipse(cb, _colorCircles[i]);
                }
                if (SelectedColorHex == _neonPalette[i])
                {
                    using (Pen cp = new Pen(Color.White, 2f))
                    {
                        g.DrawEllipse(cp, _colorCircles[i].X - 4, _colorCircles[i].Y - 4, _colorCircles[i].Width + 8, _colorCircles[i].Height + 8);
                    }
                }
            }
        }

        private void DrawFormatOption(Graphics g, Rectangle r, string title, string desc, bool active, Color accent)
        {
            using (GraphicsPath p = GetRoundedPath(r, 10))
            using (SolidBrush cardBrush = new SolidBrush(_cardColor))
            {
                g.FillPath(cardBrush, p);
            }

            using (Pen border = new Pen(active ? accent : Color.FromArgb(45, 45, 55), active ? 2f : 1f))
            using (GraphicsPath p = GetRoundedPath(r, 10))
            {
                g.DrawPath(border, p);
            }

            using (Font fTitle = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (Font fDesc = new Font("Segoe UI", 8.5f))
            using (SolidBrush textBrush = new SolidBrush(_mutedText))
            {
                g.DrawString(title, fTitle, Brushes.White, r.X + 15, r.Y + 14);
                g.DrawString(desc, fDesc, textBrush, r.X + 15, r.Y + 42);
            }
        }

        private void DrawFinalPage(Graphics g, Color accent)
        {
            using (Font fTitle = new Font("Segoe UI", 18f, FontStyle.Bold))
            using (Font fDesc = new Font("Segoe UI", 11f))
            using (SolidBrush textBrush = new SolidBrush(_mutedText))
            {
                g.DrawString("Environment ready!", fTitle, Brushes.White, 50, 130);
                g.DrawString("Configuration stored. Core moving to system tray.", fDesc, textBrush, 50, 175);

                Rectangle summary = new Rectangle(50, 220, this.Width - 100, 110);
                using (GraphicsPath p = GetRoundedPath(summary, 10))
                using (SolidBrush cardBrush = new SolidBrush(_cardColor))
                {
                    g.FillPath(cardBrush, p);
                }

                using (Font fInf = new Font("Segoe UI Semibold", 10f))
                {
                    g.DrawString($"▶ System Boot Run: {(IsAutostartEnabled ? "ENABLED" : "DISABLED")}", fInf, Brushes.White, 75, 240);
                    g.DrawString($"▶ Primary Codec: {SelectedFormat}", fInf, Brushes.White, 75, 265);
                    g.DrawString($"▶ Target Theme: {SelectedColorHex}", fInf, Brushes.White, 75, 290);
                }
            }
        }

        private void DrawNavigation(Graphics g, Color accent)
        {
            if (_currentStep > 1)
            {
                Color bColor = _isBackHover ? Color.FromArgb(45, 45, 55) : Color.FromArgb(26, 26, 36);
                using (GraphicsPath p = GetRoundedPath(_btnBack, 8))
                using (SolidBrush btnBrush = new SolidBrush(bColor))
                {
                    g.FillPath(btnBrush, p);
                }
                using (Font f = new Font("Segoe UI", 10f, FontStyle.Bold))
                {
                    g.DrawString("BACK", f, Brushes.White, _btnBack.X + 34, _btnBack.Y + 10);
                }
            }

            Color nColor = _isNextHover ? Color.FromArgb(Math.Min(255, accent.R + 20), Math.Min(255, accent.G + 20), Math.Min(255, accent.B + 20)) : accent;
            using (GraphicsPath p = GetRoundedPath(_btnNext, 8))
            using (SolidBrush nextBrush = new SolidBrush(nColor))
            {
                g.FillPath(nextBrush, p);
            }

            string txt = _currentStep == TotalSteps ? "FINISH" : "NEXT";
            using (Font f = new Font("Segoe UI", 10f, FontStyle.Bold))
            {
                g.DrawString(txt, f, Brushes.Black, _btnNext.X + (_dynamicX(txt)), _btnNext.Y + 10);
            }
        }

        private int _dynamicX(string t) => t == "FINISH" ? 30 : 36;

        private GraphicsPath GetRoundedPath(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseAllFigures();
            return p;
        }
    }
}