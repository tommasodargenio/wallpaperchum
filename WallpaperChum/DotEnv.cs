namespace WallpaperChum
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.IO;
    using System.Reflection;
    public static class DotEnv
    {
        public static bool Load(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            foreach (var line in File.ReadAllLines(filePath))
            {
                string[] parts = line.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2)
                    continue;

                Environment.SetEnvironmentVariable(parts[0], parts[1]);
            }
            return true;
        }

        public static bool LoadResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourcePath = assembly.GetManifestResourceNames().Single(str => str.EndsWith(resourceName));

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            using (StreamReader reader = new StreamReader(stream))
            {
                var line = "";
                while( (line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2)
                        continue;

                    Environment.SetEnvironmentVariable(parts[0], parts[1]);
                }
                
            }

            return true;
        }
    }
}
