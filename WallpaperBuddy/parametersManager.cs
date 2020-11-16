using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        }
        private List<parameter> _parameters = new List<parameter>();
        #endregion

        #region Internal Setters / Getters
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
    }
}
