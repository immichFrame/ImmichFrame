
namespace ImmichFrame.Core.Helpers
{
    public static class DotEnv
    {
        public static void Load(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            foreach (var line in File.ReadAllLines(filePath))
            {
                var index = line.IndexOf('=');

                if (index == -1)
                    continue;

                var variable = line.Substring(0, index);
                var value = line.Substring(index + 1);

                // Container env and tests set these before startup. Do not clobber them.
                if (Environment.GetEnvironmentVariable(variable) != null)
                {
                    continue;
                }

                Environment.SetEnvironmentVariable(variable, value);
            }
        }
    }
}
