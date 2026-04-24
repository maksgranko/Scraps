using Scraps.Database;
using static Scraps.Database.Database;
using Scraps.Localization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace Scraps.UI.WinForms
{
    /// <summary>
    /// ComboBox-колонка для Foreign Key с автозагрузкой справочника.
    /// </summary>
    public class DataGridViewFKComboBoxColumn : DataGridViewComboBoxColumn
    {
        /// <summary>Имя таблицы-справочника.</summary>
        public string ReferenceTable { get; set; }

        /// <summary>Колонка ID в справочнике.</summary>
        public string ReferenceIdColumn { get; set; } = "ID";

        /// <summary>Колонка отображения в справочнике (null = авто).</summary>
        public string ReferenceDisplayColumn { get; set; }

        /// <summary>Загрузить данные справочника в колонку.</summary>
        public void LoadLookupData()
        {
            if (string.IsNullOrWhiteSpace(ReferenceTable)) return;

            try
            {
                var dt = GetTableData(ReferenceTable);
                if (dt == null || dt.Rows.Count == 0) return;

                var displayCol = ReferenceDisplayColumn ?? ResolveDisplayColumn(ReferenceTable, ReferenceIdColumn);
                
                DataSource = dt;
                ValueMember = ReferenceIdColumn;
                DisplayMember = displayCol;
                DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox;
                FlatStyle = FlatStyle.Flat;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FKComboBoxColumn.LoadLookupData error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Редактор ячейки DataGridView с ComboBox для выбора значения из справочника.
    /// </summary>
    public class FKCellEditingControl : ComboBox, IDataGridViewEditingControl
    {
        private DataGridView _dataGridView;
        private int _rowIndex;
        private bool _valueChanged;

        /// <inheritdoc/>
        public object EditingControlFormattedValue
        {
            get => SelectedValue?.ToString() ?? Text;
            set
            {
                if (value == null) return;
                var str = value.ToString();
                if (!string.IsNullOrEmpty(str))
                    Text = str;
            }
        }

        /// <inheritdoc/>
        public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context)
            => EditingControlFormattedValue;

        /// <inheritdoc/>
        public void ApplyCellStyleToEditingControl(DataGridViewCellStyle dataGridViewCellStyle)
        {
            Font = dataGridViewCellStyle.Font;
            ForeColor = dataGridViewCellStyle.ForeColor;
            BackColor = dataGridViewCellStyle.BackColor;
        }

        /// <inheritdoc/>
        public DataGridView EditingControlDataGridView
        {
            get => _dataGridView;
            set => _dataGridView = value;
        }

        /// <inheritdoc/>
        public int EditingControlRowIndex
        {
            get => _rowIndex;
            set => _rowIndex = value;
        }

        /// <inheritdoc/>
        public bool EditingControlWantsInputKey(Keys key, bool dataGridViewWantsInputKey)
        {
            return key == Keys.Down || key == Keys.Up;
        }

        /// <inheritdoc/>
        public void PrepareEditingControlForEdit(bool selectAll)
        {
            if (selectAll) SelectAll();
        }

        /// <inheritdoc/>
        public bool RepositionEditingControlOnValueChange => false;

        /// <inheritdoc/>
        public Cursor EditingPanelCursor => Cursors.Default;

        /// <inheritdoc/>
        public bool EditingControlValueChanged
        {
            get => _valueChanged;
            set => _valueChanged = value;
        }

        /// <inheritdoc/>
        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            _valueChanged = true;
            _dataGridView?.NotifyCurrentCellDirty(true);
        }
    }
}
