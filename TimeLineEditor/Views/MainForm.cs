using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using TimelineEditor.Controls;
using TimelineEditor.Models;
using TimelineEditor.Services;
using System.Drawing.Imaging;     // для ImageFormat


namespace TimelineEditor.Views
{
    public class MainForm : Form
    {
        private TimelineProject _project = new TimelineProject();
        private TimelineControl _timeline;
        private ListBox _eventsList;
        private TextBox _txtTitle;
        private TextBox _txtCategory;
        private TextBox _txtDescription;
        private DateTimePicker _dtStart;
        private DateTimePicker _dtEnd;
        private Button _btnColor;
        private Button _btnAdd;
        private Button _btnUpdate;
        private Button _btnDelete;
        private TextBox _txtSearch;
        private ComboBox _cmbFilterCategory;
        private StatusStrip _status;
        private ToolStripStatusLabel _statusLabel;

        private string _currentFilePath;

        public MainForm()
        {
            Text = "Редактор таймлайна событий";
            Width = 1200;
            Height = 700;
            StartPosition = FormStartPosition.CenterScreen;

            InitializeLayout();
            UpdateBindings();
        }

        private void InitializeLayout()
        {
            // Меню
            MenuStrip menu = new MenuStrip();
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("Файл");
            fileMenu.DropDownItems.Add("Новый", null, delegate { NewProject(); });
            fileMenu.DropDownItems.Add("Открыть...", null, delegate { OpenProject(); });
            fileMenu.DropDownItems.Add("Сохранить", null, delegate { SaveProject(); });
            fileMenu.DropDownItems.Add("Сохранить как...", null, delegate { SaveProjectAs(); });
            fileMenu.DropDownItems.Add("Экспорт PNG/JPEG...", null, delegate { ExportImage(); });
            fileMenu.DropDownItems.Add(new ToolStripSeparator());


            ToolStripMenuItem viewMenu = new ToolStripMenuItem("Вид");
            viewMenu.DropDownItems.Add("Годы", null, delegate { ChangeScale(TimeScale.Years); });
            viewMenu.DropDownItems.Add("Месяцы", null, delegate { ChangeScale(TimeScale.Months); });
            viewMenu.DropDownItems.Add("Недели", null, delegate { ChangeScale(TimeScale.Weeks); });
            viewMenu.DropDownItems.Add("Дни", null, delegate { ChangeScale(TimeScale.Days); });

            ToolStripMenuItem themeMenu = new ToolStripMenuItem("Тема");
            themeMenu.DropDownItems.Add("Светлая", null, delegate { SetTheme(false); });
            themeMenu.DropDownItems.Add("Тёмная", null, delegate { SetTheme(true); });

            menu.Items.Add(fileMenu);
            menu.Items.Add(viewMenu);
            menu.Items.Add(themeMenu);
            MainMenuStrip = menu;
            Controls.Add(menu);

            // Основной сплит: слева таймлайн + список, справа редактор
            SplitContainer splitMain = new SplitContainer();
            splitMain.Dock = DockStyle.Fill;
            splitMain.Orientation = Orientation.Vertical;
            splitMain.SplitterDistance = 800;
            Controls.Add(splitMain);
            splitMain.BringToFront();

            // Внутри левой части — вертикальный сплит
            SplitContainer splitLeft = new SplitContainer();
            splitLeft.Dock = DockStyle.Fill;
            splitLeft.Orientation = Orientation.Horizontal;
            splitLeft.SplitterDistance = 400;
            splitMain.Panel1.Controls.Add(splitLeft);

            // Таймлайн
            _timeline = new TimelineControl();
            _timeline.Dock = DockStyle.Fill;
            _timeline.SelectedEventChanged += Timeline_SelectedEventChanged;
            splitLeft.Panel1.Controls.Add(_timeline);

            // Список событий + поиск/фильтр
            Panel panelList = new Panel();
            panelList.Dock = DockStyle.Fill;
            splitLeft.Panel2.Controls.Add(panelList);

            _txtSearch = new TextBox();
            // Если у тебя .NET Framework и нет свойства PlaceholderText — просто удали следующую строку.
            // _txtSearch.PlaceholderText = "Поиск по названию / описанию...";
            _txtSearch.Dock = DockStyle.Top;
            _txtSearch.TextChanged += delegate { UpdateBindings(); };
            panelList.Controls.Add(_txtSearch);

            _cmbFilterCategory = new ComboBox();
            _cmbFilterCategory.Dock = DockStyle.Top;
            _cmbFilterCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbFilterCategory.SelectedIndexChanged += delegate { UpdateBindings(); };
            panelList.Controls.Add(_cmbFilterCategory);

            _eventsList = new ListBox();
            _eventsList.Dock = DockStyle.Fill;
            _eventsList.SelectedIndexChanged += EventsList_SelectedIndexChanged;
            panelList.Controls.Add(_eventsList);

            // Панель редактирования справа
            Panel panelEditor = new Panel();
            panelEditor.Dock = DockStyle.Fill;
            panelEditor.Padding = new Padding(8);
            splitMain.Panel2.Controls.Add(panelEditor);

            int y = 10;
            panelEditor.Controls.Add(MakeLabel("Название:", 10, ref y));
            _txtTitle = new TextBox { Left = 10, Top = y, Width = 330 };
            panelEditor.Controls.Add(_txtTitle);
            y += 30;

            panelEditor.Controls.Add(MakeLabel("Категория:", 10, ref y));
            _txtCategory = new TextBox { Left = 10, Top = y, Width = 200 };
            panelEditor.Controls.Add(_txtCategory);
            y += 30;

            panelEditor.Controls.Add(MakeLabel("Начало:", 10, ref y));
            _dtStart = new DateTimePicker
            {
                Left = 10,
                Top = y,
                Width = 200,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy HH:mm"
            };
            panelEditor.Controls.Add(_dtStart);
            y += 30;

            panelEditor.Controls.Add(MakeLabel("Конец:", 10, ref y));
            _dtEnd = new DateTimePicker
            {
                Left = 10,
                Top = y,
                Width = 200,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy HH:mm"
            };
            panelEditor.Controls.Add(_dtEnd);
            y += 30;

            panelEditor.Controls.Add(MakeLabel("Описание:", 10, ref y));
            _txtDescription = new TextBox
            {
                Left = 10,
                Top = y,
                Width = 330,
                Height = 120,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            panelEditor.Controls.Add(_txtDescription);
            y += 130;

            _btnColor = new Button
            {
                Left = 10,
                Top = y,
                Width = 100,
                Text = "Цвет..."
            };
            _btnColor.Click += BtnColor_Click;
            panelEditor.Controls.Add(_btnColor);
            y += 40;

            _btnAdd = new Button
            {
                Left = 10,
                Top = y,
                Width = 80,
                Text = "Добавить"
            };
            _btnAdd.Click += delegate { AddEvent(); };
            panelEditor.Controls.Add(_btnAdd);

            _btnUpdate = new Button
            {
                Left = 100,
                Top = y,
                Width = 80,
                Text = "Обновить"
            };
            _btnUpdate.Click += delegate { UpdateEvent(); };
            panelEditor.Controls.Add(_btnUpdate);

            _btnDelete = new Button
            {
                Left = 190,
                Top = y,
                Width = 80,
                Text = "Удалить"
            };
            _btnDelete.Click += delegate { DeleteEvent(); };
            panelEditor.Controls.Add(_btnDelete);

            // Статусбар
            _status = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel("Готово");
            _status.Items.Add(_statusLabel);
            Controls.Add(_status);
            _status.BringToFront();
        }

        private void EventsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            EventItem ev = _eventsList.SelectedItem as EventItem;
            if (ev != null)
            {
                _timeline.SelectedEvent = ev;
                _timeline.Invalidate();
                FillEditorFromEvent(ev);
            }
        }

