using System;
using System.Data;
using System.Windows.Forms;
using LibraryLoanSystem.DataAccess;
using Microsoft.Data.SqlClient;

namespace LibraryLoanSystem.Forms
{
    public partial class BooksForm : BaseForm
    {
        private int _selectedBookId = 0;

        public BooksForm()
        {
            InitializeComponent();

            if (IsDesignTime)
            {
                return;
            }

            LoadAuthors();
            LoadCategories();
            LoadBooks();

            InputValidationHelper.AttachText(txtTitle, 200);
            InputValidationHelper.AttachNumeric(txtStock, 6);
            // initialize button states
            ClearForm();
        }

        private void LoadAuthors()
        {
            DataTable authors = DbHelper.GetDataTable("SELECT AuthorId, Name FROM Authors ORDER BY Name");
            cmbAuthors.DataSource = authors;
            cmbAuthors.DisplayMember = "Name";
            cmbAuthors.ValueMember = "AuthorId";
            cmbAuthors.SelectedIndex = authors.Rows.Count > 0 ? 0 : -1;
        }

        private void LoadCategories()
        {
            DataTable categories = DbHelper.GetDataTable("SELECT CategoryId, Name FROM Categories ORDER BY Name");
            cmbCategories.DataSource = categories;
            cmbCategories.DisplayMember = "Name";
            cmbCategories.ValueMember = "CategoryId";
            cmbCategories.SelectedIndex = categories.Rows.Count > 0 ? 0 : -1;
        }

        private void LoadBooks()
        {
            dgvBooks.DataSource = DbHelper.GetDataTable(@"
                SELECT 
                    B.BookId,
                    B.Title,
                    A.Name AS Author,
                    C.Name AS Category,
                    B.Stock
                FROM Books B
                INNER JOIN Authors A ON A.AuthorId = B.AuthorId
                INNER JOIN Categories C ON C.CategoryId = B.CategoryId
                ORDER BY B.BookId DESC");
        }

        private void ClearForm()
        {
            _selectedBookId = 0;
            txtTitle.Clear();
            txtStock.Clear();

            if (cmbAuthors.Items.Count > 0) cmbAuthors.SelectedIndex = 0;
            if (cmbCategories.Items.Count > 0) cmbCategories.SelectedIndex = 0;

            txtTitle.Focus();
            // reset action buttons: allow adding, disable update/delete when no selection
            btnAdd.Enabled = true;
            btnUpdate.Enabled = false;
            btnDelete.Enabled = false;
        }

        private bool ValidateForm()
        {
            int stock;

            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show("Kitap adı boş olamaz.");
                return false;
            }

            if (cmbAuthors.SelectedIndex == -1)
            {
                MessageBox.Show("Yazar seç.");
                return false;
            }

            if (cmbCategories.SelectedIndex == -1)
            {
                MessageBox.Show("Kategori seç.");
                return false;
            }

            if (!int.TryParse(txtStock.Text.Trim(), out stock) || stock < 0)
            {
                MessageBox.Show("Stok değeri 0 veya daha büyük sayı olmalı.");
                return false;
            }

            return true;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateForm()) return;

            DbHelper.ExecuteNonQuery(@"
                INSERT INTO Books (Title, AuthorId, CategoryId, Stock)
                VALUES (@Title, @AuthorId, @CategoryId, @Stock)",
                new SqlParameter("@Title", txtTitle.Text.Trim()),
                new SqlParameter("@AuthorId", Convert.ToInt32(cmbAuthors.SelectedValue)),
                new SqlParameter("@CategoryId", Convert.ToInt32(cmbCategories.SelectedValue)),
                new SqlParameter("@Stock", int.Parse(txtStock.Text.Trim()))
            );

            LoadBooks();
            ClearForm();
            MessageBox.Show("Kitap eklendi.");
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (_selectedBookId == 0)
            {
                MessageBox.Show("Güncellenecek kitap seç.");
                return;
            }

            if (!ValidateForm()) return;

            DbHelper.ExecuteNonQuery(@"
                UPDATE Books
                SET Title = @Title,
                    AuthorId = @AuthorId,
                    CategoryId = @CategoryId,
                    Stock = @Stock
                WHERE BookId = @BookId",
                new SqlParameter("@Title", txtTitle.Text.Trim()),
                new SqlParameter("@AuthorId", Convert.ToInt32(cmbAuthors.SelectedValue)),
                new SqlParameter("@CategoryId", Convert.ToInt32(cmbCategories.SelectedValue)),
                new SqlParameter("@Stock", int.Parse(txtStock.Text.Trim())),
                new SqlParameter("@BookId", _selectedBookId)
            );

            LoadBooks();
            ClearForm();
            MessageBox.Show("Kitap güncellendi.");
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedBookId == 0)
            {
                MessageBox.Show("Silinecek kitap seç.");
                return;
            }

            object activeLoanCount = DbHelper.ExecuteScalar(
                "SELECT COUNT(*) FROM Loans WHERE BookId = @BookId AND IsReturned = 0",
                new SqlParameter("@BookId", _selectedBookId)
            );

            if (Convert.ToInt32(activeLoanCount) > 0)
            {
                MessageBox.Show("Bu kitap şu an ödünçte. Silemezsin.");
                return;
            }

            DialogResult result = MessageBox.Show("Bu kitabı silmek istiyor musun?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            DbHelper.ExecuteNonQuery(
                "DELETE FROM Books WHERE BookId = @BookId",
                new SqlParameter("@BookId", _selectedBookId)
            );

            LoadBooks();
            ClearForm();
            MessageBox.Show("Kitap silindi.");
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearForm();
        }

        private void dgvBooks_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dgvBooks.Rows[e.RowIndex];
            _selectedBookId = Convert.ToInt32(row.Cells["BookId"].Value);
            txtTitle.Text = row.Cells["Title"].Value.ToString();
            txtStock.Text = row.Cells["Stock"].Value.ToString();

            string authorName = row.Cells["Author"].Value.ToString();
            string categoryName = row.Cells["Category"].Value.ToString();

            cmbAuthors.Text = authorName;
            cmbCategories.Text = categoryName;
            // when a row is selected, disable Add to prevent accidental duplicate insert
            btnAdd.Enabled = false;
            btnUpdate.Enabled = true;
            btnDelete.Enabled = true;
        }
    }
}