using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace CRUDPerson
{
    public partial class Form1 : Form
    {
        // CADENA DE CONEXIÓN CORREGIDA: Encrypt=False para compatibilidad con LocalDB
        private string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=PersonDB;Integrated Security=True;Encrypt=False;Connect Timeout=30;";

        public Form1()
        {
            InitializeComponent();

            try
            {
                // Verificar conexión antes de continuar
                if (!TestConnection())
                {
                    MessageBox.Show("No se pudo conectar a la base de datos. Verifica que LocalDB esté instalado y corriendo.",
                        "Error de Conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Crear la tabla si no existe
                CreateTableIfNotExists();

                // Configurar eventos
                ConfigureEvents();

                // Configurar el DataGridView
                ConfigureDataGridView();

                // Cargar todos los datos al iniciar
                LoadAllData();

                // Deshabilitar botones de actualizar y eliminar al inicio
                btnActulizar.Enabled = false;
                btnEliminar.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al inicializar la aplicación: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region Configuración Inicial

        private bool TestConnection()
        {
            try
            {
                // Conexión a 'master' para verificar la instancia
                string masterConnectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;Encrypt=False;Connect Timeout=10;";

                using (SqlConnection conn = new SqlConnection(masterConnectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}", "Error de Conexión",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void ConfigureEvents()
        {
            btnInsertar.Click += BtnInsertar_Click;
            btnActulizar.Click += BtnActualizar_Click;
            btnEliminar.Click += BtnEliminar_Click;
            btnBuscar.Click += BtnBuscar_Click;
            btnLimpiar.Click += BtnLimpiar_Click;
            btnVertodos.Click += BtnVerTodos_Click;

            // Evento para seleccionar una fila del DataGridView
            dvgDatos.SelectionChanged += DvgDatos_SelectionChanged;
        }

        private void ConfigureDataGridView()
        {
            dvgDatos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dvgDatos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dvgDatos.ReadOnly = true;
            dvgDatos.AllowUserToAddRows = false;
            dvgDatos.AllowUserToDeleteRows = false;
            dvgDatos.RowHeadersVisible = false;
        }

        #endregion

        #region Métodos de Base de Datos con Validaciones

        private void CreateTableIfNotExists()
        {
            try
            {
                string masterConnString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;Encrypt=False;Connect Timeout=30;";

                // 1. Verificar si la base de datos existe
                bool dbExists = false;
                string checkDbQuery = "SELECT COUNT(*) FROM sys.databases WHERE name = 'PersonDB'";

                using (SqlConnection conn = new SqlConnection(masterConnString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(checkDbQuery, conn))
                    {
                        int count = (int)cmd.ExecuteScalar();
                        dbExists = count > 0;
                    }
                }

                // 2. Si no existe la BD, crearla
                if (!dbExists)
                {
                    using (SqlConnection conn = new SqlConnection(masterConnString))
                    {
                        conn.Open();
                        string createDbQuery = "CREATE DATABASE PersonDB";
                        using (SqlCommand cmd = new SqlCommand(createDbQuery, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                // 3. Conectar a PersonDB y crear la tabla si no existe
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string createTableQuery = @"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Persons' AND xtype='U')
                        CREATE TABLE Persons (
                            PersonID INT IDENTITY(1,1) PRIMARY KEY,
                            FirstName NVARCHAR(50) NOT NULL,
                            LastName NVARCHAR(50) NOT NULL
                        )";

                    using (SqlCommand cmd = new SqlCommand(createTableQuery, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al crear la tabla: {ex.Message}");
            }
        }

        private bool PersonExists(int personId)
        {
            string query = "SELECT COUNT(*) FROM Persons WHERE PersonID = @PersonID";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@PersonID", personId);
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        private bool PersonExistsByName(string firstName, string lastName)
        {
            string query = "SELECT COUNT(*) FROM Persons WHERE FirstName = @FirstName AND LastName = @LastName";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FirstName", firstName);
                    cmd.Parameters.AddWithValue("@LastName", lastName);
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        private bool PersonExistsByNameExcludingId(string firstName, string lastName, int personId)
        {
            string query = "SELECT COUNT(*) FROM Persons WHERE FirstName = @FirstName AND LastName = @LastName AND PersonID != @PersonID";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FirstName", firstName);
                    cmd.Parameters.AddWithValue("@LastName", lastName);
                    cmd.Parameters.AddWithValue("@PersonID", personId);
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        private void LoadAllData()
        {
            try
            {
                string query = "SELECT PersonID, FirstName, LastName FROM Persons ORDER BY PersonID DESC";
                DataTable dataTable = new DataTable();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
                    {
                        adapter.Fill(dataTable);
                    }
                }

                dvgDatos.DataSource = dataTable;

                // Renombrar los encabezados de las columnas dinámicamente
                if (dvgDatos.Columns["PersonID"] != null)
                    dvgDatos.Columns["PersonID"].HeaderText = "ID";

                if (dvgDatos.Columns["FirstName"] != null)
                    dvgDatos.Columns["FirstName"].HeaderText = "Nombre";

                if (dvgDatos.Columns["LastName"] != null)
                    dvgDatos.Columns["LastName"].HeaderText = "Apellido";

                // Actualizar contador de registros
                lblRegistros.Text = $"Total de registros: {dataTable.Rows.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los datos: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool InsertPerson(string firstName, string lastName)
        {
            if (PersonExistsByName(firstName, lastName))
            {
                MessageBox.Show($"Ya existe una persona con el nombre '{firstName} {lastName}' en la base de datos.",
                    "Validación - Registro Duplicado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string query = "INSERT INTO Persons (FirstName, LastName) VALUES (@FirstName, @LastName)";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FirstName", firstName);
                    cmd.Parameters.AddWithValue("@LastName", lastName);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        private bool UpdatePerson(int personId, string firstName, string lastName)
        {
            if (!PersonExists(personId))
            {
                MessageBox.Show($"No existe ninguna persona con el ID {personId} en la base de datos.",
                    "Validación - ID No Encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (PersonExistsByNameExcludingId(firstName, lastName, personId))
            {
                MessageBox.Show($"Ya existe otra persona con el nombre '{firstName} {lastName}' en la base de datos.",
                    "Validación - Registro Duplicado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string query = "UPDATE Persons SET FirstName = @FirstName, LastName = @LastName WHERE PersonID = @PersonID";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@PersonID", personId);
                    cmd.Parameters.AddWithValue("@FirstName", firstName);
                    cmd.Parameters.AddWithValue("@LastName", lastName);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        private bool DeletePerson(int personId)
        {
            if (!PersonExists(personId))
            {
                MessageBox.Show($"No existe ninguna persona con el ID {personId} en la base de datos.",
                    "Validación - ID No Encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string query = "DELETE FROM Persons WHERE PersonID = @PersonID";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@PersonID", personId);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

        private DataTable GetPersonById(int personId)
        {
            if (!PersonExists(personId))
            {
                MessageBox.Show($"No existe ninguna persona con el ID {personId} en la base de datos.",
                    "Validación - ID No Encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            string query = "SELECT PersonID, FirstName, LastName FROM Persons WHERE PersonID = @PersonID";
            DataTable dataTable = new DataTable();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@PersonID", personId);

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }

            return dataTable;
        }

        private DataTable SearchPersonsByName(string searchTerm)
        {
            string query = @"SELECT PersonID, FirstName, LastName FROM Persons 
                            WHERE FirstName LIKE @SearchTerm 
                            OR LastName LIKE @SearchTerm 
                            ORDER BY PersonID DESC";

            DataTable dataTable = new DataTable();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }

            return dataTable;
        }

        #endregion

        #region Eventos del Formulario

        private void DvgDatos_SelectionChanged(object sender, EventArgs e)
        {
            if (dvgDatos.SelectedRows.Count > 0)
            {
                DataGridViewRow row = dvgDatos.SelectedRows[0];

                if (row.Cells["PersonID"] != null && row.Cells["PersonID"].Value != null)
                {
                    txtId.Text = row.Cells["PersonID"].Value.ToString();
                    btnActulizar.Enabled = true;
                    btnEliminar.Enabled = true;
                }

                if (row.Cells["FirstName"] != null && row.Cells["FirstName"].Value != null)
                    txtFirstName.Text = row.Cells["FirstName"].Value.ToString();

                if (row.Cells["LastName"] != null && row.Cells["LastName"].Value != null)
                    txtLastName.Text = row.Cells["LastName"].Value.ToString();
            }
        }

        private void BtnInsertar_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtFirstName.Text))
                {
                    MessageBox.Show("El campo Nombre es obligatorio.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtFirstName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtLastName.Text))
                {
                    MessageBox.Show("El campo Apellido es obligatorio.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtLastName.Focus();
                    return;
                }

                if (txtFirstName.Text.Length > 50)
                {
                    MessageBox.Show("El Nombre no puede tener más de 50 caracteres.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtFirstName.Focus();
                    return;
                }

                if (txtLastName.Text.Length > 50)
                {
                    MessageBox.Show("El Apellido no puede tener más de 50 caracteres.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtLastName.Focus();
                    return;
                }

                if (InsertPerson(txtFirstName.Text.Trim(), txtLastName.Text.Trim()))
                {
                    MessageBox.Show("Persona insertada correctamente.", "Éxito",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadAllData();
                    ClearFields();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al insertar: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnActualizar_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtId.Text) || !int.TryParse(txtId.Text, out int personId))
                {
                    MessageBox.Show("Por favor, selecciona una persona para actualizar.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtFirstName.Text))
                {
                    MessageBox.Show("El campo Nombre es obligatorio.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtFirstName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtLastName.Text))
                {
                    MessageBox.Show("El campo Apellido es obligatorio.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtLastName.Focus();
                    return;
                }

                if (txtFirstName.Text.Length > 50)
                {
                    MessageBox.Show("El Nombre no puede tener más de 50 caracteres.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtFirstName.Focus();
                    return;
                }

                if (txtLastName.Text.Length > 50)
                {
                    MessageBox.Show("El Apellido no puede tener más de 50 caracteres.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtLastName.Focus();
                    return;
                }

                if (UpdatePerson(personId, txtFirstName.Text.Trim(), txtLastName.Text.Trim()))
                {
                    MessageBox.Show("Persona actualizada correctamente.", "Éxito",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadAllData();
                    ClearFields();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtId.Text) || !int.TryParse(txtId.Text, out int personId))
                {
                    MessageBox.Show("Por favor, selecciona una persona para eliminar.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult result = MessageBox.Show(
                    $"¿Estás seguro de que deseas eliminar a {txtFirstName.Text} {txtLastName.Text}?",
                    "Confirmar Eliminación",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    if (DeletePerson(personId))
                    {
                        MessageBox.Show("Persona eliminada correctamente.", "Éxito",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadAllData();
                        ClearFields();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnBuscar_Click(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(txtId.Text) && int.TryParse(txtId.Text, out int personId))
                {
                    DataTable result = GetPersonById(personId);

                    if (result != null && result.Rows.Count > 0)
                    {
                        dvgDatos.DataSource = result;
                        DataRow row = result.Rows[0];
                        txtFirstName.Text = row["FirstName"].ToString();
                        txtLastName.Text = row["LastName"].ToString();
                        lblRegistros.Text = "Resultado de búsqueda por ID";
                    }
                }
                else if (!string.IsNullOrWhiteSpace(txtFirstName.Text) || !string.IsNullOrWhiteSpace(txtLastName.Text))
                {
                    string searchTerm = $"{txtFirstName.Text.Trim()} {txtLastName.Text.Trim()}".Trim();
                    DataTable results = SearchPersonsByName(searchTerm);

                    if (results.Rows.Count > 0)
                    {
                        dvgDatos.DataSource = results;
                        lblRegistros.Text = $"Resultados encontrados: {results.Rows.Count}";
                        MessageBox.Show($"Se encontraron {results.Rows.Count} persona(s).", "Resultados",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("No se encontraron personas con ese criterio.", "No Encontrado",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadAllData();
                    }
                }
                else
                {
                    MessageBox.Show("Por favor, ingresa un ID o un nombre para buscar.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnLimpiar_Click(object sender, EventArgs e)
        {
            ClearFields();
        }

        private void BtnVerTodos_Click(object sender, EventArgs e)
        {
            LoadAllData();
            ClearFields();
            btnActulizar.Enabled = false;
            btnEliminar.Enabled = false;
            MessageBox.Show("Mostrando todos los registros.", "Información",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ClearFields()
        {
            txtId.Clear();
            txtFirstName.Clear();
            txtLastName.Clear();
            btnActulizar.Enabled = false;
            btnEliminar.Enabled = false;
            txtId.Focus();
        }

        #endregion
    }
}