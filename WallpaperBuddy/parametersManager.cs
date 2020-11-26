using System;
using System.Collections.Generic;
using System.Linq;


namespace WallpaperBuddy
{
    internal enum parameterType { cli, configFile }
    class parametersManager
    {
        #region Internal Properties        
        private class parameter
        {
            private parameterType _type;
            private string _key;
            private string _help;
            private string _callback;

            public parameterType Type
            {
                get { return _type; }
                set { _type = value; }
            }
            public string Key
            {
                get { return _key; }
                set { _key = value; }
            }
            public string Help
            {
                get { return _help; }
                set { _help = value; }
            }
            public string Callback
            {
                get { return _callback ; }
                set { _callback = value; }
            }

            public override string ToString()
            {
                return Help;                
            }
        }
        private List<parameter> _parameters = new List<parameter>();
        #endregion

               
        #region Public Setters / Getter
        public void addParameter(parameterType type, string key, string help = "", string callback = "")
        {
            if (key != null && key != "")
            {
                parameter newParameter = new parameter();
                newParameter.Type = type;
                newParameter.Key = key;
                newParameter.Help = help;
                newParameter.Callback = callback;
                _parameters.Add(newParameter);
            }
        }
        #endregion

        #region Private Methods
        private void writeLog(string message)
        {
            DateTime dt = DateTime.Now;
            Console.WriteLine(dt.ToString("dd-MMM-yyyy HH:mm:ss: ") + message);            
        }

        // check if a parameter exist with the given key and type
        // @return parameter match
        // @return null if not found
        private parameter parameterExist(string key, bool strict, parameterType type)
        {
            if (key != "")
            {
                if (strict)
                {
                    var matches = _parameters.Where(p => string.Equals(p.Key, key, StringComparison.CurrentCulture) && p.Type == type).First();
                    return matches;
                } else
                {
                    var matches = _parameters.Where(p => string.Equals(p.Key, key, StringComparison.CurrentCulture)).First();
                    return matches;
                }
            } else
            {
                return null;
            }
        }
        #endregion

        #region Public Methods
        // Return total number of parameters stored
        public int totParameters()
        {
            return _parameters.Count();
        }
        // Return the help string all concatenated with newline, ready to be printed
        public override string ToString() 
        {
            string outStr = "";
            if (totParameters()>0)
            {
                for (int i = 0; i < totParameters(); i++)
                {
                    outStr += _parameters[i] + "\n";
                }
            }
            return outStr;
        }

        // Run the callback method for the related parameter
        public void callMethod(string className, string method, string param)
        {
            object methodClass;
            // First try to create an instance of the class.
            try
            {
                Type methodType = Type.GetType(className);
                methodClass = Activator.CreateInstance(methodType);
            }
            catch (Exception ex)
            {
                writeLog("ERROR: Exception caught while creating the class "+ className +" before invoking the argument method " + method + " with message: " + ex.Message);
                Environment.Exit((int)ExitCode.CANT_INSTANCIATE_CLASS);
            }
            // Create and invoke the callback method
            try
            {
                Type methodType = Type.GetType(className);
                methodClass = Activator.CreateInstance(methodType);
                var methodToCall = methodClass.GetType().GetMethod(method);

                if (method != null)
                {
                    if (param != null)
                    {
                        var parameters = new object[] { new object[] { param } };
                        methodToCall.Invoke(methodClass, parameters);
                    }
                    else
                    {
                        methodToCall.Invoke(methodClass, null);
                    }
                } else
                {
                    writeLog("ERROR: Callback method " + method + " not found ");
                    Environment.Exit((int)ExitCode.CALLBACK_NOT_FOUND_OR_INVALID);
                }
            }
            catch (Exception ex)
            {
                writeLog("ERROR: Exception caught while invoking the argument method " + method + " with message: " + ex.Message);
                Environment.Exit(101);
            }
        }

        // Process input parameters, if any parameter is defined and matches one of the input, the relate callback will be executed
        // Input parameters are passed as dictionary with string keys and string values in the format inputParameters["parameterKey"] = "parameterValue"
        // if Strict is true, then it will only consider parameters of parameterType
        public void processParameters(IDictionary<string, string> inputParameters, bool strict, parameterType type) 
        {
            if (inputParameters.Count() <= 0)
            {
                writeLog("ERROR: Parameter list empty or non valid!");
                Environment.Exit((int)ExitCode.MISSING_REQUIRED_PARAMETER);
            }
            
            foreach(KeyValuePair<string, string> entry in inputParameters)
            {
                var match = parameterExist(entry.Key, strict, type);
                if (match != null)
                {
                    callMethod("WallpaperBuddy.Program", match.Callback, entry.Value);
                }
            }
        }
        #endregion
    }
}
