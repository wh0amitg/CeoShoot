using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CeoShootMain
{
    public class SetupForm : Form
    {
        public bool IsAutostartEnabled { get; private set; }
        public string SelectedFormat { get; private set; }
        public string SelectedColorHex { get; private set; }

        private readonly Color _bgColor = Color.FromArgb(15, 15, 18);
        private readonly Color _panelColor = Color.FromArgb(24, 24, 28);
        private readonly Color _mutedText = Color.FromArgb(150, 155, 165);

        private readonly string[] _palette = { "#7289DA", "#F04747", "#43B581", "#FAA61A", "#00F0FF" };

        private Rectangle _btnSave, _btnClose;
        private Rectangle _tgAutoFull;
        private Rectangle _btnPng, _btnJpg;
        private Rectangle[] _colorRects = new Rectangle[5];

        private bool _isSaveHover, _isCloseHover, _isDragging;
        private Point _dragStart;

        public SetupForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(420, 380);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true;

            IsAutostartEnabled = Program.Autostart;
            SelectedFormat = Program.SaveFormat;
            SelectedColorHex = Program.AccentColor;

            UpdateGeometry();
        }

        private void UpdateGeometry()
        {
            _btnClose = new Rectangle(Width - 35, 12, 20, 20);
            _tgAutoFull = new Rectangle(widthOffset(85), 105, 50, 26);
            _btnPng = new Rectangle(35, 195, 165, 38);
            _btnJpg = new Rectangle(220, 195, 165, 38);
            _btnSave = new Rectangle(35, 310, Width - 70, 42);

            for (int i = 0; i < 5; i++)
                _colorRects[i] = new Rectangle(35 + (i * 48), 262, 32, 32);
        }

        private int widthOffset(int rightFromEdge) => Width - rightFromEdge;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            if (_btnClose.Contains(e.Location))
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            if (_tgAutoFull.Contains(e.Location))
            {
                IsAutostartEnabled = !IsAutostartEnabled;
                Invalidate();
                return;
            }

            if (_btnPng.Contains(e.Location))
            {
                SelectedFormat = "PNG";
                Invalidate();
                return;
            }
            if (_btnJpg.Contains(e.Location))
            {
                SelectedFormat = "JPG";
                Invalidate();
                return;
            }

            for (int i = 0; i < 5; i++)
            {
                if (_colorRects[i].Contains(e.Location))
                {
                    SelectedColorHex = _palette[i];
                    Invalidate();
                    return;
                }
            }

            if (_btnSave.Contains(e.Location))
            {
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            if (e.Y < 50)
            {
                _isDragging = true;
                _dragStart = e.Location;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point p = PointToScreen(e.Location);
                this.Location = new Point(p.X - _dragStart.X, p.Y - _dragStart.Y);
                return;
            }

            _isSaveHover = _btnSave.Contains(e.Location);
            _isCloseHover = _btnClose.Contains(e.Location);

            bool isHand = _isSaveHover || _isCloseHover || _tgAutoFull.Contains(e.Location) ||
                          _btnPng.Contains(e.Location) || _btnJpg.Contains(e.Location);

            if (!isHand)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (_colorRects[i].Contains(e.Location))
                    {
                        isHand = true;
                        break;
                    }
                }
            }

            Cursor = isHand ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e) => _isDragging = false;

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Color accent = ColorTranslator.FromHtml(SelectedColorHex);

            using (SolidBrush bg = new SolidBrush(_bgColor))
            {
                g.FillRectangle(bg, 0, 0, Width, Height);
            }
            using (Pen bp = new Pen(Color.FromArgb(40, accent), 1.5f))
            {
                g.DrawRectangle(bp, 1, 1, Width - 2, Height - 2);
            }

            using (Font f = new Font("Segoe UI", 12f, FontStyle.Bold))
            {
                g.DrawString("SYSTEM CONFIGURATION", f, Brushes.White, 32, 22);
            }

            using (Font f = new Font("Segoe UI", 10f, FontStyle.Bold))
            using (SolidBrush closeBrush = new SolidBrush(_isCloseHover ? Color.White : _mutedText))
            {
                g.DrawString("✕", f, closeBrush, _btnClose.X, _btnClose.Y);
            }

            using (Font f = new Font("Segoe UI", 10f, FontStyle.Bold))
            {
                g.DrawString("Launch application on system startup", f, Brushes.White, 35, 108);
            }

            using (GraphicsPath path = GetRoundedPath(_tgAutoFull, 13))
            using (SolidBrush toggleBg = new SolidBrush(IsAutostartEnabled ? accent : Color.FromArgb(50, 50, 60)))
            {
                g.FillPath(toggleBg, path);
            }
            g.FillEllipse(Brushes.White, IsAutostartEnabled ? _tgAutoFull.X + 27 : _tgAutoFull.X + 3, _tgAutoFull.Y + 3, 20, 20);

            using (Font f = new Font("Segoe UI", 10f, FontStyle.Bold))
            {
                g.DrawString("File encoder format standard", f, Brushes.White, 35, 165);
            }

            DrawButton(g, _btnPng, "PNG Encoding", SelectedFormat == "PNG", accent);
            DrawButton(g, _btnJpg, "JPG Encoding", SelectedFormat == "JPG", accent);

            using (Font f = new Font("Segoe UI", 10f, FontStyle.Bold))
            {
                g.DrawString("UI theme branding palette", f, Brushes.White, 35, 240);
            }

            for (int i = 0; i < 5; i++)
            {
                using (SolidBrush sb = new SolidBrush(ColorTranslator.FromHtml(_palette[i])))
                {
                    g.FillEllipse(sb, _colorRects[i]);
                }
                if (SelectedColorHex == _palette[i])
                {
                    using (Pen p = new Pen(Color.White, 2f))
                    {
                        g.DrawEllipse(p, _colorRects[i].X - 3, _colorRects[i].Y - 3, _colorRects[i].Width + 6, _colorRects[i].Height + 6);
                    }
                }
            }

            Color saveColor = _isSaveHover ? Color.FromArgb(Math.Min(255, accent.R + 25), Math.Min(255, accent.G + 25), Math.Min(255, accent.B + 25)) : accent;
            using (GraphicsPath p = GetRoundedPath(_btnSave, 8))
            using (SolidBrush sb = new SolidBrush(saveColor))
            {
                g.FillPath(sb, p);
            }

            using (Font f = new Font("Segoe UI", 10f, FontStyle.Bold))
            {
                Size sz = g.MeasureString("SAVE CHANGES", f).ToSize();
                g.DrawString("SAVE CHANGES", f, Brushes.Black, _btnSave.X + (_btnSave.Width - sz.Width) / 2, _btnSave.Y + (_btnSave.Height - sz.Height) / 2);
            }
        }

        private void DrawButton(Graphics g, Rectangle r, string text, bool active, Color accent)
        {
            using (GraphicsPath p = GetRoundedPath(r, 6))
            using (SolidBrush b = new SolidBrush(active ? accent : _panelColor))
            using (Pen pen = new Pen(accent, active ? 0 : 1))
            {
                g.FillPath(b, p);
                if (!active) g.DrawPath(pen, p);
                using (Font f = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                {
                    Size sz = g.MeasureString(text, f).ToSize();
                    g.DrawString(text, f, active ? Brushes.Black : Brushes.White, r.X + (r.Width - sz.Width) / 2, r.Y + (r.Height - sz.Height) / 2);
                }
            }
        }

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