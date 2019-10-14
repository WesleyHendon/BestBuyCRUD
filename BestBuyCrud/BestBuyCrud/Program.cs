using System;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BestBuyCrud
{
    class Program
    {
        protected static readonly string connectionPath = "connectionString.txt";

        static bool programRunning = true;
        static void Main(string[] args)
        {
            programRunning = true;
            ClearConsole(true);
            while (programRunning)
            {
                Console.Write("CRUD Command Input -> ");
                UserInput currentUserInput = new UserInput(Console.ReadLine());
                UserInput.InvalidCommandType commandType = currentUserInput.CommandValidity();

                if (commandType != UserInput.InvalidCommandType.validCommand)
                {
                    InvalidCommandEntered(currentUserInput.GetInvalidCommandString());
                    programRunning = false;
                    break;
                }
                else
                { // Command is valid
                    bool breakFromWhile = false;
                    switch (currentUserInput.command)
                    {
                        case "create":
                            foreach (var toAdd in currentUserInput.stringParameters)
                            {
                                AddDepartment(toAdd);
                            }
                            break;
                        case "read":
                            ClearConsole(true);
                            Console.WriteLine("Reading...");
                            System.Console.WriteLine(GetDepartmentString());
                            break;
                        case "update":
                            UpdateDepartment(currentUserInput.stringParameters[0], currentUserInput.stringParameters[1]);
                            break;
                        case "delete":
                            foreach (var toDelete in currentUserInput.stringParameters)
                            {
                                DeleteDepartment(toDelete);
                            }
                            foreach (var toDelete in currentUserInput.intParameters)
                            {
                                DeleteDepartment(toDelete);
                            }
                            break;
                        case "quit":
                            programRunning = false;
                            breakFromWhile = true;
                            break;
                    }
                    if (breakFromWhile)
                    {
                        break;
                    }
                }
            }
        }

        static void InvalidCommandEntered(string message)
        {
            if (!programRunning) return;

            ClearConsole(false);
            DateTime desired = DateTime.Now.AddSeconds(3.5);
            System.Console.WriteLine($"<!! You have entered an invalid command! !!>");
            Console.WriteLine(message);
            while (DateTime.Now < desired)
            {
                // do nothing but wait
            }
            Main(new string[] { });
        }

        static void ClearConsole(bool displayInstructions)
        {
            Console.Clear();

            if (!displayInstructions) return;
            Console.WriteLine("<-------------------- Program Commands (Commands are case insensitive) -------------------->");
            Console.WriteLine("                    (Include quotes around department names with spaces)");
            Console.WriteLine("<------------------------------------------------------------------------------------------>");
            Console.WriteLine("'Create \"new department name\"': Add this department to the departments table");
            Console.WriteLine("    ---->    (You can include multiple parameters in the 'create' command)    <----");
            Console.WriteLine();
            Console.WriteLine("'Read': Display the current departments table");
            Console.WriteLine();
            Console.WriteLine("'Update \"old department name\" \"new department name\"': Updates a department to a new name");
            Console.WriteLine();
            Console.WriteLine("'Delete \"department name\": Removes this department from the departments table");
            Console.WriteLine("'Delete \"departmentID\": Removes this department from the departments table");
            Console.WriteLine("    ---->    (You can include multiple parameters in the 'delete' command)    <----");
            Console.WriteLine();
            Console.WriteLine("'Quit': Exits the program");
            Console.WriteLine("<------------------------------------------------------------------------------------------>");
        }

        #region String Output Manager
        public static string GetDepartmentString()
        {
            List<Department> departments = ReadDepartments();
            string result = "<--  Departments  -->\n";
            foreach (var department in departments)
            {
                result += department.uniqueKey + ": " + department.departmentName + "\n";
            }
            result += "<------------------->";
            ClearConsole(true);
            return result;
        }
        #endregion

        static MySqlConnection GetMySqlConnection()
        {
            MySqlConnection conn = new MySqlConnection();
            conn.ConnectionString = System.IO.File.ReadAllText(connectionPath);
            return conn;
        }

        #region CRUD Operations

        /* Adds a department to the bestbuy database
         * Parameter: name of department to add
         */
        static void AddDepartment(string departmentName)
        {
            MySqlConnection conn = GetMySqlConnection();
            MySqlCommand cmd = conn.CreateCommand();

            // Prevent sql injection with parameterized statement
            cmd.CommandText = "INSERT INTO departments (Name) VALUES (@departmentName);";
            cmd.Parameters.AddWithValue("departmentName", departmentName);

            using (conn)
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /* Returns the current departments in the best buy database
         *
         */
        static List<Department> ReadDepartments()
        {
            MySqlConnection conn = GetMySqlConnection();
            MySqlCommand cmd = conn.CreateCommand();

            // sql command
            cmd.CommandText = "SELECT * FROM departments";

            List<Department> departments = new List<Department>();
            using (conn)
            {
                conn.Open();
                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    //int testInt = int.Parse((string)reader[0]);
                    //string testString = (string)reader[1];
                    int key = reader.GetInt32("DepartmentID");
                    string name = reader.GetString("Name");
                    departments.Add(new Department(key, name));
                }
            }
            return departments;
        }

        /* Updates a departments name
         * newName: New name of department
         * oldName: Name of department to change
         */
        static void UpdateDepartment(string oldName, string newName)
        {
            MySqlConnection conn = GetMySqlConnection();
            MySqlCommand cmd = conn.CreateCommand();

            // Parameterized sql command
            cmd.CommandText = "UPDATE departments SET Name = @newName WHERE BINARY Name = @oldName AND DepartmentID NOT IN (1,2,3,4);";
            cmd.Parameters.AddWithValue("newName", newName);
            cmd.Parameters.AddWithValue("oldName", oldName);

            using (conn)
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }


        /* Deletes a department from the bestbuy database
         * Parameter: name of department to remove
         */
        static void DeleteDepartment(string departmentName)
        {
            MySqlConnection conn = GetMySqlConnection();
            MySqlCommand selectCmd = conn.CreateCommand();
            MySqlCommand deleteCmd = conn.CreateCommand();

            // Prevent sql injection with parameterized statement
            selectCmd.CommandText = "SELECT DepartmentID FROM departments WHERE BINARY Name = @departmentName";
            selectCmd.Parameters.AddWithValue("departmentName", departmentName);

            int keyToDelete = -1;
            using (conn)
            {
                conn.Open();
                MySqlDataReader reader = selectCmd.ExecuteReader();

                while (reader.Read())
                {
                    keyToDelete = reader.GetInt32("DepartmentID");
                }
            }
            if (keyToDelete >= 0)
            {
                using (conn)
                {
                    try
                    {
                        conn.Open();
                        deleteCmd.CommandText = "DELETE FROM departments WHERE DepartmentID = @departmentID";
                        deleteCmd.Parameters.AddWithValue("departmentID", keyToDelete);
                        deleteCmd.ExecuteNonQuery();
                    }
                    catch (MySqlException)
                    {
                        InvalidCommandEntered("Tried to delete something you shouldnt have, try again!");
                    }
                }
            }
            else
            {
                InvalidCommandEntered($"Failed to find the {departmentName} department");
            }
        }

        static void DeleteDepartment(int departmentID)
        {
            MySqlConnection conn = GetMySqlConnection();
            MySqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = "DELETE FROM departments WHERE DepartmentID = @departmentID;";
            cmd.Parameters.AddWithValue("departmentID", departmentID);

            using (conn)
            {
                conn.Open();
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (MySqlException)
                {
                    InvalidCommandEntered("Tried to delete something you shouldnt have, try again!");
                }
            }
        }
        #endregion
    }

    class UserInput
    {
        public enum InvalidCommandType
        {
            validCommand,
            wrongFormat,
            invalidCommand,
            tooFewParamaters,
            tooManyParameters
        }

        public string fullCommand;
        public string command;
        public List<string> stringParameters = new List<string>();
        public List<int> intParameters = new List<int>();
        public InvalidCommandType invalidCommandType;

        public UserInput(string fullCommand_)
        {
            fullCommand = fullCommand_;
            command = ParseCommand(fullCommand_);
            string parameterString = SeperateParameters(fullCommand);
            stringParameters = ParseStringParameters(parameterString);
            intParameters = ParseIntParameters(parameterString);
            invalidCommandType = CommandValidity();
        }

        public string SeperateParameters(string fullCommand)
        {
            string[] split = fullCommand.Split(' ');
            string parameterString = "";
            for (int i = 1; i < split.Length; i++)
            {
                parameterString += split[i];
                if (i != split.Length - 1)
                {
                    parameterString += " ";
                }
            }
            return parameterString;
        }

        public string ParseCommand(string fullCommand)
        {
            string[] splitString = fullCommand.Split(" ");
            return splitString[0].ToLower();
        }

        public List<int> ParseIntParameters(string withoutCommand)
        {
            List<int> newParameters = new List<int>();

            string currentParameter = "";
            bool buildingParam = false;
            bool usingQuotes = false;
            for (int i = 0; i < withoutCommand.Length; i++)
            {
                char thisChar = withoutCommand[i];
                char nextChar = ' ';
                try
                {
                    nextChar = withoutCommand[i + 1];
                }
                catch (Exception e)
                { // There is no next character
                    if (thisChar != ' ' && thisChar != '"')
                    {
                        currentParameter += thisChar.ToString();
                    }
                    buildingParam = false;
                    usingQuotes = false;
                    string temp = currentParameter;
                    int primaryKey = -1;
                    if (int.TryParse(temp, out primaryKey))
                    {
                        newParameters.Add(primaryKey);
                    }
                    currentParameter = "";
                    continue;
                }
                if (!buildingParam)
                {
                    if (char.IsNumber(thisChar))
                    {
                        buildingParam = true;
                        usingQuotes = false;
                        currentParameter += thisChar.ToString();
                    }
                    else if (thisChar == '"')
                    {
                        buildingParam = true;
                        usingQuotes = true;
                    }
                    else if (thisChar == ' ' && nextChar != '"')
                    {
                        buildingParam = true;
                        usingQuotes = false;
                    }
                }
                else if (buildingParam && usingQuotes && thisChar != '"')
                {
                    currentParameter += thisChar.ToString();
                }
                else if (buildingParam && usingQuotes && thisChar == '"')
                {
                    buildingParam = false;
                    usingQuotes = false;
                    string temp = currentParameter;
                    int primaryKey = -1;
                    if (int.TryParse(temp, out primaryKey))
                    {
                        newParameters.Add(primaryKey);
                    }
                    currentParameter = "";
                }
                else if (buildingParam && !usingQuotes && thisChar != ' ')
                {
                    currentParameter += thisChar.ToString();
                }
                else if (buildingParam && !usingQuotes && thisChar == ' ')
                {
                    buildingParam = false;
                    usingQuotes = false;
                    string temp = currentParameter;
                    int primaryKey = -1;
                    if (int.TryParse(temp, out primaryKey))
                    {
                        newParameters.Add(primaryKey);
                    }
                    currentParameter = "";
                }
            }
            return newParameters;
        }

        public List<string> ParseStringParameters(string withoutCommand)
        {
            List<string> newParameters = new List<string>();

            string currentParameter = "";
            bool buildingParam = false;
            bool usingQuotes = false;
            for (int i = 0; i < withoutCommand.Length; i++)
            {
                char thisChar = withoutCommand[i];
                char nextChar = ' ';
                try
                {
                    nextChar = withoutCommand[i + 1];
                }
                catch (Exception e)
                { // There is no next character
                    if (thisChar != ' ' && thisChar != '"')
                    {
                        currentParameter += thisChar.ToString();
                    }
                    buildingParam = false;
                    usingQuotes = false;
                    string temp = currentParameter;
                    int primaryKey = -1;
                    if (!int.TryParse(temp, out primaryKey))
                    {
                        newParameters.Add(temp);
                    }
                    currentParameter = "";
                    continue;
                }
                if (!buildingParam)
                {
                    if (char.IsLetter(thisChar))
                    {
                        buildingParam = true;
                        usingQuotes = false;
                        currentParameter += thisChar.ToString();
                    }
                    else if (thisChar == '"')
                    {
                        buildingParam = true;
                        usingQuotes = true;
                    }
                    else if (thisChar == ' ' && nextChar != '"')
                    {
                        buildingParam = true;
                        usingQuotes = false;
                    }
                }
                else if (buildingParam && usingQuotes && thisChar != '"')
                {
                    currentParameter += thisChar.ToString();
                }
                else if (buildingParam && usingQuotes && thisChar == '"')
                {
                    buildingParam = false;
                    usingQuotes = false;
                    string temp = currentParameter;
                    int primaryKey = -1;
                    if (!int.TryParse(temp, out primaryKey))
                    {
                        newParameters.Add(temp);
                    }
                    currentParameter = "";
                }
                else if (buildingParam && !usingQuotes && thisChar != ' ')
                {
                    currentParameter += thisChar.ToString();
                }
                else if (buildingParam && !usingQuotes && thisChar == ' ')
                {
                    buildingParam = false;
                    usingQuotes = false;
                    string temp = currentParameter;
                    int primaryKey = -1;
                    if (!int.TryParse(temp, out primaryKey))
                    {
                        newParameters.Add(temp);
                    }
                    currentParameter = "";
                }
            }
            return newParameters;
        }

        public InvalidCommandType CommandValidity()
        {
            int parameterCount = stringParameters.Count + intParameters.Count;
            InvalidCommandType valid = InvalidCommandType.validCommand;
            switch (command)
            {
                case "create":
                    if (parameterCount >= 1) return valid;
                    else if (parameterCount < 1) return InvalidCommandType.tooFewParamaters;
                    else return InvalidCommandType.wrongFormat;
                case "read":
                    if (parameterCount == 0) return valid;
                    else if (parameterCount > 0) return InvalidCommandType.tooManyParameters;
                    else if (parameterCount < 0) return InvalidCommandType.tooFewParamaters;
                    else return InvalidCommandType.wrongFormat;
                case "update":
                    if (parameterCount == 2) return valid;
                    else if (parameterCount > 2) return InvalidCommandType.tooManyParameters;
                    else if (parameterCount < 2) return InvalidCommandType.tooFewParamaters;
                    else return InvalidCommandType.wrongFormat;
                case "delete":
                    if (parameterCount >= 1) return valid;
                    else if (parameterCount < 1) return InvalidCommandType.tooFewParamaters;
                    else return InvalidCommandType.wrongFormat;
                case "quit":
                    if (parameterCount == 0) return valid;
                    else if (parameterCount > 0) return InvalidCommandType.tooManyParameters;
                    else return InvalidCommandType.wrongFormat;
                default:
                    return InvalidCommandType.invalidCommand;
            }
        }

        public string GetInvalidCommandString()
        {
            switch (invalidCommandType)
            {
                case InvalidCommandType.invalidCommand:
                    return $"Command \"{command}\" is invalid";
                case InvalidCommandType.tooFewParamaters:
                    return $"Command \"{fullCommand}\" is missing parameters";
                case InvalidCommandType.tooManyParameters:
                    return $"Command \"{fullCommand}\" has too many parameters";
                case InvalidCommandType.wrongFormat:
                    return $"Command \"{fullCommand}\" is in the wrong format";
                case InvalidCommandType.validCommand:
                    return $"Command \"{fullCommand}\" is valid";
            }
            return command;
        }
    }

    class Department
    {
        public int uniqueKey { get; set; }
        public string departmentName { get; set; }

        public Department(int key, string name)
        {
            uniqueKey = key;
            departmentName = name;
        }
    }
}