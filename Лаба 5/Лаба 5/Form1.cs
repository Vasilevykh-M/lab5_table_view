using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;

namespace Лаба_5
{
    public partial class Form1 : Form
    {

        private readonly string _connStr = new NpgsqlConnectionStringBuilder
        {
            /*заполнение через файл конфигов*/

            Host = data_base.Default.Host,
            Port = data_base.Default.Port,
            Database = data_base.Default.Name,
            Username = data_base.Default.User,
            Password = data_base.Default.Password,


            /*оптимизация*/

            //MaxAutoPrepare = 10, // 10 запросов будем хранить НЕ БОЛЕЕ
            //AutoPrepareMinUsages = 2, //минимальное кол-во выполнение запроса что бы мы его выполнили
            Pooling = true // По умолчанию
        }.ConnectionString;

        public Form1()
        {
            InitializeComponent();
            Initalaze1();
            show1();
            Initalaze2();
            show2();
        }


        private void Initalaze1()
        {

            dgv1.Columns.Add("id", "id");
            dgv1.Columns["id"].Visible = false;

            dgv1.Columns.Add("name_pr", "Название поставщика");

            dgv1.Columns.Add("code_payment", "Счёт поставщика");

            dgv1.Columns.Add("count_delays", "Кол-во задержек");

            dgv1.Columns.Add(new CalendarColumn
            {
                Name = "data_coop",
                HeaderText = "Дата договора"
            });
        }

        private void show1()
        {
            using (var conn = new NpgsqlConnection(_connStr))
            {
                try
                {
                    conn.Open();
                }
                catch (Exception a)
                {
                    MessageBox.Show(a.Message);
                    throw;
                }

                /*подготавливаем столбцы*/


                using (var sqlCommand = new NpgsqlCommand
                {
                    Connection = conn,
                    CommandText = @"SELECT * FROM provider"
                })
                {
                    var reader = sqlCommand.ExecuteReader();
                    while (reader.Read())
                    {
                        /*строки заполняем*/
                        var rovId = dgv1.Rows.Add(reader["id"], reader["name_pr"],
                            reader["code_payment"], reader["count_delays"], reader["data_coop"]);


                        var prData = new Dictionary<string, object>();

                        foreach (var columnName in new[] { "name_pr", "code_payment", "count_delays", "data_coop" })
                        {
                            prData[columnName] = reader[columnName];
                        }

                        dgv1.Rows[rovId].Tag = prData;

                    }
                }
            }
        }

        private void Initalaze2()
        {
            dgv2.Columns.Add("id", "id");
            dgv2.Columns["id"].Visible = false;

            dgv2.Columns.Add("name_bank", "Название банка");

            dgv2.Columns.Add("lvl_licenses", "Уровень лицензии");

            dgv2.Columns.Add("active", "Активы");
        }

        private void show2()
        {
            using (var conn = new NpgsqlConnection(_connStr))
            {
                try
                {
                    conn.Open();
                }
                catch (Exception a)
                {
                    MessageBox.Show(a.Message);
                    throw;
                }

                    using (var sqlCommand = new NpgsqlCommand
                    {
                        Connection = conn,
                        CommandText = @"SELECT * FROM bank"
                    })
                    {
                        var reader = sqlCommand.ExecuteReader();
                        while (reader.Read())
                        {
                            /*строки заполняем*/
                            var rovId = dgv2.Rows.Add(reader["id"], reader["name_bank"],
                                reader["lvl_licenses"], reader["active"]);

                            var prData = new Dictionary<string, object>();

                            foreach (var columnName in new[] { "name_bank", "lvl_licenses", "active" })
                            {
                                prData[columnName] = reader[columnName];
                            }

                            dgv2.Rows[rovId].Tag = prData;
                        }
                    }
                }
        }

        private void dataGridView1_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.Cancel)
                return;

            var row = dgv1.Rows[e.RowIndex];

            row.ErrorText = "";

            foreach (var columnName in new[] { "name_pr", "code_payment", "count_delays", "data_coop" })
            {
                if (row.Cells[columnName].Value != null) {
                    var cellValue = row.Cells[columnName].Value.ToString();
                    if (string.IsNullOrWhiteSpace(cellValue))
                    {
                        var columnText = row.Cells[columnName].OwningColumn.HeaderText;
                        row.ErrorText = $"Значение в стлобце ' {columnText}'не должно быть пустым";
                        return;
                    }
                }
                else
                {
                    var columnText = row.Cells[columnName].OwningColumn.HeaderText;
                    row.ErrorText = $"Значение в стлобце ' {columnText}'не должно быть пустым";
                    return;
                }
            }

