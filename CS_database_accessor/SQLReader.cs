using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.IO.Ports;
using System.Xml.Linq;

namespace CS_database_accessor
{
  class SQLReader
  {
    private SqlConnection sql_connection;
    private SqlDataReader sql_data_reader;
    private SqlCommand sql_cmd;
    private SerialPort serial_port;
    private bool is_port_open;

    /// <summary>
    /// Constructor for the SQLReader class.
    /// Takes as input the address of the MSSQL Server but does not connect to it
    /// until a query is ready to be called.
    /// </summary>
    /// <param name="sql_address_string"></param>
    public SQLReader(string sql_address_string)
    {
      sql_connection = new SqlConnection(sql_address_string);
      sql_cmd = new SqlCommand();
      sql_cmd.CommandType = CommandType.Text;
      sql_cmd.Connection = sql_connection;
      is_port_open = false;
    }

    ~SQLReader()
    {
      if (is_port_open)
      {
        serial_port.Close();
      }
    }

    /// <summary>
    /// Queries the database. Returns a list of objects.
    /// If the query returns more than one row, 
    /// each row will be one item of the list (a list itself).
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public List<object[]> query_database(string query)
    {
      List<object[]> database_rows = new List<object[]>();
      sql_cmd.CommandText = query;
      sql_connection.Open();
      sql_data_reader = sql_cmd.ExecuteReader();
      
      while (sql_data_reader.Read())
      {
        object[] contents = new object[sql_data_reader.FieldCount];
        //int a = sql_data_reader.GetInt32(0);
        sql_data_reader.GetValues(contents);
        database_rows.Add(contents);
      }
      sql_connection.Close();
      return database_rows;
    }
    public void put_file_in_database(string file_name)
    {
      byte[] file;
      using (var stream = new FileStream(file_name, FileMode.Open, FileAccess.Read))
      {
        using (var reader = new BinaryReader(stream))
        {
          file = reader.ReadBytes((int)stream.Length);
        }
      }
      const string preparedCommand = @"
                    UPDATE [dbo].[acas_table] SET
                               STL_FILE = @File
            WHERE [ID] = @ID
                    ";
      sql_cmd.CommandText = preparedCommand;
      sql_cmd.Parameters.Add("@File", SqlDbType.VarBinary, file.Length).Value = file;
      sql_cmd.Parameters.Add("@ID", SqlDbType.Int).Value = 1;
      sql_connection.Open();
      sql_data_reader = sql_cmd.ExecuteReader();
      sql_connection.Close();
    }
    public void read_database_by_RFID(string RFID)
    {
      string query = @"SELECT * FROM [dbo].[acas_table] WHERE RFID_TAG = '" + RFID + "'";
      List<object[]> query_result = query_database(query);
    }
    public string read_RFID_tag(string port_name, int timeout)
    {
      if (!is_port_open)
      {
        serial_port = new SerialPort();
        serial_port.PortName = port_name;
        serial_port.ReadTimeout = timeout;
        serial_port.Open();
        is_port_open = true;
      }
      
      byte[] bytes = new byte[16];
      int index = 0;
      do
      {
        serial_port.Read(bytes, index, 1);
        index++;
      }
      while (bytes[index - 1] != 3);

      byte[] subarray = new byte[12];
      Array.Copy(bytes, 1, subarray, 0, 12);
      return System.Text.Encoding.Default.GetString(subarray);
    }

    public string read_g_code(string RFID_tag)
    {
      string query = @"SELECT G_CODE FROM [dbo].[acas_table] WHERE RFID_TAG = '" + RFID_tag + "'";
      List<double> matrix_values = new List<double>();
      sql_cmd.CommandText = query;
      sql_connection.Open();
      sql_data_reader = sql_cmd.ExecuteReader();

      sql_data_reader.Read();

      object[] contents = new object[sql_data_reader.FieldCount];
      try
      {
        sql_data_reader.GetValues(contents);
      }
      catch (System.InvalidOperationException)
      {
        sql_connection.Close();
        return null;
      }
      sql_connection.Close();
      return contents[0].ToString();
    }
    public List<double> read_alignment(string RFID_tag)
    {
      string query = @"SELECT ALIGNMENT FROM [dbo].[acas_table] WHERE RFID_TAG = '" + RFID_tag + "'";
      List<double> matrix_values = new List<double>();
      sql_cmd.CommandText = query;
      sql_connection.Open();
      sql_data_reader = sql_cmd.ExecuteReader();

      sql_data_reader.Read();

      object[] contents = new object[sql_data_reader.FieldCount];
      try
      {
        sql_data_reader.GetValues(contents);
      }
      catch (System.InvalidOperationException)
      {
        sql_connection.Close();
        return null;
      }
      string xml_string = sql_data_reader.GetString(0);
      XDocument doc = XDocument.Parse(xml_string);
      XElement element = doc.Root.Element("rotation");
      if (element == null)
      {
        Console.WriteLine("XML does not have rotation");
        sql_connection.Close();
        return null;
      }
      //element.Attribute("values").Value;
      string rotation_values = element.Attribute("values").Value;
      string[] rotation_strings = rotation_values.Split(' ');
      foreach (string rotation in rotation_strings)
      {
        try
        {
          matrix_values.Add(Convert.ToDouble(rotation));
        }
        catch (FormatException)
        {
          Console.WriteLine("Failed to convert XML to array of numbers");
          sql_connection.Close();
          return null;
        }
      }
      if (matrix_values.Count != 9)
      {
        Console.WriteLine("Failed to convert XML to array of numbers");
        sql_connection.Close();
        return null;
      }
      element = doc.Root.Element("translation");
      if (element == null)
      {
        Console.WriteLine("XML does not have trasnlation");
        sql_connection.Close();
        return null;
      }
      //element.Attribute("values").Value;
      string translation_values = element.Attribute("values").Value;
      string[] translation_strings = rotation_values.Split(' ');
      foreach (string translation in translation_strings)
      {
        try
        {
          matrix_values.Add(Convert.ToDouble(translation));
        }
        catch (FormatException)
        {
          Console.WriteLine("Failed to convert XML to array of numbers");
          sql_connection.Close();
          return null;
        }
      }
      if (matrix_values.Count != 9)
      {
        Console.WriteLine("Failed to convert XML to array of numbers");
        sql_connection.Close();
        return null;
      }
      sql_connection.Close();
      return matrix_values;
    }
  }
}
