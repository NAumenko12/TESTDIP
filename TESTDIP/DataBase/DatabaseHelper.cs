using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TESTDIP.Model;

namespace TESTDIP.DataBase
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper()
        {

            _connectionString = @"Data Source=C:\Users\natac\source\repos\TESTDIP\TESTDIP\DataBase\Listi.db;Version=3;";
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
                                Value = reader.GetString(6) ,
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
    }
}