        private Label MakeLabel(string text, int x, ref int y)
        {
            Label lbl = new Label { Text = text, Left = x, Top = y, AutoSize = true };
            y += 18;
            return lbl;
        }

        private void SetTheme(bool dark)
        {
            _project.IsDarkTheme = dark;
            BackColor = dark ? Color.FromArgb(40, 40, 40) : SystemColors.Control;
            _timeline.BackColor = dark ? Color.FromArgb(30, 30, 30) : Color.White;
            _timeline.Invalidate();
        }

        private void ChangeScale(TimeScale scale)
        {
            _project.Scale = scale;
            _timeline.Invalidate();
        }

        private void Timeline_SelectedEventChanged(object sender, EventArgs e)
        {
            FillEditorFromEvent(_timeline.SelectedEvent);
            if (_timeline.SelectedEvent != null)
                _eventsList.SelectedItem = _timeline.SelectedEvent;
        }

        private void FillEditorFromEvent(EventItem ev)
        {
            if (ev == null) return;

            _txtTitle.Text = ev.Title;
            _txtCategory.Text = ev.Category;
            _txtDescription.Text = ev.Description;
            _dtStart.Value = ev.Start;
            _dtEnd.Value = ev.End;
            _btnColor.BackColor = ev.Color;
        }

        private void BtnColor_Click(object sender, EventArgs e)
        {
            using (ColorDialog dlg = new ColorDialog())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _btnColor.BackColor = dlg.Color;
                }
            }
        }

        private void AddEvent()
        {
            if (!ValidateEditor()) return;

            EventItem ev = new EventItem
            {
                Title = _txtTitle.Text,
                Category = _txtCategory.Text,
                Description = _txtDescription.Text,
                Start = _dtStart.Value,
                End = _dtEnd.Value,
                Color = _btnColor.BackColor
            };

            _project.Events.Add(ev);
            UpdateBindings();
            _statusLabel.Text = "Событие добавлено";
        }

        private void UpdateEvent()
        {
            EventItem ev = _timeline.SelectedEvent;
            if (ev == null) return;
            if (!ValidateEditor()) return;

            ev.Title = _txtTitle.Text;
            ev.Category = _txtCategory.Text;
            ev.Description = _txtDescription.Text;
            ev.Start = _dtStart.Value;
            ev.End = _dtEnd.Value;
            ev.Color = _btnColor.BackColor;

            UpdateBindings();
            _statusLabel.Text = "Событие обновлено";
        }

        private void DeleteEvent()
        {
            EventItem ev = _timeline.SelectedEvent;
            if (ev == null) return;

            _project.Events.Remove(ev);
            _timeline.SelectedEvent = null;

            UpdateBindings();
            _statusLabel.Text = "Событие удалено";
        }

        private bool ValidateEditor()
        {
            if (string.IsNullOrWhiteSpace(_txtTitle.Text))
            {
                MessageBox.Show("Введите название события.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (_dtEnd.Value < _dtStart.Value)
            {
                MessageBox.Show("Дата окончания не может быть раньше начала.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Обновление списка событий и таймлайна (учёт поиска/фильтра).
        /// </summary>
        private void UpdateBindings()
        {
            _timeline.Project = _project;

            IEnumerable<EventItem> eventsEnum = _project.Events;

            string q = _txtSearch != null ? _txtSearch.Text.Trim() : "";
            if (!string.IsNullOrEmpty(q))
            {
                eventsEnum = eventsEnum.Where(e =>
                    (e.Title != null && e.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (e.Description != null && e.Description.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            string cat = _cmbFilterCategory != null ? _cmbFilterCategory.SelectedItem as string : null;
            if (!string.IsNullOrEmpty(cat) && cat != "<Все категории>")
            {
                eventsEnum = eventsEnum.Where(e =>
                    string.Equals(e.Category, cat, StringComparison.OrdinalIgnoreCase));
            }

            List<EventItem> list = eventsEnum.OrderBy(e => e.Start).ToList();
            _eventsList.DataSource = null;
            _eventsList.DataSource = list;

            List<string> cats = _project.Events
                .Select(e => e.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();
            cats.Insert(0, "<Все категории>");
            _cmbFilterCategory.DataSource = cats;

            _timeline.Invalidate();
        }

        #region Работа с файлами

        private void NewProject()
        {
            _project = new TimelineProject
            {
                ViewStart = DateTime.Today.AddDays(-7),
                ViewEnd = DateTime.Today.AddDays(30)
            };
            _currentFilePath = null;
            UpdateBindings();
            _statusLabel.Text = "Создан новый проект";
        }

        private void OpenProject()
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "Timeline JSON (*.json)|*.json|Все файлы (*.*)|*.*";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        _project = ProjectStorageService.Load(dlg.FileName);
                        _currentFilePath = dlg.FileName;
                        UpdateBindings();
                        _statusLabel.Text = "Проект загружен";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка загрузки: " + ex.Message, "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void SaveProject()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveProjectAs();
                return;
            }

            try
            {
                ProjectStorageService.Save(_project, _currentFilePath);
                _statusLabel.Text = "Проект сохранён";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveProjectAs()
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Filter = "Timeline JSON (*.json)|*.json|Все файлы (*.*)|*.*";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _currentFilePath = dlg.FileName;
                    SaveProject();
                }
            }
        }

        private void ExportImage()
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Filter =
                    "PNG изображение (*.png)|*.png|" +
                    "JPEG изображение (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                    "Все файлы (*.*)|*.*";

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        ImageFormat format = ImageFormat.Png;
                        string ext = System.IO.Path.GetExtension(dlg.FileName);
                        if (!string.IsNullOrEmpty(ext))
                        {
                            ext = ext.ToLowerInvariant();
                            if (ext == ".jpg" || ext == ".jpeg")
                                format = ImageFormat.Jpeg;
                        }

                        // размер картинки можно поменять по вкусу
                        ImageExportService.ExportToImage(_project, dlg.FileName, 1600, 600, format);
                        _statusLabel.Text = "Таймлайн экспортирован в изображение";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка экспорта: " + ex.Message, "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }


        #endregion
    }
}