            var newid = (int?)dgv1.Rows[e.RowIndex].Cells["id"].Value;

            string operation = "";

            if (newid.HasValue)
            {
                operation = @"UPDATE provider 
                                    SET name_pr = @name,
                                        code_payment = @code,
                                        count_delays = @count,
                                        data_coop = @date
                                    WHERE id = @newid";
            }
            else
            {
                operation = @"INSERT INTO provider(name_pr, code_payment, count_delays, data_coop) 
                                    VALUES (@name, @code, @count, @date) RETURNING id";
            }

            using (var conn = new NpgsqlConnection(_connStr))
            {
                conn.Open();
                using (var sqlCommand = new NpgsqlCommand
                {
                    Connection = conn,
                    CommandText = operation
                })
                {
                    if(newid.HasValue)
                        sqlCommand.Parameters.AddWithValue("@newid", newid);

                    int count = 0;

                    try
                    {
                        count = int.Parse(dgv1.Rows[e.RowIndex].Cells["count_delays"].Value.ToString());
                    }
                    catch
                    {
                        row.ErrorText = $"Значение в стлобце Кол-во задержек должно быть целочисленным";
                        return;
                    }


                    sqlCommand.Parameters.AddWithValue("@name", dgv1.Rows[e.RowIndex].Cells["name_pr"].Value);
                    sqlCommand.Parameters.AddWithValue("@code", dgv1.Rows[e.RowIndex].Cells["code_payment"].Value);
                    sqlCommand.Parameters.AddWithValue("@date", dgv1.Rows[e.RowIndex].Cells["data_coop"].Value);
                    sqlCommand.Parameters.AddWithValue("@count", count);

                    var prData = new Dictionary<string, object>();

                    foreach (var columnName in new[] { "name_pr", "code_payment", "count_delays", "data_coop" })
                    {
                        prData[columnName] = dgv1.Rows[e.RowIndex].Cells[columnName];
                    }

                    dgv1.Rows[e.RowIndex].Tag = prData;

                    if (!newid.HasValue)
                        dgv1.Rows[e.RowIndex].Cells["id"].Value = sqlCommand.ExecuteReader().Read();
                    else
                        sqlCommand.ExecuteNonQuery();
                }
            }
        }

        private void dataGridView2_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.Cancel)
                return;

            var row = dgv2.Rows[e.RowIndex];

            row.ErrorText = "";

            foreach (var columnName in new[] { "name_bank", "lvl_licenses", "active"})
            {
                if (row.Cells[columnName].Value != null)
                {
                    var cellValue = row.Cells[columnName].Value.ToString();
                    if (string.IsNullOrWhiteSpace(cellValue))
                    {
                        var columnText = row.Cells[columnName].OwningColumn.HeaderText;
                        row.ErrorText = $"Значение в стлобце ' {columnText}'не должно быть пустым";
                        return;
                    }
                }
                else
                {
                    var columnText = row.Cells[columnName].OwningColumn.HeaderText;
                    row.ErrorText = $"Значение в стлобце ' {columnText}'не должно быть пустым";
                    return;
                }
            }

            var newid = (int?)dgv2.Rows[e.RowIndex].Cells["id"].Value;

            string operation = "";

            if (newid.HasValue)
            {
                operation = @"UPDATE bank 
                                    SET name_bank = @name,
                                        lvl_licenses = @lvl,
                                        active = @active
                                    WHERE id = @newid";
            }
            else
            {
                operation = @"INSERT INTO bank(name_bank, lvl_licenses, active) 
                                    VALUES (@name, @lvl, @active) RETURNING id";
            }

            using (var conn = new NpgsqlConnection(_connStr))
            {
                conn.Open();
                using (var sqlCommand = new NpgsqlCommand
                {
                    Connection = conn,
                    CommandText = operation
                })
                {

                    int lvl = 0;
                    int active = 0;

                    try
                    {
                        lvl = int.Parse(dgv2.Rows[e.RowIndex].Cells["lvl_licenses"].Value.ToString());
                    }
                    catch
                    {
                        row.ErrorText = $"Значение в стлобце Уровень лицензии должно быть целочисленным";
                        return;
                    }

                    try
                    {
                        active = int.Parse(dgv2.Rows[e.RowIndex].Cells["active"].Value.ToString());
                    }
                    catch
                    {
                        row.ErrorText = $"Значение в стлобце Активы должно быть целочисленным";
                        return;
                    }


                    using (var sqlCommand1 = new NpgsqlCommand
                    {
                        Connection = conn,
                        CommandText = "SELECT IS_ACTIVE(@act)"
                    })
                    {
                        sqlCommand1.Parameters.AddWithValue("@act", active);
                        if((int)sqlCommand1.ExecuteScalar() == 0)
                        {
                            row.ErrorText = $"Значение в стлобце Активы должно быть больше нуля";
                            return;
                        }
                    }

                    using (var sqlCommand1 = new NpgsqlCommand
                    {
                        Connection = conn,
                        CommandText = "SELECT IS_LVL(@lvl)"
                    })
                    {
                        sqlCommand1.Parameters.AddWithValue("@lvl", lvl);
                        if ((int)sqlCommand1.ExecuteScalar() == 0)
                        {
                            row.ErrorText = $"Значение в стлобце Уровень лицензии должно быть от 0 до 10";
                            return;
                        }
                    }

                    if (newid.HasValue)
                        sqlCommand.Parameters.AddWithValue("@newid", newid);

                    sqlCommand.Parameters.AddWithValue("@name", dgv2.Rows[e.RowIndex].Cells["name_bank"].Value);
                    sqlCommand.Parameters.AddWithValue("@lvl", lvl);
                    sqlCommand.Parameters.AddWithValue("@active", active);

                    var prData = new Dictionary<string, object>();

                    foreach (var columnName in new[] { "name_bank", "lvl_licenses", "active" })
                    {
                        prData[columnName] = dgv2.Rows[e.RowIndex].Cells[columnName];
                    }

                    dgv2.Rows[e.RowIndex].Tag = prData;

                    if (!newid.HasValue)
                        dgv2.Rows[e.RowIndex].Cells["id"].Value = sqlCommand.ExecuteReader().Read();
                    else
                        sqlCommand.ExecuteNonQuery();
                }
            }
        }

        private void dataGridView1_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            var newid = (int?)e.Row.Cells["id"].Value;

            if (!newid.HasValue)
                return;
            using (var conn = new NpgsqlConnection(_connStr))
            {
                conn.Open();

                using (var sqlCommand = new NpgsqlCommand
                {
                    Connection = conn,
                    CommandText = @"DELETE FROM provider WHERE id = @id"
                })
                {
                    sqlCommand.Parameters.AddWithValue("@id", newid);
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        private void dataGridView2_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            var newid = (int?)e.Row.Cells["id"].Value;

            if (!newid.HasValue)
                return;
            using (var conn = new NpgsqlConnection(_connStr))
            {
                conn.Open();

                using (var sqlCommand = new NpgsqlCommand
                {
                    Connection = conn,
                    CommandText = @"DELETE FROM bank WHERE id = @id"
                })
                {
                    sqlCommand.Parameters.AddWithValue("@id", newid);
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        private void dataGridView1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                dgv1.CancelEdit();

                if (dgv1.CurrentRow.Cells["id"].Value != null)
                {
                    foreach (var kvp in (Dictionary<string, object>)dgv1.CurrentRow.Tag)
                    {
                        dgv1.CurrentRow.Cells[kvp.Key].Value = kvp.Value;
                    }
                }
                else
                {
                    dgv1.Rows.Remove(dgv1.CurrentRow);
                }
            }
        }

        private void dataGridView2_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                dgv2.CancelEdit();

                if (dgv2.CurrentRow.Cells["id"].Value != null)
                {
                    foreach (var kvp in (Dictionary<string, object>)dgv2.CurrentRow.Tag)
                    {
                        dgv2.CurrentRow.Cells[kvp.Key].Value = kvp.Value;
                    }
                }
                else
                {
                    dgv2.Rows.Remove(dgv2.CurrentRow);
                }
            }
        }
    }
}
