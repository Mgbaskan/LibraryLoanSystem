using System;
using System.Windows.Forms;
using LibraryLoanSystem.DataAccess;
using Microsoft.Data.SqlClient;

namespace LibraryLoanSystem.Forms
{
    public partial class CategoriesForm : BaseForm
    {
        private int _selectedCategoryId = 0;

        public CategoriesForm()
        {
            InitializeComponent();

            if (IsDesignTime)
            {
                return;
            }

            LoadCategories();
            InputValidationHelper.AttachText(txtName, 200);
            // initialize button states
            ClearForm();
        }

        private void LoadCategories()
        {
            dgvCategories.DataSource = DbHelper.GetDataTable(
                "SELECT CategoryId, Name FROM Categories ORDER BY CategoryId DESC");
        }

        private void ClearForm()
        {
            _selectedCategoryId = 0;
            txtName.Clear();
            txtName.Focus();
            // reset action buttons: allow adding, disable update/delete when no selection
            btnAdd.Enabled = true;
            btnUpdate.Enabled = false;
            btnDelete.Enabled = false;
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("İsim boş olamaz.");
                return false;
            }

            return true;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateForm()) return;

            DbHelper.ExecuteNonQuery(
                "INSERT INTO Categories (Name) VALUES (@Name)",
                new SqlParameter("@Name", txtName.Text.Trim())
            );

            LoadCategories();
            ClearForm();
            MessageBox.Show("Kategori eklendi.");
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (_selectedCategoryId == 0)
            {
                MessageBox.Show("Güncellenecek kategori seç.");
                return;
            }

            if (!ValidateForm()) return;

            DbHelper.ExecuteNonQuery(
                "UPDATE Categories SET Name = @Name WHERE CategoryId = @CategoryId",
                new SqlParameter("@Name", txtName.Text.Trim()),
                new SqlParameter("@CategoryId", _selectedCategoryId)
            );

            LoadCategories();
            ClearForm();
            MessageBox.Show("Kategori güncellendi.");
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedCategoryId == 0)
            {
                MessageBox.Show("Silinecek kategori seç.");
                return;
            }

            object bookCount = DbHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM Books WHERE CategoryId = @CategoryId",
                new SqlParameter("@CategoryId", _selectedCategoryId)
            );

            if (Convert.ToInt32(bookCount) > 0)
            {
                MessageBox.Show("Bu kategoriye ait kitaplar var. Önce kitapları silin veya kategorisini değiştirin.");
                return;
            }

            DbHelper.ExecuteNonQuery(
                "DELETE FROM Categories WHERE CategoryId = @CategoryId",
                new SqlParameter("@CategoryId", _selectedCategoryId)
            );

            LoadCategories();
            ClearForm();
            MessageBox.Show("Kategori silindi.");
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearForm();
        }

        private void dgvCategories_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = dgvCategories.Rows[e.RowIndex];
            _selectedCategoryId = Convert.ToInt32(row.Cells["CategoryId"].Value);
            txtName.Text = row.Cells["Name"].Value.ToString();
            // when a row is selected, disable Add to prevent accidental duplicate insert
            btnAdd.Enabled = false;
            btnUpdate.Enabled = true;
            btnDelete.Enabled = true;
        }
    }
}
