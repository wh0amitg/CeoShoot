using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace CeoShootMain
{
    public class SelectionForm : Form
    {
        private enum ToolMode { None, Draw, Text }
        private enum GuiItem { None, ToolDraw, ToolText, ActionColor, ActionCopy, ActionSave }
        private enum TextInteractMode { None, Moving, Resizing }

        private readonly Bitmap _screenShot;
        private Point _startPos, _currentPos;
        private bool _isSelecting;
        private bool _hasSelected;
        private Rectangle _finalRect;

        private ToolMode _currentTool = ToolMode.None;
        private readonly List<List<Point>> _drawingLines = new List<List<Point>>();
        private readonly List<Color> _lineColors = new List<Color>();
        private Color _currentSelectedColor = Color.FromArgb(255, 30, 30);
        private List<Point> _currentLine;
        private bool _isDrawing;

        private class TextElement
        {
            public Point Location;
            public string Text = "";
            public float FontSize = 16f;
            public Color ElementColor = Color.FromArgb(255, 30, 30);

            public Rectangle GetBounds()
            {
                using (Font font = new Font("Segoe UI", FontSize, FontStyle.Bold))
                {
                    string measureText = string.IsNullOrEmpty(Text) ? "A" : Text;
                    Size size = TextRenderer.MeasureText(measureText, font);
                    return new Rectangle(Location.X, Location.Y, size.Width, size.Height);
                }
            }

            public Rectangle GetResizeHandleRect()
            {
                Rectangle bounds = GetBounds();
                return new Rectangle(bounds.Right - 3, bounds.Bottom - 3, 8, 8);
            }
        }

        private readonly List<TextElement> _textElements = new List<TextElement>();
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

        public SelectionForm(Bitmap background)
        {
            _screenShot = background;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = Screen.PrimaryScreen.Bounds;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.KeyPreview = true;
            this.Text = "CEOSHOOT";

            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
        }

        private void UpdateGuiPanelGeometry()
        {
            const int btnCount = 5;
            int panelWidth = (btnCount * ButtonSize) + ((btnCount + 1) * ButtonPadding);
            int panelHeight = ButtonSize + (ButtonPadding * 2);

            int x = _finalRect.Right - panelWidth;
            int y = _finalRect.Bottom + 8;

            if (y + panelHeight > this.Height) y = _finalRect.Bottom - panelHeight - 8;
            if (x < 0) x = _finalRect.Left;

            _guiPanelBounds = new Rectangle(x, y, panelWidth, panelHeight);
            _guiButtons.Clear();

            GuiItem[] items = { GuiItem.ToolDraw, GuiItem.ToolText, GuiItem.ActionColor, GuiItem.ActionCopy, GuiItem.ActionSave };
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

            if (_hasSelected && _finalRect.Contains(e.Location) && _currentTool == ToolMode.Text)
            {
                if (_activeTextElement != null && _activeTextElement.GetResizeHandleRect().Contains(e.Location))
                {
                    _textInteract = TextInteractMode.Resizing;
                    _startResizePoint = e.Location;
                    _startResizeFontSize = _activeTextElement.FontSize;
                    return;
                }

                foreach (var txt in _textElements)
                {
                    if (txt.GetBounds().Contains(e.Location))
                    {
                        _activeTextElement = txt;
                        _textInteract = TextInteractMode.Moving;
                        _textDragOffset = new Point(e.X - txt.Location.X, e.Y - txt.Location.Y);
                        this.Invalidate();
                        return;
                    }
                }

                if (_activeTextElement != null && string.IsNullOrWhiteSpace(_activeTextElement.Text))
                {
                    _textElements.Remove(_activeTextElement);
                }

                _activeTextElement = new TextElement { Location = e.Location, ElementColor = _currentSelectedColor };
                _textElements.Add(_activeTextElement);
                _textInteract = TextInteractMode.None;
                this.Invalidate();
                return;
            }

            if (_hasSelected && _finalRect.Contains(e.Location) && _currentTool == ToolMode.Draw && e.Button == MouseButtons.Left)
            {
                _isDrawing = true;
                _currentLine = new List<Point> { e.Location };
                _drawingLines.Add(_currentLine);
                _lineColors.Add(_currentSelectedColor);
                return;
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
                    if (btn.Key != GuiItem.ActionColor)
                    {
                        if (_activeTextElement != null && string.IsNullOrWhiteSpace(_activeTextElement.Text))
                            _textElements.Remove(_activeTextElement);
                        _activeTextElement = null;
                    }

                    switch (btn.Key)
                    {
                        case GuiItem.ToolDraw:
                            _currentTool = (_currentTool == ToolMode.Draw) ? ToolMode.None : ToolMode.Draw;
                            break;
                        case GuiItem.ToolText:
                            _currentTool = (_currentTool == ToolMode.Text) ? ToolMode.None : ToolMode.Text;
                            break;
                        case GuiItem.ActionColor:
                            using (ColorDialog cd = new ColorDialog())
                            {
                                cd.Color = _currentSelectedColor;
                                if (cd.ShowDialog() == DialogResult.OK)
                                {
                                    _currentSelectedColor = cd.Color;
                                    if (_activeTextElement != null)
                                    {
                                        _activeTextElement.ElementColor = cd.Color;
                                    }
                                }
                            }
                            break;
                        case GuiItem.ActionCopy:
                            ExecuteCopy();
                            break;
                        case GuiItem.ActionSave:
                            ExecuteSave();
                            break;
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
                foreach (var btn in _guiButtons)
                {
                    if (btn.Value.Contains(e.Location)) { _hoveredItem = btn.Key; break; }
                }
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
                    float newSize = _startResizeFontSize + (deltaX * 0.3f);
                    _activeTextElement.FontSize = Math.Max(8, Math.Min(newSize, 120));
                    this.Invalidate();
                    return;
                }

                if (_activeTextElement != null && _activeTextElement.GetResizeHandleRect().Contains(e.Location))
                {
                    this.Cursor = Cursors.SizeNWSE;
                    return;
                }
                foreach (var txt in _textElements)
                {
                    if (txt.GetBounds().Contains(e.Location))
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
            else if (_isDrawing && _currentLine != null)
            {
                if (_finalRect.Contains(e.Location))
                {
                    _currentLine.Add(e.Location);
                    this.Invalidate();
                }
            }
            else
            {
                this.Cursor = (_hasSelected && _finalRect.Contains(e.Location) && _currentTool == ToolMode.Draw) ? Cursors.Cross : Cursors.Default;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_isSelecting && e.Button == MouseButtons.Left)
            {
                _isSelecting = false;
                _finalRect = GetSelectionRectangle();
                if (_finalRect.Width > 20 && _finalRect.Height > 20)
                {
                    _hasSelected = true;
                    UpdateGuiPanelGeometry();
                }
                else this.Close();
                this.Invalidate();
            }

            _isDrawing = false;
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
                    if (string.IsNullOrWhiteSpace(_activeTextElement.Text)) _textElements.Remove(_activeTextElement);
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
                    if (string.IsNullOrWhiteSpace(_activeTextElement.Text)) _textElements.Remove(_activeTextElement);
                    _activeTextElement = null;
                    this.Invalidate();
                    e.Handled = true;
                }
                return;
            }

            if (_hasSelected)
            {
                if (e.Control && e.KeyCode == Keys.C)
                {
                    ExecuteCopy();
                    e.Handled = true;
                    return;
                }
                if (e.Control && e.KeyCode == Keys.S)
                {
                    ExecuteSave();
                    e.Handled = true;
                    return;
                }
            }

            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }

        private void ExecuteCopy()
        {
            _hasSelected = false;
            _activeTextElement = null;
            this.Refresh();
            try
            {
                using (Bitmap cropped = GetRenderedScreenshot()) Clipboard.SetImage(cropped);
                System.Media.SystemSounds.Asterisk.Play();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            this.Close();
        }

        private void ExecuteSave()
        {
            this.Hide();
            _activeTextElement = null;
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "PNG Image|*.png|JPEG Image|*.jpg";
                sfd.FileName = $"Screenshot_{DateTime.Now:yyyy-MM-dd_HHmmss}";
                sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (Bitmap cropped = GetRenderedScreenshot())
                        {
                            ImageFormat format = sfd.FileName.EndsWith(".jpg") ? ImageFormat.Jpeg : ImageFormat.Png;
                            cropped.Save(sfd.FileName, format);
                        }
                    }
                    catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
                }
            }
            this.Close();
        }

        private Bitmap GetRenderedScreenshot()
        {
            Bitmap result = _screenShot.Clone(_finalRect, _screenShot.PixelFormat);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.TranslateTransform(-_finalRect.X, -_finalRect.Y);
                RenderDrawingsAndText(g, false);
            }
            return result;
        }

        private void RenderDrawingsAndText(Graphics g, bool drawUiFrames)
        {
            for (int i = 0; i < _drawingLines.Count; i++)
            {
                var line = _drawingLines[i];
                if (line.Count > 1)
                {
                    Color c = (i < _lineColors.Count) ? _lineColors[i] : Color.FromArgb(255, 30, 30);
                    using (Pen drawPen = new Pen(c, 3f))
                    {
                        drawPen.StartCap = LineCap.Round; drawPen.EndCap = LineCap.Round; drawPen.LineJoin = LineJoin.Round;
                        g.DrawLines(drawPen, line.ToArray());
                    }
                }
            }

            foreach (var txt in _textElements)
            {
                Rectangle tBounds = txt.GetBounds();

                using (Font font = new Font("Segoe UI", txt.FontSize, FontStyle.Bold))
                using (SolidBrush textBrush = new SolidBrush(txt.ElementColor))
                {
                    string cursorStr = (txt == _activeTextElement && _currentTool == ToolMode.Text) ? "|" : "";
                    g.DrawString(txt.Text + cursorStr, font, textBrush, txt.Location);
                }

                if (drawUiFrames && txt == _activeTextElement && _currentTool == ToolMode.Text)
                {
                    using (Pen framePen = new Pen(Color.FromArgb(0, 162, 255), 1f))
                    {
                        framePen.DashStyle = DashStyle.Dash;
                        g.DrawRectangle(framePen, tBounds);
                    }
                    Rectangle handle = txt.GetResizeHandleRect();
                    g.FillRectangle(Brushes.White, handle);
                    using (Pen hPen = new Pen(Color.FromArgb(0, 162, 255), 1f)) g.DrawRectangle(hPen, handle);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            g.DrawImage(_screenShot, 0, 0);
            using (SolidBrush dimBrush = new SolidBrush(Color.FromArgb(140, Color.Black)))
            {
                g.FillRectangle(dimBrush, this.ClientRectangle);
            }

            if (_isSelecting || _hasSelected)
            {
                Rectangle rect = _isSelecting ? GetSelectionRectangle() : _finalRect;
                g.DrawImage(_screenShot, rect, rect, GraphicsUnit.Pixel);

                RenderDrawingsAndText(g, true);

                using (Pen neonPen = new Pen(Color.FromArgb(0, 162, 255), 1.5f)) g.DrawRectangle(neonPen, rect);

                const int mSize = 5;
                using (SolidBrush whiteBrush = new SolidBrush(Color.White))
                {
                    g.FillRectangle(whiteBrush, rect.Left - mSize / 2, rect.Top - mSize / 2, mSize, mSize);
                    g.FillRectangle(whiteBrush, rect.Right - mSize / 2, rect.Top - mSize / 2, mSize, mSize);
                    g.FillRectangle(whiteBrush, rect.Left - mSize / 2, rect.Bottom - mSize / 2, mSize, mSize);
                    g.FillRectangle(whiteBrush, rect.Right - mSize / 2, rect.Bottom - mSize / 2, mSize, mSize);
                }

                string resText = $"{rect.Width} × {rect.Height}";
                using (Font font = new Font("Segoe UI", 9f, FontStyle.Bold))
                {
                    Size textSize = TextRenderer.MeasureText(resText, font);
                    int textY = (rect.Top - textSize.Height - 5 < 0) ? rect.Top + 5 : rect.Top - textSize.Height - 5;
                    using (SolidBrush textBg = new SolidBrush(Color.FromArgb(220, 20, 20, 20)))
                    {
                        g.FillRectangle(textBg, rect.Left, textY, textSize.Width + 6, textSize.Height + 2);
                    }
                    g.DrawString(resText, font, Brushes.White, rect.Left + 3, textY + 1);
                }
            }

            if (_hasSelected)
            {
                using (GraphicsPath path = GetRoundedRectPath(_guiPanelBounds, 12))
                using (SolidBrush panelBrush = new SolidBrush(Color.FromArgb(240, 25, 25, 25)))
                {
                    g.FillPath(panelBrush, path);
                }

                RenderGuiButton(g, GuiItem.ToolDraw, "✏️", _currentTool == ToolMode.Draw);
                RenderGuiButton(g, GuiItem.ToolText, "Ｔ", _currentTool == ToolMode.Text);
                RenderGuiButton(g, GuiItem.ActionColor, "🎨", false);
                RenderGuiButton(g, GuiItem.ActionCopy, "📋", false);
                RenderGuiButton(g, GuiItem.ActionSave, "💾", false);
            }
        }

        private void RenderGuiButton(Graphics g, GuiItem item, string icon, bool isActive)
        {
            Rectangle r = _guiButtons[item];
            bool isHovered = (_hoveredItem == item);

            Color bg = Color.Transparent;
            if (isActive) bg = Color.FromArgb(0, 120, 215);
            else if (isHovered) bg = Color.FromArgb(60, 255, 255, 255);

            if (bg != Color.Transparent)
            {
                using (SolidBrush b = new SolidBrush(bg)) g.FillEllipse(b, r);
            }

            using (Font f = new Font("Segoe UI Emoji", 11f, FontStyle.Regular))
            {
                Size size = TextRenderer.MeasureText(icon, f);
                g.DrawString(icon, f, Brushes.White, r.X + (r.Width - size.Width) / 2 + 1, r.Y + (r.Height - size.Height) / 2);
            }
        }

        private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseAllFigures();
            return path;
        }

        private Rectangle GetSelectionRectangle()
        {
            int x = Math.Min(_startPos.X, _currentPos.X);
            int y = Math.Min(_startPos.Y, _currentPos.Y);
            return new Rectangle(x, y, Math.Abs(_startPos.X - _currentPos.X), Math.Abs(_startPos.Y - _currentPos.Y));
        }

        protected override void OnClosed(EventArgs e)
        {
            _screenShot?.Dispose();
            base.OnClosed(e);
        }
    }
}
