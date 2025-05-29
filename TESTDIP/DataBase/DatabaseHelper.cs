using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TESTDIP.Model;

namespace TESTDIP.DataBase
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper()
        {

            _connectionString = @"Data Source=C:\Users\natac\source\repos\TESTDIP\TESTDIP\DataBase\Listi.db;Version=3;";
            InitializeDatabase();
        }


        private void InitializeDatabase()
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    // Создаем только таблицу Расчеты если не существует
                    var createCalculationsTable = @"
                CREATE TABLE IF NOT EXISTS Расчеты (
                    ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    FK_металла INTEGER NOT NULL,
                    Год INTEGER NOT NULL,
                    Широта REAL NOT NULL,
                    Долгота REAL NOT NULL,
                    Концентрация REAL NOT NULL,
                    Расстояние_от_источника REAL,
                    Дата_расчета DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (FK_металла) REFERENCES Металлы(ID)
                )";

                    using (var command = new SQLiteCommand(createCalculationsTable, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    Console.WriteLine("Таблица 'Расчеты' проверена/создана успешно");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при создании таблицы Расчеты: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Проверка существования таблицы
        /// </summary>
        public bool TableExists(string tableName)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var query = "SELECT name FROM sqlite_master WHERE type='table' AND name=@tableName";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@tableName", tableName);
                        var result = command.ExecuteScalar();
                        return result != null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке существования таблицы {tableName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Проверка и создание таблицы Расчеты
        /// </summary>
        public void EnsureCalculationsTableExists()
        {
            try
            {
                if (!TableExists("Расчеты"))
                {
                    using (var connection = new SQLiteConnection(_connectionString))
                    {
                        connection.Open();

                        var createCalculationsTable = @"
                    CREATE TABLE Расчеты (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        FK_металла INTEGER NOT NULL,
                        Год INTEGER NOT NULL,
                        Широта REAL NOT NULL,
                        Долгота REAL NOT NULL,
                        Концентрация REAL NOT NULL,
                        Расстояние_от_источника REAL,
                        Дата_расчета DATETIME DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (FK_металла) REFERENCES Металлы(ID)
                    )";

                        using (var command = new SQLiteCommand(createCalculationsTable, connection))
                        {
                            command.ExecuteNonQuery();
                        }

                        Console.WriteLine("Таблица 'Расчеты' создана успешно");
                    }
                }
                else
                {
                    Console.WriteLine("Таблица 'Расчеты' уже существует");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при создании таблицы Расчеты: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Сохранение результатов расчета в БД
        /// </summary>
        public int SaveCalculationResults(List<GridPoint> gridPoints, int metalId, int year)
        {
            if (gridPoints == null || gridPoints.Count == 0)
                throw new ArgumentException("Нет данных для сохранения");

            // Убеждаемся, что таблица Расчеты существует
            EnsureCalculationsTableExists();

            int savedCount = 0;

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    // Начинаем транзакцию для повышения производительности
                    using (var transaction = connection.BeginTransaction())
                    {
                        var insertQuery = @"
                    INSERT INTO Расчеты 
                    (FK_металла, Год, Широта, Долгота, Концентрация, Расстояние_от_источника, Дата_расчета) 
                    VALUES 
                    (@metalId, @year, @lat, @lon, @concentration, @distance, @date)";

                        using (var command = new SQLiteCommand(insertQuery, connection, transaction))
                        {
                            // Подготавливаем параметры
                            command.Parameters.Add("@metalId", System.Data.DbType.Int32);
                            command.Parameters.Add("@year", System.Data.DbType.Int32);
                            command.Parameters.Add("@lat", System.Data.DbType.Double);
                            command.Parameters.Add("@lon", System.Data.DbType.Double);
                            command.Parameters.Add("@concentration", System.Data.DbType.Double);
                            command.Parameters.Add("@distance", System.Data.DbType.Double);
                            command.Parameters.Add("@date", System.Data.DbType.DateTime);

                            foreach (var point in gridPoints)
                            {
                                command.Parameters["@metalId"].Value = metalId;
                                command.Parameters["@year"].Value = year;
                                command.Parameters["@lat"].Value = point.Lat;
                                command.Parameters["@lon"].Value = point.Lon;
                                command.Parameters["@concentration"].Value = point.Concentration;
                                command.Parameters["@distance"].Value = point.DistanceFromSource;
                                command.Parameters["@date"].Value = DateTime.Now;

                                command.ExecuteNonQuery();
                                savedCount++;
                            }
                        }

                        transaction.Commit();
                    }
                }

                Console.WriteLine($"Успешно сохранено {savedCount} записей в таблицу Расчеты");
                return savedCount;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при сохранении данных в БД: {ex.Message}", ex);
            }
        }
        public bool UpdateSample(Sample sample)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SQLiteCommand(
                        "UPDATE Пробы SET " +
                        "[FK_Металла] = @MetalId, " +
                        "[Значение] = @Value, " +
                        "[Дата_отбора] = @Date, " +
                        "[Номер Аналитики] = @Analytics, " +
                        "[ВИД] = @Type, " +
                        "[Фракция] = @Fraction, " +
                        "[Повторность] = @Repetition " +
                        "WHERE [Id] = @Id", connection);

                    command.Parameters.AddWithValue("@MetalId", sample.Metal.Id);
                    command.Parameters.AddWithValue("@Value", sample.Value);
                    command.Parameters.AddWithValue("@Date", sample.SamplingDate.ToString("yyyy-MM-dd")); // Форматируем дату
                    command.Parameters.AddWithValue("@Analytics", sample.AnalyticsNumber);
                    command.Parameters.AddWithValue("@Type", sample.Type);
                    command.Parameters.AddWithValue("@Fraction", sample.Fraction);
                    command.Parameters.AddWithValue("@Repetition", sample.Repetition);
                    command.Parameters.AddWithValue("@Id", sample.Id);

                    return command.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении пробы: {ex.Message}");
                return false;
            }
        }

      

        public bool DeleteSample(int sampleId)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SQLiteCommand(
                        "DELETE FROM Пробы WHERE [Id] = @Id", connection);

                    command.Parameters.AddWithValue("@Id", sampleId);
                    return command.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении пробы: {ex.Message}");
                return false;
            }
        }
        public List<Location> GetLocations()
        {
            var locations = new List<Location>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string query = @"
                SELECT ID, Название, [Номер площадки], [Расстояние от источника], Описание, широта, долгота 
                FROM Локации";

                using (var command = new SQLiteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        locations.Add(new Location
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            SiteNumber = reader.GetString(2),
                            DistanceFromSource = reader.GetString(3),
                            Description = reader.GetString(4),
                            Latitude = double.Parse(reader.GetString(5)),
                            Longitude = double.Parse(reader.GetString(6))
                        });
                    }
                }
            }

            return locations;
        }

        public List<Sample> GetSamplesForLocation(int locationId)
        {
            var samples = new List<Sample>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string query = @"
                SELECT p.Id, p.FK_Металла, p.ВИД, p.Фракция, p.Повторность, p.Значение, 
                       p.Дата_отбора, p.[Номер Аналитики], m.Название, m.Обозначение, m.ед_изм
                FROM Пробы p
                JOIN Металлы m ON p.FK_Металла = m.ID
                WHERE p.FK_Локации = @LocationId";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@LocationId", locationId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            samples.Add(new Sample
                            {
                                Id = reader.GetInt32(0),
                                MetalId = reader.GetInt32(1),
                                Type = !reader.IsDBNull(2) ? reader.GetString(2) : null,
                                Fraction = reader.GetString(3),
                                Repetition = !reader.IsDBNull(4) ? reader.GetInt32(4) : null,
                                Value = reader.GetString(5),
                                SamplingDate = DateTime.Parse(reader.GetString(6)),
                                AnalyticsNumber = reader.GetString(7),
                                Metal = new Metal
                                {
                                    Id = reader.GetInt32(1),
                                    Name = reader.GetString(8),
                                    Symbol = reader.GetString(9),
                                    Unit = reader.GetString(10)
                                }
                            });
                        }
                    }
                }
            }

            return samples;
        }


        public int AddSample(Sample sample)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string query = @"
            INSERT INTO Пробы (FK_Локации, FK_Металла, ВИД, Фракция, Повторность, Значение, Дата_отбора, [Номер Аналитики])
            VALUES (@LocationId, @MetalId, @Type, @Fraction, @Repetition, @Value, @SamplingDate, @AnalyticsNumber);
            SELECT last_insert_rowid();";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@LocationId", sample.LocationId);
                    command.Parameters.AddWithValue("@MetalId", sample.MetalId);
                    command.Parameters.AddWithValue("@Type", sample.Type ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Fraction", sample.Fraction ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Repetition", sample.Repetition ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Value", sample.Value);
                    command.Parameters.AddWithValue("@SamplingDate", sample.SamplingDate.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@AnalyticsNumber", sample.AnalyticsNumber);

                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        public Sample GetSampleById(int sampleId)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string query = @"
            SELECT p.Id, p.FK_Локации, p.FK_Металла, p.ВИД, p.Фракция, p.Повторность, 
                   p.Значение, p.Дата_отбора, p.[Номер Аналитики],
                   m.Название, m.Обозначение, m.ед_изм,
                   l.Название, l.[Номер площадки]
            FROM Пробы p
            JOIN Металлы m ON p.FK_Металла = m.ID
            JOIN Локации l ON p.FK_Локации = l.ID
            WHERE p.Id = @SampleId";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SampleId", sampleId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Sample
                            {
                                Id = reader.GetInt32(0),
                                LocationId = reader.GetInt32(1),
                                MetalId = reader.GetInt32(2),
                                Type = !reader.IsDBNull(3) ? reader.GetString(3) : null,
                                Fraction = !reader.IsDBNull(4) ? reader.GetString(4) : null,
                                Repetition = !reader.IsDBNull(5) ? reader.GetInt32(5) : (int?)null,
                                Value = reader.GetString(6),
                                SamplingDate = DateTime.Parse(reader.GetString(7)),
                                AnalyticsNumber = reader.GetString(8),
                                Metal = new Metal
                                {
                                    Id = reader.GetInt32(2),
                                    Name = reader.GetString(9),
                                    Symbol = reader.GetString(10),
                                    Unit = reader.GetString(11)
                                },
                                Location = new Location
                                {
                                    Id = reader.GetInt32(1),
                                    Name = reader.GetString(12),
                                    SiteNumber = reader.GetString(13)
                                }
                            };
                        }
                    }
                }
            }
            return null;
        }
        public void AddLocation(Location location)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string query = @"
            INSERT INTO Локации (Название, [Номер площадки], [Расстояние от источника], 
                                Описание, широта, долгота)
            VALUES (@Name, @SiteNumber, @Distance, @Description, @Latitude, @Longitude)";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", location.Name);
                    command.Parameters.AddWithValue("@SiteNumber", location.SiteNumber);
                    command.Parameters.AddWithValue("@Distance", location.DistanceFromSource);
                    command.Parameters.AddWithValue("@Description", location.Description);
                    command.Parameters.AddWithValue("@Latitude", location.Latitude.ToString());
                    command.Parameters.AddWithValue("@Longitude", location.Longitude.ToString());

                    command.ExecuteNonQuery();
                }
            }
        }
        public List<Sample> GetAllSamplesWithLocations()
        {
            var samples = new List<Sample>();
            var connectionString = @"Data Source=C:\Users\natac\source\repos\TESTDIP\TESTDIP\DataBase\Listi.db;Version=3;";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string query = @"
                SELECT p.Id, p.FK_Локации, p.FK_Металла, p.Значение, p.Дата_отбора,
                       l.[Расстояние от источника], m.Название
                FROM Пробы p
                JOIN Локации l ON p.FK_Локации = l.ID
                JOIN Металлы m ON p.FK_Металла = m.ID";

                using (var command = new SQLiteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        samples.Add(new Sample
                        {
                            Id = reader.GetInt32(0),
                            LocationId = reader.GetInt32(1),
                            MetalId = reader.GetInt32(2),
                            Value = reader.GetString(3),
                            SamplingDate = DateTime.Parse(reader.GetString(4)),
                            Location = new Location
                            {
                                DistanceFromSource = reader.GetString(5)
                            },
                            Metal = new Metal
                            {
                                Name = reader.GetString(6)
                            }
                        });
                    }
                }
            }

            return samples;
        }
        public List<Metal> GetMetals()
        {
            var metals = new List<Metal>();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT ID, Название, Обозначение, ед_изм FROM Металлы";

                using (var command = new SQLiteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        metals.Add(new Metal
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Symbol = reader.GetString(2),
                            Unit = reader.GetString(3)
                        });
                    }
                }
            }

            return metals;
        }
        public List<Metal> GetMetalsForLocation(int locationId)
        {
            var metals = new List<Metal>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SQLiteCommand(@"
            SELECT DISTINCT m.ID, m.Название, m.Обозначение, m.ед_изм 
            FROM Металлы m
            JOIN Пробы p ON m.ID = p.FK_Металла
            WHERE p.FK_Локации = @locId",
                    conn);

                cmd.Parameters.AddWithValue("@locId", locationId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Чтение данных металла
                        metals.Add(new Metal
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Symbol = reader.GetString(2),
                            Unit = reader.GetString(3)
                        });
                    }
                }
            }
            return metals;
        }

        public List<Location> GetLocationss()
        {
            var locations = new List<Location>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SQLiteCommand("SELECT ID, широта, долгота FROM Локации", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            var loc = new Location
                            {
                                Id = reader.GetInt32(0),
                                Latitude = ParseCoordinate(reader.GetString(1)), // Парсинг широты
                                Longitude = ParseCoordinate(reader.GetString(2)) // Парсинг долготы
                            };
                            locations.Add(loc);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка чтения локации ID {reader.GetInt32(0)}: {ex.Message}");
                        }
                    }
                }
            }
            return locations;
        }

        private double ParseCoordinate(string value)
        {
            // Замена запятых на точки и удаление пробелов
            var normalized = value.Replace(",", ".").Trim();
            if (double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }
            throw new FormatException($"Некорректный формат координаты: {value}");
        }

        public List<int> GetYearsForLocation(int locationId)
        {
            var years = new HashSet<int>();
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                var cmd = new SQLiteCommand(
                    @"SELECT Дата_отбора FROM Пробы WHERE FK_Локации = @locId",
                    conn);
                cmd.Parameters.AddWithValue("@locId", locationId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(0)) continue;

                        var dateStr = reader.GetString(0);
                        if (DateTime.TryParseExact(dateStr, "dd.MM.yyyy",
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                        {
                            years.Add(date.Year);
                        }
                        else
                        {
                            Console.WriteLine($"Некорректный формат даты: {dateStr}");
                        }
                    }
                }
            }
            return years.OrderBy(y => y).ToList();
        }

        public double? GetMetalConcentration(int locationId, int metalId, int year)
        {
            try
            {
                using (var conn = new SQLiteConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SQLiteCommand(
                        @"SELECT Значение 
                FROM Пробы 
                WHERE 
                    FK_Локации = @locId 
                    AND FK_Металла = @metalId 
                    AND SUBSTR(Дата_отбора, 7, 4) = @year",
                        conn);

                    // Передаем параметры
                    cmd.Parameters.AddWithValue("@locId", locationId);
                    cmd.Parameters.AddWithValue("@metalId", metalId);
                    cmd.Parameters.AddWithValue("@year", year.ToString()); // Год как строка!

                    var result = cmd.ExecuteScalar();
                    return result != null ? ParseStringToDouble(result.ToString()) : null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return null;
            }
        }

        public static double ParseStringToDouble(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return 0.0;

            try
            {
                // Удаляем все нецифровые символы, кроме точки и запятой
                string cleanInput = new string(input.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());

                // Заменяем запятые на точки для корректного парсинга
                cleanInput = cleanInput.Replace(",", ".");

                // Если строка пустая после очистки, возвращаем 0
                if (string.IsNullOrWhiteSpace(cleanInput)) return 0.0;

                // Пробуем разные форматы
                if (double.TryParse(cleanInput, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                {
                    return Math.Round(result, 3);
                }

                // Если не удалось распарсить, пробуем извлечь первое число из строки
                var match = System.Text.RegularExpressions.Regex.Match(input, @"(\d+[.,]?\d*)");
                if (match.Success && double.TryParse(match.Groups[1].Value.Replace(",", "."),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                {
                    return Math.Round(result, 3);
                }

                return 0.0;
            }
            catch
            {
                return 0.0;
            }
        }
        public List<CalculationResultSummary> GetCalculationResultsSummary()
        {
            var results = new List<CalculationResultSummary>();

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var query = @"
                        SELECT 
                            r.FK_металла as MetalId,
                            m.Название as MetalName,
                            r.Год as Year,
                            r.Дата_расчета as CalculationDate,
                            COUNT(*) as PointCount,
                            MIN(r.Концентрация) as MinConcentration,
                            MAX(r.Концентрация) as MaxConcentration,
                            AVG(r.Концентрация) as AvgConcentration,
                            MAX(r.Расстояние_от_источника) as MaxDistance,
                            MIN(r.ID) as Id
                        FROM Расчеты r
                        INNER JOIN Металлы m ON r.FK_металла = m.ID
                        GROUP BY r.FK_металла, r.Год, DATE(r.Дата_расчета)
                        ORDER BY r.Дата_расчета DESC";

                    using (var command = new SQLiteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new CalculationResultSummary
                            {
                                Id = reader.GetInt32("Id"),
                                MetalId = reader.GetInt32("MetalId"),
                                MetalName = reader.GetString("MetalName"),
                                Year = reader.GetInt32("Year"),
                                CalculationDate = reader.GetDateTime("CalculationDate"),
                                PointCount = reader.GetInt32("PointCount"),
                                MinConcentration = reader.GetDouble("MinConcentration"),
                                MaxConcentration = reader.GetDouble("MaxConcentration"),
                                AvgConcentration = reader.GetDouble("AvgConcentration"),
                                MaxDistance = reader.IsDBNull("MaxDistance") ? 0 : reader.GetDouble("MaxDistance")
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении сводки результатов: {ex.Message}", ex);
            }

            return results;
        }

        /// <summary>
        /// Получение детальных данных расчета
        /// </summary>
        public List<GridPoint> GetCalculationDetails(int calculationId)
        {
            var results = new List<GridPoint>();

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var query = @"
                        SELECT Широта, Долгота, Концентрация, Расстояние_от_источника 
                        FROM Расчеты 
                        WHERE FK_металла = (SELECT FK_металла FROM Расчеты WHERE ID = @calculationId LIMIT 1)
                        AND Год = (SELECT Год FROM Расчеты WHERE ID = @calculationId LIMIT 1)
                        AND DATE(Дата_расчета) = (SELECT DATE(Дата_расчета) FROM Расчеты WHERE ID = @calculationId LIMIT 1)
                        ORDER BY Расстояние_от_источника";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@calculationId", calculationId);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(new GridPoint
                                {
                                    Lat = reader.GetDouble("Широта"),
                                    Lon = reader.GetDouble("Долгота"),
                                    Concentration = reader.GetDouble("Концентрация"),
                                    DistanceFromSource = reader.IsDBNull("Расстояние_от_источника") ? 0 : reader.GetDouble("Расстояние_от_источника")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении деталей расчета: {ex.Message}", ex);
            }

            return results;
        }

        /// <summary>
        /// Получение списка годов для которых есть расчеты
        /// </summary>
        public List<int> GetCalculationYears()
        {
            var years = new List<int>();

            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var query = "SELECT DISTINCT Год FROM Расчеты ORDER BY Год DESC";

                    using (var command = new SQLiteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            years.Add(reader.GetInt32("Год"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении списка годов: {ex.Message}", ex);
            }

            return years;
        }
        public bool DeleteCalculationResult(int calculationId)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    var getParamsQuery = @"
                        SELECT FK_металла, Год, DATE(Дата_расчета) as CalcDate 
                        FROM Расчеты 
                        WHERE ID = @calculationId";

                    int metalId;
                    int year;
                    string calcDate;

                    using (var command = new SQLiteCommand(getParamsQuery, connection))
                    {
                        command.Parameters.AddWithValue("@calculationId", calculationId);
                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                                return false;

                            metalId = reader.GetInt32("FK_металла");
                            year = reader.GetInt32("Год");
                            calcDate = reader.GetString("CalcDate");
                        }
                    }

                    // Удаляем все записи этого расчета
                    var deleteQuery = @"
                        DELETE FROM Расчеты 
                        WHERE FK_металла = @metalId 
                        AND Год = @year 
                        AND DATE(Дата_расчета) = @calcDate";

                    using (var command = new SQLiteCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue("@metalId", metalId);
                        command.Parameters.AddWithValue("@year", year);
                        command.Parameters.AddWithValue("@calcDate", calcDate);

                        return command.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при удалении результата расчета: {ex.Message}", ex);
            }
        }
    }
}