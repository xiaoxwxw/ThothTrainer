using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using ThothTrainer.Models;
using System.Collections.ObjectModel;
using System.Data;
using System.Configuration;

namespace ThothTrainer.Managers
{
    internal class UserManager
    {
        private static string myConnectionString = ConfigurationManager.AppSettings["LOCAL_FACES_DATABASE"];

        private static MySqlConnection _connection;

        public static MySqlConnection getConnection()
        {
            if (_connection == null)
            {
                _connection = new MySqlConnection(myConnectionString);
            }
            return _connection;
        }

        /// <summary>
        /// Open connection
        /// </summary>
        /// <returns></returns>
        public static bool Open()
        {
            getConnection();
            try
            {
                if (_connection.State.Equals(ConnectionState.Closed) || _connection.State.Equals(ConnectionState.Broken))
                {
                    _connection.Open();
                    return true;
                }
                if(_connection.State.Equals(ConnectionState.Open))
                {
                    return true;
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Close connection
        /// </summary>
        private static void Close()
        {
            if (_connection == null)
            {
                return;
            }
            try
            {
                _connection.Close();
                _connection.Dispose();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        public static MySqlCommand GetCommand(string sql, bool prepare = true)
        {
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = _connection;
            cmd.CommandText = sql;
            if (prepare)
            {
                cmd.Prepare();
            }

            return cmd;
        }

        public static int AddUserWithFaces(User user, ObservableCollection<Face> faces)
        {
            int result = 0;
            long identity = ValidateUser(user);
            if (identity.Equals(0))
            {
                identity = AddUser(user);
            }
            else
            {
                UpdateUser(identity, user);
            }
            user.ID = identity;

            if (identity > 0 && Open())
            {
                try
                {
                    MySqlCommand faceCommand = GetCommand("INSERT INTO faces (identity, face) VALUES (@identity, @face)");
                    var faceParameter = new MySqlParameter("@face", MySqlDbType.LongBlob);
                    var identityParameter = new MySqlParameter("@identity", MySqlDbType.Int32);
                    identityParameter.Value = identity;

                    foreach (var face in faces)
                    {
                        faceParameter.Value = face.Image;

                        faceCommand.Parameters.Clear();
                        faceCommand.Parameters.Add(identityParameter);
                        faceCommand.Parameters.Add(faceParameter);

                        result += faceCommand.ExecuteNonQuery();
                    }
                    Close();
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            
            return result;
        }

        private static int UpdateUser(long identity, User user)
        {
            int result = 0;
            if (Open())
            {
                var identityParameter = new MySqlParameter("@identity", MySqlDbType.Int32);
                identityParameter.Value = identity;
                var nameParameter = new MySqlParameter("@name", MySqlDbType.VarChar);
                nameParameter.Value = user.Name;
                var emailParameter = new MySqlParameter("@email", MySqlDbType.VarChar);
                emailParameter.Value = user.Email;
                string sql = "UPDATE users SET name = @name, email = @email WHERE identity = @identity";
                MySqlCommand newUserCommand = GetCommand(sql);

                newUserCommand.Parameters.Add(nameParameter);
                newUserCommand.Parameters.Add(emailParameter);
                newUserCommand.Parameters.Add(identityParameter);

                result = newUserCommand.ExecuteNonQuery();
                
                Close();
            }
            return result;
        }

        private static long ValidateUser(User user)
        {
            long identity = 0;
            if (Open())
            {
                var identityParameter = new MySqlParameter("@identity", MySqlDbType.Int32);
                identityParameter.Value = user.ID;

                MySqlCommand userCommand = GetCommand("SELECT * FROM users WHERE identity = @identity");
                userCommand.Parameters.Add(identityParameter);
                MySqlDataReader dataReader = userCommand.ExecuteReader();
                var hasUser = dataReader.HasRows;
                if (hasUser && dataReader.Read())
                {
                    identity = dataReader.GetInt64("identity");
                }
                dataReader.Close();
                Close();
            }
            return identity;
        }

        private static long AddUser(User user)
        {
            long identity = 0;
            if (Open())
            {
                var nameParameter = new MySqlParameter("@name", MySqlDbType.VarChar);
                nameParameter.Value = user.Name;
                var emailParameter = new MySqlParameter("@email", MySqlDbType.VarChar);
                emailParameter.Value = user.Email;

                MySqlCommand newUserCommand = GetCommand("INSERT INTO users (name, email) VALUES (@name, @email)");
                newUserCommand.Parameters.Add(nameParameter);
                newUserCommand.Parameters.Add(emailParameter);

                int result = newUserCommand.ExecuteNonQuery();
                if(result == 1)
                {
                    identity = newUserCommand.LastInsertedId;
                }
                Close();
            }
            return identity;
        }

        public static int QueryUserCount()
        {
            int count = 0;
            string sql = "SELECT COUNT(*) AS UserCount FROM users;";
            DataRow dr = MySqlHelper.ExecuteDataRow(myConnectionString, sql);
            int.TryParse(dr[0].ToString(), out count);
            // Console.WriteLine(count + " rows");
            return count;
        }

        public static DataSet SearchUser()
        {
            string sql = "SELECT users.identity AS ID, users.name AS Name, users.email AS Email, faces.face AS DisplayImage FROM users LEFT JOIN faces ON faces.identity = users.identity";

            DataSet ds = MySqlHelper.ExecuteDataset(myConnectionString, sql);

            return ds;
        }

        public static DataSet SearchUser(User user)
        {
            List<MySqlParameter> userParameters = new List<MySqlParameter>();

            string sql = "SELECT users.identity AS ID, users.name AS Name, users.email AS Email, (SELECT faces.face FROM faces WHERE faces.identity = users.identity ORDER BY rand() LIMIT 1) AS DisplayImage, (SELECT COUNT(identity) FROM faces WHERE faces.identity = users.identity) AS DisplayFaceCount FROM users";
            
            if (string.IsNullOrWhiteSpace(user.Name).Equals(false))
            {
                var nameParameter = new MySqlParameter("@name", MySqlDbType.VarChar);
                nameParameter.Value = "%" + user.Name + "%";

                sql += AppendSQL(userParameters, "name LIKE @name", nameParameter);
            }

            if (string.IsNullOrWhiteSpace(user.Email).Equals(false))
            {
                var emailParameter = new MySqlParameter("@email", MySqlDbType.VarChar);
                emailParameter.Value = "%" + user.Email + "%";

                sql += AppendSQL(userParameters, "users.email LIKE @email", emailParameter);
            }
            if(user.ID > 0)
            {
                var identityParameter = new MySqlParameter("@identity", MySqlDbType.Int32);
                identityParameter.Value = user.ID;

                sql += AppendSQL(userParameters, "identity = @identity", identityParameter);
            }

            DataSet ds = MySqlHelper.ExecuteDataset(myConnectionString, sql, userParameters.ToArray());

            return ds;
        }

        public static int DeleteUser(User user, bool deleteUser = true)
        {
            int result = 0;
            if (Open())
            {
                var identityParameter = new MySqlParameter("@identity", MySqlDbType.Int32);
                identityParameter.Value = user.ID;

                string sql = "DELETE FROM faces WHERE identity = @identity";
                MySqlCommand deleteUserCommand = GetCommand(sql);
                deleteUserCommand.Parameters.Add(identityParameter);
                result = deleteUserCommand.ExecuteNonQuery();

                if (deleteUser)
                {
                    sql = "DELETE FROM users WHERE identity = @identity";
                    deleteUserCommand = GetCommand(sql);
                    deleteUserCommand.Parameters.Add(identityParameter);

                    result = deleteUserCommand.ExecuteNonQuery();
                }

                Close();
            }
            return result;
        }

        private static string AppendSQL(List<MySqlParameter> userParameters, string conditionSQL, MySqlParameter newParameter)
        {
            string sql = "";
            if (userParameters.Count<MySqlParameter>().Equals(0))
            {
                sql += " WHERE ";
            }
            else
            {
                sql += " AND ";
            }
            sql += conditionSQL;
            userParameters.Add(newParameter);
            return sql;
        }

    }
}
