using System;
using System.Data.SqlClient;
using System.Data;
using System.Collections.Generic;


namespace CS_database_accessor
{
  class Program
  {
    static void Main(string[] args)
    {
      SQLReader sql_reader = new SQLReader("Data Source=192.168.4.1\\SQLEXPRESS;Initial Catalog=test_database;User ID=test;Password=test");
      string current_RFID = "";
      Console.WriteLine("Enter the command to execute");
      Console.WriteLine("R - new RFID");
      Console.WriteLine("G - Get G-code for current RFID");
      Console.WriteLine("A - Read alignment for current RFID");
      Console.WriteLine("Q - quit");
      while (true)
      {
        string command = Console.ReadLine();
        command = command.ToUpper();
        if (command == "Q")
        {
          break;
        }
        switch (command)
        {
          case "R":
            while (true)
            {
              Console.WriteLine("Scan RFID tag on the reader");
              try
              {
                current_RFID = sql_reader.read_RFID_tag("COM18", 5000);
                Console.WriteLine(current_RFID);
                break;
              }
              catch (System.TimeoutException)
              {
                Console.WriteLine("Did you forget to scan RFID?");
              }
              catch (System.IO.IOException)
              {
                Console.WriteLine("Failed to open COM port. Is RFID scanner connected?");
                break;
              }
            }
            break;
          case "G":
            if (current_RFID == "")
            {
              Console.WriteLine("No RFID is read. Please scan the tag by pressing R in previous menu");
              break;
            }
            try
            {
              string g_code = sql_reader.read_g_code(current_RFID);
              if (g_code == null)
              {
                Console.WriteLine("Database returned NULL for the given RFID");
              }
              else
              {
                Console.WriteLine(g_code);
              }
              break;
            }
            catch (System.TimeoutException)
            {
              Console.WriteLine("Did you forget to scan RFID?");
            }
            break;
          case "A":
            if (current_RFID == "")
            {
              Console.WriteLine("No RFID is read. Please scan the tag by pressing R in previous menu");
              break;
            }
            try
            {
              sql_reader.read_alignment(current_RFID);
              break;
            }
            catch (System.TimeoutException)
            {
              Console.WriteLine("Did you forget to scan RFID?");
            }
            break;
          default:
            break;
        }

        
      }
    }
  }
}
