using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace CeoShootMain
{
    public class SelectionForm : Form
    {
        private enum ToolMode { None, Draw, Text, Blur, Pixelate }
        private enum GuiItem { None, ToolDraw, ToolText, ToolBlur, ToolPixelate, ActionColor, ActionCopy, ActionSave }
        private enum TextInteractMode { None, Moving, Resizing }

        private abstract class CanvasElement { public abstract void Draw(Graphics g, Bitmap src, Rectangle offset); }

        private class DrawLine : CanvasElement
        {
            public List<Point> Points = new List<Point>();
            public Color Color;
            public float Thickness;
            public override void Draw(Graphics g, Bitmap src, Rectangle offset)
            {
                if (Points.Count < 2) return;
                using (Pen p = new Pen(Color, Thickness) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                {
                    Point[] pts = new Point[Points.Count];
                    for (int i = 0; i < Points.Count; i++) pts[i] = new Point(Points[i].X - offset.X, Points[i].Y - offset.Y);
                    g.DrawLines(p, pts);
                }
            }
        }

        private class BlurElement : CanvasElement
        {
            public Rectangle Rect;
            public int Strength;
            public override void Draw(Graphics g, Bitmap src, Rectangle offset)
            {
                Rectangle target = new Rectangle(Rect.X - offset.X, Rect.Y - offset.Y, Rect.Width, Rect.Height);
                if (target.Width <= 0 || target.Height <= 0) return;

                int w = Math.Max(1, Rect.Width / Strength);
                int h = Math.Max(1, Rect.Height / Strength);

                using (Bitmap temp = src.Clone(Rect, src.PixelFormat))
                using (Bitmap small = new Bitmap(temp, new Size(w, h)))
                {
                    var oldMode = g.InterpolationMode;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(small, target);
                    g.InterpolationMode = oldMode;
                }
            }
        }

        private class PixelateElement : CanvasElement
        {
            public Rectangle Rect;
            public int Strength;
            public override void Draw(Graphics g, Bitmap src, Rectangle offset)
            {
                Rectangle target = new Rectangle(Rect.X - offset.X, Rect.Y - offset.Y, Rect.Width, Rect.Height);
                if (target.Width <= 0 || target.Height <= 0) return;

                int w = Math.Max(1, Rect.Width / Strength);
                int h = Math.Max(1, Rect.Height / Strength);

                using (Bitmap temp = src.Clone(Rect, src.PixelFormat))
                using (Bitmap small = new Bitmap(w, h))
                {
                    using (Graphics gSmall = Graphics.FromImage(small))
                    {
                        gSmall.InterpolationMode = InterpolationMode.NearestNeighbor;
                        gSmall.DrawImage(temp, 0, 0, w, h);
                    }
                    var oldMode = g.InterpolationMode;
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.DrawImage(small, target);
                    g.InterpolationMode = oldMode;
                }
            }
        }

        private class TextElement : CanvasElement
        {
            public Point Location;
            public string Text = "";
            public float FontSize = 16f;
            public Color Color;
            public bool IsActive;

            public Rectangle GetBounds()
            {
                using (Font font = new Font("Segoe UI", FontSize, FontStyle.Bold))
                using (Bitmap fakeBmp = new Bitmap(1, 1))
                using (Graphics g = Graphics.FromImage(fakeBmp))
                {
                    string measure = string.IsNullOrEmpty(Text) ? "A" : Text;
                    SizeF size = g.MeasureString(measure, font);
                    return new Rectangle(Location.X, Location.Y, (int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height));
                }
            }

            public Rectangle GetResizeHandleRect()
            {
                Rectangle b = GetBounds();
                return new Rectangle(b.Right - 4, b.Bottom - 4, 10, 10);
            }

            public override void Draw(Graphics g, Bitmap src, Rectangle offset)
            {
                using (Font font = new Font("Segoe UI", FontSize, FontStyle.Bold))
                using (SolidBrush b = new SolidBrush(Color))
                {
                    Point drawLoc = new Point(Location.X - offset.X, Location.Y - offset.Y);
                    g.DrawString(Text + (IsActive ? "|" : ""), font, b, drawLoc);
                }
            }
        }

        private readonly Bitmap _screenShot;
        private Point _startPos, _currentPos;
        private bool _isSelecting, _hasSelected;
        private Rectangle _finalRect;

        private ToolMode _currentTool = ToolMode.None;
        private readonly List<CanvasElement> _elements = new List<CanvasElement>();

        private DrawLine _currentLine;
        private bool _isDraggingRect;
        private Point _rectStart;
        private Rectangle _currentDragRect;

        private Color _currentSelectedColor = Color.FromArgb(255, 30, 30);
        private Color _appAccentColor;
        private float _currentThickness = 4f;
        private int _currentEffectStrength = 15;

        private TextElement _activeTextElement;
        private TextInteractMode _textInteract = TextInteractMode.None;
        private Point _textDragOffset;
        private float _startResizeFontSize;
        private Point _startResizePoint;

        private Rectangle _guiPanelBounds;
        private readonly Dictionary<GuiItem, Rectangle> _guiButtons = new Dictionary<GuiItem, Rectangle>();
        private GuiItem _hoveredItem = GuiItem.None;
        private const int ButtonSize = 36;
        private const int ButtonPadding = 8;

        private Timer _overlayTimer;
        private bool _showOverlay = false;
        private string _overlayText = "";

        public SelectionForm(Bitmap background)
        {
            _screenShot = background;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ControlBox = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = SystemInformation.VirtualScreen;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.KeyPreview = true;

            try { _appAccentColor = ColorTranslator.FromHtml(Program.AccentColor); }
            catch { _appAccentColor = Color.FromArgb(88, 101, 242); }

            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();

            _overlayTimer = new Timer { Interval = 800 };
            _overlayTimer.Tick += (s, e) => { _showOverlay = false; _overlayTimer.Stop(); this.Invalidate(); };
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!_hasSelected) return;

            if (_currentTool == ToolMode.Draw)
            {
                if (e.Delta > 0) _currentThickness = Math.Min(40f, _currentThickness + 2f);
                else if (e.Delta < 0) _currentThickness = Math.Max(2f, _currentThickness - 2f);
                ShowOverlay($"Size: {_currentThickness}px");
            }
            else if (_currentTool == ToolMode.Blur || _currentTool == ToolMode.Pixelate)
            {
                if (e.Delta > 0) _currentEffectStrength = Math.Min(50, _currentEffectStrength + 2);
                else if (e.Delta < 0) _currentEffectStrength = Math.Max(2, _currentEffectStrength - 2);
                ShowOverlay($"Strength: {_currentEffectStrength}");
            }
        }

        private void ShowOverlay(string text)
        {
            _overlayText = text;
            _showOverlay = true;
            _overlayTimer.Stop();
            _overlayTimer.Start();
            this.Invalidate();
        }

        private void UpdateGuiPanelGeometry()
        {
            const int btnCount = 7;
            int panelWidth = (btnCount * ButtonSize) + ((btnCount + 1) * ButtonPadding);
            int panelHeight = ButtonSize + (ButtonPadding * 2);

            int x = _finalRect.Right - panelWidth;
            int y = _finalRect.Bottom + 8;

            if (y + panelHeight > this.Height) y = _finalRect.Bottom - panelHeight - 8;
            if (x < 0) x = _finalRect.Left;

            x = Math.Max(0, Math.Min(x, this.Width - panelWidth));
            y = Math.Max(0, Math.Min(y, this.Height - panelHeight));

            _guiPanelBounds = new Rectangle(x, y, panelWidth, panelHeight);
            _guiButtons.Clear();

            GuiItem[] items = { GuiItem.ToolDraw, GuiItem.ToolText, GuiItem.ToolBlur, GuiItem.ToolPixelate, GuiItem.ActionColor, GuiItem.ActionCopy, GuiItem.ActionSave };
            int currentX = x + ButtonPadding;
            foreach (var item in items)
            {
                _guiButtons[item] = new Rectangle(currentX, y + ButtonPadding, ButtonSize, ButtonSize);
                currentX += ButtonSize + ButtonPadding;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (_hasSelected && _guiPanelBounds.Contains(e.Location))
            {
                HandleGuiClick(e.Location);
                this.Invalidate();
                return;
            }

            if (_hasSelected && _finalRect.Contains(e.Location))
            {
                if (_currentTool == ToolMode.Text)
                {
                    if (_activeTextElement != null && _activeTextElement.GetResizeHandleRect().Contains(e.Location))
                    {
                        _textInteract = TextInteractMode.Resizing;
                        _startResizePoint = e.Location;
                        _startResizeFontSize = _activeTextElement.FontSize;
                        return;
                    }

                    foreach (var el in _elements)
                    {
                        if (el is TextElement txt && txt.GetBounds().Contains(e.Location))
                        {
                            if (_activeTextElement != null) _activeTextElement.IsActive = false;
                            _activeTextElement = txt;
                            _activeTextElement.IsActive = true;
                            _textInteract = TextInteractMode.Moving;
                            _textDragOffset = new Point(e.X - txt.Location.X, e.Y - txt.Location.Y);
                            this.Invalidate();
                            return;
                        }
                    }

                    if (_activeTextElement != null && string.IsNullOrWhiteSpace(_activeTextElement.Text))
                        _elements.Remove(_activeTextElement);
                    if (_activeTextElement != null) _activeTextElement.IsActive = false;

                    _activeTextElement = new TextElement { Location = e.Location, Color = _currentSelectedColor, IsActive = true };
                    _elements.Add(_activeTextElement);
                    _textInteract = TextInteractMode.None;
                    this.Invalidate();
                    return;
                }

                if (_currentTool == ToolMode.Blur || _currentTool == ToolMode.Pixelate)
                {
                    _isDraggingRect = true;
                    _rectStart = e.Location;
                    _currentDragRect = new Rectangle(e.X, e.Y, 0, 0);
                    return;
                }

                if (_currentTool == ToolMode.Draw && e.Button == MouseButtons.Left)
                {
                    _currentLine = new DrawLine { Color = _currentSelectedColor, Thickness = _currentThickness };
                    _currentLine.Points.Add(e.Location);
                    _elements.Add(_currentLine);
                    return;
                }
            }

            if (e.Button == MouseButtons.Left && !_hasSelected)
            {
                _isSelecting = true;
                _startPos = e.Location;
                _currentPos = e.Location;
            }
        }

        private void HandleGuiClick(Point clickPt)
        {
            foreach (var btn in _guiButtons)
            {
                if (btn.Value.Contains(clickPt))
                {
                    if (btn.Key != GuiItem.ActionColor && btn.Key != GuiItem.ToolText)
                    {
                        if (_activeTextElement != null)
                        {
                            _activeTextElement.IsActive = false;
                            if (string.IsNullOrWhiteSpace(_activeTextElement.Text)) _elements.Remove(_activeTextElement);
                            _activeTextElement = null;
                        }
                    }

                    switch (btn.Key)
                    {
                        case GuiItem.ToolDraw: _currentTool = _currentTool == ToolMode.Draw ? ToolMode.None : ToolMode.Draw; break;
                        case GuiItem.ToolText: _currentTool = _currentTool == ToolMode.Text ? ToolMode.None : ToolMode.Text; break;
                        case GuiItem.ToolBlur: _currentTool = _currentTool == ToolMode.Blur ? ToolMode.None : ToolMode.Blur; break;
                        case GuiItem.ToolPixelate: _currentTool = _currentTool == ToolMode.Pixelate ? ToolMode.None : ToolMode.Pixelate; break;
                        case GuiItem.ActionColor:
                            using (ColorDialog cd = new ColorDialog { Color = _currentSelectedColor })
                            {
                                if (cd.ShowDialog() == DialogResult.OK)
                                {
                                    _currentSelectedColor = cd.Color;
                                    if (_activeTextElement != null) _activeTextElement.Color = cd.Color;
                                }
                            }
                            break;
                        case GuiItem.ActionCopy: ExecuteCopy(); break;
                        case GuiItem.ActionSave: ExecuteQuickSave(); break;
                    }
                    break;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_hasSelected && _guiPanelBounds.Contains(e.Location))
            {
                GuiItem oldHover = _hoveredItem;
                _hoveredItem = GuiItem.None;
                foreach (var btn in _guiButtons) if (btn.Value.Contains(e.Location)) { _hoveredItem = btn.Key; break; }
                this.Cursor = Cursors.Hand;
                if (oldHover != _hoveredItem) this.Invalidate();
                return;
            }

            if (_hoveredItem != GuiItem.None)
            {
                _hoveredItem = GuiItem.None;
                this.Invalidate();
            }

            if (_hasSelected && _currentTool == ToolMode.Text)
            {
                if (_textInteract == TextInteractMode.Moving && _activeTextElement != null)
                {
                    _activeTextElement.Location = new Point(e.X - _textDragOffset.X, e.Y - _textDragOffset.Y);
                    this.Invalidate();
                    return;
                }
                if (_textInteract == TextInteractMode.Resizing && _activeTextElement != null)
                {
                    int deltaX = e.X - _startResizePoint.X;
                    _activeTextElement.FontSize = Math.Max(8, Math.Min(_startResizeFontSize + (deltaX * 0.3f), 120));
                    this.Invalidate();
                    return;
                }

                if (_activeTextElement != null && _activeTextElement.GetResizeHandleRect().Contains(e.Location))
                {
                    this.Cursor = Cursors.SizeNWSE;
                    return;
                }
                foreach (var el in _elements)
                {
                    if (el is TextElement txt && txt.GetBounds().Contains(e.Location))
                    {
                        this.Cursor = Cursors.Hand;
                        return;
                    }
                }
                if (_finalRect.Contains(e.Location))
                {
                    this.Cursor = Cursors.IBeam;
                    return;
                }
            }

            if (_isSelecting)
            {
                _currentPos = e.Location;
                this.Invalidate();
            }
            else if (_isDraggingRect)
            {
                int x = Math.Min(_rectStart.X, e.X);
                int y = Math.Min(_rectStart.Y, e.Y);
                _currentDragRect = new Rectangle(x, y, Math.Abs(_rectStart.X - e.X), Math.Abs(_rectStart.Y - e.Y));
                this.Invalidate();
            }
            else if (_currentLine != null)
            {
                if (_finalRect.Contains(e.Location))
                {
                    _currentLine.Points.Add(e.Location);
                    this.Invalidate();
                }
            }
            else
            {
                if (_hasSelected && _finalRect.Contains(e.Location) && _currentTool != ToolMode.None)
                    this.Cursor = Cursors.Cross;
                else
                    this.Cursor = Cursors.Default;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_isSelecting && e.Button == MouseButtons.Left)
            {
                _isSelecting = false;
                _finalRect = GetSelectionRectangle();

                _finalRect = Rectangle.Intersect(_finalRect, new Rectangle(0, 0, _screenShot.Width, _screenShot.Height));

                if (_finalRect.Width > 20 && _finalRect.Height > 20)
                {
                    _hasSelected = true;
                    UpdateGuiPanelGeometry();
                }
                else this.Close();
                this.Invalidate();
            }

            if (_isDraggingRect)
            {
                _isDraggingRect = false;
                if (_currentDragRect.Width > 4 && _currentDragRect.Height > 4)
                {
                    Rectangle intersect = Rectangle.Intersect(_currentDragRect, _finalRect);
                    if (intersect.Width > 0 && intersect.Height > 0)
                    {
                        if (_currentTool == ToolMode.Blur)
                            _elements.Add(new BlurElement { Rect = intersect, Strength = _currentEffectStrength });
                        else if (_currentTool == ToolMode.Pixelate)
                            _elements.Add(new PixelateElement { Rect = intersect, Strength = _currentEffectStrength });
                    }
                }
                this.Invalidate();
            }

            _currentLine = null;
            _textInteract = TextInteractMode.None;
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (_activeTextElement != null)
            {
                if (e.KeyChar == (char)Keys.Back)
                {
                    if (_activeTextElement.Text.Length > 0)
                        _activeTextElement.Text = _activeTextElement.Text.Substring(0, _activeTextElement.Text.Length - 1);
                }
                else if (e.KeyChar == (char)Keys.Enter || e.KeyChar == (char)Keys.Escape)
                {
                    _activeTextElement.IsActive = false;
                    if (string.IsNullOrWhiteSpace(_activeTextElement.Text)) _elements.Remove(_activeTextElement);
                    _activeTextElement = null;
                }
                else if (!char.IsControl(e.KeyChar))
                {
                    _activeTextElement.Text += e.KeyChar;
                }
                this.Invalidate();
                e.Handled = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_activeTextElement != null)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    _activeTextElement.IsActive = false;
                    if (string.IsNullOrWhiteSpace(_activeTextElement.Text)) _elements.Remove(_activeTextElement);
                    _activeTextElement = null;
                    this.Invalidate();
                    e.Handled = true;
                }
                return;
            }

            if (_hasSelected)
            {
                if (e.Control && e.KeyCode == Keys.Z)
                {
                    if (_elements.Count > 0)
                    {
                        var last = _elements[_elements.Count - 1];
                        if (last == _activeTextElement) _activeTextElement = null;
                        _elements.RemoveAt(_elements.Count - 1);
                        this.Invalidate();
                    }
                    e.Handled = true;
                    return;
                }
                if (e.Control && e.KeyCode == Keys.C) { ExecuteCopy(); e.Handled = true; return; }
                if (e.Control && e.KeyCode == Keys.S) { ExecuteQuickSave(); e.Handled = true; return; }
            }

            if (e.KeyCode == Keys.Escape) this.Close();
        }

        private void ExecuteCopy()
        {
            if (_activeTextElement != null) _activeTextElement.IsActive = false;
            try
            {
                using (Bitmap c = GetRenderedScreenshot())
                {
                    if (c != null)
                    {
                        Clipboard.SetImage(c);
                    }
                }
            }
            catch { }
            this.Close();
        }

        private void ExecuteQuickSave()
        {
            if (_activeTextElement != null) _activeTextElement.IsActive = false;
            try
            {
                if (!Directory.Exists(Program.ConfigFolder)) Directory.CreateDirectory(Program.ConfigFolder);
                string path = Path.Combine(Program.ConfigFolder, $"CEOSHOOT_{DateTime.Now:yyyyMMdd_HHmmss}.{Program.SaveFormat.ToLower()}");
                using (Bitmap c = GetRenderedScreenshot())
                {
                    if (c != null)
                    {
                        c.Save(path, Program.SaveFormat == "PNG" ? ImageFormat.Png : ImageFormat.Jpeg);
                    }
                }
            }
            catch { }
            this.Close();
        }

        private Bitmap GetRenderedScreenshot()
        {
            Rectangle safeRect = Rectangle.Intersect(_finalRect, new Rectangle(0, 0, _screenShot.Width, _screenShot.Height));
            if (safeRect.Width <= 0 || safeRect.Height <= 0) return null;

            Bitmap res = _screenShot.Clone(safeRect, _screenShot.PixelFormat);
            using (Graphics g = Graphics.FromImage(res))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                foreach (var el in _elements) el.Draw(g, _screenShot, safeRect);
            }
            return res;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.DrawImage(_screenShot, 0, 0);
            using (SolidBrush dimBrush = new SolidBrush(Color.FromArgb(140, Color.Black)))
                g.FillRectangle(dimBrush, this.ClientRectangle);

            if (_isSelecting || _hasSelected)
            {
                Rectangle rect = _isSelecting ? GetSelectionRectangle() : _finalRect;
                g.DrawImage(_screenShot, rect, rect, GraphicsUnit.Pixel);

                g.TranslateTransform(rect.X, rect.Y);
                foreach (var el in _elements) el.Draw(g, _screenShot, rect);
                g.ResetTransform();

                if (_isDraggingRect)
                {
                    Rectangle drawR = Rectangle.Intersect(_currentDragRect, rect);
                    if (drawR.Width > 0 && drawR.Height > 0)
                    {
                        if (_currentTool == ToolMode.Blur) new BlurElement { Rect = drawR, Strength = _currentEffectStrength }.Draw(g, _screenShot, new Rectangle(0, 0, 0, 0));
                        if (_currentTool == ToolMode.Pixelate) new PixelateElement { Rect = drawR, Strength = _currentEffectStrength }.Draw(g, _screenShot, new Rectangle(0, 0, 0, 0));
                        using (Pen p = new Pen(_appAccentColor, 1f) { DashStyle = DashStyle.Dash }) g.DrawRectangle(p, drawR);
                    }
                }

                if (_activeTextElement != null)
                {
                    Rectangle b = _activeTextElement.GetBounds();
                    using (Pen p = new Pen(_appAccentColor, 1f) { DashStyle = DashStyle.Dash }) g.DrawRectangle(p, b);
                    Rectangle h = _activeTextElement.GetResizeHandleRect();
                    g.FillRectangle(Brushes.White, h);
                    using (Pen p = new Pen(_appAccentColor, 1f)) g.DrawRectangle(p, h);
                }

                using (Pen p = new Pen(_appAccentColor, 1.5f)) g.DrawRectangle(p, rect);

                int mSize = 5;
                using (SolidBrush w = new SolidBrush(Color.White))
                {
                    g.FillRectangle(w, rect.Left - mSize / 2, rect.Top - mSize / 2, mSize, mSize);
                    g.FillRectangle(w, rect.Right - mSize / 2, rect.Top - mSize / 2, mSize, mSize);
                    g.FillRectangle(w, rect.Left - mSize / 2, rect.Bottom - mSize / 2, mSize, mSize);
                    g.FillRectangle(w, rect.Right - mSize / 2, rect.Bottom - mSize / 2, mSize, mSize);
                }

                using (Font f = new Font("Segoe UI", 9f, FontStyle.Bold))
                {
                    string resText = $"{rect.Width} × {rect.Height}";
                    Size sz = g.MeasureString(resText, f).ToSize();
                    int ty = (rect.Top - sz.Height - 5 < 0) ? rect.Top + 5 : rect.Top - sz.Height - 5;
                    using (SolidBrush bg = new SolidBrush(Color.FromArgb(220, 20, 20, 20))) g.FillRectangle(bg, rect.Left, ty, sz.Width + 6, sz.Height + 2);
                    g.DrawString(resText, f, Brushes.White, rect.Left + 3, ty + 1);

                    if (_showOverlay)
                    {
                        Size oSz = g.MeasureString(_overlayText, f).ToSize();
                        int ox = rect.Left + sz.Width + 15;
                        using (SolidBrush bg = new SolidBrush(Color.FromArgb(220, _appAccentColor.R, _appAccentColor.G, _appAccentColor.B)))
                            g.FillRectangle(bg, ox, ty, oSz.Width + 6, oSz.Height + 2);
                        g.DrawString(_overlayText, f, Brushes.White, ox + 3, ty + 1);
                    }
                }
            }

            if (_hasSelected)
            {
                using (GraphicsPath p = GetRoundedRectPath(_guiPanelBounds, 12))
                using (SolidBrush b = new SolidBrush(Color.FromArgb(240, 30, 30, 35))) g.FillPath(b, p);

                RenderGuiButton(g, GuiItem.ToolDraw, "✏️", _currentTool == ToolMode.Draw);
                RenderGuiButton(g, GuiItem.ToolText, "Ｔ", _currentTool == ToolMode.Text);
                RenderGuiButton(g, GuiItem.ToolBlur, "💧", _currentTool == ToolMode.Blur);
                RenderGuiButton(g, GuiItem.ToolPixelate, "▦", _currentTool == ToolMode.Pixelate);
                RenderGuiButton(g, GuiItem.ActionColor, "🎨", false);
                RenderGuiButton(g, GuiItem.ActionCopy, "📋", false);
                RenderGuiButton(g, GuiItem.ActionSave, "💾", false);
            }
        }

        private void RenderGuiButton(Graphics g, GuiItem item, string icon, bool isActive)
        {
            Rectangle r = _guiButtons[item];
            Color bg = isActive ? _appAccentColor : (_hoveredItem == item ? Color.FromArgb(60, 255, 255, 255) : Color.Transparent);
            if (bg != Color.Transparent) using (SolidBrush b = new SolidBrush(bg)) g.FillEllipse(b, r);

            using (Font f = new Font("Segoe UI Emoji", 11f))
            {
                Size sz = g.MeasureString(icon, f).ToSize();
                g.DrawString(icon, f, Brushes.White, r.X + (r.Width - sz.Width) / 2 + 1, r.Y + (r.Height - sz.Height) / 2);
            }
        }

        private GraphicsPath GetRoundedRectPath(Rectangle rect, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = rad * 2;
            p.AddArc(rect.X, rect.Y, d, d, 180, 90);
            p.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            p.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            p.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            p.CloseAllFigures();
            return p;
        }

        private Rectangle GetSelectionRectangle()
        {
            int x = Math.Min(_startPos.X, _currentPos.X);
            int y = Math.Min(_startPos.Y, _currentPos.Y);
            return new Rectangle(x, y, Math.Abs(_startPos.X - _currentPos.X), Math.Abs(_startPos.Y - _currentPos.Y));
        }

        protected override void OnClosed(EventArgs e)
        {
            _overlayTimer?.Dispose();
            _screenShot?.Dispose();
            base.OnClosed(e);
        }
    }
}